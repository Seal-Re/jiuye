# Sprint 6 — cultivation-a3 启动 + drama-engine 扩展 + 内容补完

## Sprint Goal
启动 cultivation-a3（转职/觉醒/双修）+ drama-engine 戏剧引擎扩展 + 奇遇内容积压补完

**Start**: 2026-06-27
**End**: 2026-07-03 (7 days)
**Capacity**: 5.6d (7d - 1.4d buffer)

**Note**: Sprint 5 technically started 06-26 but completed same day (06-25). Sprint 6 starts 06-27 to allow for merge/push of sprint 4+5 work.

---

## Prerequisites

- [ ] Push pending commits (GitHub currently unreachable)
- [ ] Merge feat/cultivation-a2 → master
- [ ] Create cultivation-a3 stories (epic has EPIC.md only, no stories yet)

---

## Stories

### Must Have

| ID | Story | Epic | Est | Depends |
|----|-------|------|:--:|---------|
| a3-001 | 转职系统数据模型 | cultivation-a3 | 0.5d | **需先创建故事** |
| a3-002 | 觉醒系统数据模型 | cultivation-a3 | 0.5d | **需先创建故事** |
| a3-003 | 双修系统数据模型 | cultivation-a3 | 0.5d | **需先创建故事** |
| a3-004 | 转职/觉醒/双修注册表+RNG | cultivation-a3 | 0.5d | a3-001..003 |
| a3-005 | A.3 全不变量硬化 | cultivation-a3 | 0.5d | a3-004 |
| drama-002 | 戏剧 storylet 引擎 | drama-engine | 0.5d | drama-001 |
| drama-003 | 复仇链 | drama-engine | 0.5d | drama-002 |
| content-001 | 奇遇内容批1 (20 storylets) | cultivation-a2 | 0.3d | a2-013 |

### Should Have

| ID | Story | Epic | Est |
|----|-------|------|:--:|
| drama-004 | 恩怨系统 | drama-engine | 0.5d |
| content-002 | 奇遇内容批2 (20 storylets) | cultivation-a2 | 0.3d |
| repo-tidy | 仓库整理 | repo-tidy | 0.3d |

### Nice to Have

| ID | Story | Epic | Est |
|----|-------|------|:--:|
| a3-006 | 转职→A.2 日课交互 | cultivation-a3 | 0.5d |
| content-003 | 奇遇内容批3 (14 storylets→总量60) | cultivation-a2 | 0.2d |

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
