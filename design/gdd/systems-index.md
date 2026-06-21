# Systems Index — 九野 · 江湖涌现模拟

> **Status**: Designed
> **Created**: 2026-06-21（逆向自 production/epics/index.md + 源码架构）
> **Purpose**: 系统全景图——每一行对应一个游戏系统，含状态/层/依赖/GDD/ADR。

---

## Systems Table

| # | System | slug | Layer | Status | GDD | ADR(s) | Depends On | Blocks |
|---|---|---|---|---|---|---|---|---|
| 1 | 世界模拟内核 | `foundation` | Foundation | Done | game-concept.md | adr-0001 | — | 全部 |
| 2 | 确定性 PRNG | `random` | Foundation | Done | game-concept.md | adr-0001 | — | 全部 |
| 3 | 角色/属性/关系 | `model` | Foundation | Done | game-concept.md | — | foundation | combat, cultivation, drama |
| 4 | 事件驱动调度器 | `scheduler` | Foundation | Done | game-concept.md | — | model | actions, lifecycle |
| 5 | 动作系统 | `actions` | Foundation | Done | game-concept.md | — | scheduler, model | combat |
| 6 | 修炼系统 (21路) | `cultivation` | Core | Done | cultivation-system.md | adr-0003 | actions, model | combat |
| 7 | 战斗系统 (模块化) | `combat` | Core | In Progress | combat-system.md | adr-0002 | cultivation, actions | drama |
| 8 | 全量机制结构化 | `fullstruct` | Core | In Progress | combat-system.md | adr-0002 | combat | balance-cross |
| 9 | 跨路平衡标定 | `balance-cross` | Core | Designed | cultivation-system.md | — | combat, fullstruct | — |
| 10 | 修炼 A.1 余项 (劫/寿元) | `cultivation-a1-rest` | Core | Designed | cultivation-system.md | adr-0003 | cultivation, balance-cross | cultivation-a2 |
| 11 | 修炼 A.2 (道心/奇遇) | `cultivation-a2` | Feature | Designed | cultivation-system.md | — | cultivation-a1-rest | cultivation-a3 |
| 12 | 修炼 A.3 (转职/双修) | `cultivation-a3` | Feature | Designed | cultivation-system.md | — | cultivation-a2 | — |
| 13 | 戏剧引擎 | `drama-engine` | Feature | Designed | (P8 补) | — | combat, model | — |
| 14 | 地图系统 | `map-system` | Feature | Designed | (P8 补) | — | model | drama, faction |
| 15 | 门派 Faction | `faction` | Feature | Designed | (P8 补) | — | model, map | drama |
| 16 | LLM 脑 | `llm-brain` | Feature | Not Designed | — | — | actions | — |
| 17 | 系统集成层 | `integration` | Feature | Not Designed | — | — | 全部 Core | — |
| 18 | 可视化 | `visualization` | Presentation | Spike Only | — | — | integration | — |

**Status values**: Not Started | In Progress | In Review | Designed | Approved | Done | Deferred | Blocked

---

## Layer Definitions

| Layer | 含义 | 示例 |
|---|---|---|
| Foundation | 地基——无此游戏不成立 | World, Character, PRNG, Scheduler |
| Core | 核心玩法系统 | Cultivation, Combat, Balance |
| Feature | 上层特征——增强涌现/戏剧性 | Drama, Map, Faction, LLM-Brain |
| Presentation | 渲染/UI/可视化 | Pixel art, SVG UI, Unity rendering |

---

## Dependency Graph (关键路径)

```
Foundation ──→ Core ──→ Feature ──→ Presentation
    │              │
    └── model ──→ cultivation ──→ combat ──→ fullstruct
                     │                 │
                     └──→ balance-cross (blocked until combat done)
                     └──→ cultivation-a1-rest → a2 → a3
                                          │
              drama-engine ←─────────────┘
              map-system → faction
              llm-brain (independent, needs IBrain port)
              integration ← all Core systems
              visualization ← integration
```

---

## Notes

- 设计深度源在 `docs/legacy-specs/specs/`（18 份），原地保留；GDD 增量补
- Combat 是唯一活跃 WIP epic（combat-r2 + combat-fullstruct）
- `cultivation-a1-rest` 的 A1.4 blocked-on `balance-cross`
- `drama-engine` 已含 story-001（恩怨/复仇基础，Sprint 2 done），但 epic 仍标 Designed（0 code → 现在 1 story 已建）
