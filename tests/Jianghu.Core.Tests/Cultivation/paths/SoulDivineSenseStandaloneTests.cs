using System.Linq;
using Jianghu.Cultivation;
using Xunit;

/// <summary>
/// 魂修·神识元神道 soul_divine_sense standalone 数据 gate（只验 Def 本身，不依赖注册/生成/World）。
/// ①PathValidator 全部校验过；②canon pathId + 形状（≥3 战斗类目+daoheart、≥5 战技、Curve 四列等长、
/// terms 无 ×0 无 daoHeart/innerDemon、SituationalTags=属性 tag 非 PathId、daoheart 占位 tier=0 空 Effects）。
/// 全局命名空间 public class，与 21 路并行作者文件隔离不互拖（仅 using Jianghu.Cultivation）。
/// </summary>
public class SoulDivineSenseStandaloneTests
{
    // ① 数据质量 gate：魂修 Def 必过 PathValidator 全部校验（R2/R3/R4/R6/M1/M4）。
    [Fact]
    public void Def_PassesPathValidator()
    {
        PathValidator.AssertValid(SoulDivineSensePath.Def); // 不抛即过
    }

    // ② canon pathId + 形状：≥3 类目含 daoheart、≥5 战技、Curve 四列等长、terms 无 ×0 无 daoHeart。
    [Fact]
    public void Def_IsCanonAndShaped()
    {
        var d = SoulDivineSensePath.Def;

        // canon pathId（R4）。
        Assert.Equal("soul_divine_sense", d.PathId);

        // ArtCategories ≥3 且含 1 个 Role==daoheart 占位类目（M1），每类 Arts ≥4。
        Assert.True(d.ArtCategories.Count >= 3);
        Assert.Contains(d.ArtCategories, c => c.Role == "daoheart");
        Assert.All(d.ArtCategories, c => Assert.True(c.Arts.Count >= 4));

        // daoheart 占位类目 A.0 仅装载不结算：tier=0（sumArtPower 贡献 0）且 Effects 留空（不触资源算子）。
        var daoheart = d.ArtCategories.Single(c => c.Role == "daoheart");
        Assert.All(daoheart.Arts, a => Assert.Equal(0, a.Tier));
        Assert.All(daoheart.Arts, a => Assert.Empty(a.Effects));

        // CombatSkills ≥5。
        Assert.True(d.CombatSkills.Count >= 5);

        // PowerFormula.Terms 无 ×0 项（R6）、无 Src 含 daoHeart/innerDemon（R3）。
        Assert.All(d.Power.Terms, t => Assert.NotEqual(0, t.Weight));
        Assert.DoesNotContain(d.Power.Terms,
            t => t.Src.Contains("daoHeart") || t.Src.Contains("innerDemon"));

        // RealmCurve 四列等长（M4）。
        Assert.Equal(d.Curve.RealmMultipliers.Count, d.Curve.UnifiedTierOf.Count);
        Assert.Equal(d.Curve.RealmMultipliers.Count, d.Curve.RealmNames.Count);
        Assert.Equal(d.Curve.RealmMultipliers.Count, d.Curve.RealmThresholds.Count);

        // SituationalTags = 属性/形态 tag（spirit 正交·绕物防），非对手 PathId（R2）；含 spirit_attack。
        Assert.Equal(new[] { "spirit", "spirit_attack", "ranged" }, d.SituationalTags);
        Assert.DoesNotContain(d.SituationalTags, t => PathValidator.KnownPathIds.Contains(t));

        // EntryGate 用本路唯一 entry tag soul_root。
        Assert.Equal("tag:soul_root", d.EntryGate.Pred);
    }
}
