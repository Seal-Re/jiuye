# Story 001: 恩怨/复仇基础 + RelationAdjust 跨路协议

> **Epic**: drama-engine
> **Status**: Complete
> **Layer**: Feature
> **Type**: Logic
> **Estimate**: 中 (1.5d)
> **Depends**: combat-r2 done（ModuleResolver 接入点就位）
> **ADR**: None yet（P8 补）
> **GDD**: docs/legacy-specs/specs/2026-06-13-v1.2-B-戏剧引擎-design.md

## Context

恩怨/复仇基础系统 + RelationAdjust 跨路协议。战斗模块可通过 RelationAdjust 效果修改角色间关系（恩/怨/仇），为戏剧引擎 B 打地基。

**深度源**: ModuleResolver.cs（RelationAdjust kind）+ Modules.cs（RelationAdjust 工厂）。

## Acceptance Criteria

- [x] 1.1 RelationAdjust ModKind 在 ModuleResolver 识别
- [x] 1.2 恩怨边：战斗后根据胜负/击杀更新关系值
- [x] 1.3 复仇基：关系值超阈值触发复仇标记
- [x] 1.4 跨路协议：RelationAdjust 可被任何 path 的 module 触发
- [x] 1.5 消化 2 deferred（不在本 story 范围）
- [x] 1.6 全量绿 + IL 浮点零 + off 逐字节

## Implementation Notes

- `ModuleResolver.cs` +ApplyOnDefend 分支
- `Modules.cs` RelationAdjust 工厂方法
- 关系数据在 `Relations.cs` / `MemoryStore.cs`

## Test Evidence

**Required**: 现有全量套件（410 绿）
**Commit**: `64aa4bb`

## Out of Scope

- 消化系统（deferred，见 combat-r2 EPIC）
- 完整 storylet 系统（drama-engine 后续）
