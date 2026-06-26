# Story drama-005: GrudgeLedger 恩怨账本（List 主存 + 索引 + 合并幂等 + Clone）

> **Epic**: drama-engine
> **Status**: Done（2026-06-26 /loop TDD：896 绿，+7 测试，IL 浮点零，0 警告）
> **Layer**: Core（`Jianghu.Drama`）
> **Type**: Logic
> **Estimate**: 中 (0.5d)
> **Depends**: drama-004（Grudge 值类型，done）
> **ADR**: adr-0001-integer-determinism（钳制/合并全整数，确定性排序）
> **GDD**: `design/gdd/drama-system.md` §3.1 + §9（drama-005 = spec Step 2）

## Context

恩怨账本——独立有向恩怨真相源（与 Relations 并存，不复用负 affinity）。`List<Grudge>` 主存（确定性迭代）+ `Dictionary<long,List<int>> _byHolder`（查询加速，**不参与裁决顺序**）。是复仇弧点火的数据源（drama-007 FindIgnitions 只扫 AboveIntensity）。

## Acceptance Criteria

- [ ] D5.1 `Form(holder, target, kind, intensity, originTick, cause, gen, inheritedFrom?)`：新增或**合并幂等**——同 (holder,target) 取 `Kind=max(严重度), Intensity=max, Generation=min, OriginTick=首次（保留最早）`，返回 GrudgeId。
- [ ] D5.2 Intensity 钳制 [0, GrudgeCap]（入账 chokepoint，调用方传任意值都钳）。
- [ ] D5.3 `AboveIntensity(threshold)`：返回 Intensity ≥ threshold 的恩怨，**稳定排序 (Intensity desc, OriginTick asc, Id asc)**（确定性，不依赖 Dictionary 枚举）。
- [ ] D5.4 查询：`Get(holder, target)`、`ByHolder(holder)`、`Count`、`All`（确定性序）。
- [ ] D5.5 `Adjust(holder, target, delta)`：调强度（钳制），不存在则 no-op 返 false。
- [ ] D5.6 `Clone()`：深拷 `_grudges` + `_byHolder` + `_nextId`，独立实例（R-NF2 续跑安全）。
- [ ] D5.7 纯整数确定性；IL 浮点零；既有 889 绿不变（未接线 World）。

## Implementation Notes

- `List<Grudge> _grudges`（主存，迭代序 = 插入序但裁决用显式 Sort）；`Dictionary<long, List<int>> _byHolder`（holder.Value → _grudges 索引列表，仅加速）；`long _nextId`。
- 合并：先查 (holder,target) 是否存在 → 存在则按 D5.1 规则更新该条（OriginTick 保留旧、Gen 取 min、Kind/Intensity 取 max）；不存在则 append + 更新索引。
- AboveIntensity：filter + `List.Sort` 三级比较器（Intensity desc → OriginTick asc → Id.Value asc）。**不可用 Dictionary 枚举序**。
- Clone：new + 复制 List（Grudge 是 record 不可变，浅拷元素即可）+ 重建/复制索引 + _nextId。
- 钳制 `Math.Clamp(intensity, 0, cap)`，cap 默认参数（GrudgeCap，drama-006 进 LimitsConfig，此处先常量参数）。

## Test Evidence

**Required (BLOCKING — Logic)**: `tests/Jianghu.Core.Tests/Drama/GrudgeLedgerTests.cs`
- Form 新增/合并幂等（Kind=max/Intensity=max/Gen=min/OriginTick首次）；钳制 [0,Cap]；AboveIntensity 阈值+稳定排序；ByHolder 索引；Adjust 钳制+不存在no-op；Clone 往返独立。

## Out of Scope

- 点火/弧（drama-007）；镜像负 Relations（drama-011）；World 接线（drama-010）。
- GrudgeCap 进 LimitsConfig（drama-006）——本 story 用参数默认值。
