# Story 001: 主动交锋概率拦截最小闭环（Active-Clash Variance）

> **Epic**: combat-variance
> **Status**: In Review
> **Layer**: Core
> **Type**: Logic
> **TR**: TR-BAL-001（`docs/architecture/tr-registry.yaml`；本 story 铺概率内核，[40,60]% 硬闸门最终兑现在 cv-005）
> **Estimate**: 中大 (2.5d)
> **Manifest Version**: 2026-07-03b（`docs/architecture/control-manifest.md`）
> **Last Updated**: 2026-07-06

## Context

**GDD**: `design/gdd/combat-system.md`（实现期同步修订）；深度源/权威 `docs/architecture/adr-0008-variance-reactive-combat-model.md`（决策①②③④ + 附录A）
**Requirement**: `TR-BAL-001`（同 UT 胜率 [40,60]%——本 story 铺概率化内核，使胜率成可校准曲线；硬闸门断言在 cv-005）

**ADR Governing Implementation**: **adr-0008**（primary，方差主轴 α + permille 查表 + Duel=9 流 + duel-local）· adr-0001（整数确定性 B.2）· adr-0002（Modules 工厂 B.9）· adr-0003（off 逐字节 B.3）
**ADR Decision Summary**: PE 差经整数查表映射为伯努利概率；`duelRng` 派生自 `cultRng.Split(Duel=9)`，duel-local 不入 Clone；概率判定只插"主动交锋"，DoT/压制原样保留。

**Engine**: .NET 8 / netstandard2.1 | **Risk**: **HIGH**（触 `Jianghu.Cultivation`：禁浮点 IL 扫描 / off 逐字节 / 新 PRNG 流；一处细微 bug 破确定性或平衡且难查。B.7 旗舰档实现 + 主控独立核验 A.3）
**Engine Notes**: 无 post-cutoff API。新增 `RngStreamIds.Duel=9`（append-only，绝不复用 1-8）。`Math.Clamp(int,...)` 整数重载合法（`IMapGenerator.cs` 已用先例）。

**Control Manifest Rules (Core 层)**:
- Required: 战斗效果经 Modules 工厂；新 PRNG 消费者走 `IRandom.Split`（禁裸 `new` PRNG）；新增状态离散整数；确定性（同种子逐字节）。
- Forbidden: 浮点（B.2，禁浮点 Sigmoid → 用整数 permille 查表）；裸 `new EffectOp` 七参（B.9）；`daoHeart/innerDemon` 进战力（B.5）。
- Guardrail: 改 DuelEngine 须过 off 逐字节（`Determinism/OffByteIdenticalTests`）+ IL 浮点零（`ILFloatScanner`）+ C2/C3 不退。

---

## 背景（承 adr-0008 Accepted + 用户 2026-07-06 草图 + 主控核验）

现状 `DuelEngine.ResolveR2` 全程无 RNG（`DuelEngine.cs:12` 注释"纯整数、确定性、无 RNG"）：基础伤害 = `PE/10` 每回合（`BaseDamageDivisor=10`），双方同时互扣 HP=PE → 任何 PE 优势线性累积成决定性 margin，`UT gap≥2 → auto-win`（`margin=int.MaxValue`）。实测（seed=42）切磋录充斥"差 999/差 1111"碾压，战斗沦为数值比大小。balance-006（`53e83f4`）已判死确定性对拍下 [40,60]% 不可达。

本 story 落 adr-0008 首切片：在**主动技能交锋**这一环插入 Margin→概率伯努利拦截，让胜负带悬念。

### ⚠️ 关键工程事实（2026-07-06 主控已核代码，钉死实现形态——用户草图有 3 处硬冲突，此处校正）

1. **RNG API（草图 `new DeterministicRng(seed).Next(1000)` 违 B.2，必改）**：真实端口 `IRandom`（`src/Jianghu.Core/Random/IRandom.cs`），实现 `Pcg32`。方法 = `NextInt(maxExclusive)` → `[0,maxExclusive)`。**禁裸 `new` PRNG**（ADR-0001："每个新 PRNG 消费者走 RngStreamIds.Split，不能裸 new"）。伯努利判定 = `duelRng.NextInt(1000) < p_permille`。
2. **`RngStreamIds.Duel=9` 尚不存在**：现最大 id=8（Faction）。追加 `Duel=9`（append-only，B.2 铁律"新随机流追加新 id，绝不复用既有，保 off 逐字节"）。
3. **战力/角色字段（草图 `Character.CombatPE/BaseAttack/BaseDefense` 全不存在，必改）**：真实战力经 `PowerEngine.Evaluate(cultivation, stats, pathDef, limits)`（三段式整数）算，`ResolveR2` 已在 L80-81 算好 `peA`/`peB`。Margin = `peA - peB`（攻方视角）。**不新增 Character 字段**。
4. **duel-local 铁律（守 B.3）**：`duelRng` 与任何本 story 新增计数**生活于 `ResolveR2` 作用域，非持久字段，不入 `World.Clone`/`CultivationState`**——完全比照 balance-007 `ControlLimiterState`（`DuelEngine.cs:431`："duel-local → 不入 Clone → B.3 天然安全"）。**off 更不调 DuelEngine**（off 走 legacy `SparAction`），逐字节铁律天然守住。
5. **seed 派生无挂钟**：`duelRng = cultRng.Split(RngStreamIds.Duel).Split(mix(clock, sortedId))`。`clock`=逻辑 tick，双方 CharacterId **排序**后混入（保交换律：A打B 与 B打A 同场种子）。**无 `DateTime`**（B.2 + BannedApi）。`cultRng` 需从 World 传抵 `SparAction`→`ResolveR2`（当前 `SparAction` 不持 IRandom——见 Implementation Notes 接线）。
6. **只切主动交锋，DoT/压制原样保留**（用户界定）：概率拦截插在 `ResolveExchange` 的 `PE/10` 基础伤害处（管线1）；`TickDots`（管线2 DoT）+ `ApplyEPModifiers`/`SuppressionMatrix`（管线3 压制）**一行不改**。

---

## Acceptance Criteria

> **机器证据（2026-07-06 主控独立核验 A.3）**：全量 **1102 绿 / 0 失败 / 0 跳过**（1087+15 新）；B.2 IL 浮点扫描 + B.3 off 逐字节 + cultivation 确定性轨 **27 绿**；同种子 CLI 两跑 md5 一致（`12c099f7…`）。测试文件 `CombatMathTests.cs`（10）+ `ActiveClashVarianceTests.cs`（5）。**未提交（待 /story-done 贴 sha 闭合 A.3）。**

- [x] **1.1 CombatMath 整数查表**：`CombatMath.GetSuccessPermille(peMargin, defenderPe)` → permille [1,999]，相对差分桶（scale-invariant），0→500、碾压→999、被碾压→1。✅ `CombatMathTests`（10 例：单调/钳制/极端桶/值域/scale-invariant/纯函数/防零除）。
- [x] **1.2 RngStreamIds.Duel=9**：append-only 追加；off 不构造（27 determinism 绿实证）。✅
- [x] **1.3 duelRng 确定性派生**：`cultRng.Split(Duel).Split(mix(clock,sortedIds))`，交换律（A打B=B打A 同场）。✅ `test_same_seed_produces_byte_identical_result`。
- [x] **1.4 主动交锋伯努利拦截**：`ResolveExchange` 查表→`NextInt(1000)`→ 命中/化解；DoT/控制/压制/法宝分支不经拦截（管线分离）。✅ `test_variance_changes_outcome_versus_deterministic`。
- [x] **1.5 悬念实证（后台 NPC 代掷）**：`dotnet run 42 300 --cultivation` 切磋录出现差 88/13/109 悬念对局（vs 前全 999）。✅ 弱者跨 100 种子有非零胜场且不过半（`test_weaker_side_can_win_across_seeds`）。
- [x] **1.6 B.2 + 确定性**：IL 浮点零；同种子两跑逐字节。✅ 27 determinism 绿 + CLI md5 一致。
- [x] **1.7 B.3 off 逐字节不退**：off 不调 DuelEngine，duelRng duel-local 不入 Clone。✅ `OffByteIdenticalTests` 等 27 绿。
- [x] **1.8 全量绿 + C2 不退**：1102 绿；UT gap≥2 auto-win 短路保留（C2 不退）。✅
- [~] **calibrationMode 旁路方差**（cv-005 hook）：✅ `test_calibration_mode_bypasses_variance`（标定期只测裸 PE）。

---

## Implementation Notes

*承 adr-0008 决策①②③④ + DuelEngine 现有 `ResolveExchange` 骨架：*

- **CombatMath 落点**：新建 `src/Jianghu.Core/Cultivation/CombatMath.cs`（静态类，纯整数查表）。桶表外置为整数常量数组/`LimitsConfig` 字段（承 A.10 数据驱动，勿硬编码散落）。查表用 `Math.Clamp` 钳 margin 后分桶。
- **Duel=9 落点**：`src/Jianghu.Core/Random/RngStreamIds.cs` 追加 `Duel = 9`（同行 append，注释标 append-only）。
- **拦截落点**：`DuelEngine.ResolveExchange`（`DuelEngine.cs:254`）主动攻击伤害段（`long dmg = attackerPe * Scale / BaseDamageDivisor` 附近，L267）——查表 + 伯努利 gate 决定本次交锋 `dmg` 是否生效。**Dot/Control/Suppression/Artifact 分支保持不变**（它们在同函数内但走各自路径，拦截只包裹主动直伤）。
- **duelRng 接线（最麻烦一环）**：`SparAction` 当前不持 IRandom（`SparAction.cs`）。需：① `World`/`WorldFactory` 已有 `cultRng`（`root.Split(RngStreamIds.Cultivation)`，`WorldFactory.cs:30`）→ 经 `SparAction` 构造注入（或经 `ResolveR2` 参数传入）② `ResolveR2` 内 `duelRng = cultRng.Split(Duel).Split(mix(clock,ids))`。**注意**：`SparAction` 的 off 分支（registry==null）**绝不构造 duelRng**（保 off 逐字节）。clock 来源 = `IWorldMutator.Clock`（`SparAction.Apply` 已可见 `w.Clock`）。
- **calibrationMode 复用**：balance-006 的 `calibrationMode` 参（`ResolveR2` 已有，默认 false）标定期可旁路方差（只测裸 PE），cv-005 seed-sweep 时用。
- **不碰**：削韧（cv-002）、标签门控（cv-003）、溢出/防守帧钩子/裁定优先级（cv-004）、[40,60]% 硬闸门断言（cv-005）、PowerEngine mul（balance-003 已归一化）。

---

## Out of Scope（下切片）

- 削韧副轴 Poise/Stagger + 连续格挡帧递减（cv-002）。
- DamageType 标签门控 + Chip Damage（cv-003）。
- 未命中时的防守帧钩子（本切片仅"伤害归零"占位）+ 阈值溢出 + 裁定优先级（cv-004）。
- InvCrossDuel seed-sweep [40,60]% 硬闸门断言 + 21 路概率模型重标定（cv-005）。
- Godot 侧 QTE/弹反渲染（godot-host epic）。
- 伤害带 ±X% 次轴方差（adr-0008 决策③步4；本切片先只做主轴概率拦截，伤害带留 cv-002 或并入）。

---

## QA Test Cases

*Logic 自动化测试 spec。对拍确定性，无需 RNG mock（PRNG 种子驱动）。*

- **AC-1（1.1 查表）**：Given 各档 peMargin（极小/负/0/正/极大）；When `GetSuccessPermille`；Then permille 单调 + 钳 [1,999] + 0 margin≈500 + 极端桶 999/1。Edge：`int.MinValue/MaxValue` 不溢出（先 Clamp）。
- **AC-2（1.3 派生交换律）**：Given (clock=T, idA, idB)；When A打B vs B打A 派生 duelRng；Then 同场种子（排序保对称）。Edge：同 tick 多场对拍种子不碰撞（clock+ids 混入）。
- **AC-3（1.4 拦截分流）**：Given 主动攻击 vs 纯 DoT tick；When 结算；Then 主动攻击经伯努利 gate，DoT tick 绕过（确定扣血不变）。
- **AC-4（1.5 悬念）**：Given 近等 PE 同 UT 对；When 多场 seed-sweep；Then 胜率脱离 0/100（向 50 靠），非全碾压。Edge：同种子逐字节复现。
- **AC-5（1.6/1.7 B.2/B.3）**：IL 浮点零；同种子两跑逐字节；off `OffByteIdentical` 基线不退（cultivation/drama 两轨）。
- **AC-6（1.8 C2）**：UT gap≥2 高 UT 仍 ≥80% 胜（极端桶/auto-win 短路）。
- 测试文件：`tests/Jianghu.Core.Tests/Cultivation/CombatMathTests.cs`（查表纯函数）+ `Cultivation/ActiveClashVarianceTests.cs`（拦截+派生+悬念）+ `Determinism/OffByteIdenticalTests.cs`（回归守）。

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/Jianghu.Core.Tests/Cultivation/CombatMathTests.cs` + `ActiveClashVarianceTests.cs` — 须存在且过 + off 逐字节回归守（`Determinism/`）+ IL 浮点零（`ILFloatScanner`）
**Status**: [x] 已创建 — `CombatMathTests.cs`（10 例）+ `ActiveClashVarianceTests.cs`（5 例）全绿；全量 1102 绿 + 27 determinism 绿回归守（2026-07-06 主控实测）。**待 /story-done 贴 sha 闭合 A.3**（现三项：①1102 绿 ✅ ②sha 待提交 ③测试文件存在 ✅）。

---

## Dependencies

- Depends on: **adr-0008 Accepted**（架构封闭前置门，✅）；balance-003 PE 归一化（`8c5504e`，duelRng 作用于已归一化 PE）
- Unlocks: cv-002（削韧副轴挂 duel-local）· cv-005（seed-sweep [40,60]% 硬闸门——概率内核就位后才可统计胜率）
- Blocks: balance-cross Fairness 硬闸门最终兑现（经 cv-005）
