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

    [Fact]
    public void Deceased_character_id_still_resolves_name() // R-F5
    {
        var limits = LimitsConfig.Default with { LifespanMin = 30, LifespanMax = 40 };
        var w = WorldFactory.CreateInitial(seed: 5, limits, initialCount: 5);
        for (int i = 0; i < 200; i++) w.Advance(8);
        Assert.True(w.Deceased.Count > 0);
        var dead = w.Deceased[0];
        Assert.Contains("(故)", w.NameOf(dead.Id)); // 死后 CharacterId 仍稳定可解析
    }
}
