using System.Collections.Generic;
using System.Linq;
using Jianghu.Config;
using Jianghu.Drama;
using Jianghu.Model;
using Jianghu.Random;
using Jianghu.Sim;
using Xunit;

namespace Jianghu.Core.Tests.Drama
{
    /// <summary>
    /// drama-013 INV-CHAIN 端到端验收（AC-7）+ INV-PERF（AC-5）。
    /// 预置冤孽 cultivation-on 跨种子长跑见完整复仇/跨代链；点火候选遍历 O(强恩怨) 非 O(全员)。
    /// </summary>
    public class DramaInvChainTests
    {
        // —— D13.6 INV-CHAIN：预置冤孽长跑产生复仇弧生命周期 ——
        [Fact]
        public void test_inv_chain_revenge_arc_lifecycle_emerges()
        {
            // 跨种子搜索（弧推进/继承依赖随机时序）：证端到端可见复仇弧点燃 + 推进 + 结局。
            bool sawArcLifecycle = false;
            for (ulong seed = 1; seed <= 12 && !sawArcLifecycle; seed++)
            {
                var limits = LimitsConfig.Default with { GrowthNeeded = 0 };
                var w = WorldFactory.CreateInitial(seed, limits, 8, cultivation: true, dramaOn: true, dramaSeedFeuds: true);
                for (int i = 0; i < 600; i++) w.Advance(6);
                var lines = w.Chronicle.Lines;
                bool ignited = lines.Any(l => l.Contains("立誓复仇"));
                bool progressed = lines.Any(l => l.Contains("复仇弧#") && (l.Contains("蓄力") || l.Contains("寻仇") || l.Contains("狭路")));
                bool outcome = lines.Any(l => l.Contains("大仇得报") || l.Contains("饮恨") || l.Contains("半途而废"));
                if (ignited && progressed && outcome) sawArcLifecycle = true;
            }
            Assert.True(sawArcLifecycle, "预置冤孽长跑应见完整复仇弧（点燃→推进→结局）");
        }

        // —— D13.7 INV-PERF：300 角色仅 2 强恩怨 → 点火候选遍历 O(强恩怨) 非 O(全员) ——
        // 经 IgnitionScanner 单测已验 O(强恩怨)；此处复用 scanner 直接证接线后仍成立。
        private sealed class SpyView : IDramaView
        {
            public int IsAliveCalls;
            public int Power(CharacterId who) => 100;
            public int Affinity(CharacterId from, CharacterId to) => 0;
            public bool IsAlive(CharacterId who) { IsAliveCalls++; return true; }
            public bool SameNode(CharacterId a, CharacterId b) => false;
            public Goal GoalOf(CharacterId who) => new Goal(GoalKind.Wander, 0);
        }

        [Fact]
        public void test_inv_perf_ignition_scans_only_strong_grudges()
        {
            // 300 角色规模但仅 2 条强恩怨：FindIgnitions 只扫 AboveIntensity → IsAlive 调用 O(强恩怨)。
            var led = new GrudgeLedger();
            led.Form(new CharacterId(1), new CharacterId(2), GrudgeKind.Slaughter, 90, 0, GrudgeCause.Direct, 0, null, 100);
            led.Form(new CharacterId(3), new CharacterId(4), GrudgeKind.Slaughter, 85, 0, GrudgeCause.Direct, 0, null, 100);
            // 另加 300 条弱恩怨（< 阈值），不应被扫存活检查。
            for (long h = 10; h < 310; h++)
                led.Form(new CharacterId(h), new CharacterId(1000 + h), GrudgeKind.Insult, 10, 0, GrudgeCause.Direct, 0, null, 100);

            var spy = new SpyView();
            var cands = IgnitionScanner.FindIgnitions(led, LimitsConfig.Default.GrudgeIgniteThreshold, spy,
                _ => false, (_, _) => false);
            Assert.Equal(2, cands.Count);
            // 仅 2 强恩怨参与者存活检查 ≤ 4 次（与 300+ 弱恩怨无关）。
            Assert.True(spy.IsAliveCalls <= 4, $"IsAlive 调用 {spy.IsAliveCalls}，应 O(强恩怨)≤4");
        }

        // —— D13.6 预置冤孽确定性长跑逐字节（接线后端到端确定）——
        [Fact]
        public void test_seeded_feuds_deterministic_long_run()
        {
            string Run()
            {
                var w = WorldFactory.CreateInitial(2026, LimitsConfig.Default with { GrowthNeeded = 0 }, 8,
                    cultivation: true, dramaOn: true, dramaSeedFeuds: true);
                for (int i = 0; i < 400; i++) w.Advance(6);
                return string.Join("\n", w.Chronicle.Lines);
            }
            Assert.Equal(Run(), Run());
        }
    }
}
