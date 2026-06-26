using System.Collections.Generic;
using Jianghu.Config;
using Jianghu.Drama;
using Jianghu.Model;
using Xunit;

namespace Jianghu.Core.Tests.Drama
{
    /// <summary>
    /// IgnitionScanner.FindIgnitions（drama-007b，spec §3.4C/§4）：点火候选收集——
    /// **只扫 ledger.AboveIntensity(threshold)**（O(强恩怨) 非 O(全员)，INV-PERF）；
    /// 过滤活跃弧/死亡/对子冷却；weight=max(1,intensity)；(Weight desc, Id asc) 确定性排序。
    /// </summary>
    public class IgnitionScannerTests
    {
        // 计数 view：记 IsAlive 调用次数，验只对强恩怨参与者查存活（O(强恩怨) 不 O(全员)）。
        private sealed class CountingView : IDramaView
        {
            public HashSet<long> Dead = new();
            public int IsAliveCalls;
            public int Power(CharacterId who) => 100;
            public int Affinity(CharacterId from, CharacterId to) => 0;
            public bool IsAlive(CharacterId who) { IsAliveCalls++; return !Dead.Contains(who.Value); }
            public bool SameNode(CharacterId a, CharacterId b) => false;
        }

        private static readonly LimitsConfig L = LimitsConfig.Default; // GrudgeCap=100, IgniteThreshold=60

        private static GrudgeLedger LedgerWith(params (long holder, long target, int intensity)[] rows)
        {
            var led = new GrudgeLedger();
            foreach (var (h, t, i) in rows)
                led.Form(new CharacterId(h), new CharacterId(t), GrudgeKind.Slaughter, i, 0, GrudgeCause.Direct, 0, null, L.GrudgeCap);
            return led;
        }

        private static bool NoActiveArc(CharacterId c) => false;
        private static bool NoCooldown(CharacterId a, CharacterId b) => false;

        // —— D7b.8 只收强恩怨（≥threshold）——
        [Fact]
        public void test_only_collects_grudges_above_threshold()
        {
            // 阈值 60：intensity 80/70 入选，40/59 排除。
            var led = LedgerWith((1, 2, 80), (3, 4, 70), (5, 6, 40), (7, 8, 59));
            var view = new CountingView();
            var cands = IgnitionScanner.FindIgnitions(led, L.GrudgeIgniteThreshold, view, NoActiveArc, NoCooldown);
            Assert.Equal(2, cands.Count);
            Assert.All(cands, c => Assert.True(c.Grudge.Intensity >= 60));
        }

        // —— D7b.8 INV-PERF：300 角色仅 2 强恩怨 → 存活检查 = O(强恩怨) ——
        [Fact]
        public void test_perf_scans_only_strong_grudges_not_all_population()
        {
            // 构造 2 条强恩怨（其余 298 角色无恩怨）。AboveIntensity 只返 2 条 →
            // IsAlive 至多被调常数×2 次，与"全员 300"无关。
            var led = LedgerWith((1, 2, 90), (3, 4, 75));
            var view = new CountingView();
            IgnitionScanner.FindIgnitions(led, L.GrudgeIgniteThreshold, view, NoActiveArc, NoCooldown);
            // 每候选最多查 holder+target 存活 = 2 次 → 2 候选 ≤ 4 次。绝不随人口 300 线性增长。
            Assert.True(view.IsAliveCalls <= 4, $"IsAlive 调用 {view.IsAliveCalls} 次，应 ≤4（O(强恩怨)）");
        }

        // —— D7b.8 过滤：已有活跃弧的持有者 ——
        [Fact]
        public void test_filters_holder_with_active_arc()
        {
            var led = LedgerWith((1, 2, 80), (3, 4, 70));
            var view = new CountingView();
            // holder 1 已有活跃弧 → 排除，仅剩 holder 3。
            bool HasArc(CharacterId c) => c.Value == 1;
            var cands = IgnitionScanner.FindIgnitions(led, L.GrudgeIgniteThreshold, view, HasArc, NoCooldown);
            Assert.Single(cands);
            Assert.Equal(3, cands[0].Grudge.Holder.Value);
        }

        // —— D7b.8 过滤：死亡参与者 ——
        [Fact]
        public void test_filters_dead_holder_or_target()
        {
            var led = LedgerWith((1, 2, 80), (3, 4, 70));
            var view = new CountingView { Dead = { 4 } }; // 仇人 4 已亡 → (3,4) 排除
            var cands = IgnitionScanner.FindIgnitions(led, L.GrudgeIgniteThreshold, view, NoActiveArc, NoCooldown);
            Assert.Single(cands);
            Assert.Equal(1, cands[0].Grudge.Holder.Value);
        }

        // —— D7b.8 过滤：对子冷却中 ——
        [Fact]
        public void test_filters_pair_on_cooldown()
        {
            var led = LedgerWith((1, 2, 80), (3, 4, 70));
            var view = new CountingView();
            bool OnCd(CharacterId a, CharacterId b) => a.Value == 1 && b.Value == 2; // (1,2) 冷却
            var cands = IgnitionScanner.FindIgnitions(led, L.GrudgeIgniteThreshold, view, NoActiveArc, OnCd);
            Assert.Single(cands);
            Assert.Equal(3, cands[0].Grudge.Holder.Value);
        }

        // —— D7b.9 weight = max(1,intensity) ——
        [Fact]
        public void test_weight_equals_intensity_floored_to_one()
        {
            var led = LedgerWith((1, 2, 88));
            var view = new CountingView();
            var cands = IgnitionScanner.FindIgnitions(led, L.GrudgeIgniteThreshold, view, NoActiveArc, NoCooldown);
            Assert.Single(cands);
            Assert.Equal(88, cands[0].Weight);
        }

        // —— D7b.9 确定性排序 (Weight desc, Grudge.Id asc) ——
        [Fact]
        public void test_deterministic_sort_weight_desc_then_id_asc()
        {
            // 三条强恩怨，intensity 70/90/70。排序后：90 先，两个 70 按 Id asc。
            var led = LedgerWith((1, 2, 70), (3, 4, 90), (5, 6, 70));
            var view = new CountingView();
            var cands = IgnitionScanner.FindIgnitions(led, L.GrudgeIgniteThreshold, view, NoActiveArc, NoCooldown);
            Assert.Equal(3, cands.Count);
            Assert.Equal(90, cands[0].Weight);                 // 权重最大者先
            // 两个 70：Id 升序（(1,2) 先入账 Id=1 < (5,6) Id=3）。
            Assert.True(cands[1].Grudge.Id.Value < cands[2].Grudge.Id.Value);
        }

        // —— 阈值下无候选→空表 ——
        [Fact]
        public void test_no_candidates_returns_empty()
        {
            var led = LedgerWith((1, 2, 30), (3, 4, 40)); // 全低于阈值 60
            var view = new CountingView();
            var cands = IgnitionScanner.FindIgnitions(led, L.GrudgeIgniteThreshold, view, NoActiveArc, NoCooldown);
            Assert.Empty(cands);
        }
    }
}
