# Story drama-012: 跨代恩怨继承（父债子偿）— CharacterDied → 子嗣/弟子继承 → 点燃下一弧

> **Epic**: drama-engine
> **Status**: Done（2026-06-26 /loop TDD：1039 绿 [+11]，0 警告，跨代链端到端实证）
> **Layer**: Core（`Jianghu.Drama` + `Jianghu.Sim.World`）
> **Type**: Logic + 集成
> **Estimate**: 中 (0.6d)
> **Depends**: drama-005（GrudgeLedger.Form 合并/继承字段）、drama-009b（DramaDirector）、drama-010（World 接线/IDramaMutator）、drama-004（DramaProfile 侧表）
> **ADR**: adr-0001-integer-determinism（衰减整数 + 继承人确定性排序）、adr-0003-cultivation-off-byte-identical
> **GDD**: `design/gdd/drama-system.md` §3.3(跨代继承) + §9（drama-012 = spec Step 10）

## Context

drama-001~011 已让单代复仇弧完整涌现并驱动行为。drama-012 是 walking-skeleton 的「深」——**父债子偿、冤冤相报的跨代恩怨链**（验收核心 AC-7）：复仇者**寿尽**且恩怨未了 → 在世子嗣/弟子继承一条 `Grudge(Cause=Inherited)` 指向原仇人 → 点燃下一条复仇弧 → 跨代链。这是「江湖史官读编年史看宿命流转」爽感的兑现。

**继承机制**（spec §3.3）：
- **触发**：`CharacterDied`（Lifecycle Age≥Lifespan）且死者有未了强恩怨（Intensity≥阈值）。
- **继承人**：经 `DramaProfile` 侧表（`Self→Master?/Bloodline?`）找**在世**子嗣/弟子，三级确定性排序 **年龄→武力→Id**（取首位）。
- **衰减**：`childIntensity = parentIntensity × InheritDecayPct/100`（整数衰减，单调不增）。
- **封顶**：`Generation` 达 `MaxGeneration=3` 不再继承（防无限链）。
- **绝嗣绝门**：无在世继承人 → 弧 Settled，无继承（恩怨随风消散）。

> **红线约束**：B.2 整数确定性（衰减 `x*pct/100` 整数 + 继承人三级排序确定，禁 Dictionary 枚举序选继承人）；B.3 off 逐字节（继承仅 drama 路径，off=_drama null 不可达）；DomainEvent 单源（`GrudgeInherited` 经 Emit）；继承经 GrudgeLedger.Form chokepoint（合并幂等）；R-NF2 profiles 进 Clone。

## Acceptance Criteria

- [x] **D12.1 DramaProfile 注册**：DramaDirector 持 `_profiles` + `RegisterProfile` + `ProfileOf`。Self→Master?/Bloodline? 侧表，不侵入 Character。
- [x] **D12.2 OnDeath 继承触发**：`OnDeath(deceased, livingHeirsSorted, clock, mutator)`——死者每条未了强恩怨 → 取首位继承人 → `Form(Inherited, gen+1)` + `Emit GrudgeInherited`。
- [x] **D12.3 整数衰减**：`decayed = intensity × InheritDecayPct / 100`（整数除）；decayed<1 不继承；单调不增。
- [x] **D12.4 MaxGeneration 封顶**：Generation≥MaxGeneration 不继承；继承产物 Generation=parent+1。
- [x] **D12.5 绝嗣绝门**：livingHeirsSorted 空 → 不继承（恩怨随死者消散）。
- [x] **D12.6 继承人确定性选取**：World 提供 livingHeirsSorted（年龄降→武力降→Id 升）；Director 取 [0]。
- [x] **D12.7 World 接线**：`RemoveDead` drama-on → `BuildLivingHeirs`（profiles Master/Bloodline==死者 的在世者排序）→ `_drama.OnDeath`。off 不可达。
- [x] **D12.8 跨代链涌现（AC-7）**：cultivation-on 多种子端到端长跑 → Chronicle 出现「继承」行（父债子偿），证 World 接线端到端连通。
- [x] **D12.9 Clone 含 profiles**：`_profiles` 进 Director Clone；World drama-on Clone 续跑逐字节。
- [x] **D12.10 ⚠️ off 逐字节 + 确定性 + Clone 续跑 + 全量绿 + 0 警告 + IL 浮点零**。

**机器证据（2026-06-26 /loop）**：全量 **1039 绿** / 0 失败 / 0 skip（1028 → +11）；clean rebuild 0 警告；determinism+off+DramaWiring+floatscan 共 36 focus 绿。

### 范围/实现确认
- profiles 来源：测试经 `RegisterProfile`/`World.RegisterDramaProfile` 注入师徒链。**WorldFactory 预置师徒边 + 冤孽 fixture 延 drama-013**（A.8 显式）。
- 跨代链端到端测试：依赖弟子比师父长寿的自然随机时序 → 跨种子搜索证**可**涌现（单代弧 + 继承机制本身已由单测穷尽覆盖）。承记忆 [[drama-offmode-power-cap]]：完整弧需 cultivation-on。

## Implementation Notes

- **DramaDirector**：
  - `_profiles` Dictionary<long, DramaProfile> + `RegisterProfile`。进 Clone（profile record 不可变，浅拷即深拷）。
  - `OnDeath(deceased, livingHeirsSorted, clock, mutator)`：遍历 `_ledger.ByHolder(deceased)`，对每条 `Intensity≥GrudgeIgniteThreshold && Generation<MaxGeneration` 的恩怨：
    - `decayed = g.Intensity * InheritDecayPct / 100`；`if (decayed < 1) continue;`（衰减殆尽不继承）。
    - `if (livingHeirsSorted.Count==0) continue;`（绝嗣）。
    - `heir = livingHeirsSorted[0]`；`ledger.Form(heir, g.Target, GrudgeKind, decayed, clock, GrudgeCause.Inherited, g.Generation+1, g.Id, GrudgeCap)`。
    - `mutator.Emit(new GrudgeInherited(clock, newGrudgeId, heir, g.Target, g.Id, g.Generation+1, decayed))`。
  - 继承的恩怨随后正常进点火相（drama-009b）→ 跨代弧点燃。
- **World**：`RemoveDead(c)` 末尾，若 `_drama != null`：构造 `livingHeirs`——扫 `_drama` 的 profiles（经新 `DramaProfile? ProfileOf(id)` 或让 World 持 profiles？）。**决策**：profiles 由 World 持有更自然（World 知道谁活着 + 师徒关系），但 spec 把 profiles 归 drama 侧表。**取折中**：Director 持 profiles（drama 态，进 drama Clone），World 经新 `IDramaView` 方法或直接查 `_drama` 暴露的 `HeirsOf(deceased, livingFilter)` 拿候选。**简化**：给 DramaDirector 加 ` collectHeirs` 逻辑——World 调 `_drama.OnDeath(deceased, BuildLivingHeirs(deceased), clock, this)`，`BuildLivingHeirs` 在 World（知 _alive + 经 _drama.ProfileOf 查师徒）。
  - 加 `DramaDirector.ProfileOf(CharacterId)` 只读查 + World `BuildLivingHeirs(deceased)`：扫 _alive 找 profile.Master==deceased || profile.Bloodline==deceased 的在世者，按 年龄→武力→Id 排序。
- **本 story profiles 来源**：测试经 `RegisterProfile` 直接注入师徒链。**WorldFactory 预置师徒边 + 预置冤孽 fixture 延 drama-013**（本 story 证机制；013 做端到端可观测 fixture）。**显式记 A.8**。
- **off 安全**：OnDeath 仅 World drama-on 路径调；off=_drama null → RemoveDead 不调 → 继承不可达。

## Test Evidence

**Required (BLOCKING — Logic + 确定性)**:
- `tests/Jianghu.Core.Tests/Drama/DramaInheritanceTests.cs` —— D12.2~D12.6：OnDeath 弟子继承衰减恩怨（intensity×decay/100, gen+1, Cause=Inherited）+ GrudgeInherited emitted；gen==Max 不继承；衰减殆尽(decayed<1)不继承；绝嗣(空 heirs)不继承；衰减单调不增；继承人确定性取首位。
- `tests/Jianghu.Core.Tests/Drama/DramaInheritanceWorldTests.cs` —— D12.8：cultivation-on 预置强恩怨 + RegisterProfile 师徒链长跑 → Chronicle 出现跨代链（ArcIgnited→GrudgeInherited→第二 ArcIgnited）；Clone 含 profiles 续跑一致。
- ⚠️ 既有 determinism/off/DramaWiring/DramaCoupling 全绿。

## Out of Scope

- WorldFactory 预置师徒边 + 预置冤孽 fixture（drama-013，端到端可观测）。
- Showdown 超时强制结算（drama-013）。
- INV-CHAIN 完整端到端验收 + INV-PERF/INV-NO-DEADLOCK（drama-013）。
