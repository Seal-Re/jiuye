# Technical Preferences

<!-- jiuye 采用 CCGS 时手工填（非 /setup-engine，因 jiuye 非引擎项目）。2026-06-15。 -->
<!-- All agents reference this file for project-specific standards and conventions. -->

## Engine & Language

- **Engine**: **Godot 4.x (.NET / C#)（目标平台，后期）** — 2026-07-03 由 Unity 切换（ADR-0004）。当前阶段 Core 为纯 C# 逻辑库经 CLI 驱动；`Jianghu.Core` 为 `netstandard2.1`，Godot 4.x .NET（CoreCLR）**可直接引用**渲染/玩家介入（「Core 纯逻辑零改写进引擎」）。**非"无引擎"——是分阶段：v1 Core+CLI，后期 Godot 宿主。** TFM 保留 `netstandard2.1`（Godot .NET 直接引用，无需升级）。
- **Language**: C# (Jianghu.Core: netstandard2.1 库 → Godot 引用; Jianghu.Cli: net8.0 当前驱动)
- **Rendering**: 后期 Godot 渲染（只读 World 状态，`WorldBridge`→`[Signal]`，见 ADR-0004 §9.1）；当前 CLI 文本快照。原始基础件/UI 见红线 B.8 分轨。
- **Physics**: N/A — Core 确定性整数模拟（红线 B.2 禁浮点；同种子逐字节复现，跨 CLI/Godot CoreCLR/AOT 一致）。渲染插值/帧时属 Godot 宿主层（`delta` 不进 Core，ADR-0004 §9.2）。

## Input & Platform

- **Target Platforms**: **Godot 4.x（桌面，后期玩家介入）**；当前 CLI 驱动 Core 模拟
- **Input Methods**: 当前 CLI；**后期 Godot 玩家输入**（采集为整数意图 → 经命令端口进确定性 Tick，ADR-0004 §9.1）
- **Primary Input**: 当前 CLI
- **Gamepad Support**: None（后期 Godot 宿主可评估）
- **Touch Support**: None
- **Platform Notes**: 无实时渲染/输入；模拟以种子驱动逐字节可复现（B.2）。Godot `_Process` 走固定时间步累加器（ADR-0004 §9.2）

## Naming Conventions

- **Classes**: PascalCase
- **Variables**: camelCase（局部）；私有字段 `_camelCase`
- **Signals/Events**: PascalCase 事件记录（event-driven 内核）
- **Files**: 文件名匹配主类型名（PascalCase.cs）
- **Scenes/Prefabs**: 后期 Godot `.tscn`/`PackedScene`（宿主层）；当前 N/A
- **Constants**: PascalCase（冻结数字常量见战斗硬化）

## Performance Budgets

- **Target Framerate**: N/A（非实时）
- **Frame Budget**: N/A
- **Draw Calls**: N/A
- **Memory Ceiling**: N/A（模拟规模由 LimitsConfig 约束）

## Testing

- **Framework**: xUnit
- **Minimum Coverage**: 回归基线 = 全量绿（采用时 282 绿，不退）
- **Required Tests**: 确定性（IL 浮点扫描 / 逐字节复现）、off 逐字节、平衡 gate、战斗模块差分

## Forbidden Patterns

<!-- 红线落实，治理 code-review / story 验收 -->
- 浮点 in `Jianghu.Cultivation`（红线 B.2，IL 扫描守）
- 裸 `new EffectOp(七参)` 散写在 path 文件（红线 B.9 — 必经 `Modules` 工厂）
- `daoHeart`/`innerDemon` 进 EffectivePower（红线 B.5 — 仅突破劫 ResistTerms）
- 前台浏览器窗口（红线 B.1 — 联网只 headless）
- 改 v1.0 文件后不验 off 逐字节（红线 B.3）
- `Godot.*`（及 `UnityEngine.*`）进 `Jianghu.Core`（ADR-0004 — 零改写前提，桥接只属宿主层）
- 渲染帧时 `delta`（浮点）/ iso 屏幕坐标进 Core（ADR-0004 §9.2/9.3 — 只属 View；`Advance` 走固定时间步）

## Allowed Libraries / Addons

- 标准库 .NET 8 / netstandard2.1；xUnit（测试）；Microsoft.CodeAnalysis.BannedApiAnalyzers（红线守，见 BannedSymbols.txt）
- 可视化轨（独立）：Pillow（Python 像素）；SVG/HTML-CSS（古风 UI）

## Architecture Decisions Log

<!-- 链接 docs/architecture/ 全 ADR（P8 增量补） -->
- adr-0001-integer-determinism（B.2，P8 补）
- adr-0002-module-factory-effect-system（B.9，P8 补，治理 combat-r2）
- adr-0003-cultivation-off-byte-identical（B.3，P8 补）
- adr-0004-godot-view-host-boundary（A.10，2026-07-03，Unity→Godot 引擎切换 + Model/View 边界）

## Engine Specialists

<!-- 分阶段:当前 Core=纯逻辑库(无引擎API,故无引擎专家);后期 Godot 宿主层=Godot-C# 专家适用。引擎 2026-07-03 由 Unity 切至 Godot 4.x .NET(ADR-0004)。 -->

- **Primary**: 当前阶段通用 C# code review（Core 是纯 netstandard2.1 逻辑库，**禁** `Godot.*`/`UnityEngine.*` 等引擎 API，BannedApiAnalyzers 守 + P-FORBIDDEN-1）
- **Language/Code Specialist**: C# 通用（Core 层）；**后期 Godot 宿主层 → `godot-csharp-specialist`**（`.claude/agents/` 已具 godot-* 专家集）
- **Shader Specialist**: 后期 Godot 阶段 `godot-shader-specialist`；当前 N/A
- **UI Specialist**: 后期 Godot UI（Control 节点 / Theme）；古风 UI 资产轨独立（红线 B.8）
- **Additional Specialists**: 后期 Godot（`godot-specialist` 渲染/输入；`godot-gdextension-specialist` 原生集成如需）
- **Routing Notes**: 当前 `.cs`（Core）路由通用 C# 审查，**Core 必须无引擎 API**（零改写进 Godot 的前提）；Godot 宿主代码（后期）路由 godot-* 专家。

### File Extension Routing

| File Extension / Type | Specialist to Spawn |
|-----------------------|---------------------|
| Core 逻辑 (.cs, netstandard2.1) | 通用 C# 审查（必须无 `Godot.*`/`UnityEngine.*` — 零改写前提） |
| Godot 宿主代码 (.cs, 后期) | `godot-csharp-specialist`（后期阶段） |
| Shader files (.gdshader / Godot 可视化 shader) | `godot-shader-specialist`（Godot 阶段） |
| UI / Control 节点 / Theme files | `godot-specialist` / UI（Godot 阶段）+ 古风 UI 资产轨(B.8) |
| Scene / resource files (.tscn / .tres) | Godot 阶段（`godot-specialist`） |
| General architecture review | Primary |
