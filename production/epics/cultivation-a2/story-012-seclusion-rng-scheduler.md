# Story 012: 闭关RNG自洽+Scheduler集成

> **Epic**: cultivation-a2
> **Status**: Not Started
> **Last Updated**: 2026-06-24
> **Layer**: Core
> **Type**: Integration
> **Estimate**: 小 (0.5d)
> **Depends**: story-009
> **ADR**: adr-0001-integer-determinism
> **GDD**: A3-FINAL §7

## Context

A3-FINAL §7 定义了闭关期间的 RNG 自洽——闭关时长/收益的随机骰子必须在进入闭关时确定（冻结），出关时不重新骰。SECLUSION_STREAM 独立于主修炼流。

## Acceptance Criteria

- [ ] 12.1 `SECLUSION_STREAM = 0x5EC1` ——闭关专用 RNG 子流（从 `_cultRng` Split）
- [ ] 12.2 进入闭关时冻结 RNG 状态（骰 Duration/收益/Epiphany），出关时使用冻结值
- [ ] 12.3 闭关期间 spawn/encounter RNG 不扰闭关流（隔离）
- [ ] 12.4 Scheduler 不 PopMin 闭关角色（实证：100 tick 模拟中闭关角色仅出现在 Wake tick）
- [ ] 12.5 闭关角色在 Scheduler 堆中位置 = WakeAt（与正常角色混排，无特殊堆）
- [ ] 12.6 确定性：同种子→闭关角色 WakeAt 一致
- [ ] 12.7 off 模式不集成

## Implementation Notes

**RNG 流隔离**：
```
EnterSeclusion():
    seclusionRng = _cultRng.Split(SECLUSION_STREAM)
    duration = ComputeDuration(workUnits, insight, seclusionRng)
    benefits = FreezeBenefits(seclusionRng)
    // benefits 存入 CultivationState.Flags["seclusionBenefits"] (序列化)
    character.NextActAt = world.Now + duration

ExitSeclusion():
    benefits = Deserialize(CultivationState.Flags["seclusionBenefits"])
    ApplyBenefits(benefits)
```

**Scheduler 实证**：集成测试——启动 10 NPC（2 闭关 + 8 正常），运行 200 tick。验证闭关角色仅在 WakeAt tick 出现。

## Out of Scope

- 闭关被打扰提前出关的 RNG 处理（G5）

## Test Evidence Requirement

**Type**: Integration — integrated test. Scheduler non-pop proof, RNG freeze determinism, stream isolation, off mode no-op.
