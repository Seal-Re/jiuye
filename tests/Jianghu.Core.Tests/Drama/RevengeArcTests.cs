using System;
using System.Collections.Generic;
using Jianghu.Config;
using Jianghu.Drama;
using Jianghu.Model;
using Xunit;

namespace Jianghu.Core.Tests.Drama
{
    /// <summary>
    /// RevengeArc.TryAdvance（drama-007b，spec §3.2）：复仇弧 5 态**纯转移**——
    /// 门控读 IDramaView/LimitsConfig，返回下一弧 + 转移结果。无 rng/事件/mutate（纯函数确定）。
    /// Victimized→BuildUp→Hunting→Showdown→Resolved | Abandoned；终态推进抛。
    /// </summary>
    public class RevengeArcTests
    {
        private sealed class FakeView : IDramaView
        {
            public Dictionary<long, int> Powers = new();
            public HashSet<long> Alive = new() { 1, 2 }; // 默认双方在世
            public HashSet<(long, long)> Together = new();
            public int Power(CharacterId who) => Powers.TryGetValue(who.Value, out var p) ? p : 0;
            public int Affinity(CharacterId from, CharacterId to) => 0;
            public bool IsAlive(CharacterId who) => Alive.Contains(who.Value);
            public bool SameNode(CharacterId a, CharacterId b)
                => Together.Contains((a.Value, b.Value)) || Together.Contains((b.Value, a.Value));
        }

        private static readonly CharacterId Avenger = new(1);
        private static readonly CharacterId Target = new(2);
        private static readonly LimitsConfig L = LimitsConfig.Default; // GrowthNeeded=50

        private static ArcInstance Arc(ArcStage stage, int basePower = 0, bool completed = false)
            => new ArcInstance(new ArcId(10), ArcKind.Revenge, Avenger, Target, stage, 0, basePower, completed);

        // —— D7b.2 Victimized→BuildUp 记 BasePower ——
        [Fact]
        public void test_victimized_advances_to_buildup_recording_base_power()
        {
            var v = new FakeView { Powers = { [1] = 120 } };
            var t = RevengeArc.TryAdvance(Arc(ArcStage.Victimized), v, L);
            Assert.Equal(ArcResolution.Advanced, t.Resolution);
            Assert.Equal(ArcStage.BuildUp, t.Next.Stage);
            Assert.Equal(120, t.Next.BuildUpBasePower); // 进 BuildUp 即记当前战力
        }

        // —— D7b.3 BuildUp→Hunting 门控 ——
        [Fact]
        public void test_buildup_advances_to_hunting_when_power_grown_enough()
        {
            // base=100, GrowthNeeded=50 → 需 ≥150。当前 150 → 过门。
            var v = new FakeView { Powers = { [1] = 150 } };
            var t = RevengeArc.TryAdvance(Arc(ArcStage.BuildUp, basePower: 100), v, L);
            Assert.Equal(ArcResolution.Advanced, t.Resolution);
            Assert.Equal(ArcStage.Hunting, t.Next.Stage);
        }

        [Fact]
        public void test_buildup_stalls_when_power_not_grown_enough()
        {
            // base=100 需 ≥150，当前仅 149 → 未过门 → Stalled，仍 BuildUp。
            var v = new FakeView { Powers = { [1] = 149 } };
            var t = RevengeArc.TryAdvance(Arc(ArcStage.BuildUp, basePower: 100), v, L);
            Assert.Equal(ArcResolution.Stalled, t.Resolution);
            Assert.Equal(ArcStage.BuildUp, t.Next.Stage);
        }

        // —— D7b.4 Hunting→Showdown 门控 ——
        [Fact]
        public void test_hunting_advances_to_showdown_when_same_node()
        {
            var v = new FakeView { Powers = { [1] = 200 } };
            v.Together.Add((1, 2));
            var t = RevengeArc.TryAdvance(Arc(ArcStage.Hunting), v, L);
            Assert.Equal(ArcResolution.Advanced, t.Resolution);
            Assert.Equal(ArcStage.Showdown, t.Next.Stage);
        }

        [Fact]
        public void test_hunting_stalls_when_not_same_node()
        {
            var v = new FakeView { Powers = { [1] = 200 } }; // 未同节点
            var t = RevengeArc.TryAdvance(Arc(ArcStage.Hunting), v, L);
            Assert.Equal(ArcResolution.Stalled, t.Resolution);
            Assert.Equal(ArcStage.Hunting, t.Next.Stage);
        }

        // —— D7b.5 Showdown 结算 ——
        [Fact]
        public void test_showdown_avenger_prevails_when_stronger()
        {
            var v = new FakeView { Powers = { [1] = 300, [2] = 200 } };
            v.Together.Add((1, 2));
            var t = RevengeArc.TryAdvance(Arc(ArcStage.Showdown), v, L);
            Assert.Equal(ArcResolution.Completed, t.Resolution);
            Assert.Equal(ArcStage.Resolved, t.Next.Stage);
            Assert.True(t.Next.Completed);
            Assert.True(t.AvengerPrevailed);
        }

        [Fact]
        public void test_showdown_avenger_loses_when_weaker()
        {
            var v = new FakeView { Powers = { [1] = 150, [2] = 200 } };
            v.Together.Add((1, 2));
            var t = RevengeArc.TryAdvance(Arc(ArcStage.Showdown), v, L);
            Assert.Equal(ArcResolution.Completed, t.Resolution);
            Assert.Equal(ArcStage.Resolved, t.Next.Stage);
            Assert.False(t.AvengerPrevailed);
        }

        // —— D7b.6 死亡→Abandoned（优先于门控）——
        [Fact]
        public void test_target_dead_abandons_arc()
        {
            var v = new FakeView { Powers = { [1] = 200 } };
            v.Alive.Remove(2); // 仇人亡
            var t = RevengeArc.TryAdvance(Arc(ArcStage.Hunting), v, L);
            Assert.Equal(ArcResolution.Abandoned, t.Resolution);
            Assert.Equal(ArcStage.Abandoned, t.Next.Stage);
            Assert.True(t.Next.Completed);
        }

        [Fact]
        public void test_avenger_dead_abandons_arc()
        {
            var v = new FakeView();
            v.Alive.Remove(1); // 复仇者亡
            var t = RevengeArc.TryAdvance(Arc(ArcStage.BuildUp, basePower: 100), v, L);
            Assert.Equal(ArcResolution.Abandoned, t.Resolution);
            Assert.Equal(ArcStage.Abandoned, t.Next.Stage);
        }

        [Fact]
        public void test_death_check_precedes_stage_gate()
        {
            // 即使在 Showdown 同节点本应结算，仇人若已亡 → Abandoned 而非 Completed。
            var v = new FakeView { Powers = { [1] = 300, [2] = 200 } };
            v.Together.Add((1, 2));
            v.Alive.Remove(2);
            var t = RevengeArc.TryAdvance(Arc(ArcStage.Showdown), v, L);
            Assert.Equal(ArcResolution.Abandoned, t.Resolution);
        }

        // —— D7b.7 终态推进抛 ——
        [Fact]
        public void test_resolved_arc_throws()
            => Assert.Throws<InvalidOperationException>(() => RevengeArc.TryAdvance(Arc(ArcStage.Resolved, completed: true), new FakeView(), L));

        [Fact]
        public void test_abandoned_arc_throws()
            => Assert.Throws<InvalidOperationException>(() => RevengeArc.TryAdvance(Arc(ArcStage.Abandoned, completed: true), new FakeView(), L));

        [Fact]
        public void test_completed_flag_arc_throws()
            => Assert.Throws<InvalidOperationException>(() => RevengeArc.TryAdvance(Arc(ArcStage.Hunting, completed: true), new FakeView(), L));

        // —— 纯函数确定性：同入参同出，且不改入参 ——
        [Fact]
        public void test_pure_function_deterministic_and_nonmutating()
        {
            var v = new FakeView { Powers = { [1] = 150 } };
            var arc = Arc(ArcStage.BuildUp, basePower: 100);
            var t1 = RevengeArc.TryAdvance(arc, v, L);
            var t2 = RevengeArc.TryAdvance(arc, v, L);
            Assert.Equal(t1.Resolution, t2.Resolution);
            Assert.Equal(t1.Next.Stage, t2.Next.Stage);
            Assert.Equal(ArcStage.BuildUp, arc.Stage); // 入参未被改（record 非破坏式）
        }
    }
}
