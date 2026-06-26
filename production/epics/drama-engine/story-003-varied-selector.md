# Story drama-003: VariedSelector<TKey> 共享原语（戏剧引擎先决依赖）

> **Epic**: drama-engine
> **Status**: Done（2026-06-26 /loop TDD：882 绿，+6 测试，IL 浮点零）
> **Layer**: Core（Jianghu.Util 共享原语）
> **Type**: Logic
> **Estimate**: 小 (0.3d)
> **Depends**: 无（Step 0 先决，dependency-free）
> **ADR**: adr-0001-integer-determinism（long 计数纯整数，注入 IRandom 确定性）
> **GDD**: `design/gdd/drama-system.md` §6 Dependencies + §9（drama-003 = spec Step 0）
> **真相源**: `docs/legacy-specs/specs/2026-06-13-v1.2-B-戏剧引擎-design.md` §5.5 / Step 0

## Context

戏剧 storylet 去重/破单调需"少用优先"抽取原语。经全仓 grep 复核：`VariedSelector` 源码**确不存在**，仅见于 spec。本 story 按 spec §5.5 形式化为共享原语，供后续 storylet 选择（drama-007）+ 任何需"均匀轮替"的场景复用。

**语义**（spec §5.5）：状态 = 每元素一个 `long` 使用计数；抽取时在**最小计数子集**内用注入 `IRandom` 均匀抽，抽后该元素计数 +1。保证长程均匀轮替（破单调），且确定（同种子同序列）。

## Acceptance Criteria

- [ ] D3.1 `VariedSelector<TKey>`：`Note(TKey)`（计数+1）、`Pick(IReadOnlyList<TKey> candidates, IRandom rng)`（最小计数子集内均匀抽 + 抽后 Note）、`UsageOf(TKey)` 查询、`Clone()` 深拷计数表。
- [ ] D3.2 纯整数确定性：计数 `long`；最小计数子集裁决序确定（按候选传入序，不依赖 Dictionary 枚举）；同种子 + 同候选 → 同抽取序列。
- [ ] D3.3 破单调性质：N 元素各抽 K 轮，每元素使用次数差 ≤ 1（最小计数集机制保证均匀）。
- [ ] D3.4 Clone 往返等价：`Clone()` 后两实例独立（改一个不影响另一个），计数表逐键相等。
- [ ] D3.5 边界：空候选 → 抛或返回 default（明确语义）；单候选 → 恒返它并 Note；未见过的 key 计数视为 0。
- [ ] D3.6 IL 浮点零（B.2）；clean rebuild 0 警告；既有 876 绿不退（新文件不被引用，不影响轨迹 → off 逐字节自然成立）。

## Implementation Notes

- 放 `src/Jianghu.Core/Util/VariedSelector.cs`（新建 Util 目录）或 `Jianghu.Random/`（仿 RandomExtensions 命名空间）——择 Util，语义非纯随机。
- 计数表 `Dictionary<TKey, long>`，但**裁决遍历最小计数子集时按候选 list 传入序**（确定性），Dictionary 仅存计数不参与序。
- 最小计数子集算法：先求候选中 min usage，收集所有 == min 的候选（保持传入序），`rng.NextInt(minSet.Count)` 抽一个。
- `Clone()`：`new VariedSelector` + 复制 `Dictionary`（值类型 long 浅拷即深拷）。
- 纯整数、无浮点、不消费除注入 rng 外的随机。

## Test Evidence

**Required (BLOCKING — Logic)**: `tests/Jianghu.Core.Tests/Util/VariedSelectorTests.cs`
- Note/UsageOf 计数正确；Pick 最小计数集内抽 + 抽后+1；破单调（各元素次数差≤1）；Clone 往返独立；空/单/未见 key 边界；同种子同序列。

## Out of Scope

- storylet 接入（drama-007 用它做 storylet 去重）。
- 多维度加权（本原语只做"少用优先"均匀轮替，加权抽取是 WeightedPicker = drama-006）。
