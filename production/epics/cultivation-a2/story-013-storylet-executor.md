# Story 013: 奇遇storylet最小执行器

> **Epic**: cultivation-a2
> **Status**: Not Started
> **Last Updated**: 2026-06-24
> **Layer**: Core
> **Type**: Logic
> **Estimate**: 大 (1.5d)
> **Depends**: story-004 (Roam mode triggers encounter)
> **ADR**: adr-0001-integer-determinism
> **GDD**: A3-FINAL §4；A123 §A.2.4

## Context

A3-FINAL §4 定义了奇遇（storylet/encounter）系统——角色在游历（Roam）中有概率遭遇随机事件。这是"涌现叙事"的最小载体：奇遇可改变 daoHeart/innerDem/资源/关系/realm。先建最小执行器（触发→选择→结算），后续 drama-engine B 可替换故事内容层。

## Acceptance Criteria

- [ ] 13.1 `StoryletDef` record：Id, Title, Category, Triggers, Options, Rewards, SalienceInit
- [ ] 13.2 `StoryletExecutor`：Roam 模式下每 tick 骰 encounter（概率 = encounterRate * exposure）
- [ ] 13.3 触发时：从内容池选题（按 salience 加权），角色从 Options 中选 1（NPC 贪心选择）
- [ ] 13.4 结算：应用 Rewards（daoHeart±/innerDemon±/resource±/relation±/progress±）
- [ ] 13.5 每个角色每 `ActorMinGap=30` tick 最多触发 1 次奇遇
- [ ] 13.6 确定性：同种子→同 tick 触相同奇遇
- [ ] 13.7 内容与 drama engine B 解耦——storylet defs 独立文件，后期可替换
- [ ] 13.8 off 模式不触发奇遇

## Implementation Notes

**最小内容池**（≥10 条内置 storylet，覆盖常见事件）：
1. 山洞遗宝：daoHeart+3, 获得灵石
2. 邪修伏击：innerDemon+2, spar 可能
3. 前辈指点：breakProgress+10
4. 灵药发现：lifespanBonus + 轻微
5. 心魔试炼：innerDemon+5 或 daoHeart+5（option 分支）
6. 同门相助：relation+, daoHeart+2
7. 天象异变：随机 resource+/-, comprehension+
8. 古籍残卷：Epiphany 概率提升
9. 市井交易：资源交换
10. 荒野迷途：无收益，仅叙事

**Option 选择**（NPC）：贪心评分——max(daoHeartGain - innerDemonRisk + progressGain)。

## Out of Scope

- 频率 cap 收敛（→ story-014）
- 收益护栏（→ story-015）

## Test Evidence Requirement

**Type**: Logic — automated unit tests. Trigger rate (1000 tick sample), ActorMinGap enforce, deterministic replay, off mode.
