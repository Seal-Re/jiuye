# Story 009: 闭关DES单点唤醒

> **Epic**: cultivation-a2
> **Status**: Complete （A.6 审计 2026-06-26 订正：git/代码证据证实已实现，台账滞后）
> **Last Updated**: 2026-06-24
> **Layer**: Core
> **Type**: Logic
> **Estimate**: 大 (1.5d)
> **Depends**: story-007
> **ADR**: adr-0001-integer-determinism
> **GDD**: A3-FINAL §2

## Context

A3-FINAL §2 定义了闭关（Seclusion）的离散事件模拟（DES）模型。核心思想：闭关角色在 Scheduler 中设 `NextActAt` 到遥远未来，中途不 Tick（不被 PopMin），出关时一次性补 Age。这避免了"每个闭关口都要逐 tick 处理"的性能灾难。

## Acceptance Criteria

- [ ] 9.1 `Seclusion.Enter(CultivationState, Character, int workUnits)` → 角色进入闭关态
- [ ] 9.2 进入时：`NextActAt = Now + Duration`（Duration 见 story-010），角色标记 `Flags["secluded"]=true`
- [ ] 9.3 闭关期间：Scheduler 不 PopMin 该角色（NextActAt 极远，自然沉底）
- [ ] 9.4 出关时（单点 Wake）：`Seclusion.Exit()` 触发——计算收益、补 Age、生成 Chronicle 事件
- [ ] 9.5 中途可被打扰（Disturb）：`Disturb++`，Disturb ≥ 3 → 强制提前出关（G5 defer）
- [ ] 9.6 闭关期间 `spar` 动作 no-op（闭关不许比武）
- [ ] 9.7 闭关锁 Breakthrough Phase（突破期不能闭关——已在突破中）
- [ ] 9.8 确定性：同 WorkUnits+同种子 → 同 Duration+同收益
- [ ] 9.9 off 模式不激活闭关

## Implementation Notes

**Scheduler 集成**：闭关角色设置 `character.NextActAt = world.Now + duration`。Scheduler min-heap 维持不变——闭关角色的 NextActAt 极远，自然不会成为堆顶。

**中途 Tick 跳过**：因为 NextActAt 是单点远未来值，Scheduler.PopMin() 在闭关期间不会弹出该角色。特殊处理：其他角色与闭关角色的互动（如拜访）→ 访问判断 `IsSecluded()` → Disturb++。

**Wake 检查**：World.Advance 主循环弹出角色时检查 `Flags["secluded"]` → 调用 `Seclusion.Exit()` 而非正常 Tick。

## Out of Scope

- 被打扰提前出关的完整实现（G5 defer，当前 Disturb 累积但不强制出关）
- Disturb 对收益的影响（→ story-011）

## Test Evidence Requirement

**Type**: Logic — automated unit tests. Enter/exit lifecycle, NextActAt timeline, Scheduler non-pop during seclusion, mid-duel no-op, off mode no-op.
