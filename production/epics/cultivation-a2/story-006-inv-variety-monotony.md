# Story 006: 破单调INV-VARIETY真判据

> **Epic**: cultivation-a2
> **Status**: Not Started
> **Last Updated**: 2026-06-24
> **Layer**: Core
> **Type**: Logic
> **Estimate**: 中 (1d)
> **Depends**: story-005
> **ADR**: adr-0001-integer-determinism
> **GDD**: A3-FINAL §1.3, §8

## Context

A3-FINAL §1.3 定义了破单调的真判据——两个不变量确保 NPC 行为不塌缩：
1. **INV-VARIETY**：每 K 个连续 tick 中，DailyMode 至少覆盖 ≥2 种不同模式
2. **INV-NO-DOMINANT**：单一模式占比 ≤ 80%（长程统计）
3. **INV-VARIETY-CONTENT**（§8 增补）：奇遇内容池多样性下界

## Acceptance Criteria

- [ ] 6.1 `INV-VARIETY`：K=10 tick 窗口内，DailyMode 种类 ≥ 2（硬断言，失败 = 行为塌缩）
- [ ] 6.2 `INV-NO-DOMINANT`：任意 50 tick 窗口内，单一模式占比 ≤ 80%
- [ ] 6.3 `VarietyTracker`：轻量追踪器——滑动窗口记录最近 N 个 DailyMode 选择
- [ ] 6.4 违反不变量时写 `Chronicle` Warning 事件（不抛异常——NPC 仍可运行但记录退化）
- [ ] 6.5 `INV-VARIETY-CONTENT` 预埋——content pool diversity 下界（→ story-016 接入）
- [ ] 6.6 确定性：VarietyTracker 状态可 Clone 深拷
- [ ] 6.7 off 模式不追踪

## Implementation Notes

**VarietyTracker**（`CultivationState.Flags["varietyWindow"]` 存储最近 10 个 mode 索引）：
```
varietyWindow: int[10]  // 循环缓冲区，存 DailyMode 枚举 int 值
varietyIndex: int       // 写指针
```

**不变量检查**（每 Tick 触发，纯整数）：
```
CheckInvariants():
    modesInWindow = distinct(varietyWindow)
    if modesInWindow < 2: chronicle.Warn("INV-VARIETY violated")
    
    dominantCount = max(count of each mode in window)
    if dominantCount > windowSize * 4 / 5: chronicle.Warn("INV-NO-DOMINANT")
```

**长程统计**：50 tick 窗口用滑动直方图（5 个桶，O(1) 更新）。

## Out of Scope

- 不变量违反后的自动纠正（当前仅记录，不干预 NPC 行为）
- 奇遇内容池多样性（→ story-016）

## Test Evidence Requirement

**Type**: Logic — automated unit tests. INV-VARIETY satisfaction proof (random walk → ≥2 modes), INV-NO-DOMINANT boundary, tracker determinism, off mode no-track.
