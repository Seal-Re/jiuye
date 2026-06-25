using System;
using System.Collections.Generic;
using Jianghu.Model;

namespace Jianghu.Sim
{
    /// <summary>门派生命周期阶段。</summary>
    public enum FactionPhase { Founding = 0, Growth = 1, Peak = 2, Decline = 3, Fallen = 4 }

    /// <summary>门派定义。</summary>
    public sealed record FactionDef(
        int Id,
        string Name,
        int HomeRegion,
        NodeId HomeSite,
        int AlignmentAxis,
        IReadOnlyList<string> EntryRequirements
    )
    {
        /// <summary>野心（story-011：0-100，夺地侵略性。init 属性，既有构造点默认 0 不破坏）。</summary>
        public int Ambition { get; init; }
    }

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
        private readonly Dictionary<CharacterId, int> _contribution = new(); // 贡献度（story-010：切磋胜累加→过阈晋升）

        public int FactionCount => _factions.Count;

        /// <summary>全部门派 Id（确定性顺序：升序）。供接线驱动/查询遍历派系（story-008）。</summary>
        public IReadOnlyList<int> AllFactionIds
        {
            get
            {
                var ids = new List<int>(_factions.Keys);
                ids.Sort();
                return ids;
            }
        }

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

        /// <summary>累加成员贡献度（story-010：纯整数，仅门派成员有效；散修无操作）。</summary>
        public void AddContribution(CharacterId id, int amount)
        {
            if (!_members.ContainsKey(id)) return; // 散修不累
            _contribution.TryGetValue(id, out int cur);
            _contribution[id] = cur + amount;
        }

        /// <summary>查询成员贡献度（无记录=0）。</summary>
        public int ContributionOf(CharacterId id)
            => _contribution.TryGetValue(id, out var v) ? v : 0;

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
            // story-008 R-3（A.8 诚实标注）：maxDistance 暂忽略——地理过滤待 membership×geo 接线后补。
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
            _phases.Remove(factionId);
            _territories.Remove(factionId);
            _treasuries.Remove(factionId);
        }

        // ================================================================
        // Territory control (§3.2)
        // ================================================================

        private readonly Dictionary<int, HashSet<NodeId>> _territories = new(); // factionId → controlled sites

        /// <summary>门派控制站点列表。</summary>
        public IReadOnlyList<NodeId> ControlledSites(int factionId)
            => _territories.TryGetValue(factionId, out var s)
                ? new List<NodeId>(s) : Array.Empty<NodeId>();

        /// <summary>占领站点。</summary>
        public void ControlSite(int factionId, NodeId site)
        {
            if (!_territories.TryGetValue(factionId, out var s))
                _territories[factionId] = s = new HashSet<NodeId>();
            s.Add(site);
        }

        /// <summary>失去站点。</summary>
        public void LoseSite(int factionId, NodeId site)
        {
            if (_territories.TryGetValue(factionId, out var s))
                s.Remove(site);
        }

        /// <summary>站点归属门派（0=无主）。</summary>
        public int OwnerOf(NodeId site)
        {
            foreach (var (fid, sites) in _territories)
                if (sites.Contains(site)) return fid;
            return 0;
        }

        // ================================================================
        // Faction phase + lifecycle (§3)
        // ================================================================

        private readonly Dictionary<int, FactionPhase> _phases = new(); // factionId → phase
        private readonly Dictionary<int, long> _foundedAt = new();      // factionId → founding tick
        private readonly Dictionary<int, int> _treasuries = new();      // factionId → treasury

        /// <summary>门派阶段。</summary>
        public FactionPhase PhaseOf(int factionId)
            => _phases.TryGetValue(factionId, out var p) ? p : FactionPhase.Founding;

        /// <summary>门派金库。</summary>
        public int TreasuryOf(int factionId)
            => _treasuries.TryGetValue(factionId, out var t) ? t : 0;

        /// <summary>Add treasury.</summary>
        public void AddTreasury(int factionId, int amount)
        {
            if (!_treasuries.TryGetValue(factionId, out var t)) t = 0;
            _treasuries[factionId] = Math.Max(0, t + amount);
        }

        /// <summary>初始化门派阶段（注册时调用）。</summary>
        public void InitPhase(int factionId, long tick)
        {
            _phases[factionId] = FactionPhase.Founding;
            _foundedAt[factionId] = tick;
            _treasuries[factionId] = 100; // 初始金库
        }

        /// <summary>
        /// 门派 Pump——每 Clock tick 调用一次（§3 节流）。
        /// 处理阶段转换 + 税收 + 衰落 + 夺地（story-011）。
        /// </summary>
        /// <param name="factionMight">per-faction Might 快照（factionId→战力和）；null=不夺地（off 安全）。</param>
        /// <returns>本次夺地产生的领地易主（World 投影入 Chronicle）；无则空。</returns>
        public IReadOnlyList<(long Site, int From, int To)> Pump(
            long clock, IGeoQuery? geo = null,
            IReadOnlyDictionary<int, int>? factionMight = null)
        {
            foreach (var (fid, phase) in _phases)
            {
                long age = clock - (_foundedAt.TryGetValue(fid, out var ft) ? ft : clock);
                int memberCount = MembersOf(fid).Count;
                int siteCount = ControlledSites(fid).Count;

                // —— Phase transitions ——
                FactionPhase newPhase = phase;
                if (phase == FactionPhase.Founding && age > 200 && memberCount >= 2)
                    newPhase = FactionPhase.Growth;
                else if (phase == FactionPhase.Growth && memberCount >= 5 && siteCount >= 2)
                    newPhase = FactionPhase.Peak;
                else if (phase == FactionPhase.Peak && (memberCount < 3 || siteCount == 0))
                    newPhase = FactionPhase.Decline;
                else if (phase == FactionPhase.Decline && memberCount == 0)
                    newPhase = FactionPhase.Fallen;

                if (newPhase != phase)
                    _phases[fid] = newPhase;

                // —— Revenue: controlled sites generate treasury ——
                if (newPhase != FactionPhase.Fallen && geo != null && clock % 50 == 0)
                {
                    foreach (var site in ControlledSites(fid))
                        AddTreasury(fid, Math.Max(1, geo.ResourceAt(site) / 10));
                }
            }

            // —— 夺地兑现世仇（story-011，design §3）：仅 geo+Might 在位时（off/factionOff 不触）——
            // 节流：与 revenue 同 clock%50 周期；只扫相邻边界 site（非全图）。
            if (geo != null && factionMight != null && clock % 50 == 0)
                return ResolveConquest(geo, factionMight, clock);

            return System.Array.Empty<(long, int, int)>();
        }

        // 夺地阈值（design §3 ConquestGap）：攻方 Ambition≥阈 + Might 差≥Gap + 区域相邻 → 夺地。
        private const int AmbitionThreshold = 60;
        private const int ConquestGap = 30;

        /// <summary>
        /// 非致死夺地结算（story-011）。只扫相邻边界 site（攻方领地的邻居中属敌方者），非全图 O(site²)。
        /// 纯整数确定性：攻方按 factionId 升序、候选 site 升序裁决；不杀人、不清成员（致死灭门留 C.1）。
        /// </summary>
        private IReadOnlyList<(long Site, int From, int To)> ResolveConquest(
            IGeoQuery geo, IReadOnlyDictionary<int, int> might, long clock)
        {
            var changes = new List<(long, int, int)>();
            // 确定性：攻方按 Id 升序。
            var attackers = new List<int>(_factions.Keys); attackers.Sort();
            foreach (var atk in attackers)
            {
                if (!_factions.TryGetValue(atk, out var def) || def.Ambition < AmbitionThreshold) continue;
                if (_phases.TryGetValue(atk, out var ph) && ph == FactionPhase.Fallen) continue;
                might.TryGetValue(atk, out int atkMight);

                // 候选：攻方每块领地的相邻 site 中，属**敌方门派**者（边界扫描，去重 + 升序裁决）。
                // design §3 = 夺地兑现世仇（取敌方地）；无主地"拓土"属另一机制，本 story 不含（defender==0 跳过）。
                var candidates = new SortedSet<int>();
                foreach (var owned in ControlledSites(atk))
                    foreach (var nbr in geo.AdjacentTo(owned))
                    {
                        int owner = OwnerOf(nbr);
                        if (owner != atk && owner != 0) candidates.Add(nbr.Value); // 仅敌方有主地
                    }

                foreach (var siteVal in candidates) // SortedSet 升序 → 确定性
                {
                    var site = new NodeId(siteVal);
                    int defender = OwnerOf(site);
                    if (defender == 0 || defender == atk) continue; // 已被同轮夺走/无主 → 跳过
                    might.TryGetValue(defender, out int defMight);
                    if (atkMight - defMight < ConquestGap) continue;

                    // 夺地兑现：守方失地 + 攻方得地 + 双向关系恶化。
                    LoseSite(defender, site);
                    ControlSite(atk, site);
                    changes.Add((siteVal, defender, atk));
                    int cur = FactionRelation(defender, atk);
                    SetRelation(defender, atk, Math.Max((int)FactionRelationKind.Enemy, cur - 40));
                    SetRelation(atk, defender, Math.Max((int)FactionRelationKind.Enemy, FactionRelation(atk, defender) - 40));
                }
            }
            return changes;
        }

        public SectLedger Clone()
        {
            var clone = new SectLedger();
            foreach (var kv in _factions) clone._factions[kv.Key] = kv.Value;
            foreach (var kv in _members) clone._members[kv.Key] = kv.Value;
            foreach (var kv in _relations) clone._relations[kv.Key] = kv.Value;
            foreach (var kv in _territories) clone._territories[kv.Key] = new HashSet<NodeId>(kv.Value);
            foreach (var kv in _phases) clone._phases[kv.Key] = kv.Value;
            foreach (var kv in _foundedAt) clone._foundedAt[kv.Key] = kv.Value;
            foreach (var kv in _treasuries) clone._treasuries[kv.Key] = kv.Value;
            foreach (var kv in _contribution) clone._contribution[kv.Key] = kv.Value;
            return clone;
        }

        /// <summary>
        /// 确定性全状态序列化（story-010：StateSnapshot 接入，补 Faction 快照空白）。
        /// 显式排序（factionId / CharacterId.Value / 关系键 升序），不依赖字典枚举序。
        /// 覆盖 members(含 Rank/贡献度) + phases + treasuries + territories + relations。
        /// </summary>
        public string CaptureState()
        {
            var sb = new System.Text.StringBuilder();

            // factions：按 Id 升序，附 phase + treasury + 领地（排序）。
            var fids = new List<int>(_factions.Keys); fids.Sort();
            foreach (var fid in fids)
            {
                sb.Append('F').Append(fid)
                  .Append(":ph").Append((int)PhaseOf(fid))
                  .Append(":tr").Append(TreasuryOf(fid))
                  .Append(":founded").Append(_foundedAt.TryGetValue(fid, out var ft) ? ft : 0)
                  .Append(":sites[");
                var sites = new List<int>();
                if (_territories.TryGetValue(fid, out var ts)) { foreach (var s in ts) sites.Add(s.Value); }
                sites.Sort();
                for (int i = 0; i < sites.Count; i++) { if (i > 0) sb.Append(','); sb.Append(sites[i]); }
                sb.Append("]\n");
            }

            // members：按 CharacterId.Value 升序，附 faction/rank/贡献度。
            var mids = new List<long>(); foreach (var k in _members.Keys) mids.Add(k.Value);
            mids.Sort();
            foreach (var mv in mids)
            {
                var cid = new CharacterId(mv);
                var m = _members[cid];
                sb.Append('M').Append(mv)
                  .Append(":f").Append(m.FactionId)
                  .Append(":r").Append(m.Rank)
                  .Append(":c").Append(ContributionOf(cid))
                  .Append('\n');
            }

            // relations：按键升序。
            var rkeys = new List<int>(_relations.Keys); rkeys.Sort();
            foreach (var rk in rkeys)
                sb.Append('R').Append(rk).Append(':').Append(_relations[rk]).Append('\n');

            return sb.ToString();
        }
    }
}
