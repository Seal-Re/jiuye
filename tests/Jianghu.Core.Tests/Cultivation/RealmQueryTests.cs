using System.Collections.Generic;
using System.Linq;
using Jianghu.Cultivation;
using Xunit;

// A1.5 三轴查询 RealmQuery.Describe：单 flatIndex 取齐 大/小/UT/名。
// 验：① 单小境界路全名=裸名；② 多小境界段首无冗余「·」；③ 非段首=「大·小」；④ 全 21 路 UT 与曲线/投影自洽。
public class RealmQueryTests
{
    static IReadOnlyList<CultivationPathDef> All() => new CodePathSource().Load();
    static RealmCurveDef Curve(string uniqueRealmName) =>
        All().First(p => p.Curve.RealmNames.Contains(uniqueRealmName)).Curve;

    [Fact]
    public void SingleSubMajor_FullNameIsBareName()
    {
        var jian = Curve("剑仙");                 // 剑修，全程 SubLevelCount=1
        var info = RealmQuery.Describe(jian, 8);   // fi8 = 剑仙
        Assert.Equal(1, info.SubCount);
        Assert.Equal(0, info.Sub);
        Assert.Equal(12, info.UnifiedTier);
        Assert.Equal("剑仙", info.FullName);
        Assert.Equal("剑仙（UT12）", info.Display);
    }

    [Fact]
    public void MultiSubMajor_Head_NoRedundantDot()
    {
        var head = RealmQuery.Describe(Curve("锻皮"), 0);  // 体修 UT0 段={锻皮,淬肉}，fi0=段首
        Assert.Equal(0, head.Major);
        Assert.Equal(0, head.Sub);
        Assert.Equal(2, head.SubCount);
        Assert.Equal(0, head.UnifiedTier);
        Assert.Equal("锻皮", head.MajorName);
        Assert.Equal("锻皮", head.SubName);
        Assert.Equal("锻皮", head.FullName);              // 段首：大名==小名 → 无冗余「·」
    }

    [Fact]
    public void MultiSubMajor_NonHead_DotJoined()
    {
        var sub = RealmQuery.Describe(Curve("锻皮"), 1);   // fi1=淬肉（体 UT0 段 sub1）
        Assert.Equal(0, sub.Major);
        Assert.Equal(1, sub.Sub);
        Assert.Equal(0, sub.UnifiedTier);
        Assert.Equal("锻皮·淬肉", sub.FullName);           // 大境界·小境界

        var fo = RealmQuery.Describe(Curve("不坏金身"), 9); // 佛 plateau：金身大境界 sub1=不坏金身
        Assert.Equal(12, fo.UnifiedTier);
        Assert.Equal("金身·不坏金身", fo.FullName);
    }

    [Fact]
    public void AllPaths_DescribeUT_MatchesCurveAndProjection()
    {
        foreach (var p in All())
        {
            var c = p.Curve;
            for (int fi = 0; fi < c.RealmNames.Count; fi++)
            {
                var info = RealmQuery.Describe(c, fi);
                Assert.Equal(c.UnifiedTierOf[fi], info.UnifiedTier);                              // UT 与曲线一致
                Assert.Equal(fi, RealmProjection.Encode(info.Major, info.Sub, c.SubLevelCount));  // 投影自洽
                Assert.False(string.IsNullOrEmpty(info.FullName), $"{p.PathId} fi{fi} 全名空");
            }
        }
    }
}
