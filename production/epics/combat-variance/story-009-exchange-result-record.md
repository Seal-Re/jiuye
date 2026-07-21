# Story 009: CombatExchangeResult record — ResolveExchange 5 元组重构

> **Epic**: combat-variance
> **Status**: Complete（2026-07-21 — CombatExchangeResult 已实现，1281绿，行为零改变）
> **Layer**: Core
> **Type**: Logic
> **Estimate**: 小 (0.5d)
> **Depends**: cv-004 (DefenseFrameHook 第 5 字段已就位)
> **ADR**: adr-0001 (B.2), adr-0003 (B.3)

## Context

cv-001~004 增量扩展使 `ResolveExchange` 返回从 2 元组累积到 5 元组（`(int Dmg, int Reflect, int Poise, bool ChipImmune, DefenseFrameHook? FrameHook)`）。未命名元组语义弱、扩展需改所有调用方。用 `record` 重构为命名数据对象。

## Acceptance Criteria

- [x] 9.1 `CombatExchangeResult` sealed record：DmgToDefender / ReflectToAttacker / PoiseBreakBonus / ChipImmuneToPoise / FrameHook
- [x] 9.2 `ResolveExchange` 返回 `CombatExchangeResult`（替换 5 元组）
- [x] 9.3 所有调用方适配（ResolveR2 ×2 + 全量测试文件）
- [x] 9.4 行为零改变——全量 1281 绿不退
- [x] 9.5 B.2/B.3 守（IL 浮点零 + off 逐字节）

## Implementation Notes

- 新建 `src/Jianghu.Core/Cultivation/CombatExchangeResult.cs`
- 改动面：`ResolveExchange` 签名 + `ResolveR2` 两处解构 + 测试文件引用
- 纯重构——不改行为、不改公式、不碰 off 路径

## Test Evidence

**Required**: 全量 1271 绿不退（行为零改变即证重构正确）
