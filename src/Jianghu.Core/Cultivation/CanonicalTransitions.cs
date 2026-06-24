using System.Collections.Generic;

namespace Jianghu.Cultivation
{
    /// <summary>
    /// 标准转职路线数据（story-003）。≥5 条 canon 跃迁。
    /// 加跃迁=加数据行。
    /// </summary>
    public static class CanonicalTransitions
    {
        public static readonly IReadOnlyList<TransitionDef> All = new TransitionDef[]
        {
            // 1. 剑修→剑仙 (Transmute)
            new("canon_sword_to_sage", TransitionKind.Transmute,
                "sword_immortal", "sword_sage_ascended",
                new TransitionGate(6, null, "HasSwordHeart"),
                new CarryoverRule(new[] { "qi", "sword_intent" }, new[] { "sword_art_01" }, 0),
                Cost: 500),

            // 2. 武夫→修真 (Transmute)
            new("canon_martial_to_cultivator", TransitionKind.Transmute,
                "ti_xiu_hengshi", "sword_immortal",
                new TransitionGate(4, null, "Enlightened"),
                new CarryoverRule(new[] { "qi" }, System.Array.Empty<string>(), -1),
                Cost: 300),

            // 3. 散修→入门 (Transmute)
            new("canon_drifter_to_entry", TransitionKind.Transmute,
                "drifter", "fa_xiu",
                new TransitionGate(0, null, "HasRootTag"),
                new CarryoverRule(System.Array.Empty<string>(), System.Array.Empty<string>(), 0),
                Cost: 100),

            // 4. 鬼修→阳神 (Transmute)
            new("canon_ghost_to_yang", TransitionKind.Transmute,
                "gui_xiu_yang_hun", "gui_xiu_yang_shen",
                new TransitionGate(8, null, "YangSoul"),
                new CarryoverRule(new[] { "yin_qi", "yang_qi" }, new[] { "ghost_art_01" }, 1),
                Cost: 800),

            // 5. 魔修→天魔 (Transmute)
            new("canon_demon_to_heavenly", TransitionKind.Transmute,
                "mo_xiu_xinmo", "mo_xiu_tianmo",
                new TransitionGate(8, null, "DemonHeart"),
                new CarryoverRule(new[] { "demon_qi" }, System.Array.Empty<string>(), 1),
                Cost: 1000),
        };

        /// <summary>标准 RiskModifier 数据（story-011）。≥5 条反噬模板。</summary>
        public static readonly IReadOnlyList<RiskModifier> StandardRiskModifiers = new RiskModifier[]
        {
            new("rm_transition_backlash", RiskTrigger.Transition, 100,
                RiskPenaltyKind.InnerDemonGain, 10, 50, "innerDemon<30"),
            new("rm_dual_overload", RiskTrigger.Breakthrough, 50,
                RiskPenaltyKind.ProgressLoss, 20, 100, null),
            new("rm_dark_path", RiskTrigger.Cast, 80,
                RiskPenaltyKind.InnerDemonGain, 15, 50, "daoHeart<20"),
            new("rm_heavenly_trial", RiskTrigger.Breakthrough, 30,
                RiskPenaltyKind.StatLoss, 5, 200, "innerDemon>=70"),
            new("rm_karma_debt", RiskTrigger.Kill, 200,
                RiskPenaltyKind.InnerDemonGain, 25, 30, null),
        };
    }
}
