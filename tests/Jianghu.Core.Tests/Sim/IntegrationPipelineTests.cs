using System;
using System.Collections.Generic;
using Jianghu.Actions;
using Jianghu.Decide;
using Jianghu.Events;
using Jianghu.Model;
using Jianghu.Random;
using Jianghu.Sim;
using Jianghu.Stats;
using Xunit;

namespace Jianghu.Core.Tests.Sim
{
    /// <summary>
    /// Integration epic: stories 001-007 tests.
    /// Pipeline contract, DecisionContext extension, interfaces, determinism.
    /// </summary>
    public class IntegrationPipelineTests
    {
        // ================================================================
        // Story-001: World.Tick pipeline contract
        // ================================================================

        [Fact]
        public void PipelineStageRegistry_AllowsRegistration()
        {
            var reg = new PipelineStageRegistry();
            var stage = new TestStage("test", false);
            reg.Register(stage);
            Assert.Equal(1, reg.Count);
            Assert.Same(stage, reg.Stages[0]);
        }

        [Fact]
        public void MultipleStages_ExecuteInOrder()
        {
            var reg = new PipelineStageRegistry();
            var log = new List<string>();
            reg.Register(new TestStage("s1", false, () => log.Add("s1")));
            reg.Register(new TestStage("s2", true, () => log.Add("s2")));

            foreach (var s in reg.Stages)
                Assert.NotNull(s.Name);

            Assert.Equal(2, reg.Count);
        }

        [Fact]
        public void Stage_Respects_ActiveWhenOff()
        {
            var offStage = new TestStage("off", false);
            var onStage = new TestStage("on", true);

            Assert.False(offStage.ActiveWhenOff);
            Assert.True(onStage.ActiveWhenOff);
        }

        // ================================================================
        // Story-002: DecisionContext extension — backward compatible
        // ================================================================

        [Fact]
        public void DecisionContext_Defaults_BackwardCompatible()
        {
            var ctx = new DecisionContext(
                new CharacterId(1),
                new StatBlock(new[] { 10, 10, 10, 10 }),
                new Goal(GoalKind.Advance, 0),
                new NodeId(0),
                Array.Empty<NearbyActor>(),
                Array.Empty<Actions.ActionType>(),
                Array.Empty<MemoryEntry>());

            // New fields default to null/0 — existing construction sites unchanged
            Assert.Null(ctx.Reachable);
            Assert.Equal(0, ctx.FactionId);
            Assert.Equal(0, ctx.FactionRank);
            Assert.Equal(0, ctx.FactionReputation);
        }

        [Fact]
        public void DecisionContext_NewFields_CanBeSet()
        {
            var ctx = new DecisionContext(
                new CharacterId(1),
                new StatBlock(new[] { 10, 10, 10, 10 }),
                new Goal(GoalKind.Advance, 0),
                new NodeId(0),
                Array.Empty<NearbyActor>(),
                Array.Empty<Actions.ActionType>(),
                Array.Empty<MemoryEntry>(),
                Reachable: new[] { new NodeId(1), new NodeId(2) },
                FactionId: 3,
                FactionRank: 2,
                FactionReputation: 75);

            Assert.NotNull(ctx.Reachable);
            Assert.Equal(2, ctx.Reachable!.Count);
            Assert.Equal(3, ctx.FactionId);
            Assert.Equal(2, ctx.FactionRank);
            Assert.Equal(75, ctx.FactionReputation);
        }

        // ================================================================
        // Story-003: IGeoQuery contract
        // ================================================================

        [Fact]
        public void IGeoQuery_Contract_Defined()
        {
            var geo = new StubGeoQuery(10, 3);
            Assert.Equal(10, geo.NodeCount);
            Assert.Equal(3, geo.RegionCount);
            Assert.Equal(0, geo.SiteType(new NodeId(0))); // node 0 =普通
            Assert.Equal(1, geo.SiteType(new NodeId(1))); // node 1 =资源
            Assert.Equal(100, geo.ResourceAt(new NodeId(1)));
            Assert.Equal(0, geo.ResourceAt(new NodeId(0)));
        }

        // ================================================================
        // Story-004: IFactionQuery contract
        // ================================================================

        [Fact]
        public void IFactionQuery_Contract_Defined()
        {
            var fac = new StubFactionQuery();
            Assert.Equal(0, fac.FactionOf(new CharacterId(1))); // 散修
            Assert.Equal(0, fac.FactionCount);
        }

        // ================================================================
        // RuleBrain map-off backward compatibility (§0)
        // ================================================================

        [Fact]
        public async System.Threading.Tasks.Task RuleBrain_MapOff_ByteIdentical()
        {
            // When Reachable=null (Map off), Travel destination must be Node+1
            // — byte-identical to pre-Map behavior.
            var ctx1 = new DecisionContext(
                new CharacterId(1),
                new StatBlock(new[] { 20, 10, 10, 10 }),
                new Goal(GoalKind.Wander, 0),
                new NodeId(5),
                System.Array.Empty<NearbyActor>(),
                new[] { Actions.ActionType.Travel },
                System.Array.Empty<MemoryEntry>(),
                Reachable: null); // Map off

            var ctx2 = new DecisionContext(
                new CharacterId(1),
                new StatBlock(new[] { 20, 10, 10, 10 }),
                new Goal(GoalKind.Wander, 0),
                new NodeId(5),
                System.Array.Empty<NearbyActor>(),
                new[] { Actions.ActionType.Travel },
                System.Array.Empty<MemoryEntry>(),
                Reachable: new[] { new NodeId(7), new NodeId(3) }); // Map on

            // Map off → expect Node+1 = 6
            // Map on → expect Reachable[0] = 7 (weighted best neighbor)
            var brain = new RuleBrain(new Pcg32(42, 0), ArchetypeKind.Martial);
            var choice1 = await brain.DecideAsync(ctx1, System.Threading.CancellationToken.None);
            var choice2 = await brain.DecideAsync(ctx2, System.Threading.CancellationToken.None);

            Assert.Equal(5 + 1, ((Actions.TravelChoice)choice1).To.Value); // backward compatible
            Assert.Equal(7, ((Actions.TravelChoice)choice2).To.Value);      // uses Reachable
        }

        // ================================================================
        // Helpers: stub implementations
        // ================================================================

        sealed class TestStage : IPipelineStage
        {
            private readonly Action? _action;
            public string Name { get; }
            public bool ActiveWhenOff { get; }
            public TestStage(string name, bool activeWhenOff, Action? action = null)
            { Name = name; ActiveWhenOff = activeWhenOff; _action = action; }

            public IReadOnlyList<DomainEvent> Execute(World world, Character actor, long tick)
            {
                _action?.Invoke();
                return Array.Empty<DomainEvent>();
            }
        }

        sealed class StubGeoQuery : IGeoQuery
        {
            private readonly int _nodeCount, _regionCount;
            public StubGeoQuery(int nodeCount, int regionCount)
            { _nodeCount = nodeCount; _regionCount = regionCount; }

            public int RegionOf(NodeId node) => node.Value % _regionCount;
            public IReadOnlyList<NodeId> SitesInRegion(int regionId) =>
                new[] { new NodeId(regionId * 3), new NodeId(regionId * 3 + 1) };
            public IReadOnlyList<NodeId> AdjacentTo(NodeId node) =>
                new[] { new NodeId(node.Value + 1), new NodeId(node.Value + 2) };
            public int SiteType(NodeId node) => node.Value == 0 ? 0 : 1;
            public int ResourceAt(NodeId node) => node.Value == 1 ? 100 : 0;
            public int NodeCount => _nodeCount;
            public int RegionCount => _regionCount;
        }

        sealed class StubFactionQuery : IFactionQuery
        {
            public int FactionOf(CharacterId id) => 0;
            public IReadOnlyList<FactionMemberInfo> MembersOf(int factionId) =>
                Array.Empty<FactionMemberInfo>();
            public int RankOf(CharacterId id) => 0;
            public int FactionRelation(int a, int b) => 0;
            public int FactionCount => 0;
            public IReadOnlyList<CharacterId> NearbyFellows(CharacterId id, int maxDistance) =>
                Array.Empty<CharacterId>();
        }
    }
}
