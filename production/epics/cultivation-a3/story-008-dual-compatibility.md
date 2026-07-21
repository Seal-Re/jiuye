# Story 008: 双修兼容矩阵+bandwidth

> **Epic**: cultivation-a3 | **Status**: Complete — EPIC Done @2026-07-17 (Sprint 9) | **Type**: Logic | **Estimate**: 0.3d
> **Depends**: story-007

## Context
双修路线必须满足兼容性——雷修+鬼修排斥，剑修+体修兼容等。

## Acceptance Criteria
- [ ] 8.1 `CultivationPathDef` 新增 `Excludes` 字段（List\<string\>）——排斥路线 ID 列表
- [ ] 8.2 `CanDualCultivate(pathA, pathB)` 判据——双方不互斥
- [ ] 8.3 标准排斥对：雷修↔鬼修，剑修↔魔修（正邪不两立），佛修↔魔修
- [ ] 8.4 Bandwidth 公式：`bandwidth = 50 + Insight * 2`（整数，纯计算）
- [ ] 8.5 确定性——兼容性检查纯静态（无 RNG）

## Out of Scope
- 战力公式（→ story-009）
