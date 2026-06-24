# Story 004: 4路DailyMode枚举+整数倍率表

> **Epic**: cultivation-a2
> **Status**: Not Started
> **Last Updated**: 2026-06-24
> **Layer**: Core
> **Type**: Logic
> **Estimate**: 中 (1d)
> **Depends**: story-001
> **ADR**: adr-0001-integer-determinism
> **GDD**: A3-FINAL §1；A123 §A.2.1

## Context

A3-FINAL §1 定义了破单调的核心机制——4 路 DailyMode 日课微决策。每个 NPC 每 Tick 选择一种日课模式，影响 progress/daoHeart/innerDemon 的变化方向和速率。收敛为唯一子动作集合，不修改 RuleBrain（仅 cultivation 内部决策）。

## Acceptance Criteria

- [ ] 4.1 `DailyMode` 枚举落地：`{ Fast, Steady, Comprehend, Roam }`
- [ ] 4.2 整数倍率表（纯整数，禁浮点）：

| Mode | Progress 倍率 | innerDemon Δ | daoHeart Δ | 其他 |
|------|:---------:|:----------:|:--------:|------|
| Fast | ×6/4 | +2 | — | — |
| Steady | ×3/4 | -1 | — | Foundation+1 |
| Comprehend | ×1/2 | — | 见 §1.1 | EpiphanyRoll |
| Roam | ×1/4 | -2 | — | Move, encounter×3 |

- [ ] 4.3 `Comprehend.EpiphanyRoll`: `Roll(1,20) < Insight - 18` → breakProgress+25 或 daoHeart+5
- [ ] 4.4 `Roam` 触 `Move` 动作（TravelAction）+ 奇遇曝光 ×3（频率不破 per-actor cap）
- [ ] 4.5 DailyMode 选择存入 `CultivationState.Flags["dailyMode"]`（深拷自动带）
- [ ] 4.6 off 模式不写入 dailyMode flag

## Implementation Notes

**位置**：新建 `DailyMode.cs`（枚举 + 倍率表 + Apply 方法）。`DailyMode.Apply(CultivationState, Character, IRandom)` → 返回 `DailyModeResult`（progressDelta, daoHeartDelta, innerDemonDelta, epiphanyTriggered, shouldMove）。

**纯整数倍率**：`progress * 6 / 4` 等使用整数除法，不依赖浮点。

**Comprehend Epiphany**：`Roll(1,20)` 用 `IRandom.NextInt(1, 21)`。`Insight` 从 `Character.Stats[3]` 读取。阈值 `Insight - 18`：Insight=20 → threshold=2（10% 概率）。Insight=25 → threshold=7（35% 概率）。

**Roam Move**：不直接修改位置——触发 TravelAction 入队（类似 RuleBrain 但走 cultivation 内部路径）。

## Out of Scope

- DailyMode 选择算法/NPC 贪心逻辑（→ story-005）
- 迟滞规则（→ story-005）
- DailyMode↔Phase FSM 集成（→ story-019）

## Test Evidence Requirement

**Type**: Logic — automated unit tests. 4 mode × integer multiplier precision tests, Epiphany probability distribution, off mode no flag.
