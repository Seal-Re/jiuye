using System;
using Jianghu.Random;

namespace Jianghu.Cultivation
{
    /// <summary>
    /// BreakAid 执行服务（story-020：BreakAid→Breakthrough 集成）。
    /// 在 Bottleneck 阶段选择并应用破障方法，加速突破。
    /// </summary>
    public static class BreakAidService
    {
        /// <summary>
        /// 为处于瓶颈期的角色选择最优 BreakAid 方法并计算收益。
        /// </summary>
        /// <param name="st">角色修炼状态</param>
        /// <param name="insight">角色 Insight 值</param>
        /// <param name="hasGuardian">同地点是否有高 realm 守护者</param>
        /// <param name="rng">确定性 RNG（Epiphany 判定用）</param>
        /// <returns>选中的方法 + 实际突破进度增益</returns>
        public static (BreakAidMethod Method, int ProgressGain) ApplyBest(
            CultivationState st, int insight, bool hasGuardian, IRandom rng)
        {
            int phase = st.Flags.TryGetValue("cultPhase", out var p) ? p : -1;
            if (phase != 5) return (BreakAidMethod.Epiphany, 0); // 5=Bottleneck, only apply in bottleneck

            int streak = st.Flags.TryGetValue("seclusionStreak", out var sk) ? sk : 0;

            // Filter available methods
            int bestScore = int.MinValue;
            BreakAidMethod best = BreakAidMethod.Epiphany;

            foreach (BreakAidMethod method in Enum.GetValues(typeof(BreakAidMethod)))
            {
                var def = BreakAidRegistry.Get(method);
                int score = ScoreMethod(def, st, insight, hasGuardian, streak, rng);
                if (score > bestScore) { bestScore = score; best = method; }
            }

            int gain = ComputeGain(best, streak, insight, rng);
            return (best, gain);
        }

        private static int ScoreMethod(BreakAidDef def, CultivationState st,
            int insight, bool hasGuardian, int streak, IRandom rng)
        {
            int score = def.BreakProgressBonus * 2;

            // InnerDemon risk penalty
            score -= def.InnerDemonRisk * 3;

            // DaoHeart requirement check
            if (st.DaoHeart < def.DaoHeartReq) score -= 50;

            // Resource check
            if (def.ResourceCost != null)
            {
                foreach (var (key, amount) in def.ResourceCost)
                    if (st.Resources.TryGetValue(key, out var v) && v >= amount)
                        score += 5; // Can afford
                    else
                        score -= 50; // Cannot afford
            }

            // Guardian check
            if (def.Method == BreakAidMethod.Guardian && !hasGuardian)
                score -= 100;

            // Seclusion lockout
            if (def.Method == BreakAidMethod.Seclusion && SeclusionFormulas.IsLockedOut(streak))
                score -= 100;

            return score;
        }

        private static int ComputeGain(BreakAidMethod method, int streak,
            int insight, IRandom rng)
        {
            switch (method)
            {
                case BreakAidMethod.Seclusion:
                    return SeclusionFormulas.ProgressGain(1, streak);

                case BreakAidMethod.Epiphany:
                    // Epiphany: roll vs Insight threshold
                    int threshold = DailyModeApplier.EpiphanyThreshold(insight);
                    bool triggered = threshold > 0 && rng.NextInt(20) + 1 < threshold;
                    return triggered ? BreakAidRegistry.Epiphany.BreakProgressBonus : 0;

                case BreakAidMethod.Resource:
                    return BreakAidRegistry.Resource.BreakProgressBonus;

                case BreakAidMethod.Guardian:
                    return BreakAidRegistry.Guardian.BreakProgressBonus;

                default:
                    return 0;
            }
        }
    }
}
