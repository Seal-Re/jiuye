using System.Collections.Generic;
using System.Linq;
using Jianghu.Actions;
using Jianghu.Config;
using Jianghu.Events;
using Jianghu.Model;
using Jianghu.Stats;
using Xunit;

public class ActionsTests
{
    private sealed class FakeWorld : IWorldMutator
    {
        public long Clock { get; set; } = 5;
        public List<Character> All { get; } = new List<Character>();
        public Relations Rel { get; } = new Relations();
        public LimitsConfig Limits { get; } = LimitsConfig.Default;
        public int NodeCount => 3;
        public IReadOnlyList<Character> AtNode(NodeId n) => All.Where(c => c.Node.Value == n.Value && c.Alive).ToList();
        public void ApplyStat(Character c, StatKind k, int d) => c.Stats.Apply(k, d, Limits);
        public int AdjustRelation(CharacterId f, CharacterId t, int d) => Rel.Adjust(f, t, d);
        public void Move(Character c, NodeId to) => c.Node = to;
    }

    private static Character Make(long id, int node, int force) =>
        new Character(new CharacterId(id), new Persona("x", "y", "z", ArchetypeKind.Martial, null),
            new StatBlock(new[] { force, 20, 20, 20 }), new NodeId(node), new Goal(GoalKind.Advance, 0), 0, 800, 16);

    [Fact]
    public void Train_raises_stat_and_emits_event()
    {
        var w = new FakeWorld(); var a = Make(1, 0, 20); w.All.Add(a);
        var evs = new ActionSystem(w.Limits).Execute(w, a, new TrainChoice(StatKind.Force));
        Assert.True(a.Stats.Get(StatKind.Force) > 20);
        Assert.Single(evs.OfType<CharacterTrained>());
    }

    [Fact]
    public void Spar_changes_relations_and_emits_duel()
    {
        var w = new FakeWorld();
        var a = Make(1, 0, 25); var b = Make(2, 0, 20); w.All.Add(a); w.All.Add(b);
        var evs = new ActionSystem(w.Limits).Execute(w, a, new SparChoice(new CharacterId(2)));
        Assert.Single(evs.OfType<DuelResolved>());
        Assert.True(evs.OfType<RelationChanged>().Any());
    }

    [Fact]
    public void Travel_moves_and_emits()
    {
        var w = new FakeWorld(); var a = Make(1, 0, 20); w.All.Add(a);
        var evs = new ActionSystem(w.Limits).Execute(w, a, new TravelChoice(new NodeId(2)));
        Assert.Equal(2, a.Node.Value);
        Assert.Single(evs.OfType<CharacterTraveled>());
    }
}
