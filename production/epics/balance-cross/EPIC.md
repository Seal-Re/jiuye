# Epic: 平衡标定 INV-CROSS

**Layer**: Core
**Status**: Done（2026-07-06 收官；DoD 4/5 直达 + C1 代理达成，见下「收官记录」）
> ⚠️ 2026-06-26 圆桌订正（红线 A.3/A.4）：此前误标 Done，但 DoD 5 项全空、契约 C1[40,60]% 未兑现。
> 实证：测试仅 `C1Gate_…Within_35_65`（advisory，`KnownViolationBaseline=47/48` 钉为已知违规放行），
> 全仓无 `Within_40_60` 测试；story-003（[40,60]% 硬闸门）git `76a9a17` 证实 Deferred。
> 真实态 = balance-001/002 派生视图+advisory gate 已建；balance-003 收敛硬闸门未做。详见 docs/reports/宏观对账圆桌纪要-2026-06-26.md §1。
**GDD**: design/gdd/xxx.md（P8 补）或 — ；深度源 docs/legacy-specs/specs/2026-06-14-v1.2-B5-平衡标定INV-CROSS-design.md
**Governing ADRs**: None yet（P8 增量补）
**Engine Risk**: LOW（.NET 8 纯整数）
**Created**: 2026-06-15（迁自 TASKS.md）

## Summary
平衡标定 INV-CROSS（跨路战力当量）——🔴 最大功能缺口，同 UT 24-70× 失衡。

## Scope
- INV-CROSS 契约 C1：战斗路同 UT 胜率 [40,60]%
- C2：碾压单调
- C3：辅助路豁免但 UT 诚实
- 解析校准 mul = target×10 / BaseSum
- 辅助路 UT 重锚解 A1.4

## Dependencies
**Unblocked by**: —
**Blocks**: combat-r2 / story-005（辅助路 UT 重锚）+ cultivation-a1-rest（寿元/劫）

## Definition of Done
- [~] INV-CROSS 契约 C1 同 UT 胜率 gate（[40,60]%）— **确定性对拍下数学不可达**（无方差 → 胜率坍缩 ~100%/0%）；**代理达成**：PE-band 硬 gate（sword±15%，151/151 入带）+ calibrationMode 中性化 counter/压制（balance-006 @53e83f4）
- [x] C2 碾压单调 — `InvCrossDuelTests.C2Gate_UTGap2OrMore...`（UT gap≥2 → 高 UT ≥80%）
- [x] C3 辅助路豁免但 UT 诚实 — `InvCrossDuelTests.C3Gate_AuxiliaryPaths...`（Dan≤7/Array≤7/Qixiu≤10）
- [x] 解析校准 mul = target×10 / BaseSum — `C1RecalibrationTests`（18 路 §5 校准 @8c5504e，C1 违规 47→32）
- [x] 辅助路 UT 锚 — 辅助路 C3 豁免 + power-单调护栏（解 A1.4 UT 平段整数舍入）

## Notes
设计完 sha 336280d，范围限标定。

## Stories（story 级指针；机器可读状态在各 story 文件 Status 字段）

> sprint-7 已 closed，balance-007/008 属 epic 级 backlog（下个 sprint 规划时拉入），不塞闭合 sprint 的 sprint-status.yaml。

- **story-001** balance-matrix-dump — Done（派生视图 harness）
- **story-002** inv-cross-duel-gate — Done（advisory gate）
- **story-003** c1-convergence-40-60 — **Done @ 8c5504e**（PE 归一化 18 路 §5 校准；AC 3.4 拆 006 已 Done，AC 3.7 advisory→tech-debt；`/story-done` 闭合 2026-07-06，1087 绿）
- **story-004** spar-stomp-smoothing — Done（9b73358，碾压率 33%→24%）
- **story-005** INV-CROSS 派生视图刷新 + /balance-check — **Deferred**（nice-to-have，非 epic DoD 条件；派生 harness 已在 `BalanceMatrixDumpTests`/`BalanceCrossHarness`；`/balance-check` 需新建 `design/balance/` 数据层，留观测性立项或方差模型时一并做）
- **story-006** c1-counter-exemption — Done（53e83f4，calibrationMode + PE-band gate）
- **story-007** control-cooldown-dr — **Done @ 9592d41**（CD/DR 消 turns≥2 stun-lock；AC 7.1-7.6 ✅，7.7 指标正交另证）
- **story-008** turns1-control-nop — **Done @ d385cd8**（裁决=方案C 分级语义：turns=1 即时打断/turns≥2 长控不变；1087 绿(+10)，balance-007 CD/DR 不退）

## 收官记录（2026-07-06）

**Status: Designed → Done**（`/story-done` balance-003 闭合触发 epic 收官核验，旗舰档主控独立核验 A.3）。

**DoD 达成 4/5 直达 + 1 代理**：C2 单调 / C3 豁免 / §5 解析校准 / 辅助路 UT 锚 = 直达（named test 溯源见上）。C1 [40,60]% 硬闸门 = **确定性对拍下数学不可达**（`DuelEngine` 无方差：HP=PE、dmg=PE/10 → 同 UT 胜率坍缩 ~100%/0%），经 balance-006 裁决改 **PE-band 代理**（sword±15%，151/151 入带）+ calibrationMode 中性化。此为模型限制下的诚实替代，非缺口静默放行。

**story 交付**：001/002（派生视图+advisory gate）· 003（PE 归一化 18 路）· 004（碎压平滑 33%→24%）· 006（C1 counter 中性化+PE-band gate）· 007（控制 CD/DR）· 008（turns=1 即时打断）全 Done。005（派生视图刷新+/balance-check）Deferred（nice-to-have，非 DoD 条件）。

**遗留（A.8 诚实记录）**：
- AC 3.7 反扁平多样性 — `docs/tech-debt-register.md`，跟踪至方差战斗模型立项。
- balance-005 观测性增强 — 待方差模型或独立观测立项一并做。
- **根因单**：C1 硬闸门 + AC 3.7 同源于确定性对拍无方差。**方差战斗模型**（sprint-9 候选）= 真正解锁 Fairness [40,60]% 天花板的架构决定，需先出 GDD/ADR 定护栏。
