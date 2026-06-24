using System;
using System.Collections.Generic;
using Jianghu.Events;
using Jianghu.Model;
using Jianghu.Random;
using Jianghu.Stats;

namespace Jianghu.Cultivation
{
    /// <summary>
    /// A.2 修炼 tick 集成处理器——DailyMode→Phase FSM 桥接（story-019）。
    /// 纯整数、确定性、off 模式 skip。
    /// 不修改 World.Tick 核心流程，仅作为 cultivation-on 分支插件。
    /// </summary>
    public static class CultivationTickA2
    {
        /// <summary>DailyMode flag 键名。</summary>
        public const string FLAG_DAILY_MODE = "dailyMode";

        /// <summary>VarietyTracker window 键名。</summary>
        public const string FLAG_VARIETY_WINDOW = "varietyWindow";

        /// <summary>
        /// 执行一个 A.2 修炼 tick：选择 DailyMode → 结算效果 → 接入 Phase FSM。
        /// 调用方（World.Tick）在 cultivation-on 分支调用此方法替代 A.0 简单累加。
        /// </summary>
        /// <param name="st">角色修炼状态</param>
        /// <param name="character">角色对象</param>
        /// <param name="pathDef">路线定义</param>
        /// <param name="rng">修炼 RNG（_cultRng 子流）</param>
        /// <param name="tick">当前 tick 号（用于 Chronicle 事件）</param>
        /// <param name="chronicle">编年史（用于写事件）</param>
        /// <returns>A.2 tick 结算结果</returns>
        public static A2TickResult Tick(
            CultivationState st, Character character, CultivationPathDef pathDef,
            IRandom rng, long tick, Action<DomainEvent> chronicle)
        {
            // —— 1. 选择 DailyMode（贪心 + 迟滞） ——
            int insight = character.Stats.Get(Stats.StatKind.Insight);
            bool inDanger = st.Flags.TryGetValue("inDanger", out var d) && d != 0;
            DailyMode? prevMode = st.Flags.TryGetValue(FLAG_DAILY_MODE, out var pm)
                ? (DailyMode?)pm : null;

            var (mode, newInDanger) = DailyModeSelector.Select(st, insight, inDanger, prevMode, rng);

            // —— 2. 结算 DailyMode 效果 ——
            const int BaseProgress = 4; // per-tick base progress (A.0 CultivationGainPerAction=1, A.2 scaled)
            var result = DailyModeApplier.Apply(mode, BaseProgress, insight, rng);

            // —— 3. 应用道心/心魔变化 ——
            int daoHeartBefore = st.DaoHeart;
            int innerDemonBefore = st.InnerDemon;

            if (result.DaoHeartDelta != 0)
            {
                st.GainDaoHeart(result.DaoHeartDelta);
                chronicle(new DaoHeartChanged(tick, character.Id, daoHeartBefore, st.DaoHeart, mode.ToString()));
            }
            if (result.InnerDemonDelta != 0)
            {
                st.GainInnerDemon(result.InnerDemonDelta);
                chronicle(new InnerDemonChanged(tick, character.Id, innerDemonBefore, st.InnerDemon, mode.ToString()));
            }

            // —— 4. 应用 progress 变化 ——
            int progressBefore = st.CultivationPoints;
            if (result.ProgressDelta != 0)
            {
                st.CultivationPoints = Math.Max(0, st.CultivationPoints + result.ProgressDelta);
            }

            // Epiphany: breakProgress boost handled by phase machine (via Flags)
            if (result.EpiphanyTriggered)
            {
                int bp = st.Flags.TryGetValue("breakProgress", out var b) ? b : 0;
                st.Flags["breakProgress"] = bp + 25;
            }

            // Foundation bonus
            if (result.FoundationBonus)
            {
                int fdn = st.Flags.TryGetValue("foundation", out var f) ? f : 0;
                st.Flags["foundation"] = fdn + 1;
            }

            // —— 5. 更新状态 flags ——
            st.Flags[FLAG_DAILY_MODE] = (int)mode;
            st.Flags["inDanger"] = newInDanger ? 1 : 0;

            // —— 6. Phase 前置条件检查（接入 CultivationPhase） ——
            // Phase 转移由现有的 CultivationPhase 状态机处理（在 World.Tick 中），
            // 我们只更新必要的输入 flags。
            // innerDemon 阈值检查由 Phase 状态机内部处理。

            return new A2TickResult(
                mode, result.ProgressDelta, result.DaoHeartDelta, result.InnerDemonDelta,
                progressBefore, st.CultivationPoints,
                result.EpiphanyTriggered, result.ShouldMove, result.FoundationBonus,
                newInDanger);
        }

        /// <summary>
        /// Off 模式：不执行 A.2 tick，走 A.0 简单累积（CultivationPoints + 1）。
        /// </summary>
        public static bool IsActive(CultivationState? st)
            => st != null && st.Flags.TryGetValue(FLAG_DAILY_MODE, out _);
    }

    /// <summary>
    /// A.2 tick 结算结果（纯数据，不可变）。
    /// </summary>
    public readonly struct A2TickResult
    {
        public readonly DailyMode Mode;
        public readonly int ProgressDelta;
        public readonly int DaoHeartDelta;
        public readonly int InnerDemonDelta;
        public readonly int ProgressBefore;
        public readonly int ProgressAfter;
        public readonly bool EpiphanyTriggered;
        public readonly bool ShouldMove;
        public readonly bool FoundationBonus;
        public readonly bool InDanger;

        public A2TickResult(DailyMode mode, int progressDelta, int daoHeartDelta,
            int innerDemonDelta, int progressBefore, int progressAfter,
            bool epiphanyTriggered, bool shouldMove, bool foundationBonus,
            bool inDanger)
        {
            Mode = mode;
            ProgressDelta = progressDelta;
            DaoHeartDelta = daoHeartDelta;
            InnerDemonDelta = innerDemonDelta;
            ProgressBefore = progressBefore;
            ProgressAfter = progressAfter;
            EpiphanyTriggered = epiphanyTriggered;
            ShouldMove = shouldMove;
            FoundationBonus = foundationBonus;
            InDanger = inDanger;
        }
    }
}
