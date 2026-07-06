using System;
using System.Collections.Generic;
using Jianghu.Config;
using Jianghu.Cultivation;
using Jianghu.Model;
using Jianghu.Stats;
using Xunit;

namespace Jianghu.Core.Tests.Cultivation
{
    /// <summary>
    /// balance-007: 控制衰减与冷却（Hybrid Cooldown & Diminishing Returns）— 消 stun-lock。
    ///
    /// 治理 turns≥2 硬控被每回合无限重挂 → 被控方永久锁死（stun-lock 单向锁）。
    /// 机制侧根治零博弈碾压，不碰 PE（balance-003 归一化保持）。
    ///
    /// 关键工程事实（2026-07-06 主控实测钉死，见 story-007）：
    ///  - 对拍纯确定无 RNG → DR 走"持续回合数阶梯降"（duration-based），非成功率（B.2）。
    ///  - tick 时序：回合首查 IsControlled / 回合末 TickDots 递减移除 → turns=N 实际拒止 N-1 回合。
    ///    故 turns=1 现为哑弹（另立项），本 story 的 CD/DR 只作用 turns≥2 真实 stun-lock。
    ///  - CD/DR 状态 duel-local（ResolveR2 内，不入 Clone）→ B.3 off 逐字节天然安全。
    /// </summary>
    public class ControlCooldownTests
    {
        // —— 自包含 fixtures（test-standards：不共享可变状态）——

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

        /// <summary>
        /// 攻方带 turns=2 硬控技能、防方无技能、双方等 PE 的对拍。攻方唯一优势=控制。
        /// 返回 Result 供断言（margin 越大=控制碾压越强；margin 小=博弈窗口恢复）。
        /// </summary>
        static DuelEngine.Result RunControlDuel(int cooldown, int drStep, int ctrlTurns = 2)
        {
            var ctrlSkill = new CombatSkillDef("sk_ctrl", "sk_ctrl", 0,
                new[] { Modules.Control("gouhun", ctrlTurns) }, new Dictionary<string, int>());
            var atkPath = MakePath("atk", new[] { ctrlSkill });
            var defPath = MakePath("def", Array.Empty<CombatSkillDef>());
            var reg = new PathRegistry(new ListPathSource(new[] { atkPath, defPath }));

            var atkCult = CultivationState.NewForPath("atk", atkPath.Resources);
            var defCult = CultivationState.NewForPath("def", defPath.Resources);
            var atk = MakeChar(1, 20, atkCult);
            var def = MakeChar(2, 20, defCult);

            var limits = LimitsConfig.Default with { ControlCooldown = cooldown, ControlDRStep = drStep };
            return DuelEngine.ResolveR2(atk, def, atkPath, defPath, reg, limits,
                null, ctrlSkill, null);
        }

        // ================================================================
        // AC 7.2 抗性递减 — 纯函数（duration-based，B.2 整数）
        // ================================================================

        [Theory]
        [InlineData(2, 0, 1, 2)]   // 首次：无递减 → 满 baseTurns
        [InlineData(2, 1, 1, 1)]   // 第2次：-1 → 1（=0 实际拒止回合）
        [InlineData(2, 2, 1, 0)]   // 第3次：免疫
        [InlineData(2, 5, 1, 0)]   // 超额：地板钳 0（不负）
        [InlineData(3, 1, 1, 2)]   // base3 第2次 → 2
        [InlineData(2, 3, 0, 2)]   // DRStep=0 退化：永不递减
        public void EffectiveControlTurns_DurationBasedLadder(int baseTurns, int priorHits, int drStep, int expected)
        {
            // Act
            int eff = DuelEngine.EffectiveControlTurns(baseTurns, priorHits, drStep);

            // Assert
            Assert.Equal(expected, eff);
        }

        // ================================================================
        // AC 7.1 硬冷却 — 隔离验证（CD=2/DR=0 vs 0/0）
        // ================================================================

        [Fact]
        public void HardCooldown_ReducesAttackerDominance_AndDefenderPunishesMore()
        {
            // Arrange/Act：无 CD（全程锁）vs CD=2（隔回合才可再挂）
            var noCd = RunControlDuel(cooldown: 0, drStep: 0);
            var withCd = RunControlDuel(cooldown: 2, drStep: 0);

            // Assert：冷却给被控方博弈窗口 → 攻方碾压幅度降、且被控方还手使攻方 HP 更低
            Assert.True(withCd.Margin < noCd.Margin,
                $"CD 应降碾压 margin：withCd={withCd.Margin} 应 < noCd={noCd.Margin}");
            Assert.True(withCd.AttackerHpRemaining < noCd.AttackerHpRemaining,
                $"CD 给防方还手窗口 → 攻方受创更多：withCd atkHP={withCd.AttackerHpRemaining} 应 < noCd={noCd.AttackerHpRemaining}");
        }

        // ================================================================
        // AC 7.2 抗性递减 — 隔离验证（CD=0/DR=1 vs 0/0）
        // ================================================================

        [Fact]
        public void DiminishingReturns_ReducesAttackerDominance()
        {
            // Arrange/Act
            var noDr = RunControlDuel(cooldown: 0, drStep: 0);
            var withDr = RunControlDuel(cooldown: 0, drStep: 1);

            // Assert：重复控制 turns 阶梯降→免疫 → 攻方碾压幅度降
            Assert.True(withDr.Margin < noDr.Margin,
                $"DR 应降碾压 margin：withDr={withDr.Margin} 应 < noDr={noDr.Margin}");
        }

        // ================================================================
        // AC 7.3 博弈窗口 — stun-lock 被打破（组合 CD=2/DR=1 vs 0/0）
        // ================================================================

        [Fact]
        public void NoCdNoDr_CharacterizesStunLock_AttackerDominates()
        {
            // Arrange/Act：退化档（0/0）= 现状每回合重挂永久锁死
            var locked = RunControlDuel(cooldown: 0, drStep: 0);

            // Assert：被控方被碾压——攻方大幅 margin（stun-lock 特征基线）
            Assert.False(locked.WasAutoWin);
            Assert.Equal(1, locked.Winner.Value);          // 攻方（控制者）胜
            Assert.True(locked.Margin >= 50, $"stun-lock 基线应现碾压 margin≥50，实为 {locked.Margin}");
        }

        [Fact]
        public void CombinedCdDr_BreaksStunLock_MarginCollapses()
        {
            // Arrange/Act
            var locked = RunControlDuel(cooldown: 0, drStep: 0);   // 现状锁死
            var healthy = RunControlDuel(cooldown: 2, drStep: 1);  // CD+DR 默认档

            // Assert：博弈窗口恢复 → 碾压 margin 至少腰斩（非全程锁）
            Assert.True(healthy.Margin * 2 <= locked.Margin,
                $"CD+DR 应至少腰斩碾压：healthy={healthy.Margin} 应 ≤ locked/2={locked.Margin / 2}");
        }

        // ================================================================
        // AC 7.4 确定性 — 同种子对拍逐字节复现
        // ================================================================

        [Fact]
        public void Deterministic_SameConfig_TwoRunsByteIdentical()
        {
            // Act：同配置跑两次
            var r1 = RunControlDuel(cooldown: 2, drStep: 1);
            var r2 = RunControlDuel(cooldown: 2, drStep: 1);

            // Assert：确定性标量字段逐一相等（Result record 含 IReadOnlyDictionary 成员，
            // record== 对其走引用相等 → 不能整体 Assert.Equal；按可观测标量比对才是确定性的正确验法）。
            Assert.Equal(r1.Winner.Value, r2.Winner.Value);
            Assert.Equal(r1.Loser.Value, r2.Loser.Value);
            Assert.Equal(r1.Margin, r2.Margin);
            Assert.Equal(r1.WasAutoWin, r2.WasAutoWin);
            Assert.Equal(r1.AttackerHpRemaining, r2.AttackerHpRemaining);
            Assert.Equal(r1.DefenderHpRemaining, r2.DefenderHpRemaining);
        }

        // ================================================================
        // AC 7.4/7.5 旋钮校验（数据驱动 + 可行域断言）
        // ================================================================

        [Fact]
        public void Knobs_HaveSafeDefaults()
        {
            // Assert：默认档激活 CC 健康化（CD=2/DR=1，story 用户既定）
            Assert.Equal(2, LimitsConfig.Default.ControlCooldown);
            Assert.Equal(1, LimitsConfig.Default.ControlDRStep);
        }

        [Theory]
        [InlineData(-1, 1)]
        [InlineData(2, -1)]
        public void Knobs_NegativeValues_ThrowOnValidate(int cooldown, int drStep)
        {
            // Arrange
            var bad = LimitsConfig.Default with { ControlCooldown = cooldown, ControlDRStep = drStep };

            // Act/Assert
            Assert.Throws<InvalidOperationException>(() => bad.Validate());
        }

        [Fact]
        public void Knobs_ZeroValues_Legal_DegenerateToNoLimiter()
        {
            // Arrange：0/0 = 退化为无冷却无递减（向后兼容档）
            var degenerate = LimitsConfig.Default with { ControlCooldown = 0, ControlDRStep = 0 };

            // Act/Assert：合法（不抛）
            var ex = Record.Exception(() => degenerate.Validate());
            Assert.Null(ex);
        }
    }
}
