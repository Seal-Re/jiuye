# Story drama-008: 6 戏剧 DomainEvent + Chronicle 武侠味投影（空库逐字节先证）

> **Epic**: drama-engine
> **Status**: Done（2026-06-26 /loop TDD：990 绿 [+11]，0 警告，off 逐字节实证）
> **Layer**: Core（`Jianghu.Events`，引用 `Jianghu.Drama` 类型）
> **Type**: Logic
> **Estimate**: 小-中 (0.4d)
> **Depends**: drama-004（GrudgeId/ArcId/ArcStage/GrudgeKind 值类型，done）、drama-005（GrudgeLedger，done）
> **ADR**: adr-0003-cultivation-off-byte-identical（⚠️ 本 story 首次触 v1.0 文件 DomainEvent/Chronicle）、adr-0001-integer-determinism
> **GDD**: `design/gdd/drama-system.md` §0.2(DomainEvent 单源) + §9（drama-008 = spec Step 6）

## Context（⚠️ 最高危——首触 v1.0 文件）

spec Step 6：戏剧效果经统一 `DomainEvent` → `Chronicle.Append` 管线产出（戏剧层绝不直接 mutate）。本 story 加 **6 个事件 record + 6 个 Chronicle 渲染 case**。这是 drama 首次改 v1.0 铁律文件（`DomainEvent.cs`/`Chronicle.cs`），**确定性最高危**——故执行 spec 强制纪律：

> **先证后改**：先写「空恩怨库/off 模式下这些新 case 永不被触发 → 既有 979 绿 + OffByteIdentical 逐字节不变」专测，再实现。

**安全性论证（为何纯加安全）**：6 新事件均由**戏剧层产出**（drama-009 Pump / drama-010 Advance 接线才会发），off 模式与空库**根本不构造 drama 子系统**，故这些事件**永不进入 Chronicle.Append 的 switch**。新增 `case` 是 switch 的纯扩张——对不匹配的既有事件零影响（C# switch 按类型匹配，新 case 不改既有 case 行为）。`DomainEvent.cs` 加 record 是同 abstract record 的兄弟扩张，不改既有 record 字段顺序/语义。

### 范围决策（A.8 显式）

- **本 story 只做**：6 事件 record + 6 Chronicle 文本 case。
- **显式延后到 drama-010**：`World.Project` 的 drama 事件 memory 投影（如 RevengeConsummated 写参与者负 valence Memory）——该投影仅在 Pump 接入 Advance 后可达，此刻加 case 无法单测（死代码），故随 World 接线一并落。**不静默 descope，记此处**。

> **红线约束**：B.3 off 逐字节（核心验收，新 case off 不可达）；B.2 整数确定性（事件字段全整数/枚举/Id，无浮点）；DomainEvent 单源（戏剧效果只经此管线，不旁路 mutate）；B.8 武侠味文本仅渲染层（不进数值路径）。

## Acceptance Criteria

- [x] **D8.1 六事件 record**（`DomainEvent.cs`，继承 `DomainEvent(long Tick)`，纯整数/枚举/Id 字段）：
  - `GrudgeFormed(Tick, GrudgeId, Holder, Target, GrudgeKind, Intensity)`
  - `GrudgeInherited(Tick, GrudgeId, Heir, Target, FromGrudge:GrudgeId, Generation, Intensity)`
  - `ArcIgnited(Tick, ArcId, Avenger, Target)`
  - `ArcStageEntered(Tick, ArcId, ArcStage)`
  - `RevengeConsummated(Tick, ArcId, Avenger, Target, AvengerPrevailed:bool)`
  - `ArcAbandoned(Tick, ArcId, Reason:string)`（reason 仅渲染串，如 "TargetDied"）
- [x] **D8.2 六 Chronicle case**（`Chronicle.Append` switch 各加一 case，武侠味文本 + name 解析）：恩怨结成 / 父债子偿继承 / 复仇弧点燃 / 阶段推进 / 决战了断（胜负措辞不同）/ 复仇弧中止。每条含 `[{Tick}]` 前缀（同既有格式）。
- [x] **D8.3 事件值相等性**：6 record 各满足值相等（同字段→相等，异字段→不等）。
- [x] **D8.4 ⚠️ 空库 no-op 逐字节（核心）**：off 模式 World 跑 N 步，Chronicle 不含任何 drama 行（6 新事件文本特征串均不出现）；既有 `OffByteIdenticalTests` 全绿（新 case 不可达 → 轨迹零偏移）。
- [x] **D8.5 各事件渲染快照**：每事件单独 Append → Lines 含其特征措辞（恩怨/父债子偿/复仇/决战 等），且 name 解析器映射 Id→称谓生效。
- [x] **D8.6 IL 浮点零 + 既有 979 绿不退 + clean rebuild 0 警告**：事件字段无浮点；`Jianghu.Drama` 扫描仍零。

**机器证据（2026-06-26 /loop）**：全量 **990 绿** / 0 失败 / 0 skip（979 → +11）；clean rebuild 0 警告；
⚠️ 命门验证：off 逐字节 + determinism + Chronicle + drama off-proof 共 38 绿（首触 v1.0 Chronicle/DomainEvent 证安全）。

### 实现细节确认
- DomainEvent.cs 加 `using Jianghu.Drama`——同程序集跨命名空间引用 GrudgeId/ArcId/ArcStage/GrudgeKind，**无循环**（预案未触发，编译通过）。
- Chronicle switch 在 default 前插 6 case，用 `Drama.GrudgeKind`/`Drama.ArcStage` 全限定 switch 表达式映射武侠措辞。

## Implementation Notes

- **DomainEvent.cs**：在末尾（`TerritoryLost` 后）追加 6 record。`using Jianghu.Drama;` 引入 GrudgeId/ArcId/ArcStage/GrudgeKind（Drama→Events 已是既有依赖方向的反向，但同程序集 `Jianghu.Core` 内无循环——Events 引用 Drama 类型合法，Drama 也引用 Events，C# 同程序集允许）。**先验编译无循环**（若 analyzer 报循环，则把 6 事件放 `Jianghu.Drama` 命名空间下新文件 `DramaEvents.cs`，继承 `Jianghu.Events.DomainEvent`——预案）。
- **Chronicle.Append**：在 `default` 前插 6 case。文本风格对齐既有（如 TerritoryLost 的「两派自此结怨」）。胜负措辞：`RevengeConsummated.AvengerPrevailed ? "手刃仇人，大仇得报" : "技不如人，饮恨当场"`。
- **GrudgeKind 措辞**：Insult→"羞辱之仇" / Maiming→"残身之仇" / Slaughter→"灭门血仇"（switch 表达式）。
- 新事件**不**进 `World.Project`（本 story），见范围决策。

## Test Evidence

**Required (BLOCKING — Logic + 确定性)**:
- `tests/Jianghu.Core.Tests/Drama/DramaEventChronicleTests.cs` —— D8.2/D8.3/D8.5：6 事件各 Append 渲染特征串；record 值相等；GrudgeKind 措辞分支；胜/负措辞不同。
- `tests/Jianghu.Core.Tests/Determinism/DramaOffByteIdenticalTests.cs` —— D8.4：off World 跑 N 步 Chronicle 无 drama 特征串；同种子两跑逐字节（复用既有 off caller）。
- 既有 `OffByteIdenticalTests` / `DeterminismTests` / `CultivationFloatScanTests` / `DramaFloatScanTests` 全绿（回归守护）。

## Out of Scope

- `World.Project` drama 事件 memory 投影（→ drama-010 随接线落，已显式记）。
- DramaScheduler / Pump 实际产这些事件（drama-009）。
- World 字段 / Advance / Clone（drama-010）。
- 事件触发的 Relations/Goal 副作用（drama-011）。
