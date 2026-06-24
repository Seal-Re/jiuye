# Story 001: TransitionDef 数据模型

> **Epic**: cultivation-a3 | **Status**: Not Started | **Type**: Logic | **Estimate**: 0.3d
> **Depends**: cultivation-a2 (done)
> **GDD**: A123 §A.3.1

## Context
A123 §A.3.1 定义 TransitionDef——转职/觉醒/双修的统一跃迁数据模型。数据驱动：加跃迁=加数据行。

## Acceptance Criteria
- [ ] 1.1 `TransitionDef` record：Id, FromPathPred, ToPathId, Gate, Carryover, Cost
- [ ] 1.2 `TransitionKind` 枚举：`{ Transmute, Awaken, DualCultivation }`
- [ ] 1.3 `TransitionGate` 子 record：RealmMin, ResourceReqs, KarmicPredicate（可选）
- [ ] 1.4 `CarryoverRule` 子 record：保留资源/功法/境界映射规则
- [ ] 1.5 `TransitionRegistry`（类似 PathRegistry 模式）
- [ ] 1.6 数据驱动——加跃迁=加数据行

## Out of Scope
- 实际 PathId 迁移执行（→ story-002）
- 标准转职路线数据填充（→ story-003）
