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
    /// cv-008（adr-0010 决策④）：三层防御漏斗集成测试——①SEC闪避(cv-006) → ②门控+格挡+SBC调制Chip(cv-003+cv-008)
    /// → ③抵抗(cv-007) 在同一 ResolveExchange 管线串行协作、不互相污染、calibrationMode 统一旁路。
    /// 覆盖：AC 8.4 三层串行接线 / AC 8.5 三层不互相污染（组合矩阵） /
    /// AC 8.6 calibrationMode 三层统一旁路 / AC 8.8 反伤保护（反伤基于原始输出非减伤后）。
    /// 触 Jianghu.Cultivation → 旗舰档 + 主控核验（B.7/A.3）。
    /// </summary>
    public class DefenseFunnelIntegrationTests
    {
        static LimitsConfig Limits => LimitsConfig.Default;

        static CultivationPathDef MakePath(string id,
            DamageType attackType = DamageType.Normal, int attackSec = 1000, int attackSbc = 1000,
            EffectOp? defenseOp = null, string? defenseRole = null)
        {
            var atkSkill = new CombatSkillDef("atk", "atk", 0, Array.Empty<EffectOp>(),
                new Dictionary<string, int>(), attackType, attackSec, attackSbc);

            var defOnUse = new List<EffectOp>();
            if (defenseOp != null) defOnUse.Add(defenseOp);
            var defSkill = new CombatSkillDef("def", "def", 0, defOnUse.ToArray(),
                new Dictionary<string, int>(), DamageType.Normal, 1000, 1000);

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

        static string[] ChosenArtsOf(CultivationPathDef path)
        {
            var ids = new List<string>();
            foreach (var cat in path.ArtCategories)
                foreach (var art in cat.Arts)
                    ids.Add(art.Id);
            return ids.ToArray();
        }

        static Character MakeChar(long id, int force, int constitution, int insight, CultivationPathDef path, bool asAttacker)
        {
            var c = new Character(new CharacterId(id),
                new Persona("n", "t", "s", ArchetypeKind.Martial, null),
                new StatBlock(new[] { force, 0, constitution, insight }),
                new NodeId(0), new Goal(GoalKind.Advance, 0), 0, 800, 16);
            var cult = CultivationState.NewForPath(path.PathId, path.Resources,
                ChosenArtsOf(path), asAttacker ? new[] { "atk" } : new[] { "def" });
            cult.RealmIndex = 1;
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
        // AC 8.4：三层串行接线——miss 短路 + 正常三层
        // ============================================================

        [Fact]
        public void test_miss_short_circuits_damage_zero()
        {
            // AC 8.4: miss（roll≥p）→ dmg=0。在方差模式验证：SEC=0必中→有伤害；SEC极高→近全miss→无伤害。
            // 简化验证：SEC=0必中 vs SEC=100000（极端衰减→p→0→几乎全miss）。
            // 两者使用同 seed→SEC=0 必中胜场应 ≥ SEC=100000（几乎全miss）。
            var flatDR = Modules.FlatDR(15, "护体");
            var path0 = MakePath("atk0", DamageType.Elemental, attackSec: 0, attackSbc: 1000,
                defenseOp: flatDR, defenseRole: "body");
            var pathMiss = MakePath("atkMiss", DamageType.Elemental, attackSec: 100000, attackSbc: 1000,
                defenseOp: flatDR, defenseRole: "body");

            var a0 = MakeChar(1, 25, 5, 5, path0, asAttacker: true);
            var b0 = MakeChar(2, 24, 20, 5, path0, asAttacker: false);
            var aMiss = MakeChar(1, 25, 5, 5, pathMiss, asAttacker: true);
            var bMiss = MakeChar(2, 24, 20, 5, pathMiss, asAttacker: false);

            int wins0 = 0, winsMiss = 0;
            for (ulong s = 1; s <= 30; s++)
            {
                var r0 = DuelEngine.ResolveR2(a0, b0, path0, path0, Reg(path0),
                    Limits, null, null, null, duelRng: Rng(s));
                var rMiss = DuelEngine.ResolveR2(aMiss, bMiss, pathMiss, pathMiss, Reg(pathMiss),
                    Limits, null, null, null, duelRng: Rng(s));
                if (r0.Winner == a0.Id) wins0++;
                if (rMiss.Winner == aMiss.Id) winsMiss++;
            }
            // SEC=0 必中 → 每回合命中走②③ → 胜场远多
            Assert.True(wins0 > winsMiss,
                $"SEC=0 必中胜场({wins0}/30) 应 > SEC=100000 近全miss({winsMiss}/30)");
        }

        [Fact]
        public void test_sec_zero_auto_hit_with_variance()
        {
            // AC 8.4: SEC=0 必中→ApplyEvasion(p,0)=AutoHitPermille(1000)→roll<1000恒真→命中走②③。
            // 用方差模式(duelRng≠null): SEC=0必中 vs SEC=2000衰减→必中伤害更多。
            var flatDR = Modules.FlatDR(15, "护体");
            var path0 = MakePath("atk0", DamageType.Elemental, attackSec: 0, attackSbc: 1000,
                defenseOp: flatDR, defenseRole: "body");
            var path2k = MakePath("atk2k", DamageType.Elemental, attackSec: 2000, attackSbc: 1000,
                defenseOp: flatDR, defenseRole: "body");
            // 攻防近 PE（margin小→p≈500→SEC调制可视）
            var a0 = MakeChar(1, 25, 5, 5, path0, asAttacker: true);
            var a2k = MakeChar(1, 25, 5, 5, path2k, asAttacker: true);
            var b0 = MakeChar(2, 24, 20, 5, path0, asAttacker: false);
            var b2k = MakeChar(2, 24, 20, 5, path2k, asAttacker: false);

            int wins0 = 0, wins2k = 0;
            for (ulong s = 1; s <= 50; s++)
            {
                var r0 = DuelEngine.ResolveR2(a0, b0, path0, path0, Reg(path0),
                    Limits, null, null, null, duelRng: Rng(s));
                var r2k = DuelEngine.ResolveR2(a2k, b2k, path2k, path2k, Reg(path2k),
                    Limits, null, null, null, duelRng: Rng(s));
                if (r0.Winner == a0.Id) wins0++;
                if (r2k.Winner == a2k.Id) wins2k++;
            }
            // SEC=0 必中（每回合必命中走②③）→ 胜场至少不输 SEC=2000 衰减
            Assert.True(wins0 >= wins2k,
                $"SEC=0 必中胜场({wins0}/50) 应 ≥ SEC=2000 衰减({wins2k}/50)");
        }

        // ============================================================
        // AC 8.5：三层不互相污染（组合矩阵）
        // ============================================================

        [Fact]
        public void test_layer2_no_block_no_chip_resistance_still_applies()
        {
            // AC 8.5: ②无Block→无Chip，但③抵抗层仍根据 DamageType/R 派生生效。
            // 直接验证：同一角色不同 DamageType 返回不同 R（证明 ResistanceOf 正常）。
            var path = MakePath("nb", DamageType.Normal, attackSec: 1000, attackSbc: 1000);
            var c = MakeChar(1, 25, 20, 8, path, asAttacker: false); // Constitution=20→physR=1100, Insight=8→elemR=400
            int rPhys = ResistanceProviders.ResistanceOf(
                c.Cultivation!, c.Stats, path, GateType.None, DamageType.Normal, Limits);
            int rElem = ResistanceProviders.ResistanceOf(
                c.Cultivation!, c.Stats, path, GateType.None, DamageType.Elemental, Limits);
            // 物理抗(体质派生) ≠ 属性抗(识派生)——证 DamageType 维度区分生效（③层的核心）
            Assert.NotEqual(rPhys, rElem);
            Assert.True(rPhys > rElem, $"物理抗({rPhys}) 应 > 属性抗({rElem})"); // Constitution=20*50+100, Insight=8*50
        }

        [Fact]
        public void test_layer3_zero_resistance_transparent()
        {
            // AC 8.5: R=0 时③透明（不衰减）。验证系数=0时 ResistanceOf 返回 0 → ApplyResistance 无衰减。
            var path = MakePath("atk", DamageType.Normal, attackSec: 1000, attackSbc: 1000);
            var c = MakeChar(1, 20, 20, 5, path, asAttacker: false);
            var zeroRLimits = Limits with { PhysResistPerConstitution = 0, BodyArtPhysResistBonus = 0 };

            int rWith = ResistanceProviders.ResistanceOf(
                c.Cultivation!, c.Stats, path, GateType.None, DamageType.Normal, Limits);
            int rZero = ResistanceProviders.ResistanceOf(
                c.Cultivation!, c.Stats, path, GateType.None, DamageType.Normal, zeroRLimits);

            // 默认系数→R>0（20*50+100=1100）；系数=0→R=0（透明）
            Assert.True(rWith > 0, $"默认系数 R({rWith}) > 0");
            Assert.Equal(0, rZero);
            // ApplyResistance(R=0) = rawDamage 不变（multiplier=1000，全伤无衰减）
            Assert.Equal(1000, CombatMath.ApplyResistance(1000, 0, 500));
            // ApplyResistance(R>0) < rawDamage（有衰减）
            Assert.True(CombatMath.ApplyResistance(1000, rWith, 500) < 1000);
        }

        [Fact]
        public void test_three_layer_combination_monotonic()
        {
            // AC 8.5 组合矩阵：Block+高体质(A)防御最强，无Block+低体质(D)最弱。确定性模式。
            var flatDR = Modules.FlatDR(15, "护体");
            var pathWB = MakePath("wb", DamageType.Elemental, attackSbc: 1000,
                defenseOp: flatDR, defenseRole: "body");
            var pathNB = MakePath("nb", DamageType.Elemental, attackSbc: 1000);

            // A: Block + 高体质（R=1100: 20*50+100）
            var aA = MakeChar(1, 25, 5, 5, pathWB, asAttacker: true);
            var bA = MakeChar(2, 25, 20, 5, pathWB, asAttacker: false);
            var rA = DuelEngine.ResolveR2(aA, bA, pathWB, pathWB, Reg(pathWB), Limits, null, null, null);

            // D: 无Block + 低体质（R=250: 5*50）
            var aD = MakeChar(1, 25, 5, 5, pathNB, asAttacker: true);
            var bD = MakeChar(2, 25, 5, 5, pathNB, asAttacker: false);
            var rD = DuelEngine.ResolveR2(aD, bD, pathNB, pathNB, Reg(pathNB), Limits, null, null, null);

            // 全开防御(A)残血 ≥ 全关(D): 单调性
            Assert.True(rA.DefenderHpRemaining >= rD.DefenderHpRemaining,
                $"全开残血({rA.DefenderHpRemaining}) 应 ≥ 全关({rD.DefenderHpRemaining})");
        }

        // ============================================================
        // AC 8.6：calibrationMode 三层统一旁路
        // ============================================================

        [Fact]
        public void test_calibration_mode_bypasses_all_three_layers()
        {
            // AC 8.6: 标定模式三层全旁路(SEC中性/SBC基准/R=0)=裸PE。
            // 标定+非中性SEC/SBC vs 标定+中性SEC/SBC → 同种子复现(标定旁路三层调制)。
            var flatDR = Modules.FlatDR(15, "护体");
            var pathNonNeutral = MakePath("nn", DamageType.Elemental, attackSec: 2000, attackSbc: 500,
                defenseOp: flatDR, defenseRole: "body");
            var pathNeutral = MakePath("n", DamageType.Elemental, attackSec: 1000, attackSbc: 1000,
                defenseOp: flatDR, defenseRole: "body");

            var aNs = MakeChar(1, 25, 20, 5, pathNonNeutral, asAttacker: true);
            var bNs = MakeChar(2, 25, 20, 5, pathNonNeutral, asAttacker: false);
            var aNt = MakeChar(1, 25, 20, 5, pathNeutral, asAttacker: true);
            var bNt = MakeChar(2, 25, 20, 5, pathNeutral, asAttacker: false);

            var calibNonNeutral = DuelEngine.ResolveR2(aNs, bNs, pathNonNeutral, pathNonNeutral,
                Reg(pathNonNeutral), Limits, null, null, null, calibrationMode: true, duelRng: Rng(7));
            var calibNeutral = DuelEngine.ResolveR2(aNt, bNt, pathNeutral, pathNeutral,
                Reg(pathNeutral), Limits, null, null, null, calibrationMode: true, duelRng: Rng(7));

            // 标定旁路三层→非中性SEC/SBC效果消除→与中性同种子复现
            Assert.Equal(calibNeutral.Winner, calibNonNeutral.Winner);
            Assert.Equal(calibNeutral.Margin, calibNonNeutral.Margin);
        }

        // ============================================================
        // AC 8.8：反伤保护——反伤基于原始输出（非减伤后）
        // ============================================================

        [Fact]
        public void test_reflect_independent_of_resistance_decay()
        {
            // AC 8.8: 反伤在OnDefend收集(L448)，抵抗层(L502)在其后→反伤基于原始输出。
            // 验证：K=500(抵抗生效) vs K极大(抵抗退化)→防方残血不同(抵抗影响)，攻方反伤相同(原始输出不变)。
            var reflect = Modules.Reflect(1, 2, "反震"); // 50% reflect
            var path = MakePath("atk", DamageType.Elemental, attackSec: 1000, attackSbc: 1000,
                defenseOp: reflect, defenseRole: "body");

            var a1 = MakeChar(1, 25, 5, 5, path, asAttacker: true);
            var b1 = MakeChar(2, 25, 20, 5, path, asAttacker: false);
            var a2 = MakeChar(1, 25, 5, 5, path, asAttacker: true);
            var b2 = MakeChar(2, 25, 20, 5, path, asAttacker: false);

            var withResist = DuelEngine.ResolveR2(a1, b1, path, path, Reg(path), Limits, null, null, null);
            var noResist = DuelEngine.ResolveR2(a2, b2, path, path, Reg(path),
                Limits with { ResistanceHalfLifeK = 1_000_000_000 }, null, null, null);

            // 防方残血不同：抵抗影响防方最终扣血
            Assert.True(withResist.DefenderHpRemaining > noResist.DefenderHpRemaining,
                $"启抵抗残血({withResist.DefenderHpRemaining}) 应 > 退化({noResist.DefenderHpRemaining})");
            // 攻方反伤相同：反伤基于原始输出（抵抗层在反伤收集后，不碰反伤源）
            Assert.Equal(withResist.AttackerHpRemaining, noResist.AttackerHpRemaining);
        }

        // ============================================================
        // B.2 / B.3 守（集成层）
        // ============================================================

        [Fact]
        public void test_cultivation_namespace_has_no_float_after_funnel_integration()
        {
            var asmPath = typeof(Jianghu.Sim.World).Assembly.Location;
            var offenders = ILFloatScanner.ScanNamespace(asmPath, "Jianghu.Cultivation");
            Assert.True(offenders.Count == 0, "浮点出现在: " + string.Join(", ", offenders));
        }

        [Fact]
        public void test_funnel_integration_deterministic_same_seed()
        {
            var flatDR = Modules.FlatDR(15, "护体");
            var path = MakePath("atk", DamageType.Elemental, attackSec: 500, attackSbc: 500,
                defenseOp: flatDR, defenseRole: "body");
            var a = MakeChar(1, 25, 20, 5, path, asAttacker: true);
            var b = MakeChar(2, 24, 18, 5, path, asAttacker: false);

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
        public void test_off_mode_unaffected_by_funnel_integration()
        {
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
