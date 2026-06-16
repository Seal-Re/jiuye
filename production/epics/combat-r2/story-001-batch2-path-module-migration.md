# Story 001: batch2 — 普通/稀有档全路迁（21 路招牌招 → Modules 模块）

> **Epic**: combat-r2
> **Status**: Done
> **Layer**: Core
> **Type**: Logic
> **Estimate**: 大（21 路，逐路 TDD）
> **Last Updated**: 2026-06-15

## Context

**GDD**: design/gdd/combat-system.md（P8 补）；深度源 docs/legacy-specs/specs/2026-06-14-v1.2-B5-模块化效果系统-design.md §10 覆盖账 + §15.9 per-path 映射。
**Requirement**: TR-combat-???（tr-registry P8 bootstrap）
**Governing ADR**: adr-0002-module-factory-effect-system（P8）

## Acceptance Criteria

逐路把招牌招 Note 机制改写为结构化 Modules 算子（差分测试：装备 vs 剥离结果不同 + 语义正确，非仅 ≠0，§10 第 1+2 条）：

- [x] 剑（PenFromResource(swordWill)/AoePerTarget/Backlash）`4d5cc26`
- [x] 体（PenFromResource(qixue,÷10)/ReflectDamage）`6f2f7bd`
- [x] 法（CounterMul(evil)/PenFromResource(spellBreadth)）`413434e`
- [x] 鬼（PenFromResource(shaCharge)/Control）`6fdb156`
- [x] 丹（PenFromResource(flameTier) 占位；改人造网 deferred FULLSTRUCT）`d3b73ea`
- [x] 器（PenFromResource(itemTier)/Drain；落宝→batch3 Special）`43a5873`
- [x] 阵（Control(困龙)；炸阵→batch3 Special、Σ阵→FULLSTRUCT defer）`065403b`
- [x] 魂（PenFromResource(soulForce)；夺舍→batch3 Special）`065403b`
- [x] 雷（CounterMul(evil) 灭阴/Backlash(引天劫承雷)）`065403b`
- [x] 血/妖/儒/驭兽/命/因果/毒蛊/符/傀儡/音/佛/魔（余 12 路）`da12104` — 329 绿
- [x] §10 覆盖账每路标"已结构化"（签名 Special/derived 显式 deferred，A.8）

## Implementation Notes

每路 TDD 仪式：①写差分测试 ②经 `Modules.*` 工厂改写招牌招（禁裸 new EffectOp，B.9）③测试绿 ④更新 §10 覆盖账 ⑤单路单提交。

## Test Evidence

`tests/Jianghu.Core.Tests/Cultivation/paths/*ModuleMigrationTests.cs`（已有 6 路 + ModulesTests）。当前 **282 绿 / 0 失败**（dotnet 8.0.422 主控独立核验）。IL 浮点零、off 逐字节守。

## Out of Scope

- 唯一档 Special（落宝/炸阵/夺舍…）→ story-002。
- DuelEngine 接线 → story-003。
