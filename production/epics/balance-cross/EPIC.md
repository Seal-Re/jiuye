# Epic: 平衡标定 INV-CROSS

**Layer**: Core
**Status**: Designed
> ⚠️ 2026-06-26 圆桌订正（红线 A.3/A.4）：此前误标 Done，但 DoD 5 项全空、契约 C1[40,60]% 未兑现。
> 实证：测试仅 `C1Gate_…Within_35_65`（advisory，`KnownViolationBaseline=47/48` 钉为已知违规放行），
> 全仓无 `Within_40_60` 测试；story-003（[40,60]% 硬闸门）git `76a9a17` 证实 Deferred。
> 真实态 = balance-001/002 派生视图+advisory gate 已建；balance-003 收敛硬闸门未做。详见 docs/reports/宏观对账圆桌纪要-2026-06-26.md §1。
**GDD**: design/gdd/xxx.md（P8 补）或 — ；深度源 docs/legacy-specs/specs/2026-06-14-v1.2-B5-平衡标定INV-CROSS-design.md
**Governing ADRs**: None yet（P8 增量补）
**Engine Risk**: LOW（.NET 8 纯整数）
**Created**: 2026-06-15（迁自 TASKS.md）

## Summary
平衡标定 INV-CROSS（跨路战力当量）——🔴 最大功能缺口，同 UT 24-70× 失衡。

## Scope
- INV-CROSS 契约 C1：战斗路同 UT 胜率 [40,60]%
- C2：碾压单调
- C3：辅助路豁免但 UT 诚实
- 解析校准 mul = target×10 / BaseSum
- 辅助路 UT 重锚解 A1.4

## Dependencies
**Unblocked by**: —
**Blocks**: combat-r2 / story-005（辅助路 UT 重锚）+ cultivation-a1-rest（寿元/劫）

## Definition of Done
- [ ] INV-CROSS 契约 C1 同 UT 胜率 gate（[40,60]%）
- [ ] C2 碾压单调
- [ ] C3 辅助路豁免但 UT 诚实
- [ ] 解析校准 mul = target×10 / BaseSum
- [ ] 辅助路 UT 锚

## Notes
设计完 sha 336280d，范围限标定。
