using System.Linq;
using Jianghu.Cultivation;
using Jianghu.Cultivation.Paths;
using Xunit;

/// <summary>
/// 命修 ming_fate_causality 数据自洽 standalone 测试（只验 Def 本身，不依赖注册/生成/世界）。
/// ①PathValidator 全部校验过；②canon pathId + 形态校验（≥3+daoheart 类目、每类 ≥4、≥5 战技、
/// RealmCurve 四列等长、terms 无 ×0 无 daoHeart/innerDemon、SituationalTags 非对手 PathId）。
/// 全局命名空间 public class，避免与注册/生成端测试耦合。
/// </summary>
public class MingFateCausalityStandaloneTests
{
    // ① 数据质量 gate：命修 Def 必过 PathValidator 全部校验（R2/R3/R4/R6/M1/M4）。
    [Fact]
    public void Def_PassesPathValidator()
    {
        PathValidator.AssertValid(MingFateCausalityPath.Def); // 不抛即过
    }

    // ② canon pathId + 形态：≥3 战斗类目+daoheart 占位、每类 ≥4 具名、≥5 战技、
    //    Curve 四列等长、terms 无 ×0 无 daoHeart/innerDemon、daoheart 占位 tier=0 空 Effects、SituationalTags 非 PathId。
    [Fact]
    public void Def_IsCanonAndShaped()
    {
        var d = MingFateCausalityPath.Def;

        // canon pathId（R4）+ EntryGate 唯一 entry tag。
        Assert.Equal("ming_fate_causality", d.PathId);
        Assert.Equal("tag:ming_root", d.EntryGate.Pred);

        // 含 1 个 Role==daoheart 类目（M1）；类目 ≥3（含 daoheart）；每类 Arts ≥4。
        Assert.Contains(d.ArtCategories, c => c.Role == "daoheart");
        Assert.True(d.ArtCategories.Count >= 3);
        Assert.All(d.ArtCategories, c => Assert.True(c.Arts.Count >= 4));

        // daoheart 占位类目：A.0 仅装载不结算 → 每条 art tier=0 且 Effects 留空（不触 daoHeart/innerDemon 算子）。
        var daoheart = d.ArtCategories.Single(c => c.Role == "daoheart");
        Assert.All(daoheart.Arts, a => Assert.Equal(0, a.Tier));
        Assert.All(daoheart.Arts, a => Assert.Empty(a.Effects));

        // 战技 ≥5。
        Assert.True(d.CombatSkills.Count >= 5);

        // terms 无 ×0（R6）、无 daoHeart/innerDemon（R3）。
        Assert.All(d.Power.Terms, t => Assert.NotEqual(0, t.Weight));
        Assert.DoesNotContain(d.Power.Terms, t => t.Src.Contains("daoHeart") || t.Src.Contains("innerDemon"));

        // RealmCurve 四列等长（M4）。
        var cv = d.Curve;
        Assert.Equal(cv.RealmMultipliers.Count, cv.UnifiedTierOf.Count);
        Assert.Equal(cv.RealmMultipliers.Count, cv.RealmNames.Count);
        Assert.Equal(cv.RealmMultipliers.Count, cv.RealmThresholds.Count);
        // A.1 §2：SubLevelCount 前缀和闭合（Σ == flatIndex 数）。
        Assert.Equal(cv.RealmMultipliers.Count, cv.SubLevelCount.Sum());

        // SituationalTags = 属性/形态 tag（非已知 21 路对手 PathId，R2）。
        Assert.NotEmpty(d.SituationalTags);
        Assert.All(d.SituationalTags, t => Assert.DoesNotContain(t, PathValidator.KnownPathIds));
    }
}
