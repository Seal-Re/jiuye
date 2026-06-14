using System.Linq;
using Jianghu.Cultivation;
using Jianghu.Cultivation.Paths;
using Xunit;

/// <summary>
/// 驭兽师·兽灵契主 yu_shou —— standalone 数据质量门（只验 Def 本身，不依赖注册/生成/PowerEngine）。
/// ① PathValidator 全部校验过（R2/R3/R4/R6/M1/M4）；
/// ② canon pathId + 形状（≥3 战斗类目 + daoheart 占位、≥5 战技、Curve 四列等长、terms 无 ×0 无 daoHeart、
///    daoheart 类目 tier=0 空 Effects、SituationalTags 非已知 pathId、EntryGate=tag:beast_root）。
/// 全局命名空间 public class（与剑修范式测试同构，但不依赖任何生成/世界 infra）。
/// </summary>
public class YuShouStandaloneTests
{
    // ① 数据质量 gate：驭兽 Def 必过 PathValidator 全部校验。
    [Fact]
    public void Def_PassesPathValidator()
    {
        PathValidator.AssertValid(YuShouPath.Def); // 不抛即过
    }

    // ② canon pathId + 形状（不依赖注册/生成）。
    [Fact]
    public void Def_IsCanonAndShaped()
    {
        var d = YuShouPath.Def;

        // canon pathId。
        Assert.Equal("yu_shou", d.PathId);

        // EntryGate = 给定唯一 entry tag。
        Assert.Equal("tag:beast_root", d.EntryGate.Pred);

        // 含 daoheart 占位类目 + ≥3 类（即 ≥3 战斗类目 + 1 daoheart）；每类 Arts ≥4。
        Assert.Contains(d.ArtCategories, c => c.Role == "daoheart");
        Assert.True(d.ArtCategories.Count >= 4); // 4 战斗类目 + 1 daoheart
        Assert.All(d.ArtCategories, c => Assert.True(c.Arts.Count >= 4));

        // daoheart 占位类目：tier=0（sumArtPower 贡献 0）且 Effects 留空（A.0 仅装载不结算）。
        var daoheart = d.ArtCategories.Single(c => c.Role == "daoheart");
        Assert.All(daoheart.Arts, a => Assert.Equal(0, a.Tier));
        Assert.All(daoheart.Arts, a => Assert.Empty(a.Effects));

        // ≥5 战技。
        Assert.True(d.CombatSkills.Count >= 5);

        // terms 无 ×0、无 daoHeart/innerDemon（R3/R6）。
        Assert.All(d.Power.Terms, t => Assert.NotEqual(0, t.Weight));
        Assert.DoesNotContain(d.Power.Terms,
            t => t.Src.Contains("daoHeart") || t.Src.Contains("innerDemon"));

        // 兽群强度抽象整数资源（rosterPower）确为战力主体项（权重最高）+ 作为声明资源存在。
        Assert.Contains(d.Power.Terms, t => t.Src == "res:rosterPower");
        Assert.Contains(d.Resources, r => r.Key == "rosterPower");
        var rosterTerm = d.Power.Terms.Single(t => t.Src == "res:rosterPower");
        Assert.True(d.Power.Terms.All(t => t.Weight <= rosterTerm.Weight),
            "兽群强度项应为战力主体（权重全路最高）");

        // SituationalTags = 属性/形态 tag，绝不等于任何已知 pathId（R2）；非空。
        Assert.NotEmpty(d.SituationalTags);
        Assert.DoesNotContain(d.SituationalTags, t => PathValidator.KnownPathIds.Contains(t));

        // Curve 四列等长（M4）。
        var c2 = d.Curve;
        Assert.Equal(c2.RealmMultipliers.Count, c2.UnifiedTierOf.Count);
        Assert.Equal(c2.RealmMultipliers.Count, c2.RealmNames.Count);
        Assert.Equal(c2.RealmMultipliers.Count, c2.RealmThresholds.Count);
        // A.1 §2：SubLevelCount 前缀和闭合（Σ == flatIndex 数）。
        Assert.Equal(c2.RealmMultipliers.Count, c2.SubLevelCount.Sum());

        // 曲线倍率严格递增（厚积/枢纽型也需随境界单调抬升 → PowerEngine×Curve 随 realm 升不回落）。
        for (int i = 1; i < c2.RealmMultipliers.Count; i++)
            Assert.True(c2.RealmMultipliers[i] > c2.RealmMultipliers[i - 1],
                $"RealmMultipliers 在 realm {i} 未严格递增");
    }
}
