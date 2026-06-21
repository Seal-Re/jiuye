# Story 007: 结算回滚栈（因果逆演/夺舍续命/分魂挡刀）

> **Epic**: combat-fullstruct
> **Status**: In Progress
> **Last Updated**: 2026-06-21
> **Layer**: Core
> **Type**: Logic
> **Estimate**: 中 (1d)
> **Depends**: fullstruct-001 (derived 真求和)
> **ADR**: adr-0002-module-factory-effect-system
> **GDD**: combat-system.md §逆演/夺舍/分魂

## Context

结算回滚栈是战斗中最复杂的交互层——某些唯一效果需要"撤销上一次交锋"（因果逆演）、"濒死时恢复 HP"（夺舍续命）、"伤害转移到分魂"（分魂挡刀）。这些操作依赖 LIFO 回滚栈，改变战斗结算的时序。

当前 DuelEngine.ResolveR2 是单向结算（OnUse→OnDefend→扣血），无回滚能力。需加栈层支持逆演/夺舍/分魂。

**深度源**: combat-fullstruct EPIC §Scope / combat-system.md §逆演 / DuelEngine.cs。

## Acceptance Criteria

- [ ] 7.1 逆演栈 ≥ 3 操作可回滚（push → pop → state restored）
- [ ] 7.2 夺舍续命：濒死触发 → 回滚上次致命伤害 → HP 恢复
- [ ] 7.3 分魂挡刀：伤害转移到分魂 → 本体 HP 不减
- [ ] 7.4 每次回滚栈深度验证（push/pop 对称）
- [ ] 7.5 回滚后 Karma/关系值不变（仅撤战斗状态，不撤社会后果）
- [ ] 7.6 全量绿 + IL 浮点零 + off 逐字节

## Implementation Notes

- 在 `DuelEngine.cs` 或新 `RollbackStack.cs` 中加 LIFO 栈
- 每个 EffectOp 可选择性地 push snapshot（逆演/夺舍相关效果 push）
- 夺舍 gate：无雷/纯阳/佛光在场 → 允许；否则夺舍被压制
- 回滚不改 DuelEngine.ResolveR2 核心循环——附加层

## Test Evidence

**Required**: `tests/Jianghu.Core.Tests/Cultivation/RollbackStackTests.cs` — ~8 tests
**QA ref**: `production/qa/qa-plan-sprint-3.md` § fullstruct-007

## Out of Scope

- 多级递归回滚（回滚中触发另一个回滚）→ FULLSTRUCT 后续
- 回滚 VFX（Unity 层）
