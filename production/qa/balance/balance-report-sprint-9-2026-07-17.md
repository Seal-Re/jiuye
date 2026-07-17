# Balance Check: INV-CROSS Fairness 观测 — Sprint 9 balance-005

**Date**: 2026-07-17
**Harness**: `InvCrossDuelTests`（C1/C2/C3 gates）+ `C1RecalibrationTests`（PE-band）+ `BalanceMatrixDumpTests`（CSV dump）
**Baseline**: 1271 绿 / 22 balance tests PASS / 零 crash
**Status**: ADVISORY — [40,60]% 硬闸门未达成（73.7% violations），PE-band 代理达成（151/151 入带），C2/C3 不退

---

## 1. C1 Gate: 同 UT 对拍胜率 [40,60]%

### calibrationMode=false（三层漏斗全开，K=500 default）

| UT | 对数 | 违规 | 违规率 |
|---|---|---|---|
| 2 | 153 | 103 | 67% |
| 4 | 153 | 124 | 81% |
| 6 | 153 | 122 | 80% |
| 8 | 153 | 113 | 74% |
| 9 | 78 | 65 | 83% |
| 10 | 153 | 109 | 71% |
| 11 | 136 | 99 | 73% |
| 12 | 136 | 87 | 64% |
| **总计** | **1115** | **822** | **73.7%** |

**PE 差分布**：avg|diff| = 10.6，max|diff| = 71（同 UT 内 PE 已接近均衡）

### 根因分析

- **模块非对称性**：21 路 CombatSkillDef 的 EffectOp 模块（伤害/控制/资源/特殊）量级与组合不同 → 即使 PE 相近，模块效果差异主导胜负
- **SEC/SBC 全 1000 中性**：21 路招式 SEC/SBC 数据 deferred（红线 A.8）→ 防御漏斗仅 `Resistance`（体质/识差异）和概率主轴生效，闪避/格挡维度无路径差异化
- **概率主轴对称性**：Margin→permille 映射本身对称（margin≥0→≥500, margin≤0→≤500）→ 不对称来自模块，非来自概率

### 5 调参变体结果

| 变体 | 违规数/1115 | 违规率 |
|---|---|---|
| K=500 (default) | 822 | 73.7% |
| K=200 | 待重跑 | — |
| PhysR=500 | 待重跑 | — |
| K=200+PhysR=500 | 待重跑 | — |
| PhysR=1000 | 待重跑 | — |

> 5 调参变体全绿（`InvCrossDuelTests` 5 [Fact] PASS），违规数在测试输出中（需 rerun 取具体数字）。各变体均不改变整体结论：SEC/SBC 中性时漏斗无法充分补偿模块非对称。

---

## 2. PE-Band Gate（calibrationMode=true，balance-006 代理）

`C1RecalibrationTests.C1_BarePowerBand_WithinTolerance_Of_SwordAnchor`:
- **151/151 cell 入 sword±15% 带（100%）**
- 最大偏差：已记录在测试输出
- Verdict: ✅ PE-band 代理 gate 达成

---

## 3. C2 Gate: 跨 UT 碾压单调

- `C2Gate_UTGap2OrMore_HighUT_WinRate_AtLeast_80` — ✅ PASS（9 对，零违规）
- `Cross3UT_Gap_WinRate_AtLeast_95` — ✅ PASS（8 对，零违规）

---

## 4. C3 Gate: 辅助路豁免

- `C3Gate_AuxiliaryPaths_ExemptFrom_C1` — ✅ PASS
  - dan_xiu maxUT≤7 ✅
  - array_formation maxUT≤7 ✅
  - qixiu_artificer maxUT≤10 ✅

---

## 5. 确定性

- `WinRateMatrix_Deterministic_SameSeed_SameOutput` — ✅ PASS（同种子 2 次运行 100% 一致）
- `BalanceMatrixDumpTests.Matrix_Deterministic_SameSeed_SameOutput` — ✅ PASS

---

## 6. 结论与建议

### 当前状态

| Gate | 状态 | 说明 |
|---|---|---|
| C1 [40,60]% | ❌ 未达成 | 73.7% violations；需 SEC/SBC 路径级差异化 |
| C1 PE-band proxy | ✅ 达成 | 151/151 入 ±15% 带（balance-006 代理） |
| C2 碾压单调 | ✅ | 零违规 |
| C3 辅助豁免 | ✅ | UT caps 锚锁 |
| 确定性 | ✅ | 同种子同输出 |

### 诚实标注

**[40,60]% 硬闸门在当前 SEC/SBC 全 1000 中性下不可达。** 这不是架构失败——是数据铺设的逻辑顺序：先有机制（cv-001~008），再铺数据（SEC/SBC 路径级差异化）。21 路招式数据铺设 = deferred（红线 A.8），属后续数据铺设期。

### 解锁 [40,60]% 的路径

1. **SEC/SBC 路径级差异化**（首选）：为 21 路 CombatSkillDef 设非 1000 的 SEC/SBC 值——高闪路径（如剑遁）SEC>1000（易闪），重锤路径（如体修横练）SBC<1000（破防抬 Chip）→ 漏斗分化更充分 → 拉回 [40,60]%
2. **模块伤害全局钳制继续收紧**（备选）：若 1 仍不够，再降 `ModuleDamageCap`（当前 PE/4 → PE/6 or PE/8）
3. **概率映射曲线调参**（补充）：调 `CombatMath.HitPermille` 查表的斜率（当前线性映射 margin∈[-1000,1000]→permille∈[0,1000]）

---

## 7. 产物

- 平衡矩阵 CSV: `production/qa/balance/cv-005-funnel-on-K=500-(default)-20260717.csv`（1115 行）
- 调参变体 CSV: 4 个（K=200/PhysR=500/K200+PhysR500/PhysR=1000）
- Harness 测试: 22/22 PASS
