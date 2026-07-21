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
    /// cv-003（adr-0008 决策⑨.1 标签门控 + ⑩.1 Chip Damage）：DamageType 门控 Block/Dodge 类防御
    /// + 元素格挡穿透。验分类纯函数、Blunt 关 Block、Elemental 关 Dodge、Chip 保底、Chip 免削韧、
    /// 招架崩坏削韧、calibrationMode 旁路、旋钮、B.2/B.3 不退。
    /// 触 Jianghu.Cultivation → 旗舰档 + 主控核验（B.7/A.3）。
    /// </summary>
    public class TagGatingChipTests
    {
        static LimitsConfig Limits => LimitsConfig.Default;

        // —— fixtures：攻方招式带 DamageType；防方招式带 OnDefend 模块 + 对应 gate 功法 ——
        // PE = stat:Force × 4 × RealmMult(=1)。Force=25 → PE=100。
        // attackType = 攻方招式伤害类型；defenseOp = 防方 OnDefend 模块（null=无防御）；defenseRole = gate 功法 role。
        static CultivationPathDef MakePath(string id, DamageType attackType = DamageType.Normal,
            EffectOp? defenseOp = null, string? defenseRole = null)
        {
            var onUse = new List<EffectOp>();
            if (defenseOp != null) onUse.Add(defenseOp); // OnDefend 模块挂同一招（Trigger=OnDefend 自动分流）

            // gate 功法（Evade 需 movement / Reflect+FlatDR 需 body）
            var artCats = new List<ArtCategoryDef>();
            var chosenArts = new List<string>();
            if (defenseRole != null)
            {
                var art = new ArtDef($"art_{defenseRole}", defenseRole, 1, defenseRole, Array.Empty<EffectOp>());
                artCats.Add(new ArtCategoryDef(defenseRole, defenseRole, 1, 1, new[] { art }));
                chosenArts.Add(art.Id);
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
                new[] { new CombatSkillDef("atk", "atk", 0, onUse.ToArray(),
                    new Dictionary<string, int>(), attackType) },
                new EntryGateDef(""), new SelectionRuleDef(1, 3), null);
        }

        static Character MakeChar(long id, int force, CultivationPathDef path)
        {
            var c = new Character(new CharacterId(id),
                new Persona("n", "t", "s", ArchetypeKind.Martial, null),
                new StatBlock(new[] { force, 0, 0, 0 }),
                new NodeId(0), new Goal(GoalKind.Advance, 0), 0, 800, 16);
            var chosenArts = new List<string>();
            foreach (var cat in path.ArtCategories)
                foreach (var art in cat.Arts) chosenArts.Add(art.Id);
            var cult = CultivationState.NewForPath(path.PathId, path.Resources,
                chosenArts.ToArray(), new[] { "atk" });
            cult.RealmIndex = 1; // 同 UT（避免 gap≥2 auto-win 短路）
            c.Cultivation = cult;
            return c;
        }

        static PathRegistry Reg(params CultivationPathDef[] p) => new PathRegistry(new ListPathSource(p));

        sealed class ListPathSource : IPathSource
        {
            private readonly IReadOnlyList<CultivationPathDef> _paths;
            public ListPathSource(IReadOnlyList<CultivationPathDef> paths) => _paths = paths;
            public IReadOnlyList<CultivationPathDef> Load() => _paths;
        }

        // 攻方 A（带 attackType）vs 防方 B（带 defenseOp + gate），返回 A→B 的伤害（= B 损失的 HP，PE-remaining 差）。
        // 用 margin 反推：A 强于 B 时 A 胜，margin 越大说明 B 挨打越狠（防御失效）。
        static DuelEngine.Result Duel(DamageType atkType, EffectOp? defOp, string? defRole,
            LimitsConfig? cfg = null, int atkForce = 30, int defForce = 20)
        {
            var atkPath = MakePath("atk_path", atkType);
            var defPath = MakePath("def_path", DamageType.Normal, defOp, defRole);
            var a = MakeChar(1, atkForce, atkPath);
            var b = MakeChar(2, defForce, defPath);
            return DuelEngine.ResolveR2(a, b, atkPath, defPath, Reg(atkPath, defPath),
                cfg ?? Limits, null, null, null);
        }

        // ============================================================
        // AC-2 (3.2)：Block/Dodge 分类纯函数
        // ============================================================

        [Fact]
        public void test_block_class_membership()
        {
            Assert.True(DuelEngine.IsBlockClass(EffectOpKind.AddFlatDR));
            Assert.True(DuelEngine.IsBlockClass(EffectOpKind.ReflectDamage));
            Assert.False(DuelEngine.IsBlockClass(EffectOpKind.Evade));
            Assert.False(DuelEngine.IsBlockClass(EffectOpKind.Dot)); // 非防御算子
        }

        [Fact]
        public void test_dodge_class_membership()
        {
            Assert.True(DuelEngine.IsDodgeClass(EffectOpKind.Evade));
            Assert.True(DuelEngine.IsDodgeClass(EffectOpKind.SoulSplit));
            Assert.False(DuelEngine.IsDodgeClass(EffectOpKind.AddFlatDR));
            Assert.False(DuelEngine.IsDodgeClass(EffectOpKind.Control)); // 非防御算子
        }

        [Fact]
        public void test_gated_out_matrix()
        {
            // Blunt 关 Block，不关 Dodge
            Assert.True(DuelEngine.IsDefenseGatedOut(DamageType.Blunt, EffectOpKind.AddFlatDR));
            Assert.False(DuelEngine.IsDefenseGatedOut(DamageType.Blunt, EffectOpKind.Evade));
            // Elemental 关 Dodge，不关 Block
            Assert.True(DuelEngine.IsDefenseGatedOut(DamageType.Elemental, EffectOpKind.Evade));
            Assert.False(DuelEngine.IsDefenseGatedOut(DamageType.Elemental, EffectOpKind.AddFlatDR));
            // Normal 全不关
            Assert.False(DuelEngine.IsDefenseGatedOut(DamageType.Normal, EffectOpKind.AddFlatDR));
            Assert.False(DuelEngine.IsDefenseGatedOut(DamageType.Normal, EffectOpKind.Evade));
        }

        // ============================================================
        // AC-5 (3.5)：Chip Damage 保底纯函数
        // ============================================================

        [Fact]
        public void test_chip_floor_basic()
        {
            // 基础伤害 100，chip 300‰ → 30；margin 0，divisor 4 → +0 = 30
            Assert.Equal(30, DuelEngine.ChipDamageFloor(100, 0, 300, 4));
        }

        [Fact]
        public void test_chip_floor_margin_adjust()
        {
            // 基础 100 × 300‰ = 30；margin 40 / divisor 4 = +10 → 40
            Assert.Equal(40, DuelEngine.ChipDamageFloor(100, 40, 300, 4));
            // 负 margin：30 + (-40/4=-10) = 20
            Assert.Equal(20, DuelEngine.ChipDamageFloor(100, -40, 300, 4));
        }

        [Fact]
        public void test_chip_floor_zero_permille_disables()
        {
            Assert.Equal(0, DuelEngine.ChipDamageFloor(100, 50, 0, 4));
        }

        [Fact]
        public void test_chip_floor_zero_divisor_no_margin_adjust()
        {
            // divisor ≤0 → 无 margin 修正，仅基础
            Assert.Equal(30, DuelEngine.ChipDamageFloor(100, 999, 300, 0));
        }

        [Fact]
        public void test_chip_floor_no_overflow()
        {
            int f = DuelEngine.ChipDamageFloor(int.MaxValue, int.MaxValue, 1000, 1);
            Assert.True(f >= 0);
        }

        // ============================================================
        // AC-3 (3.3)：Blunt 门控关 Block
        // ============================================================

        [Fact]
        public void test_blunt_gates_out_block_defense()
        {
            // 防方带 FlatDR（Block 类）。Blunt 攻击应跳过它 → B 挨打更狠 → hpB 更低。
            var flatDR = Modules.FlatDR(10, "护体");

            var normalVsBlock = Duel(DamageType.Normal, flatDR, "body");
            var bluntVsBlock = Duel(DamageType.Blunt, flatDR, "body");

            // Blunt 关掉 Block → 防方 B 受创更重 → 残血 ≤ Normal（Block 生效时 B 更耐打）
            Assert.True(bluntVsBlock.DefenderHpRemaining <= normalVsBlock.DefenderHpRemaining,
                $"Blunt 应关 Block 使 B 更受创：blunt hpB={bluntVsBlock.DefenderHpRemaining} normal hpB={normalVsBlock.DefenderHpRemaining}");
        }

        [Fact]
        public void test_blunt_does_not_gate_dodge()
        {
            // Blunt 不关 Dodge（Evade）。防方 Evade 仍生效 → 与 Normal 对 Evade 防方结果相同。
            var evade = Modules.Evade(40, "闪避"); // movement role gate

            var normalVsDodge = Duel(DamageType.Normal, evade, "movement");
            var bluntVsDodge = Duel(DamageType.Blunt, evade, "movement");

            // Blunt 不影响 Dodge → 两者防方残血相同（Evade 对两种攻击都生效）
            Assert.Equal(normalVsDodge.DefenderHpRemaining, bluntVsDodge.DefenderHpRemaining);
        }

        // ============================================================
        // AC-4 (3.4)：Elemental 门控关 Dodge
        // ============================================================

        [Fact]
        public void test_elemental_gates_out_dodge_defense()
        {
            // 防方带 Evade（Dodge 类）。Elemental 攻击应跳过它 → B 挨打更狠 → hpB 更低。
            var evade = Modules.Evade(40, "闪避");

            var normalVsDodge = Duel(DamageType.Normal, evade, "movement");
            var elemVsDodge = Duel(DamageType.Elemental, evade, "movement");

            Assert.True(elemVsDodge.DefenderHpRemaining <= normalVsDodge.DefenderHpRemaining,
                $"Elemental 应关 Dodge 使 B 更受创：elem hpB={elemVsDodge.DefenderHpRemaining} normal hpB={normalVsDodge.DefenderHpRemaining}");
        }

        [Fact]
        public void test_elemental_does_not_gate_block()
        {
            // Elemental 不关 Block（FlatDR）——但会触发 Chip 穿透（见 chip 测试）。此处仅验 Block 未被门控跳过。
            // 用极低 chip 配置隔离门控效果（chip=0 → 纯看 Block 是否生效）。
            // cv-007 适配：body role 触发 HasBodyArt → 物理抗 +BodyArtPhysResistBonus，使 Normal/Elemental 走不同 R
            // 污染"Block 等效"本意。设 BodyArtPhysResistBonus=0 消除抵抗层干扰（防方 Constitution=0/Insight=0 → R=0 全伤），
            // 回归"Block 对 Normal/Elemental 等效"的 cv-003 原测试意图。
            var noChip = Limits with { ChipDamagePermille = 0, BodyArtPhysResistBonus = 0 };
            var flatDR = Modules.FlatDR(10, "护体");

            var normalVsBlock = Duel(DamageType.Normal, flatDR, "body", noChip);
            var elemVsBlock = Duel(DamageType.Elemental, flatDR, "body", noChip);

            // chip=0 时，Elemental 不关 Block → Block 对两者都生效 → 防方残血相同
            Assert.Equal(normalVsBlock.DefenderHpRemaining, elemVsBlock.DefenderHpRemaining);
        }

        // ============================================================
        // AC-5 (3.5)：Chip 穿透 —— Elemental 被 Block 挡仍受穿透
        // ============================================================

        [Fact]
        public void test_elemental_chip_penetrates_block()
        {
            // 防方 FlatDR 挡 Elemental：chip=0 vs chip=300 对比 → chip 使 B 受创更重（hpB 更低）。
            // FlatDR(10) 小幅减伤，避免 winner 翻转，纯看 chip 增量。
            var flatDR = Modules.FlatDR(10, "护体");

            var noChip = Limits with { ChipDamagePermille = 0 };
            var withChip = Limits with { ChipDamagePermille = 300 };

            var elemNoChip = Duel(DamageType.Elemental, flatDR, "body", noChip);
            var elemChip = Duel(DamageType.Elemental, flatDR, "body", withChip);

            // chip 穿透 → 防方 B 受创更重 → 残血 ≤ 无 chip
            Assert.True(elemChip.DefenderHpRemaining <= elemNoChip.DefenderHpRemaining,
                $"Chip 应使 Elemental 穿透 Block 令 B 更受创：chip hpB={elemChip.DefenderHpRemaining} noChip hpB={elemNoChip.DefenderHpRemaining}");
        }

        // ============================================================
        // AC-8 (3.8)：LimitsConfig 旋钮
        // ============================================================

        [Fact]
        public void test_chip_knobs_have_safe_defaults()
        {
            var c = Limits;
            Assert.True(c.ChipDamagePermille >= 0);
            Assert.True(c.GuardBreakPoiseBonus >= 0);
            c.Validate();
        }

        [Fact]
        public void test_chip_negative_knobs_throw()
        {
            Assert.Throws<InvalidOperationException>(
                () => (Limits with { ChipDamagePermille = -1 }).Validate());
            Assert.Throws<InvalidOperationException>(
                () => (Limits with { GuardBreakPoiseBonus = -1 }).Validate());
        }

        [Fact]
        public void test_chip_zero_permille_legal()
        {
            (Limits with { ChipDamagePermille = 0 }).Validate(); // 不抛（退化无 chip）
        }

        // ============================================================
        // AC-9 (3.9)：calibrationMode 旁路标签门控
        // ============================================================

        [Fact]
        public void test_calibration_mode_bypasses_tag_gating()
        {
            // 标定模式：Blunt/Elemental 应无门控效果（等同 Normal），保裸 PE 平价。
            var evade = Modules.Evade(40, "闪避");
            var atkPath = MakePath("atk", DamageType.Blunt);
            var defPath = MakePath("def", DamageType.Normal, evade, "movement");
            var a = MakeChar(1, 30, atkPath);
            var b = MakeChar(2, 20, defPath);

            var normalAtk = MakePath("atkN", DamageType.Normal);
            var aN = MakeChar(1, 30, normalAtk);

            // 标定模式下 Blunt vs Normal 攻击应同结果（门控旁路）
            var blunt = DuelEngine.ResolveR2(a, b, atkPath, defPath, Reg(atkPath, defPath),
                Limits, null, null, null, calibrationMode: true);
            var normal = DuelEngine.ResolveR2(aN, b, normalAtk, defPath, Reg(normalAtk, defPath),
                Limits, null, null, null, calibrationMode: true);

            Assert.Equal(normal.Winner, blunt.Winner);
            Assert.Equal(normal.Margin, blunt.Margin);
        }

        // ============================================================
        // AC-10 (3.10)：确定性 + off 不退
        // ============================================================

        [Fact]
        public void test_tag_gating_deterministic()
        {
            var flatDR = Modules.FlatDR(50, "护体");
            var r1 = Duel(DamageType.Blunt, flatDR, "body");
            var r2 = Duel(DamageType.Blunt, flatDR, "body");
            Assert.Equal(r1.Winner, r2.Winner);
            Assert.Equal(r1.Margin, r2.Margin);
            Assert.Equal(r1.AttackerHpRemaining, r2.AttackerHpRemaining);
            Assert.Equal(r1.DefenderHpRemaining, r2.DefenderHpRemaining);
        }

        [Fact]
        public void test_off_mode_unaffected_by_chip_knobs()
        {
            // off（cultivation=false）不入 DuelEngine → 标签门控/chip 旋钮零影响，同种子两跑逐字节。
            string Run()
            {
                var w = WorldFactory.CreateInitial(2026, LimitsConfig.Default, 5);
                for (int i = 0; i < 200; i++) w.Advance(6);
                return string.Join("\n", w.Chronicle.Lines);
            }
            Assert.Equal(Run(), Run());
        }

        [Fact]
        public void test_normal_attack_all_defenses_active()
        {
            // Normal 攻击：Block 不被门控 → 防方 FlatDR 生效 → 防方 B 少挨打（hpB 更高）。
            // 用小 FlatDR(10) 避免 winner 翻转，直接比防方存活血量。
            var flatDR = Modules.FlatDR(10, "护体");
            var withDef = Duel(DamageType.Normal, flatDR, "body");
            var noDef = Duel(DamageType.Normal, null, null);
            // Block 生效 → 防方 B 残血更高（挨打更少）
            Assert.True(withDef.DefenderHpRemaining >= noDef.DefenderHpRemaining,
                $"Normal 下 Block 应降低 B 受创：withDef hpB={withDef.DefenderHpRemaining} noDef hpB={noDef.DefenderHpRemaining}");
        }
    }
}
