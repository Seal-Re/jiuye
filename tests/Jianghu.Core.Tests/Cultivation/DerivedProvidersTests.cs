using System.Collections.Generic;
using Jianghu.Cultivation;
using Jianghu.Stats;
using Xunit;

namespace Jianghu.Core.Tests.Cultivation
{
    /// <summary>
    /// combat-fullstruct story-001: derived:* per-entity 真求和验证。
    /// 4 路 derived provider (fleetWeighted/rosterWeighted/ghostSoldierWeighted/guSwarmWeighted) 的 per-resource 公式测试。
    /// 真 roster 数据结构 → FULLSTRUCT 后续（逐傀/逐兽/逐鬼 list 迭代）。当前测试验证公式正确性。
    /// </summary>
    public class DerivedProvidersTests
    {
        static StatBlock Stats => new StatBlock(new[] { 10, 10, 10, 10 });

        static CultivationState StateWith(params (string key, int value)[] resources)
        {
            var st = CultivationState.NewForPath("test", new[] { new ResourceDef("qi", 0, 1000, 0) });
            foreach (var (k, v) in resources)
                st.Resources[k] = v;
            return st;
        }

        // AC 1.1: fleetWeighted = constructTier × 50 + mindBandwidth × 2
        [Fact]
        public void test_fleet_weighted_uses_construct_tier_and_mind_bandwidth()
        {
            DerivedProviders.RegisterAll();
            var st = StateWith(("constructTier", 3), ("mindBandwidth", 20));
            int val = new FleetWeightedProvider().Compute(st, Stats);
            Assert.Equal(3 * 50 + 20 * 2, val); // 150 + 40 = 190
        }

        [Fact]
        public void test_fleet_weighted_zero_when_resources_missing()
        {
            DerivedProviders.RegisterAll();
            var st = StateWith();
            int val = new FleetWeightedProvider().Compute(st, Stats);
            Assert.Equal(0, val);
        }

        // AC 1.2: rosterWeighted = rosterPower × bond / 100
        [Fact]
        public void test_roster_weighted_formula()
        {
            DerivedProviders.RegisterAll();
            var st = StateWith(("rosterPower", 500), ("bond", 80));
            int val = new RosterWeightedProvider().Compute(st, Stats);
            Assert.Equal(500 * 80 / 100, val); // 400
        }

        [Fact]
        public void test_roster_weighted_bond_zero_is_zero()
        {
            DerivedProviders.RegisterAll();
            var st = StateWith(("rosterPower", 500), ("bond", 0));
            int val = new RosterWeightedProvider().Compute(st, Stats);
            Assert.Equal(0, val);
        }

        // AC 1.3: ghostSoldierWeighted = ghostSoldierPower × (1 − devourMeter/200)
        [Fact]
        public void test_ghost_soldier_weighted_formula()
        {
            DerivedProviders.RegisterAll();
            var st = StateWith(("ghostSoldierPower", 1000), ("devourMeter", 50));
            int val = new GhostSoldierWeightedProvider().Compute(st, Stats);
            Assert.Equal(1000 * (200 - 50) / 200, val); // 1000 * 150 / 200 = 750
        }

        [Fact]
        public void test_ghost_soldier_weighted_devour_meter_max_reduces_to_zero()
        {
            DerivedProviders.RegisterAll();
            var st = StateWith(("ghostSoldierPower", 1000), ("devourMeter", 200));
            int val = new GhostSoldierWeightedProvider().Compute(st, Stats);
            Assert.Equal(0, val);
        }

        // AC 1.4: guSwarmWeighted = guSwarmPower × venomCharge / 100
        [Fact]
        public void test_gu_swarm_weighted_formula()
        {
            DerivedProviders.RegisterAll();
            var st = StateWith(("guSwarmPower", 600), ("venomCharge", 90));
            int val = new GuSwarmWeightedProvider().Compute(st, Stats);
            Assert.Equal(600 * 90 / 100, val); // 540
        }

        [Fact]
        public void test_gu_swarm_weighted_no_venom_is_zero()
        {
            DerivedProviders.RegisterAll();
            var st = StateWith(("guSwarmPower", 600), ("venomCharge", 0));
            int val = new GuSwarmWeightedProvider().Compute(st, Stats);
            Assert.Equal(0, val);
        }

        // AC 1.5: All registered and Compute returns non-zero with typical values
        [Fact]
        public void test_all_four_providers_registered_and_nonzero()
        {
            // Re-register to ensure clean state
            DerivedProviders.RegisterAll();
            var st = StateWith(
                ("constructTier", 3), ("mindBandwidth", 20),
                ("rosterPower", 500), ("bond", 80),
                ("ghostSoldierPower", 1000), ("devourMeter", 50),
                ("guSwarmPower", 600), ("venomCharge", 90));

            int fleet = new FleetWeightedProvider().Compute(st, Stats);
            int roster = new RosterWeightedProvider().Compute(st, Stats);
            int ghost = new GhostSoldierWeightedProvider().Compute(st, Stats);
            int gu = new GuSwarmWeightedProvider().Compute(st, Stats);

            Assert.True(fleet > 0, $"fleetWeighted={fleet} should be >0");
            Assert.True(roster > 0);
            Assert.True(ghost > 0);
            Assert.True(gu > 0);
        }

        // AC 1.7: Deterministic (same inputs → same outputs, no RNG)
        [Fact]
        public void test_all_providers_deterministic()
        {
            DerivedProviders.RegisterAll();
            for (int i = 0; i < 10; i++)
            {
                var st = StateWith(
                    ("constructTier", i), ("mindBandwidth", i * 10),
                    ("rosterPower", i * 100), ("bond", i * 10 + 10),
                    ("ghostSoldierPower", i * 100), ("devourMeter", i * 5),
                    ("guSwarmPower", i * 100), ("venomCharge", i * 10));

                int f1 = new FleetWeightedProvider().Compute(st, Stats);
                int f2 = new FleetWeightedProvider().Compute(st, Stats);
                Assert.Equal(f1, f2);
            }
        }
    }
}
