# Sprint 2 — 全量机制结构化 + 恩怨基础

## Sprint Goal
derived:* 真求和 + SituationalEdges 克制矩阵 + 恩怨/复仇基础

**Start**: 2026-06-19
**End**: 2026-06-25 (7 days)
**Capacity**: 5.6d (7d - 1.4d buffer)

---

## Stories

### Must Have (Critical Path)

| ID | Story | Epic | Est | Depends | AC |
|----|-------|------|:--:|---------|-----|
| fullstruct-001 | derived:* per-entity真求和 (fleetWeighted/rosterPower/ghostSoldierPower/guSwarmPower) | combat-fullstruct | 2d | — | 4 DerivedProviders真值≠0, UT8 power带收敛 |
| fullstruct-002 | SituationalEdges 克制矩阵完整 (灭阴×3/克邪×2/元素相生克+反压平/反制) | combat-fullstruct | 1.5d | 001 | 克制矩阵≥12边, 对拍胜率差≥5%实证相克 |
| drama-001 | 恩怨/复仇基础 + RelationAdjust跨路协议 (丹施丹结契/丹施丹施救) | drama-engine | 1.5d | — | ApplyRelation在CombatContext可累积, SparAction落地 |

### Should Have

| ID | Story | Epic | Est | Depends | AC |
|----|-------|------|:--:|---------|-----|
| fullstruct-003 | 辅助路UT锚锁终定 (丹/阵/器 UnifiedTierOf最终值) | cultivation-a1-rest | 0.5d | — | 3辅助路UT锚+Proxy gate PASS |
| fullstruct-004 | dot完整时序 + Control selectMove失效 (du_gu/yao/ghost) | combat-fullstruct | 1d | 001 | Dot每tick扣HP×turn, Control lock selectMove |

### Nice to Have

| ID | Story | Epic | Est | Depends | AC |
|----|-------|------|:--:|---------|-----|
| a2-001 | 道心系统MVP (daoHeart增长/InnerDemon压制) | cultivation-a2 | 1d | — | 突破劫ResistTerms使用daoHeart/innerDemon |
| map-001 | 门派排布+资源点+商路基础 | map-system | 1d | — | 9大区门派坐标+3资源点+5商路 |

---

## Carryover from Sprint 1

| Task | Reason | New Estimate |
|------|--------|:-----------:|
| G3 真对拍胜率[40,60]% | balance-cross harness就绪, 需StatGenerator采样K=12 | 0.5d (nice-to-have) |
| ADR adr-0001/0002/0003 | P8补 | 0.5d (inline) |

---

## Definition of Done

- [ ] All Must Have stories completed
- [ ] 全量测试绿 (≥450)
- [ ] off 逐字节守
- [ ] IL 浮点零
- [ ] Code reviewed
- [ ] Story files updated with completion notes
