using System;
using System.Collections.Generic;
using Jianghu.Config;
using Jianghu.Cultivation;
using Jianghu.Model;
using Jianghu.Random;
using Jianghu.Sim;
using Jianghu.Stats;
using Xunit;

namespace Jianghu.Core.Tests.Cultivation
{
    /// <summary>
    /// cv-006（adr-0010 决策①）：SEC 闪避系数合流测试。
    /// SEC 作为 cv-001 命中 permille 的前置整数调制器（不新增掷骰）。
    /// 覆盖：AC 6.2 纯函数各 SEC 值 + AC 6.3 接线 seed-sweep + AC 6.4 惰性 byte-identical +
    /// AC 6.5 calibrationMode 旁路 + AC 6.6 B.2 浮点零 / B.3 off 不退。
    /// 触 Jianghu.Cultivation → 旗舰档 + 主控核验（B.7/A.3）。
    /// </summary>
    public class EvasionSecTests
    {
        static LimitsConfig Limits => LimitsConfig.Default;

        // PE = stat:Force × 4 × RealmMult(=1)。故 Force 直控 PE：Force=25 → PE=100。
        // attackSec = 攻方招式 SEC（默认 1000 中性）；defSec = 防方招式 SEC（本 story 不验防方 SEC，恒默认）。
        static CultivationPathDef MakePath(string id, int attackSec = 1000)
        {
            var skill = new CombatSkillDef("atk", "atk", 0, Array.Empty<EffectOp>(),
                new Dictionary<string, int>(), DamageType.Normal, attackSec);
            return new CultivationPathDef(
                id, id, "physical", new[] { "melee" },
                new[] { new ResourceDef("qi", 0, 1000, 0) },
                new PowerFormulaDef(new[] { new PowerTerm("stat:Force", 4, null) },
                    Array.Empty<PowerMod>(), null),
                new RealmCurveDef(new[] { 10, 15, 25 }, new[] { 0, 1, 2 },
                    new[] { "L1", "L2", "L3" }, new[] { 0, 100, 300 },
                    new[] { 1, 1, 1 }, true, 2),
                Array.Empty<ArtCategoryDef>(),
                new[] { skill },
                new EntryGateDef(""), new SelectionRuleDef(1, 3), null);
        }

        static Character MakeChar(long id, int force, CultivationPathDef path)
        {
            var c = new Character(new CharacterId(id),
                new Persona("n", "t", "s", ArchetypeKind.Martial, null),
                new StatBlock(new[] { force, 0, 0, 0 }),
                new NodeId(0), new Goal(GoalKind.Advance, 0), 0, 800, 16);
            var cult = CultivationState.NewForPath(path.PathId, path.Resources,
                Array.Empty<string>(), new[] { "atk" });
            cult.RealmIndex = 1; // 同 UT（避免 gap≥2 auto-win 短路，测方差/SEC 区）
            c.Cultivation = cult;
            return c;
        }

        static PathRegistry Reg(CultivationPathDef p) => new PathRegistry(new ListPathSource(new[] { p }));
        static IRandom Rng(ulong seed) => new Pcg32(seed, (ulong)RngStreamIds.Duel);

        // 私有 IPathSource（同各 Cultivation 测试文件既有模式：每文件自带一份）。
        sealed class ListPathSource : IPathSource
        {
            private readonly IReadOnlyList<CultivationPathDef> _paths;
            public ListPathSource(IReadOnlyList<CultivationPathDef> paths) => _paths = paths;
            public IReadOnlyList<CultivationPathDef> Load() => _paths;
        }

        // ============================================================
        // AC 6.2：ApplyEvasionCoefficient 纯函数（无 RNG、无 IO，确定性直验 B.2）
        // ============================================================

        [Fact]
        public void test_sec_1000_is_neutral_unchanged()
        {
            // SEC=1000 中性 → p 不变（AC 6.4 惰性的纯函数根据）
            Assert.Equal(500, CombatMath.ApplyEvasionCoefficient(500, 1000));
            Assert.Equal(1, CombatMath.ApplyEvasionCoefficient(1, 1000));
            Assert.Equal(999, CombatMath.ApplyEvasionCoefficient(999, 1000));
        }

        [Fact]
        public void test_sec_0_is_auto_hit_returns_1000()
        {
            // SEC=0 必中标签 → 返回 AutoHitPermille(1000)，使 cv-001 roll<1000 恒真（真·必中，不可闪避）
            // 显式分支（非 max(1,SEC)），不进除法路径（adr-0010 决策①用户裁定）
            Assert.Equal(1000, CombatMath.ApplyEvasionCoefficient(500, 0));
            Assert.Equal(1000, CombatMath.ApplyEvasionCoefficient(1, 0));
            Assert.Equal(1000, CombatMath.ApplyEvasionCoefficient(999, 0));
        }

        [Fact]
        public void test_sec_2000_halves_hit_probability()
        {
            // SEC=2000 易闪 → p/2（整数向下取整）
            Assert.Equal(250, CombatMath.ApplyEvasionCoefficient(500, 2000));
            Assert.Equal(499, CombatMath.ApplyEvasionCoefficient(999, 2000)); // 999*1000/2000 = 499.5 → 499
            Assert.Equal(0, CombatMath.ApplyEvasionCoefficient(1, 2000));     // 1*1000/2000 = 0.5 → 0
        }

        [Fact]
        public void test_sec_500_elevates_hit_probability_no_clamp()
        {
            // SEC=500 难闪 → p×2。p≤500 时无钳制（2×≤1000）
            Assert.Equal(1000, CombatMath.ApplyEvasionCoefficient(500, 500));  // 500*1000/500 = 1000（恰 =AutoHitPermille，不钳）
            Assert.Equal(800, CombatMath.ApplyEvasionCoefficient(400, 500));   // 400*1000/500 = 800
            Assert.Equal(2, CombatMath.ApplyEvasionCoefficient(1, 500));       // 1*1000/500 = 2
        }

        [Fact]
        public void test_sec_500_clamps_to_1000_when_elevated_above()
        {
            // QA AC-2「clamped to 1000」：SEC<1000 抬升使 p*1000/SEC > 1000 → 钳 1000
            Assert.Equal(1000, CombatMath.ApplyEvasionCoefficient(600, 500));  // 600*1000/500 = 1200 → 钳 1000
            Assert.Equal(1000, CombatMath.ApplyEvasionCoefficient(999, 500));  // 999*1000/500 = 1998 → 钳 1000
        }

        [Fact]
        public void test_zero_p_returns_zero_for_any_sec()
        {
            // 边界：p=0 任意 SEC 仍为 0（攻方零基础命中，SEC 无从修正）。
            // 守卫前置 → Apply(0,0)=0（零命中不被 SEC=0 必中救活），生产 p∈[1,999] 故 SEC=0 恒必中。
            Assert.Equal(0, CombatMath.ApplyEvasionCoefficient(0, 1000));
            Assert.Equal(0, CombatMath.ApplyEvasionCoefficient(0, 0));
            Assert.Equal(0, CombatMath.ApplyEvasionCoefficient(0, 2000));
            Assert.Equal(0, CombatMath.ApplyEvasionCoefficient(0, 500));
        }

        [Fact]
        public void test_negative_p_returns_zero()
        {
            // 病态负 p（生产恒 ≥0）→ 0 守卫
            Assert.Equal(0, CombatMath.ApplyEvasionCoefficient(-1, 1000));
            Assert.Equal(0, CombatMath.ApplyEvasionCoefficient(-100, 0));
        }

        [Fact]
        public void test_negative_sec_returns_zero()
        {
            // 病态负 SEC（生产恒 ≥0）→ 防御性归 0（避免负 permille 进下游）
            Assert.Equal(0, CombatMath.ApplyEvasionCoefficient(500, -1));
        }

        [Fact]
        public void test_p_1000_sec_2000_attenuated()
        {
            // QA AC-2 边界：p=1000（非生产值，但验衰减逻辑）SEC=2000 → 500
            Assert.Equal(500, CombatMath.ApplyEvasionCoefficient(1000, 2000));
        }

        [Fact]
        public void test_auto_hit_permille_constant_is_1000()
        {
            // AutoHitPermille=1000（>MaxPermille=999），使 roll<1000 恒真 = 真·必中
            Assert.Equal(1000, CombatMath.AutoHitPermille);
            Assert.True(CombatMath.AutoHitPermille > CombatMath.MaxPermille);
        }

        [Fact]
        public void test_apply_evasion_is_deterministic_pure_function()
        {
            // 纯函数：同输入恒同输出（B.2）
            for (int p = 0; p <= 1000; p += 50)
                for (int sec = 0; sec <= 2000; sec += 100)
                    Assert.Equal(
                        CombatMath.ApplyEvasionCoefficient(p, sec),
                        CombatMath.ApplyEvasionCoefficient(p, sec));
        }

        // ============================================================
        // AC 6.3：ResolveExchange 接线——同 margin 不同 SEC → 命中率差异统计显著
        // ============================================================

        [Fact]
        public void test_lower_sec_wins_more_than_higher_sec_same_margin()
        {
            // 同 margin（近等 PE），攻方 SEC=500（难闪）vs SEC=2000（易闪）对同防方（SEC=1000 中性）。
            // 跨种子 sweep：SEC=500 攻方胜场应**严格多于** SEC=2000 攻方（SEC 调制在起作用，非哑弹）。
            var pathDef500 = MakePath("atk500", attackSec: 500);
            var pathDef2000 = MakePath("atk2000", attackSec: 2000);
            var defPath = MakePath("def", attackSec: 1000);

            var a500 = MakeChar(1, 25, pathDef500);  // PE 100
            var a2000 = MakeChar(1, 25, pathDef2000); // PE 100（同 margin，仅 SEC 不同）
            var b = MakeChar(2, 24, defPath);          // PE 96（攻方微优，margin=4）

            int wins500 = 0, wins2000 = 0, total = 0;
            for (ulong s = 1; s <= 100; s++)
            {
                var r500 = DuelEngine.ResolveR2(a500, b, pathDef500, defPath,
                    new PathRegistry(new ListPathSource(new[] { pathDef500, defPath })),
                    Limits, null, null, null, duelRng: Rng(s));
                var r2000 = DuelEngine.ResolveR2(a2000, b, pathDef2000, defPath,
                    new PathRegistry(new ListPathSource(new[] { pathDef2000, defPath })),
                    Limits, null, null, null, duelRng: Rng(s));
                total++;
                if (r500.Winner == a500.Id) wins500++;
                if (r2000.Winner == a2000.Id) wins2000++;
            }

            // SEC=500（难闪，命中抬升）攻方胜场应严格多于 SEC=2000（易闪，命中衰减）攻方
            Assert.True(wins500 > wins2000,
                $"SEC=500 胜场({wins500}/{total}) 应 > SEC=2000 胜场({wins2000}/{total}) —— SEC 调制未生效或方向反");
        }

        [Fact]
        public void test_sec_zero_is_auto_hit_in_duel()
        {
            // SEC=0 必中标签在实战中的效果：Apply(p,0)=1000（AutoHitPermille）→ roll<1000 恒真 → 每回合必命中。
            // 对比 SEC=2000（Apply(p,2000)=p/2 → 命中衰减）。
            // 用极端劣势 margin 隔离命中差异：攻方远弱 → cv-001 p 较低，SEC 调制幅度显化。
            //
            // PE = stat:Force × 4 × RealmMult(RealmIndex=1 → 15)。Force=15 → PE 900；Force=40 → PE 2400。
            // margin = 900 − 2400 = −1500；GetSuccessPermille: relPct = −1500*100/2400 = −62 → p = 500 + (−62)*5 = 190。
            //   - SEC=0：Apply(190,0)=1000 → 必中（roll<1000 恒真，无视劣势）→ 攻方每回合稳定命中防方
            //   - SEC=2000：Apply(190,2000)=95（190/2 向下取整）→ 命中概率减半 → 跨种子平均命中次数 ≈ SEC=0 的一半
            // 故 SEC=0 攻方累计伤害应**严格多于** SEC=2000 攻方（必中 vs 衰减，AC 6.3 接线 + AC 6.2 SEC=0 必中语义双重验证）。
            var pathSec0 = MakePath("atk0", attackSec: 0);
            var pathSec2000 = MakePath("atk2k", attackSec: 2000);
            var defPath = MakePath("def", attackSec: 1000);

            var a0 = MakeChar(1, 15, pathSec0);      // PE 900（远弱）
            var a2000 = MakeChar(1, 15, pathSec2000);
            var b = MakeChar(2, 40, defPath);         // PE 2400（远强）
            int defInitialHp = 2400;                  // = Force 40 × 4 × RealmMult 15

            int totalDmgSec0 = 0, totalDmgSec2000 = 0;
            for (ulong s = 1; s <= 20; s++)
            {
                var r0 = DuelEngine.ResolveR2(a0, b, pathSec0, defPath,
                    new PathRegistry(new ListPathSource(new[] { pathSec0, defPath })),
                    Limits, null, null, null, duelRng: Rng(s));
                var r2000 = DuelEngine.ResolveR2(a2000, b, pathSec2000, defPath,
                    new PathRegistry(new ListPathSource(new[] { pathSec2000, defPath })),
                    Limits, null, null, null, duelRng: Rng(s));
                // 攻方对防方造成伤害 = 防方初始 HP - 防方残血
                totalDmgSec0 += (defInitialHp - r0.DefenderHpRemaining);
                totalDmgSec2000 += (defInitialHp - r2000.DefenderHpRemaining);
            }

            // SEC=0（必中，p=190→1000）应严格多于 SEC=2000（p=190→95，命中减半）
            Assert.True(totalDmgSec0 > totalDmgSec2000,
                $"SEC=0 必中应造成更多伤害：sec0={totalDmgSec0} sec2000={totalDmgSec2000}");
            // SEC=0 必中 → 攻方必有非零伤害（roll<1000 恒真，每回合命中）
            Assert.True(totalDmgSec0 > 0,
                $"SEC=0 必中应造成非零伤害：sec0={totalDmgSec0}");
        }

        // ============================================================
        // AC 6.4：SEC 惰性——默认 SEC=1000 与显式 SEC=1000 byte-identical（cv-001 基线不退）
        // ============================================================

        [Fact]
        public void test_default_sec_equals_explicit_sec_1000_byte_identical()
        {
            // 显式传 Sec=1000 的招式 vs 默认（构造器省略 Sec，隐式 1000）→ 同种子逐字节复现。
            // 这证明 21 路现有构造（省略 Sec）与显式 1000 行为等价 = 惰性零行为改变。
            var explicitPath = MakePath("explicit", attackSec: 1000);

            // 默认路径：构造 CombatSkillDef 时省略 Sec 参数（隐式默认 1000）
            var defaultSkill = new CombatSkillDef("atk", "atk", 0, Array.Empty<EffectOp>(),
                new Dictionary<string, int>()); // 省略 Damage + Sec → 默认 Normal + 1000
            var defaultPath = new CultivationPathDef(
                "default", "default", "physical", new[] { "melee" },
                new[] { new ResourceDef("qi", 0, 1000, 0) },
                new PowerFormulaDef(new[] { new PowerTerm("stat:Force", 4, null) },
                    Array.Empty<PowerMod>(), null),
                new RealmCurveDef(new[] { 10, 15, 25 }, new[] { 0, 1, 2 },
                    new[] { "L1", "L2", "L3" }, new[] { 0, 100, 300 },
                    new[] { 1, 1, 1 }, true, 2),
                Array.Empty<ArtCategoryDef>(),
                new[] { defaultSkill },
                new EntryGateDef(""), new SelectionRuleDef(1, 3), null);

            var aExplicit = MakeChar(1, 25, explicitPath);
            var aDefault = MakeChar(1, 25, defaultPath);
            var b = MakeChar(2, 24, explicitPath);

            for (ulong s = 1; s <= 10; s++)
            {
                var rExp = DuelEngine.ResolveR2(aExplicit, b, explicitPath, explicitPath,
                    Reg(explicitPath), Limits, null, null, null, duelRng: Rng(s));
                var rDef = DuelEngine.ResolveR2(aDefault, b, defaultPath, defaultPath,
                    Reg(defaultPath), Limits, null, null, null, duelRng: Rng(s));
                // 逐字节复现：Winner/Margin/HP 全等
                Assert.Equal(rExp.Winner, rDef.Winner);
                Assert.Equal(rExp.Margin, rDef.Margin);
                Assert.Equal(rExp.AttackerHpRemaining, rDef.AttackerHpRemaining);
                Assert.Equal(rExp.DefenderHpRemaining, rDef.DefenderHpRemaining);
            }
        }

        // ============================================================
        // AC 6.5：calibrationMode 旁路——SEC≠1000 但标定模式 → 等同 SEC=1000 中性
        // ============================================================

        [Fact]
        public void test_calibration_mode_bypasses_sec_modulation()
        {
            // 标定模式（calibrationMode=true）：SEC 调制旁路 → 即便 SEC=2000 也等同 SEC=1000 中性。
            // 保 cv-005 seed-sweep 裸 PE 纯净（同 cv-001/002/003 一致）。
            var pathSec2000 = MakePath("atk2k", attackSec: 2000);
            var pathSec1000 = MakePath("atk1k", attackSec: 1000);

            var a2000 = MakeChar(1, 25, pathSec2000);
            var a1000 = MakeChar(1, 25, pathSec1000);
            var b = MakeChar(2, 24, pathSec1000);

            for (ulong s = 1; s <= 10; s++)
            {
                // 标定模式：SEC=2000 应等同 SEC=1000（调制旁路）
                var r2000Calib = DuelEngine.ResolveR2(a2000, b, pathSec2000, pathSec1000,
                    new PathRegistry(new ListPathSource(new[] { pathSec2000, pathSec1000 })),
                    Limits, null, null, null, calibrationMode: true, duelRng: Rng(s));
                var r1000Calib = DuelEngine.ResolveR2(a1000, b, pathSec1000, pathSec1000,
                    Reg(pathSec1000), Limits, null, null, null, calibrationMode: true, duelRng: Rng(s));

                Assert.Equal(r1000Calib.Winner, r2000Calib.Winner);
                Assert.Equal(r1000Calib.Margin, r2000Calib.Margin);
                Assert.Equal(r1000Calib.AttackerHpRemaining, r2000Calib.AttackerHpRemaining);
                Assert.Equal(r1000Calib.DefenderHpRemaining, r2000Calib.DefenderHpRemaining);
            }
        }

        [Fact]
        public void test_calibration_mode_sec_equals_deterministic_bypass()
        {
            // 标定模式 + SEC≠1000 + duelRng → 应等同确定性（duelRng=null，既有行为）。
            // 即标定模式下 SEC 调制与方差拦截双双旁路 = 裸 PE 平价。
            var pathSec500 = MakePath("atk500", attackSec: 500);
            var a = MakeChar(1, 25, pathSec500);
            var b = MakeChar(2, 24, pathSec500);

            var det = DuelEngine.ResolveR2(a, b, pathSec500, pathSec500, Reg(pathSec500),
                Limits, null, null, null); // duelRng=null 确定性
            var calib = DuelEngine.ResolveR2(a, b, pathSec500, pathSec500, Reg(pathSec500),
                Limits, null, null, null, calibrationMode: true, duelRng: Rng(7));

            Assert.Equal(det.Winner, calib.Winner);
            Assert.Equal(det.Margin, calib.Margin);
            Assert.Equal(det.AttackerHpRemaining, calib.AttackerHpRemaining);
            Assert.Equal(det.DefenderHpRemaining, calib.DefenderHpRemaining);
        }

        // ============================================================
        // AC 6.6：B.2 浮点零 + B.3 off 不退 + 确定性
        // ============================================================

        [Fact]
        public void test_cultivation_namespace_has_no_float_after_sec()
        {
            // B.2：ApplyEvasionCoefficient 全整数（long 中间量 + 整数除法），IL 扫描 Jianghu.Cultivation 命名空间零浮点。
            var asmPath = typeof(Jianghu.Sim.World).Assembly.Location;
            var offenders = ILFloatScanner.ScanNamespace(asmPath, "Jianghu.Cultivation");
            Assert.True(offenders.Count == 0, "浮点出现在: " + string.Join(", ", offenders));
        }

        [Fact]
        public void test_sec_modulation_is_deterministic_same_seed()
        {
            // B.2 确定性：SEC 调制同种子两跑逐字节复现（SEC 不新增 RNG，复用 cv-001 duelRng）
            var pathSec500 = MakePath("atk500", attackSec: 500);
            var a = MakeChar(1, 25, pathSec500);
            var b = MakeChar(2, 24, pathSec500);

            var r1 = DuelEngine.ResolveR2(a, b, pathSec500, pathSec500, Reg(pathSec500),
                Limits, null, null, null, duelRng: Rng(42));
            var r2 = DuelEngine.ResolveR2(a, b, pathSec500, pathSec500, Reg(pathSec500),
                Limits, null, null, null, duelRng: Rng(42));

            Assert.Equal(r1.Winner, r2.Winner);
            Assert.Equal(r1.Margin, r2.Margin);
            Assert.Equal(r1.AttackerHpRemaining, r2.AttackerHpRemaining);
            Assert.Equal(r1.DefenderHpRemaining, r2.DefenderHpRemaining);
        }

        [Fact]
        public void test_off_mode_unaffected_by_sec_field()
        {
            // B.3：off（cultivation=false）不入 DuelEngine → SEC 字段零影响，同种子两跑 Chronicle 逐字节。
            // SEC 是 cultivation-on 路径数据，off 走 legacy SparAction 天然安全。
            string Run()
            {
                var w = WorldFactory.CreateInitial(2026, LimitsConfig.Default, 5);
                for (int i = 0; i < 200; i++) w.Advance(6);
                return string.Join("\n", w.Chronicle.Lines);
            }
            Assert.Equal(Run(), Run());
        }
    }
}
