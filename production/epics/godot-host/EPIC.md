# Epic: Godot 宿主层（View/Host）— Phase 2 接入

**Layer**: Presentation
**Status**: Complete（2026-07-21 — gh-001~004 全 Complete + WorldView 最小闭环；1281绿）
**GDD**: —（表现层，设计权威在 adr-0004 + godot-architecture-manifest.md）
**Governing ADRs**: **adr-0004**（Model/View 边界·单向流·固定时间步·iso 预留）· adr-0001（B.2 整数确定性）· adr-0003（B.3 off 逐字节）
**Engine Risk**: **MEDIUM**（Godot 4.x .NET 首次接入；WorldBridge/Signal/累加器模式在训练数据内，需目标版本复核）
**Created**: 2026-07-17

## Summary

将 `Jianghu.Core`（纯逻辑库，netstandard2.1）接入 Godot 4.x .NET 宿主——只读渲染 + 输入采集。Core 保持零引擎依赖（不改一行），Godot 宿主层新建独立程序集（引用 Core + Godot .NET API）。**闸口前置条件（红线 A.10）**：无头数据日志证明核心机制无死锁（1271 绿 + Viability 破境 UT0→8 + 19 恩怨链已证；待形式化闸口文档）。

## Scope（Phase-2 接入，4 story）

- **gh-001** 闸口验收：Core 侧无死锁证明（headless smoke 全 UT 破境 + 恩怨链 + 战斗 1000 场 seed-sweep 零 crash）→ 形式化闸口文档
- **gh-002** WorldBridge + Signal 接线：Core `DomainEvent`/`StateSnapshot` → Godot `[Signal]` 广播 → 最小渲染回路（控制台文本/日志）
- **gh-003** 固定时间步累加器：`_Process(delta)` → 累加器 → `World.Advance()` → 渲染插值。delta 不进 Core（守 B.2）
- **gh-004** 命令端口 + CLI/Godot 双宿主确定性回归：玩家整数意图 → 下一 Tick → 同 seed 同轨迹（CLI vs Godot CoreCLR/AOT 逐字节一致）

## Out of Scope（本 epic 不含）

- 2D 等距 TileMap 渲染（adr-0004 §9.3 预留，iso 投影属后续 gh-01x）
- PCG 地形生成（godot-architecture-manifest §4，属后续 epic）
- 反应式 QTE 战斗 View（manifest §3，属后续 epic，依赖 gh-004 命令端口）
- 宏微观双层世界切换（manifest §2，属后续 epic）
- 美术资产/UI 古风界面（红线 B.8，属 visualization epic）

## Dependencies

**Unblocked by**: Core 15/15 epic Done or deferred（1271 绿）；adr-0004 Accepted；godot-architecture-manifest 基线
**Blocks**: 所有表现层 epic（可视化/QTE 战斗/PCG/iso 地图）

## Definition of Done

- [x] gh-001 闸口文档存在（headless 无死锁证据）
- [x] gh-002 WorldBridge 可订阅 Core DomainEvent 并打印到 Godot 控制台
- [x] gh-003 累加器驱动 World.Advance，CLI/Godot 同 seed 同轨迹
- [x] gh-004 命令端口可接收整数意图 → 下一 Tick 结算
- [x] CLI/Godot 双宿主确定性回归测试（同 seed 逐字节一致）
- [x] B.2/B.3 守（Godot 宿主层可含浮点渲染，但 Core 不进浮点/delta）
- [x] `Godot.*` 禁入 `Jianghu.Core`（BannedApiAnalyzers 或 code review 守）

## Stories

- **gh-001** gate-check-core-no-deadlock — 闸口验收：形式化 headless 无死锁证据
- **gh-002** worldbridge-signal-wiring — WorldBridge 新建 + Core→Godot Signal 最小回路
- **gh-003** fixed-timestep-accumulator — _Process→累加器→Advance 固定时间步
- **gh-004** command-port-dual-host-regression — 命令端口 + CLI/Godot 确定性回归
