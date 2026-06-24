using System;
using Jianghu.Cultivation;
using Xunit;

namespace Jianghu.Core.Tests.Cultivation
{
    /// <summary>
    /// Story-003: 佛修破戒修正 tests.
    /// AC 3.1-3.5: daoHeart half-life, lethal-only fallen, NPC simulation sanity.
    /// </summary>
    public class BuddhistVowTests
    {
        // ================================================================
        // AC 3.2: 破戒时 daoHeart = max(current/2, 1), innerDemon + delta
        // ================================================================

        [Fact]
        public void VowBreak_DaoHeart_Halved_NotZeroed()
        {
            var (newDH, demonGain) = BuddhistVow.ApplyVowBreak(100, 30);
            Assert.Equal(50, newDH); // 100/2 = 50, not 0
            Assert.Equal(5, demonGain);
        }

        [Fact]
        public void VowBreak_DaoHeart_MinimumOne()
        {
            var (newDH, _) = BuddhistVow.ApplyVowBreak(1, 30);
            Assert.Equal(1, newDH); // max(1/2, 1) = 1
        }

        [Fact]
        public void VowBreak_DaoHeart_ZeroInput_GivesOne()
        {
            var (newDH, _) = BuddhistVow.ApplyVowBreak(0, 30);
            Assert.Equal(1, newDH); // max(0/2, 1) = 1 — never zero
        }

        [Fact]
        public void VowBreak_InnerDemon_DoesNotExceed100()
        {
            var (_, demonGain) = BuddhistVow.ApplyVowBreak(50, 98);
            Assert.Equal(2, demonGain); // 98+5=103→clamp 100, gain=2
        }

        // ================================================================
        // AC 3.3: innerDemon >= 95 triggers Fallen, not just vow break
        // ================================================================

        [Fact]
        public void VowBreak_DoesNot_TriggerFallen_Below95()
        {
            var (newDH, demonGain) = BuddhistVow.ApplyVowBreak(50, 85);
            // After vow break: innerDemon = 85 + 5 = 90 < 95
            Assert.False(BuddhistVow.ShouldFall(90));
            Assert.True(newDH > 0);
        }

        [Fact]
        public void VowBreak_TriggersFallen_At95OrAbove()
        {
            Assert.True(BuddhistVow.TriggersFallen(95));
            Assert.True(BuddhistVow.TriggersFallen(100));
            Assert.False(BuddhistVow.TriggersFallen(94));
        }

        [Fact]
        public void VowBreak_OnlyLethal_NotDirectFallen()
        {
            // Breaking a vow at innerDemon=89: after break innerDemon=94, still not lethal
            var (newDH, demonGain) = BuddhistVow.ApplyVowBreak(60, 89);
            int newDemon = 89 + demonGain;
            Assert.Equal(94, newDemon); // 89+5=94
            Assert.False(BuddhistVow.ShouldFall(newDemon));

            // Breaking again at 94: after break = 99 → lethal
            var (_, demonGain2) = BuddhistVow.ApplyVowBreak(newDH, newDemon);
            int newDemon2 = newDemon + demonGain2;
            Assert.True(BuddhistVow.ShouldFall(newDemon2));
        }

        // ================================================================
        // AC 3.4: Buddhist NPC堕落率 sanity check
        // ================================================================

        [Fact]
        public void NPC_WithoutVowBreak_NoFallen()
        {
            // At innerDemon=50 (normal), no vow break → no Fallen
            Assert.False(BuddhistVow.ShouldFall(50));
        }

        [Fact]
        public void RepeatedVowBreaks_EventuallyTriggerFallen()
        {
            // Simulate repeated vow breaks starting from innerDemon=30
            int dh = 100, demon = 30, breaks = 0;
            bool fallen = false;
            for (int i = 0; i < 20 && !fallen; i++)
            {
                (dh, int dg) = BuddhistVow.ApplyVowBreak(dh, demon);
                demon += dg;
                breaks++;
                fallen = BuddhistVow.ShouldFall(demon);
            }
            // After ~13 breaks: demon = 30 + 13*5 = 95 → fallen
            Assert.True(fallen);
            Assert.True(breaks >= 10, $"Fallen should require reasonable number of breaks, got {breaks}");
        }

        // ================================================================
        // AC 3.5: Off mode — Buddhist Vow module loadable but inactive
        // ================================================================

        [Fact]
        public void BuddhistVow_Module_Loadable()
        {
            // Pure static functions — always loadable, no CultivationState dependency
            Assert.Equal(50, BuddhistVow.ApplyVowBreak(100, 30).NewDaoHeart);
        }
    }
}
