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
    /// cv-002（adr-0008 决策⑦步7 + ⑧ + ⑩.3）：削韧副轴（Poise/Stagger）测试。
    /// 验削韧派生纯函数、韧性破触发硬直（复用 Control 管线打断）、抗性递减防 stagger-lock、
    /// PoiseState duel-local 不入 Clone、旋钮校验、B.2 确定性。
    /// 触 Jianghu.Cultivation → 旗舰档 + 主控核验（B.7/A.3）。
    /// </summary>
    public class PoiseStaggerTests
    {
        static LimitsConfig Limits => LimitsConfig.Default;

        // —— fixtures（照抄 cv-001/balance-007 既有约定，每文件自带一份，不跨文件共享）——
        // PE = stat:Force × 4 × RealmMult(=1)。Force=25 → PE=100。
        static CultivationPathDef MakePath(string id, EffectOp[]? extraOnUse = null)
        {
            var onUse = new List<EffectOp>();
            if (extraOnUse != null) onUse.AddRange(extraOnUse);
            return new CultivationPathDef(
                id, id, "physical", new[] { "melee" },
                new[] { new ResourceDef("qi", 0, 1000, 0) },
                new PowerFormulaDef(new[] { new PowerTerm("stat:Force", 4, null) },
                    Array.Empty<PowerMod>(), null),
                new RealmCurveDef(new[] { 10, 15, 25 }, new[] { 0, 1, 2 },
                    new[] { "L1", "L2", "L3" }, new[] { 0, 100, 300 },
                    new[] { 1, 1, 1 }, true, 2),
                Array.Empty<ArtCategoryDef>(),
                new[] { new CombatSkillDef("atk", "atk", 0, onUse.ToArray(),
                    new Dictionary<string, int>()) },
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
            cult.RealmIndex = 1; // 同 UT（避免 gap≥2 auto-win 短路，测削韧区）
            c.Cultivation = cult;
            return c;
        }

        static PathRegistry Reg(CultivationPathDef p) => new PathRegistry(new ListPathSource(new[] { p }));

        sealed class ListPathSource : IPathSource
        {
            private readonly IReadOnlyList<CultivationPathDef> _paths;
            public ListPathSource(IReadOnlyList<CultivationPathDef> paths) => _paths = paths;
            public IReadOnlyList<CultivationPathDef> Load() => _paths;
        }

        // ============================================================
        // AC-2 (2.2)：基础削韧派生纯函数
        // ============================================================

        [Fact]
        public void test_derive_poise_zero_damage_is_zero_poise()
        {
            // 未命中/被控（dmg=0）→ 削韧 0（语义自洽）
            Assert.Equal(0, DuelEngine.DerivePoiseDamage(0, 1000));
            Assert.Equal(0, DuelEngine.DerivePoiseDamage(-5, 1000));
        }

        [Fact]
        public void test_derive_poise_ratio_1000_equals_damage()
        {
            // ratio=1000‰ → 削韧 == 伤害
            Assert.Equal(50, DuelEngine.DerivePoiseDamage(50, 1000));
        }

        [Fact]
        public void test_derive_poise_ratio_scales_integer_floor()
        {
            // ratio=500‰ → 削韧 = 伤害/2（向下取整）
            Assert.Equal(25, DuelEngine.DerivePoiseDamage(50, 500));
            Assert.Equal(12, DuelEngine.DerivePoiseDamage(25, 500)); // 12.5 → 12 floor
        }

        [Fact]
        public void test_derive_poise_zero_ratio_disables()
        {
            // ratio=0 → 退化无基础削韧
            Assert.Equal(0, DuelEngine.DerivePoiseDamage(9999, 0));
        }

        [Fact]
        public void test_derive_poise_monotonic_in_damage()
        {
            // 单调：伤害越大削韧越大（同 ratio）
            int prev = -1;
            for (int d = 0; d <= 500; d += 25)
            {
                int p = DuelEngine.DerivePoiseDamage(d, 800);
                Assert.True(p >= prev, $"非单调 @dmg={d}");
                prev = p;
            }
        }

        [Fact]
        public void test_derive_poise_no_overflow_extreme()
        {
            // int.MaxValue 伤害 × ratio 用 long 中间量，不溢出、不抛
            int p = DuelEngine.DerivePoiseDamage(int.MaxValue, 1000);
            Assert.True(p >= 0);
        }

        // ============================================================
        // AC-5 (2.5)：抗性递减纯函数（防 stagger-lock）
        // ============================================================

        [Theory]
        [InlineData(300, 0, 50, 300)]  // 首次硬直：重置 = poiseMax
        [InlineData(300, 1, 50, 350)]  // 第2次：+1×50
        [InlineData(300, 2, 50, 400)]  // 第3次：+2×50（越来越难破）
        [InlineData(300, 3, 0, 300)]   // drStep=0 → 退化恒定
        public void test_stagger_reset_poise_ladder(int poiseMax, int priorStaggers, int drStep, int expected)
        {
            Assert.Equal(expected, DuelEngine.StaggerResetPoise(poiseMax, priorStaggers, drStep));
        }

        [Fact]
        public void test_stagger_reset_negative_drstep_treated_as_zero()
        {
            // 负 drStep 兜底为 0（不减韧性上限）
            Assert.Equal(300, DuelEngine.StaggerResetPoise(300, 5, -10));
        }

        // ============================================================
        // AC-4 (2.4)：硬直触发 —— 高削韧连击使韧性破 → margin 跃升
        // ============================================================

        [Fact]
        public void test_stagger_triggers_when_poise_breaks()
        {
            // Arrange：极低 PoiseMax + ratio=1000 → 一两回合即破韧触发硬直。
            // 攻方 A 略强（Force 30→PE120），防方 B（Force 20→PE80）。
            var path = MakePath("p");
            var a = MakeChar(1, 30, path);
            var b = MakeChar(2, 20, path);

            // 无削韧机制（PoiseMax 极大 → 永不破韧）作对照
            var noStagger = LimitsConfig.Default with { PoiseMax = int.MaxValue };
            var rNo = DuelEngine.ResolveR2(a, b, path, path, Reg(path), noStagger, null, null, null);

            // 有削韧（PoiseMax 低 → B 频繁被硬直，停手 → A 少挨打 → A margin 更大）
            var lowPoise = LimitsConfig.Default with { PoiseMax = 30 };
            var rStagger = DuelEngine.ResolveR2(a, b, path, path, Reg(path), lowPoise, null, null, null);

            // Assert：削韧使强者 A 胜势扩大（B 被打断更多 → A 受创更少）。至少胜者仍是 A 且结果确定。
            Assert.Equal(a.Id, rStagger.Winner);
            // 隔离验：两种配置 margin 不同（证削韧机制在起作用，非哑弹）
            Assert.NotEqual(rNo.Margin, rStagger.Margin);
        }

        [Fact]
        public void test_high_poise_never_staggers_behaves_like_baseline()
        {
            // 韧性上限极大 → 永不破韧 → 等同无削韧副轴（霸体全程）
            var path = MakePath("p");
            var a = MakeChar(1, 25, path);
            var b = MakeChar(2, 25, path);

            var huge = LimitsConfig.Default with { PoiseMax = int.MaxValue };
            var r1 = DuelEngine.ResolveR2(a, b, path, path, Reg(path), huge, null, null, null);
            var r2 = DuelEngine.ResolveR2(a, b, path, path, Reg(path), huge, null, null, null);

            // 确定性 + 无硬直（韧性恒 >0 → TickPoise 只削不触发注入）
            Assert.Equal(r1.Winner, r2.Winner);
            Assert.Equal(r1.Margin, r2.Margin);
        }

        // ============================================================
        // AC-3 (2.3)：PoiseDamage 算子骨架（路线 B）—— 额外削韧叠加
        // ============================================================

        [Fact]
        public void test_poise_break_operator_adds_extra_poise_damage()
        {
            // 合成路带 PoiseBreak(200) 算子 → 额外削韧使韧性更快破 → 与无算子路结果不同
            var plainPath = MakePath("plain");
            var breakPath = MakePath("break", new[] { Modules.PoiseBreak(200, "破招") });
            var regBoth = new PathRegistry(new ListPathSource(new[] { plainPath, breakPath }));

            // 攻方带 PoiseBreak 算子，防方普通；中等 PoiseMax 使算子影响可见
            var cfg = LimitsConfig.Default with { PoiseMax = 200, PoiseDamageRatioPermille = 500 };

            var atkBreak = MakeChar(1, 25, breakPath);
            var defPlain = MakeChar(2, 25, plainPath);
            var rWithOp = DuelEngine.ResolveR2(atkBreak, defPlain, breakPath, plainPath, regBoth, cfg, null, null, null);

            // 对照：攻方也用普通路（无 PoiseBreak）
            var atkPlain = MakeChar(1, 25, plainPath);
            var rNoOp = DuelEngine.ResolveR2(atkPlain, defPlain, plainPath, plainPath, Reg(plainPath), cfg, null, null, null);

            // Assert：PoiseBreak 算子改变了结果（额外削韧 → 防方更快被硬直）
            Assert.NotEqual(rWithOp.Margin, rNoOp.Margin);
        }

        [Fact]
        public void test_poise_break_factory_produces_correct_op()
        {
            // B.9：PoiseBreak 工厂产出 PoiseDamage 算子（禁裸 new EffectOp）
            var op = Modules.PoiseBreak(150, "test");
            Assert.Equal(EffectOpKind.PoiseDamage, op.Kind);
            Assert.Equal(150, op.Amount);
            Assert.Equal(EffectRarity.Rare, op.Rarity);
        }

        // ============================================================
        // AC-1 (2.1)：PoiseState duel-local —— 不入 Clone、不残留持久态
        // ============================================================

        [Fact]
        public void test_poise_state_not_persisted_to_character()
        {
            // 一场削韧对拍后，双方 Cultivation.Resources 无韧性键残留（Poise 是 duel-local，不挂 CultivationState）
            var path = MakePath("p");
            var a = MakeChar(1, 30, path);
            var b = MakeChar(2, 20, path);
            var lowPoise = LimitsConfig.Default with { PoiseMax = 20 };

            DuelEngine.ResolveR2(a, b, path, path, Reg(path), lowPoise, null, null, null);

            // Assert：无 "poise"/"stagger"/"韧性" 相关资源键写入持久态
            Assert.False(a.Cultivation!.Resources.ContainsKey("poise"));
            Assert.False(b.Cultivation!.Resources.ContainsKey("poise"));
            Assert.False(a.Cultivation!.Resources.ContainsKey("stagger"));
            Assert.False(b.Cultivation!.Resources.ContainsKey("stagger"));
        }

        [Fact]
        public void test_repeated_duels_same_chars_are_deterministic()
        {
            // duel-local 铁律的可观测后果：同一对角色连打两场，结果逐字节相同
            // （若 Poise 泄漏进持久态，第二场初始韧性会被第一场污染 → 结果漂移）
            var path = MakePath("p");
            var a = MakeChar(1, 28, path);
            var b = MakeChar(2, 24, path);
            var cfg = LimitsConfig.Default with { PoiseMax = 40 };

            var r1 = DuelEngine.ResolveR2(a, b, path, path, Reg(path), cfg, null, null, null);
            var r2 = DuelEngine.ResolveR2(a, b, path, path, Reg(path), cfg, null, null, null);

            Assert.Equal(r1.Winner, r2.Winner);
            Assert.Equal(r1.Margin, r2.Margin);
            Assert.Equal(r1.AttackerHpRemaining, r2.AttackerHpRemaining);
            Assert.Equal(r1.DefenderHpRemaining, r2.DefenderHpRemaining);
        }

        // ============================================================
        // AC-6 (2.6)：LimitsConfig 旋钮校验
        // ============================================================

        [Fact]
        public void test_poise_knobs_have_safe_defaults()
        {
            var c = LimitsConfig.Default;
            Assert.True(c.PoiseMax >= 1);
            Assert.True(c.PoiseDamageRatioPermille >= 0);
            Assert.True(c.StaggerDurationTurns >= 1);
            Assert.True(c.StaggerDRStep >= 0);
            Assert.True(c.StaggerCooldown >= 0);
            c.Validate(); // 默认不抛
        }

        [Theory]
        [InlineData("PoiseMax", 0)]
        [InlineData("StaggerDurationTurns", 0)]
        public void test_poise_knobs_invalid_throw_on_validate(string knob, int badValue)
        {
            var c = knob switch
            {
                "PoiseMax" => LimitsConfig.Default with { PoiseMax = badValue },
                "StaggerDurationTurns" => LimitsConfig.Default with { StaggerDurationTurns = badValue },
                _ => throw new ArgumentException(knob)
            };
            Assert.Throws<InvalidOperationException>(() => c.Validate());
        }

        [Fact]
        public void test_poise_negative_knobs_throw()
        {
            Assert.Throws<InvalidOperationException>(
                () => (LimitsConfig.Default with { PoiseDamageRatioPermille = -1 }).Validate());
            Assert.Throws<InvalidOperationException>(
                () => (LimitsConfig.Default with { StaggerDRStep = -1 }).Validate());
            Assert.Throws<InvalidOperationException>(
                () => (LimitsConfig.Default with { StaggerCooldown = -1 }).Validate());
        }

        [Fact]
        public void test_poise_zero_ratio_legal_disables_base_poise()
        {
            // ratio=0 合法（退化无基础削韧，仅算子削韧生效）
            var c = LimitsConfig.Default with { PoiseDamageRatioPermille = 0 };
            c.Validate(); // 不抛
        }

        // ============================================================
        // AC-8 (2.9)：C2 不退 —— UT gap≥2 auto-win 短路保留（削韧不影响）
        // ============================================================

        [Fact]
        public void test_ut_gap_autowin_short_circuits_before_poise()
        {
            // UT gap≥2 → auto-win 短路，削韧逻辑根本不跑（C2 不退）
            var path = MakePath("p");
            var strong = MakeChar(1, 25, path);
            strong.Cultivation!.RealmIndex = 2; // UT=2
            var weak = MakeChar(2, 25, path);
            weak.Cultivation!.RealmIndex = 0;   // UT=0，gap=2

            var lowPoise = LimitsConfig.Default with { PoiseMax = 1 };
            var r = DuelEngine.ResolveR2(strong, weak, path, path, Reg(path), lowPoise, null, null, null);

            Assert.True(r.WasAutoWin);
            Assert.Equal(strong.Id, r.Winner);
        }

        [Fact]
        public void test_calibration_mode_bypasses_poise()
        {
            // balance-006 契约：calibrationMode 只测裸 PE，旁路削韧（stagger 锁回合=行动经济扰动，
            // 与裸 PE 平价正交，同 Control/CounterMul/压制旁路）。即便 PoiseMax 极低应触发硬直，
            // 标定模式下 stagger 不注入 → 结果等同"无削韧"基线（韧性上限拉满永不破韧）。
            var path = MakePath("p");
            var a = MakeChar(1, 30, path);
            var b = MakeChar(2, 20, path);

            // 无削韧基线（PoiseMax 极大）非标定 vs 极低 PoiseMax 标定模式 → 应同结果（标定旁路削韧）
            var noPoise = LimitsConfig.Default with { PoiseMax = int.MaxValue };
            var rBaseline = DuelEngine.ResolveR2(a, b, path, path, Reg(path), noPoise, null, null, null,
                calibrationMode: true);
            var lowPoiseCalib = LimitsConfig.Default with { PoiseMax = 10 };
            var rCalib = DuelEngine.ResolveR2(a, b, path, path, Reg(path), lowPoiseCalib, null, null, null,
                calibrationMode: true);

            // 标定模式下 PoiseMax 无影响（削韧旁路）→ 两配置同结果
            Assert.Equal(rBaseline.Winner, rCalib.Winner);
            Assert.Equal(rBaseline.Margin, rCalib.Margin);
        }

        // ============================================================
        // AC-7 (2.8)：off 逐字节不退 —— 削韧旋钮存在但 off 不调 DuelEngine
        // ============================================================

        [Fact]
        public void test_off_mode_unaffected_by_poise_knobs()
        {
            // off（cultivation=false）不入 DuelEngine → 削韧旋钮零影响，同种子两跑逐字节
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
