using System.Linq;
using Jianghu.Cultivation;
using Jianghu.Cultivation.Paths;
using Xunit;

/// <summary>
/// 妖修·化形道 yao_xiu_huaxing 数据自洽 standalone 测试（只验 Def 本身，不依赖注册/生成/World）。
/// ①Def 必过 PathValidator 全部校验门；②canon pathId + 形状（≥3 战斗类目 + daoheart 妖心、≥5 战技、
///   RealmCurve 四列等长、terms 无 ×0 且无 daoHeart/innerDemon、SituationalTags 非已知 pathId）。
/// 全局命名空间 public class（standalone，不进 Jianghu.Core.Tests.* 命名空间，避免依赖范式测试夹具）。
/// </summary>
public class YaoXiuHuaxingStandaloneTests
{
    // ① 数据质量 gate：妖修 Def 必过 PathValidator 全部校验（不抛即过）。
    [Fact]
    public void Def_PassesPathValidator()
    {
        PathValidator.AssertValid(YaoXiuHuaxingPath.Def); // 不抛即过（R2/R3/R4/R6/M1/M4 全绿）
    }

    // ② canon pathId + 形状自洽：类目/战技/曲线/terms 结构守红线。
    [Fact]
    public void Def_IsCanonAndShaped()
    {
        var d = YaoXiuHuaxingPath.Def;

        // canon pathId（R4）。
        Assert.Equal("yao_xiu_huaxing", d.PathId);

        // 含 1 个 Role=daoheart 类目（M1，妖心）。
        Assert.Contains(d.ArtCategories, c => c.Role == "daoheart");

        // ≥3 战斗类目（含 daoheart 共 ≥4 类），每类 Arts ≥4。
        Assert.True(d.ArtCategories.Count >= 3);
        Assert.All(d.ArtCategories, c => Assert.True(c.Arts.Count >= 4));

        // daoheart 占位类目：tier=0 + effects 空（A.0 仅装载不结算）。
        var daoheart = d.ArtCategories.Single(c => c.Role == "daoheart");
        Assert.All(daoheart.Arts, a => Assert.Equal(0, a.Tier));
        Assert.All(daoheart.Arts, a => Assert.Empty(a.Effects));

        // ≥5 战技。
        Assert.True(d.CombatSkills.Count >= 5);

        // RealmCurve 四列等长（M4）。
        Assert.Equal(d.Curve.RealmMultipliers.Count, d.Curve.UnifiedTierOf.Count);
        Assert.Equal(d.Curve.RealmMultipliers.Count, d.Curve.RealmNames.Count);
        Assert.Equal(d.Curve.RealmMultipliers.Count, d.Curve.RealmThresholds.Count);

        // terms 无 ×0（R6）、无 daoHeart/innerDemon（R3）。
        Assert.All(d.Power.Terms, t => Assert.NotEqual(0, t.Weight));
        Assert.DoesNotContain(d.Power.Terms, t => t.Src.Contains("daoHeart") || t.Src.Contains("innerDemon"));

        // SituationalTags = 属性/形态 tag（非对手 PathId）：本路 physical 兽躯肉身。
        Assert.NotEmpty(d.SituationalTags);
        Assert.Equal(new[] { "melee", "brute", "parasite", "evil" }, d.SituationalTags);
        Assert.DoesNotContain(d.SituationalTags, t => PathValidator.KnownPathIds.Contains(t));

        // AttackDimension=physical（兽躯肉身碾压）；EntryGate 唯一 entry tag。
        Assert.Equal("physical", d.AttackDimension);
        Assert.Equal("tag:yao_root", d.EntryGate.Pred);
    }
}
