using Jianghu.Config;
using Jianghu.Sim;
using Xunit;

namespace Jianghu.Core.Tests.Determinism
{
    /// <summary>
    /// drama-010 ⚠️ 命门：World 接 DramaDirector 后，off（dramaOn=false 默认）仍与 v1.0 逐字节；
    /// drama-on 但空库时（无预置恩怨）亦与 off 逐字节——证 Split(6) 构造不扰 Split(1..4)、
    /// 空库 Pump no-op 不消费 dramaRng、不产 Chronicle 行。
    /// </summary>
    public class DramaWiringByteIdenticalTests
    {
        static string Run(World w, int steps, int budget)
        {
            for (int i = 0; i < steps; i++) w.Advance(budget);
            return string.Join("\n", w.Chronicle.Lines);
        }

        // dramaOn=false（默认）与显式不传 → 完全相同（off 路径不分叉）。
        [Fact]
        public void Off_DramaDefault_EqualsExplicitFalse()
        {
            var def = WorldFactory.CreateInitial(2026, LimitsConfig.Default, 8);
            var explicitOff = WorldFactory.CreateInitial(2026, LimitsConfig.Default, 8, dramaOn: false);
            Assert.Equal(Run(def, 200, 6), Run(explicitOff, 200, 6));
        }

        // ⚠️ dramaOn=true 但空恩怨库 → 与 off 逐字节（Split(6) 构造本身不扰其他流，空库 Pump no-op）。
        [Fact]
        public void DramaOn_EmptyLedger_ByteIdenticalToOff()
        {
            var off = WorldFactory.CreateInitial(777, LimitsConfig.Default, 8);
            var dramaOn = WorldFactory.CreateInitial(777, LimitsConfig.Default, 8, dramaOn: true);
            Assert.Equal(Run(off, 300, 6), Run(dramaOn, 300, 6));
        }

        // 多种子 + 多预算稳健性。
        [Theory]
        [InlineData(1, 250, 4)]
        [InlineData(42, 200, 6)]
        [InlineData(999, 150, 8)]
        public void DramaOn_EmptyLedger_ByteIdentical_MultiSeed(ulong seed, int steps, int budget)
        {
            var off = WorldFactory.CreateInitial(seed, LimitsConfig.Default, 8);
            var dramaOn = WorldFactory.CreateInitial(seed, LimitsConfig.Default, 8, dramaOn: true);
            Assert.Equal(Run(off, steps, budget), Run(dramaOn, steps, budget));
        }
    }
}
