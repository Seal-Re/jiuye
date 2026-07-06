using System;
using System.Collections.Generic;
using System.Linq;
using Jianghu.Config;
using Jianghu.Cultivation;
using Jianghu.Cultivation.Paths;
using Jianghu.Model;
using Jianghu.Stats;
using Xunit;

namespace Jianghu.Core.Tests.Cultivation
{
    /// <summary>
    /// balance-008: turns=1 控制哑弹裁决 — 方案C 分级语义（2026-07-06 用户裁定）。
    ///
    /// turns=1 = 即时打断（interrupt，拒止 1 回合）；turns≥2 = 定身/长控（保持 balance-007 的 N−1 拒止不变）。
    /// 根因：tick 时序令控制在"施加回合末"白耗一次递减 → turns=N 实际拒止 N−1 回合，故 base=1 现为哑弹。
    /// 修法（隔离补偿）：StoredControlTurns(base,eff) 对 base=1 存 eff+1（补偿→拒止1），base≥2 存 eff 原样。
    ///
    /// Red lines：B.2（纯整数确定）、B.3（off 不调 DuelEngine 天然守）、B.4（21 路 4 招不漏）。
    /// </summary>
    public class ControlInterruptTests
    {
        // —— 自包含 fixtures（同 ControlCooldownTests 风格）——

        static Character MakeChar(long id, int force, CultivationState cult)
        {
            var c = new Character(new CharacterId(id),
                new Persona("n", "t", "s", ArchetypeKind.Martial, null),
                new StatBlock(new[] { force, 0, 0, 0 }),
                new NodeId(0), new Goal(GoalKind.Advance, 0), 0, 800, 16);
            c.Cultivation = cult;
            return c;
        }

        static CultivationPathDef MakePath(string id, IReadOnlyList<CombatSkillDef> skills)
            => new CultivationPathDef(
                id, id, "physical",
                new[] { "melee" },
                new[] { new ResourceDef("qi", 0, 1000, 0) },
                new PowerFormulaDef(new[] { new PowerTerm("stat:Force", 4, null) }, Array.Empty<PowerMod>(), null),
                new RealmCurveDef(new[] { 10, 15, 25 }, new[] { 0, 1, 2 }, new[] { "L1", "L2", "L3" },
                    new[] { 0, 100, 300 }, new[] { 1, 1, 1 }, true, 2),
                Array.Empty<ArtCategoryDef>(),
                skills,
                new EntryGateDef(""),
                new SelectionRuleDef(1, 3),
                null);

        /// <summary>攻方带 turns=baseTurns 控制、防方无技能、等 PE 的对拍。攻方唯一优势=控制。</summary>
        static DuelEngine.Result RunControlDuel(int baseTurns, int cooldown = 0, int drStep = 0)
        {
            var ctrlSkill = new CombatSkillDef("sk_ctrl", "sk_ctrl", 0,
                new[] { Modules.Control("testlock", baseTurns) }, new Dictionary<string, int>());
            var atkPath = MakePath("atk", new[] { ctrlSkill });
            var defPath = MakePath("def", Array.Empty<CombatSkillDef>());
            var reg = new PathRegistry(new ListPathSource(new[] { atkPath, defPath }));

            var atkCult = CultivationState.NewForPath("atk", atkPath.Resources);
            var defCult = CultivationState.NewForPath("def", defPath.Resources);
            var atk = MakeChar(1, 20, atkCult);
            var def = MakeChar(2, 20, defCult);

            var limits = LimitsConfig.Default with { ControlCooldown = cooldown, ControlDRStep = drStep };
            return DuelEngine.ResolveR2(atk, def, atkPath, defPath, reg, limits, null, ctrlSkill, null);
        }

        // ================================================================
        // AC 8.2 — StoredControlTurns 纯函数（分级语义阶梯）
        // ================================================================

        [Theory]
        [InlineData(1, 1, 2)]   // base=1 即时打断：存 eff+1=2 → 实际拒止 1 回合（补偿 tick 损耗）
        [InlineData(2, 2, 2)]   // base=2 长控：存 eff 原样 → 拒止 1 回合（现有 N−1，不变）
        [InlineData(3, 3, 3)]   // base=3：存原样 → 拒止 2 回合（不变）
        [InlineData(2, 1, 1)]   // base=2 经 DR eff=1：存原样 1 → 拒止 0（balance-007 守，不补偿）
        public void StoredControlTurns_TieredSemantics(int baseTurns, int effTurns, int expectedStored)
        {
            // Act
            int stored = DuelEngine.StoredControlTurns(baseTurns, effTurns);

            // Assert
            Assert.Equal(expectedStored, stored);
        }

        // ================================================================
        // AC 8.2 — turns=1 现在真打断（哑弹 → 拒止 1 回合，攻方获优势）
        // ================================================================

        [Fact]
        public void Turns1Control_NowInterrupts_AttackerGainsAdvantage()
        {
            // Act
            var r = RunControlDuel(baseTurns: 1);

            // Assert：turns=1 打断防方 1 回合 → 攻方胜且有正 margin（现状哑弹时 margin=0 平局）
            Assert.Equal(1, r.Winner.Value);
            Assert.True(r.Margin > 0, $"turns=1 打断应使攻方获优势 margin>0，实为 {r.Margin}（哑弹回归）");
        }

        // ================================================================
        // AC 8.4 — turns≥2 拒止不变（balance-007 守）
        // ================================================================

        [Fact]
        public void Turns2Control_Unchanged_StillDominates()
        {
            // Act
            var r = RunControlDuel(baseTurns: 2);

            // Assert：turns=2 现有 N−1=1 拒止不变 → 攻方碾压（与 balance-007 基线一致）
            Assert.Equal(1, r.Winner.Value);
            Assert.True(r.Margin >= 50, $"turns=2 长控碾压 margin≥50 应不变，实为 {r.Margin}");
        }

        [Fact]
        public void Turns1_Interrupt_Equals_Turns2_BothDenyOneRound()
        {
            // Act
            var r1 = RunControlDuel(baseTurns: 1);
            var r2 = RunControlDuel(baseTurns: 2);

            // Assert：裁决下二者等价——turns=1 补偿后存 2（拒止1回合），turns=2 存 2（现有 N−1=1 拒止）。
            // 均拒止 1 回合 → margin 相同（这正是"turns=1 打断=拒止1回合"的正确落地）。
            Assert.Equal(r2.Margin, r1.Margin);
        }

        [Fact]
        public void Turns3_LocksLonger_Than_Turns1Interrupt()
        {
            // Act
            var r1 = RunControlDuel(baseTurns: 1);   // 打断：拒止 1 回合
            var r3 = RunControlDuel(baseTurns: 3);   // 长控：拒止 N−1=2 回合

            // Assert：turns=3 拒止更久 → 碾压幅度 ≥ turns=1 打断（长控档确实比即时打断更强/不弱）
            Assert.True(r3.Margin >= r1.Margin,
                $"turns=3(拒止2回合) 应 ≥ turns=1(打断，拒止1回合)：r3={r3.Margin} r1={r1.Margin}");
        }

        // ================================================================
        // AC 8.3 — 21 路 4 招 turns=1 数据一致性（B.4 不漏路）
        // ================================================================

        [Fact]
        public void FourSignatureControls_AreTurns1_DataLevel()
        {
            // Arrange：4 条路的签名 turns=1 控制招（story-008 问题陈述）
            var cases = new (CultivationPathDef path, string ctrlKey)[]
            {
                (YinXiuYuedaoPath.Def, "mihun"),
                (GhostYangHunPath.Def, "soulLock"),
                (RuXiuHaoranPath.Def, "lawPrison"),
                (YinguoFazePath.Def, "voidPrison"),
            };

            // Assert：每条路存在一个 turns=1 的 Control 算子（裁决作用对象）
            foreach (var (path, ctrlKey) in cases)
            {
                bool found = path.CombatSkills
                    .SelectMany(s => s.OnUse)
                    .Any(op => op.Kind == EffectOpKind.Control && op.Key == ctrlKey && op.Amount == 1);
                Assert.True(found, $"路 {path.PathId} 应含 turns=1 控制 '{ctrlKey}'（4 招裁决对象，B.4）");
            }
        }

        // ================================================================
        // AC 8.5 — 确定性
        // ================================================================

        [Fact]
        public void Turns1Interrupt_Deterministic()
        {
            // Act
            var a = RunControlDuel(baseTurns: 1);
            var b = RunControlDuel(baseTurns: 1);

            // Assert：标量逐一相等（Result 含 IReadOnlyDictionary → 不整体 Assert.Equal）
            Assert.Equal(a.Winner.Value, b.Winner.Value);
            Assert.Equal(a.Margin, b.Margin);
            Assert.Equal(a.AttackerHpRemaining, b.AttackerHpRemaining);
            Assert.Equal(a.DefenderHpRemaining, b.DefenderHpRemaining);
        }
    }
}
