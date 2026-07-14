using System;
using System.Collections.Generic;
using Jianghu.Config;
using Jianghu.Cultivation;
using Jianghu.Model;
using Jianghu.Stats;
using Xunit;

namespace Jianghu.Core.Tests.Cultivation
{
    public class TransitionServiceTests
    {
        static CultivationPathDef MakePath(string id, int[] uts, int[] realmMuls)
            => new(id, id, "physical", new[] { "melee" },
                new[] { new ResourceDef("qi", 0, 1000, 0) },
                new PowerFormulaDef(new[] { new PowerTerm("stat:Force", 4, null) },
                    Array.Empty<PowerMod>(), null),
                new RealmCurveDef(realmMuls, uts,
                    new[] { "L1", "L2", "L3" }, new[] { 0, 100, 300 },
                    new[] { 1, 1, 1 }, true, 2),
                Array.Empty<ArtCategoryDef>(),
                Array.Empty<CombatSkillDef>(),
                new EntryGateDef(""), new SelectionRuleDef(1, 3), null);

        sealed class ListPathSource : IPathSource
        {
            readonly IReadOnlyList<CultivationPathDef> _paths;
            public ListPathSource(IReadOnlyList<CultivationPathDef> p) => _paths = p;
            public IReadOnlyList<CultivationPathDef> Load() => _paths;
        }

        [Fact]
        public void test_migrate_path_changes_path_id()
        {
            var fromPath = MakePath("from", new[] { 0, 2, 4 }, new[] { 10, 15, 25 });
            var toPath = MakePath("to", new[] { 0, 2, 4, 6 }, new[] { 10, 15, 25, 35 });
            var source = new ListPathSource(new[] { fromPath, toPath });
            var state = CultivationState.NewForPath("from", fromPath.Resources);
            state.RealmIndex = 1; // UT=2
            var def = new TransitionDef("t1", TransitionKind.Transmute, "from", "to",
                new TransitionGate(1, null, null), null, 0);

            string oldId = TransitionService.MigratePathId(state, def, source);

            Assert.Equal("from", oldId);
            Assert.Equal("to", state.PathId);
        }

        [Fact]
        public void test_realm_mapping_aligns_by_unified_tier()
        {
            // from: UT[0,2,4], realm 1→UT=2. to: UT[0,1,2,3,4], realm 2→UT=2
            var fromPath = MakePath("from", new[] { 0, 2, 4 }, new[] { 10, 15, 25 });
            var toPath = MakePath("to", new[] { 0, 1, 2, 3, 4 }, new[] { 10, 12, 15, 20, 25 });
            var source = new ListPathSource(new[] { fromPath, toPath });
            var state = CultivationState.NewForPath("from", fromPath.Resources);
            state.RealmIndex = 1; // UT=2 on from
            var def = new TransitionDef("t1", TransitionKind.Transmute, "from", "to",
                new TransitionGate(1, null, null), null, 0);

            TransitionService.MigratePathId(state, def, source);

            Assert.Equal(2, state.RealmIndex); // toPath UT[2]=2
        }

        [Fact]
        public void test_carryover_keeps_resources()
        {
            var fromPath = MakePath("from", new[] { 0, 2 }, new[] { 10, 15 });
            var toPath = MakePath("to", new[] { 0, 2 }, new[] { 10, 15 });
            var source = new ListPathSource(new[] { fromPath, toPath });
            var state = CultivationState.NewForPath("from", fromPath.Resources);
            state.Resources["qi"] = 500;
            state.Resources["gold"] = 100;
            var def = new TransitionDef("t1", TransitionKind.Transmute, "from", "to",
                new TransitionGate(0, null, null),
                new CarryoverRule(new[] { "qi" }, Array.Empty<string>(), 0), 0);

            TransitionService.MigratePathId(state, def, source);

            Assert.True(state.Resources.TryGetValue("qi", out int qi) && qi == 500);
            Assert.False(state.Resources.ContainsKey("gold")); // 不在 keep 列表
        }

        [Fact]
        public void test_cost_deducted_from_cultivation_points()
        {
            var fromPath = MakePath("from", new[] { 0, 2 }, new[] { 10, 15 });
            var toPath = MakePath("to", new[] { 0, 2 }, new[] { 10, 15 });
            var source = new ListPathSource(new[] { fromPath, toPath });
            var state = CultivationState.NewForPath("from", fromPath.Resources);
            state.CultivationPoints = 1000;
            var def = new TransitionDef("t1", TransitionKind.Transmute, "from", "to",
                new TransitionGate(0, null, null), null, 300);

            TransitionService.MigratePathId(state, def, source);

            Assert.Equal(700, state.CultivationPoints);
        }

        [Fact]
        public void test_migrate_is_deterministic()
        {
            var fromPath = MakePath("from", new[] { 0, 2 }, new[] { 10, 15 });
            var toPath = MakePath("to", new[] { 0, 2, 4 }, new[] { 10, 15, 25 });
            var source = new ListPathSource(new[] { fromPath, toPath });

            for (int i = 0; i < 10; i++)
            {
                var s1 = CultivationState.NewForPath("from", fromPath.Resources);
                s1.RealmIndex = 1; s1.Resources["qi"] = 200;
                var s2 = CultivationState.NewForPath("from", fromPath.Resources);
                s2.RealmIndex = 1; s2.Resources["qi"] = 200;
                var def = new TransitionDef("t1", TransitionKind.Transmute, "from", "to",
                    new TransitionGate(1, null, null),
                    new CarryoverRule(new[] { "qi" }, Array.Empty<string>(), 0), 0);

                TransitionService.MigratePathId(s1, def, source);
                TransitionService.MigratePathId(s2, def, source);

                Assert.Equal(s1.PathId, s2.PathId);
                Assert.Equal(s1.RealmIndex, s2.RealmIndex);
                Assert.Equal(s1.Resources["qi"], s2.Resources["qi"]);
            }
        }
    }
}
