using System;
using System.Collections.Generic;
using Jianghu.Cultivation;
using Xunit;

namespace Jianghu.Core.Tests.Cultivation
{
    /// <summary>
    /// A.3 foundation: stories 001+002+004+007+008+010 tests.
    /// TransitionDef, PathId migration, AwakeningDef, DualPathDef, RiskModifier.
    /// </summary>
    public class A3FoundationTests
    {
        // ================================================================
        // Story-001: TransitionDef
        // ================================================================

        [Fact]
        public void TransitionDef_Creation()
        {
            var def = new TransitionDef("t1", TransitionKind.Transmute, "sword_immortal",
                "sword_sage", new TransitionGate(4, null, null),
                new CarryoverRule(new[] { "qi" }, Array.Empty<string>(), 0), 100);
            Assert.Equal("t1", def.Id);
            Assert.Equal(TransitionKind.Transmute, def.Kind);
        }

        [Fact]
        public void TransitionRegistry_Loads()
        {
            var defs = new[]
            {
                new TransitionDef("t1", TransitionKind.Transmute, "sword_immortal",
                    "sword_sage", new TransitionGate(4, null, null), null, 100),
                new TransitionDef("t2", TransitionKind.Awaken, "sword_immortal",
                    null, new TransitionGate(6, null, null), null, 0),
            };
            var reg = new TransitionRegistry(defs);
            Assert.Equal(2, reg.Count);
        }

        [Fact]
        public void TransitionRegistry_Eligible_Filters()
        {
            var defs = new[]
            {
                new TransitionDef("t1", TransitionKind.Transmute, "sword_immortal",
                    "sword_sage", new TransitionGate(4, null, null), null, 100),
                new TransitionDef("t2", TransitionKind.Awaken, "sword_immortal",
                    null, new TransitionGate(6, null, null), null, 0),
            };
            var reg = new TransitionRegistry(defs);
            var eligible = reg.Eligible("sword_immortal", TransitionKind.Transmute);
            Assert.Single(eligible);
        }

        // ================================================================
        // Story-002: PathId migration
        // ================================================================

        [Fact]
        public void PathId_IsSettable()
        {
            var st = NewSt("sword_immortal");
            Assert.Equal("sword_immortal", st.PathId);
            st.PathId = "sword_sage";
            Assert.Equal("sword_sage", st.PathId);
        }

        [Fact]
        public void Clone_PathId_Independent()
        {
            var st = NewSt("sword_immortal");
            var clone = st.Clone();
            clone.PathId = "sword_sage";
            Assert.Equal("sword_immortal", st.PathId); // original unchanged
            Assert.Equal("sword_sage", clone.PathId);
        }

        // ================================================================
        // Story-004: AwakeningDef
        // ================================================================

        [Fact]
        public void AwakeningDef_Creation()
        {
            var def = new AwakeningDef("a1", "sword_immortal", "sword_bloodline",
                AwakenTrigger.NearDeath, new[] { "sword_art_01" }, 50);
            Assert.Equal("a1", def.Id);
            Assert.Equal(AwakenTrigger.NearDeath, def.Trigger);
        }

        [Fact]
        public void AwakeningRegistry_Loads()
        {
            var defs = new[] { new AwakeningDef("a1", "sword_immortal", "tag",
                AwakenTrigger.RealmGate, Array.Empty<string>(), 30) };
            var reg = new AwakeningRegistry(defs);
            Assert.Equal(1, reg.Count);
        }

        // ================================================================
        // Story-007/008: DualPath compatibility
        // ================================================================

        [Fact]
        public void SlotCap_Is2()
        {
            Assert.Equal(2, DualCompatibility.SLOT_CAP);
        }

        [Fact]
        public void Bandwidth_Formula()
        {
            Assert.Equal(50, DualCompatibility.Bandwidth(0));
            Assert.Equal(70, DualCompatibility.Bandwidth(10));
            Assert.Equal(90, DualCompatibility.Bandwidth(20));
        }

        [Fact]
        public void CanDual_CompatiblePath()
        {
            Assert.True(DualCompatibility.CanDualCultivate("sword_immortal", "fa_xiu"));
        }

        [Fact]
        public void CanDual_ExcludedPath()
        {
            Assert.False(DualCompatibility.CanDualCultivate("lei_xiu", "gui_xiu_yang_hun"));
            Assert.False(DualCompatibility.CanDualCultivate("gui_xiu_yang_hun", "lei_xiu")); // symmetric
        }

        [Fact]
        public void Excludes_FourStandardPairs()
        {
            Assert.Equal(4, DualCompatibility.StandardExcludes.Count);
        }

        // ================================================================
        // Story-010: RiskModifier
        // ================================================================

        [Fact]
        public void RiskModifier_Creation()
        {
            var rm = new RiskModifier("rm1", RiskTrigger.Transition, 100,
                RiskPenaltyKind.InnerDemonGain, 10, 50, null);
            Assert.Equal("rm1", rm.Id);
            Assert.Equal(100, rm.ProbabilityPermille);
            Assert.Equal(50, rm.CooldownTicks);
        }

        [Fact]
        public void RiskModifierRegistry_Loads()
        {
            var rms = new[]
            {
                new RiskModifier("rm1", RiskTrigger.Transition, 100,
                    RiskPenaltyKind.InnerDemonGain, 10, 50, null),
                new RiskModifier("rm2", RiskTrigger.Breakthrough, 50,
                    RiskPenaltyKind.ProgressLoss, 20, 100, "innerDemon<30"),
            };
            var reg = new RiskModifierRegistry(rms);
            Assert.Equal(2, reg.Count);
        }

        [Fact]
        public void RiskModifier_Permille_WithinRange()
        {
            var rm = new RiskModifier("rm1", RiskTrigger.Cast, 500,
                RiskPenaltyKind.ResourceDrain, 30, 30, null);
            Assert.True(rm.ProbabilityPermille >= 0 && rm.ProbabilityPermille <= 1000);
        }

        // ================================================================
        // Helpers
        // ================================================================

        static CultivationState NewSt(string pathId = "test")
        {
            var resDefs = new List<ResourceDef> { new ResourceDef("qi", 0, 1000, 100) };
            var st = CultivationState.NewForPath(pathId, resDefs);
            return st;
        }
    }
}
