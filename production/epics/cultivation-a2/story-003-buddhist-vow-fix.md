# Story 003: 佛修破戒修正

> **Epic**: cultivation-a2
> **Status**: Not Started
> **Last Updated**: 2026-06-24
> **Layer**: Core
> **Type**: Logic
> **Estimate**: 小 (0.5d)
> **Depends**: story-002
> **ADR**: adr-0003-cultivation-off-byte-identical
> **GDD**: A3-FINAL §3.1

## Context

佛修（buddhist_golden_body）的破戒机制：当角色违反佛门誓约（vow 标记），innerDemon 上升。原设计问题：破戒后 daoHeart 归零导致佛修 NPC 大面积堕落（不可玩）。修正：vow 折半（*1/2）非归零，仅 innerDemon 达 lethal(95) 才触发堕落。

## Acceptance Criteria

- [ ] 3.1 佛修 PathDef 含 `vow` 标记（SituationalTags）
- [ ] 3.2 破戒时 innerDemon+（A3 §3.1 定义的量），daoHeart = max(current/2, 1)（折半非归零）
- [ ] 3.3 `innerDemon >= 95` 才触发 Fallen（不因破戒直接堕落）
- [ ] 3.4 佛修 NPC ≥50 代模拟：堕落率 < 10%（破戒不再导致大面积堕落）
- [ ] 3.5 off 模式不影响（佛修路径可加载但不激活破戒逻辑）

## Implementation Notes

现有 `buddhist_golden_body` 路径的 SituationalTags 需含 `"vow"`。破戒检测接入 DailyMode（→ story-019）或 Phase 转移。

## Out of Scope

- 其他路径的类似修正——仅佛修有此机制
- vow 的触发条件（→ story-019 集成）

## Test Evidence Requirement

**Type**: Logic — automated unit tests. Vow break daoHeart half-life test, lethal-only fallen test, NPC simulation sanity check.
