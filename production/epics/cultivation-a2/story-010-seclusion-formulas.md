# Story 010: 闭关时长+收益公式

> **Epic**: cultivation-a2
> **Status**: Complete （A.6 审计 2026-06-26 订正：git/代码证据证实已实现，台账滞后）
> **Last Updated**: 2026-06-24
> **Layer**: Core
> **Type**: Logic
> **Estimate**: 中 (1d)
> **Depends**: story-009
> **ADR**: adr-0001-integer-determinism
> **GDD**: A3-FINAL §2.2, §2.3, §2.4

## Context

A3-FINAL §2.2-2.4 定义了闭关的时长公式、折寿代价、收益落点。闭关是高风险高回报行为——投入 WorkUnits 获得进度跳升和 BreakAid 加成，但消耗寿命（AgeCost）。

## Acceptance Criteria

- [ ] 10.1 `Duration = 60 + 8 * WorkUnits - min(Insight / 2, 30)`（纯整数）
- [ ] 10.2 Insight=20, WorkUnits=10 → Duration = 60 + 80 - 10 = 130 ticks
- [ ] 10.3 `AgeCost = ActionInterval * ceil(Duration / ActionInterval) * FoldLifeFactor / 100`
- [ ] 10.4 `FoldLifeFactor` 初值 = 100（基线），随 streak 递增（→ story-011）
- [ ] 10.5 收益落点统一：`WorkUnits + BreakAid` —— BreakAid 经 Seclusion 方法加成
- [ ] 10.6 出关时 `progress += WorkUnits * progressMultiplier + BreakAidBonus`
- [ ] 10.7 出关时写 Chronicle 事件（SeclusionCompleted：duration, progressGain, ageCost, breakAidBonus）
- [ ] 10.8 确定性：同参数→同时长/同代价/同收益

## Implementation Notes

**时长公式详解**：
```
Duration = 60 + 8 * WorkUnits - InsightSpeedup
InsightSpeedup = min(Insight / 2, 30)

// 示例：
// WorkUnits=5, Insight=20:  Duration = 60 + 40 - 10 = 90
// WorkUnits=10, Insight=30: Duration = 60 + 80 - 15 = 125
// WorkUnits=20, Insight=40: Duration = 60 + 160 - 20 = 200 (cap)
```

**AgeCost 公式**：
```
AgeCost = ActionInterval * CeilDiv(Duration, ActionInterval) * FoldLifeFactor / 100
// ActionInterval = LimitsConfig.Default.ActionInterval (e.g., 4)
// FoldLifeFactor = 100 + streak * 20  // → story-011
```

**收益落点**：
```
progressGain = WorkUnits * CULTIVATION_GAIN_PER_ACTION  // = 1 per WU
breakAidBonus = BreakAid.Seclusion.progressBonus  // = min(K*4, 30)
totalProgress = progressGain + breakAidBonus
innerDemonDelta = BreakAid.Seclusion.innerDemonRisk  // +3 per streak
```

## Out of Scope

- FoldLifeFactor 递增（→ story-011）
- 收益的 phase 正确应用（→ story-020）

## Test Evidence Requirement

**Type**: Logic — automated unit tests. Duration formula for 5+ parameter sets, AgeCost formula, progressGain, determinism.
