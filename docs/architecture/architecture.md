# 架构总览 — 九野 · 江湖涌现模拟

> **Status**: Living（主架构文档）
> **Created**: 2026-07-03（综合 CLAUDE.md §F + ADR-0001/0002/0003 + technical-preferences.md）
> **Scope**: Foundation 层 + Core 层。Feature 层（drama/map/faction/llm-brain）与 Presentation 层（可视化/Godot 宿主）仅点到接口边界，详见各自 GDD 与 §9 / §10 / ADR-0004。
> **View 层规范锚点**：Godot 表现层（View）、PCG（程序化生成）、2D 等距地图渲染的**前瞻规范真相源** = [godot-architecture-manifest.md](godot-architecture-manifest.md)（§10 索引其与内核红线的隔离关系）。
> **Traceability**: 每条关键决策回链其 ADR（见 §7 ADR 索引）。本文档 = 派生综合，源真相在 ADR / 红线 / 源码。

---

## 1. 概览

九野是一个**确定性、事件驱动的江湖涌现模拟内核**。核心设计意图是**分层宿主**：

- **`Jianghu.Core`（纯逻辑库，netstandard2.1）**：全部模拟机制。零引擎依赖——不引用 `Godot.*`（历史目标 `UnityEngine.*` 同禁），不做 IO。这是"Model"。
- **`Jianghu.Cli`（薄壳驱动，net8.0）**：当前阶段的"View/Host"——解析 CLI 参数 → 构造 World → 推进 → 打印文本快照。后期与 Godot 宿主**并列共存**（同一 Core，两个 View；headless 回归不受宿主影响）。
- **后期 Godot 宿主（Godot 4.x .NET）**：`Jianghu.Core` 为 `netstandard2.1`，Godot 4.x .NET（CoreCLR）可**直接引用**渲染 + 承接玩家介入。Core 保持无引擎 API 是这一"零改写"承诺的前提（见 §6、§9，ADR-0004）。

> **引擎目标 = Godot 4.x (.NET)**（2026-07-03 由 Unity 切换）。切换 ≈ 术语对齐 + 新增桥接红线（ADR-0004），**非架构重写**——Model/View 分离、B.2 整数确定性、B.3 off 逐字节三条地基一字不改。`netstandard2.1` TFM 保留（Godot 4.x .NET 直接引用，比 Unity asset 导入层更贴合标准 `dotnet` 工具链）。

**Model / View 分离**是贯穿全局的第一原则：模拟状态（World）与其呈现（CLI 文本 / 后期 Godot 渲染）严格解耦。View 只读 World 状态，不反向驱动确定性内核（回写只经显式命令端口，见 §9）。这使得同一 Core 可在 CLI（.NET 8 JIT）与 Godot（CoreCLR / 各平台 AOT）下产出**逐字节一致**的模拟轨迹（见 §5.1，ADR-0001）。

模拟有两个模式，由构造参数 `cultivation` 切换：
- **off 模式**（默认）：纯 v1.0 规则，无修炼。输出必须与最初 v1.0 **逐字节一致**（ADR-0003，红线 B.3）。
- **on 模式**：21 路修炼系统叠加在 v1.0 之上，走独立 PRNG 流，不扰动 off 路径。

---

## 2. 程序集职责

| 程序集 | TFM | 职责 | 关键约束 |
|---|---|---|---|
| `Jianghu.Core` | `netstandard2.1` | **纯逻辑库**——全部模拟机制（模型/PRNG/调度/动作/修炼/战斗/戏剧/事件）。 | 零引擎依赖；禁 `System.Random`/`Console`/`DateTime`/`Thread`（BannedApiAnalyzers）；`Jianghu.Cultivation` 禁浮点（IL 扫描）。后期直接被 Godot 4.x .NET 引用。 |
| `Jianghu.Cli` | `net8.0` | CLI 控制台驱动——薄壳，解析参数 → `WorldFactory.CreateInitial` → `World.Advance` → 打印快照。 | 当前 Host；后期与 Godot 宿主并列（同一 Core，两个 View）。可用 `System.Console`（非 Core）。 |
| `Jianghu.Core.Tests` | `net8.0` | xUnit 全量测试（1051 绿）：确定性（IL 浮点扫描 / 逐字节复现）、off 逐字节、21 路独立、战斗模块差分、drama 恩怨链。 | 回归基线 = 全量绿不退。 |

---

## 3. 命名空间职责（Core 内部）

| 命名空间 | 关键类型 | 职责 | 层 |
|---|---|---|---|
| `Jianghu.Model` | `Character`(聚合根), `Persona`, `MemoryStore`, `Relations`, `Sect`, `WorldNode` | 领域模型 | Foundation |
| `Jianghu.Random` | `IRandom`, `Pcg32`, `RngStreamIds` | 确定性 PRNG + 子流编号真相源 | Foundation |
| `Jianghu.Stats` | `StatBlock`(力/内/体/识), `StatGenerator` | 角色属性 | Foundation |
| `Jianghu.Config` | `LimitsConfig` | 配置边界（模拟规模约束） | Foundation |
| `Jianghu.Sim` | `World`, `WorldFactory`, `Lifecycle`, `Scheduler`, `StateSnapshot`, `WorldMap`(+`WorldMapFactory`/`KruskalMstGenerator`/`IGeoQuery`), `SectLedger`(+`IFactionQuery`), `IPipelineStage` | 世界模拟主循环 + 地图(`--map`) + 门派派系(`--faction`) | Foundation / Feature |
| `Jianghu.Actions` | `ActionSystem`, `SparAction`, `TrainAction`, `TravelAction` | 角色动作执行 | Foundation |
| `Jianghu.Events` | `Chronicle`, `DomainEvent` 子类型 | 事件溯源（追加日志） | Foundation |
| `Jianghu.Cultivation` | `PowerEngine`, `DuelEngine`, `CombatContext`, `CultivationPhase`(10态FSM), `TribulationResolver`, `LifespanAndFailure`, `Modules`(工厂), `ModuleResolver`, `EffectOp`, `SpecialModuleRegistry`, `PathRegistry`, `RealmCurve` 等 | 21 路完整修为系统（战斗引擎/门域/压制矩阵/修炼FSM/三劫/寿元） | Core |
| `Jianghu.Cultivation.paths` | `SwordImmortalPath`…共 21 文件 | 具体路径定义（`CodePathSource` 注册） | Core |
| `Jianghu.Cultivation.special` | `BrokenChainModule`…共 8 文件 | 唯一稀有度特殊模块（注册式插件） | Core |
| `Jianghu.Cultivation.Artifacts` | `ArtifactData`, `ArtifactRegistry` | 法宝系统 | Core |
| `Jianghu.Decide` | `IBrain`, `RuleBrain`(当前), `DecisionContext` | AI 决策端口（LLM 脑未建） | Feature |
| `Jianghu.Drama` | `DramaDirector`, `GrudgeLedger`, `RevengeArc`, `IgnitionScanner`, `DramaScheduler`, `IDramaView`/`IDramaMutator` 等 | 恩怨/复仇弧/跨代继承（`--drama`，Split(6) 流） | Feature |

> Feature 层（Drama/Map/Faction/Decide）在架构上"挂靠"于 Foundation/Core：走独立 PRNG 流、off 不激活、不污染 v1.0 record（侧表纪律，ADR-0003 §4）。

---

## 4. 执行模型（事件驱动 + 确定性推进）

1. **`WorldFactory.CreateInitial(seed, limits, initialCount, cultivation, pathSource?, mapOn, factionOn, dramaOn, dramaSeedFeuds)`** → 生成世界（角色/宗门/关系；可选地图/派系/恩怨）。
   > 概念开关：`--map`/`--faction`/`--drama`/`--drama-feuds` 在 CLI 层映射到上述 bool 参数；`pathSource` 是可选注入端口（默认 `CodePathSource`）。
2. **`World.Advance(budget)`** → 主循环：Scheduler 弹事件 → Action 执行 → Cultivation 推进 → Drama Pump → Lifecycle 计时 → 可能创生。单线程、确定性顺序。
3. 所有事件入 **`Chronicle`**（追加日志，事件溯源）→ Host 打印/渲染快照。
4. **`World.Clone()`** → 深拷贝快照（v1.0 用途）；所有 PRNG 子流深拷续跑（`CloneRng`/`CloneRngOrNull`，null 安全），保克隆后续跑不发散（见 §5.1）。

### 4.1 确定性 PRNG 与子流隔离

- **`Pcg32(seed, sequence)`**：种子驱动，全程 `uint`/`ulong` 整数运算。
- **`IRandom.Split(ulong streamId)`**：跳号派生独立子流，**不消费父状态**。这是子流隔离的机制核心——每个子系统拿到自己的确定性流，互不干扰。
- **`RngStreamIds`（冻结·append-only 单一真相源）**：

  | 子流 | id | 消费者 | 构造时机 |
  |---|---|---|---|
  | `Gen` | 1 | 生成期（属性/关系） | 总是 |
  | `Domain` | 2 | 领域事件 | 总是 |
  | `Spawn` | 3 | 创生/per-character | 总是 |
  | `Brain` | 4 | AI 决策 | 总是 |
  | `Cultivation` | 5 | 修炼系统 | **仅 cultivation-on** |
  | `Drama` | 6 | 恩怨/复仇弧 | **仅 dramaOn** |
  | `Map` | 7 | 世界地图 | **仅 mapOn** |
  | `Faction` | 8 | 门派派系 | **仅 factionOn** |

  **铁律**：新随机流 = **追加新 id，绝不复用既有编号**。Cultivation/Drama/Map/Faction 流在对应开关 off 时**不构造、绝不消费** `Split(5..8)` → 保证 `Split(1..4)` 的编号与消费序列在 off 下逐字节不变（ADR-0003）。新增 PRNG 消费者必须 ① 追加 `RngStreamIds` 新 id ② 进 `World.Clone`（深拷续跑）。

---

## 5. Foundation 层不变量

三条不变量构成整个内核的地基，均有 Accepted ADR 治理，均由测试机器守护。

### 5.1 整数确定性（ADR-0001，红线 B.2）

**`Jianghu.Cultivation` 及所有逻辑层全程禁浮点，全部整数确定性。** 同种子同输入 → 任何 .NET 运行时（CLI JIT / Godot CoreCLR / 各平台 AOT / Mono）逐字节一致。

- **PRNG**：`Pcg32`（整数），`Split(id)` 派生子流。
- **禁浮点**：`Jianghu.Cultivation` 全命名空间由 **`ILFloatScanner` 测试** IL 扫描守护（零 `float`/`double`/`System.Math`）。
- **禁挂钟/多线程/System.Random**：由 **BannedApiAnalyzers** 编译期强制（见 §6）。
- **收益**：战斗回放可验证、平衡标定可对照 diff、off 模式可逐字节回归。
- **代价**：不能用 `System.Math` 三角/指数（需要时整数查表/定点）；每个 PRNG 消费者必须走 `Split`。

### 5.2 off 逐字节一致（ADR-0003，红线 B.3）

**修炼系统走独立 PRNG 流，off 模式完全旁路修炼逻辑，输出逐字节一致。**

- **独立流**：`RngStreamIds.Cultivation = Split(5)`（Drama/Map/Faction 同理），off 时为 `null`、不消费。
- **off 分支**：`cultivation=false` → 角色 `Cultivation == null` → `SparAction`/`Lifecycle` 走 legacy 公式（战力 = Force×2 + Internal + Constitution）。
- **侧表纪律**：新态（`CultivationState` 等）挂 `Character` 侧表，**不污染 v1.0 core record**（`StatBlock`/`Persona`/`Relations` 字段顺序一字不改）。
- **机器守护**：`OffByteIdenticalTests` 验证 cultivation=false 时输出 SHA256 与 v1.0 基线一致；改任何 v1.0 共享文件（如 `SparAction.cs`）后 commit 前必跑（红线 B.3）。

### 5.3 模块工厂效果系统（ADR-0002，红线 B.9）

**所有战斗效果必须经 `Modules` 静态工厂构造；唯一档经 `SpecialModuleRegistry` 注册式插件。**

- **`Modules` 工厂**（`Cultivation/Modules.cs`）：单一构造入口，每类效果一个工厂方法（`FlatPen`/`FlatDR`/`PenFromResource`/`CounterMul`/`Dot`/`Drain`/`Control`/`Reflect`/`Evade`… ），封 `ratio`/`Kind`/`Amount2≥1`/`Trigger`/`Rarity` 等易漏参。
- **`SpecialModuleRegistry`**：8 个唯一档（断链/夺舍/夺心/爆阵/场地激活/金身极限/落宝/逆演栈）注册式插件，由 `ModuleResolver` 按签名匹配。
- **禁裸写 `new EffectOp(七参)`** 散造在 path 文件（靠 code review 守——`EffectOp` internal 构造，BannedApiAnalyzers 未覆盖）。
- **新算子** = 加 1 工厂方法 + `ModuleResolver` 1 分支，不改既有积木。
- **跨路平衡视图**：`BalanceMatrixDump` harness **派生**矩阵，不靠集中源码（承单一真相源纪律 A.2）。
- **例外**：4 参资源/标记操作（`AddResource`/`GrantPassive`/`SetFlag`/`Cost`/`AddFlatDR` 等）在 path 文件裸写合法——仅 7 参战斗效果构造受 B.9 约束。

### 5.4 道心解耦（红线 B.5，无独立 ADR，源码守）

**`daoHeart`/`innerDemon` 严禁进 `EffectivePower`——仅突破劫 `ResistTerms` 可用。**

- `PowerEngine.Resolve` 遇 `res:daoHeart`/`res:innerDemon` src **直接抛 `ArgumentException`**（在解引用前拒），保证道心/心魔不进 BaseSum/Modifier/战力。
- 允许出现的唯一位置：`TribulationResolver` 的 `TribulationDef.ResistTerms`（心劫抗性，`daoHeart`/`InnerDemon` 作为抗劫项 src）。
- 意图：道心是"叙事/突破"维度，不是"战斗强度"维度——解耦二者防止道心数值污染战力平衡。

---

## 6. 引擎兼容 — "零改写进 Godot" 前提

`Jianghu.Core` 保持 `netstandard2.1` + 无引擎依赖，是"后期零改写被 Godot 4.x .NET 引用"的硬前提。由 **`Microsoft.CodeAnalysis.BannedApiAnalyzers`**（`.editorconfig` `RS0030=error`）编译期强制，违者**编译错误**。`BannedSymbols.txt` 冻结禁用清单：

| 禁用 API | 原因 |
|---|---|
| `System.Random` | 跨运行时（Framework/.NET 6+/Mono/CoreCLR/AOT）同种子序列不同 → 改用注入的 `IRandom` |
| `System.Console` | Core 不做 IO（IO 属 Host 层） |
| `System.DateTime` / `.get_Now` | 挂钟时间不可复现 → 改用逻辑 `Tick` 时钟 |
| `System.Threading.Thread` | 多线程执行顺序不确定 → Core 单线程确定性推进 |
| `float`/`double`（仅 `Jianghu.Cultivation`） | IEEE754 跨后端舍入可能不同 → 全整数（`ILFloatScanner` 测试守，非 BannedApiAnalyzers） |
| `UnityEngine.*` / **`Godot.*`** | Core 是纯逻辑库；引擎 API 会破坏"零改写"承诺。当前无 Godot 引用，`Godot.*` 只属宿主程序集（路由纪律见 technical-preferences.md，边界见 §9 / ADR-0004） |

> 若未来真需非整数（如 Godot 即时反应窗口的正态判定），放在 **Godot 宿主层**，不进 Core（红线 B.2 只约束 `Jianghu.Cultivation`）。渲染帧时 `delta` / iso 屏幕坐标同理——只属 View，绝不进 Core（§9）。

---

## 7. ADR 索引（可溯）

| ADR | 标题 | Status | 治理不变量 | 红线 |
|---|---|---|---|---|
| [adr-0001](adr-0001-integer-determinism.md) | Integer Determinism | Accepted | §5.1 整数确定性 | B.2 |
| [adr-0002](adr-0002-module-factory-effect-system.md) | Module Factory Effect System | Accepted | §5.3 模块工厂 | B.9 |
| [adr-0003](adr-0003-cultivation-off-byte-identical.md) | Cultivation-off Byte-Identical | Accepted | §5.2 off 逐字节 | B.3 |
| [adr-0004](adr-0004-godot-view-host-boundary.md) | Godot View/Host 边界 | Accepted | §9 Godot 表现层边界 | A.10 |

> 道心解耦（§5.4，红线 B.5）当前无独立 ADR——由 `PowerEngine` 源码护栏 + code review 守。如需正式化，建议补 adr-0005（adr-0004 已用于 Godot 表现层边界）。

---

## 8. 参考

- 层规则手册（Required/Forbidden/Performance）：[control-manifest.md](control-manifest.md)
- **Godot 表现层前瞻规范（View/PCG/地图渲染真相源）**：[godot-architecture-manifest.md](godot-architecture-manifest.md)（宏微观双层世界 / 反应式回合战斗 / PCG / 韧性硬直 / LLM 叙事；§10 索引隔离关系）
- 需求溯源注册表（TR-ID）：[tr-registry.yaml](tr-registry.yaml)
- 系统全景：`design/gdd/systems-index.md`
- CLAUDE.md §F 架构概览 / §B 技术红线（源真相）
- 源码：`src/Jianghu.Core/Random/`（Pcg32/RngStreamIds）、`Sim/`（World/WorldFactory）、`Cultivation/`（PowerEngine/Modules/ModuleResolver）

---

## 9. Godot 表现层边界（Presentation / Host 层，ADR-0004）

> 2026-07-03 引擎目标由 Unity 切至 **Godot 4.x (.NET)**。本节是边界**摘要**，权威真相在 [adr-0004](adr-0004-godot-view-host-boundary.md)。**当前表现层未生成、2D 等距地图未开发**——本节含"架构预留"，不落地任何宿主/地图代码。
>
> **前瞻规范扩展**：本节（§9）定的是 Model⊥View **边界纪律**（单向流/固定时间步/iso 预留）；View 层的**完整形态目标**（宏微观双层世界、反应式 QTE/弹反战斗、PCG 管线、韧性硬直、LLM 叙事）见 [godot-architecture-manifest.md](godot-architecture-manifest.md)，其与内核红线的隔离关系与开放调和项见 **§10**。

**Core = Model（纯逻辑数据层），Godot = View（只读渲染 + 输入采集）。** 三条规矩：

### 9.1 Model → View 单向数据流 + Signal 订阅
- Godot 节点**只读** `World`/`StateSnapshot`/`Chronicle`；数据只沿 Model→View 单向流。
- 宿主侧 `WorldBridge : Node` 拉取 Core `DomainEvent`/快照增量 → 转 Godot C# `[Signal]` 向渲染节点广播。**Core 不感知 Godot**——只产出 `DomainEvent`，适配在宿主层。
- **玩家回写唯一合法通道 = 显式命令端口**：输入 → 宿主收为**整数意图** → 喂进下一确定性 `Tick`。绝不让 View 直改 Core 字段，绝不把浮点/帧时/坐标塞进 Core。
- **`Godot.*` 禁入 `Jianghu.Core`**（§6，对标原 `UnityEngine.*` 禁令）。

### 9.2 `_Process()` ↔ `Tick()` = 固定时间步累加器
- Godot `_Process(double delta)` **只渲染/插值**，`delta`（浮点帧时）**绝不进 Core**。
- 内核走**固定时间步累加器**：宿主累加真实时间，够一个 `SimStepSeconds` 就 `World.Advance` 一步（`while (acc >= step) { Advance(); acc -= step; }`），渲染按 `acc/step` 插值。
- `Advance` 由**逻辑步/玩家意图**驱动，非帧率驱动 → 掉帧只影响追帧次数，不改每步确定性轨迹（同种子→同轨迹，与帧率无关）。为何：`delta` 进 Core 会破 B.2 + 模拟速度随帧率漂移破可复现。

### 9.3 2D 等距（Isometric）TileMap 坐标系转换红线（**架构预留，0 代码**）
- **地图未设计，本轮绝不实现任何 iso/地图代码。** 仅登记未来红线：
- iso 投影 `screen=((gx−gy)·tileW/2,(gx+gy)·tileH/2)` 及其逆是**浮点/像素温床**，只属 Godot 宿主（`TileMap.MapToLocal`/`LocalToMap`）；**绝不进 Core**。
- Core 若需空间坐标，只持**整数逻辑格** `(int gx,int gy)`，engine-agnostic；屏幕像素/iso 菱形投影全在 View 换算。
- 现有 `Jianghu.Sim.WorldMap`（Kruskal MST **整数图拓扑**）与未来 iso 空间层**分离**——它是引擎无关的逻辑图，非空间/像素 iso，不在"地图渲染代码"范畴。
- 预留缝（未来 `IMapProjection`，宿主实现）**当前不创建**——预留 = 文档登记，代码 0 行。

---

## 10. View 层前瞻规范索引 + 开放调和项（Godot Architecture Manifest 对接）

> 2026-07-03 落盘用户交付的 [godot-architecture-manifest.md](godot-architecture-manifest.md)（"最终形态规范"，Alpha 阶段入口基线）。它定义 View 层的**完整形态目标**，比 §9（边界纪律）更进一步。本节做两件事：**① 概念索引**（新规范引入的架构概念 → 现有红线/ADR 的对应）；**② 开放调和项**（新规范触及 Core、需专门架构裁决的张力，登记为候选 ADR，**本次不裁决**——大方向决策交用户，红线 A.1）。
>
> **落盘边界（本次仅文档基线，红线 A.10 内核前置）**：manifest 是**前瞻设计源**，非落地清单。宏微观世界/反应式战斗/PCG 等**均未实现**，本节只做登记映射，不触任何 `.cs`，不为新概念作全 8 段 GDD（那属过早雕琢）。表现层接入闸口仍为「无头数据日志证明核心机制无死锁」（红线 A.10 / adr-0004）。

### 10.1 概念索引（manifest 概念 → 现有架构对应）

| manifest 概念 | 节 | 与现有红线/ADR 关系 | 现状 |
|---|---|---|---|
| **逻辑与表现绝对隔离**（Model/View/Controller） | §1.1 | **完全一致** adr-0004 §① Model→View 单向流 + `Godot.*` 禁入 Core（§6 / P-FORBIDDEN-1）。`_Process` 只转发、`Tick()` 玩家驱动 = §9.2 固定时间步 | 边界已立 |
| **确定性与回放安全**（纯整数 + Seed+离散指令序列录像） | §1.2 | **完全一致** §5.1 整数确定性（B.2）+ §5.2 off 逐字节（B.3）。"离散指令序列"= adr-0004 §① 整数意图命令端口 | 内核已达成 |
| **宏微观双层世界**（大世界同步回合 + 微观箱庭实时反应） | §2 | **新增 View 概念**。宏观同步回合与 adr-0004 §② 累加器"观察态自动追帧"有张力 → **开放调和项 R1** | 未设计 |
| **反应式回合战斗**（QTE→离散乘子回传内核 / 弹反抵消） | §3 | **一致**：`combat-system.md` 已定"双档保真度"（Core 整数 q_core / View 浮点窗口 q）；QTE 结果映射为**离散乘子**回传 = 浮点不进 Core（P-FORBIDDEN-2）。balance-007 弹反窗口阶梯 = CC 抗性递减（已 Approved story） | View 未建，Core 侧 q_core 设计在案 |
| **PCG 动态生成**（Voronoi/BSP + 柏林噪声 + 等距 TileMap） | §4 | 数据驱动=一致（配置外置，红线 A.10）；等距 TileMap=§9.3 iso 预留。**柏林噪声浮点**若入 Core 撞 B.2 → **开放调和项 R2** | 未设计 |
| **实体平权**（主角=具外部输入特权的标准实体） | 补§1.3 | **一致** adr-0004 §① 玩家介入=命令端口喂整数意图；主角无写死特殊机制=纯涌现 | 概念一致，未实现玩家实体 |
| **性能天花板 → ECS 倾向**（限制深层 OOP，向 ECS 靠拢） | 补§1.3 | 现 `Character` 是 OOP 聚合根（§3）。ECS 化 = Core 架构方向性变动 → **开放调和项 R3** | 未评估 |
| **韧性/硬直系统**（Poise/Stagger，体质→韧性条，削韧破霸体） | 补§3.2 | **新增战斗维度**。纯整数可表达（韧性条=int，削韧=int），与 B.2 无冲突；需接 `combat-system.md` 结算层 | 未设计 |
| **动态叙事/LLM 叙事**（记事本 UI + 生成器接口 + 数值变异限 PE 阈） | 补§4.3 | **一致** `IBrain` 端口（§3 Decide 层）；数值变异"限 PE 平价阈"= INV-CROSS C1（TR-BAL-001）。llm-brain epic 已登记（未设计，需先出 GDD） | 端口在，实现未建 |

### 10.2 开放调和项（候选 ADR，本次不裁决）

> 以下三项触及 **Core 逻辑/架构**，非文本同步可解——需专门架构立项裁决（红线 A.1 大方向交用户）。此处仅**登记张力 + 候选方向**，不下结论、不改任何 `.cs`。

- **R1 — 宏观同步回合 vs 累加器自动追帧**（候选 adr-0005）
  manifest §2.1「玩家不操作，世界绝对静止；决策一次 → 全局结算一次 `Tick()`」是**纯回合驱动**；adr-0004 §② 累加器含「观察态自动追帧」（真实时间累加自动 `Advance`）。二者可**分治**（宏观层玩家驱动步进 + 微观箱庭累加器插值）或**统一为纯玩家驱动**。裁决影响 §9.2。**未决**。

- **R2 — 柏林噪声浮点 vs B.2 整数确定性**（候选 adr-0006）
  manifest §4「微观柏林噪声填充地形」天然浮点。若地形生成结果**入 Core**（影响 A* 权重/属性乘区，manifest §2.1/§2.2），则撞 B.2（`Jianghu.Cultivation` 及全逻辑层禁浮点）。候选方向：① 整数/定点柏林（查表）；② 噪声**只在 View 生成**，仅整数化结果（如离散地形枚举 int）经命令端口入 Core。裁决影响 PCG 落地层归属。**未决**。

- **R3 — ECS 倾向 vs 现 OOP 聚合根**（候选 adr-0007）
  manifest 补§1.3「限制深层 OOP 继承，强制向 ECS 靠拢」。现 `Character`（§3 Model）是 OOP 聚合根，深拷贝语义（`World.Clone`）与确定性子流绑定。ECS 化是**大架构决定**（承 active.md retro action item「评估方差战斗模型需专门立项」同级）。候选方向：① 维持 OOP（继承已浅，性能未证瓶颈）；② 局部 data-oriented 重构热路径；③ 全 ECS。**需先立性能基准证明必要性，未决**。

> 三项均登记于 [tr-registry.yaml](tr-registry.yaml)（TR-VIEW-* proposed 条目），候选 ADR 号预留 0005/0006/0007（0005 此前建议留给道心解耦正式化，见 §7 脚注——实际立项时统一重编，不在此钉死）。
