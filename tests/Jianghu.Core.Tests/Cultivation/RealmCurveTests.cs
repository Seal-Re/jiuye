using System;
using System.Collections.Generic;
using System.Linq;
using Jianghu.Cultivation;
using Xunit;

public class RealmCurveTests
{
    static RealmCurveDef Curve3() => new RealmCurveDef(
        new[] { 10, 15, 25 }, new[] { 0, 1, 2 },
        new[] { "凝气", "小成", "大成" }, new[] { 0, 100, 300 },
        new[] { 1, 1, 1 }, true, 2);

    // A1.0a：RealmCurveDef 加三列（SubLevelCount / CanAscend / MaxMajor，境界稿 §2）。
    [Fact]
    public void RealmCurveDef_HasSubLevelCount_CanAscend_MaxMajor()
    {
        var c = new RealmCurveDef(
            new[] { 10, 15, 25 }, new[] { 0, 2, 4 },
            new[] { "炼气", "筑基", "金丹" }, new[] { 0, 100, 300 },
            SubLevelCount: new[] { 1, 1, 1 }, CanAscend: true, MaxMajor: 2);
        Assert.Equal(3, c.SubLevelCount.Count);
        Assert.True(c.CanAscend);
        Assert.Equal(2, c.MaxMajor);
    }

    // A1.0b：21 路新三列与 flatIndex 一致（起步 SubLevelCount 全 1：Σ==flatIndex 数、
    // 全 ≥1、CanAscend 全 true（21 修真路；武夫 defer）、MaxMajor==大境界数-1）。
    [Fact]
    public void AllPaths_HaveConsistentSubLevelData()
    {
        foreach (var p in new CodePathSource().Load())
        {
            var c = p.Curve;
            Assert.Equal(c.RealmMultipliers.Count, c.SubLevelCount.Sum()); // 前缀和闭合
            Assert.True(c.SubLevelCount.All(s => s >= 1));
            Assert.True(c.CanAscend);                                       // 21 修真路全 true
            Assert.Equal(c.SubLevelCount.Count - 1, c.MaxMajor);
        }
    }

    [Fact]
    public void Breakthrough_AtThreshold_CapsAtMax()
    {
        var c = Curve3();
        Assert.Equal(1, RealmCurve.NextIndexIfReady(0, 120, c));   // ≥ threshold[1]=100 → 升
        Assert.Equal(0, RealmCurve.NextIndexIfReady(0, 50, c));    // 未达 → 维持
        Assert.Equal(2, RealmCurve.NextIndexIfReady(2, 99999, c)); // 已到顶 → 封顶不越界
    }

    [Fact]
    public void Validate_AcceptsEqualLengths()
    {
        RealmCurve.Validate(Curve3()); // 不抛即过
    }

    [Fact]
    public void Validate_RejectsLengthMismatch() // backlog2-M4：因果路原案 8 mult vs 7 UT
    {
        var bad = new RealmCurveDef(
            new[] { 6, 7, 9, 12, 18, 60, 110, 200 },        // 8
            new[] { 0, 2, 4, 8, 10, 11, 12 },                // 7  ← 不匹配
            new[] { "a", "b", "c", "d", "e", "f", "g", "h" }, // 8
            new[] { 0, 1, 2, 3, 4, 5, 6, 7 },               // 8
            new[] { 1, 1, 1, 1, 1, 1, 1, 1 }, true, 7);     // 新三列（仍因四列不等长被拒）
        Assert.Throws<InvalidOperationException>(() => RealmCurve.Validate(bad));
    }

    // —— A1.1：Validate 多列校验辅助（默认 3 段合法 curve，可单点改坏某列）。
    //    maxMajor 默认 = sub.Length-1（隔离 Σ/≥1 测试，不连带触发 Count 校验）。——
    static RealmCurveDef Curve(
        int[]? mult = null, int[]? ut = null, string[]? names = null, int[]? thr = null,
        int[]? sub = null, bool canAscend = true, int? maxMajor = null)
    {
        mult ??= new[] { 10, 15, 25 };
        ut ??= new[] { 0, 2, 4 };
        names ??= new[] { "炼气", "筑基", "金丹" };
        thr ??= new[] { 0, 100, 300 };
        sub ??= new[] { 1, 1, 1 };
        return new RealmCurveDef(mult, ut, names, thr, sub, canAscend, maxMajor ?? sub.Length - 1);
    }

    // Σ SubLevelCount ≠ flatIndex 数 → 抛（前缀和闭合，境界稿 §2）。
    [Fact]
    public void Validate_RejectsSubLevelSumMismatch()
    {
        var bad = Curve(sub: new[] { 1, 1 }); // Σ=2 ≠ 3 段
        Assert.Throws<InvalidOperationException>(() => RealmCurve.Validate(bad));
    }

    // 某 SubLevelCount[i] < 1 → 抛（每大境界至少 1 小境界，境界稿 §2）。
    [Fact]
    public void Validate_RejectsSubLevelCountBelowOne()
    {
        var bad = Curve(sub: new[] { 1, 0, 2 }); // 含 0
        Assert.Throws<InvalidOperationException>(() => RealmCurve.Validate(bad));
    }

    // SubLevelCount.Count ≠ MaxMajor+1 → 抛（大境界数与 MaxMajor 必须对齐，境界稿 §2）。
    [Fact]
    public void Validate_RejectsMaxMajorMismatch()
    {
        var bad = Curve(sub: new[] { 1, 1, 1 }, maxMajor: 5); // Count=3 ≠ MaxMajor+1=6
        Assert.Throws<InvalidOperationException>(() => RealmCurve.Validate(bad));
    }

    // CanAscend=false（武夫）但 UnifiedTierOf.Last() > 9 → 抛（武夫陆地神仙 UT9 封顶，境界稿 §11.2）。
    [Fact]
    public void Validate_RejectsWuxiaTopAboveUT9()
    {
        var bad = Curve(ut: new[] { 0, 2, 12 }, canAscend: false); // 武夫顶 12 > 9
        Assert.Throws<InvalidOperationException>(() => RealmCurve.Validate(bad));
    }

    // UnifiedTierOf 降序 → 抛（UT 随境界非降，境界稿 §2）。
    [Fact]
    public void Validate_RejectsNonMonotonicUT()
    {
        var bad = Curve(ut: new[] { 0, 4, 2 }); // 降序
        Assert.Throws<InvalidOperationException>(() => RealmCurve.Validate(bad));
    }

    // 反向守门：武夫顶恰为 UT9（CanAscend=false）合法，不抛。
    [Fact]
    public void Validate_AcceptsWuxiaTopAtUT9()
    {
        RealmCurve.Validate(Curve(ut: new[] { 0, 2, 9 }, canAscend: false)); // 不抛即过
    }
}
