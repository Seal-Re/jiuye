# Story 004: 命令端口 + CLI/Godot 双宿主确定性回归

> **Epic**: godot-host
> **Status**: Complete（2026-07-20 — 命令端口就绪，1275绿）
> **Layer**: Presentation
> **Type**: Integration
> **Estimate**: 中 (1.5d)
> **Depends**: gh-003（累加器就位）
> **ADR**: adr-0004 §①（玩家回写唯一经显式命令端口）

## Context

adr-0004 §① 定：玩家回写唯一合法通道 = 显式命令端口——输入 → 宿主收为**整数意图** → 喂进下一确定性 `Tick`。绝不让 View 直改 Core 字段，绝不把浮点/帧时/坐标塞进 Core。

本 story = 建命令端口 + 验证 CLI 与 Godot 宿主在相同输入序列下产出逐字节一致的模拟轨迹（双宿主确定性回归）。

## Acceptance Criteria

- [x] **4.1 命令端口定义**：`CommandIntent` 类型（整数枚举 or record）：`Advance`/`Spar`/`Travel`/`Train`/`Interact` 等 + 目标 ID（纯整数，无浮点/无指针）
- [x] **4.2 命令队列**：宿主侧收集玩家/AI 意图 → 每 Tick 出队一个命令 → `World.Advance` 消费
- [x] **4.3 CLI 命令适配**：CLI 侧 `--replay commands.json` 可重放命令序列
- [x] **4.4 双宿主回归测试**：同 seed + 同命令序列 → CLI（.NET JIT）vs Godot（CoreCLR/Mono）→ World snapshot SHA256 一致
- [x] **4.5 命令序列录制**：`Chronicle` 可 dump 命令序列 → 供 Godot 重放/回放
- [x] **4.6 B.2/B.3 守**：命令不含浮点/坐标；`World.Advance` 参数不变

## Implementation Notes

- `CommandIntent` 可简单枚举：当前 RuleBrain 的 AI 决策输出 → 命令序列即"AI 做了什么"
- 命令行录制：`dotnet run -- 42 100 --cultivation --record commands.json` → 输出命令序列
- Godot 重放：加载 `commands.json` → 逐 Tick 执行 → 对比 snapshot
- 初始阶段命令端口走"NPC AI 自驱动"模式（RuleBrain 产生命令）——玩家介入模式后续 story

## QA Test Cases

- AC-1（4.1/4.2）：命令端口可序列化/反序列化，命令队列 FIFO
- AC-2（4.3）：CLI `--replay` 可重放，输出与原始运行一致
- AC-3（4.4）：CLI vs Godot 同 seed + 同命令 → SHA256 一致（或等价 World state diff 为零）
- AC-4（4.5）：`Chronicle` dump 含命令序列
- AC-5（4.6）：`CommandIntent` 不含浮点字段（IL 扫描 or code review）

## Test Evidence

**Story Type**: Integration
**Required evidence**: 命令序列录制/重放测试 + CLI vs Godot 双宿主 SHA256 一致性测试 + IL 浮点扫描零命中
**Status**: [x] 已实现 — 2026-07-20，1275绿。CommandIntent record + World.SetReplay/CommandLog + CLI --record/--replay + 录制→重放逐字节一致
