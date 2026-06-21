# Story 004: dot 完整时序 + Control selectMove 失效

> **Epic**: combat-fullstruct
> **Status**: Complete
> **Layer**: Core
> **Type**: Logic
> **Estimate**: 中 (1d)
> **Depends**: fullstruct-001 (derived 真求和)
> **ADR**: adr-0002-module-factory-effect-system
> **GDD**: combat-system.md §dot时序 / §控场

## Context

dot 效果完整时序（OnUse→OnDefend→软情境→同时扣血）+ Control 类效果使被控方 skill→null（不施 dot/control）且 dmg=0（selectMove 失效）。

**深度源**: DuelEngine.cs / EffectOp.cs / ModuleResolver.cs。

## Acceptance Criteria

- [x] 4.1 dot tick 在 OnDefend 后结算
- [x] 4.2 dot 伤害走软情境修正
- [x] 4.3 control 施加后目标 skill→null（禁用主动技能）
- [x] 4.4 control 施加后目标 dmg=0（selectMove 失效）
- [x] 4.5 被控方不能施 dot/control（递归防）
- [x] 4.6 SparAction ON 分支接入 DuelEngine
- [x] 4.7 全量绿 + IL 浮点零 + off 逐字节

## Implementation Notes

- `DuelEngine.cs` + `ModuleResolver.cs` (+ApplyOnDefend)
- `SparAction.cs` ON→DuelEngine 分支
- `EffectOp.cs` 控场标记位

## Test Evidence

**Required**: `tests/Jianghu.Core.Tests/Cultivation/DuelEngineTests.cs`（16 tests）
**Commit**: `f64aa91`

## Out of Scope

- 召唤物系统（FULLSTRUCT 后续）
- 结算回滚栈（FULLSTRUCT 后续）
