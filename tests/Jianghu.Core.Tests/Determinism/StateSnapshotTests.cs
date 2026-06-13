using Jianghu.Config;
using Jianghu.Sim;
using Xunit;

public class StateSnapshotTests
{
    private static void Run(World w, int steps, int budget)
    {
        for (int i = 0; i < steps; i++) w.Advance(budget);
    }

    [Fact]
    public void Snapshot_OfCloneEqualsOriginal_AndContinuesIdentically()
    {
        var w = WorldFactory.CreateInitial(seed: 12345, LimitsConfig.Default, initialCount: 5);
        Run(w, steps: 40, budget: 6); // 纯 v1.0 跑 N 步

        var a = StateSnapshot.Capture(w);
        var c = w.Clone();
        Assert.Equal(a, StateSnapshot.Capture(c)); // 续跑前：Clone 在快照口径下逐字节一致

        Run(w, steps: 40, budget: 6);
        Run(c, steps: 40, budget: 6);
        Assert.Equal(StateSnapshot.Capture(w), StateSnapshot.Capture(c)); // 续跑后仍一致
    }

    [Fact]
    public void Snapshot_IsStableAcrossRepeatedRuns()
    {
        var w1 = WorldFactory.CreateInitial(seed: 999, LimitsConfig.Default, initialCount: 5);
        var w2 = WorldFactory.CreateInitial(seed: 999, LimitsConfig.Default, initialCount: 5);
        Run(w1, steps: 60, budget: 6);
        Run(w2, steps: 60, budget: 6);
        Assert.Equal(StateSnapshot.Capture(w1), StateSnapshot.Capture(w2));
    }
}
