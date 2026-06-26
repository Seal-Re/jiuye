using System;
using System.Collections.Generic;
using Jianghu.Random;
using Jianghu.Util;
using Xunit;

namespace Jianghu.Core.Tests.Util
{
    /// <summary>
    /// WeightedPicker 整数轮盘原语（drama-006，GDD §4）。无状态前缀和比例抽取。
    /// 语义：long 累加防溢出 → 单次 IRandom.NextInt((int)total) → 命中首个 draw&lt;前缀和 的索引。
    /// 零权重永不被选；与 VariedSelector（有状态轮替）分工互补。纯整数确定性。
    /// </summary>
    public class WeightedPickerTests
    {
        // 固定抽取 fake：NextInt 恒返预设值，逐边界验证轮盘命中（区别于 Pcg32 统计验证）。
        private sealed class FixedIntRandom : IRandom
        {
            private readonly int _value;
            public FixedIntRandom(int value) { _value = value; }
            public int NextInt(int maxExclusive) => _value;
            public uint NextUInt() => (uint)_value;
            public int NextInclusive(int minInclusive, int maxInclusive) => minInclusive + _value;
            public ulong[] GetState() => Array.Empty<ulong>();
            public void SetState(ulong[] state) { }
            public IRandom Split(ulong streamId) => this;
        }

        static IRandom Rng(ulong seed = 42) => new Pcg32(seed, 1);

        // —— D6.4/D6.5 前缀和边界 + 零权重永不被选 ——
        [Theory]
        [InlineData(0, 0)] // draw 0 → idx0 (前缀和 [3,3,4])
        [InlineData(1, 0)]
        [InlineData(2, 0)] // draw 2 < 3 → idx0
        [InlineData(3, 2)] // draw 3：跳过 idx0(acc3,3<3 no)、idx1(acc3,3<3 no)、idx2(acc4,3<4 yes)
        public void test_weighted_picker_prefix_sum_boundary(int draw, int expectedIdx)
        {
            // weights [3,0,1] total=4：idx1 权重 0，任何 draw 都不应命中它。
            var weights = new List<int> { 3, 0, 1 };
            int idx = WeightedPicker.PickIndex(weights, new FixedIntRandom(draw));
            Assert.Equal(expectedIdx, idx);
        }

        [Fact]
        public void test_zero_weight_index_never_selected()
        {
            // weights [3,0,1] total=4：穷举 draw∈[0,4) 不可能返回 idx1。
            var weights = new List<int> { 3, 0, 1 };
            for (int draw = 0; draw < 4; draw++)
            {
                int idx = WeightedPicker.PickIndex(weights, new FixedIntRandom(draw));
                Assert.NotEqual(1, idx);
            }
        }

        // —— D6.5 单候选 ——
        [Fact]
        public void test_single_candidate_returns_index_zero()
        {
            var weights = new List<int> { 7 };
            Assert.Equal(0, WeightedPicker.PickIndex(weights, new FixedIntRandom(0)));
            Assert.Equal(0, WeightedPicker.PickIndex(weights, Rng()));
        }

        // —— D6.5 比例收敛（确定 Pcg32 统计）：weights [1,3] → idx1 约 3× idx0 ——
        [Fact]
        public void test_weight_proportionality_converges()
        {
            var weights = new List<int> { 1, 3 }; // 期望 idx0:idx1 ≈ 1:3（25% / 75%）
            var rng = Rng(12345);
            int n = 4000;
            int c0 = 0, c1 = 0;
            for (int i = 0; i < n; i++)
            {
                int idx = WeightedPicker.PickIndex(weights, rng);
                if (idx == 0) c0++; else if (idx == 1) c1++; else Assert.Fail($"非法索引 {idx}");
            }
            Assert.Equal(n, c0 + c1);
            // idx0 期望 ~1000（25%）：宽容带 [800,1200]（±5pp）。
            Assert.InRange(c0, 800, 1200);
            // idx1 期望 ~3000（75%）。
            Assert.InRange(c1, 2800, 3200);
        }

        // —— D6.7 确定性：同 weights + 同 rng 状态 → 同索引 ——
        [Fact]
        public void test_same_state_same_index()
        {
            var weights = new List<int> { 5, 2, 8, 1 };
            var r1 = Rng(7);
            var r2 = Rng(7);
            for (int i = 0; i < 50; i++)
                Assert.Equal(WeightedPicker.PickIndex(weights, r1), WeightedPicker.PickIndex(weights, r2));
        }

        // —— D6.6 防御性边界 ——
        [Fact]
        public void test_empty_weights_throws()
            => Assert.Throws<ArgumentException>(() => WeightedPicker.PickIndex(new List<int>(), Rng()));

        [Fact]
        public void test_null_weights_throws()
            => Assert.Throws<ArgumentException>(() => WeightedPicker.PickIndex(null!, Rng()));

        [Fact]
        public void test_negative_weight_throws()
            => Assert.Throws<ArgumentException>(() => WeightedPicker.PickIndex(new List<int> { 3, -1, 2 }, Rng()));

        [Fact]
        public void test_all_zero_weights_throws()
            => Assert.Throws<ArgumentException>(() => WeightedPicker.PickIndex(new List<int> { 0, 0, 0 }, Rng()));

        [Fact]
        public void test_total_overflow_int_max_throws()
        {
            // 两个 int.MaxValue/2+ 求和越 int.MaxValue → 必须抛（杜绝静默环绕，强制调用方 MaxArcWeightSum 守门）。
            var weights = new List<int> { int.MaxValue, 1 };
            Assert.Throws<ArgumentException>(() => WeightedPicker.PickIndex(weights, Rng()));
        }
    }
}
