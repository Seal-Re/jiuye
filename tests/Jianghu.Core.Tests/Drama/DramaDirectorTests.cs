using System.Collections.Generic;
using Jianghu.Config;
using Jianghu.Drama;
using Jianghu.Events;
using Jianghu.Model;
using Jianghu.Random;
using Xunit;

namespace Jianghu.Core.Tests.Drama
{
    /// <summary>
    /// DramaDirector.Pump（drama-009b，spec §3.4）：编排核心——推进相（弹到期弧→TryAdvance→Emit→重排）
    /// + 节流点火相（FindIgnitions→WeightedPicker→创建弧→Emit ArcIgnited）。
    /// 经 mock IDramaView + recording IDramaMutator + Pcg32 单测（pre-World-wiring）。
    /// ⚠️ 空库 Pump 严格 no-op（不消费 rng、不产事件）——B.3 命门。
    /// </summary>
    public class DramaDirectorTests
    {
        private sealed class FakeView : IDramaView
        {
            public Dictionary<long, int> Powers = new();
            public HashSet<long> Dead = new();
            public HashSet<(long, long)> Together = new();
            public int Power(CharacterId who) => Powers.TryGetValue(who.Value, out var p) ? p : 0;
            public int Affinity(CharacterId from, CharacterId to) => 0;
            public bool IsAlive(CharacterId who) => !Dead.Contains(who.Value);
            public bool SameNode(CharacterId a, CharacterId b)
                => Together.Contains((a.Value, b.Value)) || Together.Contains((b.Value, a.Value));
        }

        private sealed class RecordingMutator : IDramaMutator
        {
            public List<DomainEvent> Events = new();
            public void Emit(DomainEvent e) => Events.Add(e);
            public int CountOf<T>() { int n = 0; foreach (var e in Events) if (e is T) n++; return n; }
        }

        private static readonly LimitsConfig L = LimitsConfig.Default;
        // 默认 delays：First=10, BuildUp=100, Hunting=30, Showdown=10；IgniteThreshold=60；
        // IgnitionCheckInterval=20；MaxConcurrentArcs=3；ArcPairCooldown=200；DramaBudget=4。

        private static GrudgeLedger LedgerWith(params (long h, long t, int i)[] rows)
        {
            var led = new GrudgeLedger();
            foreach (var (h, t, i) in rows)
                led.Form(new CharacterId(h), new CharacterId(t), GrudgeKind.Slaughter, i, 0, GrudgeCause.Direct, 0, null, L.GrudgeCap);
            return led;
        }

        static IRandom Rng(ulong seed = 42) => new Pcg32(seed, 1);

        // —— ⚠️ D9b.7 空库 no-op：零事件 + rng 状态不变 ——
        [Fact]
        public void test_empty_ledger_pump_is_noop_rng_untouched()
        {
            var dir = new DramaDirector(new GrudgeLedger(), L);
            var view = new FakeView();
            var mut = new RecordingMutator();
            var rng = Rng();
            var before = rng.GetState();

            for (long c = 0; c < 100; c++) dir.Pump(c, view, mut, rng);

            Assert.Empty(mut.Events);            // 无事件
            Assert.Equal(before, rng.GetState()); // rng 未被消费（点火相空候选在抽取前返回）
            Assert.Empty(dir.ActiveArcs);
        }

        // —— D9b.5 MaxConcurrentArcs=0 no-op（开关）——
        [Fact]
        public void test_max_concurrent_zero_never_ignites()
        {
            var limits = L with { MaxConcurrentArcs = 0 };
            var dir = new DramaDirector(LedgerWith((1, 2, 90)), limits);
            var view = new FakeView();
            var mut = new RecordingMutator();
            var rng = Rng();
            var before = rng.GetState();

            for (long c = 0; c < 100; c++) dir.Pump(c, view, mut, rng);

            Assert.Empty(mut.Events);
            Assert.Equal(before, rng.GetState()); // 并发上限 0 → 点火相在抽取前返回，rng 不动
        }

        // —— D9b.6 点火：强恩怨 → ArcIgnited + 弧入场 ——
        [Fact]
        public void test_ignites_arc_from_strong_grudge()
        {
            var dir = new DramaDirector(LedgerWith((1, 2, 90)), L);
            var view = new FakeView { Powers = { [1] = 100, [2] = 50 } };
            var mut = new RecordingMutator();
            dir.Pump(0, view, mut, Rng());

            Assert.Equal(1, mut.CountOf<ArcIgnited>());
            Assert.Single(dir.ActiveArcs);
            var arc = dir.ActiveArcs[0];
            Assert.Equal(ArcStage.Victimized, arc.Stage);
            Assert.Equal(1, arc.Avenger.Value);
            Assert.Equal(2, arc.Target.Value);
        }

        // —— D9b.2/D9b.3 推进 Victimized→BuildUp 发 ArcStageEntered ——
        [Fact]
        public void test_advances_victimized_to_buildup()
        {
            var dir = new DramaDirector(LedgerWith((1, 2, 90)), L);
            var view = new FakeView { Powers = { [1] = 120, [2] = 50 } };
            var mut = new RecordingMutator();
            var rng = Rng();
            dir.Pump(0, view, mut, rng);   // ignite, Victimized scheduled @10
            mut.Events.Clear();
            dir.Pump(10, view, mut, rng);  // due → Victimized→BuildUp

            Assert.Equal(1, mut.CountOf<ArcStageEntered>());
            Assert.Equal(ArcStage.BuildUp, dir.ActiveArcs[0].Stage);
            Assert.Equal(120, dir.ActiveArcs[0].BuildUpBasePower); // 进 BuildUp 记基线
        }

        // —— D9b.2 全生命周期 → RevengeConsummated ——
        [Fact]
        public void test_full_lifecycle_reaches_consummation()
        {
            var dir = new DramaDirector(LedgerWith((1, 2, 90)), L);
            var view = new FakeView { Powers = { [1] = 100, [2] = 50 } };
            var mut = new RecordingMutator();
            var rng = Rng();

            dir.Pump(0, view, mut, rng);   // ignite → Victimized @10
            dir.Pump(10, view, mut, rng);  // → BuildUp, base=100, @110
            // 蓄力涨战力 + 同节点（模拟 Goal 覆写效果，drama-011 才真接，此处手动）。
            view.Powers[1] = 200;          // 200 ≥ 100+50 → 过 Hunting 门
            view.Together.Add((1, 2));
            dir.Pump(110, view, mut, rng); // → Hunting @140
            dir.Pump(140, view, mut, rng); // → Showdown @150
            dir.Pump(150, view, mut, rng); // → Resolved，结算

            Assert.Equal(1, mut.CountOf<ArcIgnited>());
            Assert.Equal(1, mut.CountOf<RevengeConsummated>());
            Assert.Empty(dir.ActiveArcs); // 收束退场
            // 胜负：avenger 200 ≥ target 50 → 得手。
            var consummated = (RevengeConsummated)mut.Events.Find(e => e is RevengeConsummated)!;
            Assert.True(consummated.AvengerPrevailed);
        }

        // —— D9b.2 仇人死 → ArcAbandoned ——
        [Fact]
        public void test_target_death_abandons_arc()
        {
            var dir = new DramaDirector(LedgerWith((1, 2, 90)), L);
            var view = new FakeView { Powers = { [1] = 100, [2] = 50 } };
            var mut = new RecordingMutator();
            var rng = Rng();
            dir.Pump(0, view, mut, rng);  // ignite
            view.Dead.Add(2);             // 仇人亡
            mut.Events.Clear();
            dir.Pump(10, view, mut, rng); // due → 死亡守卫 → Abandoned

            Assert.Equal(1, mut.CountOf<ArcAbandoned>());
            Assert.Empty(dir.ActiveArcs);
        }

        // —— D9b.5 MaxConcurrentArcs 上限挡并发 ——
        [Fact]
        public void test_max_concurrent_arcs_caps_active()
        {
            var limits = L with { MaxConcurrentArcs = 1 };
            // 两不同 holder 强恩怨。
            var dir = new DramaDirector(LedgerWith((1, 2, 90), (3, 4, 85)), limits);
            var view = new FakeView { Powers = { [1] = 100, [3] = 100 } };
            var mut = new RecordingMutator();
            var rng = Rng();
            // 多 Pump：第一次点火 1 条，之后并发满 → 不再点火（BuildUp 停滞保持 active）。
            for (long c = 0; c <= 60; c += 20) dir.Pump(c, view, mut, rng);

            Assert.True(dir.ActiveArcs.Count <= 1, $"活跃弧 {dir.ActiveArcs.Count} 超 MaxConcurrentArcs=1");
            Assert.Equal(1, mut.CountOf<ArcIgnited>()); // 仅 1 次点火
        }

        // —— D9b.1 DramaBudget 限单 Pump 推进数 ——
        [Fact]
        public void test_drama_budget_caps_advances_per_pump()
        {
            // IgnitionCheckInterval=1 让 3 弧快速点火；DramaBudget=1 限每 Pump 仅推进 1。
            var limits = L with { IgnitionCheckInterval = 1, MaxConcurrentArcs = 3, DramaBudget = 1, FirstStageDelay = 5 };
            var dir = new DramaDirector(LedgerWith((1, 2, 90), (3, 4, 88), (5, 6, 86)), limits);
            var view = new FakeView { Powers = { [1] = 100, [3] = 100, [5] = 100 } };
            var mut = new RecordingMutator();
            var rng = Rng();
            // clock 0/1/2 各点火 1 → 3 弧 Victimized 分别 @5/6/7。
            dir.Pump(0, view, mut, rng);
            dir.Pump(1, view, mut, rng);
            dir.Pump(2, view, mut, rng);
            Assert.Equal(3, dir.ActiveArcs.Count);
            // clock 7：@5/6/7 三弧全到期，但 DramaBudget=1 → 单 Pump 仅推进 1。
            mut.Events.Clear();
            dir.Pump(7, view, mut, rng);
            Assert.Equal(1, mut.CountOf<ArcStageEntered>()); // 预算限 1
        }

        // —— D9b.9 确定性：同种子两 Director → 同事件序 ——
        [Fact]
        public void test_deterministic_same_seed_same_event_stream()
        {
            string Run()
            {
                var dir = new DramaDirector(LedgerWith((1, 2, 80), (3, 4, 80), (5, 6, 80)), L);
                var view = new FakeView { Powers = { [1] = 100, [3] = 100, [5] = 100 } };
                var mut = new RecordingMutator();
                var rng = Rng(777);
                for (long c = 0; c <= 80; c += 10) dir.Pump(c, view, mut, rng);
                var sb = new System.Text.StringBuilder();
                foreach (var e in mut.Events) sb.Append(e.GetType().Name).Append(';');
                return sb.ToString();
            }
            Assert.Equal(Run(), Run());
        }

        // —— D9b.10 Clone 独立 ——
        [Fact]
        public void test_clone_independent()
        {
            var ledger = LedgerWith((1, 2, 90));
            var dir = new DramaDirector(ledger, L);
            var view = new FakeView { Powers = { [1] = 100, [2] = 50 } };
            var mut = new RecordingMutator();
            var rng = Rng();
            dir.Pump(0, view, mut, rng); // ignite → 1 active arc

            var clonedLedger = ledger.Clone();
            var clone = dir.Clone(clonedLedger);
            Assert.Single(clone.ActiveArcs); // 克隆继承活跃弧

            // 推进原 director 到收束（目标死）→ 原清空，克隆不变。
            view.Dead.Add(2);
            dir.Pump(10, view, mut, rng);
            Assert.Empty(dir.ActiveArcs);
            Assert.Single(clone.ActiveArcs); // 克隆独立，不受原推进影响
        }

        // —— D9b.8 对子冷却挡二次点火（resolve 后冷却窗内不复燃）——
        [Fact]
        public void test_pair_cooldown_blocks_reignite_after_resolve()
        {
            var dir = new DramaDirector(LedgerWith((1, 2, 90)), L);
            var view = new FakeView { Powers = { [1] = 100, [2] = 50 }, Together = { (1, 2) } };
            var mut = new RecordingMutator();
            var rng = Rng();
            // 驱动到 resolve：ignite@0→BuildUp@10(base=100)→蓄力涨战力→Hunting@110→Showdown@140→Resolved@150。
            dir.Pump(0, view, mut, rng);
            dir.Pump(10, view, mut, rng);  // → BuildUp，记 base=100
            view.Powers[1] = 200;          // 蓄力涨战力 200 ≥ 100+50 → 过 Hunting 门
            dir.Pump(110, view, mut, rng);
            dir.Pump(140, view, mut, rng);
            dir.Pump(150, view, mut, rng); // resolved，cooldown until 0+200=200
            Assert.Empty(dir.ActiveArcs);
            int ignitesBefore = mut.CountOf<ArcIgnited>();

            // clock 160（<200 冷却内）：grudge 仍在、holder 空闲，但对子冷却 → 不复燃。
            dir.Pump(160, view, mut, rng);
            Assert.Equal(ignitesBefore, mut.CountOf<ArcIgnited>()); // 无新点火
        }
    }
}
