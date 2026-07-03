# 控制清单 — 层规则手册（Control Manifest）

> **Manifest Version: 2026-07-03**
> **Status**: Active
> **Purpose**: 按层（Foundation / Core）声明 Required / Forbidden / Performance 规则。story 的 layer 约束、`/dev-story` 实现纪律、`/code-review` 判据均以本清单为准。
> **Source of truth**: 红线（CLAUDE.md §A/§B）+ ADR-0001/0002/0003。本清单是红线的**分层编排**，红线冲突时以 CLAUDE.md 为准。
> **Traceability**: Forbidden 每条注原因 + 溯源红线/ADR + 守护机制（编译器 / IL 扫描 / 测试 / code review）。

---

## 版本历史

| Version | 变更 |
|---|---|
| 2026-07-03 | 初始 bootstrap。编入红线 B.2/B.3/B.5/B.9 + BannedApiAnalyzers 禁用清单 + RngStreamIds append-only 与 Clone 要求。 |

---

## 层模型

| Layer | 含义 | 程序集/命名空间 |
|---|---|---|
| **Foundation** | 地基——无此模拟不成立 | `Jianghu.Model` / `Random` / `Stats` / `Config` / `Sim`(核心主循环) / `Actions` / `Events` |
| **Core** | 核心玩法——修炼与战斗 | `Jianghu.Cultivation`(+`.paths`/`.special`/`.Artifacts`) |
| Feature | 上层特征（本清单不详列，规则同 Foundation 且 off 不激活） | `Jianghu.Drama` / `Decide` / `Sim`(map/faction) |

> Feature 层新增系统：**必须**走独立 PRNG 流（append-only）、off 不激活、侧表不污染 v1.0 record——即遵守 Foundation 的 F-FORBIDDEN-4 与 F-REQUIRED-1/2。

---

## Foundation 层

### Required（F-REQUIRED）

| ID | 规则 | 溯源 | 守护 |
|---|---|---|---|
| F-REQUIRED-1 | **新 PRNG 消费者必须追加 `RngStreamIds` 新 id**（append-only，绝不复用既有 1..8）。 | ADR-0001 / ADR-0003 / CLAUDE.md §F | code review + off 逐字节测试 |
| F-REQUIRED-2 | **新 PRNG 子流必须进 `World.Clone`**（深拷续跑，`CloneRng`/`CloneRngOrNull` null 安全），保克隆后续跑不发散。 | ADR-0001 / `World.cs` R1 注释 | code review + 确定性测试 |
| F-REQUIRED-3 | **改任何 v1.0 共享文件后必验 off 逐字节**（跑 `OffByteIdenticalTests`，SHA256 与 v1.0 基线一致）。commit 前必跑。 | ADR-0003 / 红线 B.3 | `OffByteIdenticalTests`（BLOCKING） |
| F-REQUIRED-4 | **可选子系统（cultivation/drama/map/faction）off 时子流为 `null`、绝不构造、绝不消费** `Split(5..8)`——保 `Split(1..4)` 编号与序列在 off 下不变。 | ADR-0003 §1-2 | off 逐字节测试 |
| F-REQUIRED-5 | 领域模型新态挂 `Character` **侧表**，不改 v1.0 core record（`StatBlock`/`Persona`/`Relations`）字段顺序。 | ADR-0003 §4（侧表纪律，红线 B.6） | off 逐字节测试 + code review |
| F-REQUIRED-6 | 时间以逻辑 `Tick`（clock 计数）为准，非挂钟。 | ADR-0001 §3 | BannedApiAnalyzers（禁 DateTime） |

### Forbidden（F-FORBIDDEN）

| ID | 禁止 | 原因 | 溯源 | 守护 |
|---|---|---|---|---|
| F-FORBIDDEN-1 | **`System.Random`** in Core | 跨运行时（Framework / .NET 6+ / Mono / IL2CPP）同种子序列不同 → 破确定性。改用注入的 `IRandom`。 | 红线 B.2 / ADR-0001 | **BannedApiAnalyzers**（`RS0030=error`，编译失败） |
| F-FORBIDDEN-2 | **`System.Console`** in Core | Core 是纯逻辑库不做 IO；IO 属 Host（CLI/Unity）层。 | ADR-0001 | **BannedApiAnalyzers** |
| F-FORBIDDEN-3 | **`System.DateTime` / `DateTime.Now`** in Core | 挂钟时间不可复现 → 破逐字节复现。改逻辑 `Tick`。 | 红线 B.2 / ADR-0001 §3 | **BannedApiAnalyzers** |
| F-FORBIDDEN-4 | **`System.Threading.Thread` / `Task.Run`** in Core | 多线程执行顺序不确定 → 破确定性。Core 单线程确定性推进。 | ADR-0001 §4 | **BannedApiAnalyzers** |
| F-FORBIDDEN-5 | **复用/重编号既有 `RngStreamIds`**（1..8 冻结） | 改动既有子流编号会改变 off 消费序列 → 破 off 逐字节。只能 append。 | ADR-0003 / CLAUDE.md §F | off 逐字节测试 + code review |
| F-FORBIDDEN-6 | **在 off 路径消费可选子流**（cultivation/drama/map/faction） | off 消费 `Split(5..8)` 会改变 `Split(1..4)` 编号 → 破 v1.0 逐字节。 | ADR-0003 §1 | `OffByteIdenticalTests` |

### Performance（F-PERF）

| ID | 规则 | 说明 |
|---|---|---|
| F-PERF-1 | 无实时帧预算（非实时模拟，见 technical-preferences.md）。 | 模拟以种子驱动逐字节可复现，非帧循环。 |
| F-PERF-2 | 模拟规模由 `LimitsConfig` 约束（角色数/节点数上限），非内存天花板硬指标。 | `limits.Validate()` 在 `CreateInitial` 强制。 |
| F-PERF-3 | 单线程确定性推进——**不得**为性能引入并行（破 F-FORBIDDEN-4）。性能优化限于算法/数据结构层。 | 确定性 > 吞吐。 |

---

## Core 层（`Jianghu.Cultivation`）

> 继承 Foundation 全部 Required/Forbidden，另加以下 Core 专属规则。

### Required（C-REQUIRED）

| ID | 规则 | 溯源 | 守护 |
|---|---|---|---|
| C-REQUIRED-1 | **新战斗算子 = 加 1 `Modules` 工厂方法 + `ModuleResolver` 1 分支**，不改既有积木。 | ADR-0002 / 红线 B.9 | code review + `ModuleResolverTests` |
| C-REQUIRED-2 | **唯一档效果经 `SpecialModuleRegistry` 注册式插件**（不散写在 path 文件）。 | ADR-0002 §2 | code review |
| C-REQUIRED-3 | **跨路平衡视图靠 `BalanceMatrixDump` harness 派生**，不靠集中源码常量表（承单一真相源 A.2）。 | ADR-0002 §5 / 红线 A.2 | code review |
| C-REQUIRED-4 | 修炼系统随机消费走 `cultRng`（`Split(5)`），不碰 `domainRng`/`spawnRng`。 | ADR-0003 §1 | off 逐字节测试 |
| C-REQUIRED-5 | 21 路全入册（加路 = 加数据行 + `CodePathSource` 注册），**none dropped**。 | 红线 B.4 | 21 路独立测试 |
| C-REQUIRED-6 | 新增劫型 = 加一条 `TribulationDef`（`ResistTerms` 数据），零改 `TribulationResolver`。 | 数据驱动（`TribulationResolver` 设计） | code review |

### Forbidden（C-FORBIDDEN）

| ID | 禁止 | 原因 | 溯源 | 守护 |
|---|---|---|---|---|
| C-FORBIDDEN-1 | **浮点（`float`/`double`/`System.Math`）in `Jianghu.Cultivation`** | IEEE754 在不同后端/优化等级（JIT vs AOT vs IL2CPP）舍入行为可能不同 → 破跨运行时逐字节一致。需要非整数运算改整数查表/定点。 | 红线 B.2 / ADR-0001 §2 | **`ILFloatScanner` 测试**（IL 扫描，BLOCKING） |
| C-FORBIDDEN-2 | **`daoHeart`/`innerDemon` 进 `EffectivePower`**（BaseSum/Modifier/战力） | 道心/心魔是叙事+突破维度，非战斗强度维度。混入战力会用道心数值污染跨路战力平衡。**唯一允许位置** = 突破劫 `TribulationDef.ResistTerms`。 | 红线 B.5 | `PowerEngine.Resolve` 抛 `ArgumentException`（`res:daoHeart`/`res:innerDemon`）+ 专项测试 |
| C-FORBIDDEN-3 | **裸写 `new EffectOp(七参)` 战斗构造在 path 文件** | 7 参极易漏（ratio/Amount2≥1/Trigger/Rarity）；散在 21 文件难查错；绕过平衡体系；重复造轮子。必经 `Modules` 工厂单点。 | 红线 B.9 / ADR-0002 | **code review**（`EffectOp` internal，BannedApiAnalyzers 未覆盖） |
| C-FORBIDDEN-4 | **改 `RealmMultipliers`/`UnifiedTierOf` 等 on 数据后不验 off 逐字节** | 虽然这些仅经 cultivation-on 路径（`PowerEngine.Evaluate`），改后仍须证 off 不受扰（`OffByteIdenticalTests`/`OffRegressionWith21PathsTests`）。 | 红线 B.3 / balance-cross story-003 §B.3 分析 | off 逐字节测试 |
| C-FORBIDDEN-5 | **丢弃任何修炼路径**（21 路必须全入册） | 加路是数据行，不是删减——设计要求 21 路完整覆盖。 | 红线 B.4 | 21 路独立测试 |

> **B.9 例外重申**：4 参资源/标记操作（`AddResource`/`AddResourceCap`/`GrantPassive`/`SetFlag`/`Cost`/`AddTermWeightStep`/`AddFlatDR`）在 path 文件裸写**合法**——仅 7 参战斗效果构造受 C-FORBIDDEN-3 约束。

### Performance（C-PERF）

| ID | 规则 | 说明 |
|---|---|---|
| C-PERF-1 | `PowerEngine` 全整数、`long` 防溢、每步 `clamp`。 | 整数运算天然快且确定；`long` 中间量防溢出。 |
| C-PERF-2 | 战斗对拍（`BalanceMatrixDump`/duel sim）是 harness/测试期开销，非运行期热路径。 | 平衡标定不进 `Advance` 主循环。 |
| C-PERF-3 | 详细平衡数值调优（`RealmMultipliers` 校准等）**推迟到 Alpha 阶段**（功能完备后）。 | 流水线纪律（balance-cross story-003 defer 复核，2026-07-02）。 |

---

## 守护机制速查

| 机制 | 类型 | 覆盖 |
|---|---|---|
| **BannedApiAnalyzers**（`RS0030=error`） | 编译期，硬失败 | F-FORBIDDEN-1/2/3/4（`BannedSymbols.txt`） |
| **`ILFloatScanner` 测试** | 测试期，BLOCKING | C-FORBIDDEN-1（`Jianghu.Cultivation` 浮点） |
| **`OffByteIdenticalTests`** | 测试期，BLOCKING | F-REQUIRED-3/4、F-FORBIDDEN-5/6、C-FORBIDDEN-4（off 逐字节 SHA256） |
| **`PowerEngine.Resolve` 护栏** | 运行期抛异常 + 测试 | C-FORBIDDEN-2（道心解耦） |
| **code review**（Opus 4.8） | 人工/agent | C-FORBIDDEN-3（裸 `EffectOp`）、C-REQUIRED-1/2/3、F-REQUIRED-1/2 |

---

## 参考

- 主架构：[architecture.md](architecture.md)
- 需求溯源：[tr-registry.yaml](tr-registry.yaml)
- ADR：[adr-0001](adr-0001-integer-determinism.md) / [adr-0002](adr-0002-module-factory-effect-system.md) / [adr-0003](adr-0003-cultivation-off-byte-identical.md)
- 红线：CLAUDE.md §A（流程）/ §B（技术）
- 禁用清单源：`src/Jianghu.Core/BannedSymbols.txt`
