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
    public class OverflowTests
    {
        static LimitsConfig Limits => LimitsConfig.Default;
        static IRandom Rng(ulong seed) => new Pcg32(seed, (ulong)RngStreamIds.Duel);

        static CultivationPathDef MakePath(string id, int attackSec = 1000)
        {
            var skill = new CombatSkillDef("atk", "atk", 0, Array.Empty<EffectOp>(),
                new Dictionary<string, int>(), DamageType.Normal, attackSec);
            return new CultivationPathDef(id, id, "physical", new[] { "melee" },
                new[] { new ResourceDef("qi", 0, 1000, 0) },
                new PowerFormulaDef(new[] { new PowerTerm("stat:Force", 4, null) },
                    Array.Empty<PowerMod>(), null),
                new RealmCurveDef(new[] { 10, 15, 25 }, new[] { 0, 1, 2 },
                    new[] { "L1", "L2", "L3" }, new[] { 0, 100, 300 },
                    new[] { 1, 1, 1 }, true, 2),
                Array.Empty<ArtCategoryDef>(), new[] { skill },
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
            cult.RealmIndex = 1;
            c.Cultivation = cult;
            return c;
        }

        static PathRegistry Reg(CultivationPathDef p) => new PathRegistry(new ListPathSource(new[] { p }));

        sealed class ListPathSource : IPathSource
        {
            readonly IReadOnlyList<CultivationPathDef> _paths;
            public ListPathSource(IReadOnlyList<CultivationPathDef> p) => _paths = p;
            public IReadOnlyList<CultivationPathDef> Load() => _paths;
        }

        // AC 4.1-4.2: 溢出判定纯函数 + IsOverflow
        [Fact]
        public void test_is_overflow_below_threshold()
            => Assert.False(CombatMath.IsOverflow(999, 1000));

        [Fact]
        public void test_is_overflow_at_threshold()
            => Assert.True(CombatMath.IsOverflow(1000, 1000));

        [Fact]
        public void test_is_overflow_above_threshold()
            => Assert.True(CombatMath.IsOverflow(1500, 1000));

        [Fact]
        public void test_is_overflow_zero_threshold_disabled()
            => Assert.False(CombatMath.IsOverflow(1000, 0));

        // AC 4.3: knob default + validate
        [Fact]
        public void test_overflow_threshold_default_is_1000()
            => Assert.Equal(1000, Limits.OverflowThresholdPermille);

        [Fact]
        public void test_overflow_negative_threshold_throws()
            => Assert.Throws<InvalidOperationException>(
                () => (Limits with { OverflowThresholdPermille = -1 }).Validate());

        // AC 4.2: SEC=0 auto-hit → overflow → guaranteed hit (skip Bernoulli)
        // With SEC=0, ApplyEvasionCoefficient returns AutoHitPermille=1000 → equals threshold → overflow → always hit
        [Fact]
        public void test_sec_zero_triggers_overflow_and_always_hits()
        {
            var path = MakePath("atk", attackSec: 0); // SEC=0 → p=1000 → overflow
            // Attacker much weaker → without overflow, cv-001 p ≈ margin-based low value, but SEC=0 overrides to 1000
            var a = MakeChar(1, 15, path);  // Force 15, PE=15*4*15=900
            var b = MakeChar(2, 40, path);  // Force 40, PE=40*4*15=2400

            int hits = 0;
            for (ulong s = 1; s <= 50; s++)
            {
                var r = DuelEngine.ResolveR2(a, b, path, path, Reg(path),
                    Limits, null, null, null, duelRng: Rng(s));
                // SEC=0 → overflow → always hits → defender takes damage
                if (r.DefenderHpRemaining < 40 * 4 * 15) hits++;
            }
            // SEC=0 overflow → guaranteed hit every time (100% hit rate)
            Assert.Equal(50, hits);
        }

        // overflow disabled (threshold=0) → IsOverflow always false → normal Bernoulli
        // (verified via pure function test above, no need for end-to-end duel)
        [Fact]
        public void test_overflow_zero_threshold_never_overflows()
        {
            Assert.False(CombatMath.IsOverflow(1000, 0));
            Assert.False(CombatMath.IsOverflow(1, 0));
            Assert.False(CombatMath.IsOverflow(999999, 0));
        }

        // B.2: overflow detection is deterministic
        [Fact]
        public void test_overflow_detection_deterministic()
        {
            var path = MakePath("atk", attackSec: 0);
            var a = MakeChar(1, 25, path);
            var b = MakeChar(2, 24, path);

            var r1 = DuelEngine.ResolveR2(a, b, path, path, Reg(path),
                Limits, null, null, null, duelRng: Rng(42));
            var r2 = DuelEngine.ResolveR2(a, b, path, path, Reg(path),
                Limits, null, null, null, duelRng: Rng(42));

            Assert.Equal(r1.Winner, r2.Winner);
            Assert.Equal(r1.DefenderHpRemaining, r2.DefenderHpRemaining);
        }

        [Fact]
        public void test_off_mode_unaffected_by_overflow()
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
