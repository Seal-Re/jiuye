using System;
using Jianghu.Cultivation;
using Jianghu.Random;
using Xunit;

namespace Jianghu.Core.Tests.Cultivation
{
    /// <summary>
    /// Story-004: 4路DailyMode枚举+整数倍率表 tests.
    /// AC 4.1-4.6: 4 mode integer multipliers, Epiphany threshold, off mode.
    /// </summary>
    public class DailyModeTests
    {
        static readonly Pcg32 TestRng = new Pcg32(42, 0);

        // ================================================================
        // AC 4.1: Enum values
        // ================================================================

        [Fact]
        public void Enum_FourValues_AreDistinct()
        {
            Assert.Equal(0, (int)DailyMode.Fast);
            Assert.Equal(1, (int)DailyMode.Steady);
            Assert.Equal(2, (int)DailyMode.Comprehend);
            Assert.Equal(3, (int)DailyMode.Roam);
            Assert.NotEqual(DailyMode.Fast, DailyMode.Steady);
            Assert.NotEqual(DailyMode.Comprehend, DailyMode.Roam);
        }

        // ================================================================
        // AC 4.2: Integer multiplier table precision
        // ================================================================

        [Fact]
        public void Fast_Mode_Progress_6Over4()
        {
            var r = DailyModeApplier.Apply(DailyMode.Fast, 100, 20, TestRng);
            Assert.Equal(150, r.ProgressDelta); // 100*6/4 = 150
            Assert.Equal(+2, r.InnerDemonDelta);
            Assert.Equal(0, r.DaoHeartDelta);
            Assert.False(r.EpiphanyTriggered);
            Assert.False(r.ShouldMove);
        }

        [Fact]
        public void Fast_Mode_InnerDemonPlus2()
        {
            var r = DailyModeApplier.Apply(DailyMode.Fast, 200, 20, TestRng);
            Assert.Equal(+2, r.InnerDemonDelta);
            Assert.Equal(300, r.ProgressDelta); // 200*6/4 = 300
        }

        [Fact]
        public void Steady_Mode_Progress_3Over4()
        {
            var r = DailyModeApplier.Apply(DailyMode.Steady, 100, 20, TestRng);
            Assert.Equal(75, r.ProgressDelta); // 100*3/4 = 75
            Assert.Equal(-1, r.InnerDemonDelta);
            Assert.Equal(0, r.DaoHeartDelta);
            Assert.True(r.FoundationBonus);
        }

        [Fact]
        public void Steady_Mode_FoundationBonus_True()
        {
            var r = DailyModeApplier.Apply(DailyMode.Steady, 50, 20, TestRng);
            Assert.True(r.FoundationBonus);
        }

        [Fact]
        public void Comprehend_Mode_Progress_1Over2()
        {
            var r = DailyModeApplier.Apply(DailyMode.Comprehend, 100, 20, TestRng);
            Assert.Equal(50, r.ProgressDelta); // 100/2 = 50
            Assert.Equal(0, r.InnerDemonDelta);
        }

        [Fact]
        public void Comprehend_Insight20_SomeEpiphanies_OverManyTrials()
        {
            int epiphanies = 0;
            int trials = 2000;
            for (int i = 0; i < trials; i++)
            {
                var trialRng = new Pcg32((ulong)(i + 1000), 0);
                var r = DailyModeApplier.Apply(DailyMode.Comprehend, 100, 20, trialRng);
                if (r.EpiphanyTriggered) epiphanies++;
            }

            // Insight=20 → threshold=2 → ~10% probability (roll 1..20, roll < 2 = roll==1 only → 5%)
            // Actually: roll < 2 means roll == 1 only → 1/20 = 5%
            double rate = (double)epiphanies / trials;
            Assert.True(rate > 0.03 && rate < 0.08, $"Expected ~5% epiphany rate, got {rate:P}");
        }

        [Fact]
        public void Comprehend_Insight25_HigherEpiphanyRate()
        {
            int epiphanies = 0;
            int trials = 2000;
            for (int i = 0; i < trials; i++)
            {
                var trialRng = new Pcg32((ulong)(i + 2000), 0);
                var r = DailyModeApplier.Apply(DailyMode.Comprehend, 100, 25, trialRng);
                if (r.EpiphanyTriggered) epiphanies++;
            }

            // Insight=25 → threshold=7 → roll<7 → rolls 1-6 → 6/20 = ~30%
            double rate = (double)epiphanies / trials;
            Assert.True(rate > 0.25 && rate < 0.35, $"Expected ~30% epiphany rate at Insight=25, got {rate:P}");
        }

        [Fact]
        public void Comprehend_Insight17_NoEpiphanies()
        {
            int epiphanies = 0;
            int trials = 500;
            for (int i = 0; i < trials; i++)
            {
                var trialRng = new Pcg32((ulong)(i + 3000), 0);
                var r = DailyModeApplier.Apply(DailyMode.Comprehend, 100, 17, trialRng);
                if (r.EpiphanyTriggered) epiphanies++;
            }

            Assert.Equal(0, epiphanies); // threshold = max(0, 17-18) = 0 → no epiphany
        }

        [Fact]
        public void Roam_Mode_Progress_1Over4()
        {
            var r = DailyModeApplier.Apply(DailyMode.Roam, 100, 20, TestRng);
            Assert.Equal(25, r.ProgressDelta); // 100/4 = 25
            Assert.Equal(-2, r.InnerDemonDelta);
            Assert.True(r.ShouldMove);
            Assert.Equal(3, r.EncounterExposure);
        }

        [Fact]
        public void Roam_Mode_ShouldMove_True()
        {
            var r = DailyModeApplier.Apply(DailyMode.Roam, 50, 20, TestRng);
            Assert.True(r.ShouldMove);
        }

        // ================================================================
        // AC 4.3: Epiphany threshold formula
        // ================================================================

        [Theory]
        [InlineData(18, 0)]  // threshold = max(0, 0) = 0
        [InlineData(19, 1)]  // threshold = 1
        [InlineData(20, 2)]  // threshold = 2
        [InlineData(21, 3)]
        [InlineData(22, 4)]
        [InlineData(23, 5)]
        [InlineData(24, 6)]
        [InlineData(25, 7)]  // threshold = 7
        [InlineData(30, 12)] // threshold = 12
        public void EpiphanyThreshold_Formula(int insight, int expectedThreshold)
        {
            Assert.Equal(expectedThreshold, DailyModeApplier.EpiphanyThreshold(insight));
        }

        // ================================================================
        // AC 4.6: off mode (no flag written — not tested here, in integration)
        // ================================================================

        [Fact]
        public void InvalidMode_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                DailyModeApplier.Apply((DailyMode)99, 100, 20, TestRng));
        }

        // ================================================================
        // AC: Progress=0 edge case
        // ================================================================

        [Fact]
        public void ZeroProgress_AllModes_ZeroDelta()
        {
            foreach (DailyMode mode in Enum.GetValues(typeof(DailyMode)))
            {
                var r = DailyModeApplier.Apply(mode, 0, 20, TestRng);
                Assert.Equal(0, r.ProgressDelta);
            }
        }

        // ================================================================
        // AC: Epiphany daoHeart gain
        // ================================================================

        [Fact]
        public void Epiphany_DaoHeartGain_WhenTriggered()
        {
            // Use Insight=50 → threshold=32 → very high trigger rate (~95%)
            // Run trials and verify some daoHeartGain occurrences
            int daoHeartGains = 0;
            int trials = 500;
            for (int i = 0; i < trials; i++)
            {
                var trialRng = new Pcg32((ulong)(i + 4000), 0);
                var r = DailyModeApplier.Apply(DailyMode.Comprehend, 100, 50, trialRng);
                if (r.EpiphanyTriggered && r.DaoHeartDelta == 5)
                    daoHeartGains++;
            }
            // ~50% of ~95% triggered → ~47% have daoHeart gain
            Assert.True(daoHeartGains > 0, "Some epiphanies should result in daoHeart gain");
        }
    }
}
