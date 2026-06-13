using System.Collections.Generic;
using Jianghu.Stats;

namespace Jianghu.Model
{
    /// <summary>聚合根。MemoryStore 私有，只经 Remember/RecallMemory 暴露（§4.5）。</summary>
    public sealed class Character
    {
        public CharacterId Id { get; }
        public Persona Persona { get; set; }
        public StatBlock Stats { get; }
        public NodeId Node { get; set; }
        public Goal Goal { get; set; }
        public long Age { get; set; }
        public long Lifespan { get; }
        public long NextActAt { get; set; }
        public bool Alive { get; set; } = true;
        private readonly MemoryStore _memory;

        public Character(CharacterId id, Persona persona, StatBlock stats, NodeId node, Goal goal,
                         long age, long lifespan, int memoryCap)
        {
            Id = id; Persona = persona; Stats = stats; Node = node; Goal = goal;
            Age = age; Lifespan = lifespan; _memory = new MemoryStore(memoryCap);
        }

        private Character(CharacterId id, Persona persona, StatBlock stats, NodeId node, Goal goal,
                          long age, long lifespan, long nextActAt, bool alive, MemoryStore mem)
        {
            Id = id; Persona = persona; Stats = stats; Node = node; Goal = goal;
            Age = age; Lifespan = lifespan; NextActAt = nextActAt; Alive = alive; _memory = mem;
        }

        public void Remember(MemoryEntry e) => _memory.Remember(e);
        public IReadOnlyList<MemoryEntry> RecallMemory() => _memory.Recall();

        public Character Clone() =>
            new Character(Id, Persona, Stats.Clone(), Node, Goal, Age, Lifespan, NextActAt, Alive, _memory.Clone());
    }
}
