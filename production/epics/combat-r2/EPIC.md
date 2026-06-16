# Epic: 战斗系统 R2 + 平衡（模块化效果系统）

**Layer**: Core
**Status**: In Progress
**GDD**: design/gdd/combat-system.md ✅（已建，306fb64：操作分类学+速度序列+即时窗口模型）；深度源 docs/legacy-specs/specs/2026-06-14-v1.2-B5-R2战斗系统重设计-design.md + ...模块化效果系统-design.md
**Architecture Module**: Jianghu.Core/Cultivation（EffectOp/ModuleResolver/Modules/CombatContext/SpecialModuleRegistry）
**Governing ADRs**: adr-0002-module-factory-effect-system（P8 补）、adr-0001-integer-determinism、adr-0003-cultivation-off-byte-identical
**Engine Risk**: LOW（.NET 8 纯整数确定性，无引擎 API 风险）
**Created**: 2026-06-15（迁自 TASKS.md EPIC-COMBAT-R2）

## Summary

把 21 路修炼的招牌招/功法战斗效果从"埋在 Note 的散文机制 + 占位 AddPenInteger"升为**结构化模块算子**，经 `Modules` 工厂（普通/稀有档）+ `SpecialModuleRegistry`（唯一档）积木化组合（红线 B.9）。三档（普通 4 / 稀有 9 参数化 Kind / 唯一 Special）+ trigger(OnUse/OnDefend/Passive) + 功法门控防御 + 法宝配套，纯整数无 RNG。最终接 DuelEngine.ResolveR2（off 不动）+ 平衡硬化 gate。

## Scope

**Systems covered**: 战斗模块系统、21 路招式迁移、DuelEngine R2、法宝战斗效果、平衡硬化。
**Requirements covered**: TR-combat-*（P8 tr-registry bootstrap 后回填）。
**Architecture module**: Jianghu.Core/Cultivation + Actions/DuelEngine。

## Stories

- story-001 batch2 普通/稀有档全路迁（21 路招牌招→模块） — **In Progress**（6/21：剑/体/法/鬼/丹/器 done，282 绿）
- story-002 batch3 唯一档 Special handler（落宝/炸阵/夺舍/金身态/律场总门） — Not Started
- story-003 batch4 DuelEngine.ResolveR2 接模块（off 逐字节不动） — Not Started
- story-004 batch5 法宝配套战斗效果 — Not Started
- story-005 batch6 硬化 DoD gate + 辅助路 UT 重锚（解 A1.4） — Not Started

> batch1 框架（EffectOp 扩字段 + 9 Kind + ModuleResolver + CombatContext + SpecialModuleRegistry + IL 扫描）= **completed**，sha `946ea75`，255→282 绿。Modules 工厂 `3e5b761`（红线 B.9 落地）。

## Dependencies

**Unblocked by**: A.1 已合（无前置阻塞）。
**Blocks**: combat-fullstruct（真全量结构化，依赖本 epic 架构侧 done）；cultivation-a1-rest 的 A1.4（辅助路 UT 重锚并入本 epic story-005）。

## Definition of Done（§13 硬化 DoD，迁自 TASKS.md）

- [ ] §13 硬化 DoD 全过（防 MVP 蒙混，每 gate 可测断言）
- [ ] 同 UT ≥2 战斗路 1v1 胜率 ∈ [40,60]% gate
- [ ] 辅助路 UT 锚战斗当量（解 A1.4）
- [ ] 全量绿 + off 逐字节 + ON 路逐字节
- [ ] IL 浮点零
- [ ] auditor 终验

## Notes

- 红线约束：B.2 整数确定性、B.3 off 逐字节、B.9 模块化工厂、A.3 证据门。
- A1.4（辅助路 UT 战斗当量重锚）原 blocked，依赖 balance-cross；其 UT 重锚动作并入本 epic story-005，不静默丢（A.8）。
