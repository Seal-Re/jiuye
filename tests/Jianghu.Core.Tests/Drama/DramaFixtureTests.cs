using Jianghu.Config;
using Jianghu.Sim;
using Xunit;

namespace Jianghu.Core.Tests.Drama
{
    /// <summary>
    /// drama-013 预置冤孽 fixture（spec §5）：seed 控制下预置强恩怨 + 师徒边，保证首刀必有可观测复仇线。
    /// 经 genRng 独立子流（不碰其他流）→ off/dramaOff 不预置 → 逐字节守恒。
    /// </summary>
    public class DramaFixtureTests
    {
        // —— D13.1 dramaSeedFeuds + dramaOn → 预置强恩怨 ——
        [Fact]
        public void test_seed_feuds_presets_strong_grudge()
        {
            var w = WorldFactory.CreateInitial(2026, LimitsConfig.Default, 8, dramaOn: true, dramaSeedFeuds: true);
            Assert.NotNull(w.Grudges);
            Assert.True(w.Grudges!.Count >= 1, "dramaSeedFeuds 应预置至少 1 对强恩怨");
            // 预置的恩怨强度应过点火阈值（强恩怨）。
            Assert.Contains(w.Grudges.All, g => g.Intensity >= LimitsConfig.Default.GrudgeIgniteThreshold);
        }

        // —— D13.2 dramaSeedFeuds 但 dramaOff → 不预置（无 Grudges 容器）——
        [Fact]
        public void test_seed_feuds_without_drama_on_is_noop()
        {
            var w = WorldFactory.CreateInitial(2026, LimitsConfig.Default, 8, dramaOn: false, dramaSeedFeuds: true);
            Assert.Null(w.Grudges); // drama off → 无账本
        }

        // —— D13.2 默认（dramaSeedFeuds=false）→ 不预置 ——
        [Fact]
        public void test_no_feuds_by_default()
        {
            var w = WorldFactory.CreateInitial(2026, LimitsConfig.Default, 8, dramaOn: true);
            Assert.NotNull(w.Grudges);
            Assert.Equal(0, w.Grudges!.Count); // dramaOn 但未 seedFeuds → 空账本
        }

        // —— ⚠️ D13.2 off 逐字节：dramaSeedFeuds 不影响 off 轨迹 ——
        [Fact]
        public void test_off_byte_identical_regardless_of_seed_feuds_flag()
        {
            string Run(bool seedFeuds)
            {
                // off 模式（dramaOn=false）：dramaSeedFeuds 标记应完全无影响（feudRng 不构造）。
                var w = WorldFactory.CreateInitial(42, LimitsConfig.Default, 8, dramaOn: false, dramaSeedFeuds: seedFeuds);
                for (int i = 0; i < 200; i++) w.Advance(6);
                return string.Join("\n", w.Chronicle.Lines);
            }
            Assert.Equal(Run(false), Run(true)); // off 下 seedFeuds 标记不分叉
        }

        // —— D13.1 fixture 确定性：同种子两跑同预置 ——
        [Fact]
        public void test_seed_feuds_deterministic()
        {
            var w1 = WorldFactory.CreateInitial(99, LimitsConfig.Default, 8, dramaOn: true, dramaSeedFeuds: true);
            var w2 = WorldFactory.CreateInitial(99, LimitsConfig.Default, 8, dramaOn: true, dramaSeedFeuds: true);
            Assert.Equal(w1.Grudges!.Count, w2.Grudges!.Count);
            for (int i = 0; i < w1.Grudges.All.Count; i++)
            {
                Assert.Equal(w1.Grudges.All[i].Holder, w2.Grudges.All[i].Holder);
                Assert.Equal(w1.Grudges.All[i].Target, w2.Grudges.All[i].Target);
                Assert.Equal(w1.Grudges.All[i].Intensity, w2.Grudges.All[i].Intensity);
            }
        }
    }
}
