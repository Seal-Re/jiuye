# Sprint 4 — cultivation-a2 启动 (道心 + DailyMode + 集成)

## Sprint Goal
启动 cultivation-a2：道心资源表 + 道心/心魔伪资源 + 4 路 DailyMode + Phase FSM 集成——奠定 A.2 四子系统基础

**Start**: 2026-06-25
**End**: 2026-07-01 (7 days)
**Capacity**: 5.6d (7d - 1.4d buffer)

---

## Stories

### Must Have (Critical Path)

| ID | Story | Epic | Est | Depends | AC |
|----|-------|------|:--:|---------|-----|
| a2-001 | 21路道心资源表 (per-path daoHeart_init + 增益/减损来源) | cultivation-a2 | 1d | — | 21路 daoHeart_init>0, ≥3 gain/demon sources per path, standalone test per path |
| a2-002 | 道心/心魔伪资源系统 (gain/loss ops, [0,100] clamp, R3实证) | cultivation-a2 | 1d | a2-001 | Clamp [0,100], R3 decouple (IL scan), Chronicle events, Clone fidelity |
| a2-004 | 4路DailyMode枚举+整数倍率表 (Fast/Steady/Comprehend/Roam) | cultivation-a2 | 1d | a2-001 | 4 mode integer multipliers, Epiphany threshold, off mode no flag |
| a2-005 | DailyMode贪心算法+迟滞规则 (enter 65/exit 50) | cultivation-a2 | 1.5d | a2-004 | Greedy scoring, DEMON_DANGER hysteresis, breakthrough lock, determinism |
| a2-019 | DailyMode→Phase FSM集成 (日课↔修炼10态) | cultivation-a2 | 1d | a2-005 | DailyMode result → Phase transitions, mode locks during Breakthrough/Deviation, off mode legacy path |

### Should Have

| ID | Story | Epic | Est | Depends | AC |
|----|-------|------|:--:|---------|-----|
| a2-003 | 佛修破戒修正 (vow折半非归零) | cultivation-a2 | 0.5d | a2-002 | daoHeart half-life on vow break, lethal-only fallen, NPC堕落率<10% |
| a2-009 | 闭关DES单点唤醒 (Scheduler集成, skip mid ticks) | cultivation-a2 | 1.5d | a2-004 | NextActAt far future, no PopMin during seclusion, single-point Wake, Disturb counter |

### Nice to Have

| ID | Story | Epic | Est | Depends | AC |
|----|-------|------|:--:|---------|-----|
| a2-006 | 破单调INV-VARIETY真判据 | cultivation-a2 | 1d | a2-005 | K=10 window ≥2 modes, 50-tick dominant≤80%, VarietyTracker determinism |
| drama-001 | 关系调整 (恩仇/友谊变动) | drama-engine | 0.5d | — | Relation delta application, Chronicle events, determinism |

---

## Carryover from Sprint 3

| Task | Reason | New Estimate |
|------|--------|:-----------:|
| combat-r2/story-005 auditor sign-off | DoD last checkbox pending | 0.3d (inline) |

---

## Risks

| Risk | Probability | Impact | Mitigation |
|-------|------------|--------|------------|
| daoHeart/innerDemon fields exist but unwired — may need CultivationState refactor | Low | Medium | Fields already declared (A.0 skeleton), just add ops |
| DailyMode→Phase integration touches World.Tick hot path | Medium | Medium | Isolate under cultivation-on branch, off unchanged |
| Scope creep from 25-story backlog | Low | Low | Strictly adhere to Must Have selection |

---

## Definition of Done for this Sprint

- [ ] All Must Have stories completed
- [ ] 全量测试绿 (≥562 + new)
- [ ] off 逐字节守 (no regression)
- [ ] IL 浮点零
- [ ] Story 文件齐全 + completion notes

> ⚠️ **No QA Plan**: This sprint was started without a QA plan. Run `/qa-plan sprint`
> before the last story is implemented. The Production → Polish gate requires a QA
> sign-off report, which requires a QA plan.
