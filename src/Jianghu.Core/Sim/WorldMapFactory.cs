using System;
using System.Collections.Generic;

namespace Jianghu.Sim
{
    /// <summary>
    /// 地图工厂——从配置+生成器构造 WorldMap。
    /// 加生成算法 = 加 IMapGenerator 实现 + 注册到此工厂。
    /// 灾备：生成失败回退到最小拓扑（3 区域链），保证不崩溃。
    /// </summary>
    public static class WorldMapFactory
    {
        /// <summary>当前注册的生成器（可替换）。</summary>
        public static IMapGenerator Generator { get; set; } = new KruskalMstGenerator();

        /// <summary>
        /// 创建世界地图。正常路径：Generator.Generate(config, rng)。
        /// 灾备路径：生成失败 → 回退到最小拓扑（FallbackMinimal）。
        /// </summary>
        public static WorldMap Create(MapConfig config, Random.IRandom rng)
        {
            try
            {
                var result = Generator.Generate(config, rng);
                return new WorldMap(result, config);
            }
            catch (Exception ex)
            {
                // 灾备：最小拓扑保证不崩溃
                System.Diagnostics.Debug.WriteLine($"Map generation failed ({Generator.Name}): {ex.Message}. Falling back to minimal topology.");
                return CreateFallback(config, rng);
            }
        }

        /// <summary>使用指定生成器创建（跳过灾备）。</summary>
        public static WorldMap CreateWith(IMapGenerator generator, MapConfig config, Random.IRandom rng)
        {
            var prev = Generator;
            Generator = generator;
            try { return Create(config, rng); }
            finally { Generator = prev; }
        }

        /// <summary>灾备：最小连通拓扑——3 区域链，每区域 2 站点。</summary>
        private static WorldMap CreateFallback(MapConfig config, Random.IRandom rng)
        {
            var names = config.RegionNamesOrDefault;
            var regions = new RegionDef[3];
            for (int r = 0; r < 3; r++)
                regions[r] = new RegionDef(names[r], 30 + r * 20, 50, 50, 50, 50);

            var sites = new NodeGeo[6];
            var adj = new List<int>[6];
            for (int i = 0; i < 6; i++)
            {
                sites[i] = new NodeGeo(SiteKind.Normal, 0, 50, 50, 50, 1);
                adj[i] = new List<int>();
            }
            // Chain: 0-1-2-3-4-5 with cross-connections
            for (int i = 0; i < 5; i++) { adj[i].Add(i + 1); adj[i + 1].Add(i); }
            // Inter-region bridges: 1-2, 3-4
            adj[1].Add(2); adj[2].Add(1);
            adj[3].Add(4); adj[4].Add(3);

            return new WorldMap(new MapGenerationResult(regions, sites, adj), config);
        }
    }
}
