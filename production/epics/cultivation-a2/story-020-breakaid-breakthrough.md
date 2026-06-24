# Story 020: BreakAid→Breakthrough集成

> **Epic**: cultivation-a2
> **Status**: Not Started
> **Last Updated**: 2026-06-24
> **Layer**: Core
> **Type**: Integration
> **Estimate**: 中 (1d)
> **Depends**: story-007, story-009
> **ADR**: adr-0003-cultivation-off-byte-identical
> **GDD**: A2-FINAL §3.5

## Context

BreakAid 四法需接入突破流程。当角色处于 Bottleneck Phase 时，可选择一种 BreakAid 方法增加 breakProgress，加速突破。每种方法有权衡（innerDemon 风险 vs 资源消耗 vs 守护者需求）。

## Acceptance Criteria

- [ ] 20.1 Bottleneck Phase 触发 BreakAid 选择（NPC 贪心选最优方法）
- [ ] 20.2 `Seclusion` 方法：进入闭关 → 出关时 breakProgress + min(K*4, 30)，innerDemon + 3/次
- [ ] 20.3 `Epiphany` 方法：骰 EpiphanyRoll → 成功则 breakProgress + 25
- [ ] 20.4 `Resource` 方法：消耗灵石 → breakProgress + 15，innerDemon - 2
- [ ] 20.5 `Guardian` 方法：需同地点有高 realm 角色 → breakProgress + 20，innerDemon - 5
- [ ] 20.6 breakProgress 累积到 RealmCurve.Threshold → 触发 Breakthrough Phase
- [ ] 20.7 BreakAid 每 tick 最多触发 1 次
- [ ] 20.8 确定性：同状态+同方法→同结果
- [ ] 20.9 off 模式不执行 BreakAid

## Implementation Notes

**选择逻辑**（NPC 贪心）：
```
if phase == Bottleneck:
    available = BreakAidRegistry.Filter(st, character, world)
    // Filter: 排除资源不足/无守护者/锁闭关(strike≥5)的方法
    best = argmax(available, aid => 
        aid.BreakProgressBonus - aid.InnerDemonRisk * 2)
    ApplyBreakAid(best, st, character, _cultRng)
```

**BreakAid→Phase 转换**：breakProgress 累积由现有 RealmCurve.NextIndexIfReady 判断。BreakAid 只是加速 breakProgress 累积的手段。

## Out of Scope

- BreakAid 在非 Bottleneck Phase 的使用（当前仅瓶颈期可用）

## Test Evidence Requirement

**Type**: Logic — automated unit tests. Each method's breakProgress contribution, innerDemon risk, resource cost, guardian check, off mode.
