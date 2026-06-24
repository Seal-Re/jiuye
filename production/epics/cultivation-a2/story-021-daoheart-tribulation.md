# Story 021: daoHeart→Tribulation集成

> **Epic**: cultivation-a2
> **Status**: Not Started
> **Last Updated**: 2026-06-24
> **Layer**: Core
> **Type**: Integration
> **Estimate**: 小 (0.5d)
> **Depends**: story-002, cultivation-a1-rest (TribulationResolver done)
> **ADR**: adr-0003-cultivation-off-byte-identical
> **GDD**: A2-FINAL §3.6；A3-FINAL §3

## Context

TribulationResolver 中 HeartDemonTribulation（心魔劫）的 ResistTerms 已包含 daoHeart（weight 2），innerDemon（weight -2）。此故事验证接线正确：高 daoHeart → 容易渡心魔劫；高 innerDemon → 难渡心魔劫。确保红 B.5（daoHeart 不进 EffectivePower）在此接线中也恪守。

## Acceptance Criteria

- [ ] 21.1 `HeartDemonTribulation.ResistTerms` 已有 `daoHeart` weight=2（已实现，需验证）
- [ ] 21.2 实证：daoHeart=100, innerDemon=0 → TribScore 显著高于 daoHeart=0, innerDemon=100
- [ ] 21.3 daoHeart 仍不进 PowerEngine（IL 扫描守，R3 不变）
- [ ] 21.4 三劫（渡劫/天劫/心魔劫）各自的 ResistTerms 覆盖率 100%（无 term 为 0 权重）
- [ ] 21.5 其他两劫的 ResistTerms 也可扩展引用 daoHeart/innerDemon（数据驱动）
- [ ] 21.6 确定性：同 daoHeart+innerDemon → 同 TribScore
- [ ] 21.7 off 模式：Tribulation 不走（off 不走 Phase 转移）

## Implementation Notes

**验证多于新代码**——TribulationResolver 已有 daoHeart 接线（来自 A.1）。主要工作是：
1. 添加实证测试：定量证明 daoHeart 对心魔劫 TribScore 的影响
2. 审计其他两劫的 ResistTerms——是否需要引用 daoHeart
3. 确保 IL 扫描覆盖 TribulationResolver 全量

## Out of Scope

- 新 tribulation 类型（数据驱动加类型 = 加 TribulationDef，已有框架）

## Test Evidence Requirement

**Type**: Logic — automated unit tests. daoHeart impact on TribScore, innerDemon counter-effect, R3 IL scan, off mode.
