# Story drama-009b: DramaDirector.Pump 推进相 + 节流点火相

> **Epic**: drama-engine
> **Status**: Done（2026-06-26 /loop TDD：1008 绿 [+11]，0 警告，空库 no-op rng 不消费实证）
> **Layer**: Core（`Jianghu.Drama`）
> **Type**: Logic
> **Estimate**: 中 (0.6d)
> **Depends**: drama-005（GrudgeLedger）、006（WeightedPicker）、007（DramaContext/IDramaView）、007b（RevengeArc/IgnitionScanner）、009（DramaScheduler/IDramaMutator）—— **全 done**
> **ADR**: adr-0001-integer-determinism（点火主流串行消费、确定性裁决序）、adr-0003-cultivation-off-byte-identical
> **GDD**: `design/gdd/drama-system.md` §3.4(Pump) + §4(裁决序) + §9（drama-009b = spec Step 7b）

## Context

承 drama-009 后半（spec Step 7 的 Pump 编排）。`DramaDirector` 是戏剧引擎的**编排核心**——持戏剧可变态，每次世界 Advance 末尾调一次 `Pump`，把已建的积木（账本 / 调度器 / 状态机 / 点火扫描 / 轮盘 / 事件汇）串成完整的「推进到期弧 + 节流点火新弧」流程。**本 story 仍 pre-World-wiring**：Director 是独立可单测单元（注入 mock `IDramaView` + recording `IDramaMutator` + `Pcg32`），World 接线（持有 Director + 每 Advance 调 Pump + Clone 深拷）是 drama-010。

`Pump(clock, view, mutator, rng)` 两相（spec §3）：
- **A 推进相**：弹到期弧（`scheduler.HasDue` + `≤ DramaBudget`）→ `RevengeArc.TryAdvance` → 按转移结果 `Emit` 事件 → 收束（移出 activeArcs）或重排（按新阶段 NextDelay）。
- **B/C 点火相**（节流）：`clock < _nextIgnitionCheckAt` 或 `activeArcs ≥ MaxConcurrentArcs` → 返回；否则 `IgnitionScanner.FindIgnitions` → `WeightedPicker.PickIndex`（点火串行消费主流）→ 创建 Victimized 弧 → 设对子冷却 → `Emit ArcIgnited` → 入调度。

> **红线约束**：B.2 整数确定性（点火主流串行消费、裁决序经 IgnitionScanner/WeightedPicker 已定，禁 Dictionary 枚举序裁决）；B.3 off 逐字节（空库 Pump 严格 no-op——不消费 rng、不产事件；Director 仅 drama-010 在 on 路径构造）；DomainEvent 单源（所有效果经 mutator.Emit）；R-NF2（Director 全可变态 Clone 清单，drama-010 深拷）。

## Acceptance Criteria

- [x] **D9b.1 推进相弹到期弧**：`scheduler.HasDue(clock)` 为真时弹弧 → `TryAdvance` → `Emit` → 重排/收束；单次 Pump 推进数 `≤ DramaBudget`（防卡死，呼应 Advance budget）。
- [x] **D9b.2 转移→事件映射**：Advanced→`ArcStageEntered(Next.Stage)`；Completed→`RevengeConsummated(AvengerPrevailed)`；Abandoned→`ArcAbandoned(reason)`；Stalled→无事件，仅按当前阶段 NextDelay 重排（待下次重试）。
- [x] **D9b.3 重排/收束**：Advanced/Stalled → 更新 activeArcs 中该弧 + `scheduler.Push(arc, clock + StageDelay(stage))`；Completed/Abandoned → 移出 activeArcs（不再排）。
- [x] **D9b.4 点火节流**：`clock < _nextIgnitionCheckAt` → 跳过点火相；过闸后 `_nextIgnitionCheckAt = clock + IgnitionCheckInterval`。
- [x] **D9b.5 并发上限**：`activeArcs.Count >= MaxConcurrentArcs` → 不点火（**MaxConcurrentArcs=0 → 永不点火**，AC-1 no-op 开关）。
- [x] **D9b.6 点火创建弧**：`FindIgnitions`→`WeightedPicker.PickIndex`→创建 Victimized 弧入 activeArcs→设对子冷却→`Emit ArcIgnited`→入调度。
- [x] **D9b.7 ⚠️ 空库 no-op（B.3 命门）**：空恩怨库 + 无活跃弧 → Pump 不产事件、**不消费 rng**（GetState 前后比对相等）；`MaxConcurrentArcs=0` 同样 no-op 且 rng 不动。
- [x] **D9b.8 对子冷却挡二次点火**：点火后 (holder,target) 在 `ArcPairCooldown` 内 → `onPairCooldown` 谓词排除，不复燃。
- [x] **D9b.9 确定性**：同种子 rng + 同初始态两 Director → 逐 Pump 同事件序。
- [x] **D9b.10 Clone 支持**：`Clone(GrudgeLedger)` 深拷全可变态（activeArcs/scheduler/pairCooldown/_nextIgnitionCheckAt/_nextArcId），独立实例。
- [x] **D9b.11 IL 浮点零 + 既有 997 绿不退 + clean rebuild 0 警告**。

**机器证据（2026-06-26 /loop）**：全量 **1008 绿** / 0 失败 / 0 skip（997 → +11，破千）；clean rebuild 0 警告；红线 focus 20 绿（DramaDirector + off 逐字节 + 浮点扫描）。⚠️ 空库 no-op rng 不消费实证（GetState 前后逐字节相等）。
> 中途 1 测试自身计时 bug（pair_cooldown 测试 avenger 战力起点=200 致 base 捕获后无法过 Hunting 门）→ 改起点 100、BuildUp 后涨 200 修正（非 impl bug）。

## Implementation Notes

- **DramaDirector 状态**（全进 Clone 清单 R-NF2）：`GrudgeLedger _ledger`（引用，drama-010 World 持有同一实例）、`DramaScheduler _scheduler`、`List<ArcInstance> _activeArcs`（确定性迭代）、`Dictionary<(long,long),long> _pairCooldownUntil`（对子→冷却到期 tick，**仅成员测试不枚举裁决**）、`long _nextIgnitionCheckAt`、`long _nextArcId`、`LimitsConfig _limits`。
- **hasActiveArc(holder)** = `_activeArcs` 线性扫 `Avenger==holder`（活跃弧 ≤ MaxConcurrentArcs，小集）。**onPairCooldown(h,t)** = `_pairCooldownUntil` 查 (h.Value,t.Value) 且 `clock < expiry`。
- **推进相**：弹出的 item.Arc 在 activeArcs 找不到（已被前面收束）→ skip（防陈旧调度项）。`TryAdvance` 内部死亡守卫已处理参与者亡→Abandoned。
- **点火主流串行**：`WeightedPicker.PickIndex(weights, rng)` 用注入 rng（drama-010 是 dramaRng 主流，顺序敏感）。弧内推进 TryAdvance 不消费 rng（纯转移），故本 Pump 仅点火消费 rng。
- **延后项（A.8 显式）**：
  - **Showdown/Hunting 超时强制结算**（AC-8 no-deadlock）：当前设计 Hunting 须同节点才进 Showdown，若仇人永不同节点则 Stalled 重试（受参与者死亡→Abandoned 兜底）。显式超时（停留 > ShowdownTimeout 强制结算）延 **drama-013**（验收期补，需 arc 入场 tick 跟踪）。
  - **escape 掷骰**（EscapeRatioPct + arcRng 私有子流）延后续调参。
  - **同节点的自发促成**（Goal 覆写 / 镜像 Relations 让 RuleBrain 自发趋同节点）= drama-011 受控耦合。本 story Pump 只读 view 判同节点，不写 Goal/Relations。

## Test Evidence

**Required (BLOCKING — Logic + 确定性)**:
- `tests/Jianghu.Core.Tests/Drama/DramaDirectorTests.cs` —— D9b.1~D9b.9：
  - ⚠️ 空库 Pump → 零事件 + rng 位置不变（NextUInt 前后比对）+ MaxConcurrentArcs=0 同样 no-op。
  - 预置强恩怨 → 点火 Emit ArcIgnited + 入调度；到期推进 Victimized→BuildUp Emit ArcStageEntered；Showdown→Emit RevengeConsummated（胜负）；仇人死→ArcAbandoned。
  - MaxConcurrentArcs 上限挡点火；对子冷却挡二次点火；DramaBudget 限单 Pump 推进数。
  - 确定性：同种子两 Director 逐 Pump 同事件序。
  - Clone：深拷后改原不影响克隆（activeArcs/scheduler/cooldown 独立）。

## Out of Scope

- World 字段 / Advance 末尾调 Pump / Clone 接线 / dramaRng=Split(6)（drama-010）。
- Goal 覆写 / 镜像 Relations（drama-011）。
- 跨代继承监听 CharacterDied（drama-012）。
- Showdown 超时 + escape 掷骰（延后，见 Notes）。
