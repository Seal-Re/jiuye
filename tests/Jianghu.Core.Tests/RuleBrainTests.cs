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

    [Fact]
    public async System.Threading.Tasks.Task No_neighbor_still_varies_actions()
    {
        var brain = new RuleBrain(new Pcg32(2, 1), ArchetypeKind.Martial);
        var ctx = Ctx(GoalKind.Advance, ArchetypeKind.Martial, new List<NearbyActor>(),
            new List<ActionType> { ActionType.Train, ActionType.Travel, ActionType.Spar });
        var seen = new HashSet<ActionType>();
        for (int i = 0; i < 12; i++) seen.Add((await brain.DecideAsync(ctx, CancellationToken.None)).Type);
        Assert.True(seen.Count >= 2, "独行者也应在 Train/Travel 间交替，不单一刷屏");
    }

    // —— balance-004：单元级——势均对手仍可切磋（不误伤 16dc54c 势均选择）。
    // 注：碾压抑制的真实效果在 sim 级验证（SparStompRateTests，动态 goal/streak），
    // 单元级固定 Advance goal 下 Train 本就压制 Spar，无法复现刷屏。
    private static DecisionContext CtxWithPower(int selfPower, List<NearbyActor> nearby)
        => new DecisionContext(new CharacterId(1), new StatBlock(new[] { 20, 20, 20, 20 }),
            new Goal(GoalKind.Advance, 0), new NodeId(0), nearby,
            new List<ActionType> { ActionType.Train, ActionType.Travel, ActionType.Spar },
            new List<MemoryEntry>(), SelfPower: selfPower);

    [Fact]
    public async System.Threading.Tasks.Task Even_rival_still_sparred()
    {
        var brain = new RuleBrain(new Pcg32(7, 1), ArchetypeKind.Martial);
        // 势均对手 PE=210 vs 自身 200（gap=10）→ 切磋意愿应保留（不误伤势均选择）。
        // 用 Wander goal（Train 权重降）让 Spar 有机会胜出，验势均对手可被切磋。
        var ctx = new DecisionContext(new CharacterId(1), new StatBlock(new[] { 20, 20, 20, 20 }),
            new Goal(GoalKind.Wander, 0), new NodeId(0),
            new List<NearbyActor> { new NearbyActor(new CharacterId(2), 210, 0) },
            new List<ActionType> { ActionType.Spar }, new List<MemoryEntry>(), SelfPower: 200);
        var choice = await brain.DecideAsync(ctx, CancellationToken.None);
        Assert.Equal(ActionType.Spar, choice.Type); // 仅 Spar 可选且势均 → 应选 Spar（非 long.MinValue 弃权）
    }
}
