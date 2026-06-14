using System.Linq;
using Jianghu.Cultivation;
using Jianghu.Cultivation.Paths;
using Xunit;

/// <summary>
/// 阵修 array_formation 独立数据 gate（standalone：只验 <see cref="ArrayFormationPath.Def"/> 本身，
/// 不依赖路线注册/世界生成/对战）。守 PathValidator 全校验 + canon/shape 形状不变量。
/// 全局命名空间 public class，避免与注册/生成端到端测试耦合。
/// </summary>
public class ArrayFormationStandaloneTests
{
    // ① 数据质量 gate：阵修 Def 必过 PathValidator 全部校验（R2/R3/R4/R6/M1/M4 一站式）。
    [Fact]
    public void Def_PassesPathValidator()
    {
        PathValidator.AssertValid(ArrayFormationPath.Def); // 不抛即过
    }

    // ② 形状不变量：canon pathId + ≥3 战斗类目含 daoheart(阵心) + ≥5 战技 + Curve 四列等长 + terms 无 ×0 无 daoHeart。
    [Fact]
    public void Def_IsCanonAndShaped()
    {
        var d = ArrayFormationPath.Def;

        // canon pathId（R4）。
        Assert.Equal("array_formation", d.PathId);

        // ≥3 类目（含 1 个 Role==daoheart 的阵心占位类目，M1），每类 Arts ≥4。
        Assert.True(d.ArtCategories.Count >= 3);
        Assert.Contains(d.ArtCategories, c => c.Role == "daoheart");
        Assert.All(d.ArtCategories, c => Assert.True(c.Arts.Count >= 4));

        // daoheart 占位类目：tier=0（A.0 仅装载不结算）且 Effects 留空（不触 daoHeart 资源算子）。
        var daoheart = d.ArtCategories.Single(c => c.Role == "daoheart");
        Assert.All(daoheart.Arts, a => Assert.Equal(0, a.Tier));
        Assert.All(daoheart.Arts, a => Assert.Empty(a.Effects));

        // ≥5 战技。
        Assert.True(d.CombatSkills.Count >= 5);

        // RealmCurve 四列等长（M4）。
        Assert.Equal(d.Curve.RealmMultipliers.Count, d.Curve.UnifiedTierOf.Count);
        Assert.Equal(d.Curve.RealmMultipliers.Count, d.Curve.RealmNames.Count);
        Assert.Equal(d.Curve.RealmMultipliers.Count, d.Curve.RealmThresholds.Count);
        // A.1 §2：SubLevelCount 前缀和闭合（Σ == flatIndex 数）。
        Assert.Equal(d.Curve.RealmMultipliers.Count, d.Curve.SubLevelCount.Sum());

        // terms 无 ×0（R6）、无 daoHeart/innerDemon（R3）。
        Assert.All(d.Power.Terms, t => Assert.NotEqual(0, t.Weight));
        Assert.DoesNotContain(d.Power.Terms, t => t.Src.Contains("daoHeart") || t.Src.Contains("innerDemon"));

        // SituationalTags = 属性/形态 tag（非对手 PathId）。
        Assert.Equal(new[] { "physical", "control", "terrain_bound" }, d.SituationalTags);
    }
}
