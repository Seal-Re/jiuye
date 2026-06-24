using System;
using System.Collections.Generic;
using Jianghu.Cultivation;
using Jianghu.Model;
using Jianghu.Stats;
using Xunit;

namespace Jianghu.Core.Tests.Cultivation
{
    /// <summary>
    /// Story-009: 闭关DES单点唤醒 tests.
    /// AC 9.1-9.9: Enter/Exit lifecycle, NextActAt, Scheduler non-pop, Disturb, determinism, off mode.
    /// </summary>
    public class SeclusionDESTests
    {
        static CultivationState NewSt(int phase = 0)
        {
            var resDefs = new List<ResourceDef> { new ResourceDef("qi", 0, 1000, 100) };
            var st = CultivationState.NewForPath("test", resDefs);
            if (phase >= 0) st.Flags["cultPhase"] = phase;
            return st;
        }

        static Character MakeChar(long id)
        {
            return new Character(new CharacterId(id),
                new Persona("n", "t", "s", ArchetypeKind.Martial, null),
                new StatBlock(new[] { 20, 10, 10, 20 }),
                new NodeId(0), new Goal(GoalKind.Advance, 0), 0, 800, 16);
        }

        // ================================================================
        // AC 9.1-9.2: Enter seclusion
        // ================================================================

        [Fact]
        public void Enter_SetsFlags_And_NextActAt()
        {
            var st = NewSt();
            var ch = MakeChar(1);
            long now = 100;

            SeclusionState.Enter(st, ch, now, duration: 130, workUnits: 5);

            Assert.True(SeclusionState.IsSecluded(st));
            Assert.Equal(0, st.Flags["seclusionDisturb"]);
            Assert.Equal(5, st.Flags["seclusionWorkUnits"]);
            Assert.Equal(now + 130, ch.NextActAt);
        }

        [Fact]
        public void Enter_Increments_Streak()
        {
            var st = NewSt();
            st.Flags["seclusionStreak"] = 2;
            var ch = MakeChar(1);

            SeclusionState.Enter(st, ch, 100, 130, 5);

            Assert.Equal(3, st.Flags["seclusionStreak"]);
        }

        // ================================================================
        // AC 9.4: Exit seclusion
        // ================================================================

        [Fact]
        public void Exit_ReturnsWorkUnits_And_ClearsFlags()
        {
            var st = NewSt();
            var ch = MakeChar(1);
            SeclusionState.Enter(st, ch, 100, 130, 5);

            int wu = SeclusionState.Exit(st, ch);

            Assert.Equal(5, wu);
            Assert.False(SeclusionState.IsSecluded(st));
            Assert.False(st.Flags.ContainsKey("seclusionDisturb"));
            Assert.False(st.Flags.ContainsKey("seclusionWorkUnits"));
            Assert.False(st.Flags.ContainsKey("seclusionWakeAt"));
        }

        [Fact]
        public void Exit_WithDisturb_HalvesWorkUnits()
        {
            var st = NewSt();
            var ch = MakeChar(1);
            SeclusionState.Enter(st, ch, 100, 130, 10);
            st.Flags["seclusionDisturb"] = 3; // Force exit threshold

            int wu = SeclusionState.Exit(st, ch);

            Assert.Equal(5, wu); // 10/2 = 5
        }

        // ================================================================
        // AC 9.5: Disturb counter
        // ================================================================

        [Fact]
        public void Disturb_IncrementsCounter()
        {
            var st = NewSt();
            var ch = MakeChar(1);
            SeclusionState.Enter(st, ch, 100, 130, 5);

            SeclusionState.Disturb(st);
            Assert.Equal(1, st.Flags["seclusionDisturb"]);

            SeclusionState.Disturb(st);
            Assert.Equal(2, st.Flags["seclusionDisturb"]);
        }

        [Fact]
        public void Disturb_NoOp_WhenNotSecluded()
        {
            var st = NewSt();
            SeclusionState.Disturb(st);
            Assert.False(st.Flags.ContainsKey("seclusionDisturb"));
        }

        // ================================================================
        // AC 9.6: CanSpar — no spar during seclusion
        // ================================================================

        [Fact]
        public void CanSpar_False_WhenSecluded()
        {
            var st = NewSt();
            var ch = MakeChar(1);
            Assert.True(SeclusionState.CanSpar(st)); // Not secluded

            SeclusionState.Enter(st, ch, 100, 130, 5);
            Assert.False(SeclusionState.CanSpar(st));
        }

        // ================================================================
        // AC 9.7: CanEnter — Breakthrough lock
        // ================================================================

        [Fact]
        public void CanEnter_False_DuringBreakthrough()
        {
            var st = NewSt(phase: 6); // Breakthrough
            Assert.False(SeclusionState.CanEnter(st));
        }

        [Fact]
        public void CanEnter_True_InNormalPhase()
        {
            var st = NewSt(phase: 0); // Mortal
            Assert.True(SeclusionState.CanEnter(st));
        }

        [Fact]
        public void CanEnter_False_WhenAlreadySecluded()
        {
            var st = NewSt();
            var ch = MakeChar(1);
            SeclusionState.Enter(st, ch, 100, 130, 5);
            Assert.False(SeclusionState.CanEnter(st));
        }

        // ================================================================
        // AC 9.8: Determinism
        // ================================================================

        [Fact]
        public void SameParams_SameDuration_SameWakeAt()
        {
            var st1 = NewSt();
            var ch1 = MakeChar(1);
            var st2 = NewSt();
            var ch2 = MakeChar(2);

            SeclusionState.Enter(st1, ch1, 100, 130, 5);
            SeclusionState.Enter(st2, ch2, 100, 130, 5);

            Assert.Equal(ch1.NextActAt, ch2.NextActAt);
            Assert.Equal(SeclusionState.Exit(st1, ch1), SeclusionState.Exit(st2, ch2));
        }

        // ================================================================
        // AC 9.9: Lockout after 5 streaks
        // ================================================================

        [Fact]
        public void IsLockedOut_WhenStreak5()
        {
            var st = NewSt();
            st.Flags["seclusionStreak"] = 5;
            Assert.True(SeclusionState.IsLockedOut(st));
        }

        [Fact]
        public void IsLockedOut_False_WhenStreakLessThan5()
        {
            var st = NewSt();
            st.Flags["seclusionStreak"] = 4;
            Assert.False(SeclusionState.IsLockedOut(st));
        }

        // ================================================================
        // Off mode: no flags set
        // ================================================================

        [Fact]
        public void OffMode_NoSeclusionFlags()
        {
            var st = NewSt();
            Assert.False(SeclusionState.IsSecluded(st));
        }
    }
}
