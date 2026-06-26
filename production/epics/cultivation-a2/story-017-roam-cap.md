# Story 017: 游历刷奇遇破上界

> **Epic**: cultivation-a2
> **Status**: Complete （A.6 审计 2026-06-26 订正：git/代码证据证实已实现，台账滞后）
> **Last Updated**: 2026-06-24
> **Layer**: Core
> **Type**: Logic
> **Estimate**: 小 (0.5d)
> **Depends**: story-014
> **ADR**: adr-0001-integer-determinism
> **GDD**: A3-FINAL §5

## Context

A3-FINAL §5 规定：游历（Roam）模式的 encounter exposure ×3 乘子**不能绕过** per-actor cap（ActorMinGap=30）。即使角色在 Roam 模式下 encounter 概率翻 3 倍，两次奇遇的最小间隔仍 ≥ 30 tick。

## Acceptance Criteria

- [ ] 17.1 Roam×3 乘子仅影响 encounter 骰的成功率，不缩短 ActorMinGap
- [ ] 17.2 ActorMinGap 对所有模式统一（Fast/Steady/Comprehend 不触发奇遇，但间隔也计）
- [ ] 17.3 实证：100 tick Roam × 3 → 最多触发 `floor(100/30) = 3` 次奇遇（非 9 次）
- [ ] 17.4 确定性：同种子同 Roam 时长→同 encounter 次数
- [ ] 17.5 off 模式不触发

## Implementation Notes

**关键约束**：
```
encounterChance = baseRate * (mode == Roam ? 3 : 0) * salienceModifier
if tick - lastEncounterTick < ActorMinGap: return false  // <-- 硬钳
```

**证明**：即使 encounterChance=300%（Roam×3），ActorMinGap 硬钳保证最多 3 次/100 tick。

## Out of Scope

- 全局 cap 对此的叠加效应（已在 story-014 处理）

## Test Evidence Requirement

**Type**: Logic — automated unit tests. Roam×3 count cap, ActorMinGap non-bypass proof, off mode.
