# Story drama-004: 戏剧值类型骨架（Grudge / Arc / Predicate / Effect / DramaProfile）

> **Epic**: drama-engine
> **Status**: Done（2026-06-26 /loop TDD：889 绿，+7 测试，IL 浮点零，未接线零轨迹影响）
> **Layer**: Core（`Jianghu.Drama` 命名空间）
> **Type**: Logic（纯值类型，无逻辑）
> **Estimate**: 小 (0.3d)
> **Depends**: drama-003（VariedSelector，done）；不接线 World
> **ADR**: adr-0001-integer-determinism（全整数字段）, adr-0003-cultivation-off-byte-identical（未接线→零轨迹影响）
> **GDD**: `design/gdd/drama-system.md` §3.1/§3.2/§3.3 + §9（drama-004 = spec Step 1）

## Context

戏剧引擎核心值类型。纯 `record`/`enum`，无可变态、无逻辑——为 drama-005+（账本/弧/序列器）提供数据骨架。新增即不被引用 → 既有轨迹零影响（off 逐字节自然成立）。

## Acceptance Criteria

- [ ] D4.1 ID 值类型：`GrudgeId(long Value)`、`ArcId(long Value)`（`readonly record struct`，仿 CharacterId）。
- [ ] D4.2 枚举：`GrudgeKind{Insult, Maiming, Slaughter}`（序数=严重度，max 合并用）、`GrudgeCause{Direct, Inherited, SectFeud}`、`ArcKind{Revenge}`（v1 只一种）、`ArcStage{Victimized, BuildUp, Hunting, Showdown, Resolved, Abandoned}`（5+终态）、`DramaVar{Power, Affinity, GrudgeIntensity, SameNode, TargetAlive}`（storylet 谓词变量）、`CmpOp{Ge, Le, Eq, Gt, Lt}`、`RoleRef{Holder, Target, Self}`、`EffectKind{AdjustRelation, OverrideGoal, FormGrudge, EmitChronicle}`。
- [ ] D4.3 `Grudge(GrudgeId Id, CharacterId Holder, CharacterId Target, GrudgeKind Kind, int Intensity, long OriginTick, int Generation, GrudgeCause Cause, GrudgeId? InheritedFrom)`（record，Intensity 调用方钳 [0,100]）。
- [ ] D4.4 `ArcInstance`：`ArcId Id, ArcKind Kind, CharacterId Avenger, CharacterId Target, ArcStage Stage, long NextWakeAt, int BuildUpBasePower, bool Completed`（含 `Clone()` 或 record `with` 友好——record class 默认值相等 + 非破坏式 with）。
- [ ] D4.5 `Predicate(RoleRef Subject, DramaVar Var, CmpOp Op, int Threshold)` + `Effect(EffectKind Kind, RoleRef From, RoleRef To, int Amount, int Tag)`（纯整数声明式）。
- [ ] D4.6 `DramaProfile(CharacterId Self, CharacterId? Master, CharacterId? Bloodline)`（侧表类型，不侵入 Character/Persona）。
- [ ] D4.7 record 值相等性测试（同字段==、异字段!=）；枚举序数稳定（Slaughter>Maiming>Insult 供 max 合并）；零 float；既有 882 绿不变（未接线，新文件不被引用）。

## Implementation Notes

- 放 `src/Jianghu.Core/Drama/DramaTypes.cs`（或分文件；单文件聚合值类型更省）。
- ID 用 `readonly record struct`（值语义 + 零分配）；Grudge/ArcInstance/Predicate/Effect/DramaProfile 用 `record`（class，引用但值相等，支持 `with`）。
- 全整数字段，无浮点。Intensity 钳制责任在 GrudgeLedger（drama-005），此处只定字段。
- 不 new 任何 World 字段、不改 WorldFactory/Advance → 零轨迹影响。

## Test Evidence

**Required (BLOCKING — Logic)**: `tests/Jianghu.Core.Tests/Drama/DramaTypesTests.cs`
- 各 record 值相等/不等；ID struct 值语义；枚举序数（GrudgeKind 严重度序）；ArcInstance `with` 非破坏式改 Stage；DramaProfile 可空 Master/Bloodline。

## Out of Scope

- 行为/逻辑（账本合并 drama-005、弧推进 drama-007）——本 story 只定数据骨架。
- World 接线（drama-010）。
