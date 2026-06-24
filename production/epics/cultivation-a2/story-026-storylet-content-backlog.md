# Story 026: 奇遇内容积压（50+ 故事数据行）

> **Epic**: cultivation-a2
> **Status**: Backlog
> **Last Updated**: 2026-06-25
> **Layer**: Data
> **Type**: Config/Data
> **Estimate**: 持续（每次 0.1-0.2d 批）
> **Depends**: a2-013 (storylet executor)

## Context

A3-FINAL §4.4 定义 POOL_MIN ~60 条 storylet。a2-013 落地 ~10 条范例作为引擎验证模板。剩余 50+ 条为纯内容数据行——加内容=加数据行，不改引擎。

## Data Model

每 storylet = 1 行数据（JSON/YAML 或 C# 静态数据数组）：

```
StoryletDef:
  id: string          // 唯一标识 (e.g., "cave_relic_01")
  title: string       // 标题 (中)
  category: enum      // Treasure/Battle/Mentor/Trade/Fate/Relation
  salience: int       // 初始显著性 (0-100)
  minRealm: int       // 最低境界
  rarity: enum        // Common/Uncommon/Rare/Epic
  options: []         // 2-4 个选项
    text: string      // 选项文
    rewards: []       // daoHeart±/innerDemon±/resource±/progress±
    risk: string?     // 风险提示（可选）
```

## Content Backlog (50+ entries, prioritized by category)

### Treasure (宝藏类, 10 entries)
| # | ID | Title | Rarity |
|---|----|-------|--------|
| 1 | cave_relic_02 | 上古遗府 | Rare |
| 2 | cave_relic_03 | 灵石矿脉 | Common |
| 3 | cave_relic_04 | 丹师遗蜕 | Uncommon |
| 4 | cave_relic_05 | 剑冢残剑 | Rare |
| 5 | cave_relic_06 | 千年灵药 | Uncommon |
| 6 | cave_relic_07 | 古传送阵 | Epic |
| 7 | cave_relic_08 | 功法残卷 | Common |
| 8 | cave_relic_09 | 天材地宝 | Rare |
| 9 | cave_relic_10 | 秘境入口 | Epic |
| 10 | cave_relic_11 | 灵泉 | Common |

### Battle (战斗类, 10 entries)
| # | ID | Title | Rarity |
|---|----|-------|--------|
| 11 | ambush_01 | 散修劫道 | Common |
| 12 | ambush_02 | 妖兽突袭 | Uncommon |
| 13 | ambush_03 | 邪修伏击 | Uncommon |
| 14 | ambush_04 | 宗门截杀 | Rare |
| 15 | ambush_05 | 天魔降临 | Epic |
| 16 | ambush_06 | 蛊群来袭 | Uncommon |
| 17 | ambush_07 | 仇家寻仇 | Common |
| 18 | ambush_08 | 禁地守卫 | Rare |
| 19 | ambush_09 | 心魔外化 | Rare |
| 20 | ambush_10 | 兽潮 | Epic |

### Mentor (指点类, 10 entries)
| # | ID | Title | Rarity |
|---|----|-------|--------|
| 21 | mentor_01 | 隐世高人 | Rare |
| 22 | mentor_02 | 前辈遗念 | Uncommon |
| 23 | mentor_03 | 天机感悟 | Rare |
| 24 | mentor_04 | 剑意传承 | Epic |
| 25 | mentor_05 | 同门论道 | Common |
| 26 | mentor_06 | 古籍解读 | Uncommon |
| 27 | mentor_07 | 丹方交换 | Common |
| 28 | mentor_08 | 神识共鸣 | Rare |
| 29 | mentor_09 | 梦中授道 | Epic |
| 30 | mentor_10 | 观摩渡劫 | Rare |

### Trade (交易类, 8 entries)
| # | ID | Title | Rarity |
|---|----|-------|--------|
| 31 | trade_01 | 地下坊市 | Uncommon |
| 32 | trade_02 | 秘境交易会 | Rare |
| 33 | trade_03 | 灵药交换 | Common |
| 34 | trade_04 | 功法买卖 | Uncommon |
| 35 | trade_05 | 消息贩卖 | Common |
| 36 | trade_06 | 以物易物 | Common |
| 37 | trade_07 | 炉鼎交易 | Rare |
| 38 | trade_08 | 灵兽出售 | Uncommon |

### Fate (命运类, 8 entries)
| # | ID | Title | Rarity |
|---|----|-------|--------|
| 39 | fate_01 | 因果显现 | Epic |
| 40 | fate_02 | 天劫征兆 | Rare |
| 41 | fate_03 | 命格异变 | Rare |
| 42 | fate_04 | 轮回忆起 | Epic |
| 43 | fate_05 | 天道眷顾 | Rare |
| 44 | fate_06 | 血光之灾 | Uncommon |
| 45 | fate_07 | 贵人降临 | Rare |
| 46 | fate_08 | 前世因果 | Epic |

### Relation (关系类, 8 entries)
| # | ID | Title | Rarity |
|---|----|-------|--------|
| 47 | relation_01 | 联姻提议 | Rare |
| 48 | relation_02 | 师徒缘分 | Rare |
| 49 | relation_03 | 结义盟誓 | Uncommon |
| 50 | relation_04 | 门派邀请 | Uncommon |
| 51 | relation_05 | 道侣相遇 | Rare |
| 52 | relation_06 | 恩怨化解 | Uncommon |
| 53 | relation_07 | 误伤道歉 | Common |
| 54 | relation_08 | 共渡难关 | Rare |

## Out of Scope

- 故事文本润色——当前为占位标题
- 故事美术资源——引擎落地后补充
- 故事平衡——归 balance-003 后续

## Test Evidence Requirement

**Type**: Config/Data — data validation test. ≥60 entries, ≥6 categories, no duplicate IDs, all required fields non-empty.
