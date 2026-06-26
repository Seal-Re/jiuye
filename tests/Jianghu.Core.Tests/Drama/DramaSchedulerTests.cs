using System;
using System.Collections.Generic;
using Jianghu.Drama;
using Jianghu.Events;
using Jianghu.Model;
using Xunit;

namespace Jianghu.Core.Tests.Drama
{
    /// <summary>
    /// DramaScheduler（drama-009，spec §3.4A）：确定性最小堆 (NextWakeAt, ArcId)——仿 Jianghu.Sim.Scheduler。
    /// 弧到期推进的时间轮 + HasDue 到期判定 + Snapshot/LoadFrom 续跑（供 drama-010 Clone）。
    /// IDramaMutator：戏剧层唯一写 seam（Emit 事件汇）。
    /// </summary>
    public class DramaSchedulerTests
    {
        // —— D9.2/D9.6 乱序 Push → 有序 PopMin (At asc, ArcId asc) ——
        [Fact]
        public void test_popmin_orders_by_at_then_arcid()
        {
            var s = new DramaScheduler();
            s.Push(new ArcId(3), 50);
            s.Push(new ArcId(1), 20);
            s.Push(new ArcId(2), 20); // 同 At=20，ArcId 2 > 1 → 1 先
            s.Push(new ArcId(5), 10);

            Assert.Equal(new ArcId(5), s.PopMin().Arc); // At=10 最早
            Assert.Equal(new ArcId(1), s.PopMin().Arc); // At=20, ArcId=1
            Assert.Equal(new ArcId(2), s.PopMin().Arc); // At=20, ArcId=2
            Assert.Equal(new ArcId(3), s.PopMin().Arc); // At=50
        }

        // —— D9.3 PeekMin 不弹 ——
        [Fact]
        public void test_peekmin_does_not_remove()
        {
            var s = new DramaScheduler();
            s.Push(new ArcId(1), 5);
            Assert.Equal(new ArcId(1), s.PeekMin().Arc);
            Assert.Equal(new ArcId(1), s.PeekMin().Arc); // 再 peek 仍在
            Assert.Equal(1, s.Count);
        }

        // —— D9.3 HasDue 到期边界 ——
        [Fact]
        public void test_hasdue_boundary()
        {
            var s = new DramaScheduler();
            Assert.False(s.HasDue(100)); // 空堆 → false
            s.Push(new ArcId(1), 50);
            Assert.True(s.HasDue(50));   // At==clock → 到期
            Assert.True(s.HasDue(51));   // At<clock → 到期
            Assert.False(s.HasDue(49));  // At>clock → 未到期
        }

        // —— D9.2 空堆 PopMin 抛 ——
        [Fact]
        public void test_empty_popmin_throws()
            => Assert.Throws<InvalidOperationException>(() => new DramaScheduler().PopMin());

        // —— D9.4 Snapshot/LoadFrom 往返 ——
        [Fact]
        public void test_snapshot_loadfrom_roundtrip()
        {
            var s = new DramaScheduler();
            s.Push(new ArcId(3), 30);
            s.Push(new ArcId(1), 10);
            s.Push(new ArcId(2), 20);
            var snap = s.Snapshot();
            Assert.Equal(3, snap.Count);

            var restored = new DramaScheduler();
            restored.LoadFrom(snap);
            // 往返后出堆序一致。
            Assert.Equal(s.PopMin().Arc, restored.PopMin().Arc);
            Assert.Equal(s.PopMin().Arc, restored.PopMin().Arc);
            Assert.Equal(s.PopMin().Arc, restored.PopMin().Arc);
        }

        // —— D9.6 确定性：乱序 Push 同集合 → 同出堆序 ——
        [Fact]
        public void test_deterministic_same_set_same_order()
        {
            var a = new DramaScheduler();
            var b = new DramaScheduler();
            // 不同插入序，同集合。
            foreach (var (id, at) in new[] { (4, 40), (1, 10), (3, 30), (2, 20) })
                a.Push(new ArcId(id), at);
            foreach (var (id, at) in new[] { (1, 10), (2, 20), (3, 30), (4, 40) })
                b.Push(new ArcId(id), at);
            for (int i = 0; i < 4; i++)
                Assert.Equal(a.PopMin(), b.PopMin());
        }

        // —— D9.5 IDramaMutator recording fake：Emit 收集事件序（drama-009b 复用）——
        private sealed class RecordingMutator : IDramaMutator
        {
            public List<DomainEvent> Events = new();
            public void Emit(DomainEvent e) => Events.Add(e);
            public void OverrideGoal(CharacterId who, GoalKind kind) { }
            public void RestoreGoal(CharacterId who, Goal original) { }
            public void MirrorRelation(CharacterId holder, CharacterId target, int delta) { }
        }

        [Fact]
        public void test_mutator_emit_collects_events_in_order()
        {
            IDramaMutator m = new RecordingMutator();
            var a = new CharacterId(1);
            var b = new CharacterId(2);
            m.Emit(new ArcIgnited(10, new ArcId(1), a, b));
            m.Emit(new ArcStageEntered(11, new ArcId(1), ArcStage.BuildUp));
            var rec = (RecordingMutator)m;
            Assert.Equal(2, rec.Events.Count);
            Assert.IsType<ArcIgnited>(rec.Events[0]);
            Assert.IsType<ArcStageEntered>(rec.Events[1]);
        }
    }
}
