using System;
using System.Collections.Generic;
using System.IO;
using Jianghu.Cultivation;
using Jianghu.Config;
using Jianghu.Model;
using Jianghu.Sim;
using Jianghu.Stats;
using Xunit;
using Xunit.Abstractions;

namespace Jianghu.Core.Tests.Cultivation
{
    /// <summary>
    /// Diagnostic: dump PE per combat path at UT=8 for RealmMultipliers recalibration.
    /// NOT a gate test — diagnostic only. Remove after balance-003 completion.
    /// </summary>
    public class PeDiagnosticTests
    {
        readonly ITestOutputHelper _out;
        public PeDiagnosticTests(ITestOutputHelper output) { _out = output; }

        [Fact]
        public void DumpPE_UT8_CombatPaths()
        {
            var allPaths = new CodePathSource().Load();
            var auxSet = new HashSet<string> { "dan_xiu", "array_formation", "qixiu_artificer" };
            int ut = 8;
            var results = new List<(string PathId, int PE)>();

            foreach (var path in allPaths)
            {
                if (auxSet.Contains(path.PathId)) continue;

                // Check if path can reach UT8
                bool canReach = false;
                foreach (var tier in path.Curve.UnifiedTierOf)
                    if (tier == ut) { canReach = true; break; }
                if (!canReach) continue;

                // Create typical char at UT8
                var ch = WorldFactory.CreateTypicalChar(path.PathId, ut, path, path.PathId.GetHashCode());
                if (ch?.Cultivation == null) continue;

                int pe = PowerEngine.Evaluate(ch.Cultivation, ch.Stats, path, LimitsConfig.Default);
                results.Add((path.PathId, pe));
            }

            // Sort by PE descending
            results.Sort((a, b) => b.PE.CompareTo(a.PE));

            // Dump
            _out.WriteLine($"UT={ut} PE per combat path (post module-cap):");
            _out.WriteLine("PathId,PE");
            foreach (var (pid, pe) in results)
                _out.WriteLine($"{pid},{pe}");

            // Identify sword_immortal as baseline
            var sword = results.Find(r => r.PathId == "sword_immortal");
            if (sword.PathId != null)
            {
                _out.WriteLine($"\nBaseline (sword_immortal): PE={sword.PE}");
                _out.WriteLine("Deviations >15% from baseline:");
                foreach (var (pid, pe) in results)
                {
                    int deviation = sword.PE > 0
                        ? Math.Abs(pe - sword.PE) * 100 / sword.PE
                        : 0;
                    if (deviation > 15)
                        _out.WriteLine($"  {pid}: PE={pe} ({deviation}% off baseline)");
                }
            }
        }
    }
}
