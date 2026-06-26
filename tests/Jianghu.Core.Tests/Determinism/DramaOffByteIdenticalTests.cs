using Jianghu.Config;
using Jianghu.Sim;
using Xunit;

namespace Jianghu.Core.Tests.Determinism
{
    /// <summary>
    /// drama-008 ⚠️ 命门：戏剧 DomainEvent/Chronicle case 纯加，off 模式**永不触发**——
    /// off 轨迹逐字节不变（既有 OffByteIdentical 基线不退）。
    /// 安全性：6 新事件由戏剧层（drama-009/010）产出，off 根本不构造 drama 子系统 → 新 case 不可达。
    /// </summary>
    public class DramaOffByteIdenticalTests
    {
        static string Run(ulong seed, int steps, int budget)
        {
            var w = WorldFactory.CreateInitial(seed, LimitsConfig.Default, 5);
            for (int i = 0; i < steps; i++) w.Advance(budget);
            return string.Join("\n", w.Chronicle.Lines);
        }

        // off 跑长程：Chronicle 不含任何戏剧特征串（6 新事件文本均不出现）。
        [Fact]
        public void Off_Chronicle_ContainsNoDramaLines()
        {
            string chron = Run(2026, 400, 6);
            // drama-008 六事件渲染特征串（武侠味措辞），off 下绝不出现。
            Assert.DoesNotContain("血仇", chron);
            Assert.DoesNotContain("残身之仇", chron);
            Assert.DoesNotContain("羞辱之仇", chron);
            Assert.DoesNotContain("复仇", chron);
            Assert.DoesNotContain("父债子偿", chron);
            Assert.DoesNotContain("继承", chron);
            Assert.DoesNotContain("手刃仇人", chron);
            Assert.DoesNotContain("饮恨当场", chron);
        }

        // off 同种子两跑逐字节（新 case 不引入任何分叉）。
        [Fact]
        public void Off_SameSeed_ByteIdentical_AfterDramaEvents()
        {
            Assert.Equal(Run(42, 300, 6), Run(42, 300, 6));
            Assert.Equal(Run(999, 250, 8), Run(999, 250, 8));
        }
    }
}
