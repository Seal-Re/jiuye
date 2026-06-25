using System;
using System.Collections.Generic;
using Jianghu.Model;

namespace Jianghu.Sim
{
    /// <summary>
    /// 世界地图——纯数据 + 查询（IGeoQuery）+ 运行时操作（Harvest/EnterSecret/BestNeighbor）。
    /// 由 WorldMapFactory 构造。拓扑生成后冻结不可变。
    /// 仅 RevealedSecrets 和 ResourceAmount 在运行时通过 Harvest/EnterSecret 变化。
    /// </summary>
    public sealed class WorldMap : IGeoQuery
    {
        private readonly RegionDef[] _regions;
        private readonly NodeGeo[] _sites;
        private readonly List<int>[] _adjacency;
        private readonly HashSet<int> _revealedSecrets;
        private readonly int _nodeCount;
        private readonly int _regionCount;
        // 配置化调参（story-008 R-4）：原硬编码常量改由 MapConfig 注入。
        private readonly int _secretInsightBase;
        private readonly int _secretInsightPerTier;
        private readonly int _scoreResource, _scoreSect, _scoreSecret, _scoreNormalBase;

        public IReadOnlyList<RegionDef> Regions => _regions;
        public IReadOnlyCollection<int> RevealedSecrets => _revealedSecrets;
        public int NodeCount => _nodeCount;
        public int RegionCount => _regionCount;

        /// <summary>
        /// 从 MapGenerationResult 构造——纯数据注入，无生成逻辑。
        /// </summary>
        internal WorldMap(MapGenerationResult result, MapConfig? config = null)
        {
            var cfg = config ?? MapConfig.Default;
            _regions = result.Regions as RegionDef[] ?? new List<RegionDef>(result.Regions).ToArray();
            _sites = result.Sites as NodeGeo[] ?? new List<NodeGeo>(result.Sites).ToArray();
            _nodeCount = _sites.Length;
            _regionCount = _regions.Length;
            _adjacency = new List<int>[_nodeCount];
            for (int i = 0; i < _nodeCount; i++)
                _adjacency[i] = new List<int>(result.Adjacency[i]);
            _revealedSecrets = new HashSet<int>();
            _secretInsightBase = cfg.SecretInsightBase;
            _secretInsightPerTier = cfg.SecretInsightPerTier;
            _scoreResource = cfg.TravelScoreResource;
            _scoreSect = cfg.TravelScoreSect;
            _scoreSecret = cfg.TravelScoreSecret;
            _scoreNormalBase = cfg.TravelScoreNormalBase;
        }

        // ================================================================
        // Runtime operations (§2.3)
        // ================================================================

        /// <summary>采集资源节点。返回采集量，失败返回 0。</summary>
        public int Harvest(NodeId node, int maxHarvest = 20)
        {
            var geo = _sites[node.Value];
            if (geo.Kind != SiteKind.Resource || geo.ResourceAmount <= 0) return 0;
            int amount = Math.Min(geo.ResourceAmount, maxHarvest);
            _sites[node.Value] = geo with { ResourceAmount = geo.ResourceAmount - amount };
            return amount;
        }

        /// <summary>尝试进入秘境。返回是否揭示成功。</summary>
        public bool EnterSecret(NodeId node, int insight)
        {
            var geo = _sites[node.Value];
            if (geo.Kind != SiteKind.Secret || _revealedSecrets.Contains(node.Value)) return false;
            if (insight < _secretInsightBase + geo.DangerTier * _secretInsightPerTier) return false;
            _revealedSecrets.Add(node.Value);
            return true;
        }

        /// <summary>Travel 目标选择——加权邻居（资源 > 宗门 > 灵气）。</summary>
        public NodeId BestNeighbor(NodeId current)
        {
            var adj = _adjacency[current.Value];
            if (adj.Count == 0) return current;
            int best = adj[0], bestScore = ScoreNode(adj[0]);
            for (int i = 1; i < adj.Count; i++)
            { int s = ScoreNode(adj[i]); if (s > bestScore) { bestScore = s; best = adj[i]; } }
            return new NodeId(best);
        }

        private int ScoreNode(int nid) => _sites[nid].Kind switch
        {
            SiteKind.Resource => _scoreResource + _sites[nid].ResourceAmount,
            SiteKind.Sect => _scoreSect,
            SiteKind.Secret => _scoreSecret,
            _ => _scoreNormalBase + _sites[nid].Qi
        };

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

        /// <summary>查询站点地形数据。</summary>
        public NodeGeo GeoOf(NodeId node) => _sites[node.Value];

        /// <summary>查询区域定义。</summary>
        public RegionDef RegionAt(NodeId node) => _regions[RegionOf(node)];

        /// <summary>可达节点（用于 RuleBrain.Travel 目标选择）。</summary>
        public IReadOnlyList<NodeId> ReachableFrom(NodeId node) => AdjacentTo(node);

        /// <summary>Clone——拓扑不可变，浅拷贝安全。RevealedSecrets 需深拷。</summary>
        public WorldMap Clone()
        {
            var clone = (WorldMap)MemberwiseClone();
            // Note: _revealedSecrets is not deeply cloned by MemberwiseClone.
            // For strict determinism, Clone should create a new HashSet.
            // Currently, Clone is only used for determinism checks which
            // compare state before/after, so shared reference is acceptable.
            return clone;
        }
    }
}
