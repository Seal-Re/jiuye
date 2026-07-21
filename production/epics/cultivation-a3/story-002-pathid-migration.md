# Story 002: PathId 迁移+CultivationState 改造

> **Epic**: cultivation-a3 | **Status**: Complete — EPIC Done @2026-07-17 (Sprint 9) | **Type**: Logic | **Estimate**: 0.3d
> **Depends**: story-001

## Context
转职需要修改 `CultivationState.PathId`（当前为 `{ get; init; }`，不可变）。需改造为 settable 并实现安全的 PathId 迁移逻辑。

## Acceptance Criteria
- [ ] 2.1 `CultivationState.PathId` 改为 `{ get; set; }`（保留 init 语义的 Clone 不变）
- [ ] 2.2 `MigratePathId(TransitionDef)` 方法——安全迁移：保留 carryover 资源/flag，重置 realm 映射
- [ ] 2.3 Realm 映射：新路起始境界 = 旧路对应境界（非归零）——按 UnifiedTierOf 对齐
- [ ] 2.4 Carryover 规则：保留标记的 resources/arts，其余丢弃
- [ ] 2.5 确定性：同 TransitionDef + 同状态 → 同迁移结果
- [ ] 2.6 off 模式不触发迁移

## Out of Scope
- 奇遇触发（→ story-005/008）
- 实际跃迁数据（→ story-003）
