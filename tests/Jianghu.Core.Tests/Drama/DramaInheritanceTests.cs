using System.Collections.Generic;
using Jianghu.Config;
using Jianghu.Drama;
using Jianghu.Events;
using Jianghu.Model;
using Xunit;

namespace Jianghu.Core.Tests.Drama
{
    /// <summary>
    /// drama-012 跨代继承（spec §3.3，验收核心）：复仇者寿尽且恩怨未了 → 在世子嗣/弟子继承
    /// 衰减恩怨(Cause=Inherited, gen+1) → GrudgeInherited → 点燃下一弧（跨代链）。
    /// 整数衰减单调不增；MaxGeneration 封顶；绝嗣不继承；继承人确定性取首位。
    /// </summary>
    public class DramaInheritanceTests
    {
        private sealed class RecordingMutator : IDramaMutator
        {
            public List<DomainEvent> Events = new();
            public void Emit(DomainEvent e) => Events.Add(e);
            public void OverrideGoal(CharacterId who, GoalKind kind) { }
            public void RestoreGoal(CharacterId who, Goal original) { }
            public void MirrorRelation(CharacterId holder, CharacterId target, int delta) { }
            public GrudgeInherited? FirstInherited()
            {
                foreach (var e in Events) if (e is GrudgeInherited gi) return gi;
                return null;
            }
            public int CountOf<T>() { int n = 0; foreach (var e in Events) if (e is T) n++; return n; }
        }

        private static readonly LimitsConfig L = LimitsConfig.Default; // IgniteThreshold=60, InheritDecayPct=60, MaxGeneration=3, GrudgeCap=100

        private static readonly CharacterId Avenger = new(1);  // 复仇者（寿尽）
        private static readonly CharacterId Target = new(2);   // 仇人
        private static readonly CharacterId Heir = new(3);     // 弟子（继承人）

        private static GrudgeLedger LedgerWithGrudge(int intensity, int generation)
        {
            var led = new GrudgeLedger();
            led.Form(Avenger, Target, GrudgeKind.Slaughter, intensity, 0, GrudgeCause.Direct, generation, null, L.GrudgeCap);
            return led;
        }

        // —— D12.2/D12.3 弟子继承衰减恩怨 + GrudgeInherited ——
        [Fact]
        public void test_heir_inherits_decayed_grudge()
        {
            var led = LedgerWithGrudge(intensity: 90, generation: 0);
            var dir = new DramaDirector(led, L);
            var mut = new RecordingMutator();
            // 死者 Avenger，在世继承人 [Heir]。
            dir.OnDeath(Avenger, new List<CharacterId> { Heir }, 100, mut);

            // 继承恩怨：Heir→Target，强度 90×60/100=54，gen 1，Cause=Inherited。
            var g = led.Get(Heir, Target);
            Assert.NotNull(g);
            Assert.Equal(54, g!.Intensity);
            Assert.Equal(1, g.Generation);
            Assert.Equal(GrudgeCause.Inherited, g.Cause);
            // 事件发出。
            var gi = mut.FirstInherited();
            Assert.NotNull(gi);
            Assert.Equal(3, gi!.Heir.Value);
            Assert.Equal(2, gi.Target.Value);
            Assert.Equal(54, gi.Intensity);
            Assert.Equal(1, gi.Generation);
        }

        // —— D12.3 衰减单调不增 ——
        [Fact]
        public void test_decay_monotonic_decrease()
        {
            var led = LedgerWithGrudge(intensity: 100, generation: 0);
            var dir = new DramaDirector(led, L);
            dir.OnDeath(Avenger, new List<CharacterId> { Heir }, 100, new RecordingMutator());
            var g = led.Get(Heir, Target);
            Assert.True(g!.Intensity < 100, "继承强度应严格小于原强度（衰减单调不增）");
            Assert.Equal(60, g.Intensity); // 100×60/100
        }

        // —— D12.4 MaxGeneration 封顶 ——
        [Fact]
        public void test_max_generation_stops_inheritance()
        {
            // 恩怨已是第 3 代（==MaxGeneration）→ 不再继承。
            var led = LedgerWithGrudge(intensity: 90, generation: L.MaxGeneration);
            var dir = new DramaDirector(led, L);
            var mut = new RecordingMutator();
            dir.OnDeath(Avenger, new List<CharacterId> { Heir }, 100, mut);
            Assert.Null(led.Get(Heir, Target));   // 无继承
            Assert.Equal(0, mut.CountOf<GrudgeInherited>());
        }

        // —— D12.5 绝嗣：无在世继承人 ——
        [Fact]
        public void test_no_living_heir_no_inheritance()
        {
            var led = LedgerWithGrudge(intensity: 90, generation: 0);
            var dir = new DramaDirector(led, L);
            var mut = new RecordingMutator();
            dir.OnDeath(Avenger, new List<CharacterId>(), 100, mut); // 空继承人
            Assert.Equal(0, mut.CountOf<GrudgeInherited>());
        }

        // —— D12.3 衰减殆尽（decayed<1）不继承 ——
        [Fact]
        public void test_decayed_to_zero_no_inheritance()
        {
            // 强度 1 × 60/100 = 0（整数除）→ 衰减殆尽不继承。
            // 但 intensity 1 < IgniteThreshold(60) 本就不强——用恰好阈值边界另测；
            // 此处用低 InheritDecayPct 制造衰减到 0：intensity=60(过阈), decay=1 → 60×1/100=0。
            var limits = L with { InheritDecayPct = 1 };
            var led = LedgerWithGrudge(intensity: 60, generation: 0);
            var dir = new DramaDirector(led, limits);
            var mut = new RecordingMutator();
            dir.OnDeath(Avenger, new List<CharacterId> { Heir }, 100, mut);
            Assert.Null(led.Get(Heir, Target));
            Assert.Equal(0, mut.CountOf<GrudgeInherited>());
        }

        // —— 弱恩怨（<阈值）不继承 ——
        [Fact]
        public void test_weak_grudge_below_threshold_not_inherited()
        {
            var led = LedgerWithGrudge(intensity: 50, generation: 0); // < IgniteThreshold 60
            var dir = new DramaDirector(led, L);
            var mut = new RecordingMutator();
            dir.OnDeath(Avenger, new List<CharacterId> { Heir }, 100, mut);
            Assert.Equal(0, mut.CountOf<GrudgeInherited>());
        }

        // —— D12.6 继承人取首位（World 已排序）——
        [Fact]
        public void test_inherits_to_first_sorted_heir()
        {
            var led = LedgerWithGrudge(intensity: 90, generation: 0);
            var dir = new DramaDirector(led, L);
            var mut = new RecordingMutator();
            // 多继承人：取 [0]（World 已按 年龄→武力→Id 排序）。
            dir.OnDeath(Avenger, new List<CharacterId> { new(7), new(8), new(9) }, 100, mut);
            var gi = mut.FirstInherited();
            Assert.NotNull(gi);
            Assert.Equal(7, gi!.Heir.Value); // 首位
        }

        // —— D12.9 Clone 含 profiles ——
        [Fact]
        public void test_clone_preserves_profiles()
        {
            var led = LedgerWithGrudge(intensity: 90, generation: 0);
            var dir = new DramaDirector(led, L);
            dir.RegisterProfile(new DramaProfile(Heir, Master: Avenger, Bloodline: null));
            var clone = dir.Clone(led.Clone());
            // 克隆应保留 profile：Heir 的 Master==Avenger。
            var p = clone.ProfileOf(Heir);
            Assert.NotNull(p);
            Assert.Equal(Avenger, p!.Master);
        }
    }
}
