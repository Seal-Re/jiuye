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
        private long _nextIgnitionCheckAt;
        private long _nextArcId;

        public DramaDirector(GrudgeLedger ledger, LimitsConfig limits)
        {
            _ledger = ledger;
            _limits = limits;
            _scheduler = new DramaScheduler();
            _activeArcs = new List<ArcInstance>();
            _pairCooldownUntil = new Dictionary<(long, long), long>();
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
    }
}
