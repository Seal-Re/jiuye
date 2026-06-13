using System;
using System.Collections.Generic;
using Jianghu.Cultivation;

namespace Jianghu.Core.Tests.Cultivation
{
    /// <summary>
    /// 最小 CultivationPathDef 测试工厂（B2 引擎层用）。仅造引擎求值所需的最薄数据，
    /// 不含完整 21 路数据校验关切（那属 Task 1.8 / Phase 4-5）。
    /// </summary>
    public static class TestPaths
    {
        /// <summary>
        /// 剑修最小路：terms = stat:Force×4 + stat:Insight×3（无 daoHeart，无 ×0）；
        /// 无 Modifier / PostMul；RealmCurve 三段 mul=[10,15,25]，四列等长。
        /// 手算对拍：Force=20×4 + Insight=10×3 = 110；realm0 mul=10 → 110×10/10 = 110。
        /// </summary>
        public static CultivationPathDef SwordMinimal()
        {
            return new CultivationPathDef(
                "sword_immortal", "剑修", "physical",
                new[] { "melee", "sword" },
                new[] { new ResourceDef("swordWill", 0, 100, 0) },
                new PowerFormulaDef(
                    new[]
                    {
                        new PowerTerm("stat:Force", 4, null),
                        new PowerTerm("stat:Insight", 3, null),
                    },
                    Array.Empty<PowerMod>(),
                    null),
                new RealmCurveDef(
                    new[] { 10, 15, 25 },
                    new[] { 0, 1, 2 },
                    new[] { "凝气", "小成", "大成" },
                    new[] { 0, 100, 300 }),
                new[]
                {
                    new ArtCategoryDef("剑法", "attack", 1, 1, new[]
                    {
                        new ArtDef("a1", "三尺霜", 1, "剑法", Array.Empty<EffectOp>()),
                    }),
                },
                new[]
                {
                    new CombatSkillDef("s1", "剑二十三", 3, Array.Empty<EffectOp>(),
                        new Dictionary<string, int>()),
                },
                new EntryGateDef("tag:sword_root"),
                new SelectionRuleDef(1, 3),
                null);
        }

        // —— Task 1.8 数据校验（PathValidator）用：合法完整 mock 路 + 各违规变体 ——

        static ArtDef Art(string id) => new ArtDef(id, id, 1, "cat", Array.Empty<EffectOp>());

        static ArtCategoryDef Cat(string name, string role) =>
            new ArtCategoryDef(name, role, 1, 1, new[]
            {
                Art(name + "_a1"), Art(name + "_a2"), Art(name + "_a3"), Art(name + "_a4"),
            });

        static CombatSkillDef Skill(string id) =>
            new CombatSkillDef(id, id, 1, Array.Empty<EffectOp>(), new Dictionary<string, int>());

        /// <summary>
        /// 合法完整 mock 路（过 PathValidator 全部 gate）：4 类目（含 1 个 daoheart）每类 4 功法、
        /// 5 战技、SituationalTags 非空非 PathId、四列等长 RealmCurve、terms 无 ×0 无 daoHeart、
        /// PathId canon 全名（含下划线）。各违规测试从此基线 with 单点改坏。
        /// </summary>
        public static CultivationPathDef ValidFull()
        {
            return new CultivationPathDef(
                "mock_test_path", "测试路", "physical",
                new[] { "melee", "righteous" },
                new[] { new ResourceDef("qi", 0, 100, 0) },
                new PowerFormulaDef(
                    new[]
                    {
                        new PowerTerm("stat:Force", 4, null),
                        new PowerTerm("res:qi", 3, null),
                    },
                    Array.Empty<PowerMod>(),
                    null),
                new RealmCurveDef(
                    new[] { 10, 15, 25 },
                    new[] { 0, 1, 2 },
                    new[] { "凝气", "小成", "大成" },
                    new[] { 0, 100, 300 }),
                new[]
                {
                    Cat("攻法", "attack"),
                    Cat("身法", "movement"),
                    Cat("心法", "internal"),
                    Cat("道心", "daoheart"),
                },
                new[]
                {
                    Skill("k1"), Skill("k2"), Skill("k3"), Skill("k4"), Skill("k5"),
                },
                new EntryGateDef("tag:mock_root"),
                new SelectionRuleDef(1, 3),
                null);
        }

        /// <summary>从 ValidFull 基线替换 Power.Terms（造 ×0 / daoHeart 等违规项）。</summary>
        public static CultivationPathDef WithTerm(PowerTerm term)
        {
            var b = ValidFull();
            return b with { Power = b.Power with { Terms = new[] { term, new PowerTerm("stat:Force", 4, null) } } };
        }

        /// <summary>从 ValidFull 基线替换 ArtCategories。</summary>
        public static CultivationPathDef WithCategories(IReadOnlyList<ArtCategoryDef> cats)
        {
            var b = ValidFull();
            return b with { ArtCategories = cats };
        }

        /// <summary>从 ValidFull 基线替换 CombatSkills。</summary>
        public static CultivationPathDef WithSkills(IReadOnlyList<CombatSkillDef> skills)
        {
            var b = ValidFull();
            return b with { CombatSkills = skills };
        }

        /// <summary>从 ValidFull 基线替换 SituationalTags。</summary>
        public static CultivationPathDef WithTags(IReadOnlyList<string> tags)
        {
            var b = ValidFull();
            return b with { SituationalTags = tags };
        }

        /// <summary>从 ValidFull 基线替换 PathId。</summary>
        public static CultivationPathDef WithPathId(string pathId)
        {
            var b = ValidFull();
            return b with { PathId = pathId };
        }
    }
}
