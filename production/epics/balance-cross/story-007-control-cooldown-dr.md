# Story 007: 控制衰减与冷却（Hybrid Cooldown & Diminishing Returns）

> **Epic**: balance-cross
> **Status**: Ready
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

`mihun` 等硬控（`Modules.Control("mihun", 1)`）现无冷却/递减/免疫：控制者每回合出招重挂 1 回合控 → 被控方 skill→null 永久锁死（**stun-lock 单向锁**，代码证实 `IsControlled` + 每回合重挂）。这是 balance-006 揭示的 C1 残余碾压的机制根源之一，且与 PE 正交——降面板（方案 A）治标（锁住仍必胜）且破坏 balance-003 PE 归一化。

**用户裁决（方案 B 变体）**：引入**混合冷却与递减**，从时间经济根治零博弈碾压，不碰 PE。

### ⚠️ 关键工程事实（已核代码，钉死 AC 形态）

1. **对拍纯确定（无 RNG）**：`DuelEngine.ResolveR2` 回合循环无 rng/roll。故**抗性递减必须走"持续回合数阶梯降"**（duration-based），**不可**用"成功率阶梯降"（需 RNG，违 B.2）。用户指南的"成功率或持续回合数下降"→ 本项目取**持续回合数**。
2. **控制状态 duel-local**：`pendingControls` 在 `ResolveR2` 内新建、不逃逸（`Result` 不携带）。新增 CD/DR 计数字段**同为 duel-local**（每场对拍内），**不入 World.Clone/Character 持久态** → B.3 逐字节天然安全（off 更不调 DuelEngine）。若未来需跨对拍持久控制史再评。

---

## Acceptance Criteria

- [ ] 7.1 **硬冷却（Hard Cooldown）**：控制者成功施加控制后，进入固定 `ControlCooldown` 回合的"该控制不可再挂"期，强制给被控方博弈窗口（不能连续锁）。
- [ ] 7.2 **抗性递减（Diminishing Returns，duration-based）**：同一目标重复受同类控制，`TurnsRemaining` 阶梯下降（如首次 1 回合 → 第 2 次 0 回合=免疫），阶梯由 `ControlDRStep` 定。
- [ ] 7.3 **博弈窗口实证**：stun-lock 被打破——被控方在 CD/DR 窗内能正常行动（skill≠null），有还手机会。
- [ ] 7.4 **离散整数 + 确定性**：新增 CD/DR 状态全离散整数（B.2）；同种子对拍逐字节复现。
- [ ] 7.5 **duel-local 或正确 Clone**：CD/DR 状态若 duel-local 则不需入 Clone（论证之）；若引入持久态则必须序列化进 `World.Clone()` + `CultivationState`（B.3 off 逐字节回放安全）。
- [ ] 7.6 **off 逐字节 + C2/C3 不退**：off 不调 DuelEngine（SparAction ON 分支才调），逐字节守；C2 碾压单调、C3 辅助豁免不退。
- [ ] 7.7 **CC 碾压率降**：多 seed 长跑，带强控路的碾压对拍占比较 balance-004 后进一步降（sim gate 复用 `SparStompRateTests` or 新增；量化目标实现时定，勿硬编造）。

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

---

## QA Test Cases

*Logic 自动化测试 spec。对拍确定性，无需 RNG mock。*

- **AC-1（7.1 硬冷却）**
  - Given：控制者带 `Control` 技能，CD=2
  - When：连续回合对拍
  - Then：控制不能每回合重挂——两次成功控制间隔 ≥ ControlCooldown 回合
  - Edge：CD=0（退化为无冷却，向后兼容检查）
- **AC-2（7.2 抗性递减）**
  - Given：同目标反复受同类控制，DRStep=1，baseTurns=1
  - When：第 2 次控制
  - Then：TurnsRemaining 降为 0（免疫），被控方该回合可行动
  - Edge：不同 key 的控制不共享 DR 计数
- **AC-3（7.3 博弈窗口）**
  - Given：yin_xiu(mihun) vs 势均对手
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
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: balance-006（C1 判据 done，确认 CC 是残余碾压根源之一）；balance-004（切磋行为侧 done）
- Unlocks: CC 机制健康化（消 stun-lock）；balance-cross epic 战斗机制侧收敛
