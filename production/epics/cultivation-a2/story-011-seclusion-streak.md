# Story 011: 闭关避险刷点

> **Epic**: cultivation-a2
> **Status**: Not Started
> **Last Updated**: 2026-06-24
> **Layer**: Core
> **Type**: Logic
> **Estimate**: 中 (1d)
> **Depends**: story-010
> **ADR**: adr-0001-integer-determinism
> **GDD**: A3-FINAL §2.6

## Context

A3-FINAL §2.6 定义了闭关避险刷点——NPC 连续闭关会导致边际代价递增，防"无限闭关刷进度"策略。每连续一次闭关，FoldLifeFactor 递增（折寿加剧），innerDemon 上升，strike 累积。strike ≥ 3 → 闭关收益减半。strike ≥ 5 → 强制禁止闭关（需间隔冷却）。

## Acceptance Criteria

- [ ] 11.1 `seclusionStreak` 追踪：每次闭关 +1，每次正常 Tick（非闭关）折半
- [ ] 11.2 `FoldLifeFactor = 100 + seclusionStreak * 20`（首次=100, 第5次=200 折寿翻倍）
- [ ] 11.3 `innerDemon += 3 * seclusionStreak`（出关结算时）
- [ ] 11.4 `strike` 计数：strike ≥ 3 → 闭关收益 ×1/2（progressGain 折半）
- [ ] 11.5 `strike ≥ 5` → 闭关被拒（Seclusion.Enter 返回 false），需 ≥10 个正常 tick 冷却
- [ ] 11.6 被打扰出关（Disturb ≥ 3）→ streak 不归零（折半），strike 不递增（非自愿）
- [ ] 11.7 确定性：同 streak+同参数 → 同代价
- [ ] 11.8 off 模式无 streak 追踪

## Implementation Notes

**Streak 生命周期**：
```
EnterSeclusion(): streak++, strike++
ExitSeclusion(): apply costs based on streak/strike
NormalTick(): streak = max(0, streak / 2)  // 折半衰减
```

**递减收益表**：

| Strike | FoldLifeFactor | innerDemon+ | 收益倍率 | 闭关允许? |
|:------:|:-------------:|:---------:|:------:|:------:|
| 0 | 100 | +3 | ×1 | ✅ |
| 1 | 120 | +6 | ×1 | ✅ |
| 2 | 140 | +9 | ×1 | ✅ |
| 3 | 160 | +12 | ×1/2 | ✅ (减半) |
| 4 | 180 | +15 | ×1/2 | ✅ (减半) |
| 5+ | 200+ | +18+ | — | ❌ (需冷却) |

**冷却**：strike ≥ 5 后需 ≥10 个正常 tick（非闭关）才能重置 strike=0。

## Out of Scope

- 被打扰提前出关的完整实现（G5）

## Test Evidence Requirement

**Type**: Logic — automated unit tests. Streak escalation, strike cost table, cooldown lockout, disturbance non-escalation, determinism, off mode.
