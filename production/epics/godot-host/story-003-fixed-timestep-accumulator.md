# Story 003: 固定时间步累加器 — _Process→Advance 确定性驱动

> **Epic**: godot-host
> **Status**: Complete（2026-07-20 — 累加器就绪，1272 绿）
> **Layer**: Presentation
> **Type**: Integration
> **Estimate**: 中 (1.0d)
> **Depends**: gh-002（WorldBridge 就位）
> **ADR**: adr-0004 §②（固定时间步累加器，delta 不进 Core）

## Context

adr-0004 §② 定：Godot `_Process(double delta)` 只渲染/插值，`delta`（浮点帧时）**绝不进 Core**。内核走**固定时间步累加器**：宿主累加真实时间，够一个 `SimStepSeconds` 就 `World.Advance` 一步，渲染按 `acc/step` 插值。

`Advance` 由**逻辑步/玩家意图**驱动，非帧率驱动 → 掉帧只影响追帧次数，不改每步确定性轨迹。

## Acceptance Criteria

- [x] **3.1 累加器实现**：Godot 宿主侧 `_Process(delta)` 累加 `_accumulator += delta`；`while (_accumulator >= SimStepSeconds) { World.Advance(1); _accumulator -= SimStepSeconds; }`
- [x] **3.2 插值因子**：渲染帧读取 `_accumulator / SimStepSeconds` 作为插值因子（0→1），但不影响 Core 状态
- [x] **3.3 追帧上限**：设每帧最大追帧数（防掉帧螺旋）；默认 `MaxCatchupSteps = 5`
- [x] **3.4 delta 不进 Core**：`World.Advance` 参数不含 `delta`；`SimStepSeconds` 是 Core 侧常量（整数毫秒 or 来自 `LimitsConfig`）
- [x] **3.5 确定性验证**：同 seed，不同帧率（60fps vs 10fps）→ 同 `Advance` 步数 → 同轨迹（World state 逐字节一致）

## Implementation Notes

- `SimStepSeconds` 暂定 1.0（1 秒/步），可经 `LimitsConfig` 或宿主配置调
- 累加器逻辑在 `WorldBridge` 或独立 `SimDriver` 节点
- 插值因子仅供 View 层动画/UI 使用（角色位置平滑移动等），绝不反向写 Core

## QA Test Cases

- AC-1（3.1/3.2）：Godot 运行 N 秒 → `Advance` 调用次数 ≈ N / SimStepSeconds
- AC-2（3.3）：模拟极低帧率（1fps）→ 不崩溃，不无限追帧
- AC-3（3.4）：`grep -r "delta" src/Jianghu.Core/` 零命中（除注释）
- AC-4（3.5）：同 seed 60fps vs 10fps → 同步数后 World snapshot SHA256 一致

## Test Evidence

**Story Type**: Integration
**Required evidence**: 累加器单元测试 + 确定性别帧率测试 + delta 不进 Core grep 零命中
**Status**: [x] 已实现 — 2026-07-20，1272 绿。WorldBridge._Process(delta) 累加器 (SimStep=1s, MaxCatchup=5)；delta 不进 Core（B.2守）
