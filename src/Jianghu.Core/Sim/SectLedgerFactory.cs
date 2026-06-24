using System;

namespace Jianghu.Sim
{
    /// <summary>
    /// 门派工厂——从配置+生成器构造 SectLedger。
    /// 加生成算法 = 加 IFactionGenerator 实现 + 注册到此工厂。
    /// 灾备：生成失败回退到空账本（散修江湖），不崩溃。
    /// </summary>
    public static class SectLedgerFactory
    {
        public static IFactionGenerator Generator { get; set; } = new DefaultFactionGenerator();

        /// <summary>创建门派账本。灾备 = 空账本（纯散修）→ 不崩溃。</summary>
        public static SectLedger Create(FactionConfig config, Random.IRandom rng, int nodeCount)
        {
            try
            {
                var result = Generator.Generate(config, rng, nodeCount);
                var ledger = new SectLedger();

                // Register factions
                foreach (var f in result.Factions)
                    ledger.RegisterFaction(f);

                // Set alignment-based relations
                var factions = result.Factions;
                for (int i = 0; i < factions.Count; i++)
                {
                    for (int j = i + 1; j < factions.Count; j++)
                    {
                        int alignA = factions[i].AlignmentAxis;
                        int alignB = factions[j].AlignmentAxis;

                        int relation = alignA == alignB ? (int)FactionRelationKind.Ally
                            : (alignA * alignB == -1) ? (int)FactionRelationKind.Enemy
                            : (int)FactionRelationKind.Neutral;

                        ledger.SetRelation(factions[i].Id, factions[j].Id, relation);
                        ledger.SetRelation(factions[j].Id, factions[i].Id, relation);
                    }
                }

                return ledger;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Faction generation failed ({Generator.Name}): {ex.Message}. Fallback: empty ledger.");
                return new SectLedger(); // 空账本 = 纯散修，江湖仍可运转
            }
        }

        /// <summary>使用指定生成器创建（跳过灾备）。</summary>
        public static SectLedger CreateWith(IFactionGenerator generator, FactionConfig config,
            Random.IRandom rng, int nodeCount)
        {
            var prev = Generator;
            Generator = generator;
            try { return Create(config, rng, nodeCount); }
            finally { Generator = prev; }
        }
    }
}
