# Story 004: batch5 — 法宝配套战斗效果

> **Epic**: combat-r2
> **Status**: Not Started
> **Layer**: Core
> **Type**: Logic
> **Estimate**: 中
> **Depends**: story-003

## Context

**深度源**: docs/superpowers/specs/...模块化效果系统-design.md §6 法宝配套；impl plan 批5。
**Governing ADR**: adr-0002-module-factory-effect-system。

## Acceptance Criteria

- [ ] 5.1 御器功法解锁模块：御剑 `PenFromResource(itemTier)`、护宝罡 OnDefend `FlatDR`+`Reflect`、落宝 `Special(luobao)`、万宝 `PenFromResource(itemTier)`
- [ ] 5.2 无配套功法 → 裸数值底（itemTier→pe，不解锁模块）
- [ ] 测：器修御宝流（多 Special + Drain）差分参战

## Implementation Notes

法宝不只是道具——经 Modules 工厂反映到数值/特殊效果/异化招数（用户原始要求）。全经 `Modules` 工厂构造（B.9）。

## Out of Scope

硬化 gate + 平衡冻结 → story-005。
