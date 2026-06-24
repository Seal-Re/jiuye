# Sprint 6 — cultivation-a3 启动 + drama-engine 扩展 + 内容补完

## Sprint Goal
启动 cultivation-a3（转职/觉醒/双修）+ drama-engine 戏剧引擎扩展 + 奇遇内容积压补完

**Start**: 2026-06-27
**End**: 2026-07-03 (7 days)
**Capacity**: 5.6d (7d - 1.4d buffer)

**Note**: Sprint 5 technically started 06-26 but completed same day (06-25). Sprint 6 starts 06-27 to allow for merge/push of sprint 4+5 work.

---

## Prerequisites

- [x] Create cultivation-a3 stories — 15 stories created (`fcb43cf`)
- [ ] Push pending commits (GitHub currently unreachable)
- [ ] Merge feat/cultivation-a2 → master

---

## Stories

### Must Have (5.0d)

| ID | Story | Epic | Est | Depends |
|----|-------|------|:--:|---------|
| a3-001 | TransitionDef 数据模型 | cultivation-a3 | 0.3d | — |
| a3-002 | PathId 迁移+CultivationState 改造 | cultivation-a3 | 0.3d | a3-001 |
| a3-004 | AwakeningDef 数据模型 | cultivation-a3 | 0.3d | a3-001 |
| a3-005 | 觉醒触发器（濒死/秘境/血统法器） | cultivation-a3 | 0.3d | a3-004 |
| a3-007 | DualPathDef 数据模型+slotCap | cultivation-a3 | 0.3d | a3-001 |
| a3-008 | 双修兼容矩阵+bandwidth | cultivation-a3 | 0.3d | a3-007 |
| a3-010 | RiskModifier 反噬系统 | cultivation-a3 | 0.3d | a3-001 |
| a3-014 | A.3 不变量硬化+确定性 | cultivation-a3 | 0.5d | a3-002, a3-010 |
| a3-015 | A.3 审计员终验 | cultivation-a3 | 0.3d | a3-014 |
| drama-002 | 戏剧 storylet 引擎 | drama-engine | 0.5d | drama-001 |
| content-001 | 奇遇内容批1 (20 storylets) | cultivation-a2 | 0.3d | a2-013 |

### Should Have (1.5d)

| ID | Story | Epic | Est |
|----|-------|------|:--:|
| a3-003 | 标准转职路线数据 | cultivation-a3 | 0.3d |
| a3-006 | 觉醒→功法/战力解锁 | cultivation-a3 | 0.3d |
| a3-011 | RiskModifier 数据+cooldown | cultivation-a3 | 0.2d |
| a3-012 | 正邪分叉框架 | cultivation-a3 | 0.3d |
| drama-003 | 复仇链 | drama-engine | 0.5d |
| content-002 | 奇遇内容批2 (20 storylets) | cultivation-a2 | 0.3d |

### Nice to Have

| ID | Story | Epic | Est |
|----|-------|------|:--:|
| a3-009 | 双修战力公式+反噬 | cultivation-a3 | 0.5d |
| a3-013 | 正邪→天劫强化/正道围剿 | cultivation-a3 | 0.3d |
| content-003 | 奇遇内容批3 (14 storylets→总量60) | cultivation-a2 | 0.2d |
| repo-tidy | 仓库整理 | repo-tidy | 0.3d |

---

## Carryover

| Task | Reason | New Estimate |
|------|--------|:-----------:|
| balance-003 prong2 | Deferred from sprint 5 — needs PE diagnostic harness | When ready |
| Push pending commits | GitHub unreachable during sprint 5 | 0.1d |

---

## Risks

| Risk | Probability | Impact | Mitigation |
|-------|------------|--------|------------|
| cultivation-a3 has no stories — needs /create-stories first | **Certain** | High | Create stories before sprint starts |
| drama-engine is green-field — no existing code patterns | Medium | Medium | Keep stories small and incremental |

---

## Definition of Done

- [ ] All Must Have stories completed
- [ ] 全量测试绿 (≥767 + new)
- [ ] off 逐字节守 + IL 浮点零
- [ ] Sprint 4+5 commits merged to master
