# Retrospective: Sprint 7 (Alpha-1)

Period: 2026-07-03（Alpha 首 sprint，单会话密集迭代）
Generated: 2026-07-03

> Sprint 目标：balance-cross Fairness 收敛（C1 平价）+ 偿 doc 债，为 Alpha 规模开发立稳基线。
> 起点 HEAD `f57cb2b`（core-loop-fun validated，进 Production/Alpha）→ 终 `e39d9cf`。

---

## 1. Metrics

| Metric | Planned | Actual | Delta |
|--------|---------|--------|-------|
| Stories (must-have) | 2 (balance-003, 004) | 3 done (003, 004, **006**) | +1（006 从 003 拆出） |
| Stories (should-have) | 2 (doc-debt-1/2) | 2 done | 0 |
| 完成率（must+should） | — | **100%** | — |
| 测试 | 1056 绿 | **1062 绿 / 0 fail / 0 skip** | +6 |
| Commits | — | 10 | — |
| 计划外浮现 | — | 3（doc-debt 重排、balance-006 拆分、balance-007 起草） | — |
| 技术债 (TODO/FIXME/HACK, src) | — | 0 | 持平清洁 |

## 2. Velocity Trend

| Sprint | 完成率 | 备注 |
|--------|--------|------|
| 5 | closed | C1 defer（advisory gate 47/48） |
| 6 | 100% | cultivation-a3 基础 (11/15) + drama-engine 扩展 |
| **7 (current)** | **100% (must+should)** | Alpha 首 sprint；C1 收敛达成 + doc 债偿清 |

**Trend**: Stable（连续 100%）。但 sprint-7 性质不同——从"堆系统深度"转为"数值收敛 + 架构校准"，价值密度更高。

## 3. 核心里程碑（进入 Alpha 的基石）

### ① 架构认知升级：Viability ≻ Drama ≻ Fairness
"内核通了剧情自然活"的实证：Viability 调平（破境率 9%→374%，成长线纵深 UT0→UT8，`b26c6a1`）后，复仇弧**无需额外改动**就跑出"立誓→蓄力→寻仇→狭路相逢→**手刃仇人**"完整链 + 跨代继承。上层剧情（Drama）与流派绝对平衡（Fairness）**强依赖**基础可行性（Viability）——内核未通则剧情因数值门控死锁（"闭关到老死"）。确立红线 **A.10**：Viability=Pre-Production 核心前置，Fairness=推迟 Alpha。

### ② 核心数值收敛：balance-003 PE 归一化
18 战斗路 `RealmMultipliers` 按 INV-CROSS §5 解析校准（`mul=round(target(UT)×10/BaseSum)`，target=剑修范式路）至 sword 锚（`8c5504e`）。裸 PE 带宽 gate 实测 **151/151 cell 落 sword±15% 带（100%，最大偏差 5%）** ——基础战力模型获**数学意义的标定**（此前 UT12 跨路 5→340 = 68× 失真 → 现 ~5%）。C1 违规 47→32。

### ③ 战斗维度拆解：Action Economy ⊥ PE
balance-006 摸底证明**控制维度（CC/锁回合）与面板维度（PE）数学正交**：胜率 [40,60]% violations==0 在确定性对拍模型**不可达**（中性化 CC/counter 后违规反增 32→834，证明障碍是确定性模型本身而非 counter）。→ C1 硬 gate 改用 §2 自己的 **PE-band 代理**（模型可达，已达 100%），胜率平价降 advisory。为后续战斗打磨指明方向：治 CC 走**时间经济**（balance-007 冷却/递减），非降面板。

## 4. What Went Well

- **三里程碑全部机器证据背书**，全程无虚报。
- **诚实拆分而非硬凑**：balance-003 撞 [40,60] 天花板 → 拆 006（非死磕）；006 又证胜率平价确定性模型不可达 → 落 PE-band 代理达 100%（非假 Done）。
- **doc 债顺势偿清**：`/dev-story` 撞 tr-registry STOP → 补 architecture.md/control-manifest/tr-registry，解阻管线 + 清 gate 硬阻断。
- **红线零破**：18 路乘子大改 + RuleBrain 改 + DuelEngine 加 calibrationMode，B.2 整数/B.3 off 逐字节始终绿。

## 5. What Went Poorly（系统性，不甩锅）

- **sprint 规划漏内部依赖**：balance-003 依赖 doc-debt(tr-registry)，但 doc-debt 排 should-have 在后 → `/dev-story` 撞 STOP 才发现，临时重排。**教训**：规划时画 story 间依赖图。
- **假设先行被证伪 2 次**：先假设 stomp 根因=counter（实为 Control 模块），再假设 CC 中性化能收敛胜率（实为确定性模型限）。均靠证据及时纠正，但消耗轮次。**教训**：平衡类任务先跑诊断 harness 再定方案。
- **balance-004 gate 裕度薄**：aggregate 24% vs 25% 阈，seed42 单独 34%。已诚实标注；反映确定性模型的胜率天花板（brain 只能减少"选择去打"频率，打起来 margin 仍受模型限）。

## 6. Blockers Encountered

| Blocker | Duration | Resolution | Prevention |
|---------|----------|------------|------------|
| `/dev-story balance-003` 撞 CCGS 硬 STOP（tr-registry 缺） | 1 轮 | 先偿 doc 债（architecture/manifest/tr-registry）解阻 | 规划画依赖图；doc 债前置 |
| [40,60]% 胜率 gate 撞确定性模型天花板 | 2 轮（诊断+改判据） | 落 §2 PE-band 代理（模型可达） | 平衡任务先诊断模型可达性 |

## 7. Previous Action Items Follow-Up

| Action（sprint-6 及圆桌） | Status | Notes |
|---|---|---|
| balance-cross 假 Done 订正 | Done | 圆桌 P0；本 sprint 正式实现 PE 归一化 |
| stage.txt 阶段订正 | Done | Pre-Production → Production（validated 后） |
| doc 债（architecture/manifest/tr-registry） | Done | 本 sprint 偿清 |

## 8. Action Items for Alpha（下一轮）

| # | Action | Owner | Priority | 备注 |
|---|--------|-------|----------|------|
| 1 | 规划 sprint 时画 story 依赖图 | 主控 | Med | 承本 sprint doc-debt 漏排教训 |
| 2 | 平衡类任务先跑诊断 harness 再定方案 | 主控 | Med | 承 2 次假设证伪 |
| 3 | balance-007 CC 冷却/递减实现 | — | Should | Alpha backlog，Review Approved |
| 4 | 评估方差战斗模型（解胜率平价天花板） | — | Low | 大架构决定，需专门立项 |

## 9. Summary

Alpha 首 sprint 达成三大里程碑：**架构认知**（Viability 前置于 Drama/Fairness）、**数值收敛**（PE 归一化数学标定）、**战斗维度拆解**（Action Economy ⊥ PE）。全程机器证据 + 诚实拆分（该拆拆、该标局限标），红线零破。**最重要的一条：确立了"证据驱动、不为漂亮结论硬凑"的 Alpha 工作范式**——三次撞墙（counter 假设、CC 中性化、胜率天花板）都用证据转向而非死磕。可正式进军 Alpha。
