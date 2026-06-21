# ADR-0002: Module Factory Pattern for Combat Effect System

- **Status**: Accepted
- **Date**: 2026-06-21（逆向自红线 B.9 + Modules.cs / SpecialModuleRegistry 实现）
- **Deciders**: huangjiaqi13 + Claude (architecture-review)
- **Affects**: `Jianghu.Core.Cultivation` — `Modules`, `SpecialModuleRegistry`, `ModuleResolver`, `EffectOp`

---

## Context

战斗效果系统需支持 21 条修炼路径各自独特的战斗风格（剑仙物攻、毒修 dot、丹修改四维、命修改因果 等）。战斗效果包括：伤害修正（FlatPen/FlatDR）、资源消耗加成（PenFromResource）、反击（CounterMul）、吸血（Drain）、dot、控制、闪避/反射/招架等。

若允许任何 path 文件直接 `new EffectOp(7 params)`，会导致：
- 参数漏传（ratio/Amount2≥1/Trigger/Rarity 等 7 参极易漏）
- 重复造轮子（每条路自己写相同逻辑）
- 难查错（散在 21 个文件，排查需全局搜索）
- 新算子无约束（可能绕过平衡体系）

同时，部分效果是全路通用（普通/稀有档），部分是某路独有（唯一档，如夺舍/断链/爆阵）。

---

## Decision

**所有战斗效果必须经 `Modules` 静态工厂构造。唯一档经 `SpecialModuleRegistry` 注册式插件。**

具体：
1. **`Modules` 工厂**（`Cultivation/Modules.cs`）：单一构造入口，每类效果一个工厂方法，封 `ratio`/`Kind`/`Amount2≥1`/`Trigger`/`Rarity` 等易漏参。
   - 普通档：`Modules.FlatPen(...)`, `Modules.FlatDR(...)`
   - 稀有档：`Modules.PenFromResource(...)`, `Modules.CounterMul(...)`, `Modules.AoePerTarget(...)`, `Modules.Backlash(...)`, `Modules.Drain(...)`, `Modules.Dot(...)`, `Modules.Control(...)`, `Modules.Reflect(...)`, `Modules.Evade(...)`, `Modules.ModifyStat(...)`, `Modules.ModifyEP(...)`, `Modules.RelationAdjust(...)`
2. **`SpecialModuleRegistry`** 注册式插件：每条路的唯一档效果注册到 registry，由 `ModuleResolver` 按签名匹配调用。
   - 断链（BrokenChain）、夺舍（Duoshe）、夺心（Duoxin）、爆阵（ExplodeArray）、场地激活（FieldActive）、金身极限（GoldenBodyMax）、落宝（Luobao）、逆演栈（ReverseStack）
3. **新算子** = 加 1 工厂方法 + `ModuleResolver` 1 分支，不改既有积木。
4. **禁裸 `new EffectOp(七参)` 散造**（BannedApiAnalyzers 未覆盖此因为 EffectOp 是 internal 构造 → 靠 code review 守）。
5. **跨路平衡视图**：`BalanceMatrixDump` harness 派生矩阵，不靠集中源码。

---

## Consequences

### Positive
- 所有效果参数经过统一构造（不漏 ratio/Amount2/Trigger/Rarity）
- 新加效果类型只需 1 工厂方法 + 1 resolver 分支
- 21 条路文件不直接 new EffectOp → 代码干净、可搜索、可审计
- BalanceMatrixDump 可自动 dump 所有路 × 所有 UT 的 pe/效果矩阵

### Negative
- Modules 工厂是单点（很多方法），但这是设计意图（chokepoint 好过散落）
- 新效果类型需要理解 Modules 工厂模式（学习成本，但模式统一）

### Neutral
- SpecialModule 注册式插件模式与 Modules 工厂互补：全路通用 → 工厂；某路独有 → 注册

---

## References
- `src/Jianghu.Core/Cultivation/Modules.cs`
- `src/Jianghu.Core/Cultivation/SpecialModuleRegistry.cs`
- `src/Jianghu.Core/Cultivation/ModuleResolver.cs`
- `src/Jianghu.Core/Cultivation/EffectOp.cs`
- `src/Jianghu.Core/Cultivation/special/`（8 个 unique handler）
- `tests/.../Cultivation/ModulesTests.cs`
- `tests/.../Cultivation/ModuleResolverTests.cs`
- 红线 B.9 (CLAUDE.md §B.9)
