using System;
using System.Collections.Generic;
using Jianghu.Drama;
using Jianghu.Model;
using Jianghu.Random;
using Xunit;

namespace Jianghu.Core.Tests.Drama
{
    /// <summary>
    /// StoryletSelector（drama-007，spec §3 候选 + §4 裁决序）：过滤(Arc/Stage/谓词)→
    /// 确定性排序(BaseWeight desc, Id asc)→WeightedPicker 抽取。空候选→null 不消费 rng。
    /// w&lt;1 兜底为 1。禁 Dictionary/HashSet 枚举序参与裁决。
    /// </summary>
    public class StoryletSelectorTests
    {
        private sealed class FixedIntRandom : IRandom
        {
            private readonly int _value;
            private int _calls;
            public int Calls => _calls;
            public FixedIntRandom(int value) { _value = value; }
            public int NextInt(int maxExclusive) { _calls++; return _value; }
            public uint NextUInt() { _calls++; return (uint)_value; }
            public int NextInclusive(int min, int max) { _calls++; return min + _value; }
            public ulong[] GetState() => Array.Empty<ulong>();
            public void SetState(ulong[] state) { }
            public IRandom Split(ulong streamId) => this;
        }

        private sealed class FakeView : IDramaView
        {
            public Dictionary<long, int> Powers = new();
            public int Power(CharacterId who) => Powers.TryGetValue(who.Value, out var p) ? p : 0;
            public int Affinity(CharacterId from, CharacterId to) => 0;
            public bool IsAlive(CharacterId who) => true;
            public bool SameNode(CharacterId a, CharacterId b) => false;
        }

        private static readonly CharacterId Avenger = new(1);
        private static readonly CharacterId Target = new(2);
        private static ArcInstance Arc(ArcStage stage)
            => new ArcInstance(new ArcId(10), ArcKind.Revenge, Avenger, Target, stage, 0, 0, false);

        private static StoryletSpec Spec(int id, int weight, ArcStage stage,
            IReadOnlyList<Predicate>? pre = null) => new StoryletSpec(
            id, ArcKind.Revenge, stage, weight, false, 0, CooldownScope.Global,
            pre ?? new List<Predicate>(), new List<Effect>(), "t");

        static IRandom Rng(ulong seed = 42) => new Pcg32(seed, 1);

        // —— D7.6 过滤：错 Stage 被排除 ——
        [Fact]
        public void test_filters_out_wrong_stage()
        {
            var pool = new List<StoryletSpec> { Spec(1, 100, ArcStage.BuildUp) };
            var ctx = new DramaContext(new FakeView(), Arc(ArcStage.Hunting));
            var picked = StoryletSelector.Select(pool, Arc(ArcStage.Hunting), ctx, Rng());
            Assert.Null(picked); // pool 内唯一 storylet 属 BuildUp，当前 Hunting → 无候选
        }

        [Fact]
        public void test_filters_out_failing_precondition()
        {
            // 谓词要求 Self.Power>=500，但 view 给 100 → 不过 → 无候选。
            var pre = new List<Predicate> { new Predicate(RoleRef.Self, DramaVar.Power, CmpOp.Ge, 500) };
            var pool = new List<StoryletSpec> { Spec(1, 100, ArcStage.Hunting, pre) };
            var view = new FakeView { Powers = { [1] = 100 } };
            var ctx = new DramaContext(view, Arc(ArcStage.Hunting));
            Assert.Null(StoryletSelector.Select(pool, Arc(ArcStage.Hunting), ctx, Rng()));
        }

        [Fact]
        public void test_passing_precondition_is_eligible()
        {
            var pre = new List<Predicate> { new Predicate(RoleRef.Self, DramaVar.Power, CmpOp.Ge, 50) };
            var pool = new List<StoryletSpec> { Spec(7, 100, ArcStage.Hunting, pre) };
            var view = new FakeView { Powers = { [1] = 100 } };
            var ctx = new DramaContext(view, Arc(ArcStage.Hunting));
            var picked = StoryletSelector.Select(pool, Arc(ArcStage.Hunting), ctx, Rng());
            Assert.NotNull(picked);
            Assert.Equal(7, picked!.Id);
        }

        // —— D7.6 空候选→null，不消费 rng ——
        [Fact]
        public void test_empty_candidates_returns_null_without_consuming_rng()
        {
            var pool = new List<StoryletSpec>(); // 空池
            var ctx = new DramaContext(new FakeView(), Arc(ArcStage.Hunting));
            var rng = new FixedIntRandom(0);
            Assert.Null(StoryletSelector.Select(pool, Arc(ArcStage.Hunting), ctx, rng));
            Assert.Equal(0, rng.Calls); // no-op：未抽取
        }

        // —— D7.6 权重抽取命中（前缀和边界）——
        [Fact]
        public void test_weighted_pick_hits_by_prefix_sum()
        {
            // 两候选排序后 weights=[30,10]（id1 先，权重 desc 已是 30,10）。
            // draw=0 → idx0 (id 较大权重那条)；draw=30 → idx1。
            var pool = new List<StoryletSpec>
            {
                Spec(1, 30, ArcStage.Hunting),
                Spec(2, 10, ArcStage.Hunting),
            };
            var ctx = new DramaContext(new FakeView(), Arc(ArcStage.Hunting));
            var hit0 = StoryletSelector.Select(pool, Arc(ArcStage.Hunting), ctx, new FixedIntRandom(0));
            Assert.Equal(1, hit0!.Id);   // draw 0 落第一候选
            var hit1 = StoryletSelector.Select(pool, Arc(ArcStage.Hunting), ctx, new FixedIntRandom(30));
            Assert.Equal(2, hit1!.Id);   // draw 30 落第二候选
        }

        // —— D7.6 排序：BaseWeight desc, Id asc（与 pool 原序无关）——
        [Fact]
        public void test_deterministic_sort_weight_desc_id_asc()
        {
            // pool 乱序，但排序后 [w50,w50(id小先),w10]。draw=0 → 权重最大且 id 最小那条。
            var pool = new List<StoryletSpec>
            {
                Spec(9, 10, ArcStage.Hunting),
                Spec(5, 50, ArcStage.Hunting),
                Spec(3, 50, ArcStage.Hunting), // 同权重 50，id 3 < 5 → 排前
            };
            var ctx = new DramaContext(new FakeView(), Arc(ArcStage.Hunting));
            var picked = StoryletSelector.Select(pool, Arc(ArcStage.Hunting), ctx, new FixedIntRandom(0));
            Assert.Equal(3, picked!.Id); // draw 0 → 排序首位 = (w50,id3)
        }

        // —— D7.6 w<1 兜底为 1：BaseWeight=0 仍可被选 ——
        [Fact]
        public void test_zero_baseweight_floored_to_one_still_selectable()
        {
            var pool = new List<StoryletSpec> { Spec(1, 0, ArcStage.Hunting) }; // 权重 0
            var ctx = new DramaContext(new FakeView(), Arc(ArcStage.Hunting));
            var picked = StoryletSelector.Select(pool, Arc(ArcStage.Hunting), ctx, new FixedIntRandom(0));
            Assert.NotNull(picked); // 兜底 w=1 → total=1 → 可抽
            Assert.Equal(1, picked!.Id);
        }

        // —— D7.7 确定性：同 rng 状态两跑同结果 ——
        [Fact]
        public void test_same_rng_state_same_pick()
        {
            var pool = new List<StoryletSpec>
            {
                Spec(1, 30, ArcStage.Hunting),
                Spec(2, 30, ArcStage.Hunting),
                Spec(3, 40, ArcStage.Hunting),
            };
            var ctx = new DramaContext(new FakeView(), Arc(ArcStage.Hunting));
            for (int i = 0; i < 20; i++)
            {
                var a = StoryletSelector.Select(pool, Arc(ArcStage.Hunting), ctx, Rng(99));
                var b = StoryletSelector.Select(pool, Arc(ArcStage.Hunting), ctx, Rng(99));
                Assert.Equal(a!.Id, b!.Id);
            }
        }
    }
}
