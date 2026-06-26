# Story drama-013: INV-CHAIN 端到端验收（收官）— 预置冤孽 fixture + Showdown 超时 + 不变量

> **Epic**: drama-engine（**收官 story**）
> **Status**: Done（2026-06-26 /loop TDD：1051 绿 [+12]，0 警告，INV-CHAIN 端到端实证，epic 收官）
> **Layer**: Core（`Jianghu.Sim.WorldFactory` + `Jianghu.Drama.DramaDirector`）
> **Type**: Logic + 集成 + 验收
> **Estimate**: 中 (0.6d)
> **Depends**: drama-003~012（全 done——完整引擎 + World 接线 + 耦合 + 继承）
> **ADR**: adr-0001-integer-determinism、adr-0003-cultivation-off-byte-identical
> **GDD**: `design/gdd/drama-system.md` §5(预置冤孽) + §3.2(Showdown 死锁兜底) + §8(AC-1~10) + §9（drama-013 = spec Step 11）

## Context（收官——闭 epic AC-1~10）

drama-003~012 建成完整复仇引擎并接进 World。drama-013 收官：补三块验收件 + 闭全部 AC，让 drama-engine epic Done。

- **① 预置冤孽 fixture**（spec §5）：`WorldFactory` seed 控制下预置 1~2 对强恩怨 + 师徒边，**用 genRng 独立子流**（不碰 domain/spawn/drama/cult 流），保证首刀必有可观测复仇线。off / dramaOff 时不预置 → 逐字节守恒。
- **② Showdown 超时强制结算**（spec §3.2 死锁兜底，AC-8）：弧停 Showdown 超 `ShowdownTimeout`（仇人持续游历躲开）→ `TryAdvance` 强制结算一次，避免永久占并发槽。需 Director 跟踪「弧进入当前阶段的 tick」。
- **③ INV-CHAIN 端到端**（AC-7 验收核心）：预置冤孽 + cultivation-on 长跑 → 编年史出现完整跨代链。
- **④ 不变量守护**：INV-PERF（AC-5，候选遍历 O(强恩怨) 非 O(全员)）+ INV-NO-DEADLOCK（AC-8，无弧永久卡）+ INV-CAP（AC-4 容量门控）。

> **红线约束**：B.2 整数确定性（fixture 经 genRng 确定、超时 tick 比较整数）；B.3 off 逐字节（fixture 仅 dramaSeedFeuds+dramaOn 时预置，off 不调；超时逻辑仅 drama 路径）；R-NF2（_stageEnteredAt 进 Clone）；DomainEvent 单源。

## Acceptance Criteria

- [x] **D13.1 预置冤孽 fixture**：`CreateInitial(..., bool dramaSeedFeuds=false)`；`dramaSeedFeuds && dramaOn` → genRng.Split(7777) 子流预置强恩怨 + 师徒边。genRng 子流隔离（不新增 root.Split 编号）。
- [x] **D13.2 fixture off 逐字节**：默认/dramaOff → 不预置 → off 逐字节守恒（标记不分叉验证）。
- [x] **D13.3 Showdown 超时跟踪**：`_stageEnteredAt` 跟踪每弧进入当前阶段 tick；进新阶段更新；进 Clone 深拷。
- [x] **D13.4 Showdown 超时强制结算（AC-8）**：Showdown 分支无条件结算（drama-007b）+ Stalled 重排 → 弧不饿死；超时跟踪为显式保险。
- [x] **D13.5 INV-NO-DEADLOCK**：长跑活跃弧最终清空/有界（Director 单测 + World 长跑实证）。
- [x] **D13.6 INV-CHAIN 端到端（AC-7）**：dramaSeedFeuds + cultivation-on 跨种子长跑见复仇弧点燃→推进→结局 + 跨代继承；确定性长跑逐字节。
- [x] **D13.7 INV-PERF（AC-5）**：spy view 验 300 角色 2 强恩怨时 IsAlive 调用 ≤4（O(强恩怨)）。
- [x] **D13.8 INV-CAP（AC-4）**：长跑 ActiveArcs ≤ MaxConcurrentArcs；Intensity 恒 [0,Cap]；继承代数 ≤ MaxGeneration。
- [x] **D13.9 epic DoD 闭合**：AC-1~10 全过；drama-engine EPIC.md DoD 勾闭。
- [x] **D13.10 全量绿 + clean rebuild 0 警告**。

**机器证据（2026-06-26 /loop）**：全量 **1051 绿** / 0 失败 / 0 skip（1039 → +12）；clean rebuild 0 警告；
RuleBrain.cs UNCHANGED；determinism+off+floatscan+RuleBrain+StateSnapshot 41 focus 绿。
INV-CHAIN 端到端跨种子实证复仇弧完整生命周期 + 父债子偿跨代继承。drama-engine epic 收官。

## Implementation Notes

- **DramaDirector `_stageEnteredAt`** Dictionary<long(arcId), long(tick)>：点火创建弧时 set（Victimized@clock）；每次 Advanced 转移 set 新阶段 tick。进 Clone。
- **Showdown 超时**：AdvancePhase 弹弧时，若 `arc.Stage==Showdown && clock - _stageEnteredAt[arcId] > ShowdownTimeout` → 调 TryAdvance（Showdown 结算逻辑已存在，drama-007b：战力比较判胜负）。注：当前 RevengeArc.TryAdvance 的 Showdown 分支**无条件结算**（不检查同节点）——故超时 = 让到期的 Showdown 弧直接走 TryAdvance（已是结算）。**关键**：确保 Showdown 弧确实被调度重访（drama-009b 已 Push(clock+ShowdownDelay)）。超时的语义是「即使 Hunting 一直 Stalled 未进 Showdown，也不让弧饿死」——核查 Hunting Stalled 时仍重排（drama-009b 已 Push HuntingDelay）→ 弧不会饿死，只是反复 Stalled 重试，最终因参与者死亡 Abandoned。**故 no-deadlock 已由「Stalled 重排 + 死亡 Abandoned」保证**；Showdown 超时是额外保险（弧已进 Showdown 但 TryAdvance 因某条件未结算时）。**诚实**：drama-007b 的 Showdown 分支已无条件结算，本 story 的超时主要是 INV-NO-DEADLOCK 的显式验证 + 防御性 Showdown 停留上限。
- **fixture genRng 子流**：`WorldFactory` 在角色生成后，若 `dramaSeedFeuds && dramaOn`：`var feudRng = genRng.Split(常量)`（如 Split(7777)，纯派生不碰 root 编号）→ 选 2 个已生成角色 ID 作 (holder,target) Form 强恩怨 + 1~2 师徒边。确定（feudRng 种子驱动）。
- **off 安全**：fixture 仅 `dramaSeedFeuds && dramaOn` 分支；默认 false → 不构造 feudRng → genRng 消费序不变 → off 逐字节。

## Test Evidence

**Required (BLOCKING — Logic + 验收)**:
- `tests/Jianghu.Core.Tests/Drama/DramaFixtureTests.cs` —— D13.1/D13.2：dramaSeedFeuds 预置强恩怨（World.Grudges 非空 + 师徒边）；off/dramaOff 不预置；off 逐字节（默认参数轨迹不变）。
- `tests/Jianghu.Core.Tests/Drama/DramaDeadlockTests.cs` —— D13.4/D13.5/D13.8：Showdown 超时强制结算（Director 单测：弧停 Showdown 超时 → 结算）；长跑 INV-NO-DEADLOCK（活跃弧最终清空/有界）；INV-CAP（ActiveArcs ≤ Max）。
- `tests/Jianghu.Core.Tests/Drama/DramaInvChainTests.cs` —— D13.6/D13.7：预置冤孽 cultivation-on 跨种子长跑出现完整链；INV-PERF（spy 300 角色 2 恩怨 O(强恩怨)）。
- ⚠️ 既有 determinism/off/RuleBrain 全绿。

## Out of Scope

- 致死式灭门（spec v1 非致死边界，留后续）。
- 门派对立矩阵自动注入 SectFeuds（spec v1.2.1）。
- 主动趋仇正效用项（spec v1.2.1）。
- CLI 展示增强（drama 编年史已经 --drama 可见，足够）。
