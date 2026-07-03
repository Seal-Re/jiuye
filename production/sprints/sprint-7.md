# Sprint 7 (Alpha-1) — 2026-07-03 to 2026-07-10

## Sprint Goal
进入 Alpha 首个 sprint：攻克 #1 功能缺口 **balance-cross Fairness 收敛**（[40,60]% 同UT胜率硬闸门 + 切磋碎压平滑），并偿清 gate 硬阻断的 **doc 债**（architecture.md + control-manifest），为 Alpha 规模开发立稳基线。

> 阶段：Production（= AAA Alpha）。core-loop-fun 已 validated（`f57cb2b`，Viability 证明：破境 UT0→8 + 19 条恩怨链）。Fairness（路间对等）现名正言顺进 Alpha（承红线 A.10）。

## Capacity
- Total days: 7
- Buffer (20%): 1.4d
- Available: **5.6d**

## Tasks

### Must Have (Critical Path)
| ID | Task | Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------|-----------|-------------|---------------------|
| balance-003 | C1 收敛 [40,60]% 硬闸门 — RealmMultipliers 按 sword 锚重校准（`new_mul=old×sword_PE/path_PE`）+ 模块伤害钳制复核 | systems-designer + 主控核验 | 2.5 | — | 同UT战斗对 [40,60]% violations==0（硬闸门，替代 advisory [35,65]+47/48 基线）；C2 单调/C3 辅助豁免不退；B.2 整数/B.3 off 逐字节；全量绿 |
| balance-004 | 切磋碎压平滑 — "无势均对手则降切磋意愿（转修炼/游历）" AI 行为（Target-Selection 权重） | ai-programmer + 主控 | 1.0 | balance-003 | 碾压≥999 占比显著降（<25%，实测多 seed）；势均力敌切磋↑；RuleBrain 决策逻辑受控改；off 逐字节 |

### Should Have
| ID | Task | Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------|-----------|-------------|---------------------|
| doc-debt-1 | 合并 `docs/architecture/architecture.md`（内容散在 CLAUDE.md §F / 3 ADR / agent-guide） | technical-director | 0.8 | — | architecture.md 存在含实质内容；Foundation/Core 层覆盖 |
| doc-debt-2 | `docs/architecture/control-manifest.md`（从 3 Accepted ADR 提取层规则） | technical-director | 0.7 | doc-debt-1 | control-manifest 存在；每 ADR 规则可溯 |

### Nice to Have
| ID | Task | Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------|-----------|-------------|---------------------|
| balance-005 | INV-CROSS 派生视图刷新 + `/balance-check` 跑通（Fairness 观测） | 主控 | 0.5 | balance-003 | BalanceMatrixDump 反映重校准后 PE；平衡报告生成 |

## Carryover from Previous Sprint
| Task | Reason | New Estimate |
|------|--------|-------------|
| （无） | sprint-5/6 已 closed；cultivation-a3/drama-engine 已 Done | — |

## Risks
| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| RealmMultipliers 重校准触发 21 路连锁失衡 | 中 | 高 | 只按 sword 锚缩放、逐路验 PE；C2/C3 gate 守；单点 fix≤3（A.7），超限标 known_issue 上报 |
| balance-003 改动破 off 逐字节 | 低 | 高 | Fairness 旋钮仅 on 路径；OffByteIdentical 门守；每步验 |
| 切磋 AI 改动扰动确定性 | 低 | 中 | 承 SelfPowerOf 模式，RuleBrain 逻辑最小改；RuleBrain 决定性测守 |

## Dependencies on External Factors
- 无（纯 Core 逻辑，headless；无外部依赖）

## Definition of Done for this Sprint
- [ ] All Must Have tasks completed
- [ ] All tasks pass acceptance criteria
- [ ] QA plan exists (`production/qa/qa-plan-sprint-7.md`)
- [ ] All Logic/Integration stories have passing unit/integration tests
- [ ] Smoke check passed (`/smoke-check sprint`)
- [ ] 全量绿 + off 逐字节 + IL 浮点扫描
- [ ] Code reviewed and merged
- [ ] Design documents updated for any deviations

---

## 备注（诚实标注）

- **lean 模式** → PR-SPRINT producer 可行性 gate 跳过（非 phase-gate，承 review-mode.txt）。
- **无 milestones/risk-register 目录** — CCGS 建议有，项目从未建；本 sprint 未擅自创建（待用户定是否 `/milestone` 立 Alpha 里程碑）。
- **重名（郑寻欢与郑寻欢）不在本 sprint** — 用户已归属表现层（Unity View，接引擎时处理）。
- balance-003/004/005 需先 `/create-stories balance-cross` 拆出正式 story 文件（当前仅 EPIC + story-003 存在）。

> **Scope check**: 本 sprint 的 balance-004（切磋平滑）超出 balance-cross 原 epic 的 C1 收敛范围（属新增 Fairness&Polish）；实现前可 `/scope-check balance-cross` 确认不蔓延。
