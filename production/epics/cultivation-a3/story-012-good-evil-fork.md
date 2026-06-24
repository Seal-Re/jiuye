# Story 012: 正邪分叉框架

> **Epic**: cultivation-a3 | **Status**: Not Started | **Type**: Logic | **Estimate**: 0.3d
> **Depends**: a2-013 (storylet executor)

## Context
道德阈值下降 → 触发正邪分叉 storylet——善者正道加强，恶者天魔考验加重。复用 A.2 奇遇框架。

## Acceptance Criteria
- [ ] 12.1 `道德阈值` 定义（innerDemon 积累到特定阈值 → 邪道标签）
- [ ] 12.2 善道标签：daoHeart ≥ 80 → "righteous" tag → 正道同盟加成
- [ ] 12.3 邪道标签：innerDemon ≥ 70 → "evil" tag → 天魔考验加重，正道 NPC 围剿概率升
- [ ] 12.4 正邪 storylet 复用 StoryletExecutor（不新建引擎）
- [ ] 12.5 数据驱动——加正邪 storylet=加数据行

## Out of Scope
- 实际围剿/考验（→ story-013）
