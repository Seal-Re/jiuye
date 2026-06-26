using System.Collections.Generic;
using Jianghu.Model;

namespace Jianghu.Sim
{
    /// <summary>
    /// 地图查询接口（integration story-003）。
    /// Map 系统实现此接口。Faction 通过此接口查询地图。
    /// Map 不知道 Faction 存在（单向依赖）。
    /// </summary>
    public interface IGeoQuery
    {
        /// <summary>节点所在区域。</summary>
        int RegionOf(NodeId node);

        /// <summary>区域内的所有节点。</summary>
        IReadOnlyList<NodeId> SitesInRegion(int regionId);

        /// <summary>节点的相邻节点列表。</summary>
        IReadOnlyList<NodeId> AdjacentTo(NodeId node);

        /// <summary>节点类型（0=普通, 1=资源, 2=秘境, 3=宗门）。</summary>
        int SiteType(NodeId node);

        /// <summary>节点资源量（0=无资源）。</summary>
        int ResourceAt(NodeId node);

        /// <summary>节点数。</summary>
        int NodeCount { get; }

        /// <summary>区域数。</summary>
        int RegionCount { get; }
    }

    // ================================================================
    // Story-004: IFactionQuery
    // ================================================================

    /// <summary>宗门成员信息。</summary>
    public readonly struct FactionMemberInfo
    {
        public readonly CharacterId Id;
        public readonly int Rank;       // 0=弟子, 1=执事, 2=长老, 3=掌门
        public readonly long JoinedAt;  // 加入 tick

        public FactionMemberInfo(CharacterId id, int rank, long joinedAt)
        { Id = id; Rank = rank; JoinedAt = joinedAt; }
    }

    /// <summary>
    /// 门派查询接口（integration story-004）。
    /// Faction 系统实现此接口。Brain 通过此接口获取门派上下文。
    /// </summary>
    public interface IFactionQuery
    {
        /// <summary>角色所属门派 ID（0=散修）。</summary>
        int FactionOf(CharacterId id);

        /// <summary>门派成员列表（按 rank 降序）。</summary>
        IReadOnlyList<FactionMemberInfo> MembersOf(int factionId);

        /// <summary>角色在门派中的 rank（0=散修/无 rank）。</summary>
        int RankOf(CharacterId id);

        /// <summary>两个门派的关系（-100..100）。</summary>
        int FactionRelation(int factionA, int factionB);

        /// <summary>门派数量。</summary>
        int FactionCount { get; }

        /// <summary>
        /// 就近同门列表（契约版，忽略 <paramref name="maxDistance"/>，返回全部同门）。
        /// 地理过滤（同区域）见具体实现 <c>SectLedger.NearbyFellows(id, geo, positionOf)</c> 富重载（R-3 落地）——
        /// 需角色→节点位置映射，由 World 注入；接口版保留以稳定契约 + 无 geo 时的退化路径。
        /// </summary>
        IReadOnlyList<CharacterId> NearbyFellows(CharacterId id, int maxDistance);
    }
}
