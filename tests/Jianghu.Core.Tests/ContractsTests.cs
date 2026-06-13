using System.Collections.Generic;
using Jianghu.Actions;
using Jianghu.Decide;
using Jianghu.Model;
using Jianghu.Stats;
using Xunit;

public class ContractsTests
{
    [Fact]
    public void DecisionContext_is_immutable_snapshot()
    {
        var ctx = new DecisionContext(
            new CharacterId(1), new StatBlock(new[] { 20, 20, 20, 20 }),
            new Goal(GoalKind.Advance, 0), new NodeId(0),
            new List<NearbyActor>(), new List<ActionType> { ActionType.Train },
            new List<MemoryEntry>());
        Assert.Equal(ActionType.Train, ctx.Allowed[0]);
    }

    [Fact]
    public void ActionChoice_carries_typed_payload()
    {
        ActionChoice c = new TrainChoice(StatKind.Force);
        Assert.Equal(ActionType.Train, c.Type);
        Assert.Equal(StatKind.Force, ((TrainChoice)c).Stat);
    }
}
