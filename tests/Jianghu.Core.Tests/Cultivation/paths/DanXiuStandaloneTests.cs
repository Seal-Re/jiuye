using System.Linq;
using Jianghu.Cultivation;
using Jianghu.Cultivation.Paths;
using Xunit;

/// <summary>
/// 丹修 dan_xiu standalone 数据质量 gate（只验 Def 本身，不依赖注册/生成/对战）。
/// 全局命名空间 public class，避免与主测试命名空间耦合。
/// ①PathValidator 全部校验过；②canon + 形状（pathId、≥3+daoheart 类目、≥5 战技、Curve 四列等长、
/// terms 无 ×0 无 daoHeart）。落实丹修红线：弱战力强干预但仍合规（异火阶/丹方数专属资源入 power、
/// 武力/根骨 ×0 已按约定不发 term、pillheart 道心占位 tier=0 空 Effects）。
/// </summary>
public class DanXiuStandaloneTests
{
    // ① 数据质量 gate：丹修 Def 必过 PathValidator 全部校验（R4/M1/M4/R2/R3/R6 + §12）。
    [Fact]
    public void Def_PassesPathValidator()
    {
        PathValidator.AssertValid(DanXiuPath.Def); // 不抛即过
    }

    // ② canon pathId + 形状：≥3+daoheart 类目、每类 ≥4、≥5 战技、Curve 四列等长、terms 无 ×0 无 daoHeart。
    [Fact]
    public void Def_IsCanonAndShaped()
    {
        var d = DanXiuPath.Def;

        // canon pathId（R4）。
        Assert.Equal("dan_xiu", d.PathId);

        // 入门闸 = 唯一 dan_root（21 路唯一 entry tag 约定）。
        Assert.Equal("tag:dan_root", d.EntryGate.Pred);

        // ArtCategories ≥3 且含 daoheart 类目（M1），每类 Arts ≥4。
        Assert.True(d.ArtCategories.Count >= 3);
        Assert.Contains(d.ArtCategories, c => c.Role == "daoheart");
        Assert.All(d.ArtCategories, c => Assert.True(c.Arts.Count >= 4));

        // daoheart 占位类目：tier=0（sumArtPower 贡献 0）且 effects 留空（A.0 仅装载不结算）。
        var daoheart = d.ArtCategories.Single(c => c.Role == "daoheart");
        Assert.All(daoheart.Arts, a => Assert.Equal(0, a.Tier));
        Assert.All(daoheart.Arts, a => Assert.Empty(a.Effects));

        // CombatSkills ≥5。
        Assert.True(d.CombatSkills.Count >= 5);

        // terms 无 ×0（R6）、无 daoHeart/innerDemon（R3）。
        Assert.All(d.Power.Terms, t => Assert.NotEqual(0, t.Weight));
        Assert.DoesNotContain(d.Power.Terms,
            t => t.Src.Contains("daoHeart") || t.Src.Contains("innerDemon"));

        // Curve 四列等长（M4）。
        Assert.Equal(d.Curve.RealmMultipliers.Count, d.Curve.UnifiedTierOf.Count);
        Assert.Equal(d.Curve.RealmMultipliers.Count, d.Curve.RealmNames.Count);
        Assert.Equal(d.Curve.RealmMultipliers.Count, d.Curve.RealmThresholds.Count);

        // SituationalTags = 属性/形态 tag（非对手 PathId，R2）。
        Assert.Equal(new[] { "economic", "support", "fire" }, d.SituationalTags);

        // 丹修签名：异火阶/丹方数专属资源入 power（弱战力强干预的脊柱与库广度项）。
        Assert.Contains(d.Power.Terms, t => t.Src == "res:flameTier");
        Assert.Contains(d.Power.Terms, t => t.Src == "res:recipeCount");
    }
}
