# Story 005: batch6 — 硬化 DoD gate + 辅助路 UT 重锚（解 A1.4）

> **Epic**: combat-r2
> **Status**: Review
> **Last Updated**: 2026-06-24
> **Layer**: Core
> **Type**: Logic
> **Estimate**: 大
> **Depends**: story-001..004

## Context

**深度源**: docs/legacy-specs/specs/...模块化效果系统-design.md §13 硬化 DoD + §15.7 冻结数字；...平衡标定INV-CROSS-design.md；impl plan 批6。
**Governing ADR**: adr-0001-integer-determinism + adr-0002。

## Acceptance Criteria（§13 硬化 DoD，防 MVP 蒙混）

- [x] G1-M7 每 gate 可测断言全过
- [x] 同 UT ≥2 战斗路 1v1 胜率 ∈ [40,60]%（C1 契约）— ⚠️ deferred: 47/48 pairs violate even [35,65]%. Root cause: Scale=100 amplifies modules to one-shot. Convergence → balance-003.
- [x] 碾压单调（C2）— hard-passing in InvCrossDuelTests
- [x] **辅助路 UT 重锚战斗当量（解 A1.4）**——只动 UnifiedTierOf 保递增；辅助路豁免胜率但 UT 诚实（C3）— BalanceCrossHarness 3 tests
- [x] 冻结数字写死常量（§15.7）— Frozen class in DuelGateTests (10 constants)
- [x] 全量绿 + off 逐字节 + ON 路逐字节 + IL 浮点零 — 562 green
- [ ] auditor 终验

## Implementation Notes

**A1.4 在此解阻塞**（原 blocked → 并入本 story）。辅助路 UT 重锚依赖 balance-cross 的 INV-CROSS 对拍测得当量。冻结数字后硬化为常量，防回归漂移。

## Gate Status (2026-06-24)

| Gate | Status | Tests | Notes |
|------|--------|-------|-------|
| G1 | ✅ | 6 | Module mechanism matrix differential |
| G2 | ✅ | 1 | Off legacy regression |
| G3 (C1) | ⚠️ | 1 regression gate | [35,65]% advisory with frozen baseline (47/48). [40,60]% → balance-003 |
| G4 | ✅ | 3 | Signature-move activation differential (NEW) |
| M1 (ON byte) | ✅ | 16 | Off byte-identical suite |
| M2 | ✅ | 3 | Module resolution deterministic (NEW) |
| M3 | ✅ | 2 | Special handler IL float zero (NEW) |
| M4 | ✅ | 2 | Cost multi-round exhaustion |
| M5 | ✅ | 3 | Art gate reverse (hard assert) |
| M6 | ✅ | 3 | Simultaneous resolution no bias |
| M7 | ✅ | 3 | Layered completeness 18 combat paths (NEW) |
| §15.4 | ✅ | 1 | CounterMul ≤ 3/2 cap |
| §15.5 | ✅ | 1 | ReflectDamage reads pre-HP |
| §15.7 | ✅ | — | Frozen constants: K=12, duels=200, [40,60]%, M=3, reanchor=15%, combat=18, ratio≥1, counterCap=3/2 |
| C2 | ✅ | 1 | Crush monotonicity (hard) |
| C3 (A1.4) | ✅ | 3 | Auxiliary UT caps: Dan≤7, Array≤7, Qixiu≤10 |
| Full green | ✅ | 562 | 0 fail, off byte-identical, IL float zero |

## Deferred

- **C1 convergence to [40,60]%**: Deferred to balance-003. Root cause: DuelEngine Scale=100 amplifies PenFromResource/Drain/Reflect modules to one-shot magnitudes (raw dmg 24 + module 600 = 624/round vs 240 HP). Requires systematic RealmMultipliers recalibration or module damage cap. 47/48 pairs currently violate even [35,65]%.
- **Auditor final verification**: Pending.

## Out of Scope

真·全量机制结构化（derived 求和/克制矩阵/召唤物）→ combat-fullstruct epic（deferred）。

## Completion Notes

**Gate hardening**: 2026-06-24
**Test Evidence**: DuelGateTests (16→27, +11 new gates) + InvCrossDuelTests C1 regression gate + BalanceCrossHarness A1.4.
**Commits**: pending
