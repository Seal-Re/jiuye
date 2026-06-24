using System;
using Jianghu.Cultivation;
using Xunit;

namespace Jianghu.Core.Tests.Cultivation
{
    /// <summary>
    /// Story-006: 破单调INV-VARIETY真判据 tests.
    /// AC 6.1-6.7: INV-VARIETY, INV-NO-DOMINANT, tracker determinism, off mode.
    /// </summary>
    public class VarietyTrackerTests
    {
        // ================================================================
        // AC 6.1: INV-VARIETY — K=10 window must have >=2 modes
        // ================================================================

        [Fact]
        public void SingleMode_10Ticks_Violates_INV_VARIETY()
        {
            var t = new VarietyTracker();
            for (int i = 0; i < 10; i++)
                t.Record(DailyMode.Fast);

            Assert.Equal(1, t.DistinctModesInShortWindow());
            Assert.False(t.PassesInvariety());
        }

        [Fact]
        public void TwoModes_10Ticks_Satisfies_INV_VARIETY()
        {
            var t = new VarietyTracker();
            for (int i = 0; i < 5; i++) t.Record(DailyMode.Fast);
            for (int i = 0; i < 5; i++) t.Record(DailyMode.Steady);

            Assert.Equal(2, t.DistinctModesInShortWindow());
            Assert.True(t.PassesInvariety());
        }

        [Fact]
        public void EmptyTracker_PassesBoth()
        {
            var t = new VarietyTracker();
            Assert.True(t.PassesInvariety());
            Assert.True(t.PassesInvNoDominant());
        }

        [Fact]
        public void FewerThan2Entries_PassesInvariety()
        {
            var t = new VarietyTracker();
            t.Record(DailyMode.Fast);
            Assert.True(t.PassesInvariety()); // Only 1 entry → auto-pass
        }

        // ================================================================
        // AC 6.2: INV-NO-DOMINANT — 50-tick window single mode <= 80%
        // ================================================================

        [Fact]
        public void Dominant80Pct_Satisfies_INV_NO_DOMINANT()
        {
            var t = new VarietyTracker();
            // 40 Fast + 10 Steady = 80% Fast, exactly at threshold
            for (int i = 0; i < 40; i++) t.Record(DailyMode.Fast);
            for (int i = 0; i < 10; i++) t.Record(DailyMode.Steady);

            Assert.Equal(80, t.DominantModePct());
            Assert.True(t.PassesInvNoDominant()); // 80% <= 80% → pass
        }

        [Fact]
        public void Dominant82Pct_Violates_INV_NO_DOMINANT()
        {
            var t = new VarietyTracker();
            // 41 Fast + 9 Steady = 82% Fast
            for (int i = 0; i < 41; i++) t.Record(DailyMode.Fast);
            for (int i = 0; i < 9; i++) t.Record(DailyMode.Steady);

            Assert.True(t.DominantModePct() > 80);
            Assert.False(t.PassesInvNoDominant());
        }

        // ================================================================
        // AC 6.3: Sliding window eviction
        // ================================================================

        [Fact]
        public void Window_EvictsOldEntries()
        {
            var t = new VarietyTracker();
            // Fill with Fast
            for (int i = 0; i < 10; i++) t.Record(DailyMode.Fast);
            Assert.Equal(1, t.DistinctModesInShortWindow());

            // Replace all with Steady
            for (int i = 0; i < 10; i++) t.Record(DailyMode.Steady);
            Assert.Equal(1, t.DistinctModesInShortWindow()); // All Steady now

            // Mix in Roam
            for (int i = 0; i < 5; i++) t.Record(DailyMode.Roam);
            Assert.True(t.DistinctModesInShortWindow() >= 2); // Steady + Roam
        }

        // ================================================================
        // AC 6.7: Determinism
        // ================================================================

        [Fact]
        public void SameSequence_SameDiagnosis()
        {
            var t1 = new VarietyTracker();
            var t2 = new VarietyTracker();

            foreach (DailyMode m in new[] {
                DailyMode.Fast, DailyMode.Steady, DailyMode.Fast, DailyMode.Roam,
                DailyMode.Steady, DailyMode.Comprehend, DailyMode.Fast, DailyMode.Roam
            })
            {
                t1.Record(m);
                t2.Record(m);
            }

            var d1 = t1.Diagnosis();
            var d2 = t2.Diagnosis();
            Assert.Equal(d1.varietyOk, d2.varietyOk);
            Assert.Equal(d1.dominantOk, d2.dominantOk);
            Assert.Equal(d1.distinctModes, d2.distinctModes);
            Assert.Equal(d1.dominantPct, d2.dominantPct);
        }

        [Fact]
        public void Clone_PreservesState()
        {
            var t = new VarietyTracker();
            t.Record(DailyMode.Fast);
            t.Record(DailyMode.Steady);
            t.Record(DailyMode.Roam);

            var clone = t.Clone();
            var d = clone.Diagnosis();
            Assert.Equal(3, d.distinctModes);
        }

        [Fact]
        public void Clone_Independent()
        {
            var t = new VarietyTracker();
            t.Record(DailyMode.Fast);
            var clone = t.Clone();
            clone.Record(DailyMode.Steady);
            clone.Record(DailyMode.Roam);
            clone.Record(DailyMode.Comprehend);

            // Original unchanged
            Assert.Equal(1, t.DistinctModesInShortWindow());
            // Clone modified independently
            Assert.True(clone.DistinctModesInShortWindow() >= 3);
        }
    }
}
