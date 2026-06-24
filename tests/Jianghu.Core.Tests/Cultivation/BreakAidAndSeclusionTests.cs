using System;
using Jianghu.Cultivation;
using Xunit;

namespace Jianghu.Core.Tests.Cultivation
{
    /// <summary>
    /// Story-007 (BreakAid) + Story-008 (Epiphany) + Story-010 (Seclusion formulas) tests.
    /// </summary>
    public class BreakAidAndSeclusionTests
    {
        // ================================================================
        // Story-007: BreakAid four methods
        // ================================================================

        [Fact]
        public void BreakAid_FourMethods_AllDefined()
        {
            Assert.NotNull(BreakAidRegistry.Seclusion);
            Assert.NotNull(BreakAidRegistry.Epiphany);
            Assert.NotNull(BreakAidRegistry.Resource);
            Assert.NotNull(BreakAidRegistry.Guardian);
        }

        [Fact]
        public void BreakAid_Seclusion_Bonus_IsStreakBased()
        {
            Assert.Equal(0, BreakAidRegistry.SeclusionBonus(0));
            Assert.Equal(4, BreakAidRegistry.SeclusionBonus(1));
            Assert.Equal(8, BreakAidRegistry.SeclusionBonus(2));
            Assert.Equal(30, BreakAidRegistry.SeclusionBonus(10)); // cap at 30
            Assert.Equal(30, BreakAidRegistry.SeclusionBonus(20)); // still 30
        }

        [Fact]
        public void BreakAid_Resource_HasCost()
        {
            var def = BreakAidRegistry.Resource;
            Assert.NotNull(def.ResourceCost);
            Assert.True(def.ResourceCost.ContainsKey("灵石"));
            Assert.Equal(50, def.ResourceCost["灵石"]);
        }

        [Fact]
        public void BreakAid_Guardian_ReducesInnerDemon()
        {
            Assert.Equal(-5, BreakAidRegistry.Guardian.InnerDemonRisk);
        }

        [Fact]
        public void BreakAid_Get_ThrowsOnInvalid()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => BreakAidRegistry.Get((BreakAidMethod)99));
        }

        // ================================================================
        // Story-008: Epiphany (verified via DailyMode Comprehend)
        // ================================================================

        [Fact]
        public void Epiphany_Threshold_Formula()
        {
            Assert.Equal(0, DailyModeApplier.EpiphanyThreshold(17));
            Assert.Equal(0, DailyModeApplier.EpiphanyThreshold(18));
            Assert.Equal(2, DailyModeApplier.EpiphanyThreshold(20));
            Assert.Equal(7, DailyModeApplier.EpiphanyThreshold(25));
        }

        [Fact]
        public void ComprehendMode_Exists_And_IsDefined()
        {
            Assert.Equal(2, (int)DailyMode.Comprehend);
        }

        // ================================================================
        // Story-010: Seclusion formulas
        // ================================================================

        [Fact]
        public void Duration_BaseFormula()
        {
            // WorkUnits=5, Insight=20 → 60 + 8*5 - min(20/2, 30) = 60 + 40 - 10 = 90
            Assert.Equal(90L, SeclusionFormulas.Duration(5, 20));
        }

        [Fact]
        public void Duration_HighWorkUnits()
        {
            // WorkUnits=20, Insight=30 → 60 + 160 - 15 = 205
            Assert.Equal(205L, SeclusionFormulas.Duration(20, 30));
        }

        [Fact]
        public void Duration_ZeroWorkUnits()
        {
            // WorkUnits=0, Insight=20 → 60 + 0 - 10 = 50
            Assert.Equal(50L, SeclusionFormulas.Duration(0, 20));
        }

        [Fact]
        public void Duration_InsightSpeedup_CappedAt30()
        {
            // WorkUnits=5, Insight=80 → speedup = min(80/2, 30) = 30
            // 60 + 40 - 30 = 70
            Assert.Equal(70L, SeclusionFormulas.Duration(5, 80));
        }

        [Fact]
        public void AgeCost_FirstStreak()
        {
            long duration = SeclusionFormulas.Duration(5, 20); // 90 ticks
            long cost = SeclusionFormulas.AgeCost(duration, streak: 0);
            // intervals = ceil(90/4) = 23, foldLife = 100+0*20 = 100
            // 23 * 4 * 100 / 100 = 92
            Assert.Equal(92L, cost);
        }

        [Fact]
        public void AgeCost_FifthStreak()
        {
            long duration = SeclusionFormulas.Duration(5, 20); // 90
            long cost = SeclusionFormulas.AgeCost(duration, streak: 4);
            // intervals = 23, foldLife = 100+4*20 = 180
            // 23 * 4 * 180 / 100 = 165
            Assert.Equal(165L, cost);
        }

        [Fact]
        public void ProgressGain_Formula()
        {
            // WorkUnits=5, streak=2 → 5 + min(2*4,30) = 5 + 8 = 13
            Assert.Equal(13, SeclusionFormulas.ProgressGain(5, 2));
        }

        [Fact]
        public void InnerDemonGain_Formula()
        {
            Assert.Equal(3, SeclusionFormulas.InnerDemonGain(0));   // 3*(0+1)
            Assert.Equal(9, SeclusionFormulas.InnerDemonGain(2));   // 3*(2+1)
        }

        [Fact]
        public void IsHalved_AtStrike3()
        {
            Assert.False(SeclusionFormulas.IsHalved(2));
            Assert.True(SeclusionFormulas.IsHalved(3));
        }

        [Fact]
        public void IsLockedOut_AtStrike5()
        {
            Assert.False(SeclusionFormulas.IsLockedOut(4));
            Assert.True(SeclusionFormulas.IsLockedOut(5));
        }
    }
}
