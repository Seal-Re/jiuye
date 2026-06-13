using Jianghu.Config;
using Jianghu.Sim;
using Xunit;

public class LifecycleTests
{
    [Fact]
    public void Population_replenishes_when_below_low_band()
    {
        var limits = LimitsConfig.Default with { PopulationLow = 5, LifespanMin = 30, LifespanMax = 40 };
        var w = WorldFactory.CreateInitial(seed: 3, limits, initialCount: 5);
        for (int i = 0; i < 300; i++) w.Advance(budget: 8);
        Assert.True(w.Deceased.Count > 0, "应有角色寿尽");
        Assert.True(w.AliveCount >= 1, "应被涌现补充，不灭绝");
    }
}
