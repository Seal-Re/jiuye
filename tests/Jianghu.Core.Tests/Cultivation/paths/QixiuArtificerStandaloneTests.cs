using System.Linq;
using Jianghu.Cultivation;
using Jianghu.Cultivation.Paths;
using Xunit;

/// <summary>
/// 器修 qixiu_artificer 范式数据 standalone 校验（只验 Def 本身，不依赖注册/生成/World）。
/// ①PathValidator 全部校验过（R2/R3/R4/R6/M1/M4 数据质量 gate）；
/// ②canon pathId + 形状（≥3 战斗类目+daoheart、≥5 战技、Curve 四列等长、terms 无 ×0 无 daoHeart、
///   forgeheart 占位类目 tier=0 空 Effects、SituationalTags 非已知 pathId）。
/// 全局命名空间 public class（standalone，主控集中编译/注册测在别处）。
/// </summary>
public class QixiuArtificerStandaloneTests
{
    // ① 数据质量 gate：器修 Def 必过 PathValidator 全部校验（不抛即过）。
    [Fact]
    public void Def_PassesPathValidator()
    {
        PathValidator.AssertValid(QixiuArtificerPath.Def);
    }

    // ② canon pathId + 形状：≥3 战斗类目+daoheart、≥5 战技、Curve 四列等长、terms 无 ×0 无 daoHeart、
    //    forgeheart 占位类目 tier=0 空 Effects、SituationalTags 非对手 PathId。
    [Fact]
    public void Def_IsCanonAndShaped()
    {
        var d = QixiuArtificerPath.Def;

        // canon pathId（R4，给定 canon 全名）。
        Assert.Equal("qixiu_artificer", d.PathId);

        // ≥3 战斗类目 + 含 1 个 Role==daoheart 类目（M1，§12）。
        Assert.True(d.ArtCategories.Count >= 3);
        Assert.Contains(d.ArtCategories, c => c.Role == "daoheart");
        // 每类 Arts ≥4（§12）。
        Assert.All(d.ArtCategories, c => Assert.True(c.Arts.Count >= 4));

        // daoheart 占位类目：tier=0（A.0 仅装载不结算，sumArtPower 贡献 0）且 effects 全空（不触道心三键资源算子）。
        var daoheart = d.ArtCategories.Single(c => c.Role == "daoheart");
        Assert.True(daoheart.Arts.Count >= 4);
        Assert.All(daoheart.Arts, a => Assert.Equal(0, a.Tier));
        Assert.All(daoheart.Arts, a => Assert.Empty(a.Effects));

        // ≥5 具名战技（§12）。
        Assert.True(d.CombatSkills.Count >= 5);

        // terms 无 ×0（R6）、无 daoHeart/innerDemon（R3）。
        Assert.All(d.Power.Terms, t => Assert.NotEqual(0, t.Weight));
        Assert.DoesNotContain(d.Power.Terms,
            t => t.Src.Contains("daoHeart") || t.Src.Contains("innerDemon"));

        // RealmCurve 四列等长（M4）：倍率/UnifiedTierOf/RealmNames/RealmThresholds 同长。
        int n = d.Curve.RealmMultipliers.Count;
        Assert.Equal(n, d.Curve.UnifiedTierOf.Count);
        Assert.Equal(n, d.Curve.RealmNames.Count);
        Assert.Equal(n, d.Curve.RealmThresholds.Count);

        // SituationalTags = 属性/形态 tag（非已知 21 路 pathId，R2）。
        Assert.NotEmpty(d.SituationalTags);
        Assert.All(d.SituationalTags, tag => Assert.DoesNotContain(tag, PathValidator.KnownPathIds));

        // EntryGate = 给定唯一 entry tag。
        Assert.Equal("tag:qixiu_root", d.EntryGate.Pred);
    }
}
