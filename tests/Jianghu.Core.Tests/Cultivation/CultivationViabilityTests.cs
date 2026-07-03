using System.Collections.Generic;
using System.Linq;
using Jianghu.Config;
using Jianghu.Cultivation;
using Jianghu.Sim;
using Xunit;
using Xunit.Abstractions;

namespace Jianghu.Core.Tests.Cultivation
{
    /// <summary>
    /// Viability（可运转性）门（2026-07-03 用户指令 Step 2）：实体能否走完成长线。
    /// 内核前置于剧情——若破境率/寿命消耗失配（成长慢于寿命一个数量级），
    /// 大部分实体老死前无法破境，上层剧情因数值门控死锁（"闭关到老死"）。
    /// 本门用真实 21 路（CodePathSource）+ 默认 Limits 度量成长线是否跑通。
    /// Red lines: B.2(整数确定性), B.3(off 逐字节——本门仅 cultivation-on)。
    /// **这是 Viability（内核 work），非 Fairness（路间对等，属 Alpha）。**
    /// </summary>
    public class CultivationViabilityTests
    {
        private readonly ITestOutputHelper _out;
        public CultivationViabilityTests(ITestOutputHelper output) { _out = output; }

        // 度量一个默认 cultivation-on 世界长跑后的成长线健康度。
        // 返回 (曾破境人数, 累计入道人数, 破境事件总数, 最高 UT)。
        private (int everBroke, int everEntered, int breakthroughs, int maxUT) Measure(ulong seed, int steps)
        {
            var w = WorldFactory.CreateInitial(seed, LimitsConfig.Default, 8, cultivation: true);
            for (int i = 0; i < steps; i++) w.Advance(8);

            var lines = w.Chronicle.Lines;
            int entered = lines.Count(l => l.Contains("拜入") && l.Contains("一脉"));
            int breakthroughs = lines.Count(l => l.Contains("冲破瓶颈"));

            // 曾破境（RealmIndex>0）的在世修士 + 逝者（史册破境行不区分死活，用事件计数近似"发生过成长"）。
            int aliveBroke = w.AliveCharacters().Count(c => c.Cultivation != null && c.Cultivation.RealmIndex > 0);

            // 最高 UT（成长线是否真的往上走）。
            var registry = new PathRegistry(new CodePathSource());
            int maxUT = 0;
            foreach (var c in w.AliveCharacters())
            {
                if (c.Cultivation == null) continue;
                var curve = registry.ById(c.Cultivation.PathId).Curve;
                int idx = c.Cultivation.RealmIndex;
                if (idx < curve.UnifiedTierOf.Count) maxUT = System.Math.Max(maxUT, curve.UnifiedTierOf[idx]);
            }
            return (aliveBroke, entered, breakthroughs, maxUT);
        }

        // —— V1：成长线不死锁——一个长跑世界，破境事件数应与入道人数成合理比例，
        //    而非"绝大多数实体老死前零破境"（当前实测 ~65 入道仅 7 破境 = 死锁征兆）。——
        [Fact]
        public void test_viability_breakthrough_rate_not_deadlocked()
        {
            // 3 seed 聚合，抗单局偶然。
            int totalBreak = 0, totalEntered = 0;
            foreach (ulong seed in new ulong[] { 42, 99, 2026 })
            {
                var (_, entered, breaks, _) = Measure(seed, 600);
                totalBreak += breaks;
                totalEntered += entered;
                _out.WriteLine($"seed {seed}: 入道 {entered}, 破境 {breaks}");
            }
            _out.WriteLine($"合计: 入道 {totalEntered}, 破境 {totalBreak}, 比 {(totalEntered > 0 ? totalBreak * 100 / totalEntered : 0)}%");

            // Viability 判据：破境事件 ≥ 入道人数的 30%（大部分实体一生至少接近/达到 1 次破境）。
            // 当前实测 ~10%（死锁）→ 本断言 RED，调平后 GREEN。
            Assert.True(totalBreak * 100 >= totalEntered * 30,
                $"Viability 死锁：破境 {totalBreak} / 入道 {totalEntered} = {(totalEntered > 0 ? totalBreak * 100 / totalEntered : 0)}% < 30%。" +
                "破境率/寿命消耗失配，大部分实体老死前无法破境（闭关到老死）。");
        }

        // —— V2：典型实体有生之年能破多境（成长线纵深，非只破 1 次就死）——
        [Fact]
        public void test_viability_typical_entity_reaches_higher_realms()
        {
            var (_, _, _, maxUT) = Measure(2026, 600);
            _out.WriteLine($"seed 2026 600步 最高 UT = {maxUT}");
            // 有生之年至少有实体走到 UT≥4（化神级），证明成长线纵深可达，非全卡低境。
            Assert.True(maxUT >= 4,
                $"成长线纵深不足：最高 UT={maxUT} < 4。典型实体一生难破多境（寿命内成长线走不完）。");
        }
    }
}
