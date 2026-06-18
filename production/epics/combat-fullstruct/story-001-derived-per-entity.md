# Story 001: derived:* per-entity 真求和

> **Epic**: combat-fullstruct
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Estimate**: 中 (2d)
> **Depends**: combat-r2 done, DerivedRegistry 接口就位
> **ADR**: adr-0002-module-factory-effect-system（DerivedProvider 纪律: 纯整数/确定性/不读daoHeart）
> **GDD**: combat-fullstruct EPIC §scope + DerivedProviders.cs 现有 provider 模式

## Context

当前 4 路 `derived:*` 使用 A.0 简化 provider (stockFirepower=talismanStore×5, demonWeapon=MoGong/2 等)。真 per-entity summation 需要 roster 结构（逐傀/逐兽/逐鬼/逐蛊 各自 power 聚合）。

**深度源**: combat-fullstruct EPIC §scope / balance-cross 审计报告 / DerivedProviders.cs 现有 provider 模式。

## Acceptance Criteria

- [ ] 1.1 `derived:fleetWeighted` 真值 ≠ 简化为 craftScore×2。从 constructTier + mindBandwidth 计算
- [ ] 1.2 `derived:rosterWeighted` 真值：Σ beastPower_i × bond_i/100
- [ ] 1.3 `derived:ghostSoldierWeighted` 真值：Σ ghostSoldierPower × (1 − devourMeter/200)
- [ ] 1.4 `derived:guSwarmWeighted` 真值：Σ guPower_i × venomCharge/100
- [ ] 1.5 All providers registered via DerivedRegistry.RegisterAll(), IDerivedProvider.Compute 返回非零
- [ ] 1.6 UT8 power带: 战斗路 ∈ [0.7, 1.3] × 剑修（校准不破）
- [ ] 1.7 全量绿 + IL 浮点零 + off 逐字节

## Implementation Notes

- 使用现有 `IDerivedProvider` 接口，替换 `DerivedProviders.cs` 中简化 provider
- per-entity 数据暂用 CultivationState.Flags 或 Resources 近似（真 roster 结构 → FULLSTRUCT 后续）
- 约束：纯整数、确定性、不消费 RNG

## Test Evidence

**Required**: `tests/Jianghu.Core.Tests/Cultivation/DerivedProvidersTests.cs` — UT for each of 4 providers.
**Performance**: N/A — 4 provider.Compute() per Evaluate call, <1ms overhead.

## Out of Scope

- 真 roster 数据结构（逐傀/逐兽/逐鬼 list）→ FULLSTRUCT
- 克制矩阵 SituationalEdges → story-002
