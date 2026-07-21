# Story 008: 非战斗机制（丹改四维 + 卖丹经济晋升）

> **Epic**: combat-fullstruct
> **Status**: Complete（2026-07-21 — DanModifyStat 测试就绪，1281绿）
> **Layer**: Core
> **Type**: Logic
> **Estimate**: 小 (0.5d)
> **Depends**: combat-r2 done (Modules/DuelEngine 就位)
> **ADR**: adr-0002-module-factory-effect-system
> **GDD**: cultivation-system.md §辅助路 / combat-system.md §非战斗

## Context

辅助路（丹修/阵修/器修/符修）的战斗价值体现在"非战斗机制"——丹修改四维（丹药提升他人属性）、卖丹换 realm 晋升（经济路径）、阵修布结界、器修炼法宝。当前这些机制仅在设计层，代码未实现。

本 story 聚焦丹修的最小非战斗闭环：DanModifyStat effect + 经济晋升 gate。

**深度源**: combat-fullstruct EPIC §Scope / Modules.cs / DanXiuPath.cs。

## Acceptance Criteria

- [x] 8.1 DanModifyStat effect 落地：丹修可以对目标施放 ModifyStat 效果
- [x] 8.2 四维 Apply 钳位 [0, StatCap]（不破运行期纪律，Σ=80 仅生成期）
- [x] 8.3 经济晋升 gate：卖丹收益 → CultivationPoints 增益或 realm 晋升条件
- [x] 8.4 丹修不能无限刷自己（频率 cap 或资源冷却）
- [x] 8.5 全量绿 + IL 浮点零 + off 逐字节

## Implementation Notes

- `Modules.cs` 新增 `ModifyStat(Kind, amount, targetKind, ...)` 工厂方法
- `ModuleResolver.cs` 新增 ModifyStat 分支
- 经济晋升：`DanXiuPath.cs` 中加 `TryEconomyAdvance(cs, resources)` 方法
- 频率 cap：per-action 或 per-tick 冷却（简单版：每次 TrainAction 只能卖一次）

## Test Evidence

**Required**: `tests/Jianghu.Core.Tests/Cultivation/DanModifyStatTests.cs` — ~6 tests
**QA ref**: `production/qa/qa-plan-sprint-3.md` § fullstruct-008

## Out of Scope

- 阵修结界/器修炼宝（后续 FULLSTRUCT）
- 药效曲线（品质/稀有的差异化衰减）
- 多目标批量改四维
