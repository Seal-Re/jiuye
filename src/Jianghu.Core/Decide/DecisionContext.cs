using System.Collections.Generic;
using Jianghu.Actions;
using Jianghu.Model;
using Jianghu.Stats;

namespace Jianghu.Decide
{
    public readonly record struct NearbyActor(CharacterId Id, int Power, int Affinity);

    /// <summary>角色可感知子集（不含 World）→ Brain 无法越权改世界（R-NF1）。</summary>
    public sealed record DecisionContext(
        CharacterId Self,
        StatBlock Stats,
        Goal Goal,
        NodeId Node,
        IReadOnlyList<NearbyActor> Nearby,
        IReadOnlyList<ActionType> Allowed,
        IReadOnlyList<MemoryEntry> Memory);
}
