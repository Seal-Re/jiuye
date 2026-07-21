# Story 013: 正邪→天劫强化/正道围剿

> **Epic**: cultivation-a3 | **Status**: Complete — EPIC Done @2026-07-17 (Sprint 9) | **Type**: Logic | **Estimate**: 0.3d
> **Depends**: story-012

## Context
邪道标签触发更强天劫（HeavenlyTribulation 威胁值 +50%），正道 NPC 有概率围剿邪道 NPC。善道标签触发正道同盟加成。

## Acceptance Criteria
- [ ] 13.1 邪道 NPC 天劫威胁值 ×1.5（HeavenlyTribulation.threat * 3/2）
- [ ] 13.2 善道 NPC 天劫威胁值 ×0.8（HeavenlyTribulation.threat * 4/5）
- [ ] 13.3 善道 NPC 突破时 daoHeart bonus +3（天佑）
- [ ] 13.4 确定性——标签检查纯静态（无 RNG）
- [ ] 13.5 off 模式不触发
