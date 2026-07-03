# 架构总览 — 九野 · 江湖涌现模拟

> **Status**: Living（主架构文档）
> **Created**: 2026-07-03（综合 CLAUDE.md §F + ADR-0001/0002/0003 + technical-preferences.md）
> **Scope**: Foundation 层 + Core 层。Feature 层（drama/map/faction/llm-brain）与 Presentation 层（可视化/Unity 宿主）仅点到接口边界，详见各自 GDD。
> **Traceability**: 每条关键决策回链其 ADR（见 §7 ADR 索引）。本文档 = 派生综合，源真相在 ADR / 红线 / 源码。

---

## 1. 概览

九野是一个**确定性、事件驱动的江湖涌现模拟内核**。核心设计意图是**分层宿主**：

- **`Jianghu.Core`（纯逻辑库，netstandard2.1）**：全部模拟机制。零引擎依赖——不引用 `UnityEngine.*`，不做 IO。这是"Model"。
- **`Jianghu.Cli`（薄壳驱动，net8.0）**：当前阶段的"View/Host"——解析 CLI 参数 → 构造 World → 推进 → 打印文本快照。
- **后期 Unity 宿主**：`Jianghu.Core` 设计为 `netstandard2.1`（Unity 默认 API 兼容级），意图**零改写被 Unity 直接引用**渲染 + 承接玩家介入。Core 保持无引擎 API 是这一"零改写"承诺的前提（见 §6）。

**Model / View 分离**是贯穿全局的第一原则：模拟状态（World）与其呈现（CLI 文本 / 后期 Unity 渲染）严格解耦。View 只读 World 状态，不反向驱动确定性内核。这使得同一 Core 可在 CLI（.NET 8 JIT）与 Unity（IL2CPP）下产出**逐字节一致**的模拟轨迹（见 §5.1，ADR-0001）。

模拟有两个模式，由构造参数 `cultivation` 切换：
- **off 模式**（默认）：纯 v1.0 规则，无修炼。输出必须与最初 v1.0 **逐字节一致**（ADR-0003，红线 B.3）。
- **on 模式**：21 路修炼系统叠加在 v1.0 之上，走独立 PRNG 流，不扰动 off 路径。

---

## 2. 程序集职责

| 程序集 | TFM | 职责 | 关键约束 |
|---|---|---|---|
| `Jianghu.Core` | `netstandard2.1` | **纯逻辑库**——全部模拟机制（模型/PRNG/调度/动作/修炼/战斗/戏剧/事件）。 | 零引擎依赖；禁 `System.Random`/`Console`/`DateTime`/`Thread`（BannedApiAnalyzers）；`Jianghu.Cultivation` 禁浮点（IL 扫描）。后期直接被 Unity 引用。 |
| `Jianghu.Cli` | `net8.0` | CLI 控制台驱动——薄壳，解析参数 → `WorldFactory.CreateInitial` → `World.Advance` → 打印快照。 | 当前唯一 Host；后期由 Unity 宿主并列/取代。可用 `System.Console`（非 Core）。 |
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

**`Jianghu.Cultivation` 及所有逻辑层全程禁浮点，全部整数确定性。** 同种子同输入 → 任何 .NET 运行时（CLI JIT / Unity IL2CPP / Mono）逐字节一致。

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

## 6. 引擎兼容 — "零改写进 Unity" 前提

`Jianghu.Core` 保持 `netstandard2.1` + 无引擎依赖，是"后期零改写被 Unity 引用"的硬前提。由 **`Microsoft.CodeAnalysis.BannedApiAnalyzers`**（`.editorconfig` `RS0030=error`）编译期强制，违者**编译错误**。`BannedSymbols.txt` 冻结禁用清单：

| 禁用 API | 原因 |
|---|---|
| `System.Random` | 跨运行时（Framework/.NET 6+/Mono/IL2CPP）同种子序列不同 → 改用注入的 `IRandom` |
| `System.Console` | Core 不做 IO（IO 属 Host 层） |
| `System.DateTime` / `.get_Now` | 挂钟时间不可复现 → 改用逻辑 `Tick` 时钟 |
| `System.Threading.Thread` | 多线程执行顺序不确定 → Core 单线程确定性推进 |
| `float`/`double`（仅 `Jianghu.Cultivation`） | IEEE754 跨后端舍入可能不同 → 全整数（`ILFloatScanner` 测试守，非 BannedApiAnalyzers） |
| `UnityEngine.*` | Core 是纯逻辑库；引擎 API 会破坏"零改写"承诺（当前无 Unity 引用，路由纪律见 technical-preferences.md） |

> 若未来真需非整数（如 Unity 即时反应窗口的正态判定），放在 **Unity 宿主层**，不进 Core（红线 B.2 只约束 `Jianghu.Cultivation`）。

---

## 7. ADR 索引（可溯）

| ADR | 标题 | Status | 治理不变量 | 红线 |
|---|---|---|---|---|
| [adr-0001](adr-0001-integer-determinism.md) | Integer Determinism | Accepted | §5.1 整数确定性 | B.2 |
| [adr-0002](adr-0002-module-factory-effect-system.md) | Module Factory Effect System | Accepted | §5.3 模块工厂 | B.9 |
| [adr-0003](adr-0003-cultivation-off-byte-identical.md) | Cultivation-off Byte-Identical | Accepted | §5.2 off 逐字节 | B.3 |

> 道心解耦（§5.4，红线 B.5）当前无独立 ADR——由 `PowerEngine` 源码护栏 + code review 守。如需正式化，建议补 adr-0004。

---

## 8. 参考

- 层规则手册（Required/Forbidden/Performance）：[control-manifest.md](control-manifest.md)
- 需求溯源注册表（TR-ID）：[tr-registry.yaml](tr-registry.yaml)
- 系统全景：`design/gdd/systems-index.md`
- CLAUDE.md §F 架构概览 / §B 技术红线（源真相）
- 源码：`src/Jianghu.Core/Random/`（Pcg32/RngStreamIds）、`Sim/`（World/WorldFactory）、`Cultivation/`（PowerEngine/Modules/ModuleResolver）
