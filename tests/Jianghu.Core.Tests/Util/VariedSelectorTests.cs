using System.Collections.Generic;
using Jianghu.Random;
using Jianghu.Util;
using Xunit;

namespace Jianghu.Core.Tests.Util
{
    /// <summary>
    /// VariedSelector&lt;TKey&gt; 共享原语（drama-003，spec Step 0）。
    /// 语义：每元素 long 使用计数；最小计数子集内注入 IRandom 均匀抽，抽后 +1。
    /// 纯整数确定性，破单调（长程均匀轮替），Clone 深拷。
    /// </summary>
    public class VariedSelectorTests
    {
        // 确定性 PRNG（Pcg32）供注入。
        static IRandom Rng(ulong seed = 42) => new Pcg32(seed, 1);

        [Fact]
        public void test_note_increments_usage()
        {
            var sel = new VariedSelector<string>();
            Assert.Equal(0, sel.UsageOf("a")); // 未见过 = 0
            sel.Note("a");
            sel.Note("a");
            Assert.Equal(2, sel.UsageOf("a"));
            Assert.Equal(0, sel.UsageOf("b"));
        }

        [Fact]
        public void test_pick_chooses_from_min_usage_and_increments()
        {
            var sel = new VariedSelector<string>();
            var cands = new List<string> { "a", "b", "c" };
            // 预置：a 用过 2 次，b 1 次，c 0 次 → 最小计数子集 = {c}，必抽 c。
            sel.Note("a"); sel.Note("a"); sel.Note("b");
            var picked = sel.Pick(cands, Rng());
            Assert.Equal("c", picked);
            Assert.Equal(1, sel.UsageOf("c")); // 抽后 +1
        }

        [Fact]
        public void test_pick_breaks_monotony_even_distribution()
        {
            // 破单调：3 元素抽 30 轮，每元素使用次数差 ≤ 1。
            var sel = new VariedSelector<int>();
            var cands = new List<int> { 1, 2, 3 };
            var rng = Rng();
            for (int i = 0; i < 30; i++) sel.Pick(cands, rng);
            long u1 = sel.UsageOf(1), u2 = sel.UsageOf(2), u3 = sel.UsageOf(3);
            long max = System.Math.Max(u1, System.Math.Max(u2, u3));
            long min = System.Math.Min(u1, System.Math.Min(u2, u3));
            Assert.True(max - min <= 1, $"使用次数应均匀(差≤1)，实际 {u1}/{u2}/{u3}");
            Assert.Equal(30L, u1 + u2 + u3);
        }

        [Fact]
        public void test_same_seed_same_sequence()
        {
            // 确定性：同种子 + 同候选 → 同抽取序列。
            var cands = new List<string> { "x", "y", "z" };
            var s1 = new VariedSelector<string>(); var r1 = Rng(7);
            var s2 = new VariedSelector<string>(); var r2 = Rng(7);
            for (int i = 0; i < 20; i++)
                Assert.Equal(s1.Pick(cands, r1), s2.Pick(cands, r2));
        }

        [Fact]
        public void test_clone_independent_and_equal()
        {
            var sel = new VariedSelector<string>();
            sel.Note("a"); sel.Note("a"); sel.Note("b");
            var clone = sel.Clone();
            Assert.Equal(sel.UsageOf("a"), clone.UsageOf("a")); // 计数相等
            Assert.Equal(sel.UsageOf("b"), clone.UsageOf("b"));
            // 独立：改 clone 不影响原。
            clone.Note("a");
            Assert.Equal(2, sel.UsageOf("a"));
            Assert.Equal(3, clone.UsageOf("a"));
        }

        [Fact]
        public void test_single_candidate_always_returns_it()
        {
            var sel = new VariedSelector<string>();
            var cands = new List<string> { "only" };
            Assert.Equal("only", sel.Pick(cands, Rng()));
            Assert.Equal("only", sel.Pick(cands, Rng()));
            Assert.Equal(2, sel.UsageOf("only"));
        }
    }
}
