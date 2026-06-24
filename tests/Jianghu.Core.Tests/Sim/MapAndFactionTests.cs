using System;
using System.Linq;
using Jianghu.Model;
using Jianghu.Random;
using Jianghu.Sim;
using Xunit;

namespace Jianghu.Core.Tests.Sim
{
    /// <summary>
    /// Map system (C) + Faction system (D) foundation tests.
    /// </summary>
    public class MapAndFactionTests
    {
        // ================================================================
        // WorldMap tests
        // ================================================================

        [Fact]
        public void Map_Generated_Deterministic()
        {
            var m1 = new WorldMap(20, 4, new Pcg32(42, 0));
            var m2 = new WorldMap(20, 4, new Pcg32(42, 0));

            Assert.Equal(m1.NodeCount, m2.NodeCount);
            Assert.Equal(m1.RegionCount, m2.RegionCount);

            for (int i = 0; i < 20; i++)
            {
                Assert.Equal(m1.RegionOf(new NodeId(i)), m2.RegionOf(new NodeId(i)));
                Assert.Equal(m1.SiteType(new NodeId(i)), m2.SiteType(new NodeId(i)));
                Assert.Equal(m1.ResourceAt(new NodeId(i)), m2.ResourceAt(new NodeId(i)));
            }
        }

        [Fact]
        public void Map_Adjacency_HasRingAndBridges()
        {
            var m = new WorldMap(20, 4, new Pcg32(42, 0));
            for (int i = 0; i < 20; i++)
            {
                var adj = m.AdjacentTo(new NodeId(i));
                Assert.True(adj.Count >= 2, $"Node {i} should have >=2 neighbors"); // ring minimum
            }
        }

        [Fact]
        public void Map_SitesInRegion_ReturnsCorrectNodes()
        {
            var m = new WorldMap(20, 4, new Pcg32(42, 0));
            for (int r = 0; r < 4; r++)
            {
                var sites = m.SitesInRegion(r);
                foreach (var s in sites)
                    Assert.Equal(r, m.RegionOf(s));
            }
        }

        [Fact]
        public void Map_RevealSecret_Tracks()
        {
            var m = new WorldMap(30, 4, new Pcg32(99, 0));
            var secretNodes = Enumerable.Range(0, 30)
                .Where(i => m.SiteType(new NodeId(i)) == 2).ToList();

            if (secretNodes.Count > 0)
            {
                var node = new NodeId(secretNodes[0]);
                Assert.False(m.RevealedSecrets.Contains(node.Value));
                m.RevealSecret(node);
                Assert.True(m.RevealedSecrets.Contains(node.Value));
            }
        }

        [Fact]
        public void Map_ImplementsIGeoQuery()
        {
            var m = new WorldMap(10, 3, new Pcg32(42, 0));
            IGeoQuery q = m;
            Assert.True(q.NodeCount > 0);
            Assert.True(q.RegionCount > 0);
            Assert.NotNull(q.AdjacentTo(new NodeId(0)));
        }

        // ================================================================
        // SectLedger tests
        // ================================================================

        [Fact]
        public void Faction_RegisterAndJoin()
        {
            var ledger = new SectLedger();
            var def = new FactionDef(1, "剑宗", 0, new NodeId(0), 1, new[] { "sword_tag" });
            ledger.RegisterFaction(def);

            var id = new CharacterId(100);
            ledger.Join(id, 1, 50);

            Assert.Equal(1, ledger.FactionOf(id));
            Assert.Equal(0, ledger.RankOf(id)); // disciple
            Assert.Equal(1, ledger.FactionCount);
        }

        [Fact]
        public void Faction_PromoteIncreasesRank()
        {
            var ledger = new SectLedger();
            ledger.RegisterFaction(new FactionDef(1, "剑宗", 0, new NodeId(0), 1, Array.Empty<string>()));
            var id = new CharacterId(100);
            ledger.Join(id, 1, 50);

            ledger.Promote(id);
            Assert.Equal(1, ledger.RankOf(id)); // 执事

            ledger.Promote(id);
            Assert.Equal(2, ledger.RankOf(id)); // 长老

            ledger.Promote(id);
            Assert.Equal(3, ledger.RankOf(id)); // 掌门

            ledger.Promote(id);
            Assert.Equal(3, ledger.RankOf(id)); // 掌门 max
        }

        [Fact]
        public void Faction_Leave_RemovesMembership()
        {
            var ledger = new SectLedger();
            ledger.RegisterFaction(new FactionDef(1, "剑宗", 0, new NodeId(0), 1, Array.Empty<string>()));
            var id = new CharacterId(100);
            ledger.Join(id, 1, 50);

            ledger.Leave(id);
            Assert.Equal(0, ledger.FactionOf(id)); // 散修
        }

        [Fact]
        public void Faction_MembersOf_SortedByRank()
        {
            var ledger = new SectLedger();
            ledger.RegisterFaction(new FactionDef(1, "剑宗", 0, new NodeId(0), 1, Array.Empty<string>()));

            var a = new CharacterId(1); ledger.Join(a, 1, 0);
            var b = new CharacterId(2); ledger.Join(b, 1, 0);
            ledger.Promote(b);
            var c = new CharacterId(3); ledger.Join(c, 1, 0);
            ledger.Promote(c); ledger.Promote(c); // rank 2

            var members = ledger.MembersOf(1);
            Assert.Equal(3, members.Count);
            Assert.Equal(2, members[0].Rank); // highest first
            Assert.Equal(1, members[1].Rank);
            Assert.Equal(0, members[2].Rank);
        }

        [Fact]
        public void Faction_Succession_ReturnsHighestRank()
        {
            var ledger = new SectLedger();
            ledger.RegisterFaction(new FactionDef(1, "剑宗", 0, new NodeId(0), 1, Array.Empty<string>()));

            var a = new CharacterId(10); ledger.Join(a, 1, 0);
            var b = new CharacterId(20); ledger.Join(b, 1, 0);
            ledger.Promote(b); ledger.Promote(b); // rank 2

            var successor = ledger.Succession(1);
            Assert.NotNull(successor);
            Assert.Equal(b, successor.Value); // highest rank
        }

        [Fact]
        public void Faction_Disband_RemovesAllMembers()
        {
            var ledger = new SectLedger();
            ledger.RegisterFaction(new FactionDef(1, "剑宗", 0, new NodeId(0), 1, Array.Empty<string>()));
            ledger.Join(new CharacterId(1), 1, 0);
            ledger.Join(new CharacterId(2), 1, 0);

            ledger.Disband(1);
            Assert.Equal(0, ledger.MembersOf(1).Count);
        }

        [Fact]
        public void Faction_Relation_SetAndGet()
        {
            var ledger = new SectLedger();
            ledger.SetRelation(1, 2, -50);
            Assert.Equal(-50, ledger.FactionRelation(1, 2));
            Assert.Equal(0, ledger.FactionRelation(2, 1)); // 单向
        }
    }
}
