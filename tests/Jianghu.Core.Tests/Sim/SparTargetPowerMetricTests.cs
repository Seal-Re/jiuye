using System.Collections.Generic;
using System.Linq;
using Jianghu.Config;
using Jianghu.Cultivation;
using Jianghu.Decide;
using Jianghu.Model;
using Jianghu.Sim;
using Jianghu.Core.Tests.Cultivation; // TestPaths
using Xunit;
using Xunit.Abstractions;

namespace Jianghu.Core.Tests.Sim
{
    /// <summary>
    /// 切磋战力度量门（2026-07-03 用户诊断 Step 3 机制修复）：决策层与战斗层战力度量须一致。
    /// 缺陷：World.BuildContext 原用 raw stats(Force×2+Int+Con) 填 NearbyActor.Power，无视修为境界；
    /// 但 DuelEngine 用 PowerEngine.Evaluate(含 realm 倍率)。→ brain 对修为盲，把 999 碾压当"势均力敌"反复切磋。
    /// 修复：on 模式(registry≠null 且 other 有修为)用 PE 度量；off 保持 raw stats(B.3 逐字节)。
    /// **这是 Viability-adjacent 机制修复(决策/战斗度量对齐)，非 Fairness 数值调优(999 本身是 UT-gap auto-win 正确设计)。**
    /// Red lines: B.2(整数确定性), B.3(off 逐字节——本门仅 on)。
    /// </summary>
    public class SparTargetPowerMetricTests
    {
        private readonly ITestOutputHelper _out;
        public SparTargetPowerMetricTests(ITestOutputHelper output) { _out = output; }

        // 单路 mock（低阈速破境），两角色同路 → PE 差纯由 realm 决定。
        static IPathSource SinglePathSource()
        {
            var p = TestPaths.ValidFull() with
            {
                PathId = "fa_xiu",
                Curve = new RealmCurveDef(
                    new[] { 10, 50, 200 },          // realm 倍率：realm0=×1.0, realm2=×20 → PE 天差地别
                    new[] { 0, 2, 4 },
                    new[] { "炼气", "金丹", "元婴" },
                    new[] { 0, 3, 6 },
                    new[] { 1, 1, 1 }, true, 2),
                EntryGate = new EntryGateDef("tag:spirit_root"),
            };
            return new ListPathSource(new CultivationPathDef[] { p });
        }

        // —— on 模式：同 stats、不同 realm 的两个同节点角色，BuildContext 的 Power 应反映 PE(修为)差距，
        //    而非 raw-stats 的近似相等。当前实现返 raw stats → 两者 Power 近似 → RED。——
        [Fact]
        public void test_buildcontext_power_reflects_cultivation_not_rawstats()
        {
            var w = WorldFactory.CreateInitial(2026, LimitsConfig.Default, 5,
                cultivation: true, pathSource: SinglePathSource());

            // 取两个同节点在世修士。
            var cults = w.AliveCharacters().Where(c => c.Cultivation != null).ToList();
            Assert.True(cults.Count >= 2, "需 ≥2 修士");
            var a = cults[0];
            // 找与 a 同节点的另一人。
            var b = cults.FirstOrDefault(c => c.Id.Value != a.Id.Value && c.Node.Value == a.Node.Value);
            if (b == null)
            {
                // 若不同节点，把 b 挪到 a 节点（测试可控）。
                b = cults.First(c => c.Id.Value != a.Id.Value);
                b.Node = a.Node;
            }

            // 拉开修为：a 停 realm0，b 顶到 realm2（同 stats 下 PE 因倍率 ×1 vs ×20 天差地别）。
            a.Cultivation!.RealmIndex = 0;
            b.Cultivation!.RealmIndex = 2;

            var ctx = w.BuildContext(a);
            var bNearby = ctx.Nearby.First(n => n.Id.Value == b.Id.Value);

            // a 的自视 PE（realm0）。
            var reg = new PathRegistry(SinglePathSource());
            int aPe = PowerEngine.Evaluate(a.Cultivation!, a.Stats, reg.ById(a.Cultivation!.PathId), LimitsConfig.Default);
            _out.WriteLine($"a(realm0) PE≈{aPe}; b(realm2) ctx.Power={bNearby.Power}");

            // 修复后：b 的 ctx.Power 应 ≈ b 的 PE（realm2 ×20），远高于 a 的 PE →
            // brain 能识别悬殊、避免无意义切磋。断言 b.Power 至少是 a PE 的 3 倍（realm 差距体现）。
            Assert.True(bNearby.Power >= aPe * 3,
                $"BuildContext 战力度量对修为盲：b(realm2) Power={bNearby.Power} 未反映 PE 倍率差（应远超 a realm0 PE≈{aPe}）。" +
                "brain 因此把碾压当势均力敌反复切磋。");
        }

        sealed class ListPathSource : IPathSource
        {
            private readonly IReadOnlyList<CultivationPathDef> _paths;
            public ListPathSource(IReadOnlyList<CultivationPathDef> paths) => _paths = paths;
            public IReadOnlyList<CultivationPathDef> Load() => _paths;
        }
    }
}
