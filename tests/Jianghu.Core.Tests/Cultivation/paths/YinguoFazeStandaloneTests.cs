using System.Linq;
using Jianghu.Cultivation;
using Jianghu.Cultivation.Paths;
using Xunit;

/// <summary>
/// 因果时空命运修·大道法则 yinguo_faze 的 standalone 数据质量门：只验 <see cref="YinguoFazePath.Def"/>
/// 本身（不依赖注册/生成/对局），与主控集中注册测试解耦，避免并发竞争。
/// ①PathValidator 全校验过（R2/R3/R4/R6/M1/M4）；②canon 形态自检（pathId、≥3+daoheart 类目、≥5 战技、
/// Curve 四列等长、terms 无 ×0 无 daoHeart）。全局命名空间 public class。
/// </summary>
public class YinguoFazeStandaloneTests
{
    // ① 数据质量 gate：因果路 Def 必过 PathValidator 全部校验（不抛即过）。
    [Fact]
    public void Def_PassesPathValidator()
    {
        PathValidator.AssertValid(YinguoFazePath.Def);
    }

    // ② canon + 形态自检：pathId 锚定、含 daoheart 类目、类目 ≥3 且每类 ≥4、战技 ≥5、
    //    Curve 四列等长、terms 无 ×0 无 daoHeart/innerDemon、SituationalTags 非已知 pathId。
    [Fact]
    public void Def_IsCanonAndShaped()
    {
        var d = YinguoFazePath.Def;

        // canon pathId（R4）。
        Assert.Equal("yinguo_faze", d.PathId);

        // 含 1 个 Role=daoheart 类目（M1），且 ≥3 战斗类目、每类 ≥4 具名功法。
        Assert.Contains(d.ArtCategories, c => c.Role == "daoheart");
        Assert.True(d.ArtCategories.Count >= 3);
        Assert.All(d.ArtCategories, c => Assert.True(c.Arts.Count >= 4));

        // daoheart 占位类目：tier=0 + Effects 空（A.0 仅装载不结算）。
        var daoheart = d.ArtCategories.Single(c => c.Role == "daoheart");
        Assert.True(daoheart.Arts.Count >= 4);
        Assert.All(daoheart.Arts, a =>
        {
            Assert.Equal(0, a.Tier);
            Assert.Empty(a.Effects);
        });

        // CombatSkills ≥5。
        Assert.True(d.CombatSkills.Count >= 5);

        // terms 无 ×0（R6）、无 daoHeart/innerDemon（R3）。
        Assert.All(d.Power.Terms, t => Assert.NotEqual(0, t.Weight));
        Assert.DoesNotContain(d.Power.Terms, t =>
            t.Src.Contains("daoHeart") || t.Src.Contains("innerDemon"));

        // RealmCurve 四列等长（M4，长度 8 = major 0..7；修正原案 8vs7）。
        int len = d.Curve.RealmMultipliers.Count;
        Assert.Equal(8, len);
        Assert.Equal(len, d.Curve.UnifiedTierOf.Count);
        Assert.Equal(len, d.Curve.RealmNames.Count);
        Assert.Equal(len, d.Curve.RealmThresholds.Count);

        // SituationalTags = 属性/形态 tag，非已知 pathId（R2）。
        Assert.NotEmpty(d.SituationalTags);
        Assert.DoesNotContain(d.SituationalTags, t => PathValidator.KnownPathIds.Contains(t));

        // EntryGate 锚定唯一 entry tag。
        Assert.Contains("yinguo_root", d.EntryGate.RequiredTags());
    }
}
