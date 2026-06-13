using Jianghu.Config;
using Jianghu.Model;
using Jianghu.Stats;
using Xunit;

public class CharacterTests
{
    [Fact]
    public void Character_exposes_memory_only_through_methods()
    {
        var ch = new Character(new CharacterId(1),
            new Persona("无名", "客", "市井", ArchetypeKind.Martial, null),
            new StatBlock(new[] { 20, 20, 20, 20 }),
            new NodeId(0), new Goal(GoalKind.Advance, 0),
            age: 0, lifespan: 800, memoryCap: 16);
        ch.Remember(new MemoryEntry(1, "x", new CharacterId(1), null, 1));
        Assert.Single(ch.RecallMemory());
        Assert.True(ch.Alive);
    }
}
