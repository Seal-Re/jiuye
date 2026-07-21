# Story 006: 觉醒→功法/战力解锁

> **Epic**: cultivation-a3 | **Status**: Complete — EPIC Done @2026-07-17 (Sprint 9) | **Type**: Logic | **Estimate**: 0.3d
> **Depends**: story-005

## Context
觉醒成功后解锁高阶功法类别/战斗模块/战力加成。数据驱动：不同觉醒解锁不同内容。

## Acceptance Criteria
- [ ] 6.1 觉醒后 `st.Flags["awakened"] = 1`
- [ ] 6.2 解锁额外 ArtCategory（如 "bloodline"）
- [ ] 6.3 战力加成：PE bonus（不进 EffectivePower 的计算，仅觉醒后附加）
- [ ] 6.4 数据驱动——加觉醒=加解锁数据行
- [ ] 6.5 off 模式不影响
