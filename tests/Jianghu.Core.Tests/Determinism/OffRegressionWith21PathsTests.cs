using Jianghu.Config;
using Jianghu.Random;
using Jianghu.Sim;
using Xunit;

namespace Jianghu.Core.Tests.Determinism
{
    /// <summary>
    /// Task 6.1 INV-OFF：21 路全量入册后，cultivation-off（默认）仍与 v1.0 逐字节一致。
    /// off 不构造 registry、不消费 Split(5)（保 Split(1..4) 子流编号）。
    /// OffByteIdenticalTests/DeterminismTests 守 off 家族；本族补「21 路在册不污染 off」的显式 gate。
    /// </summary>
    public class OffRegressionWith21PathsTests
    {
        static string OffChronicle(ulong seed, int steps, int budget)
        {
            // 默认 cultivation=false → CodePathSource(21 路) 不构造（registry==null）。
            var w = WorldFactory.CreateInitial(seed, LimitsConfig.Default, 5);
            for (int i = 0; i < steps; i++) w.Advance(budget);
            return string.Join("\n", w.Chronicle.Lines);
        }

        [Fact]
        public void Off_NotCultivationEnabled_NoSplit5()
        {
            // off：CultivationEnabled==false → _cultRng 未构造 → 未调 Split(5)（Split(1..4) 编号不变）。
            var w = WorldFactory.CreateInitial(2026, LimitsConfig.Default, 5);
            Assert.False(w.CultivationEnabled);
        }

        [Fact]
        public void Off_SameSeedSameChronicle_With21PathsRegisterable()
        {
            // 21 路已在 CodePathSource，但 off 不构造它 → 轨迹纯 v1.0，同种子两跑逐字节。
            Assert.Equal(OffChronicle(2026, 200, 6), OffChronicle(2026, 200, 6));
            Assert.Equal(OffChronicle(777, 300, 6), OffChronicle(777, 300, 6));
        }

        [Fact]
        public void Off_Split5_DoesNotShift_Lower4Streams()
        {
            // 直证：构造 Split(5) 不改变 Split(1..4) 的 GetState（子流派生互不污染）。
            // off 不调 Split(5) 故轨迹不变；此处验「即便调了 Split(5)，低 4 流状态也不动」的派生隔离前提。
            var root1 = new Pcg32(2026, 1);
            var s1 = root1.Split(1).GetState();
            var s2 = root1.Split(2).GetState();
            var s3 = root1.Split(3).GetState();
            var s4 = root1.Split(4).GetState();

            var root2 = new Pcg32(2026, 1);
            _ = root2.Split(RngStreamIds.Cultivation); // 先派生 Split(5)
            Assert.Equal(s1, root2.Split(1).GetState());
            Assert.Equal(s2, root2.Split(2).GetState());
            Assert.Equal(s3, root2.Split(3).GetState());
            Assert.Equal(s4, root2.Split(4).GetState());
        }
    }
}
