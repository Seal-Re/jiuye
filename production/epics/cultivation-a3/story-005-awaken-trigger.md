# Story 005: 觉醒触发器

> **Epic**: cultivation-a3 | **Status**: Complete — EPIC Done @2026-07-17 (Sprint 9) | **Type**: Logic | **Estimate**: 0.3d
> **Depends**: story-004, a2-013 (storylet executor)

## Context
觉醒通过 A.2 奇遇框架触发——濒死/秘境/血统法器事件检测隐藏 tag，触发觉醒 storylet。

## Acceptance Criteria
- [ ] 5.1 觉醒挂 StoryletExecutor 框架——复用 tryTrigger 模式
- [ ] 5.2 濒死触发：HP < 10% → roll 觉醒检测
- [ ] 5.3 秘境触发：特定 node/tag → 觉醒概率 boost
- [ ] 5.4 血统法器触发：装备特定 artifact → 必定觉醒
- [ ] 5.5 确定性：同 seed + 同状态 → 同觉醒结果

## Out of Scope
- 觉醒后战力解锁（→ story-006）
