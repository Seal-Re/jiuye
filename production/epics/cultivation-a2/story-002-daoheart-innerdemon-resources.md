# Story 002: 道心/心魔伪资源系统

> **Epic**: cultivation-a2
> **Status**: Not Started
> **Last Updated**: 2026-06-24
> **Layer**: Core
> **Type**: Logic
> **Estimate**: 中 (1d)
> **Depends**: story-001
> **ADR**: adr-0003-cultivation-off-byte-identical
> **GDD**: cultivation-system.md §3.5；A3-FINAL §3；A123 §A.2

## Context

daoHeart/innerDemon 是 A.2 的核心二元资源：道心（守正/定力）对抗心魔（噬主/失控）。红线 B.5（道心解耦）：daoHeart/innerDemon **严禁进 EffectivePower**——仅进 Tribulation ResistTerms 和 Phase 转移条件。两资源均钳 [0,100]，整数。

## Acceptance Criteria

- [ ] 2.1 `DaoHeart`/`InnerDemon` 字段已存在（CultivationState），确认可读写
- [ ] 2.2 `GainDaoHeart(int delta)` 操作——钳 [0,100]，返回实际增益量
- [ ] 2.3 `GainInnerDemon(int delta)` 操作——钳 [0,100]，返回实际增量
- [ ] 2.4 `DaoHeart`/`InnerDemon` 不在 `PowerEngine.Evaluate` 任何 term 中（R3 实证，IL 扫描守）
- [ ] 2.5 `InnerDemon >= 60` → CultivationPhase 可触 Deviation（接入 FSM 的 T_DEVIATE 阈值）
- [ ] 2.6 `InnerDemon >= 95` → CultivationPhase 可触 Fallen（T_DEMON_LETHAL）
- [ ] 2.7 资源变化记录到 `Chronicle`（DomainEvent.DaoHeartChanged / InnerDemonChanged）
- [ ] 2.8 Clone 深拷正确（CultivationState.Clone 已覆盖）
- [ ] 2.9 off 模式：daoHeart=innerDemon=0 恒成立

## Implementation Notes

**位置**：`CultivationState.cs` 已有 `DaoHeart { get; init; }` / `InnerDemon { get; init; }`。需添加 `GainDaoHeart`/`GainInnerDemon` 方法及 clamp 逻辑。

**Chronicle 事件**：新增 `DaoHeartChanged(pathId, old, new, source)` 和 `InnerDemonChanged(pathId, old, new, source)` 事件类型。

**R3 实证**：`PowerEngine.Evaluate` 遍历 `PowerFormula.Terms`——确保无 term 的 Src 含 "daoHeart" 或 "innerDemon"（PathValidator 已有 R3 检查，§12）。

**Phase 接入点**（现有 CultivationPhase 已读 innerDemon）：
- `innerDemon >= T_DEVIATE (60)` → 可能触发 Setback→Deviation
- `innerDemon >= T_DEMON_LETHAL (95)` → 可能触发 Fallen
- `daoHeart` 在 `TribulationResolver.ComputeTribScore` 中作为正项（已接线）

## Out of Scope

- 道心/心魔的 gain source 接线（→ story-001 定义来源，story-019 接线到 DailyMode）
- 佛修破戒修正（→ story-003）

## Test Evidence Requirement

**Type**: Logic — automated unit tests. Clamp [0,100] tests, R3 decoupling (IL scan), Clone fidelity, off mode zero.
