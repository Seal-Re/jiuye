# ADR-0001: Integer Determinism for Jianghu Cultivation

- **Status**: Accepted
- **Date**: 2026-06-21（逆向自红线 B.2 + BannedApiAnalyzers 配置）
- **Deciders**: huangjiaqi13 + Claude (architecture-review)
- **Affects**: `Jianghu.Core` / `Jianghu.Core.Cultivation`

> **2026-07-03 注**：引擎目标由 Unity 切至 **Godot 4.x (.NET)**（见 [adr-0004](adr-0004-godot-view-host-boundary.md)）。本 ADR 的决策**引擎无关**、完全成立——下文 "Unity/IL2CPP" 是历史撰写时的跨运行时**举例**（IL2CPP 仍是 AOT 舍入分歧的真实例证），保留为历史记录；现宿主运行时以 Godot CoreCLR / 各平台 AOT / Mono 为准。

---

## Context

江湖涌现模拟内核需要**确定性可复现**：同种子同输入 → 逐字节相同输出。这是模拟科学性的最低门槛，也是后续"战斗回放验证""平衡标定可对照""off 模式逐字节一致"的硬前提。

C# 生态有多个浮点/时间/线程不确定性来源：
- `System.Random` 在不同 .NET 运行时（Framework vs .NET 6+ vs Mono/IL2CPP）同种子序列不同
- `float`/`double` IEEE754 在不同后端/优化等级（JIT vs AOT vs IL2CPP）舍入行为可能不同
- `System.DateTime.Now` 是挂钟时间（不可复现）
- `System.Threading` 多线程执行顺序不确定

后期 Unity 宿主（IL2CPP）与当前 CLI（.NET 8 JIT）跨运行时一致性要求更严。

---

## Decision

**Jianghu.Cultivation 及所有逻辑层全程禁浮点，全部整数确定性。**

具体：
1. **PRNG**：`Pcg32`（种子驱动，全程 `uint`/`ulong` 整数运算）。`Split(id)` 跳号派生子流，不消费父状态。
2. **禁浮点**：Jianghu.Cultivation 全命名空间 IL 扫描守（`ILFloatScanner` 测试），零 `float`/`double`/`System.Math`.
3. **禁挂钟**：`System.DateTime` / `System.DateTimeOffset` 禁入 Core。时间以 `Tick`（逻辑 tick 计数）为准。
4. **禁多线程**：`System.Threading.Thread` / `Task.Run` 禁入 Core。模拟单线程确定性推进。
5. **禁 System.Random**：强制注入 `IRandom` 端口。
6. **BannedApiAnalyzers**（`Microsoft.CodeAnalysis.BannedApiAnalyzers` 3.3.4）在 `Jianghu.Core.csproj` 编译期强制，违者编译错误（RS0030=error）。

---

## Consequences

### Positive
- 同种子在任何 .NET 运行时（CLI JIT / Unity IL2CPP / Mono）逐字节一致
- 战斗回放可验证（录 seed+操作序列 → 同结果）
- 平衡标定可对照（同参数集 → 同矩阵，可 diff）
- off 模式可回归验证（逐字节 assert）

### Negative
- 不能用 `System.Math` 做三角函数/指数（需要时用整数查表或定点近似）
- 不能用 `System.Random` 快捷随机（强制注入 IRandom）
- 每个新 PRNG 消费者需走 RngStreamIds.Split，不能裸 new

### Mitigation
- `RandomExtensions` 提供整数范围内的便利方法（`Next(min,max)` / `Pick(list)` 等）
- 若未来真需要非整数（如 Unity 即时反应窗口的正态判定），放在 Unity 宿主层，不进 Core（红线 B.2 只管 `Jianghu.Cultivation`）

---

## References
- `src/Jianghu.Core/Random/Pcg32.cs`
- `src/Jianghu.Core/Random/RngStreamIds.cs`
- `src/Jianghu.Core/BannedSymbols.txt`
- `tests/.../ILFloatScanner.cs`
- `tests/.../OffByteIdenticalTests.cs`
- 红线 B.2 (CLAUDE.md §B.2)
