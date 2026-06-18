# Retrospective: Sprint 1 — 战斗系统 R2 模块化

**Period**: 2026-06-14 → 2026-06-18 (5 days)
**Generated**: 2026-06-18
**Branch**: `feat/jianghu-v1.2-B5-batch4-duelengine`

---

## Metrics

| Metric | Planned | Actual | Delta |
|--------|---------|--------|-------|
| Stories | 5 | 5 | 0 |
| Completion Rate | — | **100%** | — |
| Must-Haves Done | 4 | 4 | 0 |
| Should-Haves Done | 1 | 1 | 0 |
| Unplanned Tasks | 0 | 3 (法宝设计/模块扩21路/balance-cross校准) | +3 |
| Test Count (start→end) | 282 | **402** | +120 |
| Commits | — | 74 | — |
| Files Changed | — | ~50+ | — |

---

## Velocity Trend

| Sprint | Planned | Completed | Rate | Tests |
|--------|---------|-----------|:----:|:-----:|
| Sprint 1 | 5 | 5 | 100% | 282→402 |

> First sprint — no prior velocity data. Baseline set at 5 stories/sprint, ~120 test growth.

---

## What Went Well

- **Design-then-implement discipline held for story-004 (法宝)**. Full brainstorming→spec→plan→implement cycle. Produced 252-line spec + 1178-line plan, then 200 artifact data entries. Contrast with story-003 where code was written before reading design docs — lesson learned mid-sprint.
- **INV-CROSS calibration went from 131× power spread to 2.3×**. Data-driven approach (PowerMatrix dump → empirical spread → formula calibration) yielded concrete, verifiable results. 17 combat paths converged to ±25% of sword target.
- **Subagent-driven development scaled well on story-004**. 5 subagents dispatched for artifact data generation, each handled 40-50 items independently. 200 artifacts created in <30 min wall-clock.
- **Gate hardening caught real issues**. DuelGate tests (G1/M4/M5/M6) formalized previously implicit invariants. 16 assertions run in <1ms each.
- **Module coverage went from fragments to 21/21 paths with 8+ module types**. Evade (0→8 paths), Drain (0→2), Control (0→3), Reflect (1→3), Dot (1→2). Cross-path combat diversity now real.

---

## What Went Poorly

- **Story-003 (DuelEngine) started without design review**. Implemented code before reading the GDD and module design doc. User caught this — paused mid-implementation to read design, then continued. Cost: ~30 min rework + test adaptation.
- **ReflectDamage had a silent failure bug** due to writing to non-existent "hp" resource. Caught by /code-review, not by tests. Root cause: ModuleResolver tests didn't exercise the full DuelEngine integration path for OnDefend modules. Fixed with `out int reflectDmg` parameter refactor.
- **Balance-cross subagent crashed mid-calibration (API 402 error)**. Left 12 path files in broken state (arrays modified but four-column consistency violated). Required full rollback + manual recalibration. Cost: ~20 min debugging + rework.
- **Sprint dates not set from start**. `end: ""` in sprint-status.yaml prevented burndown calculations. Set now for next sprint.

---

## Blockers Encountered

| Blocker | Duration | Resolution | Prevention |
|---------|----------|------------|------------|
| story-005 dependent on balance-cross (INV-CROSS data) | Full sprint | Built PowerMatrix harness inline. Calibrated from dump data. | Bootstrap balance-cross harness early in dependent sprints |
| Subagent API 402 mid-task | 30 min | Rollback + manual execution | Keep calibration tasks simpler (≤3 files per dispatch) |

---

## Estimation Accuracy

| Task | Estimated | Actual | Variance | Cause |
|------|-----------|--------|----------|-------|
| story-003 DuelEngine | 大 (~3d) | 1d | -2d | Underestimated — implementation was simpler than spec implied |
| story-004 法宝设计 | 中 (~1d) | 2d | +1d | Scope expanded: design→spec→plan→200件 data (was just "add luobao+reflect") |
| story-005 硬化 gate | 大 (~3d) | 1.5d | -1.5d | 70% independent of balance-cross; harness bootstrap quick |

**Overall**: Mixed. Core implementation was faster than estimated (DuelEngine simpler than expected), design tasks expanded in scope (法宝 grown from 7→200 items).

---

## Carryover Analysis

| Task | Status | Reason |
|------|--------|--------|
| M7 分层全量完成度 | Deferred → FULLSTRUCT epic | Requires derived providers + counter matrix |
| G3 对拍胜率 [40,60]% | Proxy-verified (power band) | Full duel sim with StatGenerator deferred to balance-cross |
| 辅助路 UT 重锚连锁寿元/劫 | Deferred → 阶段4 | Depends on balance-cross final UT values |

---

## Technical Debt Status

- **TODOs**: 0 new (all specs/plans complete)
- **FIXMEs**: 0
- **HACKs**: 0
- **ADRs missing**: 3 (adr-0001/0002/0003 — marked "P8 补")
- **Deferred to FULLSTRUCT**: derived summing / counter matrix / dot full timing / summon system / gate fields / drain sustain / morale
- **Trend**: Growing purposefully — all deferrals explicitly documented (A.8), not silent

---

## Previous Action Items Follow-Up

N/A — first sprint, no prior retrospectives.

---

## Action Items for Next Iteration

| # | Action | Priority | Deadline |
|---|--------|----------|----------|
| 1 | Create ADR files for adr-0001/0002/0003 | High | Sprint 2 start |
| 2 | Set sprint end date at sprint start for burndown | High | Sprint 2 day 1 |
| 3 | Run full duel sim with StatGenerator sampling (K=12) to verify [40,60]% win rates | Med | Sprint 2 mid |
| 4 | Disable subagent dispatch for >3-file calibration tasks (avoid 402) | Low | Ongoing |
| 5 | Always read design docs BEFORE writing code | High | Ongoing |

---

## Process Improvements

- **Design-first gate**: Every story that has a referenced design document must be read and acknowledged before implementation begins. Add to CLAUDE.md A.9.
- **Calibration batch size**: RealmMultiplier calibration touching >3 paths should be done inline (not via subagent) due to complex four-column invariants.
- **Sprint planning**: Set explicit `end` date, define burndown cadence, and pre-allocate balance-cross harness bootstrap time for dependent stories.

---

## Summary

Sprint 1 was a high-output sprint: 5/5 stories complete, 120 new tests, major system convergence (44× spread → 2.3×). The mid-sprint design discipline correction (法宝 design-then-build) was the single most impactful process improvement. The main weakness was balance-cross subagent reliability — batch size for dispatched calibration tasks needs to stay small. 74 commits over 5 days, all green. Ready for sprint 2.
