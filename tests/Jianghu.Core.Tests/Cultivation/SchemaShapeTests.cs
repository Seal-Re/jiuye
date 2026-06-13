using System;
using System.Collections.Generic;
using Jianghu.Cultivation;
using Xunit;

public class SchemaShapeTests
{
    [Fact]
    public void CanConstruct_MinimalPathDef()
    {
        var p = new CultivationPathDef(
            "sword_immortal", "剑修", "physical",
            new[] { "melee", "sword" },
            new[] { new ResourceDef("swordWill", 0, 100, 0) },
            new PowerFormulaDef(new[] { new PowerTerm("stat:Force", 4, null) }, Array.Empty<PowerMod>(), null),
            new RealmCurveDef(new[] { 10, 15, 25 }, new[] { 0, 1, 2 }, new[] { "凝气", "小成", "大成" }, new[] { 0, 100, 300 }),
            new[] { new ArtCategoryDef("剑法", "attack", 1, 1, new[] { new ArtDef("a1", "三尺霜", 1, "剑法", Array.Empty<EffectOp>()) }) },
            new[] { new CombatSkillDef("s1", "剑二十三", 3, Array.Empty<EffectOp>(), new Dictionary<string, int>()) },
            new EntryGateDef("tag:sword_root"), new SelectionRuleDef(1, 3), null);
        Assert.Equal("sword_immortal", p.PathId);
        Assert.Single(p.ArtCategories);
    }
}
