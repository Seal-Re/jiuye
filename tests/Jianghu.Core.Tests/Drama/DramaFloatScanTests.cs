namespace Jianghu.Util.__MetaProbe
{
    // 元探针：故意含浮点 IL，证明扫描器对 Jianghu.Util 前缀也有效（同 Cultivation 探针模式）。
    // 位于测试程序集命名空间下，不污染 Jianghu.Core 生产程序集。
    internal static class UtilFloatProbe
    {
        public static double LiteralR8() => 2.5;              // ldc.r8
        public static float ConvR4(int x) => (float)x + 1.0f; // conv.r4 / ldc.r4
    }
}

namespace Jianghu.Core.Tests.Drama
{
    using Xunit;

    /// <summary>
    /// drama-006 D6.8：IL 浮点扫描覆盖 Jianghu.Util + Jianghu.Drama（补 GDD §0.2 诚实缺口——
    /// 此前 ILFloatScanner 仅被 CultivationFloatScanTests 用于 Jianghu.Cultivation）。
    /// B.2 整数确定性：戏剧/共享原语命名空间零浮点 opcode。
    /// </summary>
    public class DramaFloatScanTests
    {
        // 生产程序集（Jianghu.Core）中 Jianghu.Drama 命名空间零浮点。
        [Fact]
        public void Drama_Namespace_HasNoFloatOpcodes()
        {
            var asmPath = typeof(Jianghu.Drama.GrudgeLedger).Assembly.Location;
            var offenders = ILFloatScanner.ScanNamespace(asmPath, "Jianghu.Drama");
            Assert.True(offenders.Count == 0, "Jianghu.Drama 浮点出现在: " + string.Join(", ", offenders));
        }

        // 生产程序集中 Jianghu.Util 命名空间零浮点（VariedSelector / WeightedPicker）。
        [Fact]
        public void Util_Namespace_HasNoFloatOpcodes()
        {
            var asmPath = typeof(Jianghu.Util.WeightedPicker).Assembly.Location;
            var offenders = ILFloatScanner.ScanNamespace(asmPath, "Jianghu.Util");
            Assert.True(offenders.Count == 0, "Jianghu.Util 浮点出现在: " + string.Join(", ", offenders));
        }

        // 元测试：扫描器对故意塞浮点的 Util 探针必须报告 offender，否则扫描器无效。
        [Fact]
        public void Scanner_DetectsFloatInUtilProbe()
        {
            var asmPath = typeof(DramaFloatScanTests).Assembly.Location;
            var offenders = ILFloatScanner.ScanNamespace(asmPath, "Jianghu.Util.__MetaProbe");
            Assert.Contains(offenders, o => o.Contains("LiteralR8"));
            Assert.Contains(offenders, o => o.Contains("ConvR4"));
        }
    }
}
