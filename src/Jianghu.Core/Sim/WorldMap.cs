using System;
using System.Collections.Generic;
using Jianghu.Model;

namespace Jianghu.Sim
{
    /// <summary>站点类型。</summary>
    public enum SiteKind { Normal = 0, Resource = 1, Secret = 2, Sect = 3 }

    /// <summary>区域定义——三类地区（§2.1）。</summary>
    public sealed record RegionDef(
        string Name,
        int CenterX, int CenterY,
        int Wealth, int QiDensity, int Strategic // 地利三维 [0,100]
    );

    /// <summary>节点地形数据（侧表，键 NodeId）。不可变 record。</summary>
    public sealed record NodeGeo(
        SiteKind Kind,
        int ResourceAmount,
        int Wealth, int Qi, int Strategic,
        int DangerTier
    );

    /// <summary>
    /// 世界地图——完整三层模型（Realm→Region→Site）。§2.1-2.3。
    /// 生成算法：Kruskal MST + 受控冗余边（§2.2）。确定性（同 seed → 同拓扑）。
    /// 拓扑生成后冻结不可变。仅 RevealedSecrets + ResourceAmount 运行时变化。
    /// </summary>
    public sealed class WorldMap : IGeoQuery
    {
        private readonly int _nodeCount;
        private readonly int _regionCount;
        private readonly RegionDef[] _regions;
        private readonly NodeGeo[] _sites;
        private readonly List<int>[] _adjacency;
        private readonly HashSet<int> _revealedSecrets;

        public IReadOnlyList<RegionDef> Regions => _regions;
        public IReadOnlyCollection<int> RevealedSecrets => _revealedSecrets;
        public int NodeCount => _nodeCount;
        public int RegionCount => _regionCount;

        // ================================================================
        // Construction: Kruskal MST + Redundancy (§2.2)
        // ================================================================
        public WorldMap(Random.IRandom rng)
        {
            // —— Step 1: Region generation (6-9 regions) ——
            _regionCount = rng.NextInt(4) + 6;
            _regions = new RegionDef[_regionCount];
            var names = new[] { "中原", "塞外", "江南", "西域", "苗疆", "东海", "南疆", "北漠", "蜀中" };
            for (int r = 0; r < _regionCount; r++)
            {
                _regions[r] = new RegionDef(
                    names[r % names.Length],
                    rng.NextInt(80) + 10, rng.NextInt(80) + 10,
                    rng.NextInt(80) + 10,  // Wealth [10,90]
                    rng.NextInt(60) + 20,  // QiDensity [20,80]
                    rng.NextInt(60) + 20   // Strategic [20,80]
                );
            }

            // —— Step 2: Site generation (3-5 per region) ——
            int sitesPerRegion = rng.NextInt(3) + 3;
            _nodeCount = _regionCount * sitesPerRegion;
            _sites = new NodeGeo[_nodeCount];
            _adjacency = new List<int>[_nodeCount];
            _revealedSecrets = new HashSet<int>();

            for (int r = 0; r < _regionCount; r++)
            {
                for (int s = 0; s < sitesPerRegion; s++)
                {
                    int nid = r * sitesPerRegion + s;
                    int roll = rng.NextInt(100);
                    SiteKind kind = roll < 60 ? SiteKind.Normal
                        : roll < 85 ? SiteKind.Resource
                        : roll < 97 ? SiteKind.Secret
                        : SiteKind.Sect;

                    int res = kind == SiteKind.Resource ? 50 + rng.NextInt(150) : 0;
                    int w = Math.Clamp(_regions[r].Wealth + rng.NextInt(20) - 10, 0, 100);
                    int q = Math.Clamp(_regions[r].QiDensity + rng.NextInt(20) - 10, 0, 100);
                    int st = Math.Clamp(_regions[r].Strategic + rng.NextInt(20) - 10, 0, 100);
                    int danger = q > 60 ? rng.NextInt(3) + 1 : rng.NextInt(2);

                    _sites[nid] = new NodeGeo(kind, res, w, q, st, Math.Min(3, danger));
                    _adjacency[nid] = new List<int>();
                }
            }

            // —— Step 3: Kruskal MST ——
            int connectRadius = 2;
            var edges = new List<(int Weight, int From, int To)>();
            for (int i = 0; i < _nodeCount; i++)
            {
                for (int j = i + 1; j < _nodeCount; j++)
                {
                    int ri = i / sitesPerRegion, rj = j / sitesPerRegion;
                    int dist = Math.Abs(_regions[ri].CenterX - _regions[rj].CenterX)
                             + Math.Abs(_regions[ri].CenterY - _regions[rj].CenterY);
                    if (Math.Abs(ri - rj) <= connectRadius || dist <= 30)
                        edges.Add((ri == rj ? 10 : dist, i, j));
                }
            }
            // Deterministic sort: weight, from, to
            edges.Sort((a, b) =>
            {
                int c = a.Weight.CompareTo(b.Weight);
                if (c != 0) return c;
                c = a.From.CompareTo(b.From);
                return c != 0 ? c : a.To.CompareTo(b.To);
            });

            // Union-Find
            var parent = new int[_nodeCount];
            for (int i = 0; i < _nodeCount; i++) parent[i] = i;
            int Find(int x) => parent[x] == x ? x : parent[x] = Find(parent[x]);
            void Union(int a, int b) { parent[Find(a)] = Find(b); }

            int mstEdges = 0;
            foreach (var (w, f, t) in edges)
            {
                if (Find(f) != Find(t)) { Union(f, t); _adjacency[f].Add(t); _adjacency[t].Add(f); mstEdges++; }
            }

            // —— Step 4: Redundancy (30%, degree ≤ 6) ——
            int redundTarget = mstEdges * 30 / 100;
            int redundAdded = 0;
            foreach (var (w, f, t) in edges)
            {
                if (redundAdded >= redundTarget) break;
                if (_adjacency[f].Count < 6 && _adjacency[t].Count < 6 && !_adjacency[f].Contains(t))
                { _adjacency[f].Add(t); _adjacency[t].Add(f); redundAdded++; }
            }

            // —— Step 5: Inter-region bridges ——
            for (int r = 0; r < _regionCount - 1; r++)
            {
                int a = r * sitesPerRegion, b = (r + 1) * sitesPerRegion;
                if (!_adjacency[a].Contains(b)) { _adjacency[a].Add(b); _adjacency[b].Add(a); }
            }

            // —— Step 6: Sort adjacency ——
            for (int i = 0; i < _nodeCount; i++) _adjacency[i].Sort();

            // —— Step 7: INV-MAP-CONNECTED BFS ——
            var visited = new bool[_nodeCount];
            var queue = new Queue<int>();
            queue.Enqueue(0); visited[0] = true;
            int reachable = 1;
            while (queue.Count > 0)
            {
                foreach (var next in _adjacency[queue.Dequeue()])
                    if (!visited[next]) { visited[next] = true; queue.Enqueue(next); reachable++; }
            }
            if (reachable != _nodeCount)
                throw new InvalidOperationException($"INV-MAP-CONNECTED: {reachable}/{_nodeCount}");
        }

        // ================================================================
        // Runtime operations (§2.3)
        // ================================================================

        /// <summary>采集资源节点。返回采集量。</summary>
        public int Harvest(NodeId node, int maxHarvest = 20)
        {
            var geo = _sites[node.Value];
            if (geo.Kind != SiteKind.Resource || geo.ResourceAmount <= 0) return 0;
            int amount = Math.Min(geo.ResourceAmount, maxHarvest);
            _sites[node.Value] = geo with { ResourceAmount = geo.ResourceAmount - amount };
            return amount;
        }

        /// <summary>尝试进入秘境。返回是否成功揭示。</summary>
        public bool EnterSecret(NodeId node, int insight)
        {
            var geo = _sites[node.Value];
            if (geo.Kind != SiteKind.Secret) return false;
            if (_revealedSecrets.Contains(node.Value)) return false; // already revealed
            // Insight gate: Insight ≥ 20 + DangerTier*5
            if (insight < 20 + geo.DangerTier * 5) return false;
            _revealedSecrets.Add(node.Value);
            return true;
        }

        /// <summary>Travel 目标选择——加权邻居排序（资源/Hub 优先）。</summary>
        public NodeId BestNeighbor(NodeId current)
        {
            var adj = _adjacency[current.Value];
            if (adj.Count == 0) return current;

            // Score: Resource node > Sect node > higher Qi > random tiebreak
            int bestIdx = adj[0];
            int bestScore = ScoreNode(adj[0]);
            for (int i = 1; i < adj.Count; i++)
            {
                int s = ScoreNode(adj[i]);
                if (s > bestScore) { bestScore = s; bestIdx = adj[i]; }
            }
            return new NodeId(bestIdx);
        }

        private int ScoreNode(int nodeId)
        {
            var geo = _sites[nodeId];
            return geo.Kind switch
            {
                SiteKind.Resource => 100 + geo.ResourceAmount,
                SiteKind.Sect => 80,
                SiteKind.Secret => 60,
                _ => 20 + geo.Qi
            };
        }

        // ================================================================
        // IGeoQuery implementation
        // ================================================================
        public int RegionOf(NodeId node) => node.Value / (_nodeCount / _regionCount);
        public IReadOnlyList<NodeId> SitesInRegion(int regionId)
        {
            int per = _nodeCount / _regionCount;
            var list = new List<NodeId>();
            for (int i = regionId * per; i < (regionId + 1) * per && i < _nodeCount; i++)
                list.Add(new NodeId(i));
            return list;
        }
        public IReadOnlyList<NodeId> AdjacentTo(NodeId node)
        {
            var list = new List<NodeId>();
            foreach (var adj in _adjacency[node.Value]) list.Add(new NodeId(adj));
            return list;
        }
        public int SiteType(NodeId node) => (int)_sites[node.Value].Kind;
        public int ResourceAt(NodeId node) => _sites[node.Value].ResourceAmount;

        /// <summary>查询节点地形数据。</summary>
        public NodeGeo GeoOf(NodeId node) => _sites[node.Value];

        /// <summary>查询区域定义。</summary>
        public RegionDef RegionAt(NodeId node) => _regions[RegionOf(node)];

        /// <summary>可达节点列表（用于 RuleBrain.Travel 目标选择）。</summary>
        public IReadOnlyList<NodeId> ReachableFrom(NodeId node)
        {
            var list = new List<NodeId>();
            foreach (var adj in _adjacency[node.Value])
                list.Add(new NodeId(adj));
            return list;
        }

        public WorldMap Clone() => (WorldMap)MemberwiseClone();
    }
}
