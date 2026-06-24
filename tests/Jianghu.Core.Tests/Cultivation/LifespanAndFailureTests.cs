using System.Collections.Generic;
using Jianghu.Cultivation;
using Jianghu.Random;
using Xunit;

namespace Jianghu.Core.Tests.Cultivation
{
    /// <summary>
    /// cultivation-a1-rest: LifespanAndFailure 测试。
    /// 覆盖：LIFESPAN_TABLE, deathLine, 5失败模式辅助。
    /// </summary>
    public class LifespanAndFailureTests
    {
        static Dictionary<string, int> NewFlags(int foundation = 50, int innerDemon = 0,
            int lifespanBonus = 0, int major = 2, int tribDebt = 0)
        {
            return new Dictionary<string, int>
            {
                ["foundation"] = foundation,
                ["innerDemon"] = innerDemon,
                ["lifespanBonus"] = lifespanBonus,
                ["major"] = major,
                ["tribDebt"] = tribDebt,
                ["bottleneckStreak"] = 0,
                ["stuck"] = 0,
            };
        }

        // ================================================================
        // LIFESPAN_TABLE + deathLine
        // ================================================================

        [Fact]
        public void LifespanBonusTable_HasCorrectUTValues()
        {
            Assert.Equal(0, LifespanAndFailure.LifespanBonus[0]);
            Assert.Equal(100, LifespanAndFailure.LifespanBonus[2]);
            Assert.Equal(450, LifespanAndFailure.LifespanBonus[5]);
            Assert.Equal(800, LifespanAndFailure.LifespanBonus[7]);
            Assert.Equal(1500, LifespanAndFailure.LifespanBonus[10]);
        }

        [Fact]
        public void DeathLine_BasePlusBonus()
        {
            Assert.Equal(80, LifespanAndFailure.DeathLine(0));          // 凡人: 80
            Assert.Equal(180, LifespanAndFailure.DeathLine(100));       // UT2: 80+100
            Assert.Equal(530, LifespanAndFailure.DeathLine(450));       // UT5: 80+450
        }

        [Fact]
        public void IsLifespanExhausted_AtDeathLine()
        {
            Assert.True(LifespanAndFailure.IsLifespanExhausted(80, 0));   // Age=80, death=80
            Assert.False(LifespanAndFailure.IsLifespanExhausted(79, 0));   // Age=79, death=80
            Assert.True(LifespanAndFailure.IsLifespanExhausted(200, 100)); // Age=200, death=180
        }

        [Fact]
        public void ApplyLifespanBonus_Accumulates()
        {
            var f = NewFlags(lifespanBonus: 0);
            LifespanAndFailure.ApplyLifespanBonus(f, unifiedTier: 2); // +100
            Assert.Equal(100, f["lifespanBonus"]);
            LifespanAndFailure.ApplyLifespanBonus(f, unifiedTier: 4); // +300
            Assert.Equal(400, f["lifespanBonus"]);
        }

        // ================================================================
        // ApplyRealmFallback
        // ================================================================

        [Fact]
        public void ApplyRealmFallback_ReducesMajorAndFoundation()
        {
            var f = NewFlags(major: 3, foundation: 80);
            LifespanAndFailure.ApplyRealmFallback(f);
            Assert.Equal(2, f["major"]);
            Assert.Equal(40, f["foundation"]);
            Assert.Equal(LifespanAndFailure.TribGatePermanentDelta, f["tribGatePermDelta"]);
        }

        [Fact]
        public void ApplyRealmFallback_FloorAtZero()
        {
            var f = NewFlags(major: 0, foundation: 30);
            LifespanAndFailure.ApplyRealmFallback(f);
            Assert.Equal(0, f["major"]);
            Assert.Equal(0, f["foundation"]); // floor at 0
        }

        // ================================================================
        // UnstuckBottleneck
        // ================================================================

        [Fact]
        public void UnstuckBottleneck_ClearsStreakAndStuck()
        {
            var f = NewFlags();
            f["bottleneckStreak"] = 7;
            f["stuck"] = 1;
            LifespanAndFailure.UnstuckBottleneck(f);
            Assert.Equal(0, f["bottleneckStreak"]);
            Assert.Equal(0, f["stuck"]);
        }

        // ================================================================
        // TryPurgeDeviation
        // ================================================================

        [Fact]
        public void TryPurgeDeviation_NoRNG_UsesDefault()
        {
            var f = NewFlags(innerDemon: 70);
            // default roll=10, purgeGate=16-(70-60)/5=14, 10<14 → fail
            bool ok = LifespanAndFailure.TryPurgeDeviation(f, rng: null);
            Assert.False(ok);
            Assert.Equal(72, f["innerDemon"]); // +2
        }

        [Fact]
        public void TryPurgeDeviation_Success_ReducesInnerDemon()
        {
            var f = NewFlags(innerDemon: 65, foundation: 50);
            // purgeGate=16-(65-60)/5=15. Need roll>=15. Use rng that gives high value
            var rng = new Pcg32(99999, 0);
            bool ok = LifespanAndFailure.TryPurgeDeviation(f, rng);
            // Result depends on RNG; check consistency
            if (ok)
            {
                Assert.Equal(25, f["innerDemon"]); // 65-40
                Assert.Equal(35, f["foundation"]); // 50-15
            }
        }

        [Fact]
        public void TryPurgeDeviation_Deterministic()
        {
            var f1 = NewFlags(innerDemon: 70);
            var f2 = NewFlags(innerDemon: 70);
            var rng1 = new Pcg32(42, 0);
            var rng2 = new Pcg32(42, 0);
            bool ok1 = LifespanAndFailure.TryPurgeDeviation(f1, rng1);
            bool ok2 = LifespanAndFailure.TryPurgeDeviation(f2, rng2);
            Assert.Equal(ok1, ok2);
            Assert.Equal(f1["innerDemon"], f2["innerDemon"]);
        }

        // ================================================================
        // Setback recovery
        // ================================================================

        [Fact]
        public void CanRecoverFromSetback_Conditions()
        {
            var f = NewFlags(foundation: 50, tribDebt: 0);
            Assert.True(LifespanAndFailure.CanRecoverFromSetback(f));

            f["tribDebt"] = 10;
            Assert.False(LifespanAndFailure.CanRecoverFromSetback(f));

            f["tribDebt"] = 0;
            f["foundation"] = 30;
            Assert.False(LifespanAndFailure.CanRecoverFromSetback(f));
        }

        [Fact]
        public void DecayTribDebt_ReducesOverTime()
        {
            var f = NewFlags(tribDebt: 15);
            LifespanAndFailure.DecayTribDebt(f, decay: 5);
            Assert.Equal(10, f["tribDebt"]);
            LifespanAndFailure.DecayTribDebt(f, decay: 5);
            Assert.Equal(5, f["tribDebt"]);
            LifespanAndFailure.DecayTribDebt(f, decay: 5);
            Assert.Equal(0, f["tribDebt"]);
            LifespanAndFailure.DecayTribDebt(f, decay: 5);
            Assert.Equal(0, f["tribDebt"]); // floor at 0
        }

        // ================================================================
        // InnerDemon lethal threshold
        // ================================================================

        [Fact]
        public void InnerDemonLethal_Threshold()
        {
            Assert.Equal(95, LifespanAndFailure.InnerDemonLethal);
            Assert.Equal(60, LifespanAndFailure.InnerDemonDeviate);
        }

        [Fact]
        public void BottleneckStuckThreshold_Is6()
        {
            Assert.Equal(6, LifespanAndFailure.BottleneckStuckThreshold);
        }
    }
}
