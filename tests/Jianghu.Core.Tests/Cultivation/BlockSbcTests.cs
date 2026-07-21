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
    /// cv-008（adr-0010 决策②）：SBC 招式格挡系数调制 Chip 穿透测试——防御漏斗第②层（格挡层）。
    /// 覆盖：AC 8.1 Sbc 字段默认 1000 + 21 路惰性 / AC 8.2 ApplyBlockCoefficient 纯函数 /
    /// AC 8.3 SBC 调制 Chip 穿透（重锤 vs 易格挡）/ AC 8.7 B.2 浮点零 + B.3 off 不退 + 确定性。
    /// 触 Jianghu.Cultivation → 旗舰档 + 主控核验（B.7/A.3）。
    /// </summary>
    public class BlockSbcTests
    {
        static LimitsConfig Limits => LimitsConfig.Default;

        // —— fixtures（承 EvasionSecTests / ResistanceTests / TagGatingChipTests 范式）——
        // PE = stat:Force × 4 × RealmMult(RealmIndex=1 → 15)。Force=25 → PE=1500。
        // attackType = 攻方招式伤害类型（Elemental 触发 Chip 段）；attackSbc = 攻方招式 SBC；
        // defenseOp = 防方 OnDefend 模块（Block 类如 FlatDR 触发 blockFired=true → 进 Chip 段）。
        // PE 公式只读 Force，故 Constitution/Insight 可独立设而不扰 PE。
        static CultivationPathDef MakePath(string id,
            DamageType attackType = DamageType.Normal, int attackSbc = 1000,
            EffectOp? defenseOp = null, string? defenseRole = null)
        {
            // 攻方招式（带 DamageType + Sbc）。Sbc 作为 CombatSkillDef 第 8 参（位置参，承 cv-006 Sec 范式）。
            var atkSkill = new CombatSkillDef("atk", "atk", 0, Array.Empty<EffectOp>(),
                new Dictionary<string, int>(), attackType, 1000, attackSbc);

            // 防方招式：可挂 OnDefend Block 类模块（FlatDR）。同一招 OnUse 列表内 Trigger=OnDefend 自动分流。
            var defOnUse = new List<EffectOp>();
            if (defenseOp != null) defOnUse.Add(defenseOp);
            var defSkill = new CombatSkillDef("def", "def", 0, defOnUse.ToArray(),
                new Dictionary<string, int>(), DamageType.Normal, 1000, 1000);

            // gate 功法（FlatDR/Reflect 需 body role gate）
            var artCats = new List<ArtCategoryDef>();
            if (defenseRole != null)
            {
                var art = new ArtDef($"art_{defenseRole}", defenseRole, 1, defenseRole, Array.Empty<EffectOp>());
                artCats.Add(new ArtCategoryDef(defenseRole, defenseRole, 1, 1, new[] { art }));
            }

            return new CultivationPathDef(
                id, id, "physical", new[] { "melee" },
                new[] { new ResourceDef("qi", 0, 1000, 0) },
                new PowerFormulaDef(new[] { new PowerTerm("stat:Force", 4, null) },
                    Array.Empty<PowerMod>(), null),
                new RealmCurveDef(new[] { 10, 15, 25 }, new[] { 0, 1, 2 },
                    new[] { "L1", "L2", "L3" }, new[] { 0, 100, 300 },
                    new[] { 1, 1, 1 }, true, 2),
                artCats.ToArray(),
                new[] { atkSkill, defSkill },
                new EntryGateDef(""), new SelectionRuleDef(1, 3), null);
        }

        // 从 path.ArtCategories 派生 chosenArtIds（承 ResistanceTests.ChosenArtsOf 范式）。
        // 消除静态状态：每个 char 从其 path 自己派生，避免跨 path 覆盖（cv-007 曾因静态变量致 BodyArt 加成测试失效）。
        static string[] ChosenArtsOf(CultivationPathDef path)
        {
            var ids = new List<string>();
            foreach (var cat in path.ArtCategories)
                foreach (var art in cat.Arts)
                    ids.Add(art.Id);
            return ids.ToArray();
        }

        // stats: [Force, Internal, Constitution, Insight]（StatKind 枚举序）。
        // Constitution/Insight 默认 5 → 低 R（隔离抵抗层干扰，纯看 SBC 调制 Chip）。
        static Character MakeChar(long id, int force, CultivationPathDef path, bool asAttacker)
        {
            var c = new Character(new CharacterId(id),
                new Persona("n", "t", "s", ArchetypeKind.Martial, null),
                new StatBlock(new[] { force, 0, 5, 5 }),
                new NodeId(0), new Goal(GoalKind.Advance, 0), 0, 800, 16);
            var cult = CultivationState.NewForPath(path.PathId, path.Resources,
                ChosenArtsOf(path), asAttacker ? new[] { "atk" } : new[] { "def" });
            cult.RealmIndex = 1; // 同 UT（避免 gap≥2 auto-win 短路）
            c.Cultivation = cult;
            return c;
        }

        static PathRegistry Reg(params CultivationPathDef[] p) => new PathRegistry(new ListPathSource(p));
        static IRandom Rng(ulong seed) => new Pcg32(seed, (ulong)RngStreamIds.Duel);

        sealed class ListPathSource : IPathSource
        {
            private readonly IReadOnlyList<CultivationPathDef> _paths;
            public ListPathSource(IReadOnlyList<CultivationPathDef> paths) => _paths = paths;
            public IReadOnlyList<CultivationPathDef> Load() => _paths;
        }

        // ============================================================
        // AC 8.1：Sbc 字段默认 1000 + 惰性（显式 1000 == 默认省略）
        // ============================================================

        [Fact]
        public void test_sbc_default_is_1000_neutral()
        {
            // 省略 Sbc 参数 → 隐式默认 1000（中性）。AC 8.1：21 路现有构造零改动惰性。
            var explicitSkill = new CombatSkillDef("k", "k", 0, Array.Empty<EffectOp>(),
                new Dictionary<string, int>(), DamageType.Normal, 1000, 1000);
            var defaultSkill = new CombatSkillDef("k", "k", 0, Array.Empty<EffectOp>(),
                new Dictionary<string, int>()); // 省略 Damage/Sec/Sbc → 默认 Normal/1000/1000
            Assert.Equal(explicitSkill.Sbc, defaultSkill.Sbc);
            Assert.Equal(1000, defaultSkill.Sbc);
        }

        [Fact]
        public void test_default_sbc_equals_explicit_sbc_1000_byte_identical()
        {
            // 显式传 Sbc=1000 的招式 vs 默认（构造器省略 Sbc，隐式 1000）→ 同种子逐字节复现。
            // 证明 21 路现有构造（省略 Sbc）与显式 1000 行为等价 = 惰性零行为改变（AC 8.1）。
            var explicitPath = MakePath("explicit", DamageType.Elemental, attackSbc: 1000,
                defenseOp: Modules.FlatDR(10, "护体"), defenseRole: "body");

            // 默认路径：构造 CombatSkillDef 时省略 Sbc 参数（隐式默认 1000）
            var defaultAtkSkill = new CombatSkillDef("atk", "atk", 0, Array.Empty<EffectOp>(),
                new Dictionary<string, int>(), DamageType.Elemental, 1000); // 省略 Sbc → 1000
            var defSkill = new CombatSkillDef("def", "def", 0,
                new[] { Modules.FlatDR(10, "护体") }, new Dictionary<string, int>(),
                DamageType.Normal, 1000, 1000);
            var defaultPath = new CultivationPathDef(
                "default", "default", "physical", new[] { "melee" },
                new[] { new ResourceDef("qi", 0, 1000, 0) },
                new PowerFormulaDef(new[] { new PowerTerm("stat:Force", 4, null) },
                    Array.Empty<PowerMod>(), null),
                new RealmCurveDef(new[] { 10, 15, 25 }, new[] { 0, 1, 2 },
                    new[] { "L1", "L2", "L3" }, new[] { 0, 100, 300 },
                    new[] { 1, 1, 1 }, true, 2),
                new[] { new ArtCategoryDef("body", "body", 1, 1,
                    new[] { new ArtDef("art_body", "body", 1, "body", Array.Empty<EffectOp>()) }) },
                new[] { defaultAtkSkill, defSkill },
                new EntryGateDef(""), new SelectionRuleDef(1, 3), null);

            var aExplicit = MakeChar(1, 25, explicitPath, asAttacker: true);
            var aDefault = MakeChar(1, 25, defaultPath, asAttacker: true);
            var b = MakeChar(2, 24, explicitPath, asAttacker: false);

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
        // AC 8.2：ApplyBlockCoefficient 纯函数（无 RNG、无 IO，确定性 B.2）
        // ============================================================

        [Fact]
        public void test_sbc_1000_is_neutral_unchanged()
        {
            // SBC=1000 中性 → effChip = baseChipPermille 不变（AC 8.1 惰性的纯函数根据）
            Assert.Equal(300, CombatMath.ApplyBlockCoefficient(300, 1000));
            Assert.Equal(1, CombatMath.ApplyBlockCoefficient(1, 1000));
            Assert.Equal(0, CombatMath.ApplyBlockCoefficient(0, 1000)); // base=0 → 0
        }

        [Fact]
        public void test_sbc_0_is_unblockable_returns_1000()
        {
            // SBC=0 不可格挡穿透 → 返回 1000（全伤），比照 cv-003 Blunt 门控。
            // 显式分支（非 max(1,SBC)），不进除法（adr-0010 决策②）
            Assert.Equal(1000, CombatMath.ApplyBlockCoefficient(300, 0));
            Assert.Equal(1000, CombatMath.ApplyBlockCoefficient(1, 0));
            Assert.Equal(1000, CombatMath.ApplyBlockCoefficient(999, 0));
        }

        [Fact]
        public void test_sbc_500_doubles_chip_penetration()
        {
            // SBC=500 重锤 → effChip = baseChipPermille*2（抬穿透）
            Assert.Equal(600, CombatMath.ApplyBlockCoefficient(300, 500));  // 300*1000/500 = 600
            Assert.Equal(2, CombatMath.ApplyBlockCoefficient(1, 500));       // 1*1000/500 = 2
            Assert.Equal(1000, CombatMath.ApplyBlockCoefficient(500, 500));  // 500*1000/500 = 1000
        }

        [Fact]
        public void test_sbc_2000_halves_chip_penetration()
        {
            // SBC=2000 易格挡 → effChip = baseChipPermille/2（降穿透）
            Assert.Equal(150, CombatMath.ApplyBlockCoefficient(300, 2000));  // 300*1000/2000 = 150
            Assert.Equal(0, CombatMath.ApplyBlockCoefficient(1, 2000));       // 1*1000/2000 = 0.5 → 0（向下取整）
        }

        [Fact]
        public void test_sbc_1_extreme_no_overflow()
        {
            // SBC=1 极端重锤 → effChip = baseChipPermille*1000（极大但不溢出，long 中间量）。
            // AC 8.2 边界：SBC=1 极端重锤 → effChip 极大但不溢出。
            Assert.Equal(300000, CombatMath.ApplyBlockCoefficient(300, 1));   // 300*1000/1 = 300000
            Assert.Equal(1000, CombatMath.ApplyBlockCoefficient(1, 1));        // 1*1000/1 = 1000
            // 极大 baseChipPermille 不溢出（long 中间 + int.MaxValue 回绕守卫）
            int result = CombatMath.ApplyBlockCoefficient(int.MaxValue, 1);
            Assert.True(result >= 0);
            Assert.Equal(int.MaxValue, result); // 钳 int.MaxValue
        }

        [Fact]
        public void test_sbc_low_does_not_clamp_upper()
        {
            // 低 SBC 重锤可让 effChip > 1000（破防语义，不钳上界）。
            // SBC=300 → 300*1000/300 = 1000（恰 1000）；SBC=250 → 300*1000/250 = 1200（>1000，破防）
            Assert.Equal(1000, CombatMath.ApplyBlockCoefficient(300, 300));
            Assert.Equal(1200, CombatMath.ApplyBlockCoefficient(300, 250));
            Assert.Equal(1500, CombatMath.ApplyBlockCoefficient(300, 200));
        }

        [Fact]
        public void test_zero_base_permille_returns_zero()
        {
            // baseChipPermille=0 → 0（无 chip 基准可调制，与 ChipDamageFloor chipPermille≤0 退化一致）
            Assert.Equal(0, CombatMath.ApplyBlockCoefficient(0, 1000));
            Assert.Equal(0, CombatMath.ApplyBlockCoefficient(0, 0));
            Assert.Equal(0, CombatMath.ApplyBlockCoefficient(0, 500));
            Assert.Equal(0, CombatMath.ApplyBlockCoefficient(0, 1));
        }

        [Fact]
        public void test_negative_sbc_returns_neutral_defensive()
        {
            // 病态负 SBC（生产恒 ≥0）→ 防御性归中性（返 baseChipPermille），避免负 permille 进下游
            Assert.Equal(300, CombatMath.ApplyBlockCoefficient(300, -1));
            Assert.Equal(300, CombatMath.ApplyBlockCoefficient(300, -100));
            Assert.Equal(1, CombatMath.ApplyBlockCoefficient(1, -1));
        }

        [Fact]
        public void test_apply_block_is_deterministic_pure_function()
        {
            // 纯函数：同输入恒同输出（B.2）
            for (int baseChip = 0; baseChip <= 1000; baseChip += 50)
                for (int sbc = 0; sbc <= 2000; sbc += 100)
                    Assert.Equal(
                        CombatMath.ApplyBlockCoefficient(baseChip, sbc),
                        CombatMath.ApplyBlockCoefficient(baseChip, sbc));
        }

        // ============================================================
        // AC 8.3：SBC 调制 Chip 穿透——同防方同攻击，SBC=500 穿透 > SBC=2000
        // ============================================================

        [Fact]
        public void test_lower_sbc_penetrates_more_than_higher_sbc()
        {
            // 同防方（高 FlatDR Block 类）+ 同 Elemental 攻击，攻方 SBC=500（重锤）vs SBC=2000（易格挡）。
            // SBC=500 → effChip=600（重锤穿透更多）→ 防方受创更重 → 残血更低。
            // SBC=2000 → effChip=150（易格挡，穿透少）→ 防方更耐打 → 残血更高。
            // 用确定性（duelRng=null）隔离 cv-001 概率噪声，纯看 SBC 调制 Chip 差异。
            // 防方 FlatDR(20) 较强减伤确保 blockFired=true 且 dmg 被压到 chipFloor 之上（触发穿透保底）。
            var flatDR = Modules.FlatDR(20, "护体");
            var pathSbc500 = MakePath("atk500", DamageType.Elemental, attackSbc: 500,
                defenseOp: flatDR, defenseRole: "body");
            var pathSbc2000 = MakePath("atk2000", DamageType.Elemental, attackSbc: 2000,
                defenseOp: flatDR, defenseRole: "body");

            var a500 = MakeChar(1, 25, pathSbc500, asAttacker: true);
            var a2000 = MakeChar(1, 25, pathSbc2000, asAttacker: true);
            var b = MakeChar(2, 25, pathSbc500, asAttacker: false); // 防方同配置

            var r500 = DuelEngine.ResolveR2(a500, b, pathSbc500, pathSbc500,
                Reg(pathSbc500), Limits, null, null, null);
            var r2000 = DuelEngine.ResolveR2(a2000, b, pathSbc2000, pathSbc2000,
                Reg(pathSbc2000), Limits, null, null, null);

            // SBC=500（重锤，穿透多）→ 防方残血更低（受创更重）
            Assert.True(r500.DefenderHpRemaining < r2000.DefenderHpRemaining,
                $"SBC=500 重锤应使防方残血({r500.DefenderHpRemaining}) < SBC=2000 易格挡({r2000.DefenderHpRemaining}) —— SBC 调制未生效或方向反");
        }

        [Fact]
        public void test_sbc_0_unblockable_penetrates_more_than_neutral()
        {
            // SBC=0（不可格挡穿透，effChip=1000 全伤）vs SBC=1000（中性，effChip=300）。
            // SBC=0 → 穿透保底更高 → 防方受创更重 → 残血更低。
            var flatDR = Modules.FlatDR(20, "护体");
            var pathSbc0 = MakePath("atk0", DamageType.Elemental, attackSbc: 0,
                defenseOp: flatDR, defenseRole: "body");
            var pathSbc1000 = MakePath("atk1k", DamageType.Elemental, attackSbc: 1000,
                defenseOp: flatDR, defenseRole: "body");

            var a0 = MakeChar(1, 25, pathSbc0, asAttacker: true);
            var a1000 = MakeChar(1, 25, pathSbc1000, asAttacker: true);
            var b = MakeChar(2, 25, pathSbc0, asAttacker: false);

            var r0 = DuelEngine.ResolveR2(a0, b, pathSbc0, pathSbc0,
                Reg(pathSbc0), Limits, null, null, null);
            var r1000 = DuelEngine.ResolveR2(a1000, b, pathSbc1000, pathSbc1000,
                Reg(pathSbc1000), Limits, null, null, null);

            // SBC=0 不可格挡（全伤穿透）→ 防方残血更低
            Assert.True(r0.DefenderHpRemaining < r1000.DefenderHpRemaining,
                $"SBC=0 不可格挡应使防方残血({r0.DefenderHpRemaining}) < SBC=1000 中性({r1000.DefenderHpRemaining})");
        }

        // ============================================================
        // AC 8.7：B.2 浮点零 + B.3 off 不退 + 确定性
        // ============================================================

        [Fact]
        public void test_cultivation_namespace_has_no_float_after_sbc()
        {
            // B.2：ApplyBlockCoefficient 全整数（long 中间量 + 整数除法），IL 扫描 Jianghu.Cultivation 命名空间零浮点。
            var asmPath = typeof(Jianghu.Sim.World).Assembly.Location;
            var offenders = ILFloatScanner.ScanNamespace(asmPath, "Jianghu.Cultivation");
            Assert.True(offenders.Count == 0, "浮点出现在: " + string.Join(", ", offenders));
        }

        [Fact]
        public void test_sbc_modulation_is_deterministic_same_seed()
        {
            // B.2 确定性：SBC 调制同种子两跑逐字节复现（SBC 不新增 RNG，确定性调制）
            var flatDR = Modules.FlatDR(15, "护体");
            var path = MakePath("atk", DamageType.Elemental, attackSbc: 500,
                defenseOp: flatDR, defenseRole: "body");
            var a = MakeChar(1, 25, path, asAttacker: true);
            var b = MakeChar(2, 24, path, asAttacker: false);

            var r1 = DuelEngine.ResolveR2(a, b, path, path, Reg(path),
                Limits, null, null, null, duelRng: Rng(42));
            var r2 = DuelEngine.ResolveR2(a, b, path, path, Reg(path),
                Limits, null, null, null, duelRng: Rng(42));

            Assert.Equal(r1.Winner, r2.Winner);
            Assert.Equal(r1.Margin, r2.Margin);
            Assert.Equal(r1.AttackerHpRemaining, r2.AttackerHpRemaining);
            Assert.Equal(r1.DefenderHpRemaining, r2.DefenderHpRemaining);
        }

        [Fact]
        public void test_off_mode_unaffected_by_sbc_field()
        {
            // B.3：off（cultivation=false）不入 DuelEngine → SBC 字段零影响，同种子两跑 Chronicle 逐字节。
            // SBC 是 cultivation-on 路径数据，off 走 legacy SparAction 天然安全。
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
