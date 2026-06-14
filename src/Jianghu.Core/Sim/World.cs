using System.Collections.Generic;
using System.Threading;
using Jianghu.Actions;
using Jianghu.Config;
using Jianghu.Cultivation;
using Jianghu.Decide;
using Jianghu.Events;
using Jianghu.Model;
using Jianghu.Random;
using Jianghu.Stats;

namespace Jianghu.Sim
{
    public sealed class World : IWorldMutator
    {
        public long Clock { get; private set; }
        public LimitsConfig Limits { get; }
        public Chronicle Chronicle { get; }
        public Relations Relations { get; }
        public List<WorldNode> Nodes { get; }
        public Sect Sect { get; }
        public List<Character> Deceased { get; }

        private readonly Dictionary<long, Character> _alive;
        private readonly Scheduler _sched;
        private readonly ActionSystem _actions;
        private readonly Lifecycle _lifecycle;
        private readonly IRandom _domainRng; // reserved v1.1: 世界级随机事件（当前未消费；删除会改变 root.Split 流编号→改变黄金轨迹）
        public IRandom SpawnRng { get; }
        // 修炼子流 root.Split(5)：仅 cultivation-on 构造（off=null，绝不消费 Split(5)，保 Split(1..4) 编号）。
        // R1 前瞻：升 World 字段 + 进 Clone（CloneRng 深拷，null 安全），为 A.1 运行期消费续跑不发散。
        private readonly IRandom? _cultRng;
        // 路线注册表：仅 on 构造（off=null）。供 SparAction 战斗期按 PathId 查对手路 def + 软情境结算。
        private readonly PathRegistry? _registry;
        private readonly Dictionary<CharacterId, IBrain> _brains;

        public int AliveCount => _alive.Count;
        public int NodeCount => Nodes.Count;

        public World(LimitsConfig limits, IRandom domainRng, IRandom spawnRng, Sect sect, Lifecycle lifecycle,
                     IRandom? cultRng = null, PathRegistry? registry = null)
        {
            Limits = limits; _domainRng = domainRng; SpawnRng = spawnRng; _cultRng = cultRng; _registry = registry; Sect = sect;
            _actions = new ActionSystem(limits, registry); _lifecycle = lifecycle;
            Chronicle = new Chronicle(); Relations = new Relations(); Nodes = new List<WorldNode>();
            Deceased = new List<Character>(); _alive = new Dictionary<long, Character>();
            _sched = new Scheduler(); _brains = new Dictionary<CharacterId, IBrain>();
        }

        // 私有全状态 ctor，仅 Clone 用（深拷贝已在 Clone 内构造好）
        private World(LimitsConfig limits, IRandom domainRng, IRandom spawnRng, IRandom? cultRng, PathRegistry? registry,
                      Sect sect, Lifecycle lifecycle,
                      long clock, Chronicle chronicle, Relations relations, List<WorldNode> nodes,
                      List<Character> deceased, Dictionary<long, Character> alive,
                      Dictionary<CharacterId, IBrain> brains, Scheduler sched)
        {
            Limits = limits; _domainRng = domainRng; SpawnRng = spawnRng; _cultRng = cultRng; _registry = registry; Sect = sect;
            _actions = new ActionSystem(limits, registry); _lifecycle = lifecycle;
            Clock = clock; Chronicle = chronicle; Relations = relations; Nodes = nodes;
            Deceased = deceased; _alive = alive; _brains = brains; _sched = sched;
        }

        public void Add(Character c, IBrain brain)
        {
            _alive[c.Id.Value] = c; _brains[c.Id] = brain;
            c.NextActAt = Clock + _lifecycle.ActionInterval(c, Limits);
            _sched.Push(c.Id, c.NextActAt);
            Chronicle.Append(new CharacterBorn(Clock, c.Id), NameOf);
        }

        public string NameOf(CharacterId id)
        {
            if (_alive.TryGetValue(id.Value, out var c)) return c.Persona.Name;
            foreach (var d in Deceased) if (d.Id.Value == id.Value) return d.Persona.Name + "(故)";
            return "无名";
        }

        public IReadOnlyList<Character> AtNode(NodeId node)
        {
            var list = new List<Character>();
            foreach (var c in _alive.Values) if (c.Node.Value == node.Value) list.Add(c);
            list.Sort((a, b) => a.Id.Value.CompareTo(b.Id.Value)); // 确定性顺序
            return list;
        }

        /// <summary>只读快照：当世角色（按 Id 排序），供展示层渲染人设卡。</summary>
        public IReadOnlyList<Character> AliveCharacters()
        {
            var list = new List<Character>(_alive.Values);
            list.Sort((a, b) => a.Id.Value.CompareTo(b.Id.Value));
            return list;
        }

        /// <summary>只读调度堆快照，供全状态快照对账（堆数组顺序确定，已含续跑等价信息）。</summary>
        public IReadOnlyList<ScheduleItem> SchedulerSnapshot() => _sched.Snapshot();

        public void ApplyStat(Character c, StatKind k, int delta) => c.Stats.Apply(k, delta, Limits);
        public int AdjustRelation(CharacterId f, CharacterId t, int d) => Relations.Adjust(f, t, d);
        public void Move(Character c, NodeId to) => c.Node = to;

        /// <summary>事件驱动推进：处理至多 budget 个到期决策（§7.1）。</summary>
        public int Advance(int budget)
        {
            int processed = 0;
            while (!_sched.IsEmpty && processed < budget)
            {
                var item = _sched.PopMin();
                if (!_alive.TryGetValue(item.Id.Value, out var actor) || !actor.Alive) continue;
                Clock = item.At > Clock ? item.At : Clock;

                var ctx = BuildContext(actor);
                var choice = _brains[actor.Id].DecideAsync(ctx, CancellationToken.None).GetAwaiter().GetResult();
                var events = _actions.Execute(this, actor, choice);
                foreach (var e in events) { Project(actor, e); Chronicle.Append(e, NameOf); }

                AdvanceCultivation(actor); // on：行动后累加本路修为 + 判突破（off=无操作）

                _lifecycle.Tick(actor, this, out var died);
                if (died != null) { Chronicle.Append(died, NameOf); RemoveDead(actor); continue; }

                actor.NextActAt = Clock + _lifecycle.ActionInterval(actor, Limits);
                _sched.Push(actor.Id, actor.NextActAt);
                processed++;
            }
            _lifecycle.MaybeSpawn(this);
            return processed;
        }

        private void RemoveDead(Character c)
        {
            _alive.Remove(c.Id.Value);
            c.Alive = false; Deceased.Add(c);
        }

        public DecisionContext BuildContext(Character a)
        {
            var nearby = new List<NearbyActor>();
            foreach (var other in AtNode(a.Node))
            {
                if (other.Id.Value == a.Id.Value) continue;
                int power = other.Stats.Get(StatKind.Force) * 2 + other.Stats.Get(StatKind.Internal) + other.Stats.Get(StatKind.Constitution);
                nearby.Add(new NearbyActor(other.Id, power, Relations.Affinity(a.Id, other.Id)));
            }
            return new DecisionContext(a.Id, a.Stats, a.Goal, a.Node, nearby, _actions.Types, a.RecallMemory());
        }

        private void Project(Character actor, DomainEvent e)
        {
            switch (e)
            {
                case DuelResolved d:
                    actor.Remember(new MemoryEntry(d.Tick, "spar", d.Winner, d.Loser, d.Winner.Value == actor.Id.Value ? 2 : -2));
                    break;
                case CharacterTrained t:
                    actor.Remember(new MemoryEntry(t.Tick, "train", t.Id, null, 1));
                    break;
            }
        }

        /// <summary>cultivation-on 时是否已构造修炼子流（off=false）。</summary>
        public bool CultivationEnabled => _cultRng != null;

        /// <summary>
        /// 生成期定路接线（spec §10）：仅 cultivation-on。经 _cultRng 生成 Persona.Tags（灵根）→
        /// PathAssigner.Assign 定路 + 选功法战技 → 挂 Character.Cultivation。off（_cultRng==null）→ 无操作。
        /// PathEntered 事件留 Phase 3（Task 3.1 加 record + Chronicle case），本 task 仅挂 Cultivation。
        /// </summary>
        /// <summary>
        /// 运行期涌现者定路接线（Phase 3.5 修正 2）：Lifecycle.MaybeSpawn 补员后调用，
        /// 用 World 自身 _registry 走与初始 spawn 同款定路。off（_cultRng/_registry==null）→ 无操作（逐字节）。
        /// </summary>
        internal void TryAssignCultivation(Character ch)
        {
            if (_cultRng == null || _registry == null) return; // off：不消费 _cultRng（已为 null）
            TryAssignCultivation(ch, _registry);
        }

        internal void TryAssignCultivation(Character ch, PathRegistry registry)
        {
            if (_cultRng == null) return; // off：不消费 _cultRng（已为 null）

            // 灵根 tag 池从注册表派生（Phase2 #6 缺口修）：= 全注册路 EntryGate 所需 tag 并集（排序去重，确定性）。
            // 加路 → 新路 gate tag 自动入池 → 自动可定。空池（无任何 gate tag）→ 不抽 tag，留散修（不消费随机）。
            var pool = registry.RootTagPool();
            if (pool.Count == 0) { ch.Cultivation = null; return; }

            // 灵根 tag（经修炼子流 _cultRng，与 genRng/v1.0 轨迹无交叉）。
            string root = pool[_cultRng.NextInt(pool.Count)];
            ch.Persona = ch.Persona with { Tags = new[] { root } };

            var result = PathAssigner.Assign(ch.Persona.Tags, registry, _cultRng);
            ch.Cultivation = result.State; // 散修时为 null
            // 定路成功（非散修）→ 产 PathEntered 入史（事件单源 §11）。散修不入史。
            if (result.State != null && result.PathId != null)
                Chronicle.Append(new PathEntered(Clock, ch.Id, result.PathId), NameOf);
        }

        // A.0 每次行动累加的本路修为定额（确定性，不掷随机 → 无运行期 _cultRng 消费）。
        private const int CultivationGainPerAction = 1;

        /// <summary>
        /// on：角色行动后累加本路修为 → <see cref="RealmCurve.NextIndexIfReady"/> 判突破（A.0 确定性，
        /// 达阈即升不掷随机，封顶不越界）→ 改 RealmIndex + 产 <see cref="RealmBreakthrough"/> 入史。
        /// off（Cultivation==null）/ 散修 / 无注册表 → 无操作（off 无此路径）。
        /// </summary>
        private void AdvanceCultivation(Character actor)
        {
            var st = actor.Cultivation;
            if (st == null || _registry == null) return;

            // 修为单调累加（显式计数器字段，非资源不经 ApplyResource）。
            st.CultivationPoints += CultivationGainPerAction;

            var curve = _registry.ById(st.PathId).Curve;
            int next = RealmCurve.NextIndexIfReady(st.RealmIndex, st.CultivationPoints, curve);
            if (next != st.RealmIndex)
            {
                st.RealmIndex = next;
                Chronicle.Append(new RealmBreakthrough(Clock, actor.Id, next), NameOf);
            }
        }

        /// <summary>深拷贝快照（v1.0 用 Clone；JSON 序列化是 v1.1）。逝者/Sect/Nodes 在 v1.0 不再变更，浅拷贝引用安全。</summary>
        public World Clone()
        {
            var alive = new Dictionary<long, Character>();
            var brains = new Dictionary<CharacterId, IBrain>();
            foreach (var kv in _alive)
            {
                var cc = kv.Value.Clone();
                alive[kv.Key] = cc;
                brains[cc.Id] = (_brains[kv.Value.Id] is RuleBrain rb) ? rb.Clone() : _brains[kv.Value.Id];
            }
            var deceased = new List<Character>(Deceased);
            var nodes = new List<WorldNode>(Nodes);
            var sched = new Scheduler(); sched.LoadFrom(_sched.Snapshot());
            return new World(Limits, CloneRng(_domainRng), CloneRng(SpawnRng), CloneRngOrNull(_cultRng), _registry, Sect, _lifecycle.Clone(),
                             Clock, Chronicle.Clone(), Relations.Clone(), nodes, deceased, alive, brains, sched);
        }

        private static IRandom CloneRng(IRandom r) { var p = new Pcg32(0, 1); p.SetState(r.GetState()); return p; }

        // R1：_cultRng off 时为 null（不深拷不消费）；on 时深拷续跑不发散。
        private static IRandom? CloneRngOrNull(IRandom? r) => r == null ? null : CloneRng(r);
    }
}
