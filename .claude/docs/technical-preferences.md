# Technical Preferences

<!-- jiuye 采用 CCGS 时手工填（非 /setup-engine，因 jiuye 非引擎项目）。2026-06-15。 -->
<!-- All agents reference this file for project-specific standards and conventions. -->

## Engine & Language

- **Engine**: .NET 8 (custom headless turn-based simulation — 非实时游戏引擎，无 Godot/Unity/Unreal)
- **Language**: C# (Jianghu.Core: netstandard2.1 库; Jianghu.Cli: net8.0 可执行)
- **Rendering**: N/A — headless 控制台；可视化为独立离线像素/SVG 轨（红线 B.8）
- **Physics**: N/A — 确定性整数回合制模拟（红线 B.2，禁浮点）

## Input & Platform

- **Target Platforms**: Desktop CLI (.NET, 跨平台)
- **Input Methods**: CLI args / stdin
- **Primary Input**: CLI
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

<!-- jiuye 无游戏引擎，无引擎专家适用。 -->

- **Primary**: 通用 code review（lead-programmer / gameplay-programmer 作协作者）
- **Language/Code Specialist**: C# 通用（**非** godot-csharp-specialist — 其假设 Godot API）
- **Shader Specialist**: N/A
- **UI Specialist**: N/A（古风 UI 轨独立，非引擎 UI）
- **Additional Specialists**: N/A
- **Routing Notes**: `.cs` 路由到通用 C# 审查；不调任何引擎专家（无 Godot/Unity/Unreal）

### File Extension Routing

| File Extension / Type | Specialist to Spawn |
|-----------------------|---------------------|
| Game code (primary language) | 通用 C# 审查（非 godot-csharp-specialist） |
| Shader / material files | N/A |
| UI / screen files | N/A（古风 UI 独立轨） |
| Scene / prefab / level files | N/A |
| Native extension / plugin files | N/A |
| General architecture review | Primary |
