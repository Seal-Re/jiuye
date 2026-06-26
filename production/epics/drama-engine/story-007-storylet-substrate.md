# Story drama-007: storylet 声明式 schema + IDramaView 读seam + DramaContext + StoryletSelector

> **Epic**: drama-engine
> **Status**: Done（2026-06-26 /loop TDD：957 绿 [+31]，0 警告，IL 浮点零）
> **Layer**: Core（`Jianghu.Drama`）
> **Type**: Logic
> **Estimate**: 中 (0.5d)
> **Depends**: drama-004（Predicate/Effect/枚举 值类型，done）、drama-005（GrudgeLedger，done）、drama-006（WeightedPicker，done）
> **ADR**: adr-0001-integer-determinism（谓词整数比较、选择器整数轮盘 + 确定性排序）
> **GDD**: `design/gdd/drama-system.md` §3.4(候选+加权) + §4(裁决序) + §9（drama-007 = spec Step 5，**本 story = 5-of-2 拆分前半：substrate**）

## Context（drama-007 拆分说明）

spec Step 5 = 「storylet schema + RevengeArc 5 态机 + FindIgnitions」三件套。为守 TDD 紧回合 + WIP 纪律，**显式拆两 story**（非静默 descope，A.8）：

- **drama-007（本 story）= storylet substrate**：声明式 schema + `IDramaView` 只读 seam + `DramaContext`（`Resolve`/`Eval`/`AllPass` 谓词解析）+ `StoryletSelector`（过滤→排序→WeightedPicker 抽取）。**纯数据 + 整数比较 + 确定性选择，可 mock，零 World/Clone/确定性风险**（不接 World，既有黄金轨迹平凡不变）。
- **drama-007b（下一 story）= 状态机 + 点火**：`RevengeArc.TryAdvance` 5 态机 + `IDramaMutator` 写 seam + `FindIgnitions`（只扫 AboveIntensity）。建立在本 substrate 之上。

二者皆归 GDD §9 drama-007 行 / spec Step 5。

> **红线约束**：B.2 整数确定性（`DramaVar` Resolve 全返 int，谓词整数比较，选择器前缀和轮盘 + `Sort(Weight desc,Id asc,...)`，禁 Dictionary/HashSet 枚举序参与裁决）；B.3 off 逐字节（纯加，无 World 接线，`IDramaView` 仅在 Pump 路径被调用 → off 不触达）；RuleBrain 零改（本 story 不碰决策，仅备 read seam）。

## Acceptance Criteria

- [x] **D7.1 storylet schema**：`StoryletSpec(Id, Arc, Stage, BaseWeight, OncePerArc, CooldownTicks, CooldownScope, Preconditions, Effects, ChronicleTemplate)`（纯不可变 record；复用 drama-004 的 `Predicate`/`Effect`）；`enum CooldownScope { Global, PerActor, PerPair, PerSect }`。`ChronicleTemplate` 仅渲染层（绝不进数值路径，B 红线）。
- [x] **D7.2 IStoryletSource + CodeStoryletSource**：`IStoryletSource { IReadOnlyList<StoryletSpec> All }` 接口（预留 JSON 适配于 CLI 层，核库只吃接口保零 IO）；`CodeStoryletSource`（C# `static readonly` 常量池实现）。
- [x] **D7.3 IDramaView 只读 seam**：`int Power(CharacterId)`（=Force×2+Internal+Constitution 同 SparAction/RuleBrain.SelfPower）/ `int Affinity(from,to)` / `bool IsAlive(CharacterId)` / `bool SameNode(a,b)`。**只读**，drama-010 由 World 实现，本 story 测试用 mock。
- [x] **D7.4 DramaContext.Resolve**：`Resolve(RoleRef subject, DramaVar var, ctx) -> int` 唯一映射（仿 World.BuildContext 纯映射）。`Power→view.Power(role)` / `Affinity→view.Affinity(self_role, other_role)` / `GrudgeIntensity→grudge.Intensity` / `SameNode→view.SameNode?1:0` / `TargetAlive→view.IsAlive?1:0`。role 经弧的 (Holder/Avenger, Target) 解析。
- [x] **D7.5 谓词求值**：`Eval(Predicate, ctx) -> bool` 按 `CmpOp`（Ge/Le/Eq/Gt/Lt）整数比较 `Resolve(...) op Threshold`；`AllPass(IReadOnlyList<Predicate>, ctx)` 全 AND（**空表→true**；任一 false→false 短路）。
- [x] **D7.6 StoryletSelector 选择**：`Select(IReadOnlyList<StoryletSpec> pool, arc, ctx, rng) -> StoryletSpec?`：① 过滤 `Arc==arc.Kind && Stage==arc.Stage && AllPass(preconditions)`；② 候选**排序** `(BaseWeight desc, Id asc)`（确定性，不依赖 pool 原序中的 Dictionary）；③ 权重 `max(1, BaseWeight)` 兜底（w≥1 防全零）；④ `WeightedPicker.PickIndex` 抽取；⑤ 空候选→`null`（no-op，不消费 rng）。
- [x] **D7.7 确定性**：同 pool + 同 ctx + 同 rng 状态 → 同 StoryletSpec；过滤/排序不依赖任何 Dictionary/HashSet 枚举序。
- [x] **D7.8 IL 浮点零 + 既有 926 绿不退**：`Jianghu.Drama` 浮点扫描仍零（drama-006 已建 DramaFloatScanTests，本 story 新增类型自动纳入）；clean rebuild 0 警告；off 逐字节（无 World 接线）。

## Implementation Notes

- **RoleRef 解析**（§D7.4）：弧有 (Avenger=Holder, Target)。`Resolve(Holder/Self, …)` → Avenger；`Resolve(Target, …)` → Target。`Affinity` 取 subject→对方（Holder 视角看 Target，或反之按 Subject）。`Eval` 的 Subject 决定 Power/Affinity 的「我方」。
- **DramaContext** 持 `IDramaView view` + `ArcInstance arc` + `Grudge? grudge`（点火候选评估时账本恩怨；推进时可空）。Resolve 是纯函数，无副作用。
- **GrudgeIntensity** Resolve：从 ctx.grudge 取（点火相）；若无 grudge 上下文（纯推进相谓词不应引用 GrudgeIntensity）→ 返 0（保守）。
- **StoryletSelector**：filter 后 `List.Sort` 三级比较器；权重列表 `weights[i]=max(1,spec.BaseWeight)`；`WeightedPicker.PickIndex(weights, rng)` 得 idx → 返候选[idx]。空候选**提前返 null**（不建 weights、不调 rng → no-op 守恒）。
- 全部 `Jianghu.Drama` 命名空间，纯整数。**本 story 不实现 cooldown 实际计时**（CooldownScope 仅 schema 字段，计时在 drama-009 Pump）。

## Test Evidence

**Required (BLOCKING — Logic)**:
- `tests/Jianghu.Core.Tests/Drama/DramaContextTests.cs` —— D7.4/D7.5：mock IDramaView（预置 Power/Affinity/Alive/SameNode）→ Resolve 各 DramaVar 返预期 int；Eval 各 CmpOp 边界（Ge/Le/Eq/Gt/Lt）；AllPass 全 AND（空→true、一 false→false）。
- `tests/Jianghu.Core.Tests/Drama/StoryletSelectorTests.cs` —— D7.6/D7.7：过滤（错 Arc/Stage/谓词不过 → 排除）；确定性排序（BaseWeight desc, Id asc）；FixedIntRandom 验权重抽取命中；空候选→null 且 rng 未消费；w<1 兜底为 1（BaseWeight=0 仍可被选）；同 rng 状态两跑同结果。
- `tests/Jianghu.Core.Tests/Drama/StoryletSchemaTests.cs` —— D7.1/D7.2：StoryletSpec record 值相等；CodeStoryletSource.All 返回稳定序；CooldownScope 枚举。

## Out of Scope

- `RevengeArc.TryAdvance` 5 态机 + 非法转移抛（drama-007b）。
- `IDramaMutator` 写 seam（Goal 覆写 / 镜像 Relations / Form grudge）（drama-007b/011）。
- `FindIgnitions`（只扫 AboveIntensity 收候选）（drama-007b）。
- cooldown 实际计时 / DramaScheduler / Pump（drama-009）。
- World 接线 / Clone（drama-010）。
- 6 DomainEvent + Project（drama-008）。
