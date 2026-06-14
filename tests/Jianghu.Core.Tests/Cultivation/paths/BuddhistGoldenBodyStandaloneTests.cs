using System.Linq;
using Jianghu.Cultivation;
using Jianghu.Cultivation.Paths;
using Xunit;

/// <summary>
/// 佛修·金身道 buddhist_golden_body 独立数据测试（standalone：只验 Def 本身，不依赖注册/生成/SparAction）。
/// ①PathValidator 过（数据质量 gate：R2/R3/R4/R6/M1/M4 全过）；
/// ②canon pathId + shape（≥3 战斗类目+daoheart、≥5 战技、Curve 四列等长、terms 无 ×0 无 daoHeart、
///   physical AttackDimension、anti_evil 克邪属性 tag、EntryGate=tag:fo_root）。
/// 全局命名空间 public class（与范式 Jianghu.Core.Tests.Cultivation.Paths 下的注册/生成测试解耦）。
/// </summary>
public class BuddhistGoldenBodyStandaloneTests
{
    // ① 数据质量 gate：佛修 Def 必过 PathValidator 全部校验（不抛即过）。
    [Fact]
    public void Def_PassesPathValidator()
    {
        PathValidator.AssertValid(BuddhistGoldenBodyPath.Def);
    }

    // ② canon pathId + shape：≥3 类目且含 daoheart、≥5 战技、Curve 四列等长、terms 无 ×0 无 daoHeart。
    [Fact]
    public void Def_IsCanonAndShaped()
    {
        var d = BuddhistGoldenBodyPath.Def;

        // canon pathId + flavor 维度 + entry tag。
        Assert.Equal("buddhist_golden_body", d.PathId);
        Assert.Equal("physical", d.AttackDimension);
        Assert.Equal("tag:fo_root", d.EntryGate.Pred);

        // ≥3 战斗类目 + 含 1 个 daoheart 占位类目；每类 Arts ≥4。
        Assert.True(d.ArtCategories.Count >= 3);
        Assert.Contains(d.ArtCategories, c => c.Role == "daoheart");
        Assert.All(d.ArtCategories, c => Assert.True(c.Arts.Count >= 4));

        // daoheart 占位类目：tier=0（sumArtPower 贡献 0）+ effects 留空（A.0 仅装载不结算）。
        var daoheart = d.ArtCategories.Single(c => c.Role == "daoheart");
        Assert.All(daoheart.Arts, a => Assert.Equal(0, a.Tier));
        Assert.All(daoheart.Arts, a => Assert.Empty(a.Effects));

        // ≥5 战技。
        Assert.True(d.CombatSkills.Count >= 5);

        // terms 无 ×0（R6）、无 daoHeart/innerDemon（R3）。
        Assert.All(d.Power.Terms, t => Assert.NotEqual(0, t.Weight));
        Assert.DoesNotContain(d.Power.Terms,
            t => t.Src.Contains("daoHeart") || t.Src.Contains("innerDemon"));

        // Curve 四列等长（M4）。
        Assert.Equal(d.Curve.RealmMultipliers.Count, d.Curve.UnifiedTierOf.Count);
        Assert.Equal(d.Curve.RealmMultipliers.Count, d.Curve.RealmNames.Count);
        Assert.Equal(d.Curve.RealmMultipliers.Count, d.Curve.RealmThresholds.Count);

        // SituationalTags = 属性/形态 tag（非对手 PathId）；含克邪签名 tag anti_evil。
        Assert.Equal(new[] { "melee", "righteous", "anti_evil" }, d.SituationalTags);
        Assert.DoesNotContain(d.SituationalTags, t => PathValidator.KnownPathIds.Contains(t));

        // 路线专属克邪资源 vow 在册（佛光克邪的唯一驱动资源）。
        Assert.Contains(d.Resources, r => r.Key == "vow");
    }
}
