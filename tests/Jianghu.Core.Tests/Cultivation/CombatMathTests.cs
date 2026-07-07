using Jianghu.Cultivation;
using Xunit;

namespace Jianghu.Core.Tests.Cultivation
{
    /// <summary>
    /// cv-001（adr-0008 决策②）：CombatMath 整数查表 permille 映射纯函数测试。
    /// 无 RNG、无 I/O、纯函数——确定性直验（B.2）。覆盖 AC-1（查表单调 + 钳制 + 极端桶）。
    /// </summary>
    public class CombatMathTests
    {
        [Fact]
        public void test_zero_margin_is_fifty_percent()
        {
            // 同 PE → 500‰（50/50 悬念拉满，adr-0008 α）
            Assert.Equal(500, CombatMath.GetSuccessPermille(0, 100));
        }

        [Fact]
        public void test_positive_margin_raises_above_500()
        {
            // 攻方占优 → >500（相对差 +20% → +100‰ = 600）
            Assert.Equal(600, CombatMath.GetSuccessPermille(20, 100));
        }

        [Fact]
        public void test_negative_margin_drops_below_500()
        {
            // 攻方劣势 → <500（相对差 −20% → −100‰ = 400）
            Assert.Equal(400, CombatMath.GetSuccessPermille(-20, 100));
        }

        [Fact]
        public void test_monotonic_increasing_in_margin()
        {
            // 单调：margin 越大，permille 越高（同一 defenderPe 基准）
            int prev = CombatMath.GetSuccessPermille(-200, 100);
            for (int m = -190; m <= 200; m += 10)
            {
                int cur = CombatMath.GetSuccessPermille(m, 100);
                Assert.True(cur >= prev, $"非单调 @margin={m}: {cur} < {prev}");
                prev = cur;
            }
        }

        [Fact]
        public void test_extreme_advantage_clamps_to_999()
        {
            // 碾压（相对差 ≥+100%）→ 上钳 999（保 1‰ 理论失手，绝对秒杀走 cv-004）
            Assert.Equal(999, CombatMath.GetSuccessPermille(100, 100));
            Assert.Equal(999, CombatMath.GetSuccessPermille(100000, 100));
        }

        [Fact]
        public void test_extreme_disadvantage_clamps_to_1()
        {
            // 被碾压（相对差 ≤−100%）→ 下钳 1（保 1‰ 理论反杀，弱胜强地基）
            Assert.Equal(1, CombatMath.GetSuccessPermille(-100, 100));
            Assert.Equal(1, CombatMath.GetSuccessPermille(-100000, 100));
        }

        [Fact]
        public void test_never_absolute_zero_or_thousand()
        {
            // 值域恒 [1,999]：既不 0（弱者永有反杀希望）也不 1000（强者永有失手可能）
            for (int m = -100000; m <= 100000; m += 137)
            {
                int p = CombatMath.GetSuccessPermille(m, 100);
                Assert.InRange(p, 1, 999);
            }
        }

        [Fact]
        public void test_zero_defender_pe_no_divide_by_zero()
        {
            // defenderPe=0 兜底（用 1 作基准），不抛除零
            int p = CombatMath.GetSuccessPermille(50, 0);
            Assert.InRange(p, 1, 999);
        }

        [Fact]
        public void test_scale_invariant_relative_margin()
        {
            // 相对差相同 → permille 相同（跨 UT 通用）：+20% 相对差在低/高 PE 下同值
            Assert.Equal(
                CombatMath.GetSuccessPermille(20, 100),   // 20/100 = +20%
                CombatMath.GetSuccessPermille(200, 1000)); // 200/1000 = +20%
        }

        [Fact]
        public void test_deterministic_pure_function()
        {
            // 纯函数：同输入恒同输出（B.2）
            for (int m = -300; m <= 300; m += 50)
                Assert.Equal(
                    CombatMath.GetSuccessPermille(m, 250),
                    CombatMath.GetSuccessPermille(m, 250));
        }
    }
}
