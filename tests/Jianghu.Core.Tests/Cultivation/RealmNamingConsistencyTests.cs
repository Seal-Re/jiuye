using System.Collections.Generic;
using Jianghu.Cultivation;
using Xunit;

// A1.3 统一双轨命名：跨路命名一致性护栏（设计 §6.4）。
// 契约：任一路若用 backbone 大境界专名（炼气/筑基/.../大乘），其 UT 必 == 该名的 canonical UT（双轨参照 §1）。
//       顶名（剑仙/血神/丹帝…）与门类自有名（鬼/妖/命…）不入此契约，各路自由。
// 防回归：原魔修「化神」误落 UT6（应 UT8）、「炼虚」误落 UT8、「合体」误落 UT9 → 此测试对未规整版本必红。
public class RealmNamingConsistencyTests
{
    // backbone 名 → canonical UT（修士轨主阶梯 §1）。
    static readonly Dictionary<string, int> BackboneUT = new Dictionary<string, int>
    {
        ["炼气"] = 0, ["筑基"] = 2, ["金丹"] = 4, ["元婴"] = 6,
        ["化神"] = 8, ["炼虚"] = 9, ["合体"] = 10, ["大乘"] = 11,
    };

    static IReadOnlyList<CultivationPathDef> All() => new CodePathSource().Load();

    [Fact]
    public void AllPaths_BackboneRealmName_ImpliesCanonicalUT()
    {
        foreach (var p in All())
        {
            var names = p.Curve.RealmNames;
            var ut = p.Curve.UnifiedTierOf;
            for (int i = 0; i < names.Count; i++)
            {
                if (BackboneUT.TryGetValue(names[i], out int expect))
                    Assert.True(ut[i] == expect,
                        $"{p.PathId} 第{i}境「{names[i]}」UT={ut[i]} 应为 canonical UT={expect}（双轨参照 backbone 名/UT 不一致）");
            }
        }
    }

    [Fact]
    public void AllPaths_NoMojibake_NoLegacyTypo()
    {
        // 规整后不得残留：U+FFFD 乱码 / 雷修旧错字「练气」「结丹」。
        foreach (var p in All())
            foreach (var n in p.Curve.RealmNames)
            {
                Assert.DoesNotContain("�", n);
                Assert.NotEqual("练气", n);
                Assert.NotEqual("结丹", n);
            }
    }

    [Fact]
    public void AscendingPath_DoesNotBorrow_WufuReservedName()
    {
        // 「陆地神仙」= 武夫轨 UT9 封顶专名（§1）；修士轨（CanAscend=true）不得借用作 UT12 顶名（体修已改肉身成圣）。
        // 武夫 ladder（CanAscend=false）落地后可于 UT9 合法使用——届时此断言天然豁免。
        foreach (var p in All())
            if (p.Curve.CanAscend)
                Assert.DoesNotContain("陆地神仙", p.Curve.RealmNames);
    }
}
