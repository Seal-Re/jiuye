using System;
using System.Collections.Generic;
using System.Linq;
using Jianghu.Config;
using Jianghu.Cultivation;
using Jianghu.Stats;
using Xunit;
using Xunit.Abstractions;

namespace Jianghu.Core.Tests.Cultivation
{
    /// <summary>
    /// 阶段5 B5 标定 harness 件1：PowerMatrix dump（设计 §4.1）。
    /// 21 路 × 各 UT 典型角色（中庸 Σ=80 + UT 段首 realm + 标准 loadout）战力矩阵 → 揭示同 UT spread。
    /// 诊断性：ITestOutputHelper 输出 + 松断言（power≥0）。run：
    ///   dotnet test --filter Name~Dump_PowerMatrix --logger "console;verbosity=detailed"
    /// </summary>
    public class BalanceMatrixDumpTests
    {
        private readonly ITestOutputHelper _out;
        public BalanceMatrixDumpTests(ITestOutputHelper o) => _out = o;

        static StatBlock Mid() => new StatBlock(new[] { 20, 20, 20, 20 }); // 中庸 Σ=80

        // 标准 loadout：每类目（含 daoheart，PathAssigner 亦选）取前 PickMin 个 art id。
        static string[] StdLoadout(CultivationPathDef def) =>
            def.ArtCategories.SelectMany(c => c.Arts.Take(c.PickMin).Select(a => a.Id)).ToArray();

        [Fact]
        public void Dump_PowerMatrix_AllPaths_AllUT()
        {
            var limits = LimitsConfig.Default;
            var paths = new CodePathSource().Load();

            // UT → [(路名, power, mul, baseSum近似)]
            var byUT = new SortedDictionary<int, List<(string name, int power, int mul, long baseApprox)>>();

            foreach (var def in paths)
            {
                var loadout = StdLoadout(def);
                for (int fi = 0; fi < def.Curve.UnifiedTierOf.Count; fi++)
                {
                    var (major, sub) = RealmProjection.Decode(fi, def.Curve.SubLevelCount);
                    if (sub != 0) continue; // 仅取每大境界段首 = 每 UT 一代表

                    int ut = def.Curve.UnifiedTierOf[fi];
                    var st = CultivationState.NewForPath(def.PathId, def.Resources, loadout, Array.Empty<string>());
                    st.RealmIndex = fi;

                    int power = PowerEngine.Evaluate(st, Mid(), def, limits);
                    int mul = def.Curve.RealmMultipliers[fi];
                    long baseApprox = mul > 0 ? (long)power * 10 / mul : 0; // 近似（忽略 mods/postmuls/clamp）

                    Assert.True(power >= 0, $"{def.PathId} fi{fi} power<0");
                    if (!byUT.TryGetValue(ut, out var lst)) byUT[ut] = lst = new List<(string, int, int, long)>();
                    lst.Add((def.Name, power, mul, baseApprox));
                }
            }

            _out.WriteLine("=== B5 PowerMatrix：21 路 × UT 典型角色(中庸 Σ=80, 段首 realm, 标准 loadout) 战力 ===");
            _out.WriteLine("（列：power | 路 | mul | baseSum近似=power*10/mul）\n");
            foreach (var kv in byUT)
            {
                int ut = kv.Key;
                var lst = kv.Value;
                lst.Sort((a, b) => a.power.CompareTo(b.power));
                int min = lst.Min(x => x.power), max = lst.Max(x => x.power);
                string spread = min > 0 ? $"{(double)max / min:F1}x" : "min=0";
                _out.WriteLine($"── UT{ut}  （{lst.Count} 路，spread {max}/{min} = {spread}）");
                foreach (var x in lst)
                    _out.WriteLine($"   {x.power,7} | {x.name,-16} | mul={x.mul,4} | base≈{x.baseApprox}");
                _out.WriteLine("");
            }
        }
    }
}
