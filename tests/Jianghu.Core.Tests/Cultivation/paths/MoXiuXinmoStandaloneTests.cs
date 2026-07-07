using System.Linq;
using Jianghu.Cultivation;
using Jianghu.Cultivation.Paths;
using Xunit;

/// <summary>
/// 魔修 mo_xiu_xinmo standalone 数据契约测试（只验 Def 本身，不依赖注册/生成/PowerEngine）。
/// ①PathValidator 全部校验过；②canon pathId + 形状（≥3 战斗类目+daoheart、≥5 战技、Curve 四列等长、
/// terms 无 ×0 无 daoHeart/innerDemon、daoheart 类目 tier=0 空 Effects、SituationalTags 非 PathId）。
/// 全局命名空间，避免与注册侧测试耦合。
/// </summary>
public class MoXiuXinmoStandaloneTests
{
    // ① 数据质量 gate：魔修 Def 必过 PathValidator 全部校验（R2/R3/R4/R6/M1/M4）。
    [Fact]
    public void Def_PassesPathValidator()
    {
        PathValidator.AssertValid(MoXiuXinmoPath.Def); // 不抛即过
    }

    // ② canon pathId + 形状契约。
    [Fact]
    public void Def_IsCanonAndShaped()
    {
        var d = MoXiuXinmoPath.Def;

        // canon pathId（R4）。
        Assert.Equal("mo_xiu_xinmo", d.PathId);

        // 含 daoheart 类目 + ≥3 类目 + 每类 ≥4 具名（M1/§12）。
        Assert.Contains(d.ArtCategories, c => c.Role == "daoheart");
        Assert.True(d.ArtCategories.Count >= 3);
        Assert.All(d.ArtCategories, c => Assert.True(c.Arts.Count >= 4));

        // ≥5 具名战技（§12）。
        Assert.True(d.CombatSkills.Count >= 5);

        // RealmCurve 四列等长（M4）。
        var curve = d.Curve;
        Assert.Equal(curve.RealmMultipliers.Count, curve.UnifiedTierOf.Count);
        Assert.Equal(curve.RealmMultipliers.Count, curve.RealmNames.Count);
        Assert.Equal(curve.RealmMultipliers.Count, curve.RealmThresholds.Count);
        // A.1 §2：SubLevelCount 前缀和闭合（Σ == flatIndex 数）。
        Assert.Equal(curve.RealmMultipliers.Count, curve.SubLevelCount.Sum());

        // terms 无 ×0（R6）、无 daoHeart/innerDemon（R3）。
        Assert.All(d.Power.Terms, t => Assert.NotEqual(0, t.Weight));
        Assert.DoesNotContain(d.Power.Terms, t => t.Src.Contains("daoHeart") || t.Src.Contains("innerDemon"));

        // daoheart 占位类目：tier=0 且 Effects 留空（A.0 仅装载不结算）。
        var daoheart = d.ArtCategories.Single(c => c.Role == "daoheart");
        Assert.All(daoheart.Arts, a => Assert.Equal(0, a.Tier));
        Assert.All(daoheart.Arts, a => Assert.Empty(a.Effects));

        // SituationalTags = 属性/形态 tag（非对手 PathId，R2）：无 tag 等于任一已知 canon pathId。
        Assert.NotEmpty(d.SituationalTags);
        Assert.DoesNotContain(d.SituationalTags, t => PathValidator.KnownPathIds.Contains(t));

        // EntryGate 用本路唯一 entry tag。
        Assert.Equal("tag:mo_root", d.EntryGate.Pred);
    }
}
