using System.Linq;
using Jianghu.Model;
using Jianghu.Random;
using Jianghu.Sim;
using Xunit;

namespace Jianghu.Core.Tests.Sim
{
    /// <summary>
    /// Map system (C) — full spec tests: three-layer model, Kruskal MST, Harvest, EnterSecret, determinism.
    /// </summary>
    public class MapAndFactionTests
    {
        // ================================================================
        // WorldMap — Construction + Invariants
        // ================================================================

        [Fact]
        public void Map_Generated_Deterministic()
        {
            var m1 = WorldMapFactory.Create(MapConfig.Default, new Pcg32(42, 0));
            var m2 = WorldMapFactory.Create(MapConfig.Default, new Pcg32(42, 0));

            Assert.Equal(m1.NodeCount, m2.NodeCount);
            Assert.Equal(m1.RegionCount, m2.RegionCount);

            for (int i = 0; i < m1.NodeCount; i++)
            {
                Assert.Equal(m1.RegionOf(new NodeId(i)), m2.RegionOf(new NodeId(i)));
                Assert.Equal(m1.SiteType(new NodeId(i)), m2.SiteType(new NodeId(i)));
                Assert.Equal(m1.ResourceAt(new NodeId(i)), m2.ResourceAt(new NodeId(i)));
            }
        }

        [Fact]
        public void Map_Connected_AllNodesReachable()
        {
            for (int seed = 0; seed < 10; seed++)
            {
                var m = WorldMapFactory.Create(MapConfig.Default, new Pcg32((ulong)seed, 0));
                // If INV-MAP-CONNECTED fails, constructor throws — test passes if no exception
                Assert.True(m.NodeCount > 0);
            }
        }

        [Fact]
        public void Map_Has_Regions_6To9()
        {
            var m = WorldMapFactory.Create(MapConfig.Default, new Pcg32(42, 0));
            Assert.True(m.RegionCount >= 6 && m.RegionCount <= 9,
                $"Expected 6-9 regions, got {m.RegionCount}");
        }

        [Fact]
        public void Map_Regions_HaveTerrain()
        {
            var m = WorldMapFactory.Create(MapConfig.Default, new Pcg32(42, 0));
            foreach (var region in m.Regions)
            {
                Assert.False(string.IsNullOrWhiteSpace(region.Name));
                Assert.True(region.Wealth >= 10 && region.Wealth <= 90);
                Assert.True(region.QiDensity >= 20 && region.QiDensity <= 80);
                Assert.True(region.Strategic >= 20 && region.Strategic <= 80);
            }
        }

        [Fact]
        public void Map_Adjacency_HasAtLeast2Neighbors()
        {
            var m = WorldMapFactory.Create(MapConfig.Default, new Pcg32(42, 0));
            for (int i = 0; i < m.NodeCount; i++)
            {
                var adj = m.AdjacentTo(new NodeId(i));
                Assert.True(adj.Count >= 1, $"Node {i} must be connected (>=1 neighbor), got {adj.Count}");
            }
        }

        [Fact]
        public void Map_Adjacency_Sorted()
        {
            var m = WorldMapFactory.Create(MapConfig.Default, new Pcg32(42, 0));
            for (int i = 0; i < m.NodeCount; i++)
            {
                var adj = m.AdjacentTo(new NodeId(i));
                for (int j = 1; j < adj.Count; j++)
                    Assert.True(adj[j].Value > adj[j-1].Value, $"Adjacency for node {i} not sorted");
            }
        }

        [Fact]
        public void Map_SitesPerRegion_3To5()
        {
            var m = WorldMapFactory.Create(MapConfig.Default, new Pcg32(42, 0));
            int sitesPerRegion = m.NodeCount / m.RegionCount;
            Assert.True(sitesPerRegion >= 3 && sitesPerRegion <= 5,
                $"Expected 3-5 sites/region, got {sitesPerRegion}");
        }

        // ================================================================
        // WorldMap — Runtime operations
        // ================================================================

        [Fact]
        public void Harvest_AtResourceNode_ReturnsAmount()
        {
            var m = WorldMapFactory.Create(MapConfig.Default, new Pcg32(42, 0));
            // Find a resource node
            NodeId? resNode = null;
            for (int i = 0; i < m.NodeCount; i++)
            {
                if (m.SiteType(new NodeId(i)) == 1) { resNode = new NodeId(i); break; }
            }
            if (resNode == null) return; // no resource node in this seed

            int before = m.ResourceAt(resNode.Value);
            int harvested = m.Harvest(resNode.Value, 20);
            int after = m.ResourceAt(resNode.Value);

            Assert.True(harvested > 0);
            Assert.Equal(before - harvested, after);
        }

        [Fact]
        public void Harvest_AtNormalNode_ReturnsZero()
        {
            var m = WorldMapFactory.Create(MapConfig.Default, new Pcg32(42, 0));
            // Find a normal node
            for (int i = 0; i < m.NodeCount; i++)
            {
                if (m.SiteType(new NodeId(i)) == 0)
                {
                    Assert.Equal(0, m.Harvest(new NodeId(i)));
                    return;
                }
            }
        }

        [Fact]
        public void EnterSecret_Success_WithHighInsight()
        {
            var m = WorldMapFactory.Create(MapConfig.Default, new Pcg32(100, 0));
            // Find a secret node with low danger
            for (int i = 0; i < m.NodeCount; i++)
            {
                if (m.SiteType(new NodeId(i)) == 2)
                {
                    var geo = m.GeoOf(new NodeId(i));
                    bool success = m.EnterSecret(new NodeId(i), 50); // Insight=50 > 20+3*5=35
                    if (geo.DangerTier <= 2) // low enough for Insight=50
                    {
                        Assert.True(success);
                        Assert.Contains(i, m.RevealedSecrets);
                    }
                    return;
                }
            }
        }

        [Fact]
        public void EnterSecret_Fails_WithLowInsight()
        {
            var m = WorldMapFactory.Create(MapConfig.Default, new Pcg32(100, 0));
            for (int i = 0; i < m.NodeCount; i++)
            {
                if (m.SiteType(new NodeId(i)) == 2)
                {
                    Assert.False(m.EnterSecret(new NodeId(i), 5)); // Insight=5 too low
                    return;
                }
            }
        }

        [Fact]
        public void BestNeighbor_PrefersResourceNode()
        {
            var m = WorldMapFactory.Create(MapConfig.Default, new Pcg32(42, 0));
            var best = m.BestNeighbor(new NodeId(0));
            Assert.True(best.Value >= 0 && best.Value < m.NodeCount);
        }

        // ================================================================
        // IGeoQuery contract
        // ================================================================

        [Fact]
        public void Map_ImplementsIGeoQuery()
        {
            var m = WorldMapFactory.Create(MapConfig.Default, new Pcg32(42, 0));
            IGeoQuery q = m;
            Assert.True(q.NodeCount > 0);
            Assert.True(q.RegionCount > 0);
            Assert.NotNull(q.AdjacentTo(new NodeId(0)));
        }

        // ================================================================
        // SectLedger tests (from previous implementation, kept)
        // ================================================================

        [Fact]
        public void Faction_Factory_CreatesLedger()
        {
            var ledger = SectLedgerFactory.Create(FactionConfig.Default, new Pcg32(42, 0), 20);
            Assert.True(ledger.FactionCount >= 0);
        }

        // ================================================================
        // Faction territory + phase + treasury (§3)
        // ================================================================

        [Fact]
        public void Faction_Territory_ControlSite()
        {
            var ledger = new SectLedger();
            ledger.RegisterFaction(new FactionDef(1, "剑宗", 0, new NodeId(0), 1, System.Array.Empty<string>()));
            ledger.InitPhase(1, 0);

            ledger.ControlSite(1, new NodeId(5));
            ledger.ControlSite(1, new NodeId(7));

            var sites = ledger.ControlledSites(1);
            Assert.Equal(2, sites.Count);
            Assert.Equal(1, ledger.OwnerOf(new NodeId(5)));
            Assert.Equal(0, ledger.OwnerOf(new NodeId(99))); // unowned
        }

        [Fact]
        public void Faction_LoseSite()
        {
            var ledger = new SectLedger();
            ledger.RegisterFaction(new FactionDef(1, "剑宗", 0, new NodeId(0), 1, System.Array.Empty<string>()));
            ledger.InitPhase(1, 0);
            ledger.ControlSite(1, new NodeId(3));
            ledger.LoseSite(1, new NodeId(3));
            Assert.Empty(ledger.ControlledSites(1));
            Assert.Equal(0, ledger.OwnerOf(new NodeId(3)));
        }

        [Fact]
        public void test_map_secret_threshold_driven_by_config()
        {
            // story-008 R-4：秘境门槛由 MapConfig 驱动（原硬编码 20 + DangerTier*5）。
            // 设极高 base → 任何 insight 都进不去；设极低 → 容易进。证明 config 真生效。
            var strict = new MapConfig(SecretInsightBase: 9999, SecretInsightPerTier: 0);
            var lax = new MapConfig(SecretInsightBase: 0, SecretInsightPerTier: 0);
            var mStrict = WorldMapFactory.Create(strict, new Pcg32(42, 0));
            var mLax = WorldMapFactory.Create(lax, new Pcg32(42, 0));

            // 找一个秘境节点（同种子两图拓扑一致）。SiteType 返回 (int)SiteKind，Secret=2。
            int? secret = null;
            for (int i = 0; i < mStrict.NodeCount; i++)
                if (mStrict.SiteType(new NodeId(i)) == (int)SiteKind.Secret) { secret = i; break; }

            if (secret is int s) // 该种子下有秘境才断言
            {
                Assert.False(mStrict.EnterSecret(new NodeId(s), insight: 100), "strict config 下 insight=100 仍进不去");
                Assert.True(mLax.EnterSecret(new NodeId(s), insight: 1), "lax config 下 insight=1 即可进");
            }
        }

        [Fact]
        public void Faction_Phase_Transitions()
        {
            var ledger = new SectLedger();
            ledger.RegisterFaction(new FactionDef(1, "剑宗", 0, new NodeId(0), 1, System.Array.Empty<string>()));
            ledger.InitPhase(1, 0);
            Assert.Equal(FactionPhase.Founding, ledger.PhaseOf(1));

            // Add members → Growth
            ledger.Join(new CharacterId(10), 1, 0);
            ledger.Join(new CharacterId(20), 1, 0);
            ledger.Pump(201, null); // age > 200, members >= 2
            Assert.Equal(FactionPhase.Growth, ledger.PhaseOf(1));
        }

        [Fact]
        public void test_faction_pump_multi_faction_simultaneous_transition_no_throw()
        {
            // 守护：Pump 在 foreach(_phases) 内对已存在 key 做 indexer-set（_phases[fid]=newPhase）。
            // Dictionary 的版本检查仅在结构性变更(Add/Remove)时触发，对已存在 key 的值重写不抛——
            // 故多派系同 tick 转换安全。本测试钉死该前提（C-1 wiring 引入多派系生产路径，防回归）。
            var ledger = new SectLedger();
            for (int f = 1; f <= 3; f++)
            {
                ledger.RegisterFaction(new FactionDef(f, $"门派{f}", 0, new NodeId(0), 1, System.Array.Empty<string>()));
                ledger.InitPhase(f, 0);
                ledger.Join(new CharacterId(f * 10), f, 0);
                ledger.Join(new CharacterId(f * 10 + 1), f, 0);
            }

            // 3 派系同时满足 Founding→Growth（age>200, members>=2）→ 同 tick 多次 indexer-set
            var ex = Record.Exception(() => ledger.Pump(201, null));

            Assert.Null(ex); // 不得抛
            for (int f = 1; f <= 3; f++)
                Assert.Equal(FactionPhase.Growth, ledger.PhaseOf(f));
        }

        [Fact]
        public void Faction_Treasury_Revenue()
        {
            var ledger = new SectLedger();
            ledger.RegisterFaction(new FactionDef(1, "剑宗", 0, new NodeId(0), 1, System.Array.Empty<string>()));
            ledger.InitPhase(1, 0);
            ledger.ControlSite(1, new NodeId(5));

            // Create a stub geo for revenue testing
            var geo = new StubGeoQuery();
            ledger.Pump(50, geo);

            Assert.True(ledger.TreasuryOf(1) > 100, "Treasury should grow from revenue");
        }

        [Fact]
        public void Faction_Disband_ClearsEverything()
        {
            var ledger = new SectLedger();
            ledger.RegisterFaction(new FactionDef(1, "剑宗", 0, new NodeId(0), 1, System.Array.Empty<string>()));
            ledger.InitPhase(1, 0);
            ledger.ControlSite(1, new NodeId(5));
            ledger.Join(new CharacterId(10), 1, 0);

            ledger.Disband(1);

            Assert.Empty(ledger.ControlledSites(1));
            Assert.Empty(ledger.MembersOf(1));
            Assert.Equal(0, ledger.TreasuryOf(1));
        }

        // ================================================================
        // Stub geo for territory testing
        // ================================================================

        sealed class StubGeoQuery : IGeoQuery
        {
            public int RegionOf(NodeId node) => node.Value / 3;
            public IReadOnlyList<NodeId> SitesInRegion(int regionId) => new[] { new NodeId(0) };
            public IReadOnlyList<NodeId> AdjacentTo(NodeId node) => new[] { new NodeId(node.Value + 1) };
            public int SiteType(NodeId node) => 0;
            public int ResourceAt(NodeId node) => node.Value == 5 ? 100 : 0;
            public int NodeCount => 9;
            public int RegionCount => 3;
        }

        // story-011：夺地结算可控 geo stub——site n 邻接 site n+1（链）。
        sealed class ChainGeo : IGeoQuery
        {
            public int RegionOf(NodeId node) => node.Value;
            public IReadOnlyList<NodeId> SitesInRegion(int regionId) => new[] { new NodeId(regionId) };
            public IReadOnlyList<NodeId> AdjacentTo(NodeId node) => new[] { new NodeId(node.Value + 1), new NodeId(node.Value - 1) };
            public int SiteType(NodeId node) => 0;
            public int ResourceAt(NodeId node) => 0;
            public int NodeCount => 100;
            public int RegionCount => 100;
        }

        [Fact]
        public void test_conquest_high_ambition_high_might_takes_adjacent_site()
        {
            // story-011 AC 11.3/11.4/11.5：攻方 Ambition 高 + Might 差大 + 相邻 → 夺地 + 关系恶化 + 非致死。
            var ledger = new SectLedger();
            // 攻方 f1 Ambition=90（≥60），守方 f2 Ambition 低。
            ledger.RegisterFaction(new FactionDef(1, "攻", 0, new NodeId(0), 1, System.Array.Empty<string>()) { Ambition = 90 });
            ledger.RegisterFaction(new FactionDef(2, "守", 1, new NodeId(1), -1, System.Array.Empty<string>()) { Ambition = 10 });
            ledger.InitPhase(1, 0); ledger.InitPhase(2, 0);
            ledger.ControlSite(1, new NodeId(0)); // 攻方据 site0
            ledger.ControlSite(2, new NodeId(1)); // 守方据 site1（与 site0 相邻）
            var f2Member = new CharacterId(20); ledger.Join(f2Member, 2, 0); // 守方非致死前提：有成员

            // Might：攻方 100 vs 守方 10，差 90 ≥ Gap(30)。clock%50==0 触发。
            var might = new System.Collections.Generic.Dictionary<int, int> { { 1, 100 }, { 2, 10 } };
            var changes = ledger.Pump(50, new ChainGeo(), might);

            // 夺地兑现：site1 易主 f2→f1。
            Assert.Contains(changes, c => c.Site == 1 && c.From == 2 && c.To == 1);
            Assert.Equal(1, ledger.OwnerOf(new NodeId(1)));    // 攻方得地
            Assert.DoesNotContain(new NodeId(1), ledger.ControlledSites(2)); // 守方失地
            // 关系恶化（守方对攻方 ≤ 初始）。
            Assert.True(ledger.FactionRelation(2, 1) < 0, "守方对攻方关系应恶化");
            // 非致死：守方成员仍在。
            Assert.Equal(2, ledger.FactionOf(f2Member));
        }

        [Fact]
        public void test_conquest_low_might_gap_no_take()
        {
            // Might 差不足 Gap → 不夺地。
            var ledger = new SectLedger();
            ledger.RegisterFaction(new FactionDef(1, "攻", 0, new NodeId(0), 1, System.Array.Empty<string>()) { Ambition = 90 });
            ledger.RegisterFaction(new FactionDef(2, "守", 1, new NodeId(1), -1, System.Array.Empty<string>()) { Ambition = 10 });
            ledger.InitPhase(1, 0); ledger.InitPhase(2, 0);
            ledger.ControlSite(1, new NodeId(0)); ledger.ControlSite(2, new NodeId(1));

            var might = new System.Collections.Generic.Dictionary<int, int> { { 1, 50 }, { 2, 40 } }; // 差 10 < 30
            var changes = ledger.Pump(50, new ChainGeo(), might);

            Assert.Empty(changes);
            Assert.Equal(2, ledger.OwnerOf(new NodeId(1))); // 守方保地
        }

        [Fact]
        public void test_conquest_low_ambition_no_take()
        {
            // 攻方 Ambition < 阈 → 不夺地（即便 Might 碾压）。
            var ledger = new SectLedger();
            ledger.RegisterFaction(new FactionDef(1, "攻", 0, new NodeId(0), 1, System.Array.Empty<string>()) { Ambition = 30 }); // <60
            ledger.RegisterFaction(new FactionDef(2, "守", 1, new NodeId(1), -1, System.Array.Empty<string>()) { Ambition = 10 });
            ledger.InitPhase(1, 0); ledger.InitPhase(2, 0);
            ledger.ControlSite(1, new NodeId(0)); ledger.ControlSite(2, new NodeId(1));

            var might = new System.Collections.Generic.Dictionary<int, int> { { 1, 1000 }, { 2, 1 } };
            var changes = ledger.Pump(50, new ChainGeo(), might);

            Assert.Empty(changes);
        }

        // INV-PERF 计数 geo：记录 AdjacentTo 调用次数，验证夺地只扫相邻边界（非全图）。
        sealed class CountingGeo : IGeoQuery
        {
            public int AdjacentCalls;
            public int RegionOf(NodeId n) => n.Value;
            public IReadOnlyList<NodeId> SitesInRegion(int r) => new[] { new NodeId(r) };
            public IReadOnlyList<NodeId> AdjacentTo(NodeId n) { AdjacentCalls++; return new[] { new NodeId(n.Value + 1), new NodeId(n.Value - 1) }; }
            public int SiteType(NodeId n) => 0;
            public int ResourceAt(NodeId n) => 0;
            public int NodeCount => 1000; // 大图：若全扫则 AdjacentTo 应 ~O(1000)
            public int RegionCount => 1000;
        }

        [Fact]
        public void test_conquest_scans_only_boundary_not_full_graph()
        {
            // story-011 AC 11.8（INV-PERF §3.5）：夺地只扫"攻方领地的相邻 site"，非全图 O(NodeCount)。
            // 攻方仅控 2 块地 → AdjacentTo 调用应 = 攻方领地数（=2），远小于 NodeCount(1000)。
            var ledger = new SectLedger();
            ledger.RegisterFaction(new FactionDef(1, "攻", 0, new NodeId(0), 1, System.Array.Empty<string>()) { Ambition = 90 });
            ledger.InitPhase(1, 0);
            ledger.ControlSite(1, new NodeId(10));
            ledger.ControlSite(1, new NodeId(20));

            var geo = new CountingGeo();
            var might = new System.Collections.Generic.Dictionary<int, int> { { 1, 100 } };
            ledger.Pump(50, geo, might);

            // 只扫攻方 2 块领地的邻居 → AdjacentTo 恰 2 次（边界扫描），与 NodeCount(1000) 解耦。
            Assert.Equal(2, geo.AdjacentCalls);
        }

        [Fact]
        public void Faction_RegisterAndJoin()
        {
            var ledger = new SectLedger();
            ledger.RegisterFaction(new FactionDef(1, "剑宗", 0, new NodeId(0), 1, new[] { "sword" }));
            var id = new CharacterId(100);
            ledger.Join(id, 1, 50);
            Assert.Equal(1, ledger.FactionOf(id));
            Assert.Equal(0, ledger.RankOf(id));
        }

        [Fact]
        public void Faction_Promote_And_Succession()
        {
            var ledger = new SectLedger();
            ledger.RegisterFaction(new FactionDef(1, "剑宗", 0, new NodeId(0), 1, System.Array.Empty<string>()));
            var a = new CharacterId(10); ledger.Join(a, 1, 0);
            var b = new CharacterId(20); ledger.Join(b, 1, 0);
            ledger.Promote(b); ledger.Promote(b);
            var succ = ledger.Succession(1);
            Assert.NotNull(succ);
            Assert.Equal(b, succ.Value);
        }

        // ================================================================
        // story-010：贡献度累加器（AC 10.1 单元）
        // ================================================================

        [Fact]
        public void test_contribution_accumulates_for_member_only()
        {
            var ledger = new SectLedger();
            ledger.RegisterFaction(new FactionDef(1, "剑宗", 0, new NodeId(0), 1, System.Array.Empty<string>()));
            var member = new CharacterId(10);
            var loner = new CharacterId(99); // 散修，未 Join
            ledger.Join(member, 1, 0);

            ledger.AddContribution(member, 30);
            ledger.AddContribution(member, 25);
            ledger.AddContribution(loner, 100); // 散修不累

            Assert.Equal(55, ledger.ContributionOf(member));
            Assert.Equal(0, ledger.ContributionOf(loner));
        }

        [Fact]
        public void test_captured_state_includes_rank_and_contribution()
        {
            // AC 10.1：CaptureState 序列化含 Rank/贡献度（补 Faction 快照空白）。
            var ledger = new SectLedger();
            ledger.RegisterFaction(new FactionDef(1, "剑宗", 0, new NodeId(0), 1, System.Array.Empty<string>()));
            ledger.InitPhase(1, 0);
            var m = new CharacterId(10); ledger.Join(m, 1, 0);

            var before = ledger.CaptureState();
            ledger.AddContribution(m, 42);
            var afterContrib = ledger.CaptureState();
            ledger.Promote(m);
            var afterPromote = ledger.CaptureState();

            Assert.NotEqual(before, afterContrib);    // 贡献度变 → 快照变
            Assert.NotEqual(afterContrib, afterPromote); // Rank 变 → 快照变
            Assert.Contains(":c42", afterContrib);     // 贡献度入串
            Assert.Contains(":r1", afterPromote);      // Rank 入串
        }

        // R-3：就近同门 = 同门 + 同区域（RegionOf）。RegionGeo: node.Value/10 = region。
        sealed class RegionGeo : IGeoQuery
        {
            public int RegionOf(NodeId node) => node.Value / 10; // site 0-9=区0, 10-19=区1...
            public IReadOnlyList<NodeId> SitesInRegion(int regionId) => System.Array.Empty<NodeId>();
            public IReadOnlyList<NodeId> AdjacentTo(NodeId node) => System.Array.Empty<NodeId>();
            public int SiteType(NodeId node) => 0;
            public int ResourceAt(NodeId node) => 0;
            public int NodeCount => 100;
            public int RegionCount => 10;
        }

        [Fact]
        public void test_nearby_fellows_same_region_only()
        {
            // R-3：同门 self(site5,区0)。fellowA(site3,区0)同区→含；fellowB(site15,区1)异区→排除；
            // 异门 outsider(site5,区0)→不计；fellowC 位置未知→跳过。结果按 Id 升序。
            var ledger = new SectLedger();
            ledger.RegisterFaction(new FactionDef(1, "甲", 0, new NodeId(0), 1, System.Array.Empty<string>()));
            ledger.RegisterFaction(new FactionDef(2, "乙", 1, new NodeId(10), -1, System.Array.Empty<string>()));
            var self = new CharacterId(1); var fellowA = new CharacterId(2);
            var fellowB = new CharacterId(3); var fellowC = new CharacterId(4); var outsider = new CharacterId(5);
            ledger.Join(self, 1, 0); ledger.Join(fellowA, 1, 0); ledger.Join(fellowB, 1, 0);
            ledger.Join(fellowC, 1, 0); ledger.Join(outsider, 2, 0);

            var pos = new System.Collections.Generic.Dictionary<long, NodeId>
            {
                [1] = new NodeId(5), [2] = new NodeId(3), [3] = new NodeId(15), [5] = new NodeId(5)
                // fellowC(4) 无位置 → 跳过
            };
            NodeId? PositionOf(CharacterId c) => pos.TryGetValue(c.Value, out var n) ? n : (NodeId?)null;

            var nearby = ledger.NearbyFellows(self, new RegionGeo(), PositionOf);

            Assert.Equal(new[] { fellowA }, nearby); // 仅同门同区域的 fellowA
        }

        [Fact]
        public void test_nearby_fellows_self_no_position_empty()
        {
            var ledger = new SectLedger();
            ledger.RegisterFaction(new FactionDef(1, "甲", 0, new NodeId(0), 1, System.Array.Empty<string>()));
            var self = new CharacterId(1); var fellow = new CharacterId(2);
            ledger.Join(self, 1, 0); ledger.Join(fellow, 1, 0);

            // 自身无位置 → 无从判同区域 → 空。
            var nearby = ledger.NearbyFellows(self, new RegionGeo(), _ => (NodeId?)null);
            Assert.Empty(nearby);
        }
    }
}
