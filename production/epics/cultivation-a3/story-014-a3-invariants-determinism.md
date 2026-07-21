# Story 014: A.3 不变量硬化+确定性

> **Epic**: cultivation-a3 | **Status**: Complete — EPIC Done @2026-07-17 (Sprint 9) | **Type**: Logic | **Estimate**: 0.5d
> **Depends**: story-002, story-010

## Context
A.3 全量硬化闸门——8 个不变量。每不变量有 CI-blocking 断言测试。

## Acceptance Criteria
- [ ] 14.1 **INV-TRANSITION-DET**: 转职迁移确定性——同 TransitionDef+同状态 → 同迁移结果
- [ ] 14.2 **INV-PATHID-CLONE**: PathId 迁移后 Clone 正确（深拷独立）
- [ ] 14.3 **INV-REALM-MAP**: 转职后 realm 不归零——新路 realm 映射到对应境界
- [ ] 14.4 **INV-SLOT-CAP**: 双修 slot ≤ 2（硬钳）
- [ ] 14.5 **INV-EXCLUDES**: 互斥路线不可双修（如 雷修↔鬼修）
- [ ] 14.6 **INV-RISKMOD-COOLDOWN**: 反噬 cooldown 正确——冷却期内不重复触发
- [ ] 14.7 **INV-BANDWIDTH-FORMULA**: bandwidth 公式纯整数 + 上限钳
- [ ] 14.8 **INV-MORAL-TAG**: 正邪标签纯静态判据——无 RNG
- [ ] 14.9 Off 模式——A.3 所有路径不触发
- [ ] 14.10 IL 浮点零 + 全量测试绿
