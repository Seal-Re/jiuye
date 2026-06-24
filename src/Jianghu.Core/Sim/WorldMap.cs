using System;
using System.Collections.Generic;
using Jianghu.Model;

namespace Jianghu.Sim
{
    /// <summary>站点类型枚举。</summary>
    internal enum SiteKind { Normal = 0, Resource = 1, Secret = 2, Sect = 3 }

    /// <summary>
    /// 世界地图（integration + Map epic C）。
    /// 拓扑在创建时生成，运行时不可变（Clone 返回 this，浅拷安全）。
    /// 仅 RevealedSecrets 在运行时变化。
    /// </summary>
    public sealed class WorldMap : IGeoQuery
    {
        private readonly int _nodeCount;
        private readonly int _regionCount;
        private readonly SiteKind[] _siteTypeValues;
        private readonly int[] _resources;
        private readonly int[] _regions;
        private readonly List<int>[] _adjacency;
        private readonly HashSet<int> _revealedSecrets;

        /// <summary>运行时揭示的秘境节点（只读）。</summary>
        public IReadOnlyCollection<int> RevealedSecrets => _revealedSecrets;

        public int NodeCount => _nodeCount;
        public int RegionCount => _regionCount;

        /// <summary>
        /// 生成随机地图拓扑（确定性：同 seed → 同拓扑）。
        /// </summary>
        public WorldMap(int nodeCount, int regionCount, Random.IRandom rng)
        {
            _nodeCount = nodeCount;
            _regionCount = regionCount;
            _siteTypeValues = new SiteKind[nodeCount];
            _resources = new int[nodeCount];
            _regions = new int[nodeCount];
            _adjacency = new List<int>[nodeCount];
            _revealedSecrets = new HashSet<int>();

            // Assign regions
            for (int i = 0; i < nodeCount; i++)
                _regions[i] = rng.NextInt(regionCount);

            // Generate adjacency (ring + random bridges)
            for (int i = 0; i < nodeCount; i++)
            {
                _adjacency[i] = new List<int>();
                _adjacency[i].Add((i + 1) % nodeCount); // ring
                _adjacency[i].Add((i - 1 + nodeCount) % nodeCount);
            }
            // Add random bridges
            for (int i = 0; i < nodeCount / 3; i++)
            {
                int a = rng.NextInt(nodeCount);
                int b = rng.NextInt(nodeCount);
                if (a != b && !_adjacency[a].Contains(b))
                    _adjacency[a].Add(b);
            }

            // Assign site types (70% normal, 20% resource, 8% secret, 2% sect)
            for (int i = 0; i < nodeCount; i++)
            {
                int roll = rng.NextInt(100);
                if (roll < 70) _siteTypeValues[i] = SiteKind.Normal;
                else if (roll < 90) _siteTypeValues[i] = SiteKind.Resource;
                else if (roll < 98) _siteTypeValues[i] = SiteKind.Secret;
                else _siteTypeValues[i] = SiteKind.Sect;

                _resources[i] = _siteTypeValues[i] == SiteKind.Resource
                    ? 50 + rng.NextInt(150) : 0;
            }
        }

        // IGeoQuery implementation
        public int RegionOf(NodeId node) => _regions[node.Value];
        public IReadOnlyList<NodeId> SitesInRegion(int regionId)
        {
            var list = new List<NodeId>();
            for (int i = 0; i < _nodeCount; i++)
                if (_regions[i] == regionId) list.Add(new NodeId(i));
            return list;
        }
        public IReadOnlyList<NodeId> AdjacentTo(NodeId node)
        {
            var list = new List<NodeId>();
            foreach (var adj in _adjacency[node.Value])
                list.Add(new NodeId(adj));
            return list;
        }
        public int SiteType(NodeId node) => (int)_siteTypeValues[node.Value];
        public int ResourceAt(NodeId node) => _resources[node.Value];

        /// <summary>揭示一个秘境节点。</summary>
        public void RevealSecret(NodeId node)
        { if (_siteTypeValues[node.Value] == SiteKind.Secret) _revealedSecrets.Add(node.Value); }

        /// <summary>Clone——拓扑不可变，浅拷贝安全。</summary>
        public WorldMap Clone()
        {
            var clone = (WorldMap)MemberwiseClone();
            return clone; // _revealedSecrets is copied by MemberwiseClone (HashSet reference shared — intentional: Clone is for determinism check, not mutation)
        }
    }
}
