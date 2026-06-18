# Core / Unity 职责拆分设计

> 2026-06-18. 明确哪些内容在迁到 Unity 前必须完成、哪些可在 Core 层设计但 Unity 层落地、哪些是 Unity 专属。

---

## 分层架构

```
┌─────────────────────────────────────────┐
│  Unity 宿主层 (后期)                      │
│  渲染 · 玩家输入 · 即时窗口 · UI · 音效    │
│  依赖: UnityEngine.*, 实时帧, 浮点判定     │
├─────────────────────────────────────────┤
│  Core 逻辑层 (netstandard2.1, 当前)       │
│  模拟 · 战斗 · 修炼 · 数据 · AI决策        │
│  依赖: 无引擎API, 纯整数, 确定性, headless  │
└─────────────────────────────────────────┘
```

**铁律**: Core 层必须零改写进 Unity（`netstandard2.1` → Unity IL2CPP 直接引用）。Core 永远不含 `UnityEngine.*`、不含浮点（`Jianghu.Cultivation` 命名空间）、不含实时帧/玩家输入。

---

## 13 Epic 分层归属

### 一、Core 层 — 迁 Unity 前必须完成（P0）

这些是 MVP 的最小可行核心。Unity 宿主启动即用，不再修改。

| Epic | 核心内容 | 当前状态 | 预估剩余 |
|------|---------|:------:|:------:|
| **combat-r2** | 模块化效果系统 + DuelEngine + 法宝数据 | ✅ **DONE** (Sprint 1 完成) | 0 |
| **balance-cross** | 跨路战力校准 + INV-CROSS gate | ✅ **DONE** (mul校准+proxy gate) | G3真对拍 ~1d |

**迁 Unity 前置 gate**:
- [x] 402 绿，off 逐字节 ✓
- [x] 战力 spread 44×→2.3× ✓
- [ ] G3 真对拍胜率 [40,60]% (需 StatGenerator + 200局/对)
- [ ] ADR 文件创建 (adr-0001/0002/0003)

---

### 二、Core 层 — 迁 Unity 前可完成（P1，高价值）

这些是纯逻辑、无引擎依赖。在 Core 做完，Unity 直接收益。

| Epic | 核心内容 | 当前状态 | 预估 |
|------|---------|:------:|:----:|
| **combat-fullstruct** | derived:* 真求和 + 克制矩阵 SituationalEdges + dot 完整时序 + 召唤物系统 + 唯一档全迁 + 非战斗机制(丹改四维/经济晋升) | **设计完，0 代码** | 大 (3-5 sprints) |
| **cultivation-a1-rest** | 剩余修炼路线(辅助路 UT 已锚) + 境界投影/查询完 | 部分完成 | 中 (1-2 sprints) |
| **cultivation-a2** | 道心 / 破单调 / 奇遇 storylet / 闭关(QBN+DES) | **设计完，0 代码** | 大 (2-3 sprints) |
| **cultivation-a3** | 突破劫 / ResistTerms / 渡劫门槛 | 设计待补 | 中 |
| **drama-engine** | 恩怨 / 复仇 / storylet 系统 | **设计完，0 代码** | 中 (1-2 sprints) |
| **faction** | 宗门系统 + 势力关系 + 气运 | 设计待补 | 大 (2-3 sprints) |
| **map-system** | 江湖地图 + 门派排布 + 资源点 + 商路 | 设计待补 | 中 |
| **integration** | Core 层各系统集成 + 全流程 smoke | 0 代码 | 中 |

**合计剩余 Core 工作量**: ~10-15 sprints（假设 sprint = 1 周）

---

### 三、Core 层可设计、Unity 层实现（P2，分立）

这些有两部分：Core 提供数据/接口/确定性算法，Unity 提供交互/渲染/即时判定。

| 系统 | Core 负责 | Unity 负责 | 状态 |
|------|----------|-----------|:----:|
| **即时窗口 (闪避/格挡/打断)** | q_core 整数退化版（NPC×NPC）✅ 已实现 | 浮点 q 公式 + 按键时机采集 + 窗口 UI | Core ✅ |
| **速度序列 CTB** | speed 公式 + 行动队列逻辑 | 行动条 UI 渲染 + 玩家选招菜单 | Core 未做 |
| **法宝Effects[] 结算** | EffectOp 模块链 + DuelEngine 集成 ✅ | 法宝视觉效果 + 音效 | Core ✅ |
| **NPC 决策** | RuleBrain 决策 + 难度曲线 | 玩家输入替代 NPC 决策 | Core 有基础 |
| **可视化 (游戏世界)** | World 状态快照 (StateSnapshot) | 像素 tile/角色/物品 渲染 (Pillow) | Spike only |
| **可视化 (UI)** | 战斗状态/HP/资源 数据接口 | 古风 UI (SVG/HTML-CSS 水墨卷轴) | 0 代码 |

---

### 四、Unity 专属 — Core 不做（P3）

这些完全在 Unity 层，Core 不涉入。

| 系统 | 说明 | 依赖 |
|------|------|------|
| **即时按键判定** | 玩家按键 vs 窗口中心偏差 Δ → 浮点正态 q | UnityEngine.Input |
| **窗口 UI** | CTB 行动条、选招菜单、战斗 HUD、q 评级反馈 | UGUI/UI Toolkit |
| **视觉特效** | 剑光/雷闪/毒雾/金光/残影 | Shader/ParticleSystem |
| **音效** | 即时窗口成功/失败音、蓄力读条音、门派战斗乐 | AudioSource |
| **场景渲染** | 3D/2D 场景、角色动画、摄像机 | 引擎渲染管线 |
| **平台适配** | 桌面 build、输入映射、分辨率 | Player Settings |
| **存档/读档** | 持久化（可用 Core 的 StateSnapshot 序列化） | Unity + Core 协作 |

---

### 五、可双轨推进（Core + Unity 并行）

| 工作流 | Core 侧 | Unity 侧 |
|--------|---------|---------|
| **新路径/功法** | PathDef + ArtDef + CombatSkillDef 数据 | 功法图标/特效资源 |
| **新法宝** | ArtifactDef 数据 + EffectOp 模块 | 法宝模型/特效 |
| **平衡调整** | RealmMultipliers + PowerFormula | 无需改动 |
| **AI 行为** | RuleBrain + DecisionContext | 玩家 override |
| **事件叙事** | Chronicle + DomainEvent 产生 | 剧情演出/对话 UI |

---

## 迁 Unity 前置条件 Gate List

在开始 Unity 宿主项目前，Core 层必须达到：

### Hard Gates（必须）

- [ ] ✅ 全 21 路径数据完整（PathDef + ArtDef + CombatSkillDef）— DONE
- [ ] ✅ 战斗系统可 headless 结算（DuelEngine.ResolveR2）— DONE
- [ ] ✅ 法宝系统数据完整（200+ ArtifactDef）— DONE
- [ ] ✅ 模块效果系统全部 EffectOpKind 有真分支 — DONE
- [ ] ✅ off 逐字节一致性 — DONE (OffByteIdenticalTests)
- [ ] ✅ 整数确定性 + IL 浮点零 — DONE
- [ ] ✅ 战力跨路收敛（spread < 3×）— DONE (2.3×)
- [ ] 全量测试 ≥ 500（当前 402）
- [ ] ADR 文档齐全（adr-0001/0002/0003 至少）
- [ ] Core API 文档（哪些 public 方法供 Unity 调用）
- [ ] StateSnapshot 序列化/反序列化可用（Unity 读 World 状态）

### Soft Gates（建议）

- [ ] combat-fullstruct: derived:* 真求和（否则 Σ鬼兵/Σ傀儡 恒0）
- [ ] combat-fullstruct: 克制矩阵完整（否则无元素相克/灭阴×3）
- [ ] drama-engine: 基础恩怨/复仇（否则 NPC 无行为动机）
- [ ] cultivation-a2: 道心系统（否则突破劫无 ResistTerms）
- [ ] integration: 端到端 smoke test（headless 全流程跑通）
- [ ] 性能基线: 100 NPC × 1000 ticks < 10s（headless）

---

## 推荐实施路线

```
Phase 1 (当前 → Sprint 2-4): Core P1 补全
  ├─ combat-fullstruct (derived:* + 克制矩阵 + dot时序 + 召唤)
  ├─ cultivation-a1-rest (辅助路 UT 锚锁)
  └─ drama-engine (恩怨/复仇/storylet)

Phase 2 (Sprint 5-7): Core 深度系统
  ├─ cultivation-a2 (道心/破单调/奇遇/闭关)
  ├─ faction (宗门/势力/气运)
  └─ map-system (江湖地图/门派排布)

Phase 3 (Sprint 8-10): 集成 + Unity 启动
  ├─ integration (全流程 smoke + API doc)
  ├─ Unity 项目骨架搭建
  └─ 可视化 spike → 正式生产

Phase 4 (Sprint 11+): Unity 宿主层
  ├─ 即时窗口（玩家输入）
  ├─ CTB UI
  ├─ 战斗 HUD
  └─ 像素/古风 渲染管线
```

## 最简可行迁移（若赶时间）

如果要在 Sprint 3 就启动 Unity 项目，Core 只需：
1. combat-r2 完成 (✅)
2. cultivation-a1-rest 辅助路 UT 锚 (1 sprint)
3. integration 全流程 smoke (1 sprint)
4. StateSnapshot 序列化 (简单，已在 Core)

其余 epic（combat-fullstruct/a2/a3/drama/faction/map）可在 Unity 开发期间持续补，Core 数据更新后 Unity 直接引用新 DLL 即可（无需改 Unity 代码，因为 Core→Unity 是单向依赖）。
