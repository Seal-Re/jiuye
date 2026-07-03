# ADR-0004: Godot View/Host 边界（Model/View 单向流 + 固定时间步 + iso 坐标系预留）

- **Status**: Accepted
- **Date**: 2026-07-03（引擎目标由 Unity 切换至 Godot 4.x .NET，用户指令）
- **Last Verified**: 2026-07-03
- **Deciders**: huangjiaqi13 + Claude（architecture）
- **Affects**: `Jianghu.Core`（约束不变，新增 `Godot.*` 禁入）/ 未来 Godot 宿主层（新建，本 ADR 立边界）/ `Jianghu.Cli`（保留为 headless 驱动，与 Godot 宿主并列）
- **Supersedes**: `docs/superpowers/specs/2026-06-18-core-unity-split-design.md` 的引擎归属（Unity→Godot 术语层面；分层原则不变）

---

## Summary

项目表现层目标由 Unity 切换至 **Godot 4.x (.NET)**。本 ADR 定义 Core（Model）与 Godot 宿主（View）之间的**单向数据流 + Signal 订阅**边界、**固定时间步累加器**驱动内核 `Tick()` 的同步规矩（渲染 `delta` 绝不进 Core），并为未来 2D 等距（isometric）TileMap **预留坐标系转换红线**——当前不写任何地图/iso 代码。Core 的整数确定性（ADR-0001）、off 逐字节（ADR-0003）、模块工厂（ADR-0002）三条地基**一字不改**。

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Godot 4.x (.NET / C#)。当前不钉死小版本；采用时经 headless 检索确立并建 `docs/engine-reference/godot/VERSION.md`（红线 B.1）。 |
| **Domain** | Rendering / UI / Input / Scripting（宿主层）；Core 仍 engine-agnostic |
| **Knowledge Risk** | MEDIUM — Godot 4.x .NET 集成 API（`[Signal]`/`_Process`/`TileMap` iso）在训练数据内，但小版本行为需对目标版本复核 |
| **References Consulted** | 当前无（本 ADR 只立**边界纪律**，不依赖具体 Godot API 实现）。宿主层落地时须补 `docs/engine-reference/godot/`。 |
| **Post-Cutoff APIs Used** | None（本 ADR 不写代码） |
| **Verification Required** | 宿主层实现时：① `[Signal]` C# 委托订阅 Core 事件的实测 ② 固定时间步累加器在目标版本 `_Process(double delta)` 下的实测 ③ `TileMap` iso `MapToLocal`/`LocalToMap` 坐标行为 |

> Knowledge Risk = MEDIUM：若项目升 Godot 大版本，本 ADR 须重新校验（`Last Verified` 更新或标 Superseded）。

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0001（整数确定性）、ADR-0003（off 逐字节）——本 ADR 的"delta 不进 Core"正是为守住二者 |
| **Enables** | 未来 Godot 宿主 epic；可视化 C#→数据桥 spike |
| **Blocks** | 「Godot 宿主初始化」「2D 等距地图」两条 backlog——未落地本边界前不得起 |
| **Ordering Note** | 本 ADR 是宿主层**任何**代码的前置门。地图/iso **本轮不开发**，仅预留 §决策-③ 缝隙。 |

---

## Context

### 问题
原引擎目标为 Unity（见 `2026-06-18-core-unity-split-design.md`），现改为 **Godot 4.x (.NET)**。需在动任何宿主代码前，把"逻辑与表现严格分离"的边界纪律换成 Godot 语汇并物理落盘（规范先行，红线 A.10 / 核心执行原则 1）。

### 现状（切换起点）
- `Jianghu.Core`（`netstandard2.1`）纯逻辑库，零引擎依赖，BannedApiAnalyzers 守（`System.Random`/`Console`/`DateTime`/`Thread` 禁；`Jianghu.Cultivation` 禁浮点 IL 扫描守）。**1062 测试绿**。
- `Jianghu.Cli`（`net8.0`）薄壳 Host：解析参数 → `WorldFactory.CreateInitial` → `World.Advance` → 文本快照。
- **表现层尚未生成**；核心处于 headless C# 状态。**2D 等距地图从未实现**（`Jianghu.Sim.WorldMap` 是引擎无关的**整数图拓扑**，非空间/像素 iso，不在本 ADR 的"地图代码"范畴）。

### 约束
- **不破 B.2 整数确定性 / TDD 套件**（核心执行原则 3）——本 ADR 只加边界，不改 Core 逻辑。
- **最小侵入**：切换 ≈ 术语对齐 + 新增桥接红线，非架构重写。
- Godot 4.x .NET 桌面运行时 = **CoreCLR**（历史 Godot 3 为 Mono）；确定性论证须覆盖跨运行时（CoreCLR JIT / 各平台 AOT / Mono），故 ADR-0001 的禁浮点更不能松。

### `netstandard2.1` 为何保留
Godot 4.x .NET（.NET 6/8 CoreCLR）**可直接引用 `netstandard2.1` 类库**。故 `Jianghu.Core` 的 TFM **不改**——"零改写进引擎"承诺从 Unity 平移到 Godot 反而更贴合标准 .NET 工具链（Godot 用 `.csproj`/`dotnet` 消费程序集，无 Unity 的 asset 导入层）。

---

## Decision

**Core = Model（纯逻辑数据层），Godot = View（只读渲染 + 输入采集）。三条规矩落定：**

### ① Model → View 单向数据流 + Signal 订阅

```
   Jianghu.Core (Model, netstandard2.1)          Godot 宿主 (View, .NET)
   ┌───────────────────────────────┐             ┌──────────────────────────────┐
   │ World / Chronicle / Snapshot  │  读状态 →   │ WorldBridge : Node            │
   │  - DomainEvent (追加日志)      │ ──────────► │  轮询/拉取 Chronicle 增量      │
   │  - StateSnapshot (只读投影)    │             │  → emit [Signal] 事件          │
   │                               │             │       ↓ 订阅                   │
   │  ▲ 命令端口 (显式, 整数意图)   │ ◄────────── │  渲染节点 (只读, 不含逻辑)     │
   └───────────────────────────────┘  回写只经   └──────────────────────────────┘
                                       command 端口
```

- **数据只沿 Model→View 单向流**：Godot 节点**只读** `World`/`StateSnapshot`/`Chronicle`，**绝不**反向写 Core 内部状态。
- **Signal 订阅**：Godot 侧 `WorldBridge`（一个 `Node`）负责拉取 Core 的 `DomainEvent`/快照增量，转成 Godot C# `[Signal]`（`public delegate void XxxEventHandler(...)`）向渲染节点广播。**Core 不知道 Godot 的存在**——它只产出 `DomainEvent`；桥接适配在宿主层。
- **玩家介入（回写）唯一合法通道 = 显式命令端口**：玩家输入 → 宿主收集为**整数意图**（如"角色 X 选招 Y"）→ 经显式 command 接口喂进下一个确定性 `Tick`。**绝不**让 View 直接改 Core 字段，**绝不**把浮点/帧时/坐标塞进 Core。
- **`Godot.*` 禁入 `Jianghu.Core`**（对标原 `UnityEngine.*` 禁令）：Core 保持零引擎依赖是"零改写"前提。桥接代码（`using Godot`）只属宿主程序集。

### ② `_Process()` ↔ 内核 `Tick()` 同步 = 固定时间步累加器

**渲染帧与模拟步彻底解耦。** Godot 的 `_Process(double delta)` **只做渲染/插值**，`delta`（浮点帧时间）**绝不进 Core**。内核推进走**固定时间步累加器**：

```csharp
// Godot 宿主节点（示意，宿主层实现；Core 不含此代码）
private double _acc;
private const double SimStepSeconds = /* 宿主常量, 如 0.1 */;

public override void _Process(double delta)   // delta: 浮点, 仅宿主可见
{
    _acc += delta;
    while (_acc >= SimStepSeconds)
    {
        _world.Advance(oneStepBudget);        // ← 内核只吃"步", 不吃 delta
        _acc -= SimStepSeconds;
    }
    RenderInterpolated(_acc / SimStepSeconds); // 视觉插值用浮点, 纯 View
}
```

- **`World.Advance` 由"逻辑步/玩家意图"驱动，不由帧率驱动**：够一个 `SimStepSeconds` 才推进一步，掉帧只影响追帧次数，不改**每步的确定性轨迹**（同种子 → 同轨迹，与帧率无关）。
- **观察态（自动播放）**：宿主累加真实时间自动追帧；**交互态**：玩家 step/命令触发 `Advance`。二者都不把 `delta` 传入 Core。
- **为何**：若 `Advance` 每帧调一次或吃 `delta`，模拟速度随帧率漂移、且浮点帧时污染整数内核 → 破 B.2 + 破可复现。固定步是确定性模拟接实时渲染的标准解法。

### ③ 2D 等距（Isometric）TileMap 坐标系转换红线（**架构预留，当前 0 代码**）

**地图系统未设计——本轮绝不实现任何地图/iso 代码。** 仅在此登记未来红线：

- **iso 投影是 View 专属**：等距屏幕坐标 `screen = ((gx - gy) * tileW/2, (gx + gy) * tileH/2)`（及其逆）是**浮点/像素温床**，只能活在 Godot 宿主层（`TileMap.MapToLocal`/`LocalToMap` 或宿主自算），**绝不进 `Jianghu.Cultivation` / Core**。
- **Core 只认整数逻辑格**：若未来地图逻辑需空间坐标，Core 侧只持**整数格坐标**（`(int gx, int gy)` 逻辑网格），engine-agnostic；屏幕像素/iso 菱形投影全在 View 换算。
- **单向仍成立**：Core 出逻辑格 → 宿主投影到 iso 屏幕像素（Model→View）；玩家点击 iso 屏幕 → 宿主逆投影回整数格 → 经 command 端口进 Core（回写只经命令端口，同 ①）。
- **预留缝、不落地**：未来可引 `IMapProjection`（宿主实现）隔离 iso 换算；`Jianghu.Sim.WorldMap`（现有整数图拓扑）与 iso 空间层**分离**。**本 ADR 不创建任何接口/类**——预留 = 文档登记，代码 0 行。

---

## Alternatives Considered

### 备选 1：`_Process` 每帧直调 `World.Advance(delta)`
- **描述**：宿主每渲染帧推进一步内核，传 `delta`。
- **拒因**：浮点 `delta` 进 Core 直接破 B.2；模拟速度随帧率漂移，破同种子可复现。**否决**。

### 备选 2：Core 暴露 Godot 友好类型（`Vector2`/`Node` 引用）
- **描述**：Core 直接用 `Godot.Vector2` 等表达坐标/句柄，省一层适配。
- **拒因**：`Godot.*` 进 Core 破"零改写/零引擎依赖"；`Vector2` 是浮点，破 B.2；且把 Core 焊死在 Godot 上，CLI/测试宿主无法复用。**否决**——坐标用整数格，句柄用 Core 侧 id。

### 备选 3（保留）：Core `netstandard2.1` 保持不动 vs 升 `net8.0`
- **决定**：**保持 `netstandard2.1`**。Godot 4.x .NET 可直接引用，无需升级；升级无收益且扩大改动面。未来若确需 net8 API，另立 ADR。

---

## Consequences

### Positive
- 引擎切换**零触碰 Core 逻辑**：1062 测试、B.2/B.3、determinism 轨全部不动（核心执行原则 3 达成）。
- Model/View 边界比 Unity 时更干净：Godot 用标准 `dotnet`/`.csproj` 消费 `netstandard2.1` 程序集。
- 固定时间步 → 渲染帧率与模拟轨迹解耦，同种子跨机器/跨帧率逐字节一致仍成立。
- CLI headless 驱动与 Godot 宿主**并列共存**（同一 Core，两个 View）——headless 回归不受宿主影响。

### Negative
- 宿主层多一层 `WorldBridge` 适配（Core 事件 → Godot Signal），非零成本。
- 固定时间步需宿主处理"追帧螺旋"（掉帧时 `while` 可能多轮）——宿主须设每帧最大追帧数上限（宿主层实现细节，不入 Core）。

### Neutral
- 原 Unity 专属清单（IL2CPP/UGUI/UI Toolkit/Addressables/ParticleSystem）→ 对应 Godot 概念（CoreCLR/Godot UI/Control 节点/资源系统/GPUParticles），术语替换，分层不变。

---

## Migration Plan（文档层，本 ADR 范围内）

1. 立本 ADR（Accepted）——边界真相源。✅
2. `architecture.md` 加 §9 Godot View/Host 边界 + §7 索引本 ADR；§1/§2/§6 Unity→Godot 术语对齐。
3. `control-manifest.md` 加 Presentation/Host 层（P-REQUIRED/P-FORBIDDEN）；`Godot.*` 禁入 Core；IL2CPP→CoreCLR/Mono 表述。
4. `technical-preferences.md` + 红线源 `CLAUDE.md` + agent 指南 Unity→Godot 对齐。
5. Phase 2：对照本边界审查现存 Core 代码（预期**零冲突**——Core 本就零引擎依赖；若查出残留 Unity 强绑定则修，否则 Skip）。

**Rollback**：本 ADR 纯文档 + 边界纪律，无代码。若引擎决策再变，标 Superseded 另立 ADR，Core 不受影响（这正是 Model/View 分离的价值）。

## Validation Criteria

- [x] 边界规矩物理落盘（本文件 + architecture/control-manifest/technical-preferences 对齐）
- [x] Core TFM `netstandard2.1` 不变、`Jianghu.Cultivation` 禁浮点不松
- [ ] （宿主层未来）`WorldBridge` 只读 Core、回写只经 command 端口、`Godot.*` 不出现在 `Jianghu.Core`
- [ ] （宿主层未来）`Advance` 走固定时间步，`_Process` 的 `delta` 不进 Core
- [ ] （地图未来）iso 投影只在宿主；Core 只持整数逻辑格

## GDD Requirements Addressed

Foundational — 无直接 GDD 需求。本 ADR 是**引擎接入的基础边界决策**，约束/解锁未来所有 Godot 宿主系统（可视化、玩家介入、2D 等距地图渲染），并守护 ADR-0001/0003 的确定性地基不被表现层污染。

## Related

- ADR-0001（整数确定性，B.2）— 本 ADR 的"delta/iso 浮点不进 Core"守之
- ADR-0003（off 逐字节，B.3）— 表现层为只读 View，天然不扰动 off 轨迹
- `docs/superpowers/specs/2026-06-18-core-unity-split-design.md` — 引擎归属被本 ADR 平移（Unity→Godot），P0-P3 分层逻辑仍有效
- 红线 A.10（表现层接入闸口）/ 核心执行原则（规范先行 / 最小侵入 / 内核基线保全）
