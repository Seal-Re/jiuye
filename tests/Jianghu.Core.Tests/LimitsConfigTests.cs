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
}
