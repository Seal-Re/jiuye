using System.Linq;
using Jianghu.Cultivation;
using Jianghu.Cultivation.Paths;
using Xunit;

/// <summary>
/// 符修 fu_xiu_fulu 的 standalone 数据 gate：只验 <see cref="FuXiuFuluPath.Def"/> 本身，
/// 不依赖路线注册 / 生成期 / SparAction（与剑修端到端 <c>SwordImmortalTests</c> 解耦，避免并发竞争共享装配）。
/// 全局命名空间 public class，确保 xUnit 发现。
/// </summary>
public class FuXiuFuluStandaloneTests
{
    // ① 数据质量 gate：符修 Def 必过 PathValidator 全部校验（R2/R3/R4/R6/M1/M4）。
    [Fact]
    public void Def_PassesPathValidator()
    {
        PathValidator.AssertValid(FuXiuFuluPath.Def); // 不抛即过
    }

    // ② canon pathId + 形态校验：≥3 战斗类目 + daoheart 占位、≥5 战技、Curve 四列等长、terms 无 ×0 无 daoHeart。
    [Fact]
    public void Def_IsCanonAndShaped()
    {
        var d = FuXiuFuluPath.Def;

        // canon 全名 pathId（R4）。
        Assert.Equal("fu_xiu_fulu", d.PathId);

        // 含 1 个 Role==daoheart 占位类目（M1），且其为 tier=0 + 空 Effects（A.0 仅装载不结算）。
        var daoheart = d.ArtCategories.Single(c => c.Role == "daoheart");
        Assert.All(daoheart.Arts, a => Assert.Equal(0, a.Tier));
        Assert.All(daoheart.Arts, a => Assert.Empty(a.Effects));

        // ≥3 战斗类目（不含 daoheart 占位）+ 每类 ≥4 具名功法。
        Assert.True(d.ArtCategories.Count(c => c.Role != "daoheart") >= 3);
        Assert.True(d.ArtCategories.Count >= 3);
        Assert.All(d.ArtCategories, c => Assert.True(c.Arts.Count >= 4));

        // ≥5 具名战技。
        Assert.True(d.CombatSkills.Count >= 5);

        // terms 无 ×0（R6）、无 daoHeart/innerDemon（R3）。
        Assert.All(d.Power.Terms, t => Assert.NotEqual(0, t.Weight));
        Assert.DoesNotContain(d.Power.Terms, t => t.Src.Contains("daoHeart") || t.Src.Contains("innerDemon"));

        // Curve 四列等长（M4）= 8。
        Assert.Equal(8, d.Curve.RealmMultipliers.Count);
        Assert.Equal(d.Curve.RealmMultipliers.Count, d.Curve.UnifiedTierOf.Count);
        Assert.Equal(d.Curve.RealmMultipliers.Count, d.Curve.RealmNames.Count);
        Assert.Equal(d.Curve.RealmMultipliers.Count, d.Curve.RealmThresholds.Count);
        // A.1 §2：SubLevelCount 前缀和闭合（Σ == flatIndex 数）。
        Assert.Equal(d.Curve.RealmMultipliers.Count, d.Curve.SubLevelCount.Sum());

        // SituationalTags = 属性/形态 tag（非对手 PathId）。
        Assert.Equal(new[] { "ranged", "fire", "thunder", "talisman" }, d.SituationalTags);

        // EntryGate 用本路唯一 entry tag。
        Assert.Equal("tag:fu_root", d.EntryGate.Pred);

        // 战技 Cost key 只引现有 per-path 资源（talismanStore/fuPotency），不引未声明键（避免 chokepoint 崩）。
        var resKeys = d.Resources.Select(r => r.Key).ToHashSet();
        Assert.All(d.CombatSkills, s => Assert.All(s.Cost.Keys, k => Assert.Contains(k, resKeys)));
    }
}
