# Story 002: INV-CROSS 对拍胜率实证 gate

> **Epic**: balance-cross
> **Status**: Review
> **Layer**: Core
> **Type**: Integration
> **Estimate**: 中 (1d)
> **Depends**: balance-001 (BalanceMatrixDump harness)
> **ADR**: adr-0001-integer-determinism
> **GDD**: cultivation-system.md §AC / legacy specs B5-平衡标定INV-CROSS-design.md §2-4

## Context

balance-001 dump 暴露全路战力矩阵后，本 story 做 INV-CROSS 对拍胜率实证——验证收敛到契约 C1（同 UT 胜率 ∈ [40,60]%）的可行性，并建立 gate 断言。不是一次性修到完美，而是建立"可测→可修→可验"的闭环。

**深层问题**（INV-CROSS spec §1）：RealmMultipliers 失衡 68× + BaseSum 失衡 24× → 两根因。收敛需同时调 mul 和 BaseSum term 权重。

**深度源**: balance-cross EPIC §DoD / INV-CROSS spec §2-4 / PowerEngine.cs。

## Acceptance Criteria

- [x] 2.1 全路组合对拍 ≥ 50 场/对（同 UT 内随机取样 K 对）
- [x] 2.2 首轮 gate：同 UT 胜率分布 ∈ [35,65]%（可接受带，非最终 [40,60]%）
- [x] 2.3 C2 碾压单调性：UT 差 ≥ 2 → 高 UT 胜率 ≥ 80%
- [x] 2.4 C3 辅助路豁免：Dan/Array/Qixiu 同锚锁 UT 在合理 power 带（非战斗路不强求 50%）
- [x] 2.5 跨 3 UT 以上碾压 → 胜率 ≥ 95%
- [x] 2.6 胜率矩阵可复现（同种子同矩阵）
- [x] 2.7 全量绿 + IL 浮点零 + off 逐字节

## Implementation Notes

- 依赖 balance-001 的 `BalanceMatrixDump` + 典型角色工厂
- 对拍引擎：在 `InvCrossDuelTests.cs` 中遍历 path-pair → simulate N 场 → 记录胜率
- 第一轮只调 RealmMultipliers（每路 1 个 `double` → 转整数 `×10`），不碰 per-term 权重
- 若首轮 mul 调后仍有 > 65% 或 < 35%，标 known_issue，不卡死

## Test Evidence

**Required**: `tests/Jianghu.Core.Tests/Cultivation/InvCrossDuelTests.cs` — ~5 tests + harness
**QA ref**: `production/qa/qa-plan-sprint-3.md` § balance-002

## Out of Scope

- 收敛到 [40,60]%（后续 sprint）
- per-term 权重重调（BaseSum term 对路差异化）
- 实战分布对拍（奇遇/功法分化）
