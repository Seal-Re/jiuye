# Story 002: WorldBridge + Signal 接线 — Core→Godot 最小渲染回路

> **Epic**: godot-host
> **Status**: Not Started
> **Layer**: Presentation
> **Type**: Integration
> **Estimate**: 中 (1.5d)
> **Depends**: gh-001（闸口 PASS）
> **ADR**: adr-0004 §①（Model→View 单向流 + Signal 订阅）

## Context

adr-0004 §① 定：Godot 节点只读 `World`/`StateSnapshot`/`Chronicle`；宿主侧 `WorldBridge : Node` 拉取 Core `DomainEvent`/快照增量 → 转 Godot C# `[Signal]` 向渲染节点广播。**Core 不感知 Godot**——只产出 `DomainEvent`，适配在宿主层。

本 story = 第一条 Godot 代码：新建独立 Godot 项目/程序集，引用 `Jianghu.Core.dll`，建 `WorldBridge` 最小回路——打印事件到 Godot 控制台即算最小渲染回路成立。

## Acceptance Criteria

- [ ] **2.1 Godot 项目骨架**：Godot 4.x .NET 项目可编译，引用 `Jianghu.Core`（netstandard2.1）成功
- [ ] **2.2 WorldBridge 节点**：`WorldBridge : Node` 挂场景，订阅 Core 事件（至少 `World.Advance` 后的 snapshot）
- [ ] **2.3 Signal 广播**：Core `DomainEvent` → `WorldBridge` → Godot `[Signal]` → 控制台 `GD.Print`
- [ ] **2.4 最小闭环**：`dotnet run` CLI 同 seed 跑 N 步 → Godot 宿主同样 seed 跑 N 步 → 控制台输出相同事件序列
- [ ] **2.5 B.2/B.3 守**：Core 不改一行；`Godot.*` 仅出现在 Godot 宿主程序集

## Implementation Notes

- 新建 Godot 4.x .NET 项目（`godot-host/` 或独立 repo；待定）
- `WorldBridge` 的 `_Ready()` 中构造 `WorldFactory.CreateInitial` → 启动定时器/累加器（gh-003）→ 每步 `Advance`
- 初始阶段 CLI 仍为主驱动；Godot 宿主为"验证回路"
- `Jianghu.Core` 可以 DLL 引用或项目引用（同 solution 内项目引用优先）

## QA Test Cases

- AC-1（2.1）：Godot 项目 `dotnet build` 成功
- AC-2（2.2/2.3）：Godot 运行后可看到 Core 事件打印
- AC-3（2.4）：Godot vs CLI 同 seed 输出序列一致（文本 diff）
- AC-4（2.5）：`grep -r "Godot\." src/Jianghu.Core/` 零命中

## Test Evidence

**Story Type**: Integration
**Required evidence**: Godot 运行截图/日志 + CLI vs Godot 文本 diff 一致 + `Godot.*` 禁入 Core grep 零命中
**Status**: [ ] 待实现
