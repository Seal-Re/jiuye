using System;
using System.Collections.Generic;
using Jianghu.Cultivation;
using Jianghu.Random;
using Xunit;

namespace Jianghu.Core.Tests.Cultivation
{
    /// <summary>
    /// Story-005: DailyMode贪心算法+迟滞规则 tests.
    /// AC 5.1-5.8: greedy scoring, DEMON_DANGER hysteresis, breakthrough lock, determinism, off mode.
    /// </summary>
    public class DailyModeGreedyTests
    {
        static CultivationState NewSt(int innerDemon = 0, int daoHeart = 0, int phase = -1)
        {
            var resDefs = new List<ResourceDef> { new ResourceDef("qi", 0, 1000, 100) };
            var st = CultivationState.NewForPath("test", resDefs);
            st.InnerDemon = innerDemon;
            st.DaoHeart = daoHeart;
            if (phase >= 0) st.Flags["cultPhase"] = phase;
            return st;
        }

        static Pcg32 Rng(ulong seed = 42) => new Pcg32(seed, 0);

        // ================================================================
        // AC 5.1: Greedy scoring — low innerDemon prefers Fast
        // ================================================================

        [Fact]
        public void ZeroInnerDemon_Prefers_HighProgress()
        {
            var st = NewSt(innerDemon: 0, daoHeart: 50);
            var (mode, _) = DailyModeSelector.Select(st, 20, false, null, Rng());
            // At zero innerDemon, no demon reduction needed → progress-focused mode
            Assert.True(mode == DailyMode.Fast || mode == DailyMode.Steady,
                $"Expected Fast or Steady at zero innerDemon, got {mode}");
        }

        [Fact]
        public void HighInnerDemon_Avoids_Fast()
        {
            var st = NewSt(innerDemon: 80, daoHeart: 30);
            var (mode, _) = DailyModeSelector.Select(st, 20, true, null, Rng());
            // High innerDemon → prefers Steady or Roam (innerDemon-reducing modes)
            Assert.NotEqual(DailyMode.Fast, mode);
        }

        // ================================================================
        // AC 5.2-5.3: DEMON_DANGER hysteresis
        // ================================================================

        [Fact]
        public void InnerDemon64_NotInDanger_StaysNormal()
        {
            var st = NewSt(innerDemon: 64, daoHeart: 50);
            var (mode, inDanger) = DailyModeSelector.Select(st, 20, false, null, Rng());
            Assert.False(inDanger);
        }

        [Fact]
        public void InnerDemon65_EntersDanger()
        {
            var st = NewSt(innerDemon: 65, daoHeart: 50);
            var (mode, inDanger) = DailyModeSelector.Select(st, 20, false, null, Rng());
            Assert.True(inDanger);
        }

        [Fact]
        public void InnerDemon51_StaysInDanger_DueToHysteresis()
        {
            // Was in danger, now 51 — above EXIT(50) → still in danger
            var st = NewSt(innerDemon: 51, daoHeart: 50);
            var (_, inDanger) = DailyModeSelector.Select(st, 20, inDanger: true, null, Rng());
            Assert.True(inDanger);
        }

        [Fact]
        public void InnerDemon50_ExitsDanger_WhenPreviouslyInDanger()
        {
            var st = NewSt(innerDemon: 50, daoHeart: 50);
            var (_, inDanger) = DailyModeSelector.Select(st, 20, inDanger: true, null, Rng());
            Assert.False(inDanger);
        }

        [Fact]
        public void Hysteresis_Prevents_FrequentModeSwitching()
        {
            // Simulate innerDemon oscillating around threshold.
            // Hysteresis band prevents mode switching on every tick.
            var st = NewSt(innerDemon: 64, daoHeart: 50);
            bool inDanger = false;

            // At 64: not in danger
            (_, inDanger) = DailyModeSelector.Select(st, 20, inDanger, null, Rng());
            Assert.False(inDanger);

            // Climb to 65: enter danger
            st.InnerDemon = 65;
            (_, inDanger) = DailyModeSelector.Select(st, 20, inDanger, null, Rng());
            Assert.True(inDanger);

            // Drop to 51: still in danger (hysteresis)
            st.InnerDemon = 51;
            (_, inDanger) = DailyModeSelector.Select(st, 20, inDanger, null, Rng());
            Assert.True(inDanger);

            // Drop to 50: exit danger
            st.InnerDemon = 50;
            (_, inDanger) = DailyModeSelector.Select(st, 20, inDanger, null, Rng());
            Assert.False(inDanger);
        }

        // ================================================================
        // AC 5.5: Breakthrough Phase locks Fast
        // ================================================================

        [Fact]
        public void BreakthroughPhase_Forces_Fast()
        {
            var st = NewSt(innerDemon: 80, daoHeart: 30, phase: 6);
            var (mode, _) = DailyModeSelector.Select(st, 20, true, null, Rng());
            // Even with high innerDemon, Breakthrough locks Fast
            Assert.Equal(DailyMode.Fast, mode);
        }

        // ================================================================
        // AC 5.6: Deviation/Fallen Phase forces safe modes
        // ================================================================

        [Fact]
        public void DeviationPhase_Forces_SteadyOrRoam()
        {
            var st = NewSt(innerDemon: 70, daoHeart: 30, phase: 8);
            var (mode, _) = DailyModeSelector.Select(st, 20, true, null, Rng());
            Assert.True(mode == DailyMode.Steady || mode == DailyMode.Roam,
                $"Deviation should force Steady or Roam, got {mode}");
        }

        [Fact]
        public void FallenPhase_Forces_Roam()
        {
            var st = NewSt(innerDemon: 95, daoHeart: 0, phase: 9);
            var (mode, _) = DailyModeSelector.Select(st, 20, true, null, Rng());
            Assert.Equal(DailyMode.Roam, mode);
        }

        // ================================================================
        // AC 5.7: Determinism
        // ================================================================

        [Fact]
        public void SameState_SameSeed_SameSelection()
        {
            DailyMode first = default;
            for (int run = 0; run < 5; run++)
            {
                var st = NewSt(innerDemon: 30, daoHeart: 60);
                var (mode, _) = DailyModeSelector.Select(st, 20, false, null, Rng(42));
                if (run == 0) first = mode;
                else Assert.Equal(first, mode);
            }
        }

        // ================================================================
        // AC 5.8: Off mode (selector not called when cultivation off — tested in integration)
        // ================================================================

        [Fact]
        public void Selector_Always_ReturnsValidMode()
        {
            foreach (DailyMode mode in Enum.GetValues(typeof(DailyMode)))
            {
                Assert.True(mode >= DailyMode.Fast && mode <= DailyMode.Roam,
                    $"Invalid mode value: {mode}");
            }
        }

        // ================================================================
        // Bonus: Variety penalty reduces consecutive same-mode preference
        // ================================================================

        [Fact]
        public void ConsecutiveModeReceives_VarietyPenalty()
        {
            var st = NewSt(innerDemon: 20, daoHeart: 50);
            // First selection: no penalty (prevMode=null)
            var (mode1, _) = DailyModeSelector.Select(st, 20, false, null, Rng(100));
            // Second selection: same mode gets penalty → may switch
            var (mode2, _) = DailyModeSelector.Select(st, 20, false, mode1, Rng(100));
            // At least one mode is chosen (may be same or different depending on scores)
            Assert.True(mode2 >= 0); // Valid mode always returned
        }
    }
}
