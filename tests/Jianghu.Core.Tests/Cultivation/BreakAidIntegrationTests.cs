using System;
using System.Collections.Generic;
using Jianghu.Cultivation;
using Jianghu.Random;
using Xunit;

namespace Jianghu.Core.Tests.Cultivation
{
    /// <summary>
    /// Story-020 (BreakAid→Breakthrough) + Story-011 (streak) + Story-012 (RNG) tests.
    /// </summary>
    public class BreakAidIntegrationTests
    {
        static CultivationState NewSt(int phase = 5, int daoHeart = 50, int streak = 0)
        {
            var resDefs = new List<ResourceDef> {
                new ResourceDef("qi", 0, 1000, 100),
                new ResourceDef("灵石", 0, 1000, 200)
            };
            var st = CultivationState.NewForPath("test", resDefs);
            st.DaoHeart = daoHeart;
            st.Flags["cultPhase"] = phase;
            st.Flags["seclusionStreak"] = streak;
            return st;
        }

        static Pcg32 Rng(ulong seed = 42) => new Pcg32(seed, 0);

        // ================================================================
        // Story-020: BreakAid→Breakthrough
        // ================================================================

        [Fact]
        public void InBottleneck_SelectsBestMethod()
        {
            var st = NewSt(phase: 5, daoHeart: 50, streak: 0);
            var (method, gain) = BreakAidService.ApplyBest(st, 25, true, Rng());
            Assert.True(method >= 0, "Should select a valid method");
        }

        [Fact]
        public void NotInBottleneck_ReturnsZero()
        {
            var st = NewSt(phase: 0, daoHeart: 50); // Mortal, not bottleneck
            var (method, gain) = BreakAidService.ApplyBest(st, 25, true, Rng());
            Assert.Equal(0, gain);
        }

        [Fact]
        public void LowDaoHeart_BlocksGuardian()
        {
            var st = NewSt(phase: 5, daoHeart: 10, streak: 0); // Below Guardian req of 30
            var (method, _) = BreakAidService.ApplyBest(st, 25, false, Rng());
            Assert.NotEqual(BreakAidMethod.Guardian, method);
        }

        [Fact]
        public void NoGuardian_BlocksGuardianMethod()
        {
            var st = NewSt(phase: 5, daoHeart: 50, streak: 0);
            var (method, _) = BreakAidService.ApplyBest(st, 25, hasGuardian: false, Rng());
            Assert.NotEqual(BreakAidMethod.Guardian, method);
        }

        // ================================================================
        // Story-011: Seclusion streak escalation
        // ================================================================

        [Fact]
        public void Streak0_ProgressGain_Full()
        {
            int gain = SeclusionFormulas.ProgressGain(5, 0);
            Assert.Equal(5, gain); // 5 + min(0*4,30) = 5
        }

        [Fact]
        public void Streak4_ProgressGain_WithBonus()
        {
            int gain = SeclusionFormulas.ProgressGain(5, 4);
            Assert.Equal(21, gain); // 5 + min(16,30) = 21
        }

        [Fact]
        public void Streak5_LockedOut()
        {
            Assert.True(SeclusionFormulas.IsLockedOut(5));
        }

        [Fact]
        public void Streak3_Halved()
        {
            Assert.True(SeclusionFormulas.IsHalved(3));
        }

        // ================================================================
        // Story-012: Seclusion RNG self-consistency
        // ================================================================

        [Fact]
        public void SameParams_SameDuration()
        {
            long d1 = SeclusionFormulas.Duration(5, 20);
            long d2 = SeclusionFormulas.Duration(5, 20);
            Assert.Equal(d1, d2);
        }

        [Fact]
        public void SeclusionFormulas_PureInteger()
        {
            // Duration, AgeCost, ProgressGain, InnerDemonGain all return integer types
            Assert.IsType<long>(SeclusionFormulas.Duration(5, 20));
            Assert.IsType<long>(SeclusionFormulas.AgeCost(90, 0));
            Assert.IsType<int>(SeclusionFormulas.ProgressGain(5, 2));
            Assert.IsType<int>(SeclusionFormulas.InnerDemonGain(0));
        }

        [Fact]
        public void Deterministic_Formulas()
        {
            var d1 = SeclusionFormulas.Duration(10, 25);
            var c1 = SeclusionFormulas.AgeCost(d1, 2);
            var g1 = SeclusionFormulas.ProgressGain(10, 2);

            var d2 = SeclusionFormulas.Duration(10, 25);
            var c2 = SeclusionFormulas.AgeCost(d2, 2);
            var g2 = SeclusionFormulas.ProgressGain(10, 2);

            Assert.Equal(d1, d2);
            Assert.Equal(c1, c2);
            Assert.Equal(g1, g2);
        }
    }
}
