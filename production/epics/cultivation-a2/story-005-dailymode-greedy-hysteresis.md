# Story 005: DailyMode贪心算法+迟滞规则

> **Epic**: cultivation-a2
> **Status**: Not Started
> **Last Updated**: 2026-06-24
> **Layer**: Core
> **Type**: Logic
> **Estimate**: 大 (1.5d)
> **Depends**: story-004
> **ADR**: adr-0001-integer-determinism
> **GDD**: A3-FINAL §1.2；A123 §A.2.1

## Context

A3-FINAL §1.2 定义了 NPC 日课选择的贪心算法 + 迟滞规则（hysteresis）。防"NPC 自动驾驶塌缩成确定锯齿"——所有 NPC 都选同一种最优模式会导致行为单一化。迟滞规则：innerDemon 超过进入阈值才切换模式，低于退出阈值才切回，防频繁抖动。

## Acceptance Criteria

- [ ] 5.1 贪心算法：NPC 根据当前状态（innerDemon/daoHeart/progress/realm）选择最优 DailyMode
- [ ] 5.2 `DEMON_DANGER_ENTER = 65`：innerDemon ≥ 65 → 倾向 Steady/Roam（降心魔）
- [ ] 5.3 `DEMON_DANGER_EXIT = 50`：innerDemon ≤ 50 → 可恢复 Fast（迟滞带宽 15）
- [ ] 5.4 迟滞规则：进入危险区后必须 innerDemon 降到 EXIT 以下才能切回 progress 优先模式
- [ ] 5.5 `Breakthrough` 阶段锁 Fast（突破期不可换模式）
- [ ] 5.6 `Deviation/Fallen` 阶段强制 Roam 或 Steady（心魔过高，不可 Fast/Comprehend）
- [ ] 5.7 确定性：相同状态+相同种子 → 相同 DailyMode 选择
- [ ] 5.8 off 模式不执行 DailyMode 选择

## Implementation Notes

**贪心评分**（纯整数）：
```
Score(mode) = progressWeight * progressMultiplier
            - innerDemonWeight * innerDemonDelta
            + daoHeartWeight * daoHeartDelta
            + epiphanyBonus(if Comprehend)
            + varietyPenalty(consecutive same mode)

if innerDemon >= DEMON_DANGER_ENTER:
    innerDemonWeight *= 3  // 心魔危险时降心魔优先
```

**迟滞状态机**：
```
Normal ──(innerDemon >= 65)──> Danger
Danger ──(innerDemon <= 50)──> Normal
```

**确定性**：所有随机决策走 `_cultRng`（Split(5)）。无 `System.Random` 或 `DateTime` 依赖。

## Out of Scope

- 破单调 INV-VARIETY 真判据（→ story-006）
- 游历刷奇遇破上界（→ story-017）

## Test Evidence Requirement

**Type**: Logic — automated unit tests. Greedy selection determinism, hysteresis enter/exit, breakthrough lock, deviation forced roam, off mode no-op.
