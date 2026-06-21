using System;
using System.Collections.Generic;
using Jianghu.Cultivation;
using Xunit;

namespace Jianghu.Core.Tests.Cultivation
{
    /// <summary>
    /// PostMul ModKind + 负向压制 单元测试（fullstruct-005）。
    /// 覆盖：乘法修正（ratio ×10）、钳位 [0,20]、多模块叠乘序、
    /// 压制矩阵（tag 命中/未命中/钳位）、off 模式路径不可达。
    /// 纯整数、确定性、无 RNG。
    /// </summary>
    public class PostMulTests
    {
        static EffectOp PostMulOp(string kind, int ratio) =>
            new EffectOp(EffectOpKind.PostMul, kind, ratio, null);

        static CultivationState MakeState(string pathId, params (string Key, int Val)[] resources)
        {
            var defs = new List<ResourceDef>();
            foreach (var (key, val) in resources)
                defs.Add(new ResourceDef(key, 0, 1000, val));
            return CultivationState.NewForPath(pathId, defs);
        }

        static CombatContext MakeCtx(
            string[]? attackerTags = null,
            string[]? defenderTags = null)
        {
            var atk = MakeState("atk_path");
            var def = MakeState("def_path");
            var atkPath = TestPaths.WithTags(attackerTags ?? Array.Empty<string>());
            var defPath = TestPaths.WithTags(defenderTags ?? Array.Empty<string>());
            return new CombatContext(atk, atkPath, def, defPath);
        }

        // ================================================================
        // AC 5.3: PostMul 乘法修正（ratio ×10）
        // ================================================================

        [Fact]
        public void PostMul_ratio10_unchanged()
        {
            // ratio=10 = ×1.0 → dmg unchanged
            var ctx = MakeCtx();
            int result = ModuleResolver.ApplyOnUse(100, PostMulOp("LawSuppress", 10), ctx);
            Assert.Equal(100, result);
        }

        [Fact]
        public void PostMul_ratio15_increases_by_50_percent()
        {
            // ratio=15 = ×1.5 → dmg = 100 * 15 / 10 = 150
            var ctx = MakeCtx();
            int result = ModuleResolver.ApplyOnUse(100, PostMulOp("Transform", 15), ctx);
            Assert.Equal(150, result);
        }

        [Fact]
        public void PostMul_ratio5_halves()
        {
            // ratio=5 = ×0.5 → dmg = 100 * 5 / 10 = 50
            var ctx = MakeCtx();
            int result = ModuleResolver.ApplyOnUse(200, PostMulOp("Literati", 5), ctx);
            Assert.Equal(100, result);
        }

        [Fact]
        public void PostMul_stack_multiplicative_order_correct()
        {
            // Two PostMul modules applied in sequence: ×1.5 then ×0.5 = ×0.75
            // 100 * 15/10 = 150; 150 * 5/10 = 75
            var ctx = MakeCtx();
            int after1 = ModuleResolver.ApplyOnUse(100, PostMulOp("HeavenSuppress", 15), ctx);
            Assert.Equal(150, after1);
            int after2 = ModuleResolver.ApplyOnUse(after1, PostMulOp("LawSuppress", 5), ctx);
            Assert.Equal(75, after2);
        }

        // ================================================================
        // AC 5.4: 钳位 [0,20] — 不过压、不反转
        // ================================================================

        [Fact]
        public void PostMul_negative_ratio_clamped_to_zero()
        {
            // ratio=-5 → clamp to 0 → dmg = 100 * 0 / 10 = 0 (no negative dmg)
            var ctx = MakeCtx();
            int result = ModuleResolver.ApplyOnUse(100, PostMulOp("LawSuppress", -5), ctx);
            Assert.Equal(0, result);
        }

        [Fact]
        public void PostMul_exceeds_max_clamped_to_20()
        {
            // ratio=25 → clamp to 20 → dmg = 100 * 20 / 10 = 200 (×2.0 max)
            var ctx = MakeCtx();
            int result = ModuleResolver.ApplyOnUse(100, PostMulOp("HeavenSuppress", 25), ctx);
            Assert.Equal(200, result);
        }

        [Fact]
        public void PostMul_zero_ratio_nullifies_damage()
        {
            // ratio=0 → dmg = 0
            var ctx = MakeCtx();
            int result = ModuleResolver.ApplyOnUse(100, PostMulOp("Literati", 0), ctx);
            Assert.Equal(0, result);
        }

        // ================================================================
        // AC 5.2: 负向压制 — tag 命中与未命中
        // ================================================================

        [Fact]
        public void Suppression_yin_vs_yang_reduces_ratio()
        {
            // 阴→阳 压制: ratio=8 (×0.8)
            int ratio = SuppressionMatrix.GetSuppressionRatio(
                new[] { "yin" }, new[] { "yang" });
            Assert.Equal(8, ratio);
        }

        [Fact]
        public void Suppression_mo_vs_fo_reduces_ratio()
        {
            // 魔→佛 压制: ratio=7 (×0.7)
            int ratio = SuppressionMatrix.GetSuppressionRatio(
                new[] { "mo" }, new[] { "fo" });
            Assert.Equal(7, ratio);
        }

        [Fact]
        public void Suppression_no_match_returns_neutral()
        {
            // No matching suppression pair → NeutralRatio=10 (×1.0)
            int ratio = SuppressionMatrix.GetSuppressionRatio(
                new[] { "sword" }, new[] { "spear" });
            Assert.Equal(SuppressionMatrix.NeutralRatio, ratio);
        }

        [Fact]
        public void Suppression_reverse_direction_no_match()
        {
            // 阳→阴 has no rule → neutral
            int ratio = SuppressionMatrix.GetSuppressionRatio(
                new[] { "yang" }, new[] { "yin" });
            Assert.Equal(SuppressionMatrix.NeutralRatio, ratio);
        }

        [Fact]
        public void Suppression_empty_tags_returns_neutral()
        {
            // Both sides have no tags → neutral
            int ratio = SuppressionMatrix.GetSuppressionRatio(
                Array.Empty<string>(), Array.Empty<string>());
            Assert.Equal(SuppressionMatrix.NeutralRatio, ratio);
        }

        // ================================================================
        // OnDefend PostMul — 防方削减来袭伤害
        // ================================================================

        [Fact]
        public void PostMul_onDefend_reduces_incoming_damage()
        {
            // Defender PostMul ratio=5 → incoming dmg halved
            var ctx = MakeCtx();
            int result = ModuleResolver.ApplyOnDefend(200,
                PostMulOp("Transform", 5), ctx, Side.Defender, out int reflect);
            Assert.Equal(100, result);
            Assert.Equal(0, reflect); // PostMul does not reflect
        }

        [Fact]
        public void PostMul_onDefend_ratio10_no_change()
        {
            var ctx = MakeCtx();
            int result = ModuleResolver.ApplyOnDefend(200,
                PostMulOp("Literati", 10), ctx, Side.Defender, out int reflect);
            Assert.Equal(200, result);
            Assert.Equal(0, reflect);
        }

        // ================================================================
        // Off mode — PostMul unreachable (B.3)
        // ================================================================

        [Fact]
        public void Cultivation_off_throws_before_PostMul_reachable()
        {
            // DuelEngine.ResolveR2 throws when cultivation is null,
            // so PostMul code path is never reached in off mode.
            var path = TestPaths.WithTags(new[] { "melee" });
            var reg = new PathRegistry(new ListPathSource(new[] { path }));
            var limits = Jianghu.Config.LimitsConfig.Default;

            var a = new Jianghu.Model.Character(
                new Jianghu.Model.CharacterId(1),
                new Jianghu.Model.Persona("n", "t", "s", Jianghu.Model.ArchetypeKind.Martial, null),
                new Jianghu.Stats.StatBlock(new[] { 20, 0, 0, 0 }),
                new Jianghu.Model.NodeId(0),
                new Jianghu.Model.Goal(Jianghu.Model.GoalKind.Advance, 0), 0, 800, 16);
            // a.Cultivation = null (off mode)

            var bCult = CultivationState.NewForPath("mock_test_path", path.Resources);
            var b = new Jianghu.Model.Character(
                new Jianghu.Model.CharacterId(2),
                new Jianghu.Model.Persona("n", "t", "s", Jianghu.Model.ArchetypeKind.Martial, null),
                new Jianghu.Stats.StatBlock(new[] { 20, 0, 0, 0 }),
                new Jianghu.Model.NodeId(0),
                new Jianghu.Model.Goal(Jianghu.Model.GoalKind.Advance, 0), 0, 800, 16);
            b.Cultivation = bCult;

            Assert.Throws<ArgumentException>(() =>
                DuelEngine.ResolveR2(a, b, path, path, reg, limits, null, null, null));
        }

        // ================================================================
        // Determinism — same input → same output
        // ================================================================

        [Fact]
        public void PostMul_deterministic_same_input_same_output()
        {
            var ctx = MakeCtx();
            var op = PostMulOp("HeavenSuppress", 12);

            int r1 = ModuleResolver.ApplyOnUse(100, op, ctx);
            int r2 = ModuleResolver.ApplyOnUse(100, op, ctx);
            Assert.Equal(r1, r2);
        }

        [Fact]
        public void Suppression_deterministic_same_tags_same_ratio()
        {
            int r1 = SuppressionMatrix.GetSuppressionRatio(new[] { "yin" }, new[] { "yang" });
            int r2 = SuppressionMatrix.GetSuppressionRatio(new[] { "yin" }, new[] { "yang" });
            Assert.Equal(r1, r2);
        }

        // ================================================================
        // ModuleResolver.ApplyOnUse default does NOT handle PostMul as no-op
        // — it returns dmg via the explicit PostMul case above.
        // (Tests 1-7 already verify PostMul is handled explicitly.)
        // ================================================================

        [Fact]
        public void Modules_PostMul_factory_creates_correct_EffectOp()
        {
            var op = Modules.PostMul("LawSuppress", 12, "test note");
            Assert.Equal(EffectOpKind.PostMul, op.Kind);
            Assert.Equal("LawSuppress", op.Key);
            Assert.Equal(12, op.Amount);
            Assert.Equal("test note", op.Note);
            Assert.Equal(EffectRarity.Rare, op.Rarity);
            Assert.Equal(EffectTrigger.OnUse, op.Trigger);
        }
    }
}
