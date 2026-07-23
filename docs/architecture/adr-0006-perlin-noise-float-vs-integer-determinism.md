# ADR-0006: 柏林噪声浮点 vs B.2 整数确定性（PCG 地形生成层归属）

- **Status**: **Accepted**（2026-07-23 — 用户裁定备选 B: View→Core 快照）
- **Date**: 2026-07-04 / Ruled: 2026-07-23
- **Last Verified**: 2026-07-23
- **Deciders**: huangjiaqi13 + Claude（architecture）
- **Affects**: 未来 PCG 生成管线（systems-index #22 `pcg`）/ `Jianghu.Sim.WorldMap`（整数图拓扑）/ 若地形入 Core 则触 [adr-0001](adr-0001-integer-determinism.md) B.2（**当前无 PCG 代码**）

---

## Summary

Godot Manifest §4 的 PCG 管线用"**微观柏林噪声填充地形特征**"，柏林噪声天然是浮点算法（梯度点积 + 平滑插值）。若地形生成**结果入 Core**（manifest §2.1 "A* 寻路权重与实体轻功/境界挂钩"、§2.2 "地形/天气应用属性乘区 Buff"），则浮点撞红线 B.2（`Jianghu.Cultivation` 及全逻辑层禁浮点）。本 ADR **登记这一张力并列备选**（整数/定点柏林 vs 噪声只在 View），**不裁决**——裁决需先定"地形数据是否参与确定性战力/寻路结算"。

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Godot 4.x (.NET)（TileMap 渲染在宿主；生成层归属 = 本 ADR 待裁） |
| **Domain** | Core（确定性）/ Navigation（A* 权重）/ Rendering（TileMap） |
| **Knowledge Risk** | LOW — 柏林噪声与整数化技术均在训练数据内 |
| **References Consulted** | [godot-architecture-manifest.md](godot-architecture-manifest.md) §4 / §2.1 / §2.2 / [adr-0001](adr-0001-integer-determinism.md) / [adr-0004](adr-0004-godot-view-host-boundary.md) §③ |
| **Post-Cutoff APIs Used** | None（本 ADR 不写代码） |
| **Verification Required** | 裁决落地时：整数/定点柏林的跨运行时逐字节一致实测（若选定点方案）；或 View 生成 + 整数化结果的确定性边界测试 |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | [adr-0001](adr-0001-integer-determinism.md)（整数确定性 — 本张力的根源约束）；[adr-0004](adr-0004-godot-view-host-boundary.md) §③（iso/地图坐标只属 View 的预留） |
| **Enables** | PCG epic（systems-index #22）；宏微观世界地形层（#20） |
| **Blocks** | 「柏林地形生成」落地——未裁决前不实现任何噪声代码（当前 0 行，adr-0004 §③ 预留一致） |
| **Ordering Note** | 三张力中**最可能先阻塞落地**者（PCG 是宏微观世界的地基）。仍属 View 层前瞻，内核前置（A.10）。 |

---

## Context

### 问题
manifest §4 生成管线第 3 步："微观**柏林噪声**填充地形特征"。柏林/单纯形噪声的标准实现依赖：梯度向量点积、`fade()` 五次平滑插值（`6t⁵−15t⁴+10t³`）、浮点坐标 —— **本质浮点**。

关键分歧在**噪声结果的去向**：
- 若结果**仅供渲染**（TileMap 选哪张贴图、地形视觉）→ 属 View，浮点合法（adr-0004 §③ "iso 投影是 View 专属"同理），**无冲突**。
- 若结果**入 Core 参与确定性结算**——manifest 明文两处需要：
  - §2.1："底层 A* 寻路权重与实体轻功/境界能力挂钩" → 地形权重进寻路 = 进 Core 逻辑；
  - §2.2："应用对应的材质贴图与**底层属性乘区 Buff**" → 地形 Buff 进战力结算 = 进 `Jianghu.Cultivation` 邻域。
  - → **浮点噪声值若直接进这些整数确定性路径，撞 B.2**（IEEE754 跨后端舍入分歧破逐字节一致，adr-0001 §2）。

### 现状（当前无冲突）
- **PCG 未实现**（systems-index #22 Not Designed）；`Jianghu.Sim.WorldMap` 是 **Kruskal MST 整数图拓扑**（int 邻接表 + NodeId，无 X/Y/浮点，active.md Phase-2 盘点证实）——不含任何噪声/空间地形。
- adr-0004 §③ 已立"Core 只认整数逻辑格 `(int gx,int gy)`，iso 投影只属 View"——本 ADR 是其在"地形**数值**"维度的延伸（§③ 管坐标，本 ADR 管地形属性值）。

### 约束
- **`Jianghu.Cultivation` 及全逻辑层禁浮点**（红线 B.2，`ILFloatScanner` IL 扫描守）——硬约束，不可协商。
- **同种子逐字节跨运行时一致**（B.2/adr-0001）——若地形入 Core，其生成必须整数确定。
- 裁决前置问题 = **产品/设计决策**："地形是否参与确定性战力/寻路"——故 defer 用户（A.1）。

---

## Decision

> **ACCEPTED: 备选 B — View 生成 → 量化整数网格 → Core 快照。**

### 裁决（2026-07-23，huangjiaqi13）

**选择备选 B**，理由：

1. **职责分离**：Core 专注于图论拓扑/数值演化/经济/门派/离散节点状态。让后端 C# 处理浮点柏林噪声是职责越位。View (Godot) 拥有原生 `FastNoiseLite` 工具，处理 2D 平滑连续地形最高效直观。

2. **实现管线（四步）**：
   - **Step 1 (View 生成)**：启动时 View 侧用 `FastNoiseLite`（基于固定 seed）生成 2D 地形大地图浮点/整数网格。
   - **Step 2 (数据提炼)**：View 侧按 Region 和 NodeId 采样聚合——计算该节点平均 TerrainKind/Element/Peril。
   - **Step 3 (单向快照写入)**：初始化阶段通过 WorldBridge 接口将离散地形数据写入 Core `NodeGeo` 和 `RegionDef`（补齐 mv-004 数据字段）。一次性写入，不复算。
   - **Step 4 (运行时独立)**：Core 拥有合法地形数据用于规则演化（RuleBrain/A* 权重/属性乘区）；View 根据同源数据四层渲染。双端逻辑闭环，互不污染。

3. **确定性保证**：地形网格是"生成期固化的整数快照"——存档存网格，Core 复现只读。不被运行期 RNG 消费破坏。同 seed 生成同网格（FastNoiseLite 同 seed = 同输出）。

### 前置判定树
- **若地形纯渲染、不入 Core** → 无 ADR 冲突，柏林浮点在 View 合法（同 adr-0004 §③ iso）。本 ADR 可直接 Accepted 为"噪声属 View"。
- **若地形入 Core**（manifest §2.1/§2.2 字面要求）→ 必走下列整数化备选之一。

### 候选备选（地形入 Core 时）

#### 备选 A：整数/定点柏林（查表 + 定点插值）
- **描述**：梯度表用整数向量，坐标用定点数（如 Q16.16），`fade` 用整数多项式或查表；输出整数地形值。全程整数 → 进 Core 合法。
- **Pros**：地形完全进确定性内核，A*/属性乘区直接用；同种子逐字节一致（跨 CoreCLR/AOT/Mono）。
- **Cons**：定点柏林实现复杂、精度需调（低精度出块状伪影）；`ILFloatScanner` 守护范围需确认覆盖新代码；性能需验（整数插值 vs 浮点）。
- **先例**：项目已有整数查表先例（adr-0001 §"不能用 System.Math 三角，改整数查表/定点"）。

#### 备选 B：噪声只在 View 生成，整数化结果经命令端口入 Core — 倾向项
- **描述**：柏林噪声在 Godot 宿主层（View）浮点生成 → 宿主把结果**离散量化为整数**（如地形枚举 `{平原=0,高山=1,水域=2,…}` 或整数权重档 `[0..100]`）→ 经 adr-0004 §① 命令端口/生成期种子注入进 Core。Core 只见整数地形网格。
- **Pros**：柏林用成熟浮点库（Godot `FastNoiseLite`）无需重造定点；Core 保持纯整数零妥协（B.2 天然守）；与 adr-0004 §③ "iso 投影 View 逆映射整数格进 Core"同构。
- **Cons**：**确定性责任转移**——地形生成不再是 Core 逐字节可复现的一部分（除非宿主量化也确定）；需约定"地形种子 → 整数网格"的固化机制（如生成期一次性写入、存档记录整数网格而非重算）；离散量化损失连续地形细节（多数策略游戏可接受）。
- **未决点**：地形网格是"生成期固化的整数快照"（存档存网格，Core 复现只读）还是"每次运行重算"？前者天然确定，倾向前者。

#### 备选 C：混合（渲染用浮点柏林，Core 逻辑用独立整数地形层）
- **描述**：View 的视觉地形（贴图/细节）用浮点柏林；Core 的逻辑地形（A* 权重/Buff）用**独立的整数生成**（如整数 Voronoi 区域 + 手配权重表），二者视觉对齐但数据分离。
- **Pros**：视觉丰富 + 逻辑确定各取所长；逻辑地形可设计师手调（数据驱动，manifest §4 "配置外置"）。
- **Cons**：两套地形需保持一致（视觉高山 = 逻辑高山），维护成本；"视觉≠逻辑"可能误导玩家。

### 与 adr-0004 §③ 的关系
adr-0004 §③ 管**坐标**（iso 屏幕像素 vs 整数逻辑格）；本 ADR 管**地形属性值**（浮点噪声 vs 整数地形）。二者同一原则的两面：**Core 只认整数，浮点温床全在 View**。备选 B 正是 §③ 模式在数值维的复用。

---

## Consequences

### Positive（登记本身的价值）
- 在 PCG 开工前锁定"浮点噪声不得裸进 Core"的底线，避免实现期撞 `ILFloatScanner` 返工。
- 备选 B 识别为倾向项——复用 adr-0004 §③ 的成熟模式（View 生成/整数化/命令端口）。

### Negative
- 未裁决 = PCG epic 不能定稿地形数据流。

### Neutral
- 当前 `WorldMap` 整数图拓扑与未来地形层分离（adr-0004 §③ 已述），不受本 ADR 影响。

---

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|-----------|
| 实现期浮点噪声裸进 Core 破 B.2 | 中 | 高 | `ILFloatScanner` IL 扫描（BLOCKING）；本 ADR 前置锁定 |
| 定点柏林精度不足出伪影（备选A） | 中 | 中 | 若选 A，先 spike 验精度；否则转备选 B |
| 备选 B 地形非确定（重算漂移） | 中 | 中 | 约定生成期固化整数网格快照，存档存网格 |

## Performance Implications

| Metric | Before | Expected After | Budget |
|--------|--------|---------------|--------|
| 地形生成（一次性） | N/A（无 PCG） | 备选B：View 浮点快；备选A：整数插值待测 | 生成期非热路径（F-PERF/C-PERF-2） |

> 地形生成属生成期一次性开销，非 `Advance` 主循环热路径（承 control-manifest C-PERF-2）。

## Validation Criteria（裁决落地后）

- [x] 用户答前置问题：地形入 Core 确定性路径（A* 权重+属性乘区），选备选 B
- [x] 选定备选写入本 ADR（Status → Accepted, 2026-07-23）
- [ ] （PCG 实现）Core 零浮点（`ILFloatScanner` 绿）—— Core 只见整数快照
- [ ] （PCG 实现）同 seed 地形网格逐字节一致（FastNoiseLite 确定性 + 快照固化）
- [ ] （PCG 实现）地形值不直接流入 `Jianghu.Cultivation` 结算（经 Core 整数字段中转）

## GDD Requirements Addressed

| GDD Document | System | Requirement | How This ADR Satisfies It |
|-------------|--------|-------------|--------------------------|
| [godot-architecture-manifest.md](godot-architecture-manifest.md) | PCG §4 | "微观柏林噪声填充地形特征" | 登记浮点噪声与 B.2 的张力，列整数化备选 |
| [godot-architecture-manifest.md](godot-architecture-manifest.md) | 宏微观世界 §2.1/§2.2 | "A* 权重挂境界 / 地形属性乘区 Buff" | 识别地形入 Core 的两处路径，界定确定性责任 |

## Related

- [adr-0001](adr-0001-integer-determinism.md)（整数确定性，B.2）— 本张力的根源约束
- [adr-0004](adr-0004-godot-view-host-boundary.md) §③（iso 坐标只属 View）— 本 ADR 是其在"地形数值"维的延伸；备选 B 复用其模式
- [tr-registry.yaml](tr-registry.yaml) `TR-VIEW-R2` / `TR-VIEW-005`（PCG 溯源锚）
- [architecture.md](architecture.md) §10.2 R2（张力首次登记处）
