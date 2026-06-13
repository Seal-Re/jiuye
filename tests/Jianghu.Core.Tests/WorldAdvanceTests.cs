using Jianghu.Config;
using Jianghu.Sim;
using Xunit;

public class WorldAdvanceTests
{
    [Fact]
    public void Advance_respects_budget_and_progresses()
    {
        var w = WorldFactory.CreateInitial(seed: 1, LimitsConfig.Default, initialCount: 5);
        long c0 = w.Clock;
        w.Advance(budget: 3);
        Assert.True(w.Clock >= c0);
        Assert.True(w.Chronicle.Count > 0);
    }

    [Fact]
    public void Long_run_does_not_freeze_and_keeps_population_band()
    {
        var w = WorldFactory.CreateInitial(seed: 2, LimitsConfig.Default, initialCount: 5);
        for (int i = 0; i < 500; i++) w.Advance(budget: 8);
        Assert.InRange(w.AliveCount, 1, LimitsConfig.Default.PopulationHigh + 5);
        Assert.True(w.Chronicle.Count > 50);
    }
}
