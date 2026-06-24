using System;
using Jianghu.Random;

namespace Jianghu.Cultivation
{
    /// <summary>
    /// DailyMode 贪心选择器 + 迟滞规则（A3-FINAL §1.2）。
    /// NPC 根据当前状态贪心选择最优日课模式，迟滞防抖动。
    /// 确定性（同状态+同 RNG → 同选择），纯整数。
    /// </summary>
    public static class DailyModeSelector
    {
        /// <summary>心魔进入危险阈值（A3 §1.2）。</summary>
        public const int DEMON_DANGER_ENTER = 65;

        /// <summary>心魔退出危险阈值（迟滞带下界）。</summary>
        public const int DEMON_DANGER_EXIT = 50;

        /// <summary>迟滞带宽 = ENTER - EXIT = 15。</summary>
        public const int HYSTERESIS_BAND = DEMON_DANGER_ENTER - DEMON_DANGER_EXIT;

        /// <summary>贪心评分权重——progress 收益权重（基准）。</summary>
        private const int PROGRESS_WEIGHT = 10;

        /// <summary>贪心评分权重——innerDemon 惩罚权重（常态）。</summary>
        private const int DEMON_WEIGHT_NORMAL = 8;

        /// <summary>贪心评分权重——innerDemon 惩罚权重（危险态 ×3）。</summary>
        private const int DEMON_WEIGHT_DANGER = DEMON_WEIGHT_NORMAL * 3;

        /// <summary>贪心评分权重——daoHeart 收益权重。</summary>
        private const int DAOHEART_WEIGHT = 6;

        /// <summary>顿悟额外加权（Comprehend 模式独有）。</summary>
        private const int EPIPHANY_BONUS = 15;

        /// <summary>连续同模式惩罚分（防单一化）。</summary>
        private const int VARIETY_PENALTY = -5;

        // CultivationPhase enum int values (from CultivationPhase.cs)
        private const int PHASE_BREAKTHROUGH = 6;
        private const int PHASE_DEVIATION = 8;
        private const int PHASE_FALLEN = 9;

        /// <summary>
        /// 选择最优 DailyMode。纯整数贪心评分，确定性。
        /// </summary>
        /// <param name="st">角色修炼状态</param>
        /// <param name="insight">角色 Insight 值</param>
        /// <param name="inDanger">当前是否处于危险态（迟滞记忆）</param>
        /// <param name="prevMode">上一 tick 的模式（null=首次）</param>
        /// <param name="rng">确定性 RNG（仅用于 tiebreak）</param>
        /// <returns>所选 DailyMode + 新的 inDanger 状态</returns>
        public static (DailyMode Mode, bool InDanger) Select(
            CultivationState st, int insight, bool inDanger,
            DailyMode? prevMode, IRandom rng)
        {
            // —— Phase 强制锁 ——
            int phase = st.Flags.TryGetValue("cultPhase", out var pVal) ? pVal : -1;

            if (phase == PHASE_FALLEN) return (DailyMode.Roam, inDanger); // 角色已废

            int innerDemon = st.InnerDemon;
            int daoHeart = st.DaoHeart;

            // —— 迟滞规则（hysteresis）——
            bool isDanger = inDanger
                ? innerDemon > DEMON_DANGER_EXIT   // 在危险态→需降到 EXIT 以下才退出
                : innerDemon >= DEMON_DANGER_ENTER; // 不在危险态→达到 ENTER 才进入

            // Demon weight scales with current innerDemon: base 2 + scaled 6 = [2..8]
            int demonWeight = isDanger
                ? DEMON_WEIGHT_DANGER
                : 2 + DEMON_WEIGHT_NORMAL * innerDemon / 133;

            // Breakthrough 锁 Fast
            if (phase == PHASE_BREAKTHROUGH) return (DailyMode.Fast, isDanger);

            // Deviation 强制 Steady 或 Roam
            if (phase == PHASE_DEVIATION)
            {
                // 贪心选 Steady vs Roam（偏 Roam 因 innerDemon-2）
                int steadyScore = ScoreMode(DailyMode.Steady, insight, demonWeight, daoHeart, prevMode);
                int roamScore = ScoreMode(DailyMode.Roam, insight, demonWeight, daoHeart, prevMode);
                return (roamScore >= steadyScore ? DailyMode.Roam : DailyMode.Steady, isDanger);
            }

            // —— 正常贪心：对所有合法模式评分 ——
            var scores = new (DailyMode Mode, int Score)[]
            {
                (DailyMode.Fast,        ScoreMode(DailyMode.Fast,        insight, demonWeight, daoHeart, prevMode)),
                (DailyMode.Steady,      ScoreMode(DailyMode.Steady,      insight, demonWeight, daoHeart, prevMode)),
                (DailyMode.Comprehend,  ScoreMode(DailyMode.Comprehend,  insight, demonWeight, daoHeart, prevMode)),
                (DailyMode.Roam,        ScoreMode(DailyMode.Roam,        insight, demonWeight, daoHeart, prevMode)),
            };

            // 选最高分（tiebreak: 确定性随机）
            int bestScore = int.MinValue;
            DailyMode bestMode = DailyMode.Fast;
            int tieCount = 0;

            for (int i = 0; i < scores.Length; i++)
            {
                if (scores[i].Score > bestScore)
                {
                    bestScore = scores[i].Score;
                    bestMode = scores[i].Mode;
                    tieCount = 1;
                }
                else if (scores[i].Score == bestScore)
                {
                    tieCount++;
                    // 确定性 tiebreak: rng coin flip among ties
                    if (rng.NextInt(tieCount) == 0)
                        bestMode = scores[i].Mode;
                }
            }

            return (bestMode, isDanger);
        }

        /// <summary>计算单一模式的贪心评分（纯整数）。</summary>
        private static int ScoreMode(DailyMode mode, int insight, int demonWeight,
            int daoHeart, DailyMode? prevMode)
        {
            int progressMul;
            int innerDemonDelta;
            int daoHeartDelta = 0;
            bool isComprehend = false;

            switch (mode)
            {
                case DailyMode.Fast:
                    progressMul = 6; innerDemonDelta = +2; break;
                case DailyMode.Steady:
                    progressMul = 3; innerDemonDelta = -1; break;
                case DailyMode.Comprehend:
                    progressMul = 2; innerDemonDelta = 0; isComprehend = true; break;
                case DailyMode.Roam:
                    progressMul = 1; innerDemonDelta = -2; break;
                default:
                    return int.MinValue;
            }

            // progress * mul / 4 归一化到可比尺度
            int progressScore = progressMul * PROGRESS_WEIGHT / 4;

            // innerDemon 惩罚（负贡献：innerDemonDelta>0 扣分，<0 加分）
            int demonScore = -innerDemonDelta * demonWeight;

            // daoHeart 微小加成
            int dhScore = daoHeartDelta * DAOHEART_WEIGHT / 5;

            // 顿悟额外加权（仅 Comprehend，且 Insight≥20 才有意义）
            int epiphanyScore = 0;
            if (isComprehend && insight >= 20)
            {
                int threshold = DailyModeApplier.EpiphanyThreshold(insight);
                // 阈值越高，加权越重（概率越大）
                epiphanyScore = threshold * EPIPHANY_BONUS / 12; // 归一化
            }

            // 多样性惩罚：连续同模式扣分
            int varietyScore = (prevMode.HasValue && prevMode.Value == mode)
                ? VARIETY_PENALTY : 0;

            return progressScore + demonScore + dhScore + epiphanyScore + varietyScore;
        }
    }
}
