# RealmMultipliers Recalibration Specification (balance-003 prong2)

**日期**: 2026-06-25
**状态**: In Progress
**前置条件**: balance-003 prong1 (module damage cap PE/4) completed

## Problem Statement

C1 gate (same-UT combat path win-rate [40,60]%) has 47/48 violation pairs even after module damage capping. Root cause: **RealmMultipliers × PowerFormula.Terms interaction creates 2-3× PE spread at same UT across different combat paths.**

Module cap (PE/4) prevents one-shot kills from modules but cannot fix the fundamental PE imbalance between paths at the same UT.

## Current State (2026-06-25)

- C1 violations: 47/48 at [35,65]%
- Paths with PE too HIGH: xue_xiu (1.65× sword), soul (1.41×), dugu_xiu (1.70×), fu_xiu (1.30×), ti_xiu (probably 1.5×+)
- Paths with PE too LOW: yin_xiu (1.12× sword but still loses 50-0 → PowerFormula deficiency), yinguo_faze (1.48× but loses → PowerFormula deficiency)
- Paths needing verification: all 18 combat paths × 9 UTs each

## Calibration Formula (from INV-CROSS-design.md §5)

```
For each combat path p at UT i:
    targetPE(UT_i) = sword_immortal_PE_at_UT_i  // anchor to sword
    actualPE_p(i) = PowerEngine(typical_char(p, UT_i))
    scaleFactor_p(i) = targetPE(i) / actualPE_p(i)
    newMultiplier_p(flatIndex) = round(oldMultiplier_p(flatIndex) * scaleFactor_p(i_flatIndex))

Re-anchor tolerance (C3): target(UT*) ±15% hard-coded
```

## Required Steps

### Step 1: Run full PE diagnostic

1. Use `PeDiagnosticTests.DumpPE_UT8_CombatPaths` or `BalanceMatrixDumpTests` to dump PE for all 18 combat paths × 9 UTs
2. Compute sword_immortal baseline PE at each UT: [PE_UT0, PE_UT2, PE_UT4, PE_UT6, PE_UT8, PE_UT9, PE_UT10, PE_UT11, PE_UT12]
3. For each path, compute deviation: `|actualPE - targetPE| / targetPE * 100`
4. Identify paths with deviation >15% at any UT

### Step 2: Apply analytic calibration

For each path with deviation >15%:
1. Compute scaleFactor per flatIndex (map flatIndex → UT → targetPE/actualPE)
2. Apply: `newMultiplier = round(oldMultiplier * scaleFactor)`
3. Ensure UnifiedTierOf remains non-decreasing (RealmCurve.Validate)
4. Ensure RealmMultipliers × SubLevelCount consistency

### Step 3: Verify

1. Re-run PE diagnostic → all combat paths within ±15% of sword at same UT
2. Re-run C1 gate → violations should drop from 47 to <10
3. Re-run C2 gate (crush monotonicity) → must still pass
4. Full test suite → 0 regressions
5. Tighten C1 threshold from [35,65]% to [40,60]%

### Step 4: Freeze

1. Update `DuelGateTests.Frozen` constants to reflect final calibrated values
2. Set `WIN_RATE_LOW = 40, WIN_RATE_HIGH = 60` as hard gate
3. Set C1 test assertion: `violations == 0` (hard, blocking)

## Target Paths (Priority Order)

| Priority | Path | Current UT8 × sword | Action | Target UT8 × sword |
|:--------:|------|:-------------------:|--------|:------------------:|
| 1 | du_gu_xiu | 1.70× | Scale down | 1.20× |
| 2 | xue_xiu_xuesha | 1.65× | Scale down | 1.20× |
| 3 | soul_divine_sense | 1.41× | Scale down | 1.20× |
| 4 | fu_xiu_fulu | 1.30× | Scale down | 1.15× |
| 5 | ti_xiu_hengshi | ? | Diagnose first | 1.15× |
| 6 | yin_xiu_yuedao | 1.12× (但输 50-0) | Diagnose PowerFormula,可能需 booster | 1.15× |
| 7 | yinguo_faze | 1.48× (但输 50-0) | Diagnose PowerFormula,可能需 booster | 1.20× |

## Known Challenges

1. **PowerFormula interaction**: A path's PE depends on both RealmMultipliers AND PowerFormula.Terms. Two paths with same multipliers may have different PE due to different stat weights. The calibration formula accounts for this via actualPE measurement.
2. **Module loadout**: Some paths have strong PenFromResource modules, others don't. Even with PE cap, module-bearing paths retain an edge. The module cap (PE/4) limits but doesn't eliminate this edge.
3. **UT granularity**: RealmMultipliers are per-flatIndex, not per-UT. Multiple flatIndices may share the same UT but have different multipliers. Scaling must be applied per-flatIndex.
4. **Auxiliary path exemption**: Dan/Array/Qixiu are exempt from C1 but must have honest UT (C3). Their multipliers should not be adjusted by this calibration.

## Previous Attempts

- **INV-CROSS v2** (2026-06-21): Adjusted 8 paths' multipliers. Result: violations dropped from ~60 to 47.
- **INV-CROSS v2r2**: Adjusted du_gu_xiu further. Still 1.70× sword.
- **balance-003 prong1** (2026-06-25): Added module damage cap PE/4. No effect on violation count (PE imbalance dominates).
- **balance-003 quick fix** (2026-06-25): Adjusted xue_xiu (-30%), soul (-15%), du_gu_xiu (-20%). Result: 47→46 violations. Manual path-by-path adjustment ineffective without full PE diagnostic.

## Recommendation

Run full PE diagnostic harness (PeDiagnosticTests) to get actual PE values for all 18 combat paths × 9 UTs, then apply analytic calibration formula systematically. This is a 2-3 hour balance iteration task best done in a dedicated session.
