using System.Collections.Generic;
using System.Linq;
using Jianghu.Cultivation;
using Xunit;

// A1.2 迁移验证：4 偏离路 UT 迁移到锚集 + 全 21 路 SubLevelCount == UnifiedTierOf 同 UT 段长。
public class RealmMigrationTests
{
    static readonly HashSet<int> Anchor = new HashSet<int> { 0, 2, 4, 6, 8, 9, 10, 11, 12 };

    static IReadOnlyList<CultivationPathDef> All() => new CodePathSource().Load();

    [Fact]
    public void AllPaths_UnifiedTierOf_InAnchorSet()
    {
        foreach (var p in All())
            foreach (var ut in p.Curve.UnifiedTierOf)
                Assert.True(Anchor.Contains(ut), $"{p.PathId} UnifiedTierOf 含锚集外 UT {ut}");
    }

    [Fact]
    public void AllPaths_SubLevelCount_MatchesUTRunLengths()
    {
        foreach (var p in All())
        {
            var runs = UTRunLengths(p.Curve.UnifiedTierOf);
            Assert.True(runs.SequenceEqual(p.Curve.SubLevelCount),
                $"{p.PathId} SubLevelCount [{string.Join(",", p.Curve.SubLevelCount)}] != UT 段长 [{string.Join(",", runs)}]");
        }
    }

    [Fact]
    public void DeviantPath_FuXiu_ReachesUT12()
    {
        var fu = All().First(p => p.PathId == "fu_xiu_fulu");
        Assert.Equal(12, fu.Curve.UnifiedTierOf.Max());
    }

    static int[] UTRunLengths(IReadOnlyList<int> ut)
    {
        var runs = new List<int>();
        int i = 0;
        while (i < ut.Count)
        {
            int j = i;
            while (j < ut.Count && ut[j] == ut[i]) j++;
            runs.Add(j - i);
            i = j;
        }
        return runs.ToArray();
    }
}
