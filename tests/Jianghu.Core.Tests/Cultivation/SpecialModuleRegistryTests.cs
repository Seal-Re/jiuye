using Jianghu.Cultivation;
using Xunit;

namespace Jianghu.Core.Tests.Cultivation
{
    /// <summary>
    /// 唯一档逃逸口纪律（spec §7）：SpecialModuleRegistry 派发确定（同 id → 同单例）；
    /// 缺失 id 抛 InvalidOperationException（不静默回 null）。
    /// </summary>
    public class SpecialModuleRegistryTests
    {
        [Fact]
        public void Registry_SameId_SameHandler() =>
            Assert.Same(SpecialModuleRegistry.Get("noop"), SpecialModuleRegistry.Get("noop")); // 派发确定

        [Fact]
        public void Registry_UnknownId_Throws() =>
            Assert.Throws<System.InvalidOperationException>(() => SpecialModuleRegistry.Get("nonexistent"));
    }
}
