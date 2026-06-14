using System.Linq;
using Jianghu.Cultivation;
using Xunit;

/// <summary>
/// 音修 yin_xiu_yuedao **standalone** 数据自检（只验 Def 本身，不依赖注册/生成/切磋）。
/// 全局命名空间（与端到端 SwordImmortalTests 等的 Jianghu.Core.Tests.Cultivation.Paths 命名空间隔离，
/// 避免类名/helper 撞名）。音修是 spirit 范围软控 + 团队 BUFF 支援枢纽路：
/// ① Def 必过 PathValidator 全部校验（R2/R3/R4/R6/M1/M4）；
/// ② canon pathId + ≥3 战斗类目 + daoheart 占位类目 + ≥5 战技 + Curve 四列等长 + terms 无 ×0 无 daoHeart。
/// </summary>
public class YinXiuYuedaoStandaloneTests
{
    // ① 数据质量 gate：音修 Def 必过 PathValidator 全部校验（不抛即过）。
    [Fact]
    public void Def_PassesPathValidator()
    {
        PathValidator.AssertValid(YinXiuYuedaoPath.Def); // 不抛即过
    }

    // ② canon pathId + spirit AttackDimension + 含 daoheart 占位类目（yueheart=乐心道）+ 形状约束
    //    （≥3 类目、每类 ≥4 功法、≥5 战技、Curve 四列等长、terms 无 ×0 无 daoHeart）+ 属性 tag 非 PathId。
    [Fact]
    public void Def_IsCanonAndShaped()
    {
        var d = YinXiuYuedaoPath.Def;

        // canon pathId（R4）+ spirit 攻击维。
        Assert.Equal("yin_xiu_yuedao", d.PathId);
        Assert.Equal("spirit", d.AttackDimension);

        // 含 1 个 Role=daoheart 占位类目（M1）；≥3 类目，每类 Arts ≥4。
        Assert.Contains(d.ArtCategories, c => c.Role == "daoheart");
        Assert.True(d.ArtCategories.Count >= 3, $"ArtCategories={d.ArtCategories.Count} <3");
        Assert.All(d.ArtCategories, c => Assert.True(c.Arts.Count >= 4, $"类目 '{c.Name}' Arts={c.Arts.Count} <4"));

        // daoheart 占位类目 A.0 仅装载不结算：tier=0（sumArtPower 贡献 0）+ Effects 留空（不触 daoHeart 资源算子）。
        var daoheart = d.ArtCategories.Single(c => c.Role == "daoheart");
        Assert.All(daoheart.Arts, a => Assert.Equal(0, a.Tier));
        Assert.All(daoheart.Arts, a => Assert.Empty(a.Effects));

        // ≥5 具名战技。
        Assert.True(d.CombatSkills.Count >= 5, $"CombatSkills={d.CombatSkills.Count} <5");

        // PowerFormula.terms 无 ×0（R6）、无 daoHeart/innerDemon（R3）。
        Assert.All(d.Power.Terms, t => Assert.NotEqual(0, t.Weight));
        Assert.DoesNotContain(d.Power.Terms, t => t.Src.Contains("daoHeart") || t.Src.Contains("innerDemon"));

        // 本路双主权 + 律场二元项确在 terms（悟性/内力/resonance/qiYun），佐证非空壳照搬。
        Assert.Contains(d.Power.Terms, t => t.Src == "stat:Insight");
        Assert.Contains(d.Power.Terms, t => t.Src == "stat:Internal");
        Assert.Contains(d.Power.Terms, t => t.Src == "res:resonance");
        Assert.Contains(d.Power.Terms, t => t.Src == "res:qiYun");

        // RealmCurve 四列等长（M4）。
        Assert.Equal(d.Curve.RealmMultipliers.Count, d.Curve.UnifiedTierOf.Count);
        Assert.Equal(d.Curve.RealmMultipliers.Count, d.Curve.RealmNames.Count);
        Assert.Equal(d.Curve.RealmMultipliers.Count, d.Curve.RealmThresholds.Count);

        // SituationalTags = 属性/形态 tag（非对手 PathId，R2：不得等于任一已知 canon pathId）。
        Assert.Equal(new[] { "spirit_attack", "ranged", "righteous" }, d.SituationalTags);
        Assert.All(d.SituationalTags, tag => Assert.DoesNotContain(tag, PathValidator.KnownPathIds));

        // EntryGate 用本路唯一 entry tag（yin_root）。
        Assert.Equal("tag:yin_root", d.EntryGate.Pred);
    }
}
