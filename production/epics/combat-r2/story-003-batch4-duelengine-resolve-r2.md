# Story 003: batch4 — DuelEngine.ResolveR2 接模块（核心引擎）

> **Epic**: combat-r2
> **Status**: Not Started
> **Layer**: Core
> **Type**: Integration
> **Estimate**: 大
> **Depends**: story-001, story-002

## Context

**深度源**: docs/superpowers/specs/...B5-R2战斗系统重设计-design.md §4 回合循环 + §15 补丁；impl plan 批4。
**Governing ADR**: adr-0002 + adr-0003-cultivation-off-byte-identical（off 不动）。

## Acceptance Criteria

- [ ] 4.1 HP=pe；选招（tier≤realm & TryPayCost & gate）；Passive 算基线
- [ ] 4.2 每回合 攻方 OnUse → 防方 OnDefend（护体/Evade 连续 §15.2 / Reflect 读扣血前不递归 §15.5）→ 软情境×联合上界 §15.4 → 同时扣（读 pre-HP）
- [ ] 4.3 dot/control 回合间结算
- [ ] 4.4 越级简版：UT 差≥2 高 UT 直判胜
- [ ] 4.5 功法门控防御：无功法→能力 0（M5）
- [ ] 4.6 胜者 HP 高；平 Tiebreak(CharacterId)；接 SparAction ON 分支
- [ ] 4.7 **off 逐字节守**（off 无 Cultivation 早返 legacy，不入 ResolveR2）

## Implementation Notes

每步 TDD（含 §13 M6 镜像对称/无先手偏置）。**红线 B.3：off 路径一字节不能变**——SparAction off 分支不碰。

## Out of Scope

法宝 → story-004；硬化 gate → story-005。
