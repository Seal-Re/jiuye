using System.Linq;
using System.Text.RegularExpressions;
using Jianghu.Config;
using Jianghu.Sim;
using Xunit;
using Xunit.Abstractions;

namespace Jianghu.Core.Tests.Sim
{
    /// <summary>
    /// balance-004 / TR-BAL-001：切磋碎压平滑（sim 级）。无势均对手时 RuleBrain 降切磋意愿→转
    /// 修炼/游历，减少 999 碾压刷屏（观察者可读性）。sim 级验证（动态 goal/streak，单元级固定
    /// goal 无法复现）。目标：多 seed 长跑碾压≥999 占比 &lt; 25%。
    /// Red lines: B.2（确定性）、B.3（off 逐字节——RuleBrain SelfPower=0 回退不变）。
    /// </summary>
    public class SparStompRateTests
    {
        readonly ITestOutputHelper _out;
        public SparStompRateTests(ITestOutputHelper o) { _out = o; }

        // 返回 (同UT可避碾压对拍总数, 其中碾压数)。排除 UT-gap≥2 auto-win（int.MaxValue，
        // C2 结构必然、非 brain 可避、balance-004 不该管）。只统计 brain 选择的势均/碾压对拍。
        static (int total, int stomp) StompRate(ulong seed, int steps)
        {
            var w = WorldFactory.CreateInitial(seed, LimitsConfig.Default, 8, cultivation: true);
            for (int i = 0; i < steps; i++) w.Advance(8);
            var margins = w.Chronicle.Lines.Where(l => l.Contains("切磋，胜"))
                .Select(l => { var m = Regex.Match(l, "差 ([0-9]+)"); return m.Success ? long.Parse(m.Groups[1].Value) : -1L; })
                .Where(x => x >= 0 && x < int.MaxValue) // 排除 auto-win（margin==int.MaxValue）
                .ToList();
            // 非 auto-win 对拍里，margin≥999 视为"实际碾压"（一方几乎无伤取胜）。
            return (margins.Count, margins.Count(x => x >= 999));
        }

        [Fact]
        public void test_spar_stomp_rate_below_threshold()
        {
            int total = 0, stomp = 0;
            foreach (ulong seed in new ulong[] { 42, 99, 2026 })
            {
                var (t, s) = StompRate(seed, 800);
                total += t; stomp += s;
                _out.WriteLine($"seed {seed}: {t} 场, 碾压≥999 {s} ({(t > 0 ? s * 100 / t : 0)}%)");
            }
            int pct = total > 0 ? stomp * 100 / total : 0;
            _out.WriteLine($"合计: {total} 场, 碾压 {stomp} ({pct}%)");
            Assert.True(pct < 25, $"切磋碾压≥999 占比 {pct}% ≥ 25%（balance-004：无势均降切磋意愿后应 <25%）。");
        }
    }
}
