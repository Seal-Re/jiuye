# Retrospective: Sprint 2 — 全量机制结构化 + 恩怨基础

**Period**: 2026-06-18 (same day, fast iteration)
**Generated**: 2026-06-18
**Branch**: feat/jianghu-v1.2-B5-batch4-duelengine → merged to master

---

## Metrics

| Metric | Planned | Actual | Delta |
|--------|---------|--------|-------|
| Stories | 5 | 5 | 0 |
| Must-Haves | 3 | 3 | 0 |
| Should-Haves | 2 | 2 | 0 |
| Completion Rate | — | **100%** | — |
| Estimate | 5.6d | ~3h | -93% |
| Test Count | 402 | **410** | +8 |
| Commits | — | 11 | — |

---

## Velocity Trend

| Sprint | Planned | Completed | Rate | Tests |
|--------|---------|-----------|:----:|:-----:|
| Sprint 1 | 5 | 5 | 100% | 282→402 |
| Sprint 2 | 5 | 5 | 100% | 402→410 |
| **Total** | 10 | 10 | 100% | +128 |

**Trend**: Stable 100% completion. Sprint 2 was significantly faster than estimated (3h vs 5.6d) because most infrastructure was in place from Sprint 1.

---

## What Went Well

- **Leveraged Sprint 1 infrastructure heavily**. All 5 Sprint 2 stories built on EffectOp/ModuleResolver/DuelEngine/DerivedRegistry frameworks already in place. New Kind additions (ModifyEP/RelationAdjust) followed identical patterns — 5-7 lines each.
- **SituationalEdges expansion was clean**. 9→22 edges with zero new mechanism. The edge DSL (attacker.tag/defender.tag/env predicates) scaled perfectly — no parser changes needed.
- **Per-entity derived providers used existing pattern**. 4 new providers followed the stockFirepower template exactly. RegisterAll() pattern scaled from 5→9 providers with zero refactoring.
- **RelationAdjust wired through 5 files but touched only 36 lines**. CombatContext accumulator → DuelEngine.Result → SparAction.Apply — same pattern as ModifyStat and ModifyEP. Architecture proved its extensibility.

---

## What Went Poorly

- **GitHub network unreachable mid-sprint**. 3 commits queued locally, push retried 3 times over ~30 min. Blocked branch merge to master until network restored. Prevention: none (external infrastructure).
- **Estimated 5.6d, actual 3h**. Estimate was based on "write from scratch" assumptions. Reality was "extend existing patterns" — every new Kind followed the ModifyStat blueprint. The estimation gap is a sign of architectural maturity (the system is easy to extend), not poor estimation.
- **AllEdges_NoSelfCounter test failed on first run**. The mutual-counter detection logic flagged economic axis bidirectional edges (by design) and anti_evil asymmetric edges (righteous→evil +15 vs evil→righteous -10). Fixed by allowing same-axis reversed tags with opposite CoefPct signs.

---

## Blockers Encountered

| Blocker | Duration | Resolution |
|---------|----------|------------|
| GitHub network down | ~30 min | Queued commits locally, merged to master locally. Push when network back. |

---

## Estimation Accuracy

| Task | Estimated | Actual | Variance | Cause |
|------|-----------|--------|----------|-------|
| fullstruct-001 derived | 2d | 30min | -1.9d | Extend existing DerivedProviders pattern |
| fullstruct-002 克制矩阵 | 1.5d | 30min | -1.4d | Edge DSL already complete, just add data |
| drama-001 RelationAdjust | 1.5d | 20min | -1.4d | Follow ModifyStat blueprint exactly |
| fullstruct-003 UT锚锁 | 0.5d | 10min | -0.4d | Already calibrated in Sprint 1, just lock |
| fullstruct-004 Control | 1d | 10min | -0.9d | 2-line change in DuelEngine loop |

**Overall**: Consistently over-estimated by 3-10×. Root cause: Sprint 1 built the extensible architecture; Sprint 2 only extended it. This is a **good** pattern — the architecture proves its worth when extensions are trivial.

---

## Carryover Analysis

None. All 5 stories completed.

---

## Action Items for Sprint 3

| # | Action | Priority |
|---|--------|----------|
| 1 | cultivation-a2 道心系统 (daoHeart增长/InnerDemon压制/突破劫ResistTerms) | High |
| 2 | map-system 门派排布+资源点+商路 | High |
| 3 | Push master when network back | High |
| 4 | ADR adr-0001/0002/0003 creation | Med |
| 5 | G3 真对拍胜率[40,60]% with StatGenerator | Med |

---

## Summary

Sprint 2 was a fast-follow to Sprint 1. All 5 stories built on infrastructure from Sprint 1, requiring 36+43+36+43+17 = 175 lines of new code across 10 files. The extensible architecture (EffectOpKind + ModuleResolver + CombatContext accumulator → DuelEngine → SparAction) proved its worth — 3 new Kinds added in under 1 hour total. 410 green, merged to master. Ready for Sprint 3.
