# Sprint 8 (Alpha-2) — 2026-07-17 to 2026-07-24

## Sprint Goal
收束 Alpha-1 遗留（balance-003/doc 债/设计文档同步），并为下一重大里程碑 **godot-host Phase-2** 做 story 拆解与前置盘点。

> 阶段：Production（= AAA Alpha）。全 15 epic 除 cultivation-a1-rest(Designed)/godot-host(Planned)/llm-brain(Not designed)/visualization(Spike)/repo-tidy(Todo) 外均为 Done。combat-variance 刚闭合（2026-07-17），WIP=0。

## Capacity
- Total days: 7
- Buffer (20%): 1.4d
- Available: **5.6d**

## Tasks

### Must Have (3.3d)
| ID | Task | Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------|-----------|-------------|---------------------|
| balance-003-close | C1 收敛 balance-003 review→done 收尾（AC 3.4 已拆 balance-006 Done；标记完成） | 主控 | 0.3 | — | story status→Complete；sprint-status 同步；epic index 更新 |
| doc-debt-1 | 合并 `docs/architecture/architecture.md`（散在 CLAUDE.md §F / 10 ADR / agent-guide） | technical-director | 0.8 | — | architecture.md 含 Foundation/Core/Feature 三层覆盖；gate 硬阻断解除 |
| doc-debt-2 | `docs/architecture/control-manifest.md`（从 Accepted ADR 提取层规则） | technical-director | 0.7 | doc-debt-1 | 每 ADR 规则可溯；层规则可执行；工程红线可引用 |
| design-sync | `design/gdd/combat-system.md` 同步修订：确定性结算→概率博弈模型（combat-variance EPIC DoD） | game-designer | 1.0 | — | 战斗 GDD 反映 adr-0008（方差模型）+ adr-0010（三层防御漏斗）；§Formulas §Edge Cases 更新 |
| godot-host-scout | Godot 宿主层 Phase-2 盘点 + story 拆解（`/create-stories godot-host`） | technical-director | 0.5 | — | EPIC story 文件就位；明确 View 闸口条件（A.10：无头日志证核心无死锁方可接入） |

### Should Have (2.2d)
| ID | Task | Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------|-----------|-------------|---------------------|
| balance-005 | INV-CROSS 派生视图刷新 + `/balance-check`（Fairness 观测） | 主控 | 0.5 | balance-003-close | BalanceMatrixDump 反映重校准后 PE；平衡报告生成 |
| cultivation-a1-unblock | cultivation-a1-rest A1.4 解除 blocked（依赖 balance-cross Done）→ 盘点剩余 scope | systems-designer | 0.7 | balance-003-close | EPIC 状态 Designed→In Progress or Planned；剩余 story 清单明确 |
| cultivation-a3-close | cultivation-a3 剩余 2 story 审计→Done，EPIC 全 15/15 Complete | 主控 | 0.5 | — | 全 story Complete；EPIC header 更新 |
| repo-tidy-kickoff | 仓库整理 kickoff：`_research/raw3` 清理 + `icon_gen bug` 修复 | 主控 | 0.5 | — | PR 合并；无遗留临时目录 |

### Nice to Have
| ID | Task | Owner | Est. Days | Dependencies |
|----|------|-------|-----------|-------------|
| combat-fullstruct-008 | combat-fullstruct story-008 backlog→实施 | gameplay-programmer | 1.5 | — |
| faction-c1-scout | Faction C.1 朝廷/经营远期范围界定 | game-designer | 0.3 | — |

## Carryover from Sprint 7
| Task | Reason | New Estimate |
|------|--------|-------------|
| doc-debt-1 | sprint-7 should-have，未开工；combat-variance 优先导致推迟 | 0.8d |
| doc-debt-2 | sprint-7 should-have，blocked by doc-debt-1 | 0.7d |
| balance-005 | sprint-7 nice-to-have，blocked by balance-003（现可解除） | 0.5d |

## Risks
| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| architecture.md 合并范围蔓延（10 ADR 内容量大） | 中 | 中 | 限 Foundation/Core 层；Feature 层只列索引 + ADR 指针 |
| godot-host story 拆解阻塞于闸口条件不明确 | 低 | 高 | Phase-2 盘点已有（1062 绿 Core 零冲突），只差闸口形式化；adr-0004 §9 已有契约 |
| design-sync GDD 修订引跨系统连锁改动 | 低 | 中 | 只修订 combat-system.md §战斗结算/公式/边界；不扩及其他 GDD |

## Dependencies on External Factors
- 无（纯 Core 逻辑 + 文档，headless；无外部依赖）

## Definition of Done for this Sprint
- [ ] All Must Have tasks completed
- [ ] All tasks pass acceptance criteria
- [ ] QA plan exists (`production/qa/qa-plan-sprint-8.md`)
- [ ] All Logic/Integration stories have passing unit/integration tests
- [ ] Smoke check passed (`/smoke-check sprint`)
- [ ] 全量绿（≥1271） + off 逐字节 + IL 浮点扫描
- [ ] Design documents updated for any deviations
- [ ] Code reviewed and merged

---

## 备注

- **lean 模式** → PR-SPRINT producer 可行性 gate 跳过（承 `production/review-mode.txt`）。
- **推荐执行顺序**：balance-003-close → doc-debt-1 → doc-debt-2 → design-sync → godot-host-scout。先收债再开路。
- balance-003 实质已完成（AC 3.4 拆出 balance-006 Done，剩余为台账标记）。
- 本 sprint 以文档/盘点为主、新代码量为轻——Alpha 阶段从"建机制"过渡到"立规范+测准入"。
- combat-variance EPIC DoD 中 `design/gdd/combat-system.md` 同步修订挂本 sprint design-sync 任务。

> **Scope check**: godot-host-scout 产出 story 文件，其实现工作属后续 sprint。若拆出 >3 story 优先纳入 sprint-9。
