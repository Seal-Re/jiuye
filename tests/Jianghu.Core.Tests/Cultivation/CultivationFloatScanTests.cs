using System.Collections.Generic;

namespace Jianghu.Cultivation.__MetaProbe
{
    // 元测试探针：故意含浮点 IL（ldc.r8 + conv.r4），证明扫描器真能抓到 float。
    // 位于测试程序集的 Jianghu.Cultivation.* 命名空间下，不污染 Jianghu.Core。
    internal static class FloatProbe
    {
        public static double LiteralR8() => 1.5;                 // ldc.r8
        public static float ConvR4(int x) => (float)x + 0.0f;    // conv.r4 / ldc.r4
    }
}

public class CultivationFloatScanTests
{
    // 真扫描：Jianghu.Core 程序集里 Jianghu.Cultivation.* 命名空间当前为空集 → 0 offenders。
    [Fact]
    public void Cultivation_Namespace_HasNoFloatOpcodes()
    {
        var asmPath = typeof(Jianghu.Sim.World).Assembly.Location;
        var offenders = ILFloatScanner.ScanNamespace(asmPath, "Jianghu.Cultivation");
        Assert.True(offenders.Count == 0, "浮点出现在: " + string.Join(", ", offenders));
    }

    // 元测试：扫描器对故意塞了浮点的探针类型必须报告 offender，否则扫描器无效。
    [Fact]
    public void Scanner_DetectsFloatInProbe()
    {
        var asmPath = typeof(CultivationFloatScanTests).Assembly.Location;
        var offenders = ILFloatScanner.ScanNamespace(asmPath, "Jianghu.Cultivation.__MetaProbe");
        Assert.Contains(offenders, o => o.Contains("LiteralR8"));
        Assert.Contains(offenders, o => o.Contains("ConvR4"));
    }
}
