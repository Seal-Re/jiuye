# Story drama-011: 受控耦合 — Goal 覆写 Advance + 镜像负 Relations（RuleBrain 零改）

> **Epic**: drama-engine
> **Status**: Done（2026-06-26 /loop TDD：1028 绿 [+9]，0 警告，RuleBrain 零改实证）
> **Layer**: Core（`Jianghu.Drama` + `Jianghu.Sim.World`）
> **Type**: Logic + 集成
> **Estimate**: 中 (0.5d)
> **Depends**: drama-009b（DramaDirector，done）、drama-010（World 接线 + IDramaMutator，done）
> **ADR**: adr-0003-cultivation-off-byte-identical、adr-0001-integer-determinism
> **GDD**: `design/gdd/drama-system.md` §0.2(RuleBrain 零改/受控通道) + §3.2(BuildUp/Hunting 效果) + §9（drama-011 = spec Step 9）

## Context

前 8 story 建了完整复仇弧引擎并接进 World，但弧只是「在编年史里自说自话」——**还不影响 NPC 真实行为**。drama-011 是设计的兑现：让复仇弧**经两条既有通道间接驱动 RuleBrain**，实现「复仇者自发疯狂修炼涨战力 → 战力够了自发寻仇切磋」的涌现，且 **RuleBrain 一行不改**（spec 最高优先唯一裁决）。

**两条受控通道**（spec §0.2，均为 RuleBrain 既有项）：
- (a) **BuildUp 阶段 → 覆写 `Character.Goal = Advance`**：RuleBrain 既有 `Train` 项在 `Goal==Advance` 时权重 1500（vs 600）→ 复仇者自发疯修涨战力。
- (b) **Hunting 阶段 → 镜像上限内负 `Relations`**（复仇者→仇人，钳 `RelationMirrorCap`）：RuleBrain 既有 `notFoe = affinity≤-50 ? -500 : 0` 项 → 复仇者对仇人敌意摩擦（不再被 notFoe 惩罚拦着切磋仇人）。
- **弧收束（Resolved/Abandoned）→ 还原原 Goal**：防角色永久卡复仇态（spec 风险）。

> **诚实标注**：`notFoe` 是「对好感 > -50 者**不**惩罚切磋」的项——镜像负 Relations 到 ≤-50 是**解除**「不打熟人」的软约束，使切磋仇人不再被扣 500，而非新增「主动趋仇」正权重。完整「主动寻仇」正效用项留后续（spec openQuestions，v1.2.1）。本 story 落 spec §0.2 明确的两通道。

> **红线约束**：**RuleBrain 零改**（核心——RuleBrainTests 全绿不变）；B.3 off 逐字节（耦合写只在 drama Pump 路径，off=_drama null 不可达）；B.2 整数确定性（Goal 覆写确定 + 镜像 delta 整数钳制）；DomainEvent 单源破例说明见下；Goal 还原往返（防永久卡）。

## Acceptance Criteria

- [x] **D11.1 IDramaMutator 扩三写口**：`OverrideGoal` / `RestoreGoal` / `MirrorRelation`。World 实现：Goal set；MirrorRelation 经 `Relations.Adjust`。
- [x] **D11.2 BuildUp 覆写 Goal**：进 BuildUp 记复仇者原 Goal + `OverrideGoal(avenger, Advance)`。仅 avenger。
- [x] **D11.3 Hunting 镜像负 Relations**：进 Hunting → `MirrorRelation(avenger, target, -RelationMirrorCap)`。
- [x] **D11.4 收束还原 Goal**：Resolved/Abandoned → `RestoreGoal(avenger, 原 Goal)`；往返验证（原 X → 收束后 X）。
- [x] **D11.5 ⚠️ RuleBrain 零改**：`RuleBrain.cs` 一字不改（git diff 实证 UNCHANGED）；`RuleBrainTests` 全绿。
- [x] **D11.6 World 端涌现**：drama-on 复仇者某刻 Goal==Advance（疯修）；Hunting 期对仇人 affinity ≤ -RelationMirrorCap（GrowthNeeded=0 使弧可达 Hunting，off 战力封顶见 Notes）。
- [x] **D11.7 Clone 含原 Goal 表**：`_originalGoals` 进 Director Clone（验克隆推进还原存档原 Goal=99）。
- [x] **D11.8 ⚠️ off 逐字节 + 确定性 + Clone 续跑**：既有逐字节不退；drama-on 同种子两跑逐字节；Clone 续跑逐字节。
- [x] **D11.9 全量绿 + clean rebuild 0 警告 + IL 浮点零**。

**机器证据（2026-06-26 /loop）**：全量 **1028 绿** / 0 失败 / 0 skip（1019 → +9）；clean rebuild 0 警告；
RuleBrain.cs **git diff UNCHANGED**（零改铁律实证）；RuleBrain+determinism+off+coupling+wiring 共 45 focus 绿。

### ⚠️ 涌现发现（off 模式战力封顶，记录非 bug）
World 端测试发现：**off 模式（无 cultivation）战力受 StatCap=30 封顶**，复仇者疯修也难涨 +GrowthNeeded(50) →
BuildUp→Hunting 门常达不到 → 弧多停在 BuildUp 直到参与者寿尽 Abandoned。这是**真实涌现行为非缺陷**：
完整复仇弧（走到 Hunting/Showdown）需 **cultivation-on**（修为突破解锁高战力增长）。镜像 Relations 测试遂用
`GrowthNeeded=0` 让弧可达 Hunting（测镜像通道本身）。drama-013 端到端验收宜用 cultivation-on 长跑见完整跨代链。

## Implementation Notes

- **IDramaMutator 扩展**（drama-009 接口加 3 法）：World 已实现 IDramaMutator，补三方法。`OverrideGoal`/`RestoreGoal`：`_alive[id].Goal = new Goal(kind, progress)`（Goal record，Progress 保留原值或置 0——还原时用记录的原 Goal 整体）。`MirrorRelation`：`Relations.Adjust(holder, target, delta)`（已钳 [-100,100]）。
- **DramaDirector 耦合时机**：在 `AdvancePhase` 的转移后、`EmitForTransition` 旁，按 `trans.Next.Stage` 触发：
  - 进 BuildUp（Advanced 到 BuildUp）→ 存 `_originalGoals[arcId] = view.GoalOf(avenger)` + `mutator.OverrideGoal(avenger, Advance)`。
  - 进 Hunting → `mutator.MirrorRelation(avenger, target, -limits.RelationMirrorCap)`。
  - Completed/Abandoned → `mutator.RestoreGoal(avenger, _originalGoals[arcId])` + 移除映射。
- **IDramaView 加 `Goal GoalOf(CharacterId)`**：Director 需读原 Goal 存档。World 实现 `_alive[id].Goal`。
- **`_originalGoals` Dictionary<long(arcId), Goal>** 进 Director Clone（D11.7）。Goal 是不可变 record，浅拷即深拷。
- **DomainEvent 单源破例**：Goal/Relations 是「受控耦合写」，非编年史事件——spec §0.2 明确这是 drama→decision 的**唯一受控通道**，经 IDramaMutator 专用写口（非旁路 mutate），符合「戏剧效果经统一管线」精神。不产 DomainEvent（Goal 变化不是史官记的事，是内在动机）。
- **off 安全**：三写口仅 Director 在 Pump 内调；off=_drama null → Pump 不跑 → 写口不可达 → off 逐字节守恒。
- **RuleBrain 零改铁律**：不碰 RuleBrain.cs。耦合完全经 Goal/Relations 既有读取项生效。

## Test Evidence

**Required (BLOCKING — Logic + 确定性)**:
- `tests/Jianghu.Core.Tests/Drama/DramaCouplingTests.cs` —— D11.2~D11.4：recording mutator 验 BuildUp→OverrideGoal(Advance) / Hunting→MirrorRelation(-cap) / Completed+Abandoned→RestoreGoal；Goal 往返（原 X → 收束后 X）。
- `tests/Jianghu.Core.Tests/Drama/DramaCouplingWorldTests.cs` —— D11.6：drama-on World 预置强恩怨，长跑断言复仇者 BuildUp 期 Goal==Advance + Hunting 期对仇人 affinity≤-cap + 收束后还原。
- ⚠️ 既有 `RuleBrainTests`（零改证）+ `OffByteIdenticalTests`/`DramaWiringByteIdenticalTests`/`DramaWiringTests`(Clone 续跑) 全绿。

## Out of Scope

- 主动「趋仇」正效用项 / RevengeBiasBrain（spec v1.2.1，否决于本期）。
- 跨代继承（drama-012）。
- 预置冤孽 fixture + Showdown 超时 + INV-CHAIN 端到端（drama-013）。
