using System;
using System.Collections.Generic;
using Jianghu.Random;

namespace Jianghu.Cultivation
{
    /// <summary>
    /// 觉醒触发服务（story-005）。挂 A.2 奇遇框架，检测触发条件。
    /// </summary>
    public static class AwakenTriggerService
    {
        /// <summary>濒死触发——HP < 10% → roll 觉醒检测。</summary>
        public const int NEAR_DEATH_HP_PCT = 10;
        public const int NEAR_DEATH_BASE_RATE = 50; // 5% base rate in permille

        /// <summary>
        /// 检查濒死觉醒触发。
        /// </summary>
        /// <param name="hpCurrent">当前 HP</param>
        /// <param name="hpMax">最大 HP</param>
        /// <param name="awakening">觉醒定义</param>
        /// <param name="rng">确定性 RNG</param>
        /// <returns>是否触发觉醒</returns>
        public static bool CheckNearDeath(int hpCurrent, int hpMax, AwakeningDef awakening, IRandom rng)
        {
            if (awakening.Trigger != AwakenTrigger.NearDeath) return false;
            if (hpMax <= 0) return false;
            int pct = hpCurrent * 100 / hpMax;
            if (pct >= NEAR_DEATH_HP_PCT) return false;
            return rng.NextInt(1000) < NEAR_DEATH_BASE_RATE;
        }

        /// <summary>
        /// 秘境触发——特定 node/tag → 觉醒概率 boost。
        /// </summary>
        public static bool CheckSecretRealm(AwakeningDef awakening, IReadOnlyList<string> nodeTags, IRandom rng)
        {
            if (awakening.Trigger != AwakenTrigger.SecretRealm) return false;
            // Secret realm boost: base 100 permille (10%), +50 per matching tag
            int rate = 100;
            foreach (var tag in nodeTags)
                if (tag.Contains("secret") || tag.Contains("realm")) rate += 50;
            return rng.NextInt(1000) < rate;
        }

        /// <summary>
        /// 血统法器触发——必定觉醒。
        /// </summary>
        public static bool CheckBloodlineArtifact(AwakeningDef awakening, bool hasArtifact)
        {
            return awakening.Trigger == AwakenTrigger.BloodlineArtifact && hasArtifact;
        }

        /// <summary>
        /// 境界关触发——达到指定境界 → roll 觉醒检测。
        /// </summary>
        public static bool CheckRealmGate(AwakeningDef awakening, int realmIndex, IRandom rng)
        {
            if (awakening.Trigger != AwakenTrigger.RealmGate) return false;
            // Realm gate: 200 permille (20%)
            return rng.NextInt(1000) < 200;
        }
    }
}
