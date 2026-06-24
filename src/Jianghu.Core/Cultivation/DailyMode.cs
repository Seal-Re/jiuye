using System;

namespace Jianghu.Cultivation
{
    /// <summary>
    /// A.2 日课微决策 4 路模式（A3-FINAL §1）。
    /// 纯整数倍率，禁浮点（红线 B.2）。
    /// </summary>
    public enum DailyMode
    {
        /// <summary>勇进：progress ×6/4, innerDemon+2</summary>
        Fast = 0,
        /// <summary>固本：progress ×3/4, innerDemon-1, Foundation+1</summary>
        Steady = 1,
        /// <summary>悟道：progress ×1/2, EpiphanyRoll→breakProgress+25 or daoHeart+5</summary>
        Comprehend = 2,
        /// <summary>游历：progress ×1/4, innerDemon-2, Move, encounter exposure ×3</summary>
        Roam = 3,
    }

    /// <summary>
    /// DailyMode.Apply 结果——纯整数，确定性（同 seed → 同结果）。
    /// </summary>
    public readonly struct DailyModeResult
    {
        /// <summary>净 progress 变化（整数，可正可负）。</summary>
        public readonly int ProgressDelta;

        /// <summary>道心变化（已钳 [0,100]，见 CultivationState.GainDaoHeart）。</summary>
        public readonly int DaoHeartDelta;

        /// <summary>心魔变化（已钳 [0,100]，见 CultivationState.GainInnerDemon）。</summary>
        public readonly int InnerDemonDelta;

        /// <summary>是否触发了顿悟（仅在 Comprehend 模式可能为 true）。</summary>
        public readonly bool EpiphanyTriggered;

        /// <summary>是否应执行 Move 动作（仅在 Roam 模式为 true）。</summary>
        public readonly bool ShouldMove;

        /// <summary>遭遇曝光乘子（Roam=3, 其他=0 或 1）。</summary>
        public readonly int EncounterExposure;

        /// <summary>基础进度增量（Foundation+1 由调用方处理）。</summary>
        public readonly bool FoundationBonus;

        public DailyModeResult(int progressDelta, int daoHeartDelta, int innerDemonDelta,
            bool epiphanyTriggered, bool shouldMove, int encounterExposure, bool foundationBonus)
        {
            ProgressDelta = progressDelta;
            DaoHeartDelta = daoHeartDelta;
            InnerDemonDelta = innerDemonDelta;
            EpiphanyTriggered = epiphanyTriggered;
            ShouldMove = shouldMove;
            EncounterExposure = encounterExposure;
            FoundationBonus = foundationBonus;
        }
    }

    /// <summary>
    /// DailyMode 应用器——4 路日课微决策的纯整数效果结算。
    /// RNG 走传入的 <c>IRandom</c>（通常为 _cultRng），确定性（红线 B.2）。
    /// </summary>
    public static class DailyModeApplier
    {
        /// <summary>A3-FINAL §1.1：顿悟阈值 = Insight - 18。Insight<18 → 概率=0。</summary>
        public static int EpiphanyThreshold(int insight)
            => Math.Max(0, insight - 18);

        /// <summary>
        /// 执行指定 DailyMode，返回结构化结果。不修改 CultivationState（调用方负责应用）。
        /// </summary>
        /// <param name="mode">所选日课模式</param>
        /// <param name="baseProgress">基础 progress（当前行动积累值）</param>
        /// <param name="insight">角色的 Insight 值（用于 EpiphanyRoll）</param>
        /// <param name="rng">确定性 PRNG（_cultRng 子流）</param>
        public static DailyModeResult Apply(DailyMode mode, int baseProgress, int insight, Jianghu.Random.IRandom rng)
        {
            switch (mode)
            {
                case DailyMode.Fast:
                    return new DailyModeResult(
                        progressDelta: baseProgress * 6 / 4,
                        daoHeartDelta: 0,
                        innerDemonDelta: +2,
                        epiphanyTriggered: false,
                        shouldMove: false,
                        encounterExposure: 0,
                        foundationBonus: false);

                case DailyMode.Steady:
                    return new DailyModeResult(
                        progressDelta: baseProgress * 3 / 4,
                        daoHeartDelta: 0,
                        innerDemonDelta: -1,
                        epiphanyTriggered: false,
                        shouldMove: false,
                        encounterExposure: 0,
                        foundationBonus: true);

                case DailyMode.Comprehend:
                    {
                        var epiphany = TryEpiphany(insight, rng, out int daoHeartGain);
                        return new DailyModeResult(
                            progressDelta: baseProgress / 2,
                            daoHeartDelta: daoHeartGain,
                            innerDemonDelta: 0,
                            epiphanyTriggered: epiphany,
                            shouldMove: false,
                            encounterExposure: 0,
                            foundationBonus: false);
                    }

                case DailyMode.Roam:
                    return new DailyModeResult(
                        progressDelta: baseProgress / 4,
                        daoHeartDelta: 0,
                        innerDemonDelta: -2,
                        epiphanyTriggered: false,
                        shouldMove: true,
                        encounterExposure: 3,
                        foundationBonus: false);

                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown DailyMode");
            }
        }

        /// <summary>
        /// 顿悟判定：Roll(1,20) < Insight - 18 → 触发顿悟。
        /// 顿悟成功时：50% breakProgress+25，50% daoHeart+5。
        /// Insight < 18 → 概率恒为 0。
        /// </summary>
        private static bool TryEpiphany(int insight, Jianghu.Random.IRandom rng, out int daoHeartGain)
        {
            daoHeartGain = 0;
            int threshold = EpiphanyThreshold(insight);
            if (threshold <= 0) return false;

            int roll = rng.NextInt(20) + 1; // 1..20
            if (roll >= threshold) return false; // roll >= threshold → fail

            // Epiphany triggered: 50% daoHeart+5, 50% (breakProgress+25 handled by caller)
            bool isDaoHeart = rng.NextInt(2) == 0; // fair coin flip
            if (isDaoHeart)
                daoHeartGain = 5;

            return true;
        }
    }
}
