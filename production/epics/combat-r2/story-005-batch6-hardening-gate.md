# Story 005: batch6 — 硬化 DoD gate + 辅助路 UT 重锚（解 A1.4）

> **Epic**: combat-r2
> **Status**: Not Started
> **Layer**: Core
> **Type**: Logic
> **Estimate**: 大
> **Depends**: story-001..004

## Context

**深度源**: docs/superpowers/specs/...模块化效果系统-design.md §13 硬化 DoD + §15.7 冻结数字；...平衡标定INV-CROSS-design.md；impl plan 批6。
**Governing ADR**: adr-0001-integer-determinism + adr-0002。

## Acceptance Criteria（§13 硬化 DoD，防 MVP 蒙混）

- [ ] G1-M7 每 gate 可测断言全过
- [ ] 同 UT ≥2 战斗路 1v1 胜率 ∈ [40,60]%（C1 契约）
- [ ] 碾压单调（C2）
- [ ] **辅助路 UT 重锚战斗当量（解 A1.4）**——只动 UnifiedTierOf 保递增；辅助路豁免胜率但 UT 诚实（C3）
- [ ] 冻结数字写死常量（§15.7）
- [ ] 全量绿 + off 逐字节 + ON 路逐字节 + IL 浮点零
- [ ] auditor 终验

## Implementation Notes

**A1.4 在此解阻塞**（原 blocked → 并入本 story）。辅助路 UT 重锚依赖 balance-cross 的 INV-CROSS 对拍测得当量。冻结数字后硬化为常量，防回归漂移。

## Out of Scope

真·全量机制结构化（derived 求和/克制矩阵/召唤物）→ combat-fullstruct epic（deferred）。
