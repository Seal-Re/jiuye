using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Jianghu.Config;
using Jianghu.Cultivation;
using Jianghu.Model;
using Jianghu.Random;
using Jianghu.Stats;
using Xunit;
using Xunit.Abstractions;

namespace Jianghu.Core.Tests.Cultivation
{
    /// <summary>
    /// balance-003 / TR-BAL-001：C1 收敛 [40,60]% 硬闸门 + RealmMultipliers 归一化校准 harness。
    /// 方法 = INV-CROSS design §5「解析校准」：mul_p[i] := round( target(UT_i) × 10 / BaseSum_p(i) )，
    /// target = 剑修（范式路）典型 power(UT)。只动 mul（战力级别），不动 term 结构（路"手感"保留）。
    /// 战斗路 18 条重校准；辅助路（dan/array/qixiu）C3 豁免不动。
    /// Red lines: B.2（整数确定性）、B.3（off 逐字节——本 harness 仅读 on 路径）。
    /// </summary>
    public class C1RecalibrationTests
    {
        private readonly ITestOutputHelper _out;
        public C1RecalibrationTests(ITestOutputHelper output) { _out = output; }

        static readonly LimitsConfig Limits = LimitsConfig.Default;
        static readonly CultivationPathDef[] AllPaths = new CodePathSource().Load().ToArray();
        static readonly PathRegistry Registry = new PathRegistry(new CodePathSource());

        // 辅助路（C3 豁免，不重校准 mul 到 target）。
        static readonly HashSet<string> AuxiliaryPathIds = new() { "dan_xiu", "array_formation", "qixiu_artificer" };
        const string AnchorPathId = "sword_immortal"; // 范式路

        // 典型角色（与 WorldFactory.CreateTypicalChar 一致：段首 realm、中位 loadout、均匀 stats）。
        static Character TypicalChar(string pathId, int realmIdx)
        {
            var def = Registry.ById(pathId);
            var chosenArts = new List<string>();
            foreach (var cat in def.ArtCategories)
            {
                if (cat.Role == "daoheart") continue;
                int pick = cat.PickMin;
                var sorted = new List<ArtDef>(cat.Arts);
                sorted.Sort((a, b) => a.Tier.CompareTo(b.Tier));
                int start = Math.Max(0, (sorted.Count - pick) / 2);
                for (int i = 0; i < pick && start + i < sorted.Count; i++) chosenArts.Add(sorted[start + i].Id);
            }
            var chosenSkills = new List<string>();
            int skillPick = def.Selection.SkillPickMin;
            if (skillPick > 0 && def.CombatSkills.Count > 0)
            {
                var ss = new List<CombatSkillDef>(def.CombatSkills);
                ss.Sort((a, b) => a.Tier.CompareTo(b.Tier));
                int st = Math.Max(0, (ss.Count - skillPick) / 2);
                for (int i = 0; i < skillPick && st + i < ss.Count; i++) chosenSkills.Add(ss[st + i].Id);
            }
            var state = CultivationState.NewForPath(pathId, def.Resources, chosenArts, chosenSkills);
            state.RealmIndex = realmIdx;
            var persona = new Persona("典型", "散客", "市井", ArchetypeKind.Martial, null);
            var ch = new Character(new CharacterId(0), persona, new StatBlock(new[] { 20, 20, 20, 20 }),
                new NodeId(0), new Goal(GoalKind.Advance, 0), 0, 800, 16);
            ch.Cultivation = state;
            return ch;
        }

        static int PowerAt(string pathId, int realmIdx)
        {
            var ch = TypicalChar(pathId, realmIdx);
            return PowerEngine.Evaluate(ch.Cultivation!, ch.Stats, Registry.ById(pathId), Limits);
        }

        // BaseSum = final×10/mul 反推（final=PowerAt，mul 已知 per realm）。
        static long BaseSumAt(string pathId, int realmIdx)
        {
            int oldMul = Registry.ById(pathId).Curve.RealmMultipliers[realmIdx];
            int finalPow = PowerAt(pathId, realmIdx);
            return oldMul > 0 ? (long)finalPow * 10 / oldMul : 0;
        }

        // §5 解析校准：mul_p[i] = round(target(UT_i)×10 / BaseSum_p(i))，
        // + power-单调护栏（UT 平段多 realm 同 UT 时整数舍入致 power 微跌 → mul 上调保 power[r]≥power[r-1]）。
        static int[] RecalibrateMul(string pathId, Dictionary<int, int> target)
        {
            var curve = Registry.ById(pathId).Curve;
            int n = curve.RealmMultipliers.Count;
            var newMul = new int[n];
            long prevPow = -1;
            for (int r = 0; r < n; r++)
            {
                long baseSum = BaseSumAt(pathId, r);
                int ut = curve.UnifiedTierOf[r];
                int tgt = target.TryGetValue(ut, out var t) ? t : PowerAt(pathId, r); // sword 不可达该 UT → 保原
                int mul = baseSum > 0 ? (int)((2L * tgt * 10 + baseSum) / (2 * baseSum)) : curve.RealmMultipliers[r]; // round
                if (mul < 1) mul = 1;
                // power-单调护栏：若舍入致 power 跌，逐步上调 mul 至 power[r]≥prevPow（确定性）。
                if (baseSum > 0)
                    while (baseSum * mul / 10 < prevPow) mul++;
                newMul[r] = mul;
                prevPow = baseSum * mul / 10;
            }
            return newMul;
        }

        // sword 的 UT → 典型 power（target 曲线）。
        static Dictionary<int, int> BuildTargetByUT()
        {
            var def = Registry.ById(AnchorPathId);
            var map = new Dictionary<int, int>();
            for (int r = 0; r < def.Curve.RealmMultipliers.Count; r++)
            {
                int ut = def.Curve.UnifiedTierOf[r];
                map[ut] = PowerAt(AnchorPathId, r);
            }
            return map;
        }

        // —— 校准 harness：dump 每条战斗路的新 mul 数组（§5 解析校准），供落地到 path 文件。——
        [Fact]
        public void Dump_Recalibrated_RealmMultipliers()
        {
            var target = BuildTargetByUT();
            _out.WriteLine("sword target(UT): " + string.Join(", ", target.OrderBy(k => k.Key).Select(k => $"UT{k.Key}={k.Value}")));
            _out.WriteLine("");

            var sb = new StringBuilder();
            foreach (var def in AllPaths)
            {
                if (AuxiliaryPathIds.Contains(def.PathId) || def.PathId == AnchorPathId) continue;
                var curve = def.Curve;
                var newMul = RecalibrateMul(def.PathId, target);
                // mul 非递增在 UT 平段（多 realm 同 UT）是正确的；真正不变量 = power 单调（C2）。
                bool mono = true;
                for (int r = 1; r < newMul.Length; r++) if (newMul[r] <= newMul[r - 1]) mono = false;
                bool powerMono = true;
                long prevPow = -1;
                for (int r = 0; r < newMul.Length; r++)
                {
                    long bs = BaseSumAt(def.PathId, r);
                    long newPow = bs * newMul[r] / 10;
                    if (newPow < prevPow) powerMono = false;
                    prevPow = newPow;
                }
                sb.AppendLine($"{def.PathId}: {{ {string.Join(", ", newMul)} }}{(mono ? "" : "  ⚠mul非递增")}{(powerMono ? "" : "  🔴POWER非单调")}");
            }
            _out.WriteLine("=== 重校准后 RealmMultipliers（战斗路 18）===");
            _out.WriteLine(sb.ToString());
        }

        // —— balance-006 方案B（模型可达 gate）：裸 PE 带宽 gate（承 §2 代理指标）。
        //    确定性对拍下"胜率[40,60] violations==0"不可达；§2 给的 C1 代理 = 同 UT 典型 power 落同一带 ±X%。
        //    重校准后测各战斗路 @各 UT 相对 sword 的偏差，报最大偏差 + 落 ±15% 带的比例。——
        [Fact]
        public void C1_BarePowerBand_WithinTolerance_Of_SwordAnchor()
        {
            var target = BuildTargetByUT();
            const int BandPct = 15; // §5 D1/§2 建议带宽 target(UT*)±15%
            int cells = 0, within = 0, maxDevPct = 0;
            string worst = "";
            foreach (var def in AllPaths)
            {
                if (AuxiliaryPathIds.Contains(def.PathId) || def.PathId == AnchorPathId) continue;
                var curve = def.Curve;
                for (int r = 0; r < curve.RealmMultipliers.Count; r++)
                {
                    int ut = curve.UnifiedTierOf[r];
                    if (!target.TryGetValue(ut, out int tgt) || tgt <= 0) continue; // sword 不可达该 UT → 跳
                    int pe = PowerAt(def.PathId, r);
                    int devPct = (int)(System.Math.Abs((long)pe - tgt) * 100 / tgt);
                    cells++;
                    if (devPct <= BandPct) within++;
                    if (devPct > maxDevPct) { maxDevPct = devPct; worst = $"{def.PathId}@UT{ut}: PE={pe} vs target={tgt} ({devPct}%)"; }
                }
            }
            int withinPct = cells > 0 ? within * 100 / cells : 0;
            _out.WriteLine($"裸 PE 带宽 gate（±{BandPct}%）：{within}/{cells} cell 入带 ({withinPct}%)；最大偏差 {maxDevPct}% [{worst}]");
            Assert.True(withinPct >= 80,
                $"裸 PE 带宽：仅 {withinPct}% cell 落 sword±{BandPct}% 带（AC: ≥80%）。最大偏差 {maxDevPct}% [{worst}]。");
        }

        // —— 诊断：5 路非严格递增根因（UT 重复 vs BaseSum 跳变）——
        [Fact]
        public void Diagnose_NonMonotonic_Paths()
        {
            var target = BuildTargetByUT();
            foreach (var pid in new[] { "ti_xiu_hengshi", "buddhist_golden_body", "ming_fate_causality", "fu_xiu_fulu" })
            {
                var curve = Registry.ById(pid).Curve;
                _out.WriteLine($"--- {pid} ---");
                for (int r = 0; r < curve.RealmMultipliers.Count; r++)
                {
                    int oldMul = curve.RealmMultipliers[r];
                    int finalPow = PowerAt(pid, r);
                    long baseSum = oldMul > 0 ? (long)finalPow * 10 / oldMul : 0;
                    int ut = curve.UnifiedTierOf[r];
                    int tgt = target.TryGetValue(ut, out var t) ? t : -1;
                    int newMul = baseSum > 0 ? (int)((2L * tgt * 10 + baseSum) / (2 * baseSum)) : oldMul;
                    _out.WriteLine($"  realm{r}: UT={ut} oldMul={oldMul} finalPow={finalPow} BaseSum={baseSum} target(UT)={tgt} → newMul={newMul}");
                }
            }
        }
    }
}
