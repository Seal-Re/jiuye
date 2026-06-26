# Story 008: 顿悟Epiphany机制

> **Epic**: cultivation-a2
> **Status**: Complete （A.6 审计 2026-06-26 订正：git/代码证据证实已实现，台账滞后）
> **Last Updated**: 2026-06-24
> **Layer**: Core
> **Type**: Logic
> **Estimate**: 中 (1d)
> **Depends**: story-004, story-007
> **ADR**: adr-0001-integer-determinism
> **GDD**: A3-FINAL §1.1

## Context

Comprehend 日课模式下的顿悟（Epiphany）是破单调的核心亮点——低概率高收益事件。当 Insight 足够高时，角色在日常领悟中有机会触发顿悟，获得 breakProgress 大幅跳升或 daoHeart 增益。

## Acceptance Criteria

- [ ] 8.1 `EpiphanyRoll`：`Roll(1,20) < Insight - 18` → 触发顿悟
- [ ] 8.2 顿悟成功时：50% breakProgress+25，50% daoHeart+5（经 `_cultRng` 公平 coin flip）
- [ ] 8.3 Insight < 18 → 概率=0（无法触发顿悟，数学保证）
- [ ] 8.4 Insight = 25 → threshold=7 → 35% 概率（最高档）
- [ ] 8.5 顿悟触发时写 `Chronicle.EpiphanyTriggered` 事件
- [ ] 8.6 同一 tick 最多触发 1 次顿悟（防重复骰）
- [ ] 8.7 确定性：同种子+同 Insight → 同 tick 顿悟结果一致
- [ ] 8.8 off 模式不触发顿悟
- [ ] 8.9 Deviation/Fallen 阶段不可触发顿悟（心魔过高无法静悟）

## Implementation Notes

**概率表**（整数阈值，无浮点）：

| Insight | Threshold | 概率 |
|--------:|:---------:|:----:|
| ≤18 | 0 | 0% |
| 19 | 1 | 5% |
| 20 | 2 | 10% |
| 21 | 3 | 15% |
| 22 | 4 | 20% |
| 23 | 5 | 25% |
| 24 | 6 | 30% |
| 25 | 7 | 35% |

**实现**：`EpiphanyResolver.TryTrigger(CultivationState, Character, IRandom) → EpiphanyResult?`。

## Out of Scope

- 顿悟对 DailyMode 选择的影响（→ story-005 贪心算法已考虑 epiphanyBonus）
- 顿悟在闭关期间的行为（闭关期不触发 Comprehend → story-009）

## Test Evidence Requirement

**Type**: Logic — automated unit tests. Threshold distribution (1000 trials per Insight level), determinism, phase lock (Deviation/Fallen no-epiphany), off mode no-op.
