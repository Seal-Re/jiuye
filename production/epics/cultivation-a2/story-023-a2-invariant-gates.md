# Story 023: A.2全不变量硬化

> **Epic**: cultivation-a2
> **Status**: Complete （A.6 审计 2026-06-26 订正：git/代码证据证实已实现，台账滞后）
> **Last Updated**: 2026-06-24
> **Layer**: Core
> **Type**: Logic
> **Estimate**: 中 (1d)
> **Depends**: story-006, story-019
> **ADR**: adr-0001-integer-determinism, adr-0003-cultivation-off-byte-identical
> **GDD**: A3-FINAL §8

## Context

A3-FINAL §8 定义了 9 个不变量——A.2 系统的行为边界。每个不变量必须有对应的硬化闸门测试，失败 = 构建阻塞。

## Acceptance Criteria

- [ ] 23.1 **INV-VARIETY**：K=10 窗口内 DailyMode 种类 ≥ 2（story-006）
- [ ] 23.2 **INV-NO-DOMINANT**：50 tick 窗口单一模式 ≤ 80%（story-006）
- [ ] 23.3 **INV-DECOUPLE**：corr(daoHeart, Insight) < 0.7（story-022）
- [ ] 23.4 **INV-CLAMP**：daoHeart ∈ [0,100], innerDemon ∈ [0,100]（story-002）
- [ ] 23.5 **INV-NO-PE**：daoHeart/innerDemon 不在任何 PowerFormula term 中（R3，IL扫描守）
- [ ] 23.6 **INV-SECLUSION-NO-POP**：闭关角色在闭关期间不被 Scheduler.PopMin（story-012）
- [ ] 23.7 **INV-ENCOUNTER-RATIO**：`encounterProgress * 3 ≤ totalProgress`（story-015）
- [ ] 23.8 **INV-STREAK-ESCALATION**：连续闭关代价递增不递减（story-011）
- [ ] 23.9 **INV-VARIETY-CONTENT**：200 tick 窗口内奇遇种类 ≥ 20（story-016）
- [ ] 23.10 所有不变量作为门控测试 —— 任何一条失败 = CI 阻断
- [ ] 23.11 off 模式：所有不变量豁免（A.2 未激活）

## Implementation Notes

**不变量门控文件**：`tests/.../A2InvariantGateTests.cs` —— 每不变量一个 [Fact]。

**违反处理**：不变量测试失败应给出清晰诊断（哪个 NPC/哪个 tick/什么值触发了违反）。

## Out of Scope

- 不变量违反后的自动修复——当前仅检测和报告

## Test Evidence Requirement

**Type**: Logic — automated unit tests. 9 invariant tests as CI-blocking gates. Off mode exemption.
