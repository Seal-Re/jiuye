using System.Linq;
using Jianghu.Cultivation;
using Jianghu.Cultivation.Paths;
using Xunit;

/// <summary>
/// 毒蛊修 du_gu_xiu standalone 数据质量 gate（只验 Def 本身，不依赖注册/生成/World）。
/// ①PathValidator.AssertValid 全部校验过；②canon pathId + shape（≥3+daoheart 类目、≥5 战技、
/// Curve 四列等长、terms 无 ×0 无 daoHeart/innerDemon、噬主度 guRevolt 不入 term、SituationalTags 非对手 PathId）。
/// 全局命名空间 public class，避免依赖 Jianghu.Core.Tests 内任何 mock/helper（与范式注册端到端测试并存不冲突）。
/// </summary>
public class DuGuXiuStandaloneTests
{
    // ① 数据质量 gate：毒蛊修 Def 必过 PathValidator 全部校验（R2/R3/R4/R6/M1/M4）。不抛即过。
    [Fact]
    public void Def_PassesPathValidator()
    {
        PathValidator.AssertValid(DuGuXiuPath.Def); // 不抛即过
    }

    // ② canon pathId + shape：≥3+daoheart 类目、≥5 战技、Curve 四列等长、terms 无 ×0 无 daoHeart。
    [Fact]
    public void Def_IsCanonAndShaped()
    {
        var d = DuGuXiuPath.Def;

        // canon pathId（R4）+ entry gate 唯一 tag。
        Assert.Equal("du_gu_xiu", d.PathId);
        Assert.Equal("tag:gu_root", d.EntryGate.Pred);

        // ≥3 战斗类目 + 含 1 个 Role==daoheart 占位类目（M1），每类 Arts ≥4。
        Assert.True(d.ArtCategories.Count >= 3);
        Assert.Contains(d.ArtCategories, c => c.Role == "daoheart");
        Assert.All(d.ArtCategories, c => Assert.True(c.Arts.Count >= 4));

        // daoheart 占位类目 A.0 仅装载不结算 → tier=0、effects 留空。
        var daoheart = d.ArtCategories.Single(c => c.Role == "daoheart");
        Assert.All(daoheart.Arts, a => Assert.Equal(0, a.Tier));
        Assert.All(daoheart.Arts, a => Assert.Empty(a.Effects));

        // ≥5 具名战技。
        Assert.True(d.CombatSkills.Count >= 5);

        // Curve 四列等长（M4）。
        Assert.Equal(d.Curve.RealmMultipliers.Count, d.Curve.UnifiedTierOf.Count);
        Assert.Equal(d.Curve.RealmMultipliers.Count, d.Curve.RealmNames.Count);
        Assert.Equal(d.Curve.RealmMultipliers.Count, d.Curve.RealmThresholds.Count);
        // A.1 §2：SubLevelCount 前缀和闭合（Σ == flatIndex 数）。
        Assert.Equal(d.Curve.RealmMultipliers.Count, d.Curve.SubLevelCount.Sum());

        // terms 无 ×0（R6）、无 daoHeart/innerDemon（R3）。
        Assert.All(d.Power.Terms, t => Assert.NotEqual(0, t.Weight));
        Assert.DoesNotContain(d.Power.Terms, t => t.Src.Contains("daoHeart") || t.Src.Contains("innerDemon"));
        // 噬主度 guRevolt 是养蛊负债账本（与 daoHeart 同性）→ 绝不入 power term（仅由功法/战技 AddResource 落账）。
        Assert.DoesNotContain(d.Power.Terms, t => t.Src.Contains("guRevolt"));
        // 悟性为本路战力第一主源（physical 渗透但脑力驱动）。
        Assert.Contains(d.Power.Terms, t => t.Src == "stat:Insight");

        // SituationalTags = 属性/形态 tag（非对手 PathId，R2）。
        Assert.Equal(new[] { "physical", "parasite", "evil" }, d.SituationalTags);
        Assert.DoesNotContain(d.SituationalTags, tag => PathValidator.KnownPathIds.Contains(tag));
    }
}
