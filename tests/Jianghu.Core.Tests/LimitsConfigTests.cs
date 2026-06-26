using System;
using Jianghu.Config;
using Xunit;

public class LimitsConfigTests
{
    [Fact]
    public void Default_is_feasible() => LimitsConfig.Default.Validate(); // 不抛

    [Fact]
    public void Infeasible_when_cap_times_count_below_sum()
    {
        var bad = LimitsConfig.Default with { StatCap = 10, StatSum = 80, StatCount = 4 }; // 4*10<80
        Assert.Throws<InvalidOperationException>(() => bad.Validate());
    }

    [Fact]
    public void Infeasible_when_min_sum_exceeds_sum()
    {
        var bad = LimitsConfig.Default with { StatMin = 25, StatSum = 80, StatCount = 4 }; // 4*25>80
        Assert.Throws<InvalidOperationException>(() => bad.Validate());
    }

    // —— drama-006 D6.1/D6.2/D6.3：戏剧上限字段 + Validate 越界断言 ——

    [Fact]
    public void Drama_defaults_feasible() // D6.3：默认全可行（含戏剧字段）
        => LimitsConfig.Default.Validate(); // 不抛

    [Fact]
    public void Drama_MaxConcurrentArcs_zero_is_legal() // D6.2：0 = no-op 开关，合法
        => (LimitsConfig.Default with { MaxConcurrentArcs = 0 }).Validate(); // 不抛

    [Fact]
    public void Drama_MaxConcurrentArcs_negative_throws()
    {
        var bad = LimitsConfig.Default with { MaxConcurrentArcs = -1 };
        Assert.Throws<InvalidOperationException>(() => bad.Validate());
    }

    [Fact]
    public void Drama_IgniteThreshold_above_cap_throws() // 须可达 [1,GrudgeCap]
    {
        var bad = LimitsConfig.Default with { GrudgeIgniteThreshold = 101, GrudgeCap = 100 };
        Assert.Throws<InvalidOperationException>(() => bad.Validate());
    }

    [Fact]
    public void Drama_IgniteThreshold_zero_throws()
    {
        var bad = LimitsConfig.Default with { GrudgeIgniteThreshold = 0 };
        Assert.Throws<InvalidOperationException>(() => bad.Validate());
    }

    [Fact]
    public void Drama_EscapeRatioPct_zero_throws() // §7 显式 [1,100]
    {
        var bad = LimitsConfig.Default with { EscapeRatioPct = 0 };
        Assert.Throws<InvalidOperationException>(() => bad.Validate());
    }

    [Fact]
    public void Drama_EscapeRatioPct_over_100_throws()
    {
        var bad = LimitsConfig.Default with { EscapeRatioPct = 101 };
        Assert.Throws<InvalidOperationException>(() => bad.Validate());
    }

    [Fact]
    public void Drama_InheritDecayPct_over_100_throws() // ≤100 保继承单调不增
    {
        var bad = LimitsConfig.Default with { InheritDecayPct = 101 };
        Assert.Throws<InvalidOperationException>(() => bad.Validate());
    }

    [Fact]
    public void Drama_InheritDecayPct_zero_is_legal() // 0 衰减=继承即灭，合法
        => (LimitsConfig.Default with { InheritDecayPct = 0 }).Validate(); // 不抛

    [Fact]
    public void Drama_DramaBudget_zero_throws() // §7 显式 ≥1
    {
        var bad = LimitsConfig.Default with { DramaBudget = 0 };
        Assert.Throws<InvalidOperationException>(() => bad.Validate());
    }

    [Fact]
    public void Drama_MaxArcsPerCharacter_zero_throws()
    {
        var bad = LimitsConfig.Default with { MaxArcsPerCharacter = 0 };
        Assert.Throws<InvalidOperationException>(() => bad.Validate());
    }

    [Fact]
    public void Drama_MaxGeneration_zero_throws()
    {
        var bad = LimitsConfig.Default with { MaxGeneration = 0 };
        Assert.Throws<InvalidOperationException>(() => bad.Validate());
    }

    [Fact]
    public void Drama_RelationMirrorCap_over_100_throws()
    {
        var bad = LimitsConfig.Default with { RelationMirrorCap = 101 };
        Assert.Throws<InvalidOperationException>(() => bad.Validate());
    }

    [Fact]
    public void Drama_MaxArcWeightSum_zero_throws()
    {
        var bad = LimitsConfig.Default with { MaxArcWeightSum = 0 };
        Assert.Throws<InvalidOperationException>(() => bad.Validate());
    }
}
