using System.Collections.Generic;
using System.Threading;
using Jianghu.Actions;
using Jianghu.Config;
using Jianghu.Cultivation;
using Jianghu.Decide;
using Jianghu.Drama;
using Jianghu.Events;
using Jianghu.Model;
using Jianghu.Random;
using Jianghu.Stats;

namespace Jianghu.Sim
{
    public sealed class World : IWorldMutator, IDramaView, IDramaMutator
    {
        public long Clock { get; private set; }
        public LimitsConfig Limits { get; }
        public Chronicle Chronicle { get; }

        /// <summary>命令日志（gh-004 命令端口）：非 null 时 Advance 记录每个 ActionChoice → 供 CLI/Godot 重放。</summary>
        public List<CommandIntent>? CommandLog { get; set; }

        /// <summary>重放队列（gh-004）：非 null 时 Advance 从此出队而非调 RuleBrain。</summary>
        private Queue<CommandIntent>? _replayQueue;
        public Relations Relations { get; }
        public List<WorldNode> Nodes { get; }
        public Sect Sect { get; }
        public List<Character> Deceased { get; }

        /// <summary>地图系统（C）。off=null——不激活，零性能影响。</summary>
        public WorldMap? Map { get; private set; }

        /// <summary>门派系统（D）。off=null——不激活，零性能影响。</summary>
        public SectLedger? Faction { get; private set; }

        /// <summary>接线 Map（story-008）：仅 WorldFactory 构造期调用一次（mapOn）。off 不调 → 保持 null。</summary>
        public void SetMap(WorldMap map) => Map = map;

        /// <summary>接线 Faction（story-008）：仅 WorldFactory 构造期调用一次（factionOn）。off 不调 → 保持 null。</summary>
        public void SetFaction(SectLedger faction) => Faction = faction;

        /// <summary>恩怨账本（戏剧 B）。off=null——不激活，零性能影响。drama-010 接线。</summary>
        public GrudgeLedger? Grudges { get; private set; }

        /// <summary>戏剧编排器（drama-010）。off=null。每 Advance 末尾 Pump（null-guarded）。</summary>
        private DramaDirector? _drama;
        // 戏剧子流 root.Split(6)：仅 dramaOn 构造（off=null，绝不消费 Split(6)，保 Split(1..4) 编号）。
        private IRandom? _dramaRng;

        /// <summary>接线 Drama（drama-010）：仅 WorldFactory 构造期调用一次（dramaOn）。off 不调 → 保持 null。</summary>
        public void SetDrama(GrudgeLedger ledger, DramaDirector director, IRandom dramaRng)
        {
            Grudges = ledger; _drama = director; _dramaRng = dramaRng;
        }

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
            _actions = new ActionSystem(limits, registry, cultRng); _lifecycle = lifecycle;
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
            _actions = new ActionSystem(limits, registry, cultRng); _lifecycle = lifecycle;
            Clock = clock; Chronicle = chronicle; Relations = relations; Nodes = nodes;
            Deceased = deceased; _alive = alive; _brains = brains; _sched = sched;
        }

        // gh-004/play-001：玩家角色注入（P0 最小可玩）—— 不同于 World.Add，
        // 需同时初始化 cultivation state（PathAssigner + CultivationState）。
        public void InjectCharacter(Character c, CultivationPathDef pathDef)
        {
            // 初始化修为（CultivationState 含资源/功法/战技 loadout）
            var chosenArts = new List<string>();
            foreach (var cat in pathDef.ArtCategories)
            {
                if (cat.Role == "daoheart") continue;
                int pick = cat.PickMin;
                var sorted = new List<ArtDef>(cat.Arts);
                sorted.Sort((a, b) => a.Tier.CompareTo(b.Tier));
                int start = System.Math.Max(0, (sorted.Count - pick) / 2);
                for (int i = 0; i < pick && start + i < sorted.Count; i++)
                    chosenArts.Add(sorted[start + i].Id);
            }
            var chosenSkills = new List<string>();
            int skillPick = pathDef.Selection.SkillPickMin;
            if (skillPick > 0 && pathDef.CombatSkills.Count > 0)
            {
                var sortedSkills = new List<CombatSkillDef>(pathDef.CombatSkills);
                sortedSkills.Sort((a, b) => a.Tier.CompareTo(b.Tier));
                int skillStart = System.Math.Max(0, (sortedSkills.Count - skillPick) / 2);
                for (int i = 0; i < skillPick && skillStart + i < sortedSkills.Count; i++)
                    chosenSkills.Add(sortedSkills[skillStart + i].Id);
            }
            var st = CultivationState.NewForPath(pathDef.PathId, pathDef.Resources, chosenArts, chosenSkills);
            st.RealmIndex = 0;
            c.Cultivation = st;
            Add(c, new RuleBrain((_cultRng ?? SpawnRng).Split((ulong)c.Id.Value), c.Persona.Archetype));
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

        /// <summary>gh-004：启用重放模式——Advance 从命令队列出队而非调 RuleBrain。</summary>
        public void SetReplay(IEnumerable<CommandIntent> commands)
        {
            _replayQueue = new Queue<CommandIntent>(commands);
        }

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
                // gh-004 命令端口：重放模式（跳过 RuleBrain，用录制命令）
                ActionChoice choice;
                if (_replayQueue != null && _replayQueue.Count > 0)
                {
                    var cmd = _replayQueue.Dequeue();
                    choice = cmd.ToChoice();
                }
                else
                {
                    choice = _brains[actor.Id].DecideAsync(ctx, CancellationToken.None).GetAwaiter().GetResult();
                }
                // gh-004 命令端口：录制模式（记录 RuleBrain 输出）
                CommandLog?.Add(CommandIntent.FromChoice(Clock, actor.Id.Value, choice));
                var events = _actions.Execute(this, actor, choice);
                foreach (var e in events)
                {
                    Project(actor, e);
                    Chronicle.Append(e, NameOf);
                    // story-010：DuelResolved 入册后再结算门派贡献/晋升 → 晋升行紧随切磋行（顺序正确）。
                    if (e is DuelResolved d) ProjectFactionContribution(d);
                }

                AdvanceCultivation(actor); // on：行动后累加本路修为 + 判突破（off=无操作）

                _lifecycle.Tick(actor, this, out var died);
                if (died != null) { Chronicle.Append(died, NameOf); RemoveDead(actor); continue; }

                actor.NextActAt = Clock + _lifecycle.ActionInterval(actor, Limits);
                _sched.Push(actor.Id, actor.NextActAt);
                processed++;
            }
            _lifecycle.MaybeSpawn(this);
            // Faction tick（story-008/011，闭 C-1）：每 Advance 末推进门派生命周期/税收/夺地（off=Faction null 无操作，
            // 逐字节）。geo=Map（IGeoQuery）；Might 快照经 World 注入（SectLedger 不知 Character 战力，承解耦）。
            // 固定在 MaybeSpawn 后 → 事件顺序确定。夺地易主经返回值投影入 Chronicle。
            if (Faction != null)
            {
                var conquests = Faction.Pump(Clock, Map, BuildFactionMight());
                foreach (var c in conquests) // SectLedger 已按确定性序返回
                    Chronicle.Append(new TerritoryLost(Clock, c.Site, c.From, c.To), NameOf);
            }
            // Drama Pump（drama-010）：每 Advance 末推进到期复仇弧 + 节流点火（off=_drama null 无操作，逐字节）。
            // 固定在 Faction 后 → 事件顺序确定。空库时 Pump 严格 no-op（不消费 _dramaRng）。
            // World 自身实现 IDramaView（读世界态）+ IDramaMutator（Emit→Chronicle+memory）。
            _drama?.Pump(Clock, this, this, _dramaRng!);
            return processed;
        }

        private void RemoveDead(Character c)
        {
            _alive.Remove(c.Id.Value);
            c.Alive = false; Deceased.Add(c);
            // drama-012：复仇者寿尽 → 在世子嗣/弟子继承未了恩怨（off=_drama null 不可达，逐字节）。
            // 先移出 _alive（继承人候选不含死者）再调 OnDeath。Clock 为当前世界时刻。
            if (_drama != null)
                _drama.OnDeath(c.Id, BuildLivingHeirs(c.Id), Clock, this);
        }

        /// <summary>
        /// 死者的在世继承人（drama-012）：profiles 中 Master 或 Bloodline == 死者 的在世角色，
        /// 按 年龄(降，长者先)→武力(降)→Id(升) 三级确定性排序（禁 Dictionary 枚举序）。
        /// </summary>
        private IReadOnlyList<CharacterId> BuildLivingHeirs(CharacterId deceased)
        {
            var heirs = new List<Character>();
            foreach (var c in _alive.Values)
            {
                var p = _drama!.ProfileOf(c.Id);
                if (p == null) continue;
                bool linked = (p.Master.HasValue && p.Master.Value.Value == deceased.Value)
                           || (p.Bloodline.HasValue && p.Bloodline.Value.Value == deceased.Value);
                if (linked) heirs.Add(c);
            }
            heirs.Sort((a, b) =>
            {
                int c = b.Age.CompareTo(a.Age);                 // 年龄降（长者先）
                if (c != 0) return c;
                c = DramaPower(b).CompareTo(DramaPower(a));      // 武力降
                if (c != 0) return c;
                return a.Id.Value.CompareTo(b.Id.Value);         // Id 升
            });
            var ids = new List<CharacterId>(heirs.Count);
            foreach (var h in heirs) ids.Add(h.Id);
            return ids;
        }

        /// <summary>注册师承/血缘侧表（drama-012）：仅 drama-on 有效（_drama!=null）。off 无操作。</summary>
        public void RegisterDramaProfile(DramaProfile profile) => _drama?.RegisterProfile(profile);

        // story-011：per-faction Might 快照（Σ 在役成员 power，与 RuleBrain.SelfPower 同公式 Force×2+Int+Con）。
        // World 算（知 Character.Stats）→ 注入 Pump，SectLedger 保持对 Character 无知（承 design 解耦）。
        private Dictionary<int, int> BuildFactionMight()
        {
            var might = new Dictionary<int, int>();
            if (Faction == null) return might;
            foreach (var c in _alive.Values)
            {
                int fid = Faction.FactionOf(c.Id);
                if (fid == 0) continue; // 散修不计
                int power = c.Stats.Get(StatKind.Force) * 2 + c.Stats.Get(StatKind.Internal) + c.Stats.Get(StatKind.Constitution);
                might.TryGetValue(fid, out int cur);
                might[fid] = cur + power;
            }
            return might;
        }

        public DecisionContext BuildContext(Character a)
        {
            var nearby = new List<NearbyActor>();
            foreach (var other in AtNode(a.Node))
            {
                if (other.Id.Value == a.Id.Value) continue;
                // 战力度量须与 DuelEngine 一致（2026-07-03 机制修复）：on 且对方有修为 → 用 PowerEngine.Evaluate
                // （含 realm 倍率，实战真值）；off/无修为 → 回退 raw stats（B.3 逐字节，off 不调 registry）。
                // 修复前一律 raw stats → brain 对修为盲，把 999 碾压当势均力敌反复切磋。
                int power;
                if (_registry != null && other.Cultivation != null)
                    power = PowerEngine.Evaluate(other.Cultivation, other.Stats,
                                                 _registry.ById(other.Cultivation.PathId), Limits);
                else
                    power = other.Stats.Get(StatKind.Force) * 2 + other.Stats.Get(StatKind.Internal) + other.Stats.Get(StatKind.Constitution);
                nearby.Add(new NearbyActor(other.Id, power, Relations.Affinity(a.Id, other.Id)));
            }

            // Map-on: Reachable nodes from current position
            System.Collections.Generic.IReadOnlyList<NodeId>? reachable = null;
            if (Map != null)
                reachable = Map.ReachableFrom(a.Node);

            // Faction-on: faction context
            int factionId = 0, factionRank = 0, factionRep = 0;
            if (Faction != null)
            {
                factionId = Faction.FactionOf(a.Id);
                factionRank = Faction.RankOf(a.Id);
                factionRep = 0; // reputation not tracked yet
            }

            // 自身战力（与 nearby.Power 同度量）：on 且有修为 → PE；否则 raw stats（off 逐字节：0 时 RuleBrain 回退）。
            int selfPower = 0;
            if (_registry != null && a.Cultivation != null)
                selfPower = PowerEngine.Evaluate(a.Cultivation, a.Stats,
                                                 _registry.ById(a.Cultivation.PathId), Limits);

            return new DecisionContext(a.Id, a.Stats, a.Goal, a.Node, nearby, _actions.Types, a.RecallMemory(),
                Reachable: reachable, FactionId: factionId, FactionRank: factionRank, FactionReputation: factionRep,
                SelfPower: selfPower);
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

        // —— IDramaView（drama-010）：戏剧只读世界视图。Power 同 RuleBrain.SelfPower/SparAction 公式。——
        int IDramaView.Power(CharacterId who)
            => _alive.TryGetValue(who.Value, out var c) ? DramaPower(c) : 0;

        int IDramaView.Affinity(CharacterId from, CharacterId to) => Relations.Affinity(from, to);

        bool IDramaView.IsAlive(CharacterId who)
            => _alive.TryGetValue(who.Value, out var c) && c.Alive;

        bool IDramaView.SameNode(CharacterId a, CharacterId b)
        {
            if (!_alive.TryGetValue(a.Value, out var ca) || !_alive.TryGetValue(b.Value, out var cb)) return false;
            return ca.Node.Value == cb.Node.Value;
        }

        Goal IDramaView.GoalOf(CharacterId who)
            => _alive.TryGetValue(who.Value, out var c) ? c.Goal : new Goal(GoalKind.Wander, 0);

        private static int DramaPower(Character c)
            => c.Stats.Get(StatKind.Force) * 2 + c.Stats.Get(StatKind.Internal) + c.Stats.Get(StatKind.Constitution);

        // —— IDramaMutator（drama-010/011）：戏剧唯一写口。事件 + 受控耦合（Goal/Relations，drama→decision 唯一通道）。——
        void IDramaMutator.Emit(DomainEvent e)
        {
            Chronicle.Append(e, NameOf);
            ProjectDrama(e);
        }

        // drama-011 受控耦合写口：覆写/还原 Goal（疯修通道）+ 镜像负 Relations（notFoe 通道）。
        // 仅 drama Pump 路径可达（off=_drama null 不调）→ off 逐字节守恒。RuleBrain 零改，纯经既有项生效。
        void IDramaMutator.OverrideGoal(CharacterId who, GoalKind kind)
        {
            if (_alive.TryGetValue(who.Value, out var c)) c.Goal = new Goal(kind, 0);
        }

        void IDramaMutator.RestoreGoal(CharacterId who, Goal original)
        {
            if (_alive.TryGetValue(who.Value, out var c)) c.Goal = original;
        }

        void IDramaMutator.MirrorRelation(CharacterId holder, CharacterId target, int delta)
            => Relations.Adjust(holder, target, delta); // Relations 自身钳 [-100,100]

        // drama 事件 memory 投影：复仇结局写参与者负 valence（大恨入记忆，仿 spar memory）。
        // 仅 drama 事件，不碰既有 Project。off 不可达（_drama==null 时 Emit 不被调）。
        private void ProjectDrama(DomainEvent e)
        {
            if (e is RevengeConsummated rc)
            {
                if (_alive.TryGetValue(rc.Avenger.Value, out var av))
                    av.Remember(new MemoryEntry(rc.Tick, "revenge", rc.Avenger, rc.Target, rc.AvengerPrevailed ? 3 : -3));
                if (_alive.TryGetValue(rc.Target.Value, out var tg))
                    tg.Remember(new MemoryEntry(rc.Tick, "revenge", rc.Target, rc.Avenger, -3));
            }
        }

        // story-010：门派贡献驱动晋升最薄反馈环（design §3.3）。
        // 胜方若为门派成员 → 贡献度 +base+margin；过阈 → Promote + FactionPromoted 入 Chronicle。
        // off/factionOff（Faction==null）→ 整体无操作（保 off 逐字节 B.3）。纯整数确定性。
        private const int ContribWinBase = 10;      // 每场切磋胜基础贡献
        private const int ContribPerRankThreshold = 50; // 每升一阶所需累计贡献
        private void ProjectFactionContribution(DuelResolved d)
        {
            if (Faction == null) return;
            if (Faction.FactionOf(d.Winner) == 0) return; // 散修不累
            int gain = ContribWinBase + (d.Margin > 0 ? d.Margin : 0);
            Faction.AddContribution(d.Winner, gain);

            // 过阈晋升：累计贡献 ≥ (当前阶+1)×阈值 且未达 cap(3) → 升一阶 + 发事件。
            int rank = Faction.RankOf(d.Winner);
            if (rank < 3 && Faction.ContributionOf(d.Winner) >= (rank + 1) * ContribPerRankThreshold)
            {
                Faction.Promote(d.Winner);
                Chronicle.Append(
                    new FactionPromoted(d.Tick, d.Winner, Faction.FactionOf(d.Winner), Faction.RankOf(d.Winner)),
                    NameOf);
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
        // Viability 调平（2026-07-03 用户指令 Step 2）：1→8。原值使首破(阈100)需~100行动≈1100 tick，
        // 超中位寿命(~900)→大部分实体老死前零破境（闭关到老死死锁）。提到 8 → 中位寿命内可破 3-4 境，
        // 成长线跑通。纯整数（B.2）；仅 cultivation-on 调（B.3 off 不受影响）。
        private const int CultivationGainPerAction = 8;

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
                Chronicle.Append(new RealmBreakthrough(Clock, actor.Id, next), NameOf,
                                 fi => RealmQuery.Describe(curve, fi).Display);  // A1.5 大小境界·UT 渲染（on 专属，off 不入此径）
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
            var w = new World(Limits, CloneRng(_domainRng), CloneRng(SpawnRng), CloneRngOrNull(_cultRng), _registry, Sect, _lifecycle.Clone(),
                             Clock, Chronicle.Clone(), Relations.Clone(), nodes, deceased, alive, brains, sched);
            if (Map != null) w.Map = Map.Clone();           // 拓扑不可变，浅拷安全
            if (Faction != null) w.Faction = Faction.Clone(); // 深拷成员+关系
            // Drama（drama-010）：账本只克隆一份，director 复用同一克隆实例（否则 director 操作的账本
            // ≠ w.Grudges → 漂移）。dramaRng 深拷续跑（R-NF2）。off=三者 null 不拷。
            if (_drama != null && Grudges != null && _dramaRng != null)
            {
                var clonedLedger = Grudges.Clone();
                w.SetDrama(clonedLedger, _drama.Clone(clonedLedger), CloneRng(_dramaRng));
            }
            return w;
        }

        private static IRandom CloneRng(IRandom r) { var p = new Pcg32(0, 1); p.SetState(r.GetState()); return p; }

        // R1：_cultRng off 时为 null（不深拷不消费）；on 时深拷续跑不发散。
        private static IRandom? CloneRngOrNull(IRandom? r) => r == null ? null : CloneRng(r);
    }
}
