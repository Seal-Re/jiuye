using System;
using Jianghu.Random;

namespace Jianghu.Cultivation
{
    /// <summary>
    /// A.3 feature services: awaken power unlock (006), dual power formula (009),
    /// good/evil fork (012/013). Pure integer, deterministic.
    /// </summary>

    // ================================================================
    // Story-006: Awaken → Power Unlock
    // ================================================================
    public static class AwakenPowerService
    {
        /// <summary>觉醒后 PE 加成（不进 EffectivePower 计算，仅附加）。</summary>
        /// <param name="basePE">基础 PE（PowerEngine.Evaluate 输出）</param>
        /// <param name="powerBonus">AwakeningDef.PowerBonus（整数）</param>
        /// <returns>觉醒后总 PE = basePE + powerBonus</returns>
        public static int ApplyAwakenBonus(int basePE, int powerBonus)
            => basePE + powerBonus;

        /// <summary>觉醒后解锁额外 ArtCategory 类型标记。</summary>
        public const string AWAKENED_FLAG = "awakened";
        public const string BLOODLINE_CATEGORY = "bloodline";

        /// <summary>标记已觉醒并记录解锁的功法。</summary>
        public static void ApplyUnlock(CultivationState st, AwakeningDef def)
        {
            st.Flags[AWAKENED_FLAG] = 1;
            if (def.UnlockArts.Count > 0)
                st.Flags[$"awaken_unlock_{def.Id}"] = 1;
        }
    }

    // ================================================================
    // Story-009: Dual Power Formula + Backlash
    // ================================================================
    public static class DualPowerService
    {
        /// <summary>双修战力：mainPE + bonus。bonus = min(secondPE * bandwidth / 100, mainPE / 2)。</summary>
        public static int ComputeDualPE(int mainPE, int secondPE, int bandwidth)
        {
            int bonus = secondPE * bandwidth / 100;
            int cap = mainPE / 2; // max 50% of main PE
            if (bonus > cap) bonus = cap;
            return mainPE + bonus;
        }

        /// <summary>双修反噬检查：bandwidth 负载 > 80% → 概率反噬（permille）。</summary>
        public static (bool Triggered, int DemonGain) CheckBacklash(
            int bandwidth, int bandwidthUsed, IRandom rng)
        {
            if (bandwidth <= 0) return (false, 0);
            int loadPct = bandwidthUsed * 100 / bandwidth;
            if (loadPct <= 80) return (false, 0);

            int riskPermille = (loadPct - 80) * 10;
            bool trig = rng.NextInt(1000) < riskPermille;
            return (trig, trig ? 5 : 0);
        }
    }

    // ================================================================
    // Story-012/013: Good/Evil Fork
    // ================================================================
    public static class MoralForkService
    {
        public const string TAG_RIGHTEOUS = "righteous";
        public const string TAG_EVIL = "evil";

        /// <summary>daoHeart ≥ 80 → 善道标签</summary>
        public const int RIGHTEOUS_THRESHOLD = 80;

        /// <summary>innerDemon ≥ 70 → 邪道标签</summary>
        public const int EVIL_THRESHOLD = 70;

        /// <summary>正邪标签判定——纯静态，无 RNG。</summary>
        public static string? MoralTag(CultivationState st)
        {
            if (st.DaoHeart >= RIGHTEOUS_THRESHOLD) return TAG_RIGHTEOUS;
            if (st.InnerDemon >= EVIL_THRESHOLD) return TAG_EVIL;
            return null; // neutral
        }

        /// <summary>善道天劫减益（威胁值 × 80%）。</summary>
        public static int RighteousTribulationModifier(int baseThreat)
            => baseThreat * 4 / 5;

        /// <summary>邪道天劫增益（威胁值 × 150%）。</summary>
        public static int EvilTribulationModifier(int baseThreat)
            => baseThreat * 3 / 2;

        /// <summary>善道突破 daHeart bonus。</summary>
        public static int RighteousBreakthroughBonus() => 3;

        /// <summary>获取天劫修正——基于正邪标签。</summary>
        public static int GetTribulationModifier(CultivationState st, int baseThreat)
        {
            var tag = MoralTag(st);
            if (tag == TAG_RIGHTEOUS) return RighteousTribulationModifier(baseThreat);
            if (tag == TAG_EVIL) return EvilTribulationModifier(baseThreat);
            return baseThreat; // neutral
        }
    }
}
