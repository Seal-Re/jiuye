# Story 024: A.2确定性+off逐字节

> **Epic**: cultivation-a2
> **Status**: Not Started
> **Last Updated**: 2026-06-24
> **Layer**: Core
> **Type**: Logic
> **Estimate**: 小 (0.5d)
> **Depends**: story-019 (integration)
> **ADR**: adr-0001-integer-determinism, adr-0003-cultivation-off-byte-identical
> **GDD**: A2-FINAL §10

## Context

A.2 全部新代码必须通过确定性闸门和 off 逐字节闸门。所有 RNG 走 `_cultRng`（Split(5)），禁止 `System.Random`（BannedApiAnalyzers 守）。off 模式下 A.2 不激活，输出与 v1.0 逐字节一致。

## Acceptance Criteria

- [ ] 24.1 确定性：同 seed → A.2 全量模拟输出逐字节一致（DailyMode/Encounter/Seclusion/daoHeart）
- [ ] 24.2 off 逐字节：`cultivation=false` 输出与 v1.0 一致（story-005 已验证 562 绿，A.2 追加后回归）
- [ ] 24.3 IL 浮点零：`Jianghu.Cultivation` 命名空间下无 ldc.r8/conv.r4（IL 扫描守）
- [ ] 24.4 RngStreamIds：A.2 不占用新 Split 号（全部经 Split(5) 子流）
- [ ] 24.5 long 溢出检查：进度/闭关系列计算不溢出（long 类型守）
- [ ] 24.6 BannedApiAnalyzers：Core 层无 System.Random/System.DateTime/System.Console/System.Threading.Thread

## Implementation Notes

**确定性回归测试**：扩展现有 `CultivationDeterminismTests` 覆盖 A.2 代码路径。

**off 逐字节回归**：`OffByteIdenticalTests` 和 `OffRegressionWith21PathsTests` 维持零回归。

## Out of Scope

- 跨平台确定性（IL2CPP/Mono）——net8.0 头less 验证即可

## Test Evidence Requirement

**Type**: Logic — automated unit tests. Determinism (2 seeds × 100 ticks), off byte-identical, IL float scan, RngStreamIds audit.
