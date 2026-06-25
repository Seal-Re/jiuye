using Jianghu.Config;
using Jianghu.Sim;
using Xunit;

namespace Jianghu.Core.Tests.Sim
{
    /// <summary>
    /// C-1 接线门占位（CR-2026-06-25 / R-1(b) 决策：标注延期，不接线）。
    ///
    /// 现状：Map(C)/Faction(D) 已建 + 21 个单测绿（见 MapAndFactionTests），但 WorldFactory.CreateInitial
    /// 从不构造它们、World.Advance 从不 tick Faction.Pump → 生产中是死代码（epics/index.md 标 "Built not wired"）。
    ///
    /// 本门 Skip 占位，记录"接线完成"的验收断言。接线 story（R-1(a)）落地时：
    ///   1. 去掉 Skip；
    ///   2. 按下方注释补全断言（WorldFactory 加 mapOn/factionOn 参后即可编译）；
    ///   3. 同时必须复跑 off 逐字节（Determinism/CultivationDeterminismTests）确认 Split(1..4) 流编号不变。
    /// </summary>
    public class MapFactionWiringGateTests
    {
        [Fact(Skip = "C-1 / R-1(b): Map/Faction built but not wired into WorldFactory/Advance — pending wiring story (un-Skip when R-1(a) lands)")]
        public void test_map_faction_wired_into_advance_populates_and_ticks()
        {
            // 接线后应成立的验收断言（当前 API 下 Map/Faction 恒 null，故 Skip）：
            //
            // Arrange: 以 map/faction 开启构造世界
            //   var w = WorldFactory.CreateInitial(seed: 2026, LimitsConfig.Default, initialCount: 8,
            //                                       cultivation: true, mapOn: true, factionOn: true);
            //
            // Assert(初始): 子系统已填充（非 null）
            //   Assert.NotNull(w.Map);
            //   Assert.NotNull(w.Faction);
            //
            // Act: 推进足够步数让派系生命周期推进
            //   for (int i = 0; i < 200; i++) w.Advance(64);
            //
            // Assert(接线生效): Faction.Pump 被主循环驱动 → 至少一个派系脱离 Founding
            //   Assert.Contains(w.Faction!.AllFactionIds, fid => w.Faction.PhaseOf(fid) != FactionPhase.Founding);
            //
            // 占位期不做任何断言（Skip 不执行体）。
            Assert.True(true);
        }
    }
}
