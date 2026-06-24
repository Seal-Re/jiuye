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
        int Wealth, int QiDensity, int Strategic
    );

    /// <summary>节点地形数据（侧表，键 NodeId）。不可变 record。</summary>
    public sealed record NodeGeo(
        SiteKind Kind,
        int ResourceAmount,
        int Wealth, int Qi, int Strategic,
        int DangerTier
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
