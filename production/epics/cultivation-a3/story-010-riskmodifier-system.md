# Story 010: RiskModifier 反噬系统

> **Epic**: cultivation-a3 | **Status**: Complete — EPIC Done @2026-07-17 (Sprint 9) | **Type**: Logic | **Estimate**: 0.3d
> **Depends**: story-001

## Context
通用反噬插件——转职/双修/邪道速成均可能触发反噬。概率整数 permille，cooldown 机制，不入核心循环（仅装配层）。

## Acceptance Criteria
- [ ] 10.1 `RiskModifier` record：Trigger, ProbabilityPermille, Penalties, Cooldown, ClearCondition
- [ ] 10.2 `RiskModifierRegistry`——数据驱动，加反噬=加数据行
- [ ] 10.3 Probability permille 整数（0-1000），禁浮点
- [ ] 10.4 Cooldown 计时——同 RiskModifier 冷却期内不重复触发
- [ ] 10.5 Penalty 类型：StatDelta, ResourceDrain, InnerDemonGain, ProgressLoss
- [ ] 10.6 确定性——同 seed → 同反噬判定
