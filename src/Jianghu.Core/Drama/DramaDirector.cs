using System.Collections.Generic;
using Jianghu.Config;
using Jianghu.Events;
using Jianghu.Model;
using Jianghu.Random;
using Jianghu.Util;

namespace Jianghu.Drama
{
    /// <summary>
    /// 戏剧引擎编排核心（drama-009b，spec §3.4）。持戏剧可变态，每次世界 Advance 末尾调一次
    /// <see cref="Pump"/>，把积木（账本/调度器/状态机/点火扫描/轮盘/事件汇）串成
    /// 「推进到期弧 + 节流点火新弧」流程。
    ///
    /// **空库严格 no-op**（B.3）：空恩怨库 + 无活跃弧 → 不消费 rng、不产事件。
    /// **确定性**（B.2）：点火主流串行消费 rng；裁决序经 IgnitionScanner/WeightedPicker 已定。
    /// **续跑**（R-NF2）：全可变态经 <see cref="Clone"/> 深拷（drama-010 World.Clone 调用）。
    /// </summary>
    public sealed class DramaDirector
    {
        private readonly GrudgeLedger _ledger;      // 引用（drama-010 World 持有同一实例）
        private readonly LimitsConfig _limits;
        private readonly DramaScheduler _scheduler;
        private readonly List<ArcInstance> _activeArcs;
        private readonly Dictionary<(long, long), long> _pairCooldownUntil; // 对子→冷却到期 tick（仅成员测试）
        private readonly Dictionary<long, Goal> _originalGoals; // 弧 Id → 复仇者原 Goal（drama-011 收束还原）
        private readonly Dictionary<long, DramaProfile> _profiles; // Self.Value → 师承/血缘侧表（drama-012 继承）
        private long _nextIgnitionCheckAt;
        private long _nextArcId;

        public DramaDirector(GrudgeLedger ledger, LimitsConfig limits)
        {
            _ledger = ledger;
            _limits = limits;
            _scheduler = new DramaScheduler();
            _activeArcs = new List<ArcInstance>();
            _pairCooldownUntil = new Dictionary<(long, long), long>();
            _originalGoals = new Dictionary<long, Goal>();
            _profiles = new Dictionary<long, DramaProfile>();
            _nextIgnitionCheckAt = 0;
            _nextArcId = 1;
        }

        private DramaDirector(GrudgeLedger ledger, LimitsConfig limits, DramaDirector src)
        {
            _ledger = ledger;
            _limits = limits;
            _scheduler = new DramaScheduler();
            _scheduler.LoadFrom(src._scheduler.Snapshot());
            _activeArcs = new List<ArcInstance>(src._activeArcs); // ArcInstance record 不可变，浅拷即深拷
            _pairCooldownUntil = new Dictionary<(long, long), long>(src._pairCooldownUntil);
            _originalGoals = new Dictionary<long, Goal>(src._originalGoals); // Goal 不可变 record，浅拷即深拷
            _profiles = new Dictionary<long, DramaProfile>(src._profiles);   // DramaProfile 不可变 record
            _nextIgnitionCheckAt = src._nextIgnitionCheckAt;
            _nextArcId = src._nextArcId;
        }

        /// <summary>当前活跃弧（确定性序，只读）。</summary>
        public IReadOnlyList<ArcInstance> ActiveArcs => _activeArcs;

        /// <summary>
        /// 每 Advance 末尾调一次。A 推进相（弹到期弧≤DramaBudget）+ B/C 节流点火相。
        /// 空库 + 无活跃弧 → no-op（点火相空候选在抽取前返回 → rng 不消费）。
        /// </summary>
        public void Pump(long clock, IDramaView view, IDramaMutator mutator, IRandom rng)
        {
            AdvancePhase(clock, view, mutator);
            IgnitionPhase(clock, view, mutator, rng);
        }

        // —— A 推进相 ——
        private void AdvancePhase(long clock, IDramaView view, IDramaMutator mutator)
        {
            int stepped = 0;
            while (stepped < _limits.DramaBudget && _scheduler.HasDue(clock))
            {
                var item = _scheduler.PopMin();
                int idx = IndexOfArc(item.Arc);
                if (idx < 0) continue; // 陈旧调度项（弧已收束）→ skip

                var arc = _activeArcs[idx];
                var trans = RevengeArc.TryAdvance(arc, view, _limits);
                EmitForTransition(clock, arc, trans, mutator);
                ApplyCoupling(arc, trans, view, mutator); // drama-011 受控耦合（Goal/Relations）

                if (trans.Resolution == ArcResolution.Completed || trans.Resolution == ArcResolution.Abandoned)
                {
                    _activeArcs.RemoveAt(idx); // 退场，不再排
                }
                else
                {
                    _activeArcs[idx] = trans.Next; // Advanced/Stalled：更新 + 重排
                    _scheduler.Push(trans.Next.Id, clock + StageDelay(trans.Next.Stage));
                }
                stepped++;
            }
        }

        private void EmitForTransition(long clock, ArcInstance prev, ArcTransition trans, IDramaMutator mutator)
        {
            switch (trans.Resolution)
            {
                case ArcResolution.Advanced:
                    mutator.Emit(new ArcStageEntered(clock, trans.Next.Id, trans.Next.Stage));
                    break;
                case ArcResolution.Completed:
                    mutator.Emit(new RevengeConsummated(clock, trans.Next.Id, prev.Avenger, prev.Target, trans.AvengerPrevailed));
                    break;
                case ArcResolution.Abandoned:
                    mutator.Emit(new ArcAbandoned(clock, trans.Next.Id, "ParticipantDied"));
                    break;
                // Stalled：无事件，仅重排（待下次重试）。
            }
        }

        // —— drama-011 受控耦合：经 IDramaMutator 两条 RuleBrain 既有通道间接驱动行为（RuleBrain 零改）。——
        // BuildUp→覆写 Goal=Advance（疯修）；Hunting→镜像负 Relations（触发 notFoe）；收束→还原原 Goal。
        private void ApplyCoupling(ArcInstance prev, ArcTransition trans, IDramaView view, IDramaMutator mutator)
        {
            long arcId = prev.Id.Value;
            if (trans.Resolution == ArcResolution.Advanced)
            {
                switch (trans.Next.Stage)
                {
                    case ArcStage.BuildUp:
                        // 存复仇者原 Goal（仅首次进 BuildUp）+ 覆写 Advance。
                        if (!_originalGoals.ContainsKey(arcId))
                            _originalGoals[arcId] = view.GoalOf(prev.Avenger);
                        mutator.OverrideGoal(prev.Avenger, GoalKind.Advance);
                        break;
                    case ArcStage.Hunting:
                        // 镜像上限内负 Relations（复仇者→仇人）→ 触发 RuleBrain notFoe。
                        mutator.MirrorRelation(prev.Avenger, prev.Target, -_limits.RelationMirrorCap);
                        break;
                }
            }
            else if (trans.Resolution == ArcResolution.Completed || trans.Resolution == ArcResolution.Abandoned)
            {
                // 收束还原原 Goal（防永久卡复仇态）。无存档则不动（弧未进过 BuildUp）。
                if (_originalGoals.TryGetValue(arcId, out var orig))
                {
                    mutator.RestoreGoal(prev.Avenger, orig);
                    _originalGoals.Remove(arcId);
                }
            }
        }

        // —— B/C 节流点火相 ——
        private void IgnitionPhase(long clock, IDramaView view, IDramaMutator mutator, IRandom rng)
        {
            if (clock < _nextIgnitionCheckAt) return;          // B 节流
            _nextIgnitionCheckAt = clock + _limits.IgnitionCheckInterval;
            if (_activeArcs.Count >= _limits.MaxConcurrentArcs) return; // 并发上限（0→永不点火）

            // C 候选收集（只扫强恩怨）+ 加权抽取。
            var cands = IgnitionScanner.FindIgnitions(
                _ledger, _limits.GrudgeIgniteThreshold, view,
                HasActiveArc, (h, t) => OnPairCooldown(h, t, clock));
            if (cands.Count == 0) return; // 空候选 → no-op（不消费 rng）

            var weights = new List<int>(cands.Count);
            for (int i = 0; i < cands.Count; i++) weights.Add(cands[i].Weight);
            int picked = WeightedPicker.PickIndex(weights, rng); // 点火串行消费主流
            var g = cands[picked].Grudge;

            var arc = new ArcInstance(new ArcId(_nextArcId++), ArcKind.Revenge,
                g.Holder, g.Target, ArcStage.Victimized, 0, 0, false);
            _activeArcs.Add(arc);
            SetPairCooldown(g.Holder, g.Target, clock + _limits.ArcPairCooldown);
            mutator.Emit(new ArcIgnited(clock, arc.Id, g.Holder, g.Target));
            _scheduler.Push(arc.Id, clock + _limits.FirstStageDelay);
        }

        private int StageDelay(ArcStage stage) => stage switch
        {
            ArcStage.Victimized => _limits.FirstStageDelay,
            ArcStage.BuildUp => _limits.BuildUpDelay,
            ArcStage.Hunting => _limits.HuntingDelay,
            ArcStage.Showdown => _limits.ShowdownDelay,
            _ => _limits.FirstStageDelay, // 终态不重排，兜底值不会被用
        };

        private int IndexOfArc(ArcId id)
        {
            for (int i = 0; i < _activeArcs.Count; i++)
                if (_activeArcs[i].Id.Value == id.Value) return i;
            return -1;
        }

        private bool HasActiveArc(CharacterId holder)
        {
            for (int i = 0; i < _activeArcs.Count; i++)
                if (_activeArcs[i].Avenger.Value == holder.Value) return true;
            return false;
        }

        private bool OnPairCooldown(CharacterId h, CharacterId t, long clock)
            => _pairCooldownUntil.TryGetValue((h.Value, t.Value), out long until) && clock < until;

        private void SetPairCooldown(CharacterId h, CharacterId t, long until)
            => _pairCooldownUntil[(h.Value, t.Value)] = until;

        /// <summary>深拷全可变态（R-NF2 续跑，drama-010 World.Clone）。ledger 由调用方传克隆实例。</summary>
        public DramaDirector Clone(GrudgeLedger clonedLedger) => new DramaDirector(clonedLedger, _limits, this);

        // —— drama-012 跨代继承（spec §3.3）——

        /// <summary>注册师承/血缘侧表（Self→Master?/Bloodline?），供寿尽继承找继承人。进 Clone。</summary>
        public void RegisterProfile(DramaProfile profile) => _profiles[profile.Self.Value] = profile;

        /// <summary>查角色的师承/血缘 profile（无→null）。</summary>
        public DramaProfile? ProfileOf(CharacterId who)
            => _profiles.TryGetValue(who.Value, out var p) ? p : null;

        /// <summary>
        /// 复仇者寿尽继承钩（World.RemoveDead 调）。死者每条未了强恩怨（Intensity≥阈值且 Generation&lt;MaxGeneration）
        /// → 取首位在世继承人（livingHeirsSorted 已按 年龄→武力→Id 由 World 排序）→ Form 衰减恩怨(Cause=Inherited,
        /// gen+1) + Emit GrudgeInherited。绝嗣/衰减殆尽/达封顶 → 不继承。off 不可达（仅 drama-on World 调）。
        /// </summary>
        public void OnDeath(CharacterId deceased, IReadOnlyList<CharacterId> livingHeirsSorted, long clock, IDramaMutator mutator)
        {
            if (livingHeirsSorted.Count == 0) return; // 绝嗣绝门：恩怨随死者消散

            // 快照死者恩怨（ByHolder 确定性序）；遍历中会向 ledger 写新条目，先拷出。
            var held = ledgerSnapshot(deceased);
            for (int i = 0; i < held.Count; i++)
            {
                var g = held[i];
                if (g.Intensity < _limits.GrudgeIgniteThreshold) continue;     // 仅强恩怨继承
                if (g.Generation >= _limits.MaxGeneration) continue;           // 封顶防无限链
                int decayed = g.Intensity * _limits.InheritDecayPct / 100;     // 整数衰减（单调不增）
                if (decayed < 1) continue;                                      // 衰减殆尽不继承

                var heir = livingHeirsSorted[0];                               // 首位继承人（World 已排序）
                var newId = _ledger.Form(heir, g.Target, g.Kind, decayed, clock,
                    GrudgeCause.Inherited, g.Generation + 1, g.Id, _limits.GrudgeCap);
                mutator.Emit(new GrudgeInherited(clock, newId, heir, g.Target, g.Id, g.Generation + 1, decayed));
            }
        }

        private IReadOnlyList<Grudge> ledgerSnapshot(CharacterId holder)
        {
            var src = _ledger.ByHolder(holder);
            return new List<Grudge>(src); // 拷出，避免遍历中写 ledger 影响迭代
        }
    }
}
