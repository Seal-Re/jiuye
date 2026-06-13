using Jianghu.Config;
using Jianghu.Sim;
using Xunit;

namespace Jianghu.Core.Tests.Determinism
{
    /// <summary>
    /// Task 2.4 命门：cultivation-off（默认）→ 与 v1.0 轨迹逐字节一致。
    /// off 绝不构造/消费 _cultRng（不调 Split(5)），Split(1..4) 子流编号不变。
    /// 默认参数 CreateInitial（无 cultivation/pathSource）= 既有 38 测试同款 caller。
    /// </summary>
    public class OffByteIdenticalTests
    {
        static string Run(ulong seed, int steps, int budget)
        {
            var w = WorldFactory.CreateInitial(seed, LimitsConfig.Default, 5);
            for (int i = 0; i < steps; i++) w.Advance(budget);
            return string.Join("\n", w.Chronicle.Lines);
        }

        [Fact]
        public void Off_DefaultCtor_SameSeedSameChronicle()
        {
            // 默认 cultivation=false：同种子两跑逐字节。
            Assert.Equal(Run(777, 300, 6), Run(777, 300, 6));
        }

        [Fact]
        public void Off_ExplicitFalse_EqualsDefault()
        {
            // 显式 cultivation:false 与默认参数轨迹完全相同（off 路径不分叉）。
            var def = WorldFactory.CreateInitial(2026, LimitsConfig.Default, 5);
            var explicitOff = WorldFactory.CreateInitial(2026, LimitsConfig.Default, 5, cultivation: false);
            for (int i = 0; i < 200; i++) { def.Advance(6); explicitOff.Advance(6); }
            Assert.Equal(
                string.Join("\n", def.Chronicle.Lines),
                string.Join("\n", explicitOff.Chronicle.Lines));
        }

        [Fact]
        public void Off_NoCultivationAttached()
        {
            var w = WorldFactory.CreateInitial(777, LimitsConfig.Default, 5);
            for (int i = 0; i < 100; i++) w.Advance(6);
            foreach (var c in w.AliveCharacters())
                Assert.Null(c.Cultivation); // off → 全员散修
        }

        [Fact]
        public void Off_CloneContinuesIdentically()
        {
            var full = WorldFactory.CreateInitial(7, LimitsConfig.Default, 5);
            for (int i = 0; i < 120; i++) full.Advance(6);
            var fullText = string.Join("\n", full.Chronicle.Lines);

            var part = WorldFactory.CreateInitial(7, LimitsConfig.Default, 5);
            for (int i = 0; i < 60; i++) part.Advance(6);
            var clone = part.Clone();
            for (int i = 0; i < 60; i++) clone.Advance(6);

            Assert.Equal(fullText, string.Join("\n", clone.Chronicle.Lines));
        }
    }
}
