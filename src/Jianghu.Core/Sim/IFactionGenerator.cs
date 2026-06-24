using System;
using System.Collections.Generic;
using Jianghu.Model;

namespace Jianghu.Sim
{
    /// <summary>
    /// 门派生成器接口——可插拔生成算法。
    /// 新生成器 = 新实现 + 注册到 SectLedgerFactory。
    /// </summary>
    public interface IFactionGenerator
    {
        string Name { get; }
        FactionGenerationResult Generate(FactionConfig config, Random.IRandom rng, int nodeCount);
    }

    /// <summary>门派生成结果——纯数据。</summary>
    public sealed record FactionGenerationResult(
        IReadOnlyList<FactionDef> Factions,
        IReadOnlyDictionary<int, int> HomeRegions  // factionId → regionId
    );

    // ================================================================
    // DefaultFactionGenerator — 基础生成器
    // ================================================================

    /// <summary>默认门派生成器——加权对齐分布 + 区域种子 + 随机成员。</summary>
    public sealed class DefaultFactionGenerator : IFactionGenerator
    {
        public string Name => "DefaultFaction";

        public FactionGenerationResult Generate(FactionConfig config, Random.IRandom rng, int nodeCount)
        {
            config.Validate();

            int count = rng.NextInt(config.FactionCountMax - config.FactionCountMin + 1) + config.FactionCountMin;
            var names = config.FactionNamesOrDefault;
            var factions = new List<FactionDef>();
            var homeRegions = new Dictionary<int, int>();

            for (int i = 0; i < count; i++)
            {
                // Alignment: weighted by config
                int roll = rng.NextInt(100);
                int alignment = roll < config.WeightRighteous ? 1
                    : roll < config.WeightRighteous + config.WeightNeutral ? 0
                    : -1;

                int homeRegion = rng.NextInt(Math.Max(1, nodeCount / 3)); // simplified region mapping
                var homeSite = new NodeId(homeRegion * 3 + rng.NextInt(3));

                var faction = new FactionDef(
                    i + 1,
                    names[i % names.Count],
                    homeRegion,
                    homeSite,
                    alignment,
                    Array.Empty<string>()  // entry requirements deferred
                );

                factions.Add(faction);
                homeRegions[i + 1] = homeRegion;
            }

            // Alignment-based relations: righteous vs evil → enemy, same alignment → ally
            for (int i = 0; i < factions.Count; i++)
            {
                for (int j = i + 1; j < factions.Count; j++)
                {
                    int alignA = factions[i].AlignmentAxis;
                    int alignB = factions[j].AlignmentAxis;

                    int relation = alignA == alignB ? (int)FactionRelationKind.Ally
                        : (alignA * alignB == -1) ? (int)FactionRelationKind.Enemy
                        : (int)FactionRelationKind.Neutral;

                    // Store bidirectional in ledger after creation — factory just returns defs
                }
            }

            return new FactionGenerationResult(factions, homeRegions);
        }
    }
}
