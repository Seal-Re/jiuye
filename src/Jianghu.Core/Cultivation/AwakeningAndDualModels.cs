using System;
using System.Collections.Generic;

namespace Jianghu.Cultivation
{
    /// <summary>觉醒触发事件类型（story-004）。</summary>
    public enum AwakenTrigger { NearDeath, SecretRealm, BloodlineArtifact, RealmGate }

    /// <summary>
    /// 觉醒定义——同路线血统/体质解锁（A123 §A.3.1, story-004）。
    /// </summary>
    public sealed record AwakeningDef(
        string Id,
        string PathId,                       // 所属路线
        string HiddenTag,                    // 隐藏 tag（觉醒前不可见）
        AwakenTrigger Trigger,               // 触发事件类型
        IReadOnlyList<string> UnlockArts,    // 解锁的功法 ID 列表
        int PowerBonus                       // 战力附加（PE 加成，不进 EffectivePower）
    );

    /// <summary>
    /// 觉醒注册表（story-004）。数据驱动。
    /// </summary>
    public sealed class AwakeningRegistry
    {
        private readonly List<AwakeningDef> _all;
        public AwakeningRegistry(IReadOnlyList<AwakeningDef> defs) { _all = new List<AwakeningDef>(defs); }
        public IReadOnlyList<AwakeningDef> All => _all;
        public int Count => _all.Count;
    }

    // ================================================================
    // Story-007: DualPathDef + slotCap
    // ================================================================

    /// <summary>双修路线排斥规则。</summary>
    public sealed record ExcludesRule(
        string PathIdA,
        string PathIdB,
        string Reason                          // 排斥原因（如 "正邪不两立"）
    );

    /// <summary>
    /// 双修兼容性服务（story-007/008）。
    /// </summary>
    public static class DualCompatibility
    {
        /// <summary>双修 slot 上限（硬钳）。</summary>
        public const int SLOT_CAP = 2;

        /// <summary>Bandwidth 公式：base + Insight * 2。</summary>
        public static int Bandwidth(int insight) => 50 + insight * 2;

        /// <summary>标准排斥规则——预定义排斥对。</summary>
        public static readonly IReadOnlyList<ExcludesRule> StandardExcludes = new ExcludesRule[]
        {
            new("lei_xiu", "gui_xiu_yang_hun", "雷鬼不两立"),
            new("sword_immortal", "mo_xiu_xinmo", "剑魔不两立"),
            new("buddhist_golden_body", "mo_xiu_xinmo", "佛魔不两立"),
            new("buddhist_golden_body", "xue_xiu_xuesha", "佛不沾血"),
        };

        /// <summary>检查两条路线是否兼容双修。</summary>
        public static bool CanDualCultivate(string pathA, string pathB)
        {
            foreach (var rule in StandardExcludes)
                if ((rule.PathIdA == pathA && rule.PathIdB == pathB)
                    || (rule.PathIdA == pathB && rule.PathIdB == pathA))
                    return false;
            return true;
        }
    }

    // ================================================================
    // Story-010: RiskModifier
    // ================================================================

    /// <summary>反噬触发条件。</summary>
    public enum RiskTrigger { Breakthrough, Cast, Kill, Transition }

    /// <summary>反噬惩罚类型。</summary>
    public enum RiskPenaltyKind { InnerDemonGain, ProgressLoss, ResourceDrain, StatLoss }

    /// <summary>
    /// 风险修改器——转职/双修/邪道速成反噬（registry-research §5, story-010）。
    /// </summary>
    public sealed record RiskModifier(
        string Id,
        RiskTrigger Trigger,                  // 触发条件
        int ProbabilityPermille,              // 概率（整数千分比，0-1000）
        RiskPenaltyKind PenaltyKind,          // 惩罚类型
        int PenaltyAmount,                    // 惩罚量
        int CooldownTicks,                    // 冷却（tick）
        string? ClearCondition                // 清除条件（可选）
    );

    /// <summary>
    /// RiskModifier 注册表（story-010）。
    /// </summary>
    public sealed class RiskModifierRegistry
    {
        private readonly List<RiskModifier> _all;
        public RiskModifierRegistry(IReadOnlyList<RiskModifier> defs) { _all = new List<RiskModifier>(defs); }
        public IReadOnlyList<RiskModifier> All => _all;
        public int Count => _all.Count;
    }
}
