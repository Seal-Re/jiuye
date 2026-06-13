using Jianghu.Config;
using Jianghu.Stats;
using Xunit;

public class StatBlockTests
{
    [Fact]
    public void Apply_clamps_to_cap_and_floor_zero()
    {
        var c = LimitsConfig.Default; // cap 30
        var s = new StatBlock(new[] { 29, 10, 10, 10 });
        s.Apply(StatKind.Force, +5, c);
        Assert.Equal(30, s.Get(StatKind.Force));   // 越 cap 被钳
        s.Apply(StatKind.Internal, -100, c);
        Assert.Equal(0, s.Get(StatKind.Internal)); // 不为负
    }

    [Fact]
    public void Sum_reflects_values()
    {
        var s = new StatBlock(new[] { 20, 20, 20, 20 });
        Assert.Equal(80, s.Sum);
    }
}
