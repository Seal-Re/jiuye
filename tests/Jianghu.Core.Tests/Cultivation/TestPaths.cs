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
    }
}
