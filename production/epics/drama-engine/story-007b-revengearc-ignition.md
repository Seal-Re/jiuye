# Story drama-007b: RevengeArc 5 态机（纯转移）+ IgnitionScanner.FindIgnitions

> **Epic**: drama-engine
> **Status**: Done（2026-06-26 /loop TDD：979 绿 [+22]，0 警告，IL 浮点零）
> **Layer**: Core（`Jianghu.Drama`）
> **Type**: Logic
> **Estimate**: 中 (0.5d)
> **Depends**: drama-005（GrudgeLedger.AboveIntensity，done）、drama-006（LimitsConfig 戏剧上限，done）、drama-007（IDramaView/ArcInstance/枚举，done）
> **ADR**: adr-0001-integer-determinism（状态门控全整数比较、候选权重整数、确定性排序）
> **GDD**: `design/gdd/drama-system.md` §3.2(5 态机) + §3.4C(候选收集) + §4(战力公式/裁决序) + §9（drama-007b = spec Step 5b）

## Context

承 drama-007 拆分后半（spec Step 5 三件套之状态机 + 点火扫描）：

- **`RevengeArc.TryAdvance`**：复仇弧 5 态**纯转移函数**——给定弧 + `IDramaView` 世界态 + `LimitsConfig` 门控，返回下一弧 + 转移结果。**纯函数无副作用**：不消费 rng、不产事件、不 mutate（事件 drama-008，Goal/Relations 耦合 drama-011，escape 掷骰/Showdown 超时 drama-009 Pump 层叠加）。这保证状态转移逻辑本身**完全确定可单测**。
- **`IgnitionScanner.FindIgnitions`**：点火候选收集——**只扫 `ledger.AboveIntensity(threshold)` 强恩怨**（O(强恩怨数) 非 O(全员)，INV-PERF），过滤「持有者已有活跃弧 / 持有者或仇人已亡 / 对子冷却中」，产 `IgnitionCandidate(Grudge, Weight)` 并按 (Weight desc, Grudge.Id asc) 确定性排序，供上层 `WeightedPicker` 抽取。

> **红线约束**：B.2 整数确定性（门控整数比较、战力 `Force×2+Internal+Constitution`、权重整数、`List.Sort` 确定序，禁 Dictionary/HashSet 枚举序裁决）；B.3 off 逐字节（纯加，无 World 接线）；RuleBrain 零改（TryAdvance 不碰决策，Goal/Relations 写入延到 drama-011 经 IDramaMutator）。

## Acceptance Criteria

- [x] **D7b.1 转移结果类型**：`enum ArcResolution { Advanced, Stalled, Completed, Abandoned }`；`record ArcTransition(ArcInstance Next, ArcResolution Resolution, bool AvengerPrevailed)`。`AvengerPrevailed` 仅 `Completed`（Showdown 结算）时有意义。
- [x] **D7b.2 Victimized→BuildUp**：非终态推进；进入 BuildUp 时记 `BuildUpBasePower = view.Power(Avenger)`（=Force×2+Internal+Constitution）；Resolution=Advanced，Next.Stage=BuildUp。
- [x] **D7b.3 BuildUp→Hunting 门控**：`view.Power(Avenger) ≥ arc.BuildUpBasePower + limits.GrowthNeeded` **且** 仇人在世 → Advanced 到 Hunting；门控不满足（战力没涨够）→ Resolution=Stalled，Next.Stage 仍 BuildUp（待下次唤醒重试）。
- [x] **D7b.4 Hunting→Showdown 门控**：`view.SameNode(Avenger, Target)` 且仇人在世 → Advanced 到 Showdown；未同节点 → Stalled，仍 Hunting。（超时强制结算属 Pump 层 drama-009，本 story 门控仅「同节点」。）
- [x] **D7b.5 Showdown→Resolved 结算**：复用战力比较 `view.Power(Avenger) ≥ view.Power(Target)` → `AvengerPrevailed`；Resolution=Completed，Next.Stage=Resolved，Next.Completed=true。（非致死：关系崩坏/恩怨化解属 Effect，drama-008/011。）
- [x] **D7b.6 死亡→Abandoned**：任一非终态下仇人**或**复仇者已亡（`!view.IsAlive`）→ Resolution=Abandoned，Next.Stage=Abandoned，Next.Completed=true。死亡检查优先于阶段门控。
- [x] **D7b.7 非法转移抛**：对终态弧（Stage∈{Resolved,Abandoned} 或 Completed=true）调 TryAdvance → 抛 `InvalidOperationException`（弧已退场不可推进）。
- [x] **D7b.8 FindIgnitions 只扫强恩怨**：`FindIgnitions(ledger, threshold, view, hasActiveArc, onPairCooldown)` 只遍历 `ledger.AboveIntensity(threshold)`；过滤 `hasActiveArc(holder) || !IsAlive(holder) || !IsAlive(target) || onPairCooldown(holder,target)`；INV-PERF：300 角色仅 2 强恩怨时，候选遍历=O(2) 非 O(300)（计数 view 验存活检查 ≤4 次）。
- [x] **D7b.9 候选权重 + 确定性排序**：`IgnitionCandidate(Grudge, Weight)`，`Weight = max(1, g.Intensity)`（w≥1 兜底，B.2）；返回列表按 (Weight desc, Grudge.Id.Value asc) 排序（确定，不依赖账本枚举序）。
- [x] **D7b.10 IL 浮点零 + 既有 957 绿不退**：`Jianghu.Drama` 浮点扫描仍零；clean rebuild 0 警告；off 逐字节（无 World 接线）。

**机器证据（2026-06-26 /loop）**：全量 **979 绿** / 0 失败 / 0 skip（957 → +22）；clean rebuild 0 警告；红线 focus 29 绿（off 逐字节 + 浮点扫描 + RevengeArc + IgnitionScanner）。

## Implementation Notes

- **TryAdvance 结构**（`RevengeArc` static）：
  1. 终态守卫：`arc.Completed || Stage∈{Resolved,Abandoned}` → throw（D7b.7）。
  2. 死亡守卫：`!IsAlive(Target) || !IsAlive(Avenger)` → Abandoned（D7b.6，先于门控）。
  3. switch(Stage)：Victimized→记 BasePower 进 BuildUp；BuildUp→门控进 Hunting 或 Stalled；Hunting→同节点进 Showdown 或 Stalled；Showdown→战力比较结算 Resolved（Completed）。
  - 纯函数：所有判定读 `view`/`arc`/`limits`，`with` 产新 ArcInstance（record 非破坏式），不改入参。
- **战力**：`view.Power(id)` 已封装 `Force×2+Internal+Constitution`（drama-007 IDramaView 契约），TryAdvance 不重算公式。
- **FindIgnitions**（`IgnitionScanner` static）：注入 `Func<CharacterId,bool> hasActiveArc` + `Func<CharacterId,CharacterId,bool> onPairCooldown`（活跃弧集 + 冷却表在 Pump/Director drama-009 持有，本 story 只吃谓词，保解耦可测）。遍历 AboveIntensity → 过滤 → 收候选 → `List.Sort((Weight desc, Grudge.Id asc))`。HashSet/Dictionary 仅可用于**成员测试**（hasActiveArc），不得枚举其序参与裁决。
- **延后项（显式，非静默）**：escape 掷骰（EscapeRatioPct + arcRng）、Showdown 超时强制结算（tick 比较）→ drama-009 Pump；ArchMul 原型乘子 → 后续调参；ArcAbandoned reason 细分 → drama-008 事件。

## Test Evidence

**Required (BLOCKING — Logic)**:
- `tests/Jianghu.Core.Tests/Drama/RevengeArcTests.cs` —— D7b.2~D7b.7：mock IDramaView 逐转移（Victimized→BuildUp 记 BasePower；BuildUp 战力够/不够→Advanced/Stalled；Hunting 同节点/异节点→Advanced/Stalled；Showdown 胜/负→Completed+AvengerPrevailed；仇人亡/复仇者亡→Abandoned；终态弧→抛）；纯函数确定性（同入参同出）。
- `tests/Jianghu.Core.Tests/Drama/IgnitionScannerTests.cs` —— D7b.8/D7b.9：只扫 AboveIntensity（计数 view 验 O(强恩怨)）；过滤活跃弧/死亡/冷却；weight=max(1,intensity)；(Weight desc, Id asc) 确定性排序；阈值下无候选→空表。

## Out of Scope

- DramaScheduler 最小堆 + Pump 推进/点火相 + escape 掷骰 + Showdown 超时（drama-009）。
- 6 DomainEvent + Project/Chronicle（drama-008）。
- World 接线 / Clone（drama-010）；Goal 覆写 / 镜像 Relations 写入（drama-011）。
- 跨代继承（drama-012）。
