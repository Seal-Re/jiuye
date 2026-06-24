using System;

namespace Jianghu.Cultivation
{
    /// <summary>
    /// 闭关时长+收益公式（A3-FINAL §2.2-2.4, story-010）。
    /// 纯整数，确定性。Duration/AgeCost/ProgressGain 三公式。
    /// </summary>
    public static class SeclusionFormulas
    {
        /// <summary>基础时长常数。</summary>
        public const int BASE_DURATION = 60;

        /// <summary>每 WorkUnit 加成的 tick 数。</summary>
        public const int TICKS_PER_WORK_UNIT = 8;

        /// <summary>InsightSpeedup 上界。</summary>
        public const int MAX_INSIGHT_SPEEDUP = 30;

        /// <summary>基础折寿因子。</summary>
        public const int BASE_FOLD_LIFE = 100;

        /// <summary>每 streak 递增的折寿因子。</summary>
        public const int FOLD_LIFE_PER_STREAK = 20;

        /// <summary>每 tick 的默认行动间隔。</summary>
        public const int DEFAULT_ACTION_INTERVAL = 4;

        /// <summary>
        /// 闭关时长（tick 数）。
        /// Duration = 60 + 8*WorkUnits - min(Insight/2, 30)
        /// </summary>
        public static long Duration(int workUnits, int insight)
        {
            int speedup = Math.Min(insight / 2, MAX_INSIGHT_SPEEDUP);
            return BASE_DURATION + (long)TICKS_PER_WORK_UNIT * workUnits - speedup;
        }

        /// <summary>
        /// 折寿代价（Age delta）。
        /// AgeCost = ActionInterval * ceil(Duration / ActionInterval) * FoldLifeFactor / 100
        /// </summary>
        public static long AgeCost(long duration, int streak, int actionInterval = DEFAULT_ACTION_INTERVAL)
        {
            long intervals = (duration + actionInterval - 1) / actionInterval; // ceil division
            int foldLife = BASE_FOLD_LIFE + streak * FOLD_LIFE_PER_STREAK;
            return intervals * actionInterval * foldLife / 100;
        }

        /// <summary>
        /// 闭关收益——突破进度增量。
        /// ProgressGain = WorkUnits + BreakAid.Seclusion.Bonus(streak)
        /// </summary>
        public static int ProgressGain(int workUnits, int streak)
        {
            return workUnits + BreakAidRegistry.SeclusionBonus(streak);
        }

        /// <summary>
        /// 闭关期间 innerDemon 增量（出关结算时）。
        /// 每 streak +3（无上限）。
        /// </summary>
        public static int InnerDemonGain(int streak) => 3 * (streak + 1);

        /// <summary>
        /// Strike ≥ 3 → 收益折半。Strike = streak（story-011 定义）。
        /// </summary>
        public static bool IsHalved(int streak) => streak >= 3;

        /// <summary>
        /// Strike ≥ 5 → 闭关被拒（story-011 定义）。
        /// </summary>
        public static bool IsLockedOut(int streak) => streak >= 5;
    }
}
