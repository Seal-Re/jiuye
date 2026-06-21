# Story 005: PostMul ModKind + 负向压制

> **Epic**: combat-fullstruct
> **Status**: Complete
> **Layer**: Core
> **Type**: Logic
> **Estimate**: 中 (1.5d)
> **Depends**: fullstruct-001 (derived 真求和)
> **ADR**: adr-0002-module-factory-effect-system
> **Last Updated**: 2026-06-21
> **GDD**: combat-system.md §模块化效果 / legacy specs B5-模块化效果系统-design.md §2-5

## Context

PostMul ModKind 是战斗效果链中的乘法修正层——在 FlatPen/FlatDR 之后乘算。负向压制是对特定目标类型（如阴→阳/魔→佛/低境界→高境界）的战力衰减机制。当前 `EffectOp` 已支持 `ModKind`，但 `PostMul` 枚举值与具体压制逻辑未结构化。

**深度源**: combat-fullstruct EPIC §Scope / EffectOp.cs / ModuleResolver.cs。

## Acceptance Criteria

- [x] 5.1 PostMul ModKind ≥ 3 种（LawSuppress/化形态/文宫/天道压制）各自独立生效
- [x] 5.2 负向压制 ≥ 2 种（压制方对特定目标类型的战力衰减）
- [x] 5.3 PostMul 在 FlatPen/FlatDR 之后乘算（乘法序验证）
- [x] 5.4 压制值钳位 [Min,Max]（不过压、不反转）
- [x] 5.5 全 UT 0-12 带上 PostMul 后 pe 仍收敛在合理范围
- [x] 5.6 全量绿 + IL 浮点零 + off 逐字节

## Implementation Notes

- `Modules.cs` 新增 `PostMul(Kind, ratio, ...)` 工厂方法
- `ModuleResolver.cs` 新增 PostMul 分支（在 FlatPen/FlatDR 之后 applied）
- `EffectOp.cs` 如需加字段则向后兼容（默认值不破现有 tests）
- 压制数据驱动：压制矩阵放在 `SituationalEdges.cs` 或新 `SuppressionMatrix.cs`

## Test Evidence

**Required**: `tests/Jianghu.Core.Tests/Cultivation/PostMulTests.cs` — ~10 tests
**Performance**: PostMul 为整数乘法，每 Evaluate <1ms overhead，无性能影响
**QA ref**: `production/qa/qa-plan-sprint-3.md` § fullstruct-005

## Out of Scope

- 即时反应窗口的 q 压制（Unity 层）
- 压制动画/VFX
