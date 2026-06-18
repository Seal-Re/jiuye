# Story 003: batch4 — DuelEngine.ResolveR2 接模块（核心引擎）

> **Epic**: combat-r2
> **Status**: Complete
> **Last Updated**: 2026-06-18
> **Layer**: Core
> **Type**: Integration
> **Estimate**: 大
> **Depends**: story-001, story-002

## Context

> ⚠️ **设计已升级**：本 story 原描述 R2 "3 回合同时结算"。**现以 `design/gdd/combat-system.md`（306fb64）为权威**——战斗升级为**速度序列(CTB) + 即时窗口模型 + 操作分类学**。batch4 实现据 GDD，下列验收条目作 R2 基线参考，speed/window 细节见 GDD。即时窗口数学放 Core(NPC 概率采样确定性)，玩家按键层留 Unity(后期)。

**深度源**: design/gdd/combat-system.md（权威）+ docs/legacy-specs/specs/...B5-R2战斗系统重设计-design.md §4 + §15 补丁。
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

## Completion Notes

**Completed**: 2026-06-18
**Criteria**: 7/7 passing
**Deviations**: ADVISORY — ADR adr-0002/0003 not yet created (P8补).
  OUT OF SCOPE — 21路 path Evade/Reflect/Control/Dot/CounterMul/Drain expansion (valid scope creep).
  ReflectDamage bug found+fixed in code-review (02b86af→561df1d).
**Test Evidence**: Integration — tests/.../DuelEngineTests.cs (16) + SparCultivationTests (2 adapted). 366 green.
**Code Review**: Complete — /code-review passed, 1 bug fixed, 3 suggestions addressed.
**Commits**: 02b86af (initial), 561df1d (ReflectDamage fix), d080fd4 (TickDots+Dot/Control+Evade+Drain), 875c681 (21路扩)。
