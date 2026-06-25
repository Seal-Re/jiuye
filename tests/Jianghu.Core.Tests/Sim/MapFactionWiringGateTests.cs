using Jianghu.Config;
using Jianghu.Sim;
using Xunit;

namespace Jianghu.Core.Tests.Sim
{
    /// <summary>
    /// C-1 接线门（story-008，闭 CR-2026-06-25 C-1）。
    ///
    /// 验证 Map(C)/Faction(D) 已接入 WorldFactory.CreateInitial + World.Advance：
    /// mapOn/factionOn 时子系统非 null，Advance 每步驱动 Faction.Pump 不抛。
    ///
    /// 注（诚实标注，红线 A.8）：本 story 仅接"工厂构造 + tick-hook 管线"。
    /// 角色→门派的 membership 接线（SectLedgerFactory 不 Join 成员）属更深层，未在本 story 范围，
    /// 故不断言"派系脱离 Founding"（需 memberCount>=2，membership 未接则永 Founding）。
    /// 派系生命周期推进的端到端验证 → 后续 membership 接线 story。
    /// </summary>
    public class MapFactionWiringGateTests
    {
        [Fact]
        public void test_map_faction_on_populates_subsystems()
        {
            // Arrange + Act
            var w = WorldFactory.CreateInitial(2026, LimitsConfig.Default, initialCount: 8,
                                               cultivation: true, mapOn: true, factionOn: true);

            // Assert: 接线后子系统已填充（接线前恒 null = 死代码）
            Assert.NotNull(w.Map);
            Assert.NotNull(w.Faction);
            Assert.True(w.Map!.NodeCount > 0, "Map 应有节点");
            Assert.True(w.Faction!.FactionCount > 0, "Faction 应有门派");
        }

        [Fact]
        public void test_advance_drives_faction_pump_without_throw()
        {
            var w = WorldFactory.CreateInitial(2026, LimitsConfig.Default, initialCount: 8,
                                               cultivation: true, mapOn: true, factionOn: true);

            // Act: 推进多步，每步 Advance 末调 Faction.Pump(Clock, Map)
            var ex = Record.Exception(() =>
            {
                for (int i = 0; i < 200; i++) w.Advance(64);
            });

            // Assert: Pump 被主循环驱动且不抛（多派系同 tick 转换安全）
            Assert.Null(ex);
            // 所有派系仍可查询（生命周期未崩）
            foreach (var fid in w.Faction!.AllFactionIds)
                Assert.True((int)w.Faction.PhaseOf(fid) >= 0);
        }

        [Fact]
        public void test_map_faction_off_default_null()
        {
            // 默认（off）：mapOn/factionOn 缺省 false → 子系统 null（零激活，保 off 逐字节 B.3）
            var w = WorldFactory.CreateInitial(2026, LimitsConfig.Default, initialCount: 8, cultivation: true);
            Assert.Null(w.Map);
            Assert.Null(w.Faction);
        }

        [Fact]
        public void test_membership_assigned_at_construction()
        {
            // story-009 AC 9.2：factionOn 时角色被分配到门派 → FactionOf 返非0，部分门派 memberCount≥2。
            var w = WorldFactory.CreateInitial(2026, LimitsConfig.Default, initialCount: 8,
                                               cultivation: true, factionOn: true);

            int assigned = 0;
            foreach (var c in w.AliveCharacters())
                if (w.Faction!.FactionOf(c.Id) != 0) assigned++;
            Assert.True(assigned > 0, "应有角色被分配到门派");

            // 8 角色分配到 ≤6 门派 → 鸽笼律保证至少一门派 memberCount≥2
            bool anyMultiMember = false;
            foreach (var fid in w.Faction!.AllFactionIds)
                if (w.Faction.MembersOf(fid).Count >= 2) { anyMultiMember = true; break; }
            Assert.True(anyMultiMember, "至少一门派应有≥2成员（生命周期 Growth 前提）");
        }

        [Fact]
        public void test_faction_lifecycle_advances_end_to_end()
        {
            // story-009 AC 9.3：membership 接通后，派系生命周期端到端——跑 200+ 步后
            // 至少一门派脱离 Founding（age>200 + memberCount≥2 → Growth）。
            var w = WorldFactory.CreateInitial(2026, LimitsConfig.Default, initialCount: 8,
                                               cultivation: true, factionOn: true);
            for (int i = 0; i < 400; i++) w.Advance(64);

            bool anyAdvanced = false;
            foreach (var fid in w.Faction!.AllFactionIds)
                if (w.Faction.PhaseOf(fid) != FactionPhase.Founding) { anyAdvanced = true; break; }
            Assert.True(anyAdvanced, "membership 接通后至少一门派应脱离 Founding（生命周期端到端）");
        }
    }
}
