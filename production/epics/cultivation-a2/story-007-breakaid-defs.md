# Story 007: BreakAid四法数据模型

> **Epic**: cultivation-a2
> **Status**: Not Started
> **Last Updated**: 2026-06-24
> **Layer**: Core
> **Type**: Logic
> **Estimate**: 小 (0.5d)
> **Depends**: story-001
> **ADR**: adr-0001-integer-determinism
> **GDD**: A2-FINAL §3.5；A123 §A.2

## Context

A2-FINAL §3.5 定义了破障四法（BreakAid）——角色在瓶颈期可用的四种突破辅助手段。每种方法有权衡：加速突破 vs innerDemon 风险 vs 资源消耗。数据驱动：新方法=加数据行。

## Acceptance Criteria

- [ ] 7.1 `BreakAidMethod` 枚举：`{ Seclusion, Epiphany, Resource, Guardian }`
- [ ] 7.2 `BreakAidDef` record：Method, BreakProgressBonus: int, InnerDemonRisk: int, ResourceCost: Dict, DaoHeartReq: int
- [ ] 7.3 四法数据定义（纯整数）：

| Method | Progress+ | innerDemon Δ | Cost | 条件 |
|--------|:------:|:----------:|------|------|
| Seclusion | +min(K*4,30) | +3/streak | — | K=连续闭关次数 |
| Epiphany | +25 | — | — | Insight≥20, EpiphanyRoll |
| Resource | +15 | -2 | 消耗灵石 | 有足够资源 |
| Guardian | +20 | -5 | — | 需守护者 NPC 在场 |

- [ ] 7.4 `BreakAidRegistry`（类似 PathRegistry 模式）——单例注册，加方法=加数据行
- [ ] 7.5 off 模式不激活

## Implementation Notes

**Seclusion streak K**：`CultivationState.Flags["seclusionStreak"]` 记录连续闭关次数，收益 K*4 钳 min(K*4, 30)。出关后 streak 置 0（中途被打扰 streak 折半）。

**Resource 消耗**：灵石等货币从 Character 资源池扣除。需要的资源类型由 BreakAidDef.ResourceCost 定义。

**Guardian 条件**：需同地点有 ≥1 高 realm（≥目标角色+2）NPC。由 Scheduler/World 查询。

## Out of Scope

- BreakAid 实际执行（→ story-019/020 集成）
- 闭关避险刷点（→ story-011）

## Test Evidence Requirement

**Type**: Logic — automated unit tests. 4 method defs validation, registry load, off mode no-op.
