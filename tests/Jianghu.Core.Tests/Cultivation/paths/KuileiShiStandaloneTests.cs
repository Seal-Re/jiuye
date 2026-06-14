using System.Linq;
using Jianghu.Cultivation;
using Jianghu.Cultivation.Paths;
using Xunit;

/// <summary>
/// 傀儡师·机关魁儡道 <c>kuilei_shi</c> standalone 数据测试（只验 Def 本身，不依赖注册/生成/世界）。
/// 全局命名空间 public class，对齐 A.0 剑修范式 standalone gate：①PathValidator 全部校验过；
/// ②canon + 形态符合（pathId、≥3+daoheart 类目、每类 ≥4、≥5 战技、Curve 四列等长、terms 无 ×0 无 daoHeart）。
/// </summary>
public class KuileiShiStandaloneTests
{
    // ① 数据质量 gate：傀儡师 Def 必过 PathValidator 全部校验（R2/R3/R4/R6/M1/M4）。
    [Fact]
    public void Def_PassesPathValidator()
    {
        PathValidator.AssertValid(KuileiShiPath.Def); // 不抛即过
    }

    // ② canon pathId + 形态：≥3 战斗类目 + daoheart 占位、每类 ≥4 功法、≥5 战技、Curve 四列等长、
    //    terms 无 ×0 项、无 daoHeart/innerDemon Src、daoheart 类目 tier=0 空 Effects、EntryGate=kuilei_root、
    //    SituationalTags 非对手 PathId（属性/形态 tag）。
    [Fact]
    public void Def_IsCanonAndShaped()
    {
        var d = KuileiShiPath.Def;

        // canon pathId。
        Assert.Equal("kuilei_shi", d.PathId);

        // ≥3 类目且含 1 个 daoheart（M1）。
        Assert.True(d.ArtCategories.Count >= 3);
        Assert.Contains(d.ArtCategories, c => c.Role == "daoheart");

        // 每类 Arts ≥4。
        Assert.All(d.ArtCategories, c => Assert.True(c.Arts.Count >= 4));

        // ≥5 战技。
        Assert.True(d.CombatSkills.Count >= 5);

        // daoheart 占位类目：tier=0（sumArtPower 贡献 0）且 Effects 留空（A.0 仅装载不结算）。
        var daoheart = d.ArtCategories.Single(c => c.Role == "daoheart");
        Assert.All(daoheart.Arts, a => Assert.Equal(0, a.Tier));
        Assert.All(daoheart.Arts, a => Assert.Empty(a.Effects));

        // terms 无 ×0（R6）、无 daoHeart/innerDemon Src（R3）。
        Assert.All(d.Power.Terms, t => Assert.NotEqual(0, t.Weight));
        Assert.DoesNotContain(d.Power.Terms, t => t.Src.Contains("daoHeart") || t.Src.Contains("innerDemon"));

        // RealmCurve 四列等长（M4）。
        int n = d.Curve.RealmMultipliers.Count;
        Assert.Equal(n, d.Curve.UnifiedTierOf.Count);
        Assert.Equal(n, d.Curve.RealmNames.Count);
        Assert.Equal(n, d.Curve.RealmThresholds.Count);

        // EntryGate = 给定唯一 entry tag。
        Assert.Equal("tag:kuilei_root", d.EntryGate.Pred);

        // SituationalTags = 属性/形态 tag（非对手 PathId，R2）：死物路核心 undead_construct（免精神/毒/魅）。
        Assert.Equal(new[] { "undead_construct", "melee", "brute" }, d.SituationalTags);
        Assert.DoesNotContain(d.SituationalTags, t => PathValidator.KnownPathIds.Contains(t));
    }
}
