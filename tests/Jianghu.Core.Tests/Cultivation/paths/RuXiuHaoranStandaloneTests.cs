using System.Linq;
using Jianghu.Cultivation;
using Xunit;

/// <summary>
/// 儒修 ru_xiu_haoran standalone 数据自检（只验 <see cref="RuXiuHaoranPath.Def"/> 本身，
/// 不依赖注册/生成/PowerEngine/SparAction，与主控集中跑的端到端套件解耦）。
/// ①PathValidator 全部校验过（R2/R3/R4/R6/M1/M4）；②canon + 形态：PathId==ru_xiu_haoran、
/// ≥3 战斗类目 + 含 daoheart 文心类目、≥5 战技、Curve 四列等长、terms 无 ×0 无 daoHeart。
/// 全局命名空间 public class（standalone，不进 Jianghu.Core.Tests.Cultivation.Paths 命名空间）。
/// </summary>
public class RuXiuHaoranStandaloneTests
{
    // ① 数据质量 gate：儒修 Def 必过 PathValidator 全部校验（不抛即过）。
    [Fact]
    public void Def_PassesPathValidator()
    {
        PathValidator.AssertValid(RuXiuHaoranPath.Def);
    }

    // ② canon pathId + 形态自检（≥3 类目含 daoheart 文心、≥5 战技、Curve 四列等长、terms 无 ×0 无 daoHeart）。
    [Fact]
    public void Def_IsCanonAndShaped()
    {
        var d = RuXiuHaoranPath.Def;

        // canon pathId（R4，给定 canon 全名）。
        Assert.Equal("ru_xiu_haoran", d.PathId);

        // ≥3 战斗类目 + 含 1 个 Role==daoheart 占位类目（M1，文心）。
        Assert.True(d.ArtCategories.Count >= 3);
        Assert.Contains(d.ArtCategories, c => c.Role == "daoheart");
        Assert.All(d.ArtCategories, c => Assert.True(c.Arts.Count >= 4));

        // daoheart 占位类目：tier=0（仅装载不结算）+ Effects 留空（不触 daoHeart/innerDemon 资源算子）。
        var daoheart = d.ArtCategories.Single(c => c.Role == "daoheart");
        Assert.All(daoheart.Arts, a => Assert.Equal(0, a.Tier));
        Assert.All(daoheart.Arts, a => Assert.Empty(a.Effects));

        // ≥5 具名战技。
        Assert.True(d.CombatSkills.Count >= 5);

        // terms 无 Weight==0（R6）、无 Src 含 daoHeart/innerDemon（R3 浩然×道心解耦）。
        Assert.All(d.Power.Terms, t => Assert.NotEqual(0, t.Weight));
        Assert.DoesNotContain(d.Power.Terms,
            t => t.Src.Contains("daoHeart") || t.Src.Contains("innerDemon"));

        // RealmCurve 四列等长（M4）。
        int m = d.Curve.RealmMultipliers.Count;
        Assert.Equal(m, d.Curve.UnifiedTierOf.Count);
        Assert.Equal(m, d.Curve.RealmNames.Count);
        Assert.Equal(m, d.Curve.RealmThresholds.Count);

        // SituationalTags = 属性/形态 tag（非对手 PathId，R2）。
        Assert.Equal(new[] { "spirit_attack", "righteous", "control" }, d.SituationalTags);

        // EntryGate 用给定唯一 entry tag。
        Assert.Equal("tag:ru_root", d.EntryGate.Pred);
    }
}
