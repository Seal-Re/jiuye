# Epic: 修炼 A.1 余项

**Layer**: Core
**Status**: Complete（2026-07-17 — 全 DoD 已实现；A1.4 在 combat-fullstruct-003 解阻塞）
**GDD**: design/gdd/xxx.md（P8 补）或 — ；深度源 A2-FINAL/A123 spec
**Governing ADRs**: None yet（P8 增量补）
**Engine Risk**: LOW（.NET 8 纯整数）
**Created**: 2026-06-15（迁自 TASKS.md）

## Summary
修炼 A.1 余项——10 态修炼流程 / 4 劫 / 5 失败 / 寿元延寿。

## Scope
- 10 态修炼流程
- 4 种劫
- 5 种失败
- 寿元延寿

## Dependencies
**Unblocked by**: balance-cross（A1.4 辅助路 UT 需 INV-CROSS 对拍测得）
**Blocks**: 无

含 **A1.4 辅助路 UT 战斗当量重锚 = blocked**（阻塞点：境界竖切 plan 列为 task 被静默 defer 已转 blocked；原因：辅助路真实战斗当量 UT 需 INV-CROSS 对拍测得；依赖 balance-cross，且本 epic 寿元/劫依赖 A1.4）。

## Definition of Done
- [x] 10 态修炼流程实现 + 测试 — `CultivationPhase.cs` + `CultivationPhaseTests.cs`（97 绿子集）
- [x] 4 种劫实现 + 测试 — `TribulationResolver.cs` + `TribulationResolverTests.cs`
- [x] 5 种失败实现 + 测试 — `LifespanAndFailure.cs` + `LifespanAndFailureTests.cs`
- [x] 寿元延寿实现 + 测试 — `SeclusionFormulas.cs` + `BreakAidAndSeclusionTests.cs` + `SeclusionDESTests.cs`

## Notes
A1.4 依赖链不丢（红线 A.8）；A1.4 UT 重锚动作并入 combat-r2 / story-005。
**2026-07-17 结算**：A1.4 辅助路 UT 锚锁在 `combat-fullstruct/story-003`（Complete）已解（Dan≤7, Array≤7, Qixiu≤10, Fu=12）；`combat-r2/story-005` 仅剩 auditor 终验（属 combat-r2 范围，不阻塞本 EPIC close）。
全量 1271 绿。本 EPIC 所有 scope 项已实现并测试覆盖。
