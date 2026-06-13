using System.Collections.Generic;
using System.Threading;
using Jianghu.Actions;
using Jianghu.Decide;
using Jianghu.Model;
using Jianghu.Random;
using Jianghu.Stats;
using Xunit;

public class RuleBrainTests
{
    private static DecisionContext Ctx(GoalKind goal, ArchetypeKind arch, List<NearbyActor> nearby, List<ActionType> allowed)
        => new DecisionContext(new CharacterId(1), new StatBlock(new[] { 10, 10, 10, 20 }),
            new Goal(goal, 0), new NodeId(0), nearby, allowed, new List<MemoryEntry>());

    [Fact]
    public async System.Threading.Tasks.Task Advance_goal_with_no_neighbors_prefers_train()
    {
        var brain = new RuleBrain(new Pcg32(1, 1), ArchetypeKind.Martial);
        var ctx = Ctx(GoalKind.Advance, ArchetypeKind.Martial, new List<NearbyActor>(),
            new List<ActionType> { ActionType.Train, ActionType.Travel, ActionType.Spar });
        var choice = await brain.DecideAsync(ctx, CancellationToken.None);
        Assert.Equal(ActionType.Train, choice.Type);
    }

    [Fact]
    public async System.Threading.Tasks.Task Repeat_decay_breaks_self_similarity()
    {
        var brain = new RuleBrain(new Pcg32(1, 1), ArchetypeKind.Martial);
        var ctx = Ctx(GoalKind.Advance, ArchetypeKind.Martial,
            new List<NearbyActor> { new NearbyActor(new CharacterId(2), 40, 0) },
            new List<ActionType> { ActionType.Train, ActionType.Travel, ActionType.Spar });
        var seen = new HashSet<ActionType>();
        for (int i = 0; i < 8; i++) seen.Add((await brain.DecideAsync(ctx, CancellationToken.None)).Type);
        Assert.True(seen.Count >= 2, "重复衰减应打破单一动作");
    }
}
