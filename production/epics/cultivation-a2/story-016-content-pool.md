# Story 016: 奇遇内容池POOL_MIN

> **Epic**: cultivation-a2
> **Status**: Complete （A.6 审计 2026-06-26 订正：git/代码证据证实已实现，台账滞后）
> **Last Updated**: 2026-06-24
> **Layer**: Core
> **Type**: Logic
> **Estimate**: 中 (1d)
> **Depends**: story-013
> **ADR**: adr-0001-integer-determinism
> **GDD**: A3-FINAL §4.4

## Context

A3-FINAL §4.4 定义了奇遇内容池的最小规模要求——POOL_MIN ~60 条 storylet，保证 INV-VARIETY-CONTENT 不变量。内容太少会导致 NPC 反复撞同几条奇遇，行为塌缩。

## Acceptance Criteria

- [ ] 16.1 `POOL_MIN = 60`——最少 60 条 storylet 定义（当前内置 ≥10，预留扩展槽）
- [ ] 16.2 `DIVERSITY_MIN`——至少覆盖 ≥5 个 Category（宝藏/战斗/指点/交易/迷途/…）
- [ ] 16.3 `StoryletRegistry` 支持外部加载（文件/json/代码注册），加内容=加文件不改引擎
- [ ] 16.4 `INV-VARIETY-CONTENT` 不变量：任意 200 tick 窗口内触发的奇遇种类 ≥ POOL_MIN / 3 (=20)
- [ ] 16.5 内容池可诊断：dump 命令输出当前池大小/种类数/未触发奇遇列表
- [ ] 16.6 确定性：内容加载顺序确定（Ordinal sort），索引确定
- [ ] 16.7 off 模式不加载

## Implementation Notes

**Storylet 分类**（≥5 Category）：
1. **宝藏类**：遗宝/灵药/秘籍——progress+ / resource+
2. **战斗类**：伏击/妖兽/邪修——innerDemon+ / spar may trigger
3. **指点类**：前辈/遗迹/梦境——daoHeart+ / Epiphany 概率
4. **交易类**：市井/坊市/秘市——resource 互换
5. **命运类**：天象/因果/劫数——重大转折，rare
6. **关系类**：同门/仇敌/路人——relation±

**StoryletDef 模板**：
```csharp
new StoryletDef(
    id: "cave_relic_01",
    title: "山洞遗宝",
    category: StoryletCategory.Treasure,
    salienceInit: 80,
    options: [
        new StoryletOption("取宝", rewards: [daoHeart+3, resource("灵石", 50)], risk: innerDemon+1),
        new StoryletOption("离去", rewards: [daoHeart+1], risk: none)
    ]
)
```

## Out of Scope

- 完整 60 条内容库——10 条内置 + 50 条预留模板。内容填充后续 sprint。
- Drama engine B 集成（→ drama-engine epic）

## Test Evidence Requirement

**Type**: Logic — automated unit tests. POOL_MIN enforce, DIVERSITY_MIN category count, registry load, determinism, off mode.
