# Sprint 9 (Alpha-3) — 2026-07-17 to 2026-07-24

## Sprint Goal
开启 Godot 宿主接入第一段（闸口验收 + WorldBridge 最小回路），同时收束 Core 侧最后遗留（balance-005 Fairness 闭合 + cultivation-a3 全 Complete）。

> 阶段：Production（= AAA Alpha）。Sprint 8 全 Must Have Done（doc 债+设计同步+godot-host scout）。全 15 epic Done or deferred；WIP=0；1271 绿。

## Capacity
- Total days: 7
- Buffer (20%): 1.4d
- Available: **5.6d**

## Tasks

### Must Have (2.8d)
| ID | Task | Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------|-----------|-------------|---------------------|
| gh-001 | 闸口验收：Core 无死锁形式化证明（gate-check 文档） | 主控 | 0.3 | — | 闸口文档存在于 `production/gate-checks/`；证据链（1271 绿+Viability+恩怨链）完整；verdict PASS/CONDITIONS |
| gh-002 | WorldBridge + Signal 接线：Core→Godot 最小渲染回路 | technical-director | 1.5 | gh-001 | Godot 4.x .NET 项目编译+引用 Core 成功；WorldBridge Signal 广播 DomainEvent；CLI vs Godot 同 seed 同输出序列 |
| balance-005 | INV-CROSS 派生视图刷新 + `/balance-check` Fairness 观测 | 主控 | 0.5 | — | BalanceMatrixDump 反映重校准后 PE；平衡报告生成 |
| cultivation-a3-close | cultivation-a3 剩余 2 story 审计→Done，全 15/15 Complete | 主控 | 0.5 | — | EPIC Done；epic index 更新 |

### Should Have (1.2d)
| ID | Task | Owner | Est. Days | Dependencies |
|----|------|-------|-----------|-------------|
| cultivation-a1-unblock | cultivation-a1-rest A1.4 解除 blocked + 盘点剩余 scope | systems-designer | 0.7 | — |
| repo-tidy-kickoff | 仓库整理：`_research/raw3` 清理 + `icon_gen bug` 修复 | 主控 | 0.5 | — |

### Nice to Have
| ID | Task | Owner | Est. Days | Dependencies |
|----|------|-------|-----------|-------------|
| gh-003 | 固定时间步累加器：`_Process→Advance` 确定性驱动 | technical-director | 1.0 | gh-002 |
| combat-fullstruct-008 | combat-fullstruct story-008 backlog→实施 | gameplay-programmer | 1.5 | — |
| faction-c1-scout | Faction C.1 朝廷/经营远期范围界定 | game-designer | 0.3 | — |

## Carryover from Sprint 8
| Task | Reason | New Estimate |
|------|--------|-------------|
| balance-005 | sprint-7/8 backlog，依赖 balance-003（现已 Done） | 0.5d |
| cultivation-a3-close | sprint-8 should-have，未开工 | 0.5d |
| cultivation-a1-unblock | sprint-8 should-have，未开工 | 0.7d |
| repo-tidy-kickoff | sprint-8 should-have，未开工 | 0.5d |

## Risks
| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| gh-002 Godot 项目初始化遇版本/工具链问题 | 中 | 高 | Godot 4.x .NET 模板已知可用；遇阻先完成 gh-001 + Core 收束，gh-002 转 blocked 不阻塞全 sprint |
| WorldBridge 首次写 Godot 代码，边界纪律可能被无意违反 | 低 | 中 | control-manifest.md P-REQUIRED/P-FORBIDDEN 先行；code review 重点查 `Godot.*` 禁入 Core |

## Dependencies on External Factors
- Godot 4.x .NET SDK 需已安装（gh-002 前置；当前环境待确认）

## Definition of Done for this Sprint
- [ ] All Must Have tasks completed
- [ ] All tasks pass acceptance criteria
- [ ] QA plan exists (`production/qa/qa-plan-sprint-9.md`)
- [ ] All Logic/Integration stories have passing unit/integration tests
- [ ] Smoke check passed (`/smoke-check sprint`)
- [ ] 全量绿（≥1271） + off 逐字节 + IL 浮点扫描
- [ ] Design documents updated for any deviations
- [ ] Code reviewed and merged

---

## 备注

- **lean 模式** → PR-SPRINT producer 可行性 gate 跳过。
- **推荐执行顺序**：gh-001 → gh-002（Godot 接入主线）穿插 balance-005 + cultivation-a3-close（Core 收束）。gh-001 为纯文档（0.3d），可快速 pass。
- gh-002 是首次写 Godot 代码——边界纪律见 control-manifest.md P 层规则。
- cultivation-a1-rest 解除 blocked 后可评估是否纳入 sprint-10 Must Have。
