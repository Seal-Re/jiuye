# Technical Preferences

<!-- jiuye 采用 CCGS 时手工填（非 /setup-engine，因 jiuye 非引擎项目）。2026-06-15。 -->
<!-- All agents reference this file for project-specific standards and conventions. -->

## Engine & Language

- **Engine**: **Unity（目标平台，后期）** — 当前阶段 Core 为纯 C# 逻辑库经 CLI 驱动；`Jianghu.Core` 设计为 `netstandard2.1`（Unity 默认 API 兼容级）**后期直接被 Unity 引用渲染/玩家介入**（原始设计 §架构：「Core 纯逻辑零改写进 Unity」）。**非"无引擎"——是分阶段：v1 Core+CLI，后期 Unity 宿主。**
- **Language**: C# (Jianghu.Core: netstandard2.1 库 → Unity 引用; Jianghu.Cli: net8.0 当前驱动)
- **Rendering**: 后期 Unity 渲染（读 World 状态）；当前 CLI 文本快照。原始基础件/UI 见红线 B.8 分轨。
- **Physics**: N/A — Core 确定性整数模拟（红线 B.2 禁浮点；同种子逐字节复现，跨 CLI/Unity IL2CPP 一致）

## Input & Platform

- **Target Platforms**: **Unity（桌面，后期玩家介入）**；当前 CLI 驱动 Core 模拟
- **Input Methods**: 当前 CLI；**后期 Unity 玩家输入（含即时交互层可行）**
- **Primary Input**: 当前 CLI
- **Gamepad Support**: None
- **Touch Support**: None
- **Platform Notes**: 无实时渲染/输入；模拟以种子驱动逐字节可复现（B.2）

## Naming Conventions

- **Classes**: PascalCase
- **Variables**: camelCase（局部）；私有字段 `_camelCase`
- **Signals/Events**: PascalCase 事件记录（event-driven 内核）
- **Files**: 文件名匹配主类型名（PascalCase.cs）
- **Scenes/Prefabs**: N/A
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

## Allowed Libraries / Addons

- 标准库 .NET 8 / netstandard2.1；xUnit（测试）；Microsoft.CodeAnalysis.BannedApiAnalyzers（红线守，见 BannedSymbols.txt）
- 可视化轨（独立）：Pillow（Python 像素）；SVG/HTML-CSS（古风 UI）

## Architecture Decisions Log

<!-- 链接 docs/architecture/ 全 ADR（P8 增量补） -->
- adr-0001-integer-determinism（B.2，P8 补）
- adr-0002-module-factory-effect-system（B.9，P8 补，治理 combat-r2）
- adr-0003-cultivation-off-byte-identical（B.3，P8 补）

## Engine Specialists

<!-- 分阶段:当前 Core=纯逻辑库(无引擎API,故无引擎专家);后期 Unity 宿主层=Unity-C# 专家适用。 -->

- **Primary**: 当前阶段通用 C# code review（Core 是纯 netstandard2.1 逻辑库，**禁** UnityEngine.* 等引擎 API，BannedApiAnalyzers 守）
- **Language/Code Specialist**: C# 通用（Core 层）；**后期 Unity 宿主层 → Unity-C# 专家**
- **Shader Specialist**: 后期 Unity 阶段适用；当前 N/A
- **UI Specialist**: 后期 Unity UI；古风 UI 资产轨独立（红线 B.8）
- **Additional Specialists**: 后期 Unity（渲染/输入/即时交互层）
- **Routing Notes**: 当前 `.cs`（Core）路由通用 C# 审查，**Core 必须无引擎 API**（零改写进 Unity 的前提）；Unity 宿主代码（后期）路由 Unity 专家。

### File Extension Routing

| File Extension / Type | Specialist to Spawn |
|-----------------------|---------------------|
| Core 逻辑 (.cs, netstandard2.1) | 通用 C# 审查（必须无 UnityEngine.* — 零改写前提） |
| Unity 宿主代码 (.cs, 后期) | Unity-C# 专家（后期阶段） |
| Shader / material files | Unity 阶段 |
| UI / screen files | Unity 阶段 + 古风 UI 资产轨(B.8) |
| Scene / prefab files | Unity 阶段 |
| General architecture review | Primary |
