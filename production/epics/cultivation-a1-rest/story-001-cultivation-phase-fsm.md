# Story 001: 10 态修炼流程状态机（CultivationPhase 骨架）

> **Epic**: cultivation-a1-rest
> **Status**: Backlog
> **Layer**: Core
> **Type**: Logic
> **Estimate**: 中 (1d)
> **Depends**: balance-001 (辅助路 UT 锚需 INV-CROSS 对拍测得)
> **ADR**: adr-0003-cultivation-off-byte-identical
> **GDD**: cultivation-system.md §3.4 / legacy specs A123-收敛对齐设计.md §A1.1

## Context

修炼流程不是"累积点数→突破"一条线。完整流程有 10 个状态（idle→breakthrough→tribulation→recovery→bottleneck→...），22 条整数守卫转移，破障四法辅助突破，三劫数据驱动。

A.0 当前只有简单的 `CultivationPoints` 累积 + `RealmIndex` 进阶。本 story 建 CultivationPhase 状态机骨架——10 态枚举 + 主转移 + 整数守卫，为后续劫/寿元/道心打地基。

**深度源**: cultivation-a1-rest EPIC §Scope / A123 spec §A1.1 / A2-FINAL §3。

## Acceptance Criteria

- [ ] 1.1 10 态枚举完整（idle/breakthrough/tribulation/recovery/bottleneck/ascending/...）
- [ ] 1.2 ≥ 6 条主转移路径覆盖（happy path + error paths）
- [ ] 1.3 整数守卫条件（breakthrough 需 CultivationPoints≥threshold）
- [ ] 1.4 状态持久化：`CultivationState.Flags["cultPhase"]` 可续跑恢复
- [ ] 1.5 非法转移不抛（状态不变 + Chronicle 记录）
- [ ] 1.6 off 模式：cultPhase 不存在，不生效
- [ ] 1.7 全量绿 + IL 浮点零 + off 逐字节

## Implementation Notes

- 新建 `CultivationPhase.cs`（枚举 + 状态机转移表）
- 接入 `World.AdvanceCultivation`（在 A.0 的 `CultivationPoints` 累积后 check phase）
- 转移表：`Dictionary<(Phase, Trigger), Phase>` + 守卫 `Func<bool>` 
- 数据驱动：phase 配置可放在 `CultivationSchema` 中
- 暂不含劫/失败逻辑——仅状态骨架，内容后续填

## Test Evidence

**Required**: `tests/Jianghu.Core.Tests/Cultivation/CultivationPhaseTests.cs` — ~10 tests
**QA ref**: `production/qa/qa-plan-sprint-3.md` § cultivation-a1-001

## Out of Scope

- 三劫具体结算（TributionDef）→ 后续 story
- 破障四法 → 后续
- 寿元/飞升判定 → 后续（依赖 A1.4 UT 锚）
