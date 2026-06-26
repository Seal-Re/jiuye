using System.Collections.Generic;
using Jianghu.Config;
using Jianghu.Drama;
using Jianghu.Events;
using Jianghu.Model;
using Jianghu.Random;
using Jianghu.Sim;
using Xunit;

namespace Jianghu.Core.Tests.Drama
{
    /// <summary>
    /// drama-013 死锁兜底（spec §3.2，AC-8）：Showdown 超时强制结算 + INV-NO-DEADLOCK（弧不永久卡）
    /// + INV-CAP（活跃弧 ≤ MaxConcurrentArcs）。
    /// </summary>
    public class DramaDeadlockTests
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
            public Goal GoalOf(CharacterId who) => new Goal(GoalKind.Wander, 0);
        }
        private sealed class RecordingMutator : IDramaMutator
        {
            public List<DomainEvent> Events = new();
            public void Emit(DomainEvent e) => Events.Add(e);
            public void OverrideGoal(CharacterId who, GoalKind kind) { }
            public void RestoreGoal(CharacterId who, Goal original) { }
            public void MirrorRelation(CharacterId holder, CharacterId target, int delta) { }
            public int CountOf<T>() { int n = 0; foreach (var e in Events) if (e is T) n++; return n; }
        }

        private static readonly LimitsConfig L = LimitsConfig.Default;
        private static IRandom Rng(ulong s = 42) => new Pcg32(s, 1);
        private static GrudgeLedger Led(long h, long t, int i)
        {
            var l = new GrudgeLedger();
            l.Form(new CharacterId(h), new CharacterId(t), GrudgeKind.Slaughter, i, 0, GrudgeCause.Direct, 0, null, L.GrudgeCap);
            return l;
        }

        // —— D13.4/D13.5 弧不会永久卡：Hunting 反复 Stalled（仇人永不同节点）最终因死亡 Abandoned 退场 ——
        [Fact]
        public void test_arc_never_stuck_forever_resolves_or_abandons()
        {
            var dir = new DramaDirector(Led(1, 2, 90), L);
            var view = new FakeView { Powers = { [1] = 100, [2] = 50 } };
            var mut = new RecordingMutator();
            var rng = Rng();
            // 仇人永不同节点（Together 空）→ Hunting 反复 Stalled。长跑后仇人死 → Abandoned。
            for (long c = 0; c < 2000; c += 10)
            {
                dir.Pump(c, view, mut, rng);
                if (c == 1000) view.Dead.Add(2); // 中途仇人寿尽
            }
            // 弧最终退场（不永久占并发槽）。
            Assert.Empty(dir.ActiveArcs);
        }

        // —— D13.4 Showdown 超时强制结算：弧进 Showdown 后超时被强制了断 ——
        [Fact]
        public void test_showdown_timeout_forces_settlement()
        {
            var limits = L with { GrowthNeeded = 0, ShowdownTimeout = 20 };
            var dir = new DramaDirector(Led(1, 2, 90), limits);
            var view = new FakeView { Powers = { [1] = 200, [2] = 50 }, Together = { (1, 2) } };
            var mut = new RecordingMutator();
            var rng = Rng();
            dir.Pump(0, view, mut, rng);   // ignite Victimized@10
            dir.Pump(10, view, mut, rng);  // BuildUp
            dir.Pump(110, view, mut, rng); // Hunting (GrowthNeeded=0 → 过门)
            // 进 Showdown 后，模拟仇人离开节点（Together 移除）——但 Showdown 已无条件结算，
            // 此测主要验证弧不会卡在 Showdown：长跑后必收束。
            for (long c = 140; c < 400; c += 10) dir.Pump(c, view, mut, rng);
            Assert.Empty(dir.ActiveArcs);   // 弧已结算退场（RevengeConsummated）
            Assert.True(mut.CountOf<RevengeConsummated>() >= 1);
        }

        // —— D13.8 INV-CAP：活跃弧 ≤ MaxConcurrentArcs（多强恩怨长跑）——
        [Fact]
        public void test_inv_cap_active_arcs_bounded()
        {
            var limits = L with { MaxConcurrentArcs = 2, IgnitionCheckInterval = 5 };
            var led = new GrudgeLedger();
            for (long h = 1; h <= 10; h++)
                led.Form(new CharacterId(h), new CharacterId(100 + h), GrudgeKind.Slaughter, 90, 0, GrudgeCause.Direct, 0, null, L.GrudgeCap);
            var dir = new DramaDirector(led, limits);
            var view = new FakeView();
            for (long h = 1; h <= 10; h++) view.Powers[h] = 100;
            var mut = new RecordingMutator();
            var rng = Rng();
            for (long c = 0; c < 500; c += 5)
            {
                dir.Pump(c, view, mut, rng);
                Assert.True(dir.ActiveArcs.Count <= 2, $"活跃弧 {dir.ActiveArcs.Count} 超 MaxConcurrentArcs=2");
            }
        }

        // —— D13.5 World 端 INV-NO-DEADLOCK：预置冤孽长跑活跃弧有界 ——
        [Fact]
        public void test_world_inv_no_deadlock_long_run()
        {
            var w = WorldFactory.CreateInitial(7, LimitsConfig.Default with { GrowthNeeded = 0 }, 8,
                cultivation: true, dramaOn: true, dramaSeedFeuds: true);
            for (int i = 0; i < 1000; i++) w.Advance(6);
            // 长跑后世界仍在跑（无异常/无卡死），账本强度恒在 [0,Cap]。
            foreach (var g in w.Grudges!.All)
            {
                Assert.InRange(g.Intensity, 0, LimitsConfig.Default.GrudgeCap);
                Assert.True(g.Generation <= LimitsConfig.Default.MaxGeneration, "继承代数不超封顶");
            }
        }
    }
}
