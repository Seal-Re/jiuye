# Story 015: 奇遇收益护栏+salience decay

> **Epic**: cultivation-a2
> **Status**: Not Started
> **Last Updated**: 2026-06-24
> **Layer**: Core
> **Type**: Logic
> **Estimate**: 中 (1d)
> **Depends**: story-013
> **ADR**: adr-0001-integer-determinism
> **GDD**: A3-FINAL §4.2, §4.3

## Context

A3-FINAL §4.2-4.3 定义了奇遇收益的硬护栏——防止"奇遇刷成帝"的极端情况。ENCOUNTER_PROGRESS_RATIO 硬钳：奇遇贡献的 progress 不能超过总 progress 的 35%。Salience decay：同一奇遇重复触发后吸引力递减，防"最优奇遇反复刷"。

## Acceptance Criteria

- [ ] 15.1 `ENCOUNTER_PROGRESS_RATIO = 3`——`encounterProgress * 3 ≤ totalProgress` 硬钳
- [ ] 15.2 违反 ratio 时：奇遇 progress 收益钳为 0（其余收益照常）
- [ ] 15.3 `SalienceDecay`：每次触发后 salience *= 2/3（整数除法，钳 min=1）
- [ ] 15.4 间隔 ≥ 50 tick 无触发 → salience 恢复（+1/tick 直到初值）
- [ ] 15.5 35% cap 扩展到所有突破货币（breakProgress/daoHeart/epiphanyCount）
- [ ] 15.6 确定性：同触发历史→同 decay 状态
- [ ] 15.7 off 模式不追踪

## Implementation Notes

**ENCOUNTER_PROGRESS_RATIO 硬钳**：
```
if encounterProgress * 3 > totalProgress:
    progressReward = 0  // 砍 progress 收益
    // 其他收益（daoHeart/innerDemon/relation/resource）照常
```

**Salience decay 表**（同奇遇连续触发）：

| 触发次数 | Salience | 概率权重 |
|:------:|:------:|:------:|
| 0 | 100 (init) | 基准 |
| 1 | 66 | 2/3 |
| 2 | 44 | 4/9 |
| 3 | 29 | ~1/3 |
| 4 | 19 | ~1/5 |
| 5+ | 钳≥1 | 极低 |

**Salience 恢复**：连续 50 tick 未触发此奇遇 → 每 tick +1 直到初值。

## Out of Scope

- 内容池最小规模（→ story-016）

## Test Evidence Requirement

**Type**: Logic — automated unit tests. Ratio hard clamp, salience decay/restore, multi-currency 35% cap, off mode.
