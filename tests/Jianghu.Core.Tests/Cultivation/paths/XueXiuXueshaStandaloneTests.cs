using System.Linq;
using Jianghu.Cultivation;
using Jianghu.Cultivation.Paths;
using Xunit;

/// <summary>
/// 血修·血煞道 <c>xue_xiu_xuesha</c> standalone 数据质量门（只验 Def 本身，不依赖注册/生成/对战）。
/// 全局命名空间 public class，专测路线数据契约，主控集中编译时与剑修范式同口径过 gate。
/// </summary>
public class XueXiuXueshaStandaloneTests
{
    // ① 数据质量 gate：血修 Def 必过 PathValidator 全部校验（R4 canon / M1 daoheart / §12 类目战技 / R2 tag / R6 无×0 / R3 无 daoHeart / M4 四列等长）。
    [Fact]
    public void Def_PassesPathValidator()
    {
        PathValidator.AssertValid(XueXiuXueshaPath.Def); // 不抛即过
    }

    // ② canon pathId + 形态：≥3 战斗类目+daoheart、每类≥4、≥5 战技、Curve 四列等长、terms 无 ×0 无 daoHeart、SituationalTags 非 PathId。
    [Fact]
    public void Def_IsCanonAndShaped()
    {
        var d = XueXiuXueshaPath.Def;

        // canon pathId（R4）+ EntryGate 唯一 entry tag。
        Assert.Equal("xue_xiu_xuesha", d.PathId);
        Assert.Equal("tag:xue_root", d.EntryGate.Pred);

        // 含 daoheart 类目（bloodheart 血心，M1）；≥3 类目；每类 Arts≥4；daoheart 类目 tier=0 且 effects 空（A.0 仅装载不结算）。
        Assert.Contains(d.ArtCategories, c => c.Role == "daoheart");
        Assert.True(d.ArtCategories.Count >= 3);
        Assert.All(d.ArtCategories, c => Assert.True(c.Arts.Count >= 4));
        var daoheart = d.ArtCategories.Single(c => c.Role == "daoheart");
        Assert.All(daoheart.Arts, a => Assert.Equal(0, a.Tier));
        Assert.All(daoheart.Arts, a => Assert.Empty(a.Effects));

        // CombatSkills ≥5。
        Assert.True(d.CombatSkills.Count >= 5);

        // terms 无 ×0（R6）、无 daoHeart/innerDemon（R3）；血煞 xuesha 不在 terms（深度设计原案 ×0 已删）。
        Assert.All(d.Power.Terms, t => Assert.NotEqual(0, t.Weight));
        Assert.DoesNotContain(d.Power.Terms, t => t.Src.Contains("daoHeart") || t.Src.Contains("innerDemon"));
        Assert.DoesNotContain(d.Power.Terms, t => t.Src == "res:xuesha");

        // 本路签名：血气项 res:qixie 挂 WeightStepKey="burnStep"（燃血阶跃爆发开关）。
        Assert.Contains(d.Power.Terms, t => t.Src == "res:qixie" && t.WeightStepKey == "burnStep");

        // Curve 四列等长（M4）。
        Assert.Equal(d.Curve.RealmMultipliers.Count, d.Curve.UnifiedTierOf.Count);
        Assert.Equal(d.Curve.RealmMultipliers.Count, d.Curve.RealmNames.Count);
        Assert.Equal(d.Curve.RealmMultipliers.Count, d.Curve.RealmThresholds.Count);
        // A.1 §2：SubLevelCount 前缀和闭合（Σ == flatIndex 数）。
        Assert.Equal(d.Curve.RealmMultipliers.Count, d.Curve.SubLevelCount.Sum());

        // SituationalTags = 属性/形态 tag（非对手 PathId，R2）。
        Assert.Equal(new[] { "melee", "brute", "evil" }, d.SituationalTags);
    }
}
