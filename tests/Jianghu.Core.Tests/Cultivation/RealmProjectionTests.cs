using System.Collections.Generic;
using System.Linq;
using Jianghu.Cultivation;
using Xunit;

/// <summary>
/// A1.2：前缀和投影 RealmProjection（INV-REALM-1 可逆）+ 4 偏离路 UT 迁移到锚集
/// {0,2,4,6,8,9,10,11,12}（境界稿 §6 投影 / §11.1 迁移表 / §11.3 佛 plateau）。
/// 纯整数，禁浮点。
/// </summary>
public class RealmProjectionTests
{
    static readonly HashSet<int> Anchor = new HashSet<int> { 0, 2, 4, 6, 8, 9, 10, 11, 12 };

    // —— INV-REALM-1：每 flatIndex Decode∘Encode == id（含佛 plateau 形 1,1,..,2）。——
    [Fact]
    public void Projection_EncodeDecodeRoundTrips()
    {
        var sub = new[] { 1, 1, 1, 1, 1, 1, 1, 1, 2 }; // 佛修 plateau：major8 含 2 小境界，Σ=10
        for (int fi = 0; fi < sub.Sum(); fi++)
        {
            var (m, s) = RealmProjection.Decode(fi, sub);
            Assert.Equal(fi, RealmProjection.Encode(m, s, sub));
        }
    }

    // —— INV-REALM-1：跨多种 SubLevelCount 形（全 1 / 段长 2 / 末 plateau）Decode∘Encode == id。——
    [Theory]
    [InlineData(new[] { 1, 1, 1 })]
    [InlineData(new[] { 2, 1, 1, 1, 1, 1, 1, 1, 1 })]      // 体/命：低段 1 个 2 长段
    [InlineData(new[] { 2, 2, 1, 1, 1, 1, 1, 1 })]          // 符：2 个 2 长段
    [InlineData(new[] { 1, 1, 1, 1, 1, 1, 1, 1, 2 })]      // 佛 plateau
    public void Projection_RoundTrips_AcrossSubLevelShapes(int[] sub)
    {
        int total = sub.Sum();
        for (int fi = 0; fi < total; fi++)
        {
            var (m, s) = RealmProjection.Decode(fi, sub);
            Assert.True(s >= 0 && s < sub[m]);             // 小境界落本大境界内
            Assert.Equal(fi, RealmProjection.Encode(m, s, sub));
        }
    }

    // —— 佛 plateau：顶大境界 major8 含 sub0(fi8)/sub1(fi9) 同段，Decode 归同 major（§11.3）。——
    [Fact]
    public void Projection_BuddhistPlateau_TwoSubsShareTopMajor()
    {
        var sub = new[] { 1, 1, 1, 1, 1, 1, 1, 1, 2 };
        Assert.Equal((8, 0), RealmProjection.Decode(8, sub)); // 金身
        Assert.Equal((8, 1), RealmProjection.Decode(9, sub)); // 不坏金身（同大境界 major8）
        Assert.Equal(8, RealmProjection.Encode(8, 0, sub));
        Assert.Equal(9, RealmProjection.Encode(8, 1, sub));
    }

    // —— 4 偏离路迁移后 UnifiedTierOf 全落锚集 {0,2,4,6,8,9,10,11,12}。——
    [Fact]
    public void DeviantPaths_MigratedToAnchorSet()
    {
        foreach (var id in new[] { "ti_xiu_hengshi", "ming_fate_causality", "fu_xiu_fulu", "buddhist_golden_body" })
            foreach (var ut in PathById(id).Curve.UnifiedTierOf)
                Assert.Contains(ut, Anchor);
    }

    // —— 全 21 路 SubLevelCount[m] == 第 m 个同 UT 连续段长（大境界=同 UT 极大连续段，境界稿 §1/§6）。——
    [Fact]
    public void AllPaths_SubLevelCountEqualsUTSegmentLengths()
    {
        foreach (var p in new CodePathSource().Load())
        {
            var segs = UtSegmentLengths(p.Curve.UnifiedTierOf);
            Assert.True(segs.SequenceEqual(p.Curve.SubLevelCount),
                $"{p.PathId}: SubLevelCount={Join(p.Curve.SubLevelCount)} ≠ UT 段长={Join(segs)}");
            Assert.Equal(segs.Count - 1, p.Curve.MaxMajor);
        }
    }

    // —— 4 偏离路顶值终值断言（迁移目标确认，对抗回归）。——
    [Fact]
    public void DeviantPaths_TopUTReachesTarget()
    {
        Assert.Equal(12, PathById("ti_xiu_hengshi").Curve.UnifiedTierOf.Last());
        Assert.Equal(12, PathById("ming_fate_causality").Curve.UnifiedTierOf.Last());
        Assert.Equal(12, PathById("fu_xiu_fulu").Curve.UnifiedTierOf.Last());          // 符顶补到 UT12
        Assert.Equal(12, PathById("buddhist_golden_body").Curve.UnifiedTierOf.Last());
    }

    // —— 同 UT 极大连续段长序列（大境界划分基准）。——
    static List<int> UtSegmentLengths(IReadOnlyList<int> ut)
    {
        var segs = new List<int>();
        int i = 0;
        while (i < ut.Count)
        {
            int j = i + 1;
            while (j < ut.Count && ut[j] == ut[i]) j++;
            segs.Add(j - i);
            i = j;
        }
        return segs;
    }

    static string Join(IReadOnlyList<int> xs) => "[" + string.Join(",", xs) + "]";

    static CultivationPathDef PathById(string id) =>
        new CodePathSource().Load().First(p => p.PathId == id);
}
