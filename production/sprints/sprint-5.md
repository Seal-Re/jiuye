# Sprint 5 — C1 收敛 + A.2 破障·闭关·硬化

## Sprint Goal
收敛 C1 平衡闸门至 [40,60]%（balance-003）+ 继续 cultivation-a2 破障·闭关阶段 + A.2 全线硬化收尾

**Start**: 2026-06-26
**End**: 2026-07-02 (7 days)
**Capacity**: 5.6d (7d - 1.4d buffer)

---

## Stories

### Must Have (Critical Path)

| ID | Story | Epic | Est | Depends | Type |
|----|-------|------|:--:|---------|------|
| balance-003 | C1收敛 [40,60]% 硬闸门 (module cap + RealmMultipliers recalibration) | balance-cross | 2d | balance-002 | Algorithm |
| a2-007 | BreakAid四法数据模型 | cultivation-a2 | 0.2d | a2-001 | Data |
| a2-008 | 顿悟Epiphany机制完善 | cultivation-a2 | 0.3d | a2-004 | Data |
| a2-010 | 闭关时长+收益公式 | cultivation-a2 | 0.5d | a2-009 | Formula |
| a2-020 | BreakAid→Breakthrough集成 | cultivation-a2 | 0.5d | a2-007, a2-009 | Integration |
| a2-023 | A.2全不变量硬化 | cultivation-a2 | 0.8d | a2-006, a2-019 | Hardening |
| a2-024 | A.2确定性+off逐字节 | cultivation-a2 | 0.3d | a2-023 | Hardening |
| a2-025 | A.2审计员终验 | cultivation-a2 | 0.5d | all above | Audit |

### Should Have

| ID | Story | Epic | Est |
|----|-------|------|:--:|
| a2-011 | 闭关避险刷点 | cultivation-a2 | 0.3d |
| a2-012 | 闭关RNG自洽+Scheduler集成 | cultivation-a2 | 0.3d |

### Nice to Have

| ID | Story | Epic | Est |
|----|-------|------|:--:|
| a2-013 | 奇遇storylet最小执行器 | cultivation-a2 | 1d |

### Carryover from Sprint 4

| Task | Reason | New Estimate |
|------|--------|:-----------:|
| C1 balance convergence | Deferred from sprint 3+4 | Embedded in balance-003 |
| Code review sprint 2-4 commits | Carried from sprint 2 | 0.3d (inline) |

---

## Risks

| Risk | Probability | Impact | Mitigation |
|-------|------------|--------|------------|
| balance-003 module cap changes DuelEngine semantics — may break existing gate tests | Medium | High | Cap first, recalibrate after; validate C2/C3/M6 incrementally |
| RealmMultipliers recalibration touches 21 paths × 12 UT matrix — unknown iteration count | Medium | Medium | Only recalibrate paths with >15% deviation (est 5-8 paths) |

---

## Definition of Done

- [ ] All Must Have stories completed
- [ ] C1 gate: 0 violations at [40,60]% (hard, blocking)
- [ ] 全量测试绿
- [ ] off 逐字节守 + IL 浮点零
- [ ] Story 文件齐全 + completion notes
