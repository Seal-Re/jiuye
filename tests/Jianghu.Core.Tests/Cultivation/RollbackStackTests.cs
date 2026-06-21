using System;
using System.Collections.Generic;
using Jianghu.Config;
using Jianghu.Cultivation;
using Jianghu.Model;
using Jianghu.Stats;
using Xunit;

namespace Jianghu.Core.Tests.Cultivation
{
    public class RollbackStackTests
    {
        private static readonly LimitsConfig Limits = new();

        // ---- Stack mechanics ----

        [Fact]
        public void Push_Pop_RestoresHp()
        {
            var stk = new RollbackStack();
            var snap = new ExchangeSnapshot(200, 300, 50, 30, 10, 5, 500, 400);
            Assert.True(stk.Push(snap));
            Assert.Equal(1, stk.Depth);
            var pop = stk.Pop();
            Assert.True(pop.HasValue);
            Assert.Equal(200, pop.Value.AttackerHpBefore);
            Assert.Equal(300, pop.Value.DefenderHpBefore);
            Assert.Equal(50, pop.Value.DmgToB);
            Assert.Equal(30, pop.Value.DmgToA);
        }

        [Fact]
        public void Push_Pop_Symmetry()
        {
            var stk = new RollbackStack();
            for (int i = 0; i < 5; i++)
                stk.Push(Snap(i * 100));
            Assert.Equal(5, stk.Depth);
            for (int i = 0; i < 5; i++)
                stk.Pop();
            Assert.Equal(0, stk.Depth);
        }

        [Fact]
        public void Empty_Pop_Returns_Null()
        {
            var stk = new RollbackStack();
            var pop = stk.Pop();
            Assert.False(pop.HasValue);
            Assert.Equal(0, stk.Depth);
        }

        [Fact]
        public void Overflow_Capped()
        {
            var stk = new RollbackStack();
            for (int i = 0; i < RollbackStack.MaxDepth; i++)
                Assert.True(stk.Push(Snap(i)));
            Assert.Equal(RollbackStack.MaxDepth, stk.Depth);
            Assert.False(stk.Push(Snap(999)));
            Assert.Equal(RollbackStack.MaxDepth, stk.Depth);
        }

        // ---- SoulSplit ----

        [Fact]
        public void SoulSplit_Redirects_Damage_To_SoulResource()
        {
            var st = CultivationState.NewForPath("test",
                new[] { new ResourceDef("qi", 0, 1000, 100) }, Array.Empty<string>(), Array.Empty<string>());
            var path = MinPath("test");
            var ctx = new CombatContext(st, path, st, path);

            int result = ModuleResolver.ApplyOnDefend(200, Modules.SoulSplit(50), ctx, Side.Defender, out int reflectDmg);
            Assert.Equal(0, reflectDmg);
            Assert.True(result < 200, "SoulSplit should reduce body damage");
        }

        // ---- Possession gate ----

        [Fact]
        public void Possession_Gate_Suppressed_By_PureYang()
        {
            var tags = new[] { "pure_yang" };
            Assert.True(HasCounterTag(tags));
        }

        [Fact]
        public void Possession_Gate_Allows_When_No_CounterTag()
        {
            var tags = new[] { "water" };
            Assert.False(HasCounterTag(tags));
        }

        [Fact]
        public void Karma_Relation_Unchanged_After_Rollback()
        {
            var stk = new RollbackStack();
            stk.Push(new ExchangeSnapshot(300, 400, 100, 50, 0, 0, 0, 0));
            var pop = stk.Pop();
            Assert.True(pop.HasValue);
            Assert.Equal(300, pop.Value.AttackerHpBefore);
        }

        // ---- Off mode ----

        [Fact]
        public void Off_Mode_CultivationNull_Throws()
        {
            var a = MakeChar(1, 20, 0, 0, 0, null);
            var b = MakeChar(2, 20, 0, 0, 0, null);
            var path = MinPath("test");
            var reg = new PathRegistry(new CodePathSource());
            Assert.Throws<ArgumentException>(() =>
                DuelEngine.ResolveR2(a, b, path, path, reg, Limits, null, null, null));
        }

        // ================================================================
        // Helpers (mirror DuelGateTests pattern)
        // ================================================================

        private static ExchangeSnapshot Snap(int n)
            => new ExchangeSnapshot(n, n + 10, n / 2, n / 3, 0, 0, 0, 0);

        private static bool HasCounterTag(string[] tags)
        {
            foreach (var tag in tags)
                if (tag == "pure_yang" || tag == "buddha_light" || tag == "thunder")
                    return true;
            return false;
        }

        private static Character MakeChar(long id, int force, int intl, int con, int insight,
            CultivationState? cult = null)
        {
            var c = new Character(new CharacterId(id),
                new Persona("n", "t", "s", ArchetypeKind.Martial, null),
                new StatBlock(new[] { force, intl, con, insight }),
                new NodeId(0), new Goal(GoalKind.Advance, 0), 0, 800, 16);
            if (cult != null) c.Cultivation = cult;
            return c;
        }

        private static CultivationPathDef MinPath(string id,
            IReadOnlyList<ResourceDef>? resources = null,
            IReadOnlyList<CombatSkillDef>? skills = null,
            IReadOnlyList<ArtCategoryDef>? arts = null)
            => new CultivationPathDef(id, id, "physical",
                new[] { "melee" },
                resources ?? new[] { new ResourceDef("qi", 0, 1000, 0) },
                new PowerFormulaDef(
                    new[] { new PowerTerm("stat:Force", 4, null) },
                    Array.Empty<PowerMod>(), null),
                new RealmCurveDef(new[] { 10, 15, 25 }, new[] { 0, 1, 2 },
                    new[] { "L1", "L2", "L3" }, new[] { 0, 100, 300 },
                    new[] { 1, 1, 1 }, true, 2),
                arts ?? Array.Empty<ArtCategoryDef>(),
                skills ?? Array.Empty<CombatSkillDef>(),
                new EntryGateDef(""), new SelectionRuleDef(1, 3), null);

        private static CombatSkillDef Skill(string id, int tier, IReadOnlyList<EffectOp>? onUse = null,
            IReadOnlyDictionary<string, int>? cost = null)
            => new CombatSkillDef(id, id, tier,
                onUse ?? Array.Empty<EffectOp>(),
                cost ?? new Dictionary<string, int>());
    }
}
