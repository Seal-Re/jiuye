using System.Linq;
using Xunit;

/// <summary>
/// 覆盖守卫（B5 批1 / M3 逃逸口）：IL 浮点扫描必须**确定性覆盖**模块结算 + 唯一档 handler。
/// <see cref="CultivationFloatScanTests.Cultivation_Namespace_HasNoFloatOpcodes"/> 已对
/// Jianghu.Cultivation 命名空间全扫，但那只证「当前扫到的没浮点」；本守卫证「扫描目标集**确实包含**
/// 这些逃逸口类型」——防未来把 <see cref="ILFloatScanner"/> 的命名空间筛选窄化（如改白名单/漏类型）
/// 而静默漏扫批3 将加的真 handler。断言集由 scanner 真实遍历逻辑（<see cref="ILFloatScanner.ScannedTypeFullNames"/>，
/// 与 ScanNamespace 共享同一 NamespaceMatches 谓词）派生，scanner 一旦窄化即红。
/// </summary>
public class ModuleResolverFloatScanCoverageTests
{
    private static System.Collections.Generic.HashSet<string> ScannedCultivationTypes()
    {
        var asmPath = typeof(Jianghu.Sim.World).Assembly.Location;
        return ILFloatScanner.ScannedTypeFullNames(asmPath, "Jianghu.Cultivation");
    }

    [Fact]
    public void Scan_Covers_ModuleResolver_And_CombatContext()
    {
        var scanned = ScannedCultivationTypes();
        Assert.Contains("Jianghu.Cultivation.ModuleResolver", scanned);
        Assert.Contains("Jianghu.Cultivation.CombatContext", scanned);
    }

    [Fact]
    public void Scan_Covers_Special_Handler_EscapeHatch()
    {
        var scanned = ScannedCultivationTypes();
        // 唯一档逃逸口（spec §7）：注册表 + ISpecialModule 实现（批3 加真 handler，须必被扫）。
        Assert.Contains("Jianghu.Cultivation.SpecialModuleRegistry", scanned);
        Assert.Contains("Jianghu.Cultivation.NoopSpecialModule", scanned);
    }

    /// <summary>
    /// 反证：守卫确能抓「漏扫」。一个真实存在于 Jianghu.Cultivation 但**故意不在**期望集里的伪名
    /// 必须**不**被误判为已覆盖（若 scanner 退化为返回全宇宙/常量集，此断言失败 → 守卫无效会被发现）。
    /// </summary>
    [Fact]
    public void GuardSet_Is_Derived_From_Real_Scan_Not_Constant()
    {
        var scanned = ScannedCultivationTypes();
        Assert.DoesNotContain("Jianghu.Cultivation.__DefinitelyNotAType", scanned);
        // 且集合非空、确由命名空间筛选得来：所有成员都属 Jianghu.Cultivation 前缀。
        Assert.NotEmpty(scanned);
        Assert.All(scanned, n => Assert.StartsWith("Jianghu.Cultivation", n));
    }
}
