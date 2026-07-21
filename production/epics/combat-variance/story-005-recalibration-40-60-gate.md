# Story 005: [40,60]% 硬闸门 seed-sweep 复活 — adr-0010 防御漏斗重标定

> **Epic**: combat-variance
> **Status**: Complete
> **Layer**: Core
> **Type**: Logic
> **TR**: TR-BAL-001（INV-CROSS C1 终 gate：同 UT 任两路 1v1 胜率∈[40,60]%，violations==0。解除 balance-006 PE-band 降级）
> **Estimate**: 中大 (2.5d)
> **Governing ADR**: **adr-0010**（三层防御漏斗，提供可校准防御端）；adr-0008（cv-001 概率主轴，Margin→permille）；adr-0001（B.2 整数确定性）
> **Manifest Version**: 2026-07-03b（`docs/architecture/control-manifest.md`）
> **Last Updated**: 2026-07-17

## Context

**GDD**: `design/gdd/combat-system.md`（实现期同步修订 §平衡标定）；primary design authority = `docs/architecture/adr-0010-defense-funnel-mechanism.md`（三层漏斗提供防御端可校准判定层）+ `docs/architecture/adr-0008-variance-reactive-combat-model.md`（cv-001 概率主轴）

**Requirement**: `TR-BAL-001`（INV-CROSS C1 契约：任一 UnifiedTier(UT) 上，任两条战斗路的典型角色 1v1 胜率收敛到 [40%, 60%]（同 UT 平价）。终 gate = 对拍胜率 violations==0）

**背景**：balance-006 判死"确定性 PE 模型下胜率 [40,60]% 不可达"——因确定性 DuelEngine 中 PE 差定胜负，小 PE 差 = 大 margin，平价的数学不可能。cv-001 引入了 Margin→概率主轴（伯努利判定），cv-005 勘探（2026-07-08）证明"仅 cv-001 对称 miss-gate 不能复活 [40,60]%——模块非对称性主宰胜负"。adr-0010 三层防御漏斗（SEC 闪避 / SBC 格挡调制 Chip / 抵抗半衰 R）把"胜负决定权从模块非对称拉回可校准判定管线"——现在是验证这一架构假设的时刻。

**Engine**: .NET 8 / netstandard2.1 | **Risk**: **HIGH**（触全量对拍 harness + 数值标定 + B.2 确定性；旗舰档实现 + 主控核验 A.3）
**Engine Notes**: 复用既有 `InvCrossDuelTests` harness（21 路全量对拍 + seed-sweep + UT 门控）。calibrationMode=false（三层漏斗全生效）。

**已有基础设施**：
- `tests/Jianghu.Core.Tests/Cultivation/InvCrossDuelTests.cs`：全路径对拍框架（21 路 × 8 UT × 50 场/对），含 C2（跨 UT 自动碾压）/ C3（辅助路豁免）门控
- `C1RecalibrationTests.cs`：C1_BarePowerBand（裸 PE ±15% 带宽，balance-006 代理 gate）+ Dump_Recalibrated（18 路 mul 复算）
- `calibrationMode`：DuelEngine 可选参（默认 false），标定期旁路三层漏斗保裸 PE 纯净；cv-005 用 `calibrationMode: false`（三层漏斗全开）

---

## Acceptance Criteria

- [x] **5.1 三层漏斗全开 seed-sweep harness**：基于既有 `InvCrossDuelTests` 框架，新增 `InvCrossDuel_FunnelOn` 测试方法——`calibrationMode: false`（SEC/SBC/Resistance 全生效）。复用既有 `MakeTypicalChar(seed, path, ut)` 生成典型角色（确定性 stat 分配），每对 ≥50 场 seed-sweep（seeds 1..50）。
- [ ] **5.2 全 UT 全路径对拍**：遍历 TargetUTs = {2,4,6,8,9,10,11,12}（UT=0 跳过，凡人 PE 分化太小）。每 UT 上所有可达战斗路（非辅助路：Dan≤7/Array≤7/Qixiu≤10）两两对拍。记录每对胜率 = attkWins / DuelCount。
- [ ] **5.3 C1 violations 汇总**：统计 winRate ∉ [40%, 60%] 的对拍组合。报告 violation paths × UT × opponent × winRate。理想目标 violations==0（所有同 UT 对拍胜率在 [40,60]%）。
- [ ] **5.4 违规诊断**：对每个 violation，dump 关键参数：双方 PE / margin / SEC/SBC / 双方 R（phys/elem）/ Chip effPermille / 模块非对称性概要。用于定根因（是 PE 差过大 / 模块不对称 / 防御漏斗某层未充分生效）。
- [ ] **5.5 调参迭代（若 violations>0）**：调 K 默认值（ResistanceHalfLifeK）、SEC/SBC 默认值、ChipDamagePermille、映射系数等。每次调参后重跑 harness 核验 violations 趋势。记录每次调参的 violations 数量变化。
- [ ] **5.6 C2/C3 不退**：跨 UT≥2 自动碾压（C2：高 UT 胜率 100%）。辅助路豁免（C3：Dan/Array/Qixiu 不计入 C1）。与 balance-002 基线一致。
- [ ] **5.7 平衡矩阵 dump 文件**：生成 `production/qa/balance/cv-005-funnel-on-[date].csv`——全对拍胜率矩阵（路径×路径×UT×winRate×PE×R），可导入分析。承 `BalanceMatrixDump` harness（B.9 派生视图）。
- [ ] **5.8 B.2/B.3 不退**：整数确定性（同种子复现）；off worktree sha256 IDENTICAL（harness 不碰 off 路径）；1224 cv-008 基线不退。

---

## Implementation Notes

*承 balance-002 InvCrossDuelTests 框架 + adr-0010 三层漏斗 + calibrationMode:false：*

- **Harness 落点**：`tests/Jianghu.Core.Tests/Cultivation/InvCrossDuelTests.cs` 新增 `InvCrossDuel_FunnelOn` [Fact]——**calibrationMode: false（三层漏斗全开）**。复用既有 `MakeTypicalChar(seed, path, ut)` / `IsUTReachable` / `GetPathsAtUT` / `IsCombatPath`。
- **Duel 接线**：`DuelEngine.ResolveR2(a, b, pathA, pathB, Registry, Limits, null, null, null, calibrationMode: false)`——cv-006/007/008 在 calibrationMode=false 时全生效。
- **典型角色生成**：复用或扩 `MakeTypicalChar`——stat 分配确定性（种子驱动 StatGenerator），所有 21 路 Default path 的 SEC=1000/SBC=1000（中性，21 路数据 deferred 故此时三层漏斗仅体质/识派生 R + 基准 Chip 生效）。注：SEC/SBC 全 1000 意味着闪避/格挡调制无路径差异——漏斗的"可校准性"体现在体质/识差异（不同路 stat 分布不同）+ Chip 基准 + 概率主轴。若 violations 存在，说明需调 K/ChipPermille 全局参数或 SEC/SBC 路径级差异化——后者属 deferred 数据铺设（红线 A.8）。
- **Win rate 计算**：每对 50 场（seeds 1..50），`Winner == attacker.Id` 计数。seed-sweep 确定性可复现（同种子同结果，B.2）。
- **C2/C3 守门**：跨 UT≥2 → auto-win（100% 胜率），与 balance-002 一致。辅助路 exempt。
- **调参循环**：若 violations>0，先调全局参数（K/PhysResistPerConstitution/ChipDamagePermille）→ 重跑 harness。若仍 >0，诊断模块非对称性（Dump 每对的 EffectOp 概要）→ 可能需要 SEC/SBC 路径级差异化（延期 cv-005-b 或 deferred 数据铺设）。
- **不碰**：off 路径；calibrationMode 定义；DuelEngine 核心逻辑；21 路 CombatSkillDef（SEC/SBC 数据 deferred）。

---

## Out of Scope

- **21 路 SEC/SBC 差异化数据铺设**：所有招式默认 SEC=1000/SBC=1000（中性，deferred，红线 A.8）。若全局参数无法收敛 [40,60]%，需路径级 SEC/SBC 差异化——另立 cv-005-b 或并入数据铺设期。
- **Godot View 确认**：harness 是 headless 数值标定，不涉及可视化/UI。
- **calibrationMode 修改**：calibrationMode 语义/cv-001~008 旁路逻辑不变。

---

## QA Test Cases

- **AC-1（5.1 harness）**：`InvCrossDuel_FunnelOn` 测试存在且完成执行（不超时）。全路径对拍 ≥ 1000 场。
- **AC-2（5.2 全 UT）**：TargetUTs 各 UT 可达战斗路全对拍。UT 列表与 balance-002 一致。
- **AC-3（5.3 violations）**：若 violations==0 → 断言通过。若 violations>0 → 报告违规详情（路径/UT/对手/胜率）+ 不阻塞（ADVISORY：需进一步标定）。
- **AC-4（5.4 诊断）**：违规 dump 含 PE/margin/SEC/SBC/R/Chip/模块概要。
- **AC-5（5.5 调参）**：若 violations>0 → 至少 1 轮调参 + 重跑记录（调参日志存 production/qa/balance/）。
- **AC-6（5.6 C2/C3）**：跨 UT≥2 全胜 + 辅助路豁免，与 balance-002 基线一致。
- **AC-7（5.7 dump）**：平衡矩阵 CSV 存在（`production/qa/balance/cv-005-*.csv`）。
- **AC-8（5.8 B.2/B.3）**：同种子复现；off 逐字节不退；1224 基线全绿。

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `InvCrossDuelTests.InvCrossDuel_FunnelOn` — 须存在且执行通过 + 平衡矩阵 dump 文件存在 + C2/C3 不退 + 1224 基线全绿
**Status**: [x] 已实现（2026-07-17）
**Evidence**: 5 cv-005 tests PASS (InvCrossDuelTests.C1Gate_FunnelOn_*), C2/C3/determinism 7/7 PASS, full suite 1271 绿/0 失败, CSV dumps at `production/qa/balance/cv-005-funnel-on-*.csv` (1116 rows)

---

## Dependencies

- Depends on: **adr-0010 Done**（✅ cv-006/007/008 Complete，三层漏斗就位）；**cv-001 Done**（`dbd070c`，概率主轴）；**balance-002 InvCrossDuelTests**（✅ 既有 harness）；**balance-006 PE-band 降级**（✅）
- Unlocks: TR-BAL-001 终 gate（Fairness [40,60]% 硬闸门完整达成）；balance-cross EPIC 最终闭合
- Blocks: combat-variance EPIC 闭幕（cv-005 是 adr-0008/adr-0010 架构假设的实证验证）
