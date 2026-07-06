# Systems Index — 九野 · 江湖涌现模拟

> **Status**: Living（2026-06-26 圆桌订正状态列对齐 production/epics/index.md）
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
| 7 | 战斗系统 (模块化) | `combat` | Core | Done | combat-system.md | adr-0002 | cultivation, actions | drama |
| 8 | 全量机制结构化 | `fullstruct` | Core | Deferred | combat-system.md | adr-0002 | combat | balance-cross |
| 9 | 跨路平衡标定 | `balance-cross` | Core | Designed | cultivation-system.md | — | combat, fullstruct | — |
| 10 | 修炼 A.1 余项 (劫/寿元) | `cultivation-a1-rest` | Core | Designed | cultivation-system.md | adr-0003 | cultivation, balance-cross | cultivation-a2 |
| 11 | 修炼 A.2 (道心/奇遇) | `cultivation-a2` | Feature | Done | cultivation-system.md | — | cultivation-a1-rest | cultivation-a3 |
| 12 | 修炼 A.3 (转职/双修) | `cultivation-a3` | Feature | Designed | cultivation-system.md | — | cultivation-a2 | — |
| 13 | 戏剧引擎 | `drama-engine` | Feature | Done | drama-system.md | — | combat, model | — |
| 14 | 地图系统 | `map-system` | Feature | Wired | (P8 补) | — | model | drama, faction |
| 15 | 门派 Faction | `faction` | Feature | C.0 Done | (P8 补) | — | model, map | drama |
| 16 | LLM 脑 | `llm-brain` | Feature | Not Designed | — | — | actions | — |
| 17 | 系统集成层 | `integration` | Feature | Partially wired | — | — | 全部 Core | — |
| 18 | 可视化 | `visualization` | Presentation | Spike Only | — | — | integration | — |
| 19 | Godot 宿主层（View/Host） | `godot-host` | Presentation | Planned | godot-architecture-manifest.md | adr-0004 | integration | — |
| 20 | 宏微观双层世界 | `macro-micro-world` | Presentation | Not Designed | godot-architecture-manifest.md §2 | (候选 adr-0005) | godot-host, map-system | — |
| 21 | 反应式回合战斗（QTE/弹反/韧性） | `reactive-combat` | Presentation | Not Designed | godot-architecture-manifest.md §3 + combat-system.md | adr-0002 | godot-host, combat | — |
| 22 | 动态江湖生成器（PCG） | `pcg` | Presentation | Not Designed | godot-architecture-manifest.md §4 | (候选 adr-0006) | godot-host | — |

**Status values**: Not Started | In Progress | In Review | Designed | Approved | Done | Deferred | Blocked | Planned | Not Designed | Wired

---

## Layer Definitions

| Layer | 含义 | 示例 |
|---|---|---|
| Foundation | 地基——无此游戏不成立 | World, Character, PRNG, Scheduler |
| Core | 核心玩法系统 | Cultivation, Combat, Balance |
| Feature | 上层特征——增强涌现/戏剧性 | Drama, Map, Faction, LLM-Brain |
| Presentation | 渲染/UI/可视化 | Pixel art, SVG UI, Godot rendering（Godot 4.x .NET，adr-0004） |

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
- **2026-07-03 引擎切换**：表现层目标 Unity→**Godot 4.x (.NET)**（[adr-0004](../../docs/architecture/adr-0004-godot-view-host-boundary.md)）。系统 #19-22（Godot 宿主/宏微观世界/反应式战斗/PCG）为 **View 层前瞻规范**，源真相 = [godot-architecture-manifest.md](../../docs/architecture/godot-architecture-manifest.md)；均 **Not Designed/Planned**，未实现（接入闸口=无头日志证核心无死锁，红线 A.10）。触及 Core 的开放调和项（柏林浮点/ECS/宏观同步回合）见 [architecture.md §10.2](../../docs/architecture/architecture.md)，候选 ADR 未裁决。
- Combat 系列已收官（combat-r2 Done；combat-fullstruct Deferred，待 balance-cross 验证近似档是否够用）
- `cultivation-a1-rest` 的 A1.4 blocked-on `balance-cross`
- `drama-engine` 已收官（drama-001~013 全落，1062 绿，`--drama` 激活）；`map-system`/`faction` 已接线（`--map`/`--faction`）
- ⚠️ `balance-cross` = Designed（非 Done）：契约 C1[40,60]% 未兑现，balance-003 硬闸门 Deferred（详见圆桌纪要 §1）
