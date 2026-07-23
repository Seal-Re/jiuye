using System;
using System.Collections.Generic;
using Jianghu.Model;

namespace Jianghu.Sim
{
    /// <summary>站点类型。</summary>
    public enum SiteKind { Normal = 0, Resource = 1, Secret = 2, Sect = 3 }

    /// <summary>地形类型（§2.1，canon 6 种 + 薄灵区地板）。L0 纯数据，追加新地形=加一行枚举。</summary>
    public enum TerrainKind
    {
        Plain = 0,      // 平原（中州）
        Sea = 1,        // 海域（东海）
        Desert = 2,     // 荒漠（北漠）
        MountainFire = 3, // 山岳·火（西陲）
        Jungle = 4,     // 林莽（南疆）
        MountainForest = 5, // 山岳·密林（苗疆）
        Marsh = 6       // 水泽（江南）
    }

    /// <summary>元素属性（§5.2，canon 6 种）。L0 纯数据。</summary>
    public enum ElementKind
    {
        Earth = 0,  // 土（中州）
        Water = 1,  // 水（东海/江南）
        Metal = 2,  // 金（北漠）
        Fire = 3,   // 火（西陲）
        Wood = 4,   // 木（南疆/苗疆）
        None = 5    // 无
    }

    /// <summary>灾厄类型（§5.2，canon 5 种）。L0 纯数据。</summary>
    public enum HazardKind
    {
        None = 0,
        Miasma = 1,     // 瘴疠（南疆·万毒尸沼）
        BeastTide = 2,  // 妖兽潮（江南·太湖鬼潮渚）
        GhostFog = 3,   // 鬼雾（中州次要决战中心）
        Storm = 4,      // 风暴（西陲·地火熔渊）
        CalamityAsh = 5 // 劫烬（北漠·玄昊古战场，玄昊大劫专属）
    }

    /// <summary>区域定义——7 大区（canon GeoCanon §1.2）。L0 纯数据行。</summary>
    public sealed record RegionDef(
        string Name,
        int CenterX, int CenterY,
        int Wealth, int QiDensity, int Strategic,
        TerrainKind Terrain = TerrainKind.Plain,
        ElementKind Element = ElementKind.Earth,
        int Peril = 0,
        HazardKind Hazard = HazardKind.None
    );

    /// <summary>PCG 地形快照数据 (ADR-0006 备选 B: View→Core)。单个 cell 的离散化地形。</summary>
    public sealed record TerrainCellData(
        int NodeId,
        TerrainKind Terrain,
        ElementKind Element,
        int Peril,
        HazardKind Hazard,
        int QiDensity
    );

    /// <summary>节点地形数据（侧表，键 NodeId）。不可变 record。</summary>
    public sealed record NodeGeo(
        SiteKind Kind,
        int ResourceAmount,
        int Wealth, int Qi, int Strategic,
        int DangerTier,
        TerrainKind TerrainVariant = TerrainKind.Plain,
        int QiLayer = 0  // 0=薄灵, 1=衔接带, 2=厚灵
    );

    /// <summary>地图生成配置——所有硬编码参数集中于此。</summary>
    public sealed record MapConfig(
        int RegionMin = 6, int RegionMax = 9,
        int SitesPerRegionMin = 3, int SitesPerRegionMax = 5,
        int MaxDegree = 6, int RedundancyPct = 30,
        int ConnectRadius = 2,
        int MaxSecrets = 15, int SecretQiThreshold = 60,
        // Site composition weights (must sum to 100)
        int WeightNormal = 60, int WeightResource = 25,
        int WeightSecret = 12, int WeightSect = 3,
        // Terrain ranges
        int WealthMin = 10, int WealthMax = 90,
        int QiMin = 20, int QiMax = 80,
        int StrategicMin = 20, int StrategicMax = 80,
        // 秘境进入门槛（story-008 R-4：原 WorldMap 硬编码 20 + DangerTier*5）
        int SecretInsightBase = 20, int SecretInsightPerTier = 5,
        // 旅行加权（story-008 R-4：原 ScoreNode 硬编码 Resource=100/Sect=80/Secret=60/Normal=20+Qi）
        int TravelScoreResource = 100, int TravelScoreSect = 80,
        int TravelScoreSecret = 60, int TravelScoreNormalBase = 20,
        // Names
        IReadOnlyList<string>? RegionNames = null
    )
    {
        public static readonly MapConfig Default = new();

        public IReadOnlyList<string> RegionNamesOrDefault =>
            RegionNames ?? new[] { "中原", "塞外", "江南", "西域", "苗疆", "东海", "南疆", "北漠", "蜀中" };

        /// <summary>验证配置合法性——fail-fast 防无效配置。</summary>
        public void Validate()
        {
            if (RegionMin < 1 || RegionMin > RegionMax)
                throw new ArgumentException($"RegionMin={RegionMin} must be ≤ RegionMax={RegionMax}");
            if (SitesPerRegionMin < 1 || SitesPerRegionMin > SitesPerRegionMax)
                throw new ArgumentException($"SitesPerRegionMin must be ≤ SitesPerRegionMax");
            if (MaxDegree < 2) throw new ArgumentException("MaxDegree must be >= 2");
            if (RedundancyPct < 0 || RedundancyPct > 100)
                throw new ArgumentException("RedundancyPct must be [0,100]");
            int totalWeight = WeightNormal + WeightResource + WeightSecret + WeightSect;
            if (totalWeight != 100)
                throw new ArgumentException($"Site weights must sum to 100, got {totalWeight}");
        }
    }
}
