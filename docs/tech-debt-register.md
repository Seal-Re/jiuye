# Tech Debt Register

> 追踪已知技术债 + advisory 偏差。每条附来源 story + 依赖/解法方向。
> 红线 A.8（诚实标 defer/债，不静默丢）· A.3（机器证据）。

## 平衡 / 确定性模型

- **2026-07-06** (balance-003 C1 收敛): AC 3.7 反扁平多样性（M=3 对 |win%−50%|∈[5,10]）无测试且当前不可测 — 根因与 AC 3.4 同：`DuelEngine` 确定性对拍无方差（HP=PE、dmg=PE/10），同 UT 胜率坍缩为 ~100%/0%，中间带宽 [40,60]% 及 [5,10] 偏差带均结构性不可达。**解法方向**：方差战斗模型立项（sprint-9 候选，需先出 GDD/ADR 定护栏）。tracked from `production/epics/balance-cross/story-003-c1-convergence-40-60.md`
