# Story 004: AwakeningDef 数据模型

> **Epic**: cultivation-a3 | **Status**: Complete — EPIC Done @2026-07-17 (Sprint 9) | **Type**: Logic | **Estimate**: 0.3d
> **Depends**: story-001

## Context
觉醒（Awaken）——同路线血统/体质觉醒，解锁高阶功法或战力阈值。不改变 PathId。

## Acceptance Criteria
- [ ] 4.1 `AwakeningDef` record：Id, PathId, HiddenTag, TriggerEvent, UnlockArts, PowerBonus
- [ ] 4.2 触发事件枚举：NearDeath, SecretRealm, BloodlineArtifact, RealmGate
- [ ] 4.3 隐藏 tag → 线索 → 揭示 → 剧变 四阶段
- [ ] 4.4 `AwakeningRegistry` 数据驱动
- [ ] 4.5 废材逆转逻辑（waste → heavenly spirit root）

## Out of Scope
- 实际触发器（→ story-005）
