# Story 006: 结构化 Gate 字段 + 唯一档签名逐路全迁

> **Epic**: combat-fullstruct
> **Status**: In Progress
> **Last Updated**: 2026-06-21
> **Layer**: Core
> **Type**: Logic
> **Estimate**: 中 (1d)
> **Depends**: combat-r2 done (Modules/SpecialModule 框架就位)
> **ADR**: adr-0002-module-factory-effect-system
> **GDD**: combat-system.md §功法门控 / legacy specs B5-模块化效果系统-design.md §8

## Context

Gate 字段是功法门控的核心——角色能否使用某种操作（如闪避/护体真气/剑意突破），取决于是否修了对应功法。当前门控仅有 `HasMovementArt`/`HasBodyArt`，需扩展到 ≥ 5 种结构化 Gate。

唯一档 SpecialModule 逐路全迁：确保 21 路中每条路的唯一效果都通过 `SpecialModuleRegistry` 注册（而非裸 `EffectOp`），使战斗结算完全走模块化路径。

**深度源**: combat-fullstruct EPIC §Scope / EffectOp.cs / SpecialModuleRegistry.cs / Modules.cs。

## Acceptance Criteria

- [ ] 6.1 Gate 字段 ≥ 5 种门控类型（HasMovementArt/HasBodyArt/HasSwordIntent/HasFormation/HasAlchemy...）
- [ ] 6.2 Gate 判定正确：有对应功法则通过，无功法阻塞（不影响其他操作）
- [ ] 6.3 每路 SpecialModule 在 registry 中可达（21 路 × 每路 SpecialModules 可查）
- [ ] 6.4 唯一档签名不冲突（两路不同 SpecialModule 签名不同）
- [ ] 6.5 迁完后无遗留裸 EffectOp 散造（code review 或架构扫描实证）
- [ ] 6.6 全量绿 + IL 浮点零 + off 逐字节

## Implementation Notes

- `GateField` 枚举扩展在 `EffectOp.cs` 或新 `GateField.cs`
- `ModuleResolver.cs` 新增 Gate 检查分支
- `SpecialModuleRegistry` 逐路审计：缺的补、重的去重
- 门控失败 → 操作跳过（非抛异常），Chronicle 记录

## Test Evidence

**Required**: `tests/Jianghu.Core.Tests/Cultivation/GateFieldTests.cs` — ~8 tests
**QA ref**: `production/qa/qa-plan-sprint-3.md` § fullstruct-006

## Out of Scope

- 门控 UI 展示（Unity 层）
- 非战斗 Gate（如炼丹/制器准入）→ fullstruct-008
