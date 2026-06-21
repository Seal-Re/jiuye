# ADR-0003: Cultivation-off Mode Byte-Identical with v1.0

- **Status**: Accepted
- **Date**: 2026-06-21（逆向自红线 B.3 + OffByteIdenticalTests 实现）
- **Deciders**: huangjiaqi13 + Claude (architecture-review)
- **Affects**: `Jianghu.Core.Sim` — `World`, `WorldFactory`, `Lifecycle`, `SparAction`

---

## Context

江湖模拟内核分两个模式：
- **off 模式**（`cultivation=false`）：纯 v1.0 规则——角色无修炼，战力 = Force×2 + Internal + Constitution，切磋纯 power 比大小。这是基线模式，确定性必须与最初 v1.0 输出**逐字节一致**。
- **on 模式**（`cultivation=true`）：修炼系统（21 路/境界/战斗模块）叠加在 v1.0 之上。

问题：修炼系统需修改共享文件（如 `SparAction.cs`、`Lifecycle.cs`），但不能破坏 off 模式。如何在叠加新系统的同时保证 off 路径不改一个字节？

---

## Decision

**修炼系统走独立 PRNG 流，off 模式完全旁路修炼逻辑，输出逐字节一致。**

具体：
1. **独立 PRNG 流**：`RngStreamIds.Cultivation = Split(5)`（`Pcg32.Split(id)` 跳号派生子流，不消费父状态）。所有修炼随机消费走 `cultRng`，不碰 `domainRng`(Split2) / `spawnRng`(Split3)。
2. **off 分支**：`cultivation=false` → `WorldFactory` 不调用 `PathAssigner.TryAssign` → 所有角色 `CultivationState == null` → `SparAction` / `Lifecycle` 走 legacy 分支（`cs==null → legacy formula`）。
3. **on 分支**：`cultivation=true` → `CultivationState != null` → 走修炼公式（PowerEngine / DuelEngine）。
4. **侧表纪律**：新态（CultivationState 等）挂 `Character` 侧表，不污染 v1.0 core record（StatBlock/Persona/Relations 字段顺序一字不改）。
5. **每个被改 v1.0 文件补 off 逐字节回归测试**：`OffByteIdenticalTests` 验证 cultivation=false 时输出 SHA256 与 v1.0 基线一致。

---

## Consequences

### Positive
- off 模式永久可回归——改任何 v1.0 文件后跑 off regression 即可
- 修炼系统可独立演进——on 模式改动不破 off
- 逐字节可证伪——不是"声称一致"，是 SHA256 断言
- PRNG 流隔离——修炼随机消费不改变 domain 事件的确定性序列

### Negative
- 改 v1.0 共享文件（如 SparAction.cs）需同时验证 off/on 两条路径
- 新 PRNG 流 = 不同种子 → on/off 的 domain 事件序列不再逐字节一致（但各自内部分别确定）

### Mitigation
- `OffByteIdenticalTests` 在 CI 跑，改 v1.0 文件后 commit 前必跑
- `Off_BothNull_UsesLegacyFormula` 等专项测试覆盖每个被改文件的 off 路径
- `DuelGateTests` 确保 on 路径不误入 off 分支

---

## References
- `src/Jianghu.Core/Sim/WorldFactory.cs`
- `src/Jianghu.Core/Sim/World.cs`
- `src/Jianghu.Core/Actions/SparAction.cs`
- `src/Jianghu.Core/Sim/Lifecycle.cs`
- `src/Jianghu.Core/Random/RngStreamIds.cs`
- `tests/.../Determinism/OffByteIdenticalTests.cs`
- `tests/.../Determinism/CultivationDeterminismTests.cs`
- 红线 B.3 (CLAUDE.md §B.3)
