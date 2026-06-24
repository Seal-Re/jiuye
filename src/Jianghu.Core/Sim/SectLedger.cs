using System;
using System.Collections.Generic;
using Jianghu.Model;

namespace Jianghu.Sim
{
    /// <summary>门派定义。</summary>
    public sealed record FactionDef(
        int Id,
        string Name,
        int HomeRegion,
        NodeId HomeSite,
        int AlignmentAxis,  // 0=neutral, +1=righteous, -1=evil
        IReadOnlyList<string> EntryRequirements
    );

    /// <summary>
    /// 门派帐本（integration + Faction epic D）。
    /// 持全量门派成员——side-table 模式：不修改 Character/Persona 结构。
    /// 实现 IFactionQuery，供 Brain 查询。
    /// </summary>
    public sealed class SectLedger : IFactionQuery
    {
        private readonly Dictionary<int, FactionDef> _factions = new();
        private readonly Dictionary<CharacterId, (int FactionId, int Rank, long JoinedAt)> _members = new();
        private readonly Dictionary<int, int> _relations = new(); // (fA<<16|fB) → affinity

        public int FactionCount => _factions.Count;

        /// <summary>注册门派。</summary>
        public void RegisterFaction(FactionDef def) => _factions[def.Id] = def;

        /// <summary>加入门派。</summary>
        public void Join(CharacterId id, int factionId, long tick)
        {
            if (!_factions.ContainsKey(factionId)) throw new ArgumentException($"Faction {factionId} not registered");
            _members[id] = (factionId, 0, tick); // rank 0 = disciple
        }

        /// <summary>离开门派（变为散修）。</summary>
        public void Leave(CharacterId id) => _members.Remove(id);

        /// <summary>晋升 rank。</summary>
        public void Promote(CharacterId id)
        {
            if (!_members.TryGetValue(id, out var m)) return;
            int newRank = Math.Min(m.Rank + 1, 3); // max rank = 3 (掌门)
            _members[id] = (m.FactionId, newRank, m.JoinedAt);
        }

        /// <summary>设置两个门派间的关系。</summary>
        public void SetRelation(int factionA, int factionB, int affinity)
            => _relations[(factionA << 16) | factionB] = affinity;

        // IFactionQuery implementation
        public int FactionOf(CharacterId id)
            => _members.TryGetValue(id, out var m) ? m.FactionId : 0;

        public int RankOf(CharacterId id)
            => _members.TryGetValue(id, out var m) ? m.Rank : 0;

        public IReadOnlyList<FactionMemberInfo> MembersOf(int factionId)
        {
            var list = new List<FactionMemberInfo>();
            foreach (var (id, (fid, rank, joinedAt)) in _members)
                if (fid == factionId)
                    list.Add(new FactionMemberInfo(id, rank, joinedAt));
            list.Sort((a, b) => b.Rank.CompareTo(a.Rank)); // rank desc
            return list;
        }

        public int FactionRelation(int factionA, int factionB)
        {
            int key = (factionA << 16) | factionB;
            return _relations.TryGetValue(key, out var r) ? r : 0;
        }

        public IReadOnlyList<CharacterId> NearbyFellows(CharacterId id, int maxDistance)
        {
            if (!_members.TryGetValue(id, out var m)) return Array.Empty<CharacterId>();
            var list = new List<CharacterId>();
            foreach (var (otherId, (fid, _, _)) in _members)
                if (fid == m.FactionId && !otherId.Equals(id))
                    list.Add(otherId);
            return list;
        }

        /// <summary>首领逝世 → 继任（rank 最高 → 资历最久 → Id 最小）。</summary>
        public CharacterId? Succession(int factionId)
        {
            var members = MembersOf(factionId);
            if (members.Count == 0) return null;
            return members[0].Id; // already sorted by rank desc
        }

        /// <summary>门派被消灭——移除全部成员。</summary>
        public void Disband(int factionId)
        {
            var toRemove = new List<CharacterId>();
            foreach (var (id, (fid, _, _)) in _members)
                if (fid == factionId) toRemove.Add(id);
            foreach (var id in toRemove) _members.Remove(id);
        }

        public SectLedger Clone()
        {
            var clone = new SectLedger();
            foreach (var kv in _factions) clone._factions[kv.Key] = kv.Value;
            foreach (var kv in _members) clone._members[kv.Key] = kv.Value;
            foreach (var kv in _relations) clone._relations[kv.Key] = kv.Value;
            return clone;
        }
    }
}
