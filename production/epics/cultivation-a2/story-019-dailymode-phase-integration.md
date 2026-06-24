# Story 019: DailyMode→Phase FSM集成

> **Epic**: cultivation-a2
> **Status**: Not Started
> **Last Updated**: 2026-06-24
> **Layer**: Core
> **Type**: Integration
> **Estimate**: 中 (1d)
> **Depends**: story-005, story-004, cultivation-a1-rest (FSM done)
> **ADR**: adr-0003-cultivation-off-byte-identical
> **GDD**: A123 §A.2.1

## Context

A.2 日课微决策系统需与 A.1 10态FSM 集成。DailyMode 的选择和结果影响 Phase 转移条件（innerDemon 阈值、progress 累积、breakProgress）。DailyMode 动作 → Phase 转移 ——这是 A.2 与 A.1 的核心接缝。

## Acceptance Criteria

- [ ] 19.1 DailyMode.Apply 结果接入 World.Tick → AdvanceCultivation 流程
- [ ] 19.2 `Fast` 模式：progress 累积加速 → 可能触 Breakthrough 条件
- [ ] 19.3 `Comprehend` 模式：Epiphany 触发 → breakProgress+25 → 可能直接通过瓶颈
- [ ] 19.4 `Steady` 模式：Foundation+1 → 影响后续 realm 品质
- [ ] 19.5 `Roam` 模式：触发 Move + encounter，innerDemon-2
- [ ] 19.6 DailyMode 结果经 `_cultRng`（Split(5)），不与 Phase RNG 冲突
- [ ] 19.7 Phase 转移时检查 DailyMode 前置条件（如 Breakthrough 锁 Fast）
- [ ] 19.8 确定性：同 seed+Dailymode → 同 Phase 转移结果
- [ ] 19.9 off 模式：DailyMode 不执行，Phase 转移走 A.0 原始路径

## Implementation Notes

**集成点**（World.Tick 修改）：
```csharp
if (cultivationOn && cs.Flags.TryGetValue("dailyMode", out var modeFlag))
{
    var mode = (DailyMode)(int)modeFlag;
    var result = DailyMode.Apply(mode, cs, character, _cultRng);
    // result.progressDelta → AdvanceCultivation
    // result.innerDemonDelta → cs.GainInnerDemon(...)
    // result.daoHeartDelta → cs.GainDaoHeart(...)
    // result.epiphanyTriggered → Phase machine check
}
// Then: Phase machine transition check (existing CultivationPhase logic)
```

**Phase 条件交互**：
- Breakthrough Phase 期间：DailyMode 锁 Fast（不可换）
- Deviation Phase 期间：强制 Steady 或 Roam
- Fallen 后：DailyMode 停用（角色已废）

## Out of Scope

- BreakAid→Breakthrough 集成（→ story-020）
- daoHeart→Tribulation 集成（→ story-021）

## Test Evidence Requirement

**Type**: Integration — integrated test. DailyMode→Phase transition chain, mode lock during Breakthrough/Deviation, off mode legacy path, determinism.
