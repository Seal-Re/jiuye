using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Jianghu.Config;
using Jianghu.Cultivation;
using Jianghu.Model;
using Jianghu.Sim;
using Jianghu.Stats;
using Xunit;
using Xunit.Abstractions;

namespace Jianghu.Core.Tests.Cultivation
{
    /// <summary>
    /// balance-001: BalanceMatrixDump harness tests.
    /// 全21路x9UT(0,2,4,6,8,9,10,11,12)=189 cell战力矩阵dump。
    /// 只dump不修——数据供balance-002收敛迭代。
    /// Red lines: B.2(禁浮点), B.3(off逐字节), B.5(道心不进pe)。
    /// </summary>
    public class BalanceMatrixDumpTests
    {
        readonly ITestOutputHelper _out;
        public BalanceMatrixDumpTests(ITestOutputHelper output) { _out = output; }

        static readonly LimitsConfig Limits = LimitsConfig.Default;
        static readonly int[] TargetUTs = { 0, 2, 4, 6, 8, 9, 10, 11, 12 };
        static readonly CultivationPathDef[] AllPaths;

        static BalanceMatrixDumpTests()
        {
            AllPaths = new CodePathSource().Load().ToArray();
        }

        // ================================================================
        // Helpers
        // ================================================================

        /// <summary>检查某路在某UT是否可达。</summary>
        static bool IsUTReachable(CultivationPathDef path, int ut)
        {
            for (int i = 0; i < path.Curve.UnifiedTierOf.Count; i++)
                if (path.Curve.UnifiedTierOf[i] == ut)
                    return true;
            return false;
        }

        /// <summary>宽矩阵CSV: header=PathId,UT=0,...UT=12; 每行一路。不可达UT="N/A"。</summary>
        static string BuildWideMatrixCsv(CultivationPathDef[] paths, int[] uts)
        {
            var sb = new System.Text.StringBuilder();
            // Header
            sb.Append("PathId");
            foreach (int ut in uts)
                sb.Append(",UT=" + ut);
            sb.AppendLine();
            // Data rows
            foreach (var path in paths)
            {
                sb.Append(path.PathId);
                foreach (int ut in uts)
                {
                    if (!IsUTReachable(path, ut))
                    {
                        sb.Append(",N/A");
                        continue;
                    }
                    var ch = WorldFactory.CreateTypicalChar(path.PathId, ut, path, id: 0);
                    int pe = PowerEngine.Evaluate(ch.Cultivation!, ch.Stats, path, Limits);
                    sb.Append("," + pe);
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }

        /// <summary>长明细CSV: PathId,UT,RealmIdx,PE,Mean,Min,Max。不可达UT行PE="N/A"。</summary>
        static string BuildDetailCsv(CultivationPathDef[] paths, int[] uts)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("PathId,UT,RealmIdx,PE,Mean,Min,Max");
            foreach (var path in paths)
            {
                foreach (int ut in uts)
                {
                    if (!IsUTReachable(path, ut))
                    {
                        sb.AppendLine(path.PathId + "," + ut + ",N/A,N/A,N/A,N/A,N/A");
                        continue;
                    }
                    var ch = WorldFactory.CreateTypicalChar(path.PathId, ut, path, id: 0);
                    int pe = PowerEngine.Evaluate(ch.Cultivation!, ch.Stats, path, Limits);
                    int realmIdx = ch.Cultivation!.RealmIndex;
                    // K=1: mean=pe, min=max=pe (balance-002 adds variance)
                    sb.AppendLine(path.PathId + "," + ut + "," + realmIdx + "," + pe + "," + pe + "," + pe + "," + pe);
                }
            }
            return sb.ToString();
        }

        /// <summary>写入CSV到文件并输出到test output。</summary>
        void WriteCsv(string filename, string content)
        {
            string dir = Directory.GetCurrentDirectory();
            string filepath = Path.Combine(dir, filename);
            File.WriteAllText(filepath, content);
            _out.WriteLine("CSV written to: " + filepath);
            _out.WriteLine(content);
        }

        // ================================================================
        // Test 1: 21x9=189 cells complete (including N/A for unreachable)
        // ================================================================

        [Fact]
        public void Matrix_Has_189_Cells_AllPaths_AllUTs()
        {
            int expectedCells = 21 * 9; // 189
            int actualCells = 0;
            foreach (var path in AllPaths)
            {
                foreach (int ut in TargetUTs)
                {
                    actualCells++;
                }
            }
            Assert.Equal(expectedCells, actualCells);

            // Verify: every (path, UT) pair either is reachable and has valid PE,
            // or is unreachable and marked N/A.
            var csv = BuildDetailCsv(AllPaths, TargetUTs);
            var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            // Header + 189 data rows = 190 lines
            Assert.Equal(190, lines.Length);
            WriteCsv("balance_matrix_detail.csv", csv);

            _out.WriteLine("Total cells: " + actualCells + " (expected " + expectedCells + ")");
            _out.WriteLine("CSV rows (incl header): " + lines.Length);
        }

        // ================================================================
        // Test 2: Deterministic -- same paths -> identical CSV
        // ================================================================

        [Fact]
        public void Matrix_Deterministic_SameSeed_SameOutput()
        {
            var csv1 = BuildWideMatrixCsv(AllPaths, TargetUTs);
            var csv2 = BuildWideMatrixCsv(AllPaths, TargetUTs);

            Assert.Equal(csv1, csv2);
            _out.WriteLine("Deterministic: 2 runs produced identical output.");
            _out.WriteLine("CSV length: " + csv1.Length + " chars");
        }

        // ================================================================
        // Test 3: Typical char PE > 0 for all reachable (path, UT)
        // ================================================================

        [Fact]
        public void TypicalChar_PE_Positive_AllPaths_AllReachableUTs()
        {
            int tested = 0;
            int failures = 0;
            foreach (var path in AllPaths)
            {
                foreach (int ut in TargetUTs)
                {
                    if (!IsUTReachable(path, ut)) continue;
                    var ch = WorldFactory.CreateTypicalChar(path.PathId, ut, path, id: 0);
                    int pe = PowerEngine.Evaluate(ch.Cultivation!, ch.Stats, path, Limits);
                    tested++;
                    if (pe <= 0)
                    {
                        failures++;
                        _out.WriteLine("FAIL: " + path.PathId + " @ UT" + ut + " PE=" + pe + " (non-positive)");
                    }
                }
            }
            _out.WriteLine("Tested " + tested + " reachable cells, " + failures + " failures");
            Assert.True(failures == 0, failures + " cells have non-positive PE");
        }

        // ================================================================
        // Test 4: CSV format is valid and parsable
        // ================================================================

        [Fact]
        public void DumpCSV_Format_Parsable_And_Consistent()
        {
            var wide = BuildWideMatrixCsv(AllPaths, TargetUTs);

            // Write wide matrix to file
            WriteCsv("balance_matrix_wide.csv", wide);

            // Parse and validate
            var lines = wide.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            Assert.Equal(22, lines.Length); // 1 header + 21 path rows

            // Header check (TrimEnd('\r') for Windows \r\n line endings)
            var header = lines[0].TrimEnd('\r').Split(',');
            Assert.Equal("PathId", header[0]);
            for (int i = 0; i < TargetUTs.Length; i++)
                Assert.Equal("UT=" + TargetUTs[i], header[i + 1]);
            Assert.Equal(10, header.Length); // PathId + 9 UT columns

            // Each data row: first column is a valid pathId, remaining are int or "N/A"
            var knownIds = new HashSet<string>(AllPaths.Select(p => p.PathId));
            foreach (var line in lines.Skip(1))
            {
                var cols = line.TrimEnd('\r').Split(',');
                Assert.Equal(10, cols.Length);
                Assert.True(knownIds.Contains(cols[0]), "Unknown pathId in CSV: " + cols[0]);
                for (int i = 1; i < cols.Length; i++)
                {
                    string cell = cols[i];
                    bool valid = cell == "N/A" || int.TryParse(cell, out _);
                    Assert.True(valid, "Invalid cell value at " + cols[0] + "/UT=" + TargetUTs[i - 1] + ": '" + cell + "'");
                }
            }

            _out.WriteLine("CSV format valid: 22 lines, 10 columns, all cells int or N/A");

            // Also verify detail CSV
            var detail = BuildDetailCsv(AllPaths, TargetUTs);
            var detailLines = detail.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            Assert.Equal(190, detailLines.Length); // 1 header + 189 rows
            _out.WriteLine("Detail CSV format valid: 190 lines");
        }

        // ================================================================
        // Test 5: UT=0 (mortal) has defined PE for all 21 paths
        // ================================================================

        [Fact]
        public void UT0_Mortal_PE_Defined_AllPaths()
        {
            int tested = 0;
            foreach (var path in AllPaths)
            {
                // UT=0 must be reachable (all paths start at UT0 or earlier)
                Assert.True(IsUTReachable(path, 0),
                    path.PathId + " should have UT=0 reachable");

                var ch = WorldFactory.CreateTypicalChar(path.PathId, 0, path, id: 0);
                int pe = PowerEngine.Evaluate(ch.Cultivation!, ch.Stats, path, Limits);
                tested++;

                Assert.True(pe > 0,
                    path.PathId + " UT=0 PE should be > 0, got " + pe);

                _out.WriteLine(string.Format("{0,-24} UT=0 realmIdx={1,2} PE={2,6}",
                    path.PathId, ch.Cultivation!.RealmIndex, pe));
            }
            _out.WriteLine("All " + tested + " paths have positive PE at UT=0");
        }

        // ================================================================
        // Test 6: Off mode -- cultivation=false -> null CultivationState, no crash
        // ================================================================

        [Fact]
        public void OffMode_TypicalChars_HaveNullCultivation_NoCrash()
        {
            // Create a World in off mode (cultivation=false)
            var limits = LimitsConfig.Default;
            var world = WorldFactory.CreateInitial(seed: 42, limits: limits, initialCount: 5,
                cultivation: false);

            // All spawned characters should have null CultivationState
            var alive = world.AliveCharacters();
            Assert.NotEmpty(alive);
            foreach (var ch in alive)
            {
                Assert.Null(ch.Cultivation);
                // Accessing Cultivation should not crash
                var cs = ch.Cultivation;
                Assert.Null(cs);
                // StatBlock still accessible
                Assert.NotNull(ch.Stats);
            }

            // Verify: accessing Cultivation?.PathId is safe (no NRE)
            foreach (var ch in alive)
            {
                string path = ch.Cultivation?.PathId ?? "off";
                Assert.Equal("off", path);
            }

            _out.WriteLine("Off mode: " + alive.Count + " characters, all Cultivation=null, no crashes");
        }

        // ================================================================
        // Bonus: Cross-check -- K=2 path pairs at same UT, run duels
        // (Story AC 1.5: random sample K pairs at same UT, record win rates)
        // ================================================================

        [Fact]
        public void CrossCheck_UT8_PairDuels_AtLeastTwoPairs()
        {
            int testUT = 8;
            // Collect paths that reach UT8
            var combatPaths = new List<CultivationPathDef>();
            foreach (var path in AllPaths)
            {
                if (IsUTReachable(path, testUT))
                {
                    var ch = WorldFactory.CreateTypicalChar(path.PathId, testUT, path, id: 0);
                    int pe = PowerEngine.Evaluate(ch.Cultivation!, ch.Stats, path, Limits);
                    // Combat path threshold: PE >= 400 at UT8
                    if (pe >= 400)
                        combatPaths.Add(path);
                }
            }
            Assert.True(combatPaths.Count >= 2,
                "Need at least 2 combat paths at UT" + testUT + ", got " + combatPaths.Count);

            var reg = new PathRegistry(new CodePathSource());

            // Pick 3 representative pairs for cross-duel
            var pairs = new (string, string)[]
            {
                ("sword_immortal", "fa_xiu"),
                ("sword_immortal", "du_gu_xiu"),
                ("lei_xiu", "gui_xiu_yang_hun"),
            };

            int pairCount = 0;
            foreach (var (aId, bId) in pairs)
            {
                var pathA = AllPaths.FirstOrDefault(p => p.PathId == aId);
                var pathB = AllPaths.FirstOrDefault(p => p.PathId == bId);
                if (pathA == null || pathB == null) continue;
                if (!IsUTReachable(pathA, testUT) || !IsUTReachable(pathB, testUT)) continue;

                var chA = WorldFactory.CreateTypicalChar(aId, testUT, pathA, id: 1);
                var chB = WorldFactory.CreateTypicalChar(bId, testUT, pathB, id: 2);

                int peA = PowerEngine.Evaluate(chA.Cultivation!, chA.Stats, pathA, Limits);
                int peB = PowerEngine.Evaluate(chB.Cultivation!, chB.Stats, pathB, Limits);

                var result = DuelEngine.ResolveR2(chA, chB, pathA, pathB, reg, Limits, null, null, null);

                int winnerPE = result.Winner == chA.Id ? peA : peB;
                long marginPct = winnerPE > 0 ? (long)result.Margin * 100 / winnerPE : 0;

                _out.WriteLine(aId + "(PE=" + peA + ") vs " + bId + "(PE=" + peB + "): " +
                    "winner=" + result.Winner.Value + " margin=" + result.Margin + " margin%=" + marginPct + "% " +
                    "autoWin=" + result.WasAutoWin);

                // Diagnostic only: log PE spread >30% for future balance-002 convergence
                if (marginPct > 30)
                    _out.WriteLine("  ⚠ PE spread: " + aId + " vs " + bId + " margin=" + marginPct + "% (>30%, needs balance-002 convergence)");
                // balance-001 is dump-only, not a gate — do not assert on spread

                pairCount++;
            }

            Assert.True(pairCount >= 2, "Need at least 2 valid cross-duel pairs, got " + pairCount);
            _out.WriteLine("Cross-check: " + pairCount + " valid cross-duel pairs at UT" + testUT);
        }
    }
}
