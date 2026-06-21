# Story 001: BalanceMatrixDump harness — 全 21 路 × UT 战力矩阵 dump

> **Epic**: balance-cross
> **Status**: Ready
> **Layer**: Core
> **Type**: Integration
> **Estimate**: 中 (1.5d)
> **Depends**: combat-r2 done (PowerEngine/DuelEngine 就位)
> **ADR**: adr-0001-integer-determinism / adr-0003-cultivation-off-byte-identical
> **GDD**: cultivation-system.md §Formulas / legacy specs B5-平衡标定INV-CROSS-design.md §1-3

## Context

跨路平衡（INV-CROSS）是 🔴 最大功能缺口——同 UT 战力差 24-70×（实测：丹 realm0=8 vs 体 realm0=146 = 18×；RealmMultipliers 丹5→雷340 = 68×）。要收敛必须先看清全貌。

本 story 建 `BalanceMatrixDump` harness：对 21 路 × 9 UT（UT {0,2,4,6,8,9,10,11,12}）生成"典型角色"并 dump 战力矩阵，输出 pe 值/范围/方差，供人工判读和后续收敛迭代。

**深度源**: balance-cross EPIC §Scope / INV-CROSS spec §1-3 / PowerEngine.cs。

## Acceptance Criteria

- [ ] 1.1 21 路 × 9 UT = 189 cell 矩阵完整（无 missing cell）
- [ ] 1.2 每个 cell 输出：pe 值、pe 均值/方差（多次采样）、实测范围 [min,max]
- [ ] 1.3 Dump 格式可读（CSV 或 JSON，含行列标签，能人工判读）
- [ ] 1.4 典型角色 @ (path p, UT u)：四维中庸 Σ=80、realm 段首、标准 loadout、flags 空
- [ ] 1.5 随机采样 K 对同 UT 路径，记录对拍胜率（≥ 2 路实证）
- [ ] 1.6 Deterministic：同种子 → 同矩阵（逐字节复现）
- [ ] 1.7 全量绿 + IL 浮点零 + off 逐字节

## Implementation Notes

- 新建 `tests/.../Cultivation/BalanceMatrixDumpTests.cs` 或 `BalanceCrossHarness.cs`（已有此文件）
- 典型角色工厂：`WorldFactory.CreateTypicalChar(path, ut)` —— 固定四维 + 固定 realm + 标准 loadout
- 矩阵输出：`BalanceMatrixDump.Run(seed)` → 写文件或 Console 输出
- 对拍：`DuelEngine.Simulate(charA, charB, N)` → 胜率
- 只 dump 不修——数据供 balance-002 收敛迭代

## Test Evidence

**Required**: `tests/Jianghu.Core.Tests/Cultivation/BalanceMatrixDumpTests.cs` — ~6 tests
**QA ref**: `production/qa/qa-plan-sprint-3.md` § balance-001

## Out of Scope

- 收敛迭代（修改 RealmMultipliers/PowerFormula）→ balance-002
- 实战分布对拍（含奇遇/功法分化的随机角色）→ balance-002
