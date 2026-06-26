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
    /// drama-011 受控耦合（spec §0.2/§3.2）：复仇弧经两条 RuleBrain 既有通道间接驱动行为——
    /// BuildUp→覆写 Goal=Advance（疯修）；Hunting→镜像负 Relations（触发 notFoe）；收束→还原 Goal。
    /// 经 recording mutator 验写口序；RuleBrain 零改。
    /// </summary>
    public class DramaCouplingTests
    {
        // 可配 view + 可读 Goal（drama-011 加 GoalOf）。
        private sealed class FakeView : IDramaView
        {
            public Dictionary<long, int> Powers = new();
            public HashSet<long> Dead = new();
            public HashSet<(long, long)> Together = new();
            public Dictionary<long, Goal> Goals = new();
            public int Power(CharacterId who) => Powers.TryGetValue(who.Value, out var p) ? p : 0;
            public int Affinity(CharacterId from, CharacterId to) => 0;
            public bool IsAlive(CharacterId who) => !Dead.Contains(who.Value);
            public bool SameNode(CharacterId a, CharacterId b)
                => Together.Contains((a.Value, b.Value)) || Together.Contains((b.Value, a.Value));
            public Goal GoalOf(CharacterId who) => Goals.TryGetValue(who.Value, out var g) ? g : new Goal(GoalKind.Wander, 0);
        }

        // 记录耦合写口调用序。
        private sealed class CouplingMutator : IDramaMutator
        {
            public List<DomainEvent> Events = new();
            public List<(long id, GoalKind kind)> Overrides = new();
            public List<(long id, Goal goal)> Restores = new();
            public List<(long holder, long target, int delta)> Mirrors = new();
            public void Emit(DomainEvent e) => Events.Add(e);
            public void OverrideGoal(CharacterId who, GoalKind kind) => Overrides.Add((who.Value, kind));
            public void RestoreGoal(CharacterId who, Goal original) => Restores.Add((who.Value, original));
            public void MirrorRelation(CharacterId holder, CharacterId target, int delta) => Mirrors.Add((holder.Value, target.Value, delta));
        }

        private static readonly LimitsConfig L = LimitsConfig.Default; // RelationMirrorCap=30, GrowthNeeded=50

        private static GrudgeLedger LedgerWith(long h, long t, int i)
        {
            var led = new GrudgeLedger();
            led.Form(new CharacterId(h), new CharacterId(t), GrudgeKind.Slaughter, i, 0, GrudgeCause.Direct, 0, null, L.GrudgeCap);
            return led;
        }

        static IRandom Rng(ulong seed = 42) => new Pcg32(seed, 1);

        // —— D11.2 BuildUp → OverrideGoal(Advance) ——
        [Fact]
        public void test_buildup_overrides_goal_to_advance()
        {
            var dir = new DramaDirector(LedgerWith(1, 2, 90), L);
            var view = new FakeView { Powers = { [1] = 100, [2] = 50 }, Goals = { [1] = new Goal(GoalKind.Wander, 7) } };
            var mut = new CouplingMutator();
            var rng = Rng();
            dir.Pump(0, view, mut, rng);   // ignite → Victimized @10
            dir.Pump(10, view, mut, rng);  // → BuildUp：覆写 avenger Goal=Advance

            Assert.Contains((1L, GoalKind.Advance), mut.Overrides);
        }

        // —— D11.3 Hunting → MirrorRelation(-cap) ——
        [Fact]
        public void test_hunting_mirrors_negative_relation()
        {
            var dir = new DramaDirector(LedgerWith(1, 2, 90), L);
            var view = new FakeView { Powers = { [1] = 100, [2] = 50 }, Goals = { [1] = new Goal(GoalKind.Wander, 0) } };
            var mut = new CouplingMutator();
            var rng = Rng();
            dir.Pump(0, view, mut, rng);   // ignite
            dir.Pump(10, view, mut, rng);  // → BuildUp，base=100
            view.Powers[1] = 200;          // 涨够战力
            dir.Pump(110, view, mut, rng); // → Hunting：镜像负 Relations

            Assert.Contains(mut.Mirrors, m => m.holder == 1 && m.target == 2 && m.delta == -L.RelationMirrorCap);
        }

        // —— D11.4 收束 → RestoreGoal（往返原值）——
        [Fact]
        public void test_resolve_restores_original_goal()
        {
            var dir = new DramaDirector(LedgerWith(1, 2, 90), L);
            var original = new Goal(GoalKind.Wander, 42);
            var view = new FakeView { Powers = { [1] = 100, [2] = 50 }, Together = { (1, 2) }, Goals = { [1] = original } };
            var mut = new CouplingMutator();
            var rng = Rng();
            dir.Pump(0, view, mut, rng);
            dir.Pump(10, view, mut, rng);  // BuildUp，存原 Goal
            view.Powers[1] = 200;
            dir.Pump(110, view, mut, rng); // Hunting
            dir.Pump(140, view, mut, rng); // Showdown
            dir.Pump(150, view, mut, rng); // Resolved → 还原

            Assert.Contains(mut.Restores, r => r.id == 1 && r.goal.Kind == GoalKind.Wander && r.goal.Progress == 42);
        }

        // —— D11.4 Abandoned 也还原 ——
        [Fact]
        public void test_abandon_restores_original_goal()
        {
            var dir = new DramaDirector(LedgerWith(1, 2, 90), L);
            var original = new Goal(GoalKind.Wander, 5);
            var view = new FakeView { Powers = { [1] = 100, [2] = 50 }, Goals = { [1] = original } };
            var mut = new CouplingMutator();
            var rng = Rng();
            dir.Pump(0, view, mut, rng);
            dir.Pump(10, view, mut, rng);  // BuildUp，存原 Goal
            view.Dead.Add(2);              // 仇人亡
            dir.Pump(110, view, mut, rng); // 死亡守卫 → Abandoned → 还原

            Assert.Contains(mut.Restores, r => r.id == 1 && r.goal.Progress == 5);
        }

        // —— Clone 含原 Goal 表（还原不漂移）——
        [Fact]
        public void test_clone_preserves_original_goal_mapping()
        {
            var ledger = LedgerWith(1, 2, 90);
            var dir = new DramaDirector(ledger, L);
            var original = new Goal(GoalKind.Wander, 99);
            var view = new FakeView { Powers = { [1] = 100, [2] = 50 }, Goals = { [1] = original } };
            var mut = new CouplingMutator();
            var rng = Rng();
            dir.Pump(0, view, mut, rng);
            dir.Pump(10, view, mut, rng);  // BuildUp，存原 Goal=99

            var clone = dir.Clone(ledger.Clone());
            var cloneMut = new CouplingMutator();
            var cloneRng = Rng();
            // 克隆推进到收束 → 应还原存档的原 Goal=99（证 _originalGoals 进 Clone）。
            view.Dead.Add(2);
            clone.Pump(110, view, cloneMut, cloneRng);
            Assert.Contains(cloneMut.Restores, r => r.id == 1 && r.goal.Progress == 99);
        }
    }
}
