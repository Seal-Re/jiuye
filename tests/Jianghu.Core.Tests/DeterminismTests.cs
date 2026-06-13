using System.Linq;
using Jianghu.Config;
using Jianghu.Sim;
using Xunit;

public class DeterminismTests
{
    private static string RunChronicle(ulong seed, int steps, int budget)
    {
        var w = WorldFactory.CreateInitial(seed, LimitsConfig.Default, 5);
        for (int i = 0; i < steps; i++) w.Advance(budget);
        return string.Join("\n", w.Chronicle.Lines);
    }

    [Fact]
    public void Same_seed_same_chronicle() // R-NF1
        => Assert.Equal(RunChronicle(2026, 200, 6), RunChronicle(2026, 200, 6));

    [Fact]
    public void Snapshot_continue_equals_uninterrupted() // R-NF2
    {
        var full = WorldFactory.CreateInitial(7, LimitsConfig.Default, 5);
        for (int i = 0; i < 120; i++) full.Advance(6);
        var fullText = string.Join("\n", full.Chronicle.Lines);

        var part = WorldFactory.CreateInitial(7, LimitsConfig.Default, 5);
        for (int i = 0; i < 60; i++) part.Advance(6);
        var clone = part.Clone();              // 快照
        for (int i = 0; i < 60; i++) clone.Advance(6);
        var cloneText = string.Join("\n", clone.Chronicle.Lines);

        Assert.Equal(fullText, cloneText);
    }
}
