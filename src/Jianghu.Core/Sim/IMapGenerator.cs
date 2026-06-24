using System;
using System.Collections.Generic;

namespace Jianghu.Sim
{
    /// <summary>
    /// 地图生成器接口——可插拔拓扑算法。
    /// 新生成器 = 新实现 + 注册到 WorldMapFactory。
    /// </summary>
    public interface IMapGenerator
    {
        /// <summary>生成器标识（用于日志/审计）。</summary>
        string Name { get; }

        /// <summary>
        /// 从配置和 RNG 生成地图拓扑和站点。
        /// 返回 (regions, sites, adjacency) 三元组——WorldMap 据此构造不可变数据。
        /// </summary>
        MapGenerationResult Generate(MapConfig config, Random.IRandom rng);
    }

    /// <summary>地图生成结果——纯数据，无行为。</summary>
    public sealed record MapGenerationResult(
        IReadOnlyList<RegionDef> Regions,
        IReadOnlyList<NodeGeo> Sites,
        IReadOnlyList<IReadOnlyList<int>> Adjacency  // Adjacency[nodeId] = sorted neighbor list
    );

    // ================================================================
    // KruskalMstGenerator — 默认生成器（§2.2 算法）
    // ================================================================

    /// <summary>Kruskal MST + 冗余边地图生成器（设计规范 §2.2 默认算法）。</summary>
    public sealed class KruskalMstGenerator : IMapGenerator
    {
        public string Name => "KruskalMst";

        public MapGenerationResult Generate(MapConfig config, Random.IRandom rng)
        {
            config.Validate();

            // —— Step 1: Region generation ——
            int regionCount = rng.NextInt(config.RegionMax - config.RegionMin + 1) + config.RegionMin;
            var regions = new RegionDef[regionCount];
            var names = config.RegionNamesOrDefault;
            for (int r = 0; r < regionCount; r++)
            {
                regions[r] = new RegionDef(
                    names[r % names.Count],
                    rng.NextInt(80) + 10, rng.NextInt(80) + 10,
                    rng.NextInt(config.WealthMax - config.WealthMin + 1) + config.WealthMin,
                    rng.NextInt(config.QiMax - config.QiMin + 1) + config.QiMin,
                    rng.NextInt(config.StrategicMax - config.StrategicMin + 1) + config.StrategicMin
                );
            }

            // —— Step 2: Site generation ——
            int sitesPerRegion = rng.NextInt(config.SitesPerRegionMax - config.SitesPerRegionMin + 1) + config.SitesPerRegionMin;
            int nodeCount = regionCount * sitesPerRegion;
            var sites = new NodeGeo[nodeCount];
            var adj = new List<int>[nodeCount];
            for (int i = 0; i < nodeCount; i++) adj[i] = new List<int>();

            for (int r = 0; r < regionCount; r++)
            {
                for (int s = 0; s < sitesPerRegion; s++)
                {
                    int nid = r * sitesPerRegion + s;
                    int roll = rng.NextInt(100);
                    SiteKind kind;
                    if (roll < config.WeightNormal) kind = SiteKind.Normal;
                    else if (roll < config.WeightNormal + config.WeightResource) kind = SiteKind.Resource;
                    else if (roll < config.WeightNormal + config.WeightResource + config.WeightSecret) kind = SiteKind.Secret;
                    else kind = SiteKind.Sect;

                    int res = kind == SiteKind.Resource ? 50 + rng.NextInt(150) : 0;
                    int w = Math.Clamp(regions[r].Wealth + rng.NextInt(20) - 10, 0, 100);
                    int q = Math.Clamp(regions[r].QiDensity + rng.NextInt(20) - 10, 0, 100);
                    int st = Math.Clamp(regions[r].Strategic + rng.NextInt(20) - 10, 0, 100);
                    int danger = q > 60 ? rng.NextInt(3) + 1 : rng.NextInt(2);
                    sites[nid] = new NodeGeo(kind, res, w, q, st, Math.Min(3, danger));
                }
            }

            // —— Step 3: Edge candidates ——
            var edges = new List<(int Weight, int From, int To)>();
            for (int i = 0; i < nodeCount; i++)
            {
                for (int j = i + 1; j < nodeCount; j++)
                {
                    int ri = i / sitesPerRegion, rj = j / sitesPerRegion;
                    int dist = Math.Abs(regions[ri].CenterX - regions[rj].CenterX)
                             + Math.Abs(regions[ri].CenterY - regions[rj].CenterY);
                    if (Math.Abs(ri - rj) <= config.ConnectRadius || dist <= 30)
                        edges.Add((ri == rj ? 10 : dist, i, j));
                }
            }
            edges.Sort((a, b) =>
            {
                int c = a.Weight.CompareTo(b.Weight);
                if (c != 0) return c;
                c = a.From.CompareTo(b.From);
                return c != 0 ? c : a.To.CompareTo(b.To);
            });

            // —— Step 4: Kruskal MST ——
            var parent = new int[nodeCount];
            for (int i = 0; i < nodeCount; i++) parent[i] = i;
            int Find(int x) => parent[x] == x ? x : parent[x] = Find(parent[x]);
            void Union(int a, int b) { parent[Find(a)] = Find(b); }

            int mstEdges = 0;
            foreach (var (w, f, t) in edges)
            {
                if (Find(f) != Find(t)) { Union(f, t); adj[f].Add(t); adj[t].Add(f); mstEdges++; }
            }

            // —— Step 5: Redundancy ——
            int redundTarget = mstEdges * config.RedundancyPct / 100;
            int redundAdded = 0;
            foreach (var (w, f, t) in edges)
            {
                if (redundAdded >= redundTarget) break;
                if (adj[f].Count < config.MaxDegree && adj[t].Count < config.MaxDegree && !adj[f].Contains(t))
                { adj[f].Add(t); adj[t].Add(f); redundAdded++; }
            }

            // —— Step 6: Inter-region bridges ——
            for (int r = 0; r < regionCount - 1; r++)
            {
                int a = r * sitesPerRegion, b = (r + 1) * sitesPerRegion;
                if (!adj[a].Contains(b)) { adj[a].Add(b); adj[b].Add(a); }
            }

            // —— Step 7: Sort adjacency ——
            for (int i = 0; i < nodeCount; i++) adj[i].Sort();

            // —— Step 8: INV-MAP-CONNECTED BFS ——
            var visited = new bool[nodeCount];
            var queue = new Queue<int>();
            queue.Enqueue(0); visited[0] = true;
            int reachable = 1;
            while (queue.Count > 0)
                foreach (var next in adj[queue.Dequeue()])
                    if (!visited[next]) { visited[next] = true; queue.Enqueue(next); reachable++; }
            if (reachable != nodeCount)
                throw new InvalidOperationException($"INV-MAP-CONNECTED violated: {reachable}/{nodeCount} reachable");

            return new MapGenerationResult(regions, sites, adj);
        }
    }
}
