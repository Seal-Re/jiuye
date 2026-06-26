# 戏剧引擎 B（Drama System — 恩怨/复仇/storylet）

> **Status**: In Design（2026-06-26 /loop 自驱补写；上游 spec 已 5-agent 圆桌定稿，本 GDD 形式化为可落地 8 节）
> **Author**: Claude（loop 全自主模式，用户 2026-06-26 授权）
> **Last Updated**: 2026-06-26
> **Implements Pillar**: 武侠"恩怨情仇·快意恩仇·父债子偿"的涌现戏剧性——NPC 江湖自发生出复仇桥段
> **真相源（上游）**: `docs/legacy-specs/specs/2026-06-13-v1.2-B-戏剧引擎-design.md`（5-agent 圆桌 + 集成官实读源码，11 步 walking-skeleton）
> **已落地（drama-001/002）**: `src/Jianghu.Core/Drama/RelationService.cs`（Adjust+Chronicle 封装）+ `DramaStoryletEngine.cs`（关系 storylet + 仇敌检测 affinity≤-50）。**仅薄工具层——spec 的恩怨账本/复仇弧/跨代继承核心引擎尚未建**。

---

## 0. 红线与约束（先于机制——本系统受这些硬约束，违反即推倒）

> 本节先立约束再谈机制（设计纪律）。下面每条都是不可协商的边界，机制设计必须在框内。

### 0.1 平台分层（项目目标 = Unity，非终端小游戏）

| 层 | 归属 | 戏剧系统的部分 |
|---|---|---|
| **Core 结算层**（`Jianghu.Core`，netstandard2.1，纯整数确定性，零引擎依赖）= **本 GDD 全部实现** | 当前 | 恩怨账本、复仇弧状态机、storylet 序列器、跨代继承——全是 NPC×NPC 涌现，headless 可跑，确定可复现 |
| **Unity 宿主层**（后期） | 后期 | 玩家介入复仇弧时的即时演出/选择 UI/镜头；**本 GDD 不实现**。Core 产 `DomainEvent` 流，Unity 读取渲染 |
| **可视化轨（B.8）** | 后期 | 编年史/关系图谱 UI = 古风（SVG/HTML-CSS）；非本 GDD 范围 |

戏剧引擎**整体落 Core 整数层**（NPC 自发恩怨复仇是涌现模拟核心）。玩家亲历某条复仇弧的即时体验属 Unity 宿主层，后期接。Core 零改写进 Unity 被引用。

### 0.2 红线约束清单（本系统强相关）

- **B.2 整数确定性（最硬）**：`Jianghu.Drama` 命名空间禁浮点。整数百分比定点 `x*pct/100` 每步 clamp，加权 total 用 `long` 防溢出。同种子逐字节复现。**需新增 IL 浮点扫描覆盖 `Jianghu.Drama`**（现 CorePurityTests 仅查引用程序集，无 IL 扫描——诚实标注，不假装复用）。
- **B.3 off 逐字节**：戏剧系统对空恩怨库/`MaxConcurrentArcs=0`/无候选时**严格 no-op**（连 `dramaRng` 都不消费、不产 Chronicle 行）→ 既有 38 off 测试 + 当前 876 绿基线逐字节不变。`_drama` 字段可空，off 即 null。
- **RNG 流隔离**：`WorldFactory` 新增 `dramaRng = root.Split(RngStreamIds.Drama=6)`（注：spec 写 Split(5)，但 Split(5)=Cultivation 已占用，**实际用已冻结预留的 Drama=6**）。不扰 Split(1..4) 顺序。决策阶段（RuleBrain）绝不碰 dramaRng；戏剧阶段绝不碰 brain/domain/spawn 流。
- **RuleBrain 零改动**：戏剧=独立事件流序列器，**不修改 RuleBrain 任何代码**（保 RuleBrainTests 不变）。仅经两条既有通道间接影响决策：(a) 覆写 `Character.Goal=Advance`（触发既有 1500 修炼权重）；(b) 镜像上限内负 `Relations`（触发既有 `notFoe` affinity≤-50 项）。**禁**新增 `ActionType.Vendetta`/`GoalKind.Revenge`。
- **DomainEvent 单源**：戏剧层**绝不直接 mutate 属性/关系**。属性只过 `StatBlock.Apply`，关系只过 `Relations.Adjust`，恩怨只过 `GrudgeLedger` chokepoint。所有效果经同一 `Project + Chronicle.Append` 管线。
- **World.Clone 命门（R-NF2）**：drama 新增的全部续跑敏感态（GrudgeLedger/ActiveArcs/DramaScheduler/dramaRng/计数器/对子cooldown/VariedSelector计数/DramaProfile）必须逐项进 `Clone()` 深拷。任一漏拷=长跑静默漂移。
- **非致死（v1 边界）**：复仇 Showdown 非致死（重创=关系崩坏到 -100 + 恩怨化解/转移）。致死式灭门留后续（触碰 Lifecycle/PopulationLow 涌现暴涌，成倍放大确定性风险）。
- **A.5 WIP≤2 / A.3 机器证据 / A.8 诚实 defer**：流程红线照常。

---

## 1. Overview

戏剧引擎是**独立于角色行动的事件流序列器**，叠加在 v1.0 单线程事件驱动核之上。它让 NPC 江湖**自发涌现复仇桥段**：A 灭了 B 的门 → B 怀恨蓄力修炼 → B 战力够了去寻仇 → 狭路相逢决战 → 重创化解或 B 寿尽 → B 的弟子继承大仇追杀 A 的传人（跨代恩怨链）。

核心范式：**恩怨账本（GrudgeLedger）** 记录有向深仇 → **复仇弧状态机（5 态）** 把一条仇驱动为多 tick 的戏剧弧 → **storylet 声明式事件库** 提供桥段模板 → **DramaDirector.Pump** 每 Advance 末尾推进到期弧 + 节流点火新弧。全整数、确定、空库 no-op。

**当前状态**：drama-001/002 只落了 `RelationService`（关系调整封装）+ `DramaStoryletEngine`（仇敌检测），是工具层；**本 GDD 描述的核心引擎（账本+弧+继承）待建**。

## 2. Player Fantasy

- **当前阶段（v1 涌现模拟）**：玩家是"江湖的史官"，读编年史看恩怨自发流转——"魔教血洗青城，幸存弟子隐忍二十载，终在华山之巅手刃仇人；其徒又因师门旧怨踏上新的复仇路"。爽感来自**跨代恩怨链的涌现宿命感**（父债子偿、冤冤相报）。
- **后期阶段（Unity 玩家介入）**：玩家操控某角色卷入复仇弧时，关键决战是可亲历的即时演出 + 抉择（手刃 / 放下 / 收其为徒化解世仇）——但这是 Unity 宿主层，本 GDD 不含。

## 3. Detailed Rules

### 3.1 恩怨账本 GrudgeLedger（独立有向表，与 Relations 并存）

- **为何独立**：不复用 `Relations` 负值——日常切磋累积的 `-4` 会误把口角当灭门。恩怨是独立真相源。
- 结构：`List<Grudge> _grudges`（确定性迭代）+ `Dictionary<long,List<int>> _byHolder`（仅查询加速，**不参与裁决顺序**）。
- `Grudge(Id, Holder, Target, Kind{Insult/Maiming/Slaughter}, Intensity[0,100], OriginTick, Generation, Cause, InheritedFrom?)`。
- 合并规则（确定性幂等）：同 (holder,target) → `Kind=max, Intensity=max, Generation=min, OriginTick=首次`。
- 镜像：恩怨产生时镜像一笔**上限内**负 Relations（`-Intensity/3` 钳制）→ 驱动 RuleBrain `notFoe`。这是 drama→decision 唯一受控通道。

### 3.2 复仇弧状态机（5 态）

```
ArcStage: Victimized → BuildUp → Hunting → Showdown → Resolved | Abandoned
```

| Stage | 进入门控（整数） | 效果 | NextDelay |
|---|---|---|---|
| Victimized | 点火即入（g.Intensity ≥ GrudgeIgniteThreshold） | `ArcStageEntered`；恩怨写参与者 Memory（负 valence 大恨不淘汰） | FirstStageDelay |
| BuildUp | 进入即记 `BuildUpBasePower`（=Force×2+Internal+Constitution，同 SparAction.Power） | `ArcStageEntered`；**Goal 覆写 Advance**（RuleBrain 自发疯修涨战力） | BuildUpDelay（长） |
| Hunting | 当前 Power ≥ BasePower + GrowthNeeded **且** 仇人在世 | `ArcStageEntered`；**镜像负 Relations** 触发 notFoe 偏好切磋仇人 | 中 |
| Showdown | 复仇者与仇人同节点（自发促成；超时强制结算"狭路相逢"） | 复用 SparAction Power 判胜负；`RevengeConsummated`（非致死：关系崩 -100 + 恩怨化解/转移） | 短 |
| Resolved/Abandoned | Showdown 完 / 仇人或复仇者先寿尽 | `ArcStageEntered(Resolved)` 或 `ArcAbandoned(reason)`；退场 | — |

**死锁兜底**：Showdown 停留超 `ShowdownTimeout` → `TryAdvance` 强制结算一次（避免永久占并发槽）。

### 3.3 跨代恩怨继承（walking-skeleton 的"深"）

- 师承/血缘走 **DramaProfile 侧表**（`CharacterId? Master/Bloodline`），不侵入 Character/Persona record。
- 触发：复仇者**寿尽**（Lifecycle Age≥Lifespan）且恩怨未了 → 在世子嗣/弟子继承一条 `Grudge(Cause=Inherited)` → `GrudgeInherited` → 点燃下一弧 → 跨代链。
- 继承强度 `= 原强度 × InheritDecayPct/100`（整数衰减）；`MaxGeneration=3` 封顶；继承人选取 年龄→武力→Id 三级确定性排序。

### 3.4 DramaDirector.Pump（每 Advance 末尾、MaybeSpawn 前调一次）

A. 推进到期弧（DramaScheduler 最小堆 `(NextWakeAt, ArcId)`，弧私有子流 `dramaRng.Split(arcId)` 顺序无关）。
B. 点火节流（`Clock < _nextIgnitionCheckAt` 则返回；`ActiveArcs ≥ MaxConcurrentArcs` 则返回）。
C. 候选收集（**只扫 `Grudges.AboveIntensity` 强恩怨，非全员** → O(强恩怨数)）+ 整数加权抽取（前缀和轮盘，串行消费主流）。

## 4. Formulas（全整数）

- **战力**（弧用）：`Power = Force×2 + Internal + Constitution`（同 RuleBrain.SelfPower / SparAction）。
- **点火权重**：`weight = BaseWeight × IntensityMul(intensity) × ArchMul(holder)`，整数定点 `w*mul/100` 每步 clamp + 兜底 `w≥1`，total 用 `long`。
- **镜像负 Relations**：`mirror = -clamp(Intensity/3, 0, RelationMirrorCap)`。
- **继承衰减**：`childIntensity = parentIntensity × InheritDecayPct / 100`。
- **Hunting 门控**：`currentPower ≥ buildUpBasePower + GrowthNeeded`。
- **加权抽取裁决序**：候选先 `Sort by (Weight desc, Id asc, CharacterId asc)`，再前缀和 + 整数轮盘。**禁 Dictionary/HashSet 枚举序参与裁决**。

## 5. Edge Cases

- 空恩怨库 / MaxConcurrentArcs=0 / 无候选 → 严格 no-op（不消费 dramaRng、不产 Chronicle）→ off 逐字节。
- 仇人先寿尽（Hunting/Showdown 前）→ 弧 Abandoned(reason=TargetDied)。
- 复仇者先寿尽且恩怨未了 → 触发继承（3.3）；绝嗣绝门 → 弧 Settled 无继承。
- Showdown 死锁（仇人持续游历躲开）→ ShowdownTimeout 强制结算。
- 继承达 MaxGeneration → 不再继承（防无限链）。
- 权重小值区整数截断为 0 → 兜底 `w≥1`；连乘溢出 → `long` total + `MaxArcWeightSum` 上界守门。
- 多弧抢同一角色 Goal → `MaxArcsPerCharacter=1` 防护；弧收束须还原原 Goal（防永久卡复仇态）。

## 6. Dependencies

- **上游（已存在）**：`Relations.Adjust`（关系 chokepoint）、`MemoryStore`（按 |Valence| 淘汰）、`Scheduler`（仿其最小堆建 DramaScheduler）、`RuleBrain`（Goal+notFoe 通道，零改）、`SparAction.Power`（战力公式）、`Lifecycle.Tick`（死亡触发继承）、`RngStreamIds.Drama=6`（已冻结预留）。
- **新增共享原语**：`VariedSelector<TKey>`（spec §5.5：long 计数 / 最小计数集内注入 IRandom 均匀抽 / 抽后+1 / Clone 计数表）——源码确无，Step 0 先建。
- **被改既有文件（最小纯加）**：World.cs（字段+Advance+Clone+Project）、WorldFactory.cs（dramaRng）、Chronicle.cs（事件 case）、LimitsConfig.cs（戏剧上限）、DomainEvent.cs（6 新 record）。
- **已落地可复用**：`RelationService`/`DramaStoryletEngine`（drama-001/002）+ A.2 `StoryletDef/StoryletExecutor` 框架。

## 7. Tuning Knobs（集中 LimitsConfig，init-only，默认值不破既有 Validate）

`GrudgeCap=100` / `GrudgeIgniteThreshold` / `MaxConcurrentArcs=3` / `MaxArcsPerCharacter=1` / `IgnitionCheckInterval` / `ArcPairCooldown` / 各 stage `NextDelay` / `GrowthNeeded` / `EscapeRatioPct[1,100]` / `DramaBudget≥1` / `MaxArcWeightSum` / `InheritDecayPct` / `MaxGeneration=3` / `RelationMirrorCap` / `ShowdownTimeout`。Validate 追加越界断言。

## 8. Acceptance Criteria（可测）

- **AC-1 空库 no-op（B.3）**：无恩怨/MaxConcurrentArcs=0 时，含 drama 的 World 跑 N 步 Chronicle 与不含 drama 逐字节一致；dramaRng 未被消费。既有 876 绿不退。
- **AC-2 确定性（B.2）**：预置强恩怨 fixture，同种子两跑 Chronicle 逐字节；tick=N Clone 续跑 == 不中断（StateSnapshot 含全 drama 态）。
- **AC-3 IL 浮点零**：新增 IL 扫描，`Jianghu.Drama` 命名空间零浮点。
- **AC-4 容量门控（INV-CAP）**：ActiveArcs ≤ MaxConcurrentArcs；每角色弧 ≤ MaxArcsPerCharacter；对子 cooldown 内不二次点火；Intensity 恒 [0,Cap]；继承单调不增。
- **AC-5 性能（INV-PERF）**：N=300 角色但仅 2 恩怨，跑 1000 Advance，点火候选遍历 = O(强恩怨数) 而非 O(N)。
- **AC-6 状态机**：弧只走合法转移，非法转移抛。
- **AC-7 跨代链（验收核心）**：预置强恩怨长跑，Chronicle 出现 `ArcIgnited → RevengeConsummated → GrudgeInherited → 第二条 ArcIgnited` 跨代链并最终收束。
- **AC-8 无死锁**：Showdown 超时强制结算，弧不永久卡。
- **AC-9 RuleBrain 零改**：RuleBrainTests 全绿；Goal 覆写/还原往返；镜像 Relations 触发 notFoe 行为。
- **AC-10 全量绿 + clean rebuild 0 警告 + off 逐字节**。

## 9. Story 拆解映射（落地顺序，承 spec 11 步）

| Story | 内容 | 对应 spec Step |
|---|---|---|
| drama-003 | VariedSelector 共享原语（先决） | Step 0 |
| drama-004 | 戏剧值类型骨架（Grudge/Arc/Predicate/Effect/DramaProfile） | Step 1 |
| drama-005 | GrudgeLedger（List+索引+合并+Clone） | Step 2 |
| drama-006 | LimitsConfig 戏剧上限 + WeightedPicker 整数轮盘 | Step 3-4 |
| drama-007 | storylet schema + RevengeArc 5 态机 + FindIgnitions | Step 5 |
| ↳ 007 | storylet substrate（schema + IDramaView + DramaContext + StoryletSelector）✅ | Step 5a |
| ↳ 007b | RevengeArc.TryAdvance 5 态机 + IDramaMutator + FindIgnitions ✅ | Step 5b |
| drama-008 | 6 DomainEvent + Project/Chronicle case（空库逐字节先证）✅ | Step 6 |
| drama-009 | DramaScheduler + DramaDirector.Pump + WorldFactory dramaRng | Step 7 |
| ↳ 009 | DramaScheduler 最小堆 + IDramaMutator 事件汇 seam ✅ | Step 7a |
| ↳ 009b | DramaDirector.Pump 推进相 + 节流点火相 ✅ | Step 7b |
| drama-010 | World 接线（字段+Advance+Clone 全 drama 态深拷）✅ | Step 8 |
| drama-011 | 受控耦合（Goal 覆写/还原 + 镜像 Relations）✅ | Step 9 |
| drama-012 | 跨代继承（寿尽→继承→点燃 + 衰减 + 封顶）✅ | Step 10 |
| drama-013 | 预置冤孽 fixture + 种子 storylet + INV-CHAIN 端到端验收 | Step 11 |

> drama-001/002（RelationService/DramaStoryletEngine）已落，作为 storylet 关系结算的复用件。

## 10. Open Questions（spec 遗留，实现时按默认裁决，不阻塞）

- Showdown 致死：v1 默认非致死（本 GDD 0.2 红线）。致死留后续独立评估。
- 预置冤孽 vs 自然累积：v1 用 WorldFactory seed 预置 1~2 对强恩怨（确定可观测）。
- DramaLimits 并入 LimitsConfig（倾向并入，集中验证）。
- Pump 内新孵化弧：默认延后到下次 Pump（推进相处理本批，点火相产的新弧下轮参与）。
