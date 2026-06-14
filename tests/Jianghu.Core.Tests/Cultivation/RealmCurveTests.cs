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
}
