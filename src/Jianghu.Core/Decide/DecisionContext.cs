using System.Collections.Generic;
using Jianghu.Actions;
using Jianghu.Model;
using Jianghu.Stats;

namespace Jianghu.Decide
{
    public readonly record struct NearbyActor(CharacterId Id, int Power, int Affinity);

    /// <summary>角色可感知子集（不含 World）→ Brain 无法越权改世界（R-NF1）。</summary>
    /// <remarks>
    /// integration story-002：新增 Reachable + FactionInfo 字段（默认 null，向后兼容）。
    /// </remarks>
    public sealed record DecisionContext(
        CharacterId Self,
        StatBlock Stats,
        Goal Goal,
        NodeId Node,
        IReadOnlyList<NearbyActor> Nearby,
        IReadOnlyList<ActionType> Allowed,
        IReadOnlyList<MemoryEntry> Memory,
        // —— integration story-002 扩展 ——
        IReadOnlyList<NodeId>? Reachable = null,             // 可达节点（Map-on → 邻接图；Map-off → null）
        int FactionId = 0,                                    // 所属门派（0=散修）
        int FactionRank = 0,                                  // 门派内 rank
        int FactionReputation = 0,                            // 门派声望
        // —— 切磋度量修复（2026-07-03）：自身战力（on=PE 含 realm 倍率，与 NearbyActor.Power/DuelEngine 一致；
        //    off/0=未提供 → RuleBrain 回退 raw stats 公式，逐字节兼容）。——
        int SelfPower = 0
    );
}
