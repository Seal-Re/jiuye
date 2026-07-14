# cv-005 三层防御漏斗重标定报告

**Date**: 2026-07-14
**Epic**: combat-variance
**Status**: 数据驱动结论——全局参数天花板 ~57% 违规率，需路径级 SEC/SBC 差异化

## 背景

adr-0010 三层防御漏斗（SEC 闪避 / SBC 格挡调制 Chip / 抵抗半衰 R）闭环后，cv-005 harness（`InvCrossDuelTests.C1Gate_FunnelOn_*`）进行全路径同 UT seed-sweep 对拍，目标 TR-BAL-001 [40,60]% 硬闸门 violations==0。

SEC/SBC 全默认 1000（中性，21 路数据 deferred），仅有 R（体质/识派生 + BodyArt 加成）+ cv-001 概率主轴 + cv-003 Chip 穿透在生效。

## 参数扫面对比

| 实验 | K | PhysR/C | 违规/总数 | 违规率 | Δ vs baseline |
|---|---|---|---|---|---|
| baseline | 500 | 50 | 822/1115 | 73% | — |
| K 激进 | 200 | 50 | 806/1115 | 72% | -1.9% |
| PhysR 10x | 500 | 500 | 737/1115 | 66% | -10.3% |
| PhysR 20x | 500 | 1000 | 641/1115 | 57% | -22.0% |
| 组合(K+PhysR) | 200 | 500 | 645/1115 | 57% | -21.5% |

Harness: `InvCrossDuelTests`（21 战斗路 × 8 UT × 50 场/对 = 1115 对，calibrationMode=false）
全量: 1246 绿

## 数据驱动结论

1. **PhysResistPerConstitution 是主力杠杆**（非 K 单独）——放大体质/识差异显著降低违规
2. **边际递减明确**——10x→20x 仅 -9%，天花板 ≈55-60%
3. **K 单独效果微弱**（-1.9%）——R 已足够大时 K 变化不敏感
4. **组合效果接近加性**——K=200+PhysR=500 ≈ PhysR=1000，无乘法协同

## 诚实判定

**全局参数空间已充分探索**。防御漏斗从 73% 降到 57%（-22%）证明架构有效，但对称性（双方 stat 同分布 → R 近似对称 → 减伤对称）限制天花板。

**完全收敛 [40,60]% (violations==0) 需要**：
1. 路径级 SEC/SBC 差异化数据（哪些路径"必中暗器"？哪些"重锤破防"？——属 game design，红线 A.8 deferred）
2. 或结构性非对称机制（如路径特有 OnUse/OnDefend 模块差异当前被 cv-001 概率部分中和）

## 文件

- harness: `tests/Jianghu.Core.Tests/Cultivation/InvCrossDuelTests.cs`（`C1Gate_FunnelOn_*` × 5）
- story: `production/epics/combat-variance/story-005-recalibration-40-60-gate.md`
