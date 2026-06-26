# Story 014: 奇遇频率cap收敛

> **Epic**: cultivation-a2
> **Status**: Complete （A.6 审计 2026-06-26 订正：git/代码证据证实已实现，台账滞后）
> **Last Updated**: 2026-06-24
> **Layer**: Core
> **Type**: Logic
> **Estimate**: 中 (1d)
> **Depends**: story-013
> **ADR**: adr-0001-integer-determinism
> **GDD**: A3-FINAL §4.1

## Context

A3-FINAL §4.1 定义了奇遇频率的全局+单角色双重 cap，防"高人口→奇遇泛滥"或"游历刷奇遇上界"。

## Acceptance Criteria

- [ ] 14.1 `GlobalCap = max(12, AliveCount * 4)`——每 tick 全局最多触发次数
- [ ] 14.2 `ActorMinGap = 30` ticks——同一角色两次奇遇间隔 ≥ 30
- [ ] 14.3 `CategoryCap = 4 / 100 ticks`——同一 category 每 100 tick 最多触发 4 次
- [ ] 14.4 GlobalCap 按 AliveCount 动态更新（角色死亡→cap 降，新生→cap 升）
- [ ] 14.5 cap 超限时：该 tick 不触发新奇遇，剩余角色等待下 tick
- [ ] 14.6 确定性：同 AliveCount→同 cap 行为
- [ ] 14.7 off 模式所有 cap=0

## Implementation Notes

**Cap 检查顺序**（每 tick）：
1. GlobalCap：`triggeredThisTick < GlobalCap`
2. ActorMinGap：`tick - lastEncounterTick >= 30`
3. CategoryCap：`categoryCount[cat] < 4`（滑动窗口 100 tick）

**GlobalCap 动态**：
```
AliveCount=10 → GlobalCap=40
AliveCount=50 → GlobalCap=200
AliveCount=100 → GlobalCap=400
但上限受每 tick 可处理角色数约束
```

## Out of Scope

- 奇遇收益护栏（→ story-015）

## Test Evidence Requirement

**Type**: Logic — automated unit tests. GlobalCap scaling with AliveCount, ActorMinGap enforcement, CategoryCap sliding window, off mode.
