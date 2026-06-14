using System.Linq;
using Jianghu.Cultivation;
using Jianghu.Cultivation.Paths;
using Xunit;

/// <summary>
/// 雷修·天劫雷法 lei_xiu standalone 数据自检（仅验 Def 本身，不依赖注册/生成/世界）。
/// ① Def 必过 PathValidator 全部校验（R2/R3/R4/R6 + M1/M4）。
/// ② canon pathId + 形态（≥3 战斗类目 + daoheart 占位类目、≥5 战技、Curve 四列等长、terms 无 ×0 无 daoHeart）。
/// 全局命名空间 public class（与范式 standalone 自检同形，不进 Jianghu.Core.Tests.* 子命名空间以示「纯数据自检」）。
/// </summary>
public class LeiXiuStandaloneTests
{
    // ① 数据质量 gate：雷修 Def 必过 PathValidator 全部校验，不抛即过。
    [Fact]
    public void Def_PassesPathValidator()
    {
        PathValidator.AssertValid(LeiXiuPath.Def); // 不抛即过
    }

    // ② canon pathId + 形态自检：≥3 战斗类目 + 含 daoheart 占位类目、每类 Arts≥4、≥5 战技、
    //    Curve 四列等长、terms 无 ×0 无 daoHeart/innerDemon、SituationalTags 非已知 pathId。
    [Fact]
    public void Def_IsCanonAndShaped()
    {
        var d = LeiXiuPath.Def;

        // canon pathId + 唯一 entry tag。
        Assert.Equal("lei_xiu", d.PathId);
        Assert.Equal("tag:lei_root", d.EntryGate.Pred);

        // ArtCategories：≥3 类、每类 Arts≥4、含 1 个 Role==daoheart 占位类目。
        Assert.True(d.ArtCategories.Count >= 3);
        Assert.All(d.ArtCategories, c => Assert.True(c.Arts.Count >= 4));
        Assert.Contains(d.ArtCategories, c => c.Role == "daoheart");

        // daoheart 占位类目：tier==0 且 Effects 全空（A.0 仅装载不结算）。
        var daoheart = d.ArtCategories.Single(c => c.Role == "daoheart");
        Assert.All(daoheart.Arts, a => Assert.Equal(0, a.Tier));
        Assert.All(daoheart.Arts, a => Assert.Empty(a.Effects));

        // 战技 ≥5。
        Assert.True(d.CombatSkills.Count >= 5);

        // terms 无 ×0（R6）、无 daoHeart/innerDemon（R3）。
        Assert.All(d.Power.Terms, t => Assert.NotEqual(0, t.Weight));
        Assert.DoesNotContain(d.Power.Terms,
            t => t.Src.Contains("daoHeart") || t.Src.Contains("innerDemon"));

        // Curve 四列等长（M4）。
        Assert.Equal(d.Curve.RealmMultipliers.Count, d.Curve.UnifiedTierOf.Count);
        Assert.Equal(d.Curve.RealmMultipliers.Count, d.Curve.RealmNames.Count);
        Assert.Equal(d.Curve.RealmMultipliers.Count, d.Curve.RealmThresholds.Count);
        // A.1 §2：SubLevelCount 前缀和闭合（Σ == flatIndex 数）。
        Assert.Equal(d.Curve.RealmMultipliers.Count, d.Curve.SubLevelCount.Sum());

        // SituationalTags = 属性/形态 tag（非对手 PathId）：雷修纯阳破邪声明 thunder/spirit_attack/righteous。
        Assert.Equal(new[] { "thunder", "spirit_attack", "righteous" }, d.SituationalTags);
        Assert.DoesNotContain(d.SituationalTags, t => PathValidator.KnownPathIds.Contains(t));
    }
}
