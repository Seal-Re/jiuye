# ADR-0005: 宏观同步回合 vs 累加器自动追帧（时间推进模型调和）

- **Status**: **Proposed**（张力登记，未裁决 — 红线 A.1 大方向交用户）
- **Date**: 2026-07-04（自 godot-architecture-manifest.md §2.1 落盘引出，architecture.md §10.2 R1 升格）
- **Last Verified**: 2026-07-04
- **Deciders**: huangjiaqi13（待裁决）+ Claude（architecture，张力起草）
- **Affects**: 未来 Godot 宿主层时间推进 / [adr-0004](adr-0004-godot-view-host-boundary.md) §② 固定时间步累加器 / `World.Advance` 的驱动语义（**当前 Core `Advance(int budget)` 不变**）

---

## Summary

Godot Manifest §2.1 要求大世界"**同步回合制**——玩家不操作时世界绝对静止，玩家决策一次则内核全局结算一次 `Tick()`"；而 adr-0004 §② 的固定时间步累加器含"**观察态自动追帧**"（宿主累加真实时间自动 `Advance`）。二者对"无玩家输入时世界是否推进"给出相反答案。本 ADR **登记这一张力并列出备选**，**不裁决**——裁决需用户定"这是观察者模拟还是玩家驱动战棋"的产品方向。

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Godot 4.x (.NET)（宿主层；Core 引擎无关） |
| **Domain** | Scripting / Core（时间推进语义） |
| **Knowledge Risk** | LOW — 纯架构语义，不依赖具体 Godot API |
| **References Consulted** | [godot-architecture-manifest.md](godot-architecture-manifest.md) §2.1 / [adr-0004](adr-0004-godot-view-host-boundary.md) §② |
| **Post-Cutoff APIs Used** | None（本 ADR 不写代码） |
| **Verification Required** | 裁决落地时：宿主 `_Process` 在两种模式下的 `Advance` 触发条件实测 |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | [adr-0004](adr-0004-godot-view-host-boundary.md)（Godot View/Host 边界 — 本 ADR 细化其 §② 的驱动语义）；[adr-0001](adr-0001-integer-determinism.md)（无论选哪案，`delta` 都不进 Core） |
| **Enables** | 宏微观双层世界 epic（systems-index #20 `macro-micro-world`）；玩家介入交互态设计 |
| **Blocks** | 「Godot 宿主时间推进」落地——未裁决前不实现自动追帧/纯回合任一分支 |
| **Ordering Note** | 属 View 层前瞻，**内核前置于此**（红线 A.10）。当前 headless Core 不受影响（`Advance` 已是"吃步不吃 delta"）。 |

---

## Context

### 问题
manifest §2.1（大世界层）："时间推演：**同步回合制**。玩家不操作时，世界绝对静止；玩家移动/决策一次，底层内核全局结算一次 `Tick()`。" —— 纯**玩家驱动步进**，无输入=无推进。

adr-0004 §②（固定时间步累加器）："**观察态（自动播放）**：宿主累加真实时间自动追帧；**交互态**：玩家 step/命令触发 `Advance`。" —— 含**观察态自动推进**（无玩家输入时世界仍按真实时间流逝追帧）。

**冲突点**：无玩家输入时，世界该不该动？manifest 说"绝对静止"，adr-0004 说"观察态自动追帧"。这不是措辞差异，是**产品形态分歧**——是"NPC 自主演化的江湖观察器"（世界自己跑）还是"玩家驱动的策略棋局"（玩家不动则静止）。

### 现状（当前无冲突）
- Core `World.Advance(int budget)` **吃逻辑步预算，不吃 `delta`**（active.md Phase-2 盘点证实）——无论选哪案，Core 侧 API 都不用改。
- 当前 CLI 是自动追帧型（`dotnet run -- seed budget` 一次跑完 budget 步），对应 adr-0004"观察态"。**game-concept.md** 现有产品定位 = "玩家是江湖观察者/编剧"（v1）+ "后期玩家介入"（Godot 阶段）—— **二者本就并存**，暗示答案可能是"分治"而非"二选一"。

### 约束
- **`delta`（浮点帧时）绝不进 Core**（adr-0004 §② / 红线 B.2）——本 ADR 任何备选都守此线。
- **同种子逐字节可复现不破**（B.2）——自动追帧的"追几步"由累加器决定，但每步轨迹仍确定（adr-0004 已论证）。
- 裁决是**产品方向决策**，非纯技术——故 defer 给用户（红线 A.1）。

---

## Decision

> **DEFERRED（未裁决）。** 本 ADR 仅登记张力 + 备选。下列方案的取舍取决于产品定位（观察器 vs 玩家驱动棋局），属大方向决策，交用户。

### 候选备选

#### 备选 A：分治（宏观玩家驱动步进 + 微观箱庭累加器插值）— 倾向项
- **描述**：大世界层（macro）取 manifest §2.1 纯玩家驱动——玩家决策一次 → `Advance` 一"宏步"，无输入则静止；微观箱庭战斗（manifest §2.2）取 adr-0004 §② 累加器——进入战斗后按 `SimStepSeconds` 自动追帧推进战斗 tick + 渲染插值。
- **Pros**：两规范各自成立、不打架；符合 game-concept"观察 + 介入"并存定位；宏观静止=省算力（视野外 NPC 降维为数据点，manifest §2.1）。
- **Cons**：两套时间驱动逻辑需清晰边界（何时切宏/微）；"观察者想看 NPC 自动演化"的诉求在宏观层丢失（除非加"自动播放"开关）。
- **未决点**：宏观是否保留一个可选"自动播放"模式（= 观察器诉求）？

#### 备选 B：统一纯玩家驱动（manifest §2.1 全局优先）
- **描述**：全局取"无输入=静止"，adr-0004 的"观察态自动追帧"降级为可选调试模式。
- **Pros**：语义最简单一致；强化"玩家驱动棋局"定位。
- **Cons**：与 v1 CLI 观察器形态（自动跑 budget 步）、game-concept"江湖观察者/编剧"定位冲突；纯 NPC 涌现模拟（当前核心卖点）失去"世界自己活"的自动性。
- **未决点**：牺牲观察器形态是否可接受？headless 回归测试怎么跑（测试恰恰依赖自动推进 N 步）？

#### 备选 C：统一自动追帧（adr-0004 §② 全局优先）
- **描述**：全局取累加器自动追帧，manifest §2.1"绝对静止"改为"玩家介入时可暂停/单步"。
- **Pros**：保留 v1 观察器形态；与现 CLI/测试一致。
- **Cons**：与 manifest §2.1 明文"世界绝对静止"直接冲突（需用户确认改规范）；微缩沙盒"33 号远征队式"体验通常期望玩家驱动节奏感。

### 影响面（无论哪案）
- Core `Advance` API **不变**（吃步不吃 delta）；差异全在**宿主层何时调 `Advance`**。
- adr-0004 §② 需据裁决更新（"观察态自动追帧"表述范围）。

---

## Consequences

### Positive（登记本身的价值）
- 张力显式化，避免宿主层开工时才发现两规范打架（规范先行，红线 A.10）。
- 备选 A 已识别为倾向项——多数情况下"宏观玩家驱动 + 微观累加器"是微缩沙盒策略游戏的标准解。

### Negative
- 未裁决 = 宏微观世界 epic 与玩家介入设计暂不能定稿时间模型。

### Neutral
- 当前 headless Core 与 CLI **完全不受影响**——本 ADR 纯属未来宿主层前瞻。

---

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|-----------|
| 迟迟不裁决阻塞宿主层 | 中 | 中 | 宿主层本就在 A.10 闸口后（内核前置）；不急于本阶段裁 |
| 裁决后 adr-0004 §② 需改 | 高 | 低 | 纯文档更新，Core 不动 |
| 误把 `delta` 引入 Core 图省事 | 低 | 高（破 B.2） | P-FORBIDDEN-2 code review 守；本 ADR 重申 |

## Validation Criteria（裁决落地后）

- [ ] 用户裁定产品方向（观察器 / 玩家驱动棋局 / 混合）
- [ ] 选定备选写入本 ADR（Status → Accepted）+ 同步更新 adr-0004 §②
- [ ] （宿主层未来）宏/微时间驱动边界有测试覆盖；`delta` 不进 Core（P-FORBIDDEN-2）

## GDD Requirements Addressed

| GDD Document | System | Requirement | How This ADR Satisfies It |
|-------------|--------|-------------|--------------------------|
| [godot-architecture-manifest.md](godot-architecture-manifest.md) | 宏微观双层世界 §2 | "玩家不操作时世界绝对静止 / 决策一次结算一次 Tick" | 登记其与 adr-0004 累加器的张力，列备选待裁 |
| `design/gdd/game-concept.md` | Foundation | "玩家是江湖观察者/编剧（v1）+ 后期玩家介入" | 备选 A 分治调和二者并存定位 |

## Related

- [adr-0004](adr-0004-godot-view-host-boundary.md) §②（固定时间步累加器）— 本 ADR 细化其驱动语义的未决部分
- [adr-0001](adr-0001-integer-determinism.md)（整数确定性）— `delta` 不进 Core 是所有备选的共同底线
- [tr-registry.yaml](tr-registry.yaml) `TR-VIEW-R1`（本张力的溯源锚）
- [architecture.md](architecture.md) §10.2 R1（张力首次登记处）
