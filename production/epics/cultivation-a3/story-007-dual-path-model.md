# Story 007: DualPathDef 数据模型+slotCap

> **Epic**: cultivation-a3 | **Status**: Not Started | **Type**: Logic | **Estimate**: 0.3d
> **Depends**: story-001

## Context
双修——同时修炼两条路线。slotCap 限制最多 2 条（第三条禁开）。

## Acceptance Criteria
- [ ] 7.1 `DualPathState` 类：MainPathId, SecondPathId, Bandwidth(神识带宽), BacklashCounter
- [ ] 7.2 `slotCap = 2`（硬钳：不可超 2 路双修）
- [ ] 7.3 神识带宽（spiritual bandwidth）= base + Insight bonus → 决定第二路线战力贡献上限
- [ ] 7.4 双修状态存入 `CultivationState.Flags`（或独立存储）
- [ ] 7.5 数据驱动——slotCap/bandwidth 为全局常量，非 per-path

## Out of Scope
- 兼容矩阵（→ story-008）
