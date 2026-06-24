using System;
using System.Collections.Generic;
using Jianghu.Cultivation;
using Jianghu.Random;
using Xunit;

namespace Jianghu.Core.Tests.Cultivation
{
    /// <summary>
    /// A.3 features: stories 006+009+012+013 tests.
    /// </summary>
    public class A3FeatureTests
    {
        static CultivationState NewSt(int daoHeart = 0, int innerDemon = 0)
        {
            var resDefs = new List<ResourceDef> { new ResourceDef("qi", 0, 1000, 100) };
            var st = CultivationState.NewForPath("test", resDefs);
            st.DaoHeart = daoHeart;
            st.InnerDemon = innerDemon;
            return st;
        }

        // ================================================================
        // Story-006: Awaken power unlock
        // ================================================================

        [Fact]
        public void AwakenBonus_AddsToBasePE()
        {
            Assert.Equal(150, AwakenPowerService.ApplyAwakenBonus(100, 50));
            Assert.Equal(100, AwakenPowerService.ApplyAwakenBonus(100, 0));
        }

        [Fact]
        public void AwakenUnlock_SetsFlags()
        {
            var st = NewSt();
            var def = new AwakeningDef("a1", "test", "tag", AwakenTrigger.NearDeath,
                new[] { "art_01", "art_02" }, 50);
            AwakenPowerService.ApplyUnlock(st, def);
            Assert.Equal(1, st.Flags["awakened"]);
            Assert.Equal(1, st.Flags["awaken_unlock_a1"]);
        }

        // ================================================================
        // Story-009: Dual power formula
        // ================================================================

        [Fact]
        public void DualPE_Formula()
        {
            // mainPE=200, secondPE=100, bandwidth=80 → bonus=100*80/100=80, cap=100 → 80
            Assert.Equal(280, DualPowerService.ComputeDualPE(200, 100, 80));
        }

        [Fact]
        public void DualPE_BonusCapped()
        {
            // mainPE=200, secondPE=300, bandwidth=80 → bonus=240, cap=100 → capped at 100
            Assert.Equal(300, DualPowerService.ComputeDualPE(200, 300, 80));
        }

        [Fact]
        public void Backlash_Below80Pct_NeverTriggers()
        {
            var rng = new Pcg32(42, 0);
            var (trig, _) = DualPowerService.CheckBacklash(100, 70, rng); // 70% load
            Assert.False(trig);
        }

        [Fact]
        public void Backlash_Above80Pct_MayTrigger()
        {
            bool anyTriggered = false;
            for (int i = 0; i < 500; i++)
            {
                var rng = new Pcg32((ulong)(i + 1), 0);
                var (trig, gain) = DualPowerService.CheckBacklash(100, 90, rng);
                if (trig) { anyTriggered = true; Assert.Equal(5, gain); break; }
            }
            Assert.True(anyTriggered, "Backlash at 90% should trigger in ~500 trials");
        }

        // ================================================================
        // Story-012/013: Good/Evil fork
        // ================================================================

        [Fact]
        public void MoralTag_Righteous_At80DaoHeart()
        {
            var st = NewSt(daoHeart: 80, innerDemon: 0);
            Assert.Equal("righteous", MoralForkService.MoralTag(st));
        }

        [Fact]
        public void MoralTag_Evil_At70InnerDemon()
        {
            var st = NewSt(daoHeart: 0, innerDemon: 70);
            Assert.Equal("evil", MoralForkService.MoralTag(st));
        }

        [Fact]
        public void MoralTag_Neutral_WhenNeither()
        {
            var st = NewSt(daoHeart: 40, innerDemon: 30);
            Assert.Null(MoralForkService.MoralTag(st));
        }

        [Fact]
        public void MoralTag_DaoHeartOverrides_InnerDemon()
        {
            // Both high — daoHeart priority (checked first in MoralTag)
            var st = NewSt(daoHeart: 90, innerDemon: 80);
            Assert.Equal("righteous", MoralForkService.MoralTag(st));
        }

        [Fact]
        public void Righteous_TribulationModifier_Reduces()
        {
            int modified = MoralForkService.RighteousTribulationModifier(100);
            Assert.Equal(80, modified); // 100 * 4/5
        }

        [Fact]
        public void Evil_TribulationModifier_Increases()
        {
            int modified = MoralForkService.EvilTribulationModifier(100);
            Assert.Equal(150, modified); // 100 * 3/2
        }

        [Fact]
        public void GetTribulationModifier_Righteous()
        {
            var st = NewSt(daoHeart: 85);
            Assert.Equal(80, MoralForkService.GetTribulationModifier(st, 100));
        }

        [Fact]
        public void GetTribulationModifier_Evil()
        {
            var st = NewSt(innerDemon: 75);
            Assert.Equal(150, MoralForkService.GetTribulationModifier(st, 100));
        }

        [Fact]
        public void GetTribulationModifier_Neutral()
        {
            var st = NewSt(daoHeart: 40, innerDemon: 30);
            Assert.Equal(100, MoralForkService.GetTribulationModifier(st, 100));
        }

        [Fact]
        public void Righteous_BreakthroughBonus()
        {
            Assert.Equal(3, MoralForkService.RighteousBreakthroughBonus());
        }

        [Fact]
        public void MoralTag_PureStatic_NoRNG()
        {
            // Deterministic: same state → same tag, no RNG involved
            var st1 = NewSt(daoHeart: 85);
            var st2 = NewSt(daoHeart: 85);
            Assert.Equal(MoralForkService.MoralTag(st1), MoralForkService.MoralTag(st2));
        }
    }
}
