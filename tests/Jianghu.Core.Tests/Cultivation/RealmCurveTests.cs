using System;
using System.Collections.Generic;
using Jianghu.Cultivation;
using Xunit;

public class RealmCurveTests
{
    static RealmCurveDef Curve3() => new RealmCurveDef(
        new[] { 10, 15, 25 }, new[] { 0, 1, 2 },
        new[] { "凝气", "小成", "大成" }, new[] { 0, 100, 300 });

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
            new[] { 0, 1, 2, 3, 4, 5, 6, 7 });               // 8
        Assert.Throws<InvalidOperationException>(() => RealmCurve.Validate(bad));
    }
}
