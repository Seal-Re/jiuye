# Story 007: 控制衰减与冷却（Hybrid Cooldown & Diminishing Returns）

> **Epic**: balance-cross
> **Status**: In Review
> **Layer**: Core
> **Type**: Logic
> **TR**: TR-BAL-001（`docs/architecture/tr-registry.yaml`；C1 平价的机制侧——消除 stun-lock 零博弈碾压）
> **Estimate**: 中 (1.5d)
> **Manifest Version**: 2026-07-03（`docs/architecture/control-manifest.md`）
> **Last Updated**: 2026-07-03

## Context

**GDD**: `docs/legacy-specs/specs/2026-06-14-v1.2-B5-平衡标定INV-CROSS-design.md`（§2 C1 平价；本 story 治机制侧 action-economy 失衡）
**Requirement**: `TR-BAL-001`（同 UT 平价——连续硬控无递减致单向锁死，破坏 action economy，与 PE 平价正交）

**ADR Governing Implementation**: adr-0001-integer-determinism（primary）, adr-0002-module-factory-effect-system
**ADR Decision Summary**: Core 整数确定性、同种子逐字节复现；战斗效果经 Modules 工厂（Control 属其一）。

**Engine**: .NET 8 / netstandard2.1 | **Risk**: LOW
**Engine Notes**: 无 post-cutoff API；纯 DuelEngine 内整数回合计数。

**Control Manifest Rules (Core 层)**:
- Required: 战斗算子经 Modules 工厂；新增状态离散整数；确定性（同种子逐字节）。
- Forbidden: 浮点（B.2）；对拍引入 RNG（当前对拍纯确定，DR 不可用成功率随机——见下）。
- Guardrail: 改 v1.0/DuelEngine 须过 off 逐字节 + C2/C3 不退。

---

## 背景（承 balance-006 发现 + 用户 2026-07-03 裁决）

`gouhun`(turns=2)/`suoming`(turns=2)/`arrayLock`(turns=3) 等硬控（`Modules.Control(key, turns≥2)`）现无冷却/递减/免疫：控制者每回合出招重挂控 → 控制在到期前被续挂，被控方 skill→null 永久锁死（**stun-lock 单向锁**，2026-07-06 主控实测证实：turns≥2 每回合重挂 → 被控方碾压死 margin=72）。这是 balance-006 揭示的 C1 残余碾压的机制根源之一，且与 PE 正交——降面板（方案 A）治标（锁住仍必胜）且破坏 balance-003 PE 归一化。

> ⚠️ **前提校正（2026-07-06 主控核验，推翻初版）**：初版称 `mihun`(turns=1) 永久锁死系**误判**。实测 turns=1 控制因 tick 时序（回合**首**查 `IsControlled`／回合**末** `TickDots` 递减移除）当回合挂、当回合消，**从不拒止行动**（等 PE 对拍平局 margin=0）。真实 stun-lock 源 = **turns≥2** 每回合重挂。turns=1 是否应生效（"轻控当回合打断"设计意图 vs off-by-one bug）**另立项**（见 Out of Scope），不混入本 story。

**用户裁决（方案 B 变体）**：引入**混合冷却与递减**，从时间经济根治零博弈碾压，不碰 PE。

### ⚠️ 关键工程事实（已核代码，钉死 AC 形态）

1. **对拍纯确定（无 RNG）**：`DuelEngine.ResolveR2` 回合循环无 rng/roll（2026-07-06 实测复核）。故**抗性递减必须走"持续回合数阶梯降"**（duration-based），**不可**用"成功率阶梯降"（需 RNG，违 B.2）。用户指南的"成功率或持续回合数下降"→ 本项目取**持续回合数**。
2. **控制状态 duel-local**：`pendingControls` 在 `ResolveR2` 内新建、不逃逸（`Result` 不携带）。新增 CD/DR 计数字段**同为 duel-local**（每场对拍内），**不入 World.Clone/Character 持久态** → B.3 逐字节天然安全（off 更不调 DuelEngine）。若未来需跨对拍持久控制史再评。
3. **tick 时序（2026-07-06 主控实测钉死）**：`IsControlled` 在回合**开始**查（L102-103），`TickDots` 在回合**末**递减+移除控制（L199→L421-427）。故一个 turns=N 的控制，实际拒止行动的回合数 = **N-1**（当回合挂上那次不计，因该回合的 start-check 已过）。**DR/CD 设计据此**：baseTurns 与阈值以"实际拒止回合"为准，测试断言用 turns≥2 观测（turns=1 现为哑弹，属另立项）。

---

## Acceptance Criteria

> **机器证据（2026-07-06 主控实测）**：全量 **1077 绿 / 0 失败 / 0 跳过**（1062+15 新）；B.3 off 逐字节+determinism **27 绿**；B.2 Cultivation 浮点扫描 **8 绿**；CLI `dotnet run … 42 60 --cultivation` exit 0。测试文件 `tests/Jianghu.Core.Tests/Cultivation/ControlCooldownTests.cs`（15 用例）。

- [x] 7.1 **硬冷却（Hard Cooldown）**：控制者成功施加控制后，进入固定 `ControlCooldown` 回合的"该控制不可再挂"期，强制给被控方博弈窗口（不能连续锁）。✅ `HardCooldown_ReducesAttackerDominance_AndDefenderPunishesMore`（CD 降碾压 margin + 攻方受创更多）。
- [x] 7.2 **抗性递减（Diminishing Returns，duration-based）**：同一目标重复受同类控制，`TurnsRemaining` 阶梯下降（阶梯由 `ControlDRStep` 定，0=免疫）。✅ `EffectiveControlTurns_DurationBasedLadder`（6 例阶梯+地板钳0+DRStep=0 退化）+ `DiminishingReturns_ReducesAttackerDominance`。落点 `DuelEngine.EffectiveControlTurns(base,hits,step)=max(0,base-hits*step)`。
- [x] 7.3 **博弈窗口实证**：stun-lock 被打破——被控方在 CD/DR 窗内能正常行动，有还手机会。✅ `CombinedCdDr_BreaksStunLock_MarginCollapses`：等 PE 控制对拍，CD+DR（2/1）使碾压 margin **至少腰斩**（vs 0/0 退化档全程锁死 `NoCdNoDr_CharacterizesStunLock` margin≥50）。
- [x] 7.4 **离散整数 + 确定性**：新增 CD/DR 状态全离散整数（B.2）；同种子对拍逐字节复现。✅ 浮点扫描 8 绿 + `Deterministic_SameConfig_TwoRunsByteIdentical`（标量逐一比对）。
- [x] 7.5 **duel-local 或正确 Clone**：CD/DR 状态 duel-local 则不需入 Clone。✅ `ControlLimiterState` 在 `ResolveR2` 内 new、不逃逸（`Result` 不携带）、不入 `World.Clone`/`CultivationState` → B.3 off 逐字节天然安全（off 更不调 DuelEngine，27 determinism 绿实证）。
- [x] 7.6 **off 逐字节 + C2/C3 不退**：✅ off 不调 DuelEngine（SparAction ON 分支才调），27 determinism 绿；C2 碾压单调（UT-gap auto-win 路径，够不到控制逻辑）、C3 辅助豁免不退（全量 1077 绿）。
- [~] 7.7 **CC 碾压率降** — **指标不适配·核心机制已另证（2026-07-06 主控裁决，用户选 A）**：`SparStompRateTests` 量的"margin≥999 占比"实测 = **24%（seed42=34%/99=23%/2026=17%），与 balance-004 基线逐位相同，CD/DR 零影响**。根因：HP=PE → ≥999 碾压由 **PE 差**驱动，与 stun-lock **正交**（承 balance-006 发现）；CD/DR 只压缩近等 PE 控制对拍 margin，那些本就够不到 999 阈值。**stun-lock 破除的正确证据 = 7.3 单元差分（等 PE、margin 腰斩），sim 的 999 指标这个轴测不到**。不硬造动了的数字（story 原文"勿硬编造"）。若需 7.7 字面兑现 → 另立"被控方被拒行动回合数随 CD/DR 下降"的 sim harness（见 Out of Scope，未来可选）。

---

## Implementation Notes

*承 adr-0001（整数确定性）+ DuelEngine 现有 ControlEntry 结构：*

- **CD 落点**：`ResolveExchange` 的 Control 分支（`pendingControls.Add` 前）。加一个 duel-local 结构（如 `Dictionary<(Side,string), int> controlCooldownUntilRound` 或每 side 的整数计数）记"该 key 控制下次可用回合"。挂控成功 → 记 `currentRound + ControlCooldown`；未到则本回合不挂。
- **DR 落点**：同结构记"该目标该 key 已受控次数"，`TurnsRemaining = max(0, baseTurns - hitCount * ControlDRStep)`。次数满 → 0 回合=免疫。
- **回合号**：`ResolveR2` 循环已有 `round` 变量（`for (int round...)`）——传入 `ResolveExchange` 供 CD 比较（纯整数）。
- **旋钮**：`LimitsConfig` 加 `ControlCooldown`（默认如 2）+ `ControlDRStep`（默认如 1），带校验（≥0/≥1），承数据驱动勿硬编码。
- **不碰 PE / 不碰 balance-003 乘子**：纯控制时间经济，C1 PE 平价保持。
- **Modules 工厂**：Control 算子本身不变（B.9）；CD/DR 是 DuelEngine 结算侧逻辑，非新算子。

---

## Out of Scope

- 降 RealmMultipliers/BasePower（方案 A，被裁决否决——破坏 C1 PE 平价）。
- 跨对拍持久控制史（除非 duel-local 不足；本 story 优先 duel-local）。
- 成功率随机递减（对拍无 RNG，B.2 禁）。
- 非硬控（Dot/Evade/CounterMul）的调整——本 story 只治硬控 stun-lock。
- **turns=1 控制哑弹问题（另立项，2026-07-06 发现）**：`mihun`/`soulLock`/`lawPrison`/`voidPrison`（全 turns=1）因 tick 时序从不拒止行动（关键事实 #3）。是否应生效需**设计裁决**（"轻控当回合打断"意图 vs off-by-one bug）——不在本 story 范围，待用户定性后单独立 story（暂记 balance-008 候选）。本 story 的 CD/DR 只作用于 turns≥2 的真实 stun-lock。

---

## QA Test Cases

*Logic 自动化测试 spec。对拍确定性，无需 RNG mock。*

- **AC-1（7.1 硬冷却）**
  - Given：控制者带 `Control` 技能，CD=2
  - When：连续回合对拍
  - Then：控制不能每回合重挂——两次成功控制间隔 ≥ ControlCooldown 回合
  - Edge：CD=0（退化为无冷却，向后兼容检查）
- **AC-2（7.2 抗性递减）**
  - Given：同目标反复受同类控制，DRStep=1，baseTurns=2（turns=2 = 实际拒止 1 回合，见关键事实 #3）
  - When：第 2 次控制
  - Then：TurnsRemaining 降为 1（=0 实际拒止回合，免疫），被控方该窗口可行动
  - Edge：不同 key 的控制不共享 DR 计数
- **AC-3（7.3 博弈窗口）**
  - Given：ghost_yang_hun(gouhun, turns=2) vs 势均对手（turns≥2 才是真实 stun-lock 源）
  - When：对拍长跑
  - Then：被控方存在 skill≠null 的行动回合（stun-lock 打破），非全程锁死
- **AC-4（7.4/7.5 确定性+Clone）**
  - Given：同种子对拍
  - When：跑两次
  - Then：逐字节相同；off 模式（无 DuelEngine 调用）与既有 OffByteIdentical 基线一致
- **AC-5（7.6 C2/C3 不退）**：UT gap≥2 高 UT ≥80%（C2）；辅助路豁免（C3）不变
- 测试文件：`tests/Jianghu.Core.Tests/Cultivation/ControlCooldownTests.cs`（新）+ `Determinism/OffByteIdenticalTests.cs`（回归守）

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/Jianghu.Core.Tests/Cultivation/ControlCooldownTests.cs` — 须存在且过 + off 逐字节回归守
**Status**: [x] 已创建 — 15 用例全绿；off 逐字节 27 绿 + 浮点扫描 8 绿回归守（2026-07-06 主控实测）。**待提交 sha 补齐 A.3 证据门第②项后转 Done。**

---

## Dependencies

- Depends on: balance-006（C1 判据 done，确认 CC 是残余碾压根源之一）；balance-004（切磋行为侧 done）
- Unlocks: CC 机制健康化（消 stun-lock）；balance-cross epic 战斗机制侧收敛
