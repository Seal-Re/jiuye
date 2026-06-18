using System;
using System.Collections.Generic;
using System.Linq;
using Jianghu.Config;
using Jianghu.Cultivation;
using Jianghu.Cultivation.Paths;
using Jianghu.Model;
using Jianghu.Stats;
using Xunit;
using Xunit.Abstractions;

namespace Jianghu.Core.Tests.Cultivation
{
    /// <summary>
    /// balance-cross harness (§4): PowerMatrix dump + Duel sim + Convergence report.
    /// Read-only diagnostic → gate assertions after calibration.
    /// 典型角色构造: 中庸四维(各20), realm=各UT段首, 标准loadout, Initial资源, 空flags.
    /// </summary>
    public class BalanceCrossHarness
    {
        readonly ITestOutputHelper _out;
        public BalanceCrossHarness(ITestOutputHelper output) { _out = output; }

        static readonly LimitsConfig Limits = LimitsConfig.Default;

        /// <summary>All 21 path definitions.</summary>
        static readonly CultivationPathDef[] AllPaths =
        {
            SwordImmortalPath.Def, FaXiuPath.Def, LeiXiuPath.Def,
            XueXiuXueshaPath.Def, YaoXiuHuaxingPath.Def, GhostYangHunPath.Def,
            MoXiuXinmoPath.Def, BodyHenglianPath.Def, BuddhistGoldenBodyPath.Def,
            SoulDivineSensePath.Def, YuShouPath.Def, RuXiuHaoranPath.Def,
            YinguoFazePath.Def, DuGuXiuPath.Def, YinXiuYuedaoPath.Def,
            KuileiShiPath.Def, MingFateCausalityPath.Def,
            QixiuArtificerPath.Def, ArrayFormationPath.Def,
            DanXiuPath.Def, FuXiuFuluPath.Def,
        };

        // ================================================================
        // 1. PowerMatrix dump — (path, UT, BaseSum, mul, power) for all UTs
        // ================================================================

        [Fact]
        public void PowerMatrix_Dump_AllPaths_AllUTs()
        {
            _out.WriteLine($"{"Path",-24} {"UT",4} {"RealmIdx",8} {"BaseSum",8} {"mul",6} {"power",8}");
            _out.WriteLine(new string('-', 70));

            foreach (var path in AllPaths)
            {
                // Collect unique UTs for this path
                var uts = path.Curve.UnifiedTierOf.Distinct().OrderBy(u => u).ToList();
                foreach (int ut in uts)
                {
                    int realmIdx = FirstRealmAtUT(path, ut);
                    var st = MakeTypicalRole(path, realmIdx);
                    int pe = PowerEngine.Evaluate(st, MakeStats(), path, Limits);

                    _out.WriteLine(
                        $"{path.PathId,-24} {ut,4} {realmIdx,8} {BaseSum(path, st),8} {path.Curve.RealmMultipliers[realmIdx],6} {pe,8}");
                }
            }

            // Assertion: all paths produce positive power at UT0
            foreach (var path in AllPaths)
            {
                int ut0Idx = FirstRealmAtUT(path, 0);
                var st = MakeTypicalRole(path, ut0Idx);
                int pe = PowerEngine.Evaluate(st, MakeStats(), path, Limits);
                Assert.True(pe > 0, $"{path.PathId} UT0 power should be positive, got {pe}");
            }
        }

        // ================================================================
        // 2. Spread analysis — quantify the real power spread at each UT
        // ================================================================

        [Fact]
        public void PowerSpread_PerUT_Quantified()
        {
            // Collect all unique UTs across all paths
            var allUTs = AllPaths.SelectMany(p => p.Curve.UnifiedTierOf).Distinct().OrderBy(u => u).ToList();

            foreach (int ut in allUTs)
            {
                var powers = new List<(string PathId, int Power)>();
                foreach (var path in AllPaths)
                {
                    if (!path.Curve.UnifiedTierOf.Contains(ut)) continue;
                    int realmIdx = FirstRealmAtUT(path, ut);
                    var st = MakeTypicalRole(path, realmIdx);
                    int pe = PowerEngine.Evaluate(st, MakeStats(), path, Limits);
                    powers.Add((path.PathId, pe));
                }

                if (powers.Count < 2) continue;

                int min = powers.Min(p => p.Power);
                int max = powers.Max(p => p.Power);
                double spread = max / (double)min;

                _out.WriteLine($"UT {ut,2}: {powers.Count,2} paths, min={min,6}, max={max,6}, spread={spread:F1}x");

                // Diagnostic: flag spread > 10x as severe
                if (spread > 10)
                {
                    var minPath = powers.First(p => p.Power == min);
                    var maxPath = powers.First(p => p.Power == max);
                    _out.WriteLine($"  ⚠ SEVERE SPREAD: {minPath.PathId}({min}) vs {maxPath.PathId}({max})");
                }
            }
        }

        // ================================================================
        // 3. Combat vs Auxiliary classification (decision D3)
        // ================================================================

        [Fact]
        public void CombatAuxiliary_Classification()
        {
            // Use 剑修 power at mid-UT as combat baseline
            var swordPath = SwordImmortalPath.Def;
            int midUT = swordPath.Curve.UnifiedTierOf[swordPath.Curve.UnifiedTierOf.Count / 2];
            int midIdx = FirstRealmAtUT(swordPath, midUT);
            var swordSt = MakeTypicalRole(swordPath, midIdx);
            int swordPower = PowerEngine.Evaluate(swordSt, MakeStats(), swordPath, Limits);

            _out.WriteLine($"剑修 baseline @ UT{midUT}: {swordPower}");
            _out.WriteLine($"战斗路下限 (50%): {swordPower / 2}");
            _out.WriteLine("");

            foreach (var path in AllPaths)
            {
                int maxUT = path.Curve.UnifiedTierOf.Max();
                int maxIdx = FirstRealmAtUT(path, maxUT);
                var st = MakeTypicalRole(path, maxIdx);
                int power = PowerEngine.Evaluate(st, MakeStats(), path, Limits);

                double ratio = (double)power / swordPower;
                string classification = ratio >= 0.5 ? "战斗路" : "辅助路";

                _out.WriteLine($"{path.PathId,-24} maxUT={maxUT,2} topPower={power,6} vsSword={ratio:F2} → {classification}");
            }
        }

        // ================================================================
        // Helpers — 典型角色构造
        // ================================================================

        /// <summary>中庸四维 Σ=80，各20。</summary>
        static StatBlock MakeStats() => new StatBlock(new[] { 20, 20, 20, 20 });

        /// <summary>构造 path 在给定 realmIdx 的典型角色。</summary>
        static CultivationState MakeTypicalRole(CultivationPathDef path, int realmIdx)
        {
            var st = CultivationState.NewForPath(path.PathId, path.Resources);
            st.RealmIndex = realmIdx;

            // 标准loadout: each ArtCategory picks PickMin arts, choose median tier
            var chosenArts = new List<string>();
            foreach (var cat in path.ArtCategories)
            {
                if (cat.Role == "daoheart") continue; // daoHeart不进战力
                int pick = cat.PickMin;
                var sorted = cat.Arts.OrderBy(a => a.Tier).ToList();
                int startIdx = Math.Max(0, (sorted.Count - pick) / 2); // median
                for (int i = 0; i < pick && startIdx + i < sorted.Count; i++)
                    chosenArts.Add(sorted[startIdx + i].Id);
            }

            // Re-create with chosenArts
            var newSt = CultivationState.NewForPath(path.PathId, path.Resources, chosenArts,
                Array.Empty<string>());
            newSt.RealmIndex = realmIdx;
            return newSt;
        }

        /// <summary>Find first realm index that maps to the given UT.</summary>
        static int FirstRealmAtUT(CultivationPathDef path, int ut)
        {
            for (int i = 0; i < path.Curve.UnifiedTierOf.Count; i++)
                if (path.Curve.UnifiedTierOf[i] == ut)
                    return i;
            return path.Curve.UnifiedTierOf.Count - 1; // fallback
        }

        /// <summary>Compute BaseSum for a typical role (段1 only, before mul/postMul).</summary>
        static long BaseSum(CultivationPathDef path, CultivationState st)
        {
            long sum = 0;
            var stats = MakeStats();
            foreach (var term in path.Power.Terms)
            {
                int weight = term.Weight;
                if (term.WeightStepKey != null && st.Flags.TryGetValue(term.WeightStepKey, out int step))
                    weight += step;
                long src = PowerEngine.Resolve(term.Src, st, stats);
                sum += (long)weight * src;
            }
            return sum;
        }
    }
}
