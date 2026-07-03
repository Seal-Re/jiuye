# Story 003: C1 收敛 [40,60]% 硬闸门 + 模块伤害钳制

> **Epic**: balance-cross
> **Status**: Deferred → **Alpha 阶段**（流水线纪律，见下「2026-07-02 defer 复核」）
> **Last Updated**: 2026-07-02
> **Layer**: Core
> **Type**: Integration
> **Estimate**: 大 (2d)
> **Depends**: balance-002 (INV-CROSS gate done, 47/48 violations at [35,65]%), combat-r2 story-005 (hardening gate done)
> **ADR**: adr-0002-module-factory-effect-system, adr-0001-integer-determinism
> **GDD**: 平衡标定INV-CROSS-design.md §2；模块化效果系统-design.md §15.7

## Context

**Carried from sprint 3, deferred again in sprint 4.** C1 gate (same-UT combat path win-rate ∈ [40,60]%) is currently advisory at [35,65]% with 47/48 violation pairs. Root cause identified: DuelEngine Scale=100 mechanism amplifies PenFromResource/DrainResource/CounterMul modules to one-shot magnitudes (unscaled dmg 24 + module 600 = 624/round vs ~240 HP). This makes module-bearing paths dominate non-module paths absolutely.

**Two-pronged fix needed:**
1. **Module damage cap**: Cap module contribution to scaled damage per round (prevent one-shots)
2. **RealmMultipliers recalibration**: After capping, recalibrate so same-UT pairs land in [40,60]%

## Acceptance Criteria

- [ ] 3.1 **Module contribution cap**: Each OnUse module's damage addition capped at `PE * 2` (internal Scale units). Prevents PenFromResource(200 qi × 3 = 600) from adding 600 to unscaled 24 → now capped at PE×2=480 internal, or scaled appropriately
- [ ] 3.2 **After capping, re-run C1 gate**: same-UT win rate distribution shifts toward [40,60]%
- [ ] 3.3 **RealmMultipliers analytic calibration**: For paths outside [40,60]%, adjust RealmMultipliers via `mul_p[i] := round(target(UT_i) * 10 / BaseSum_p(i))` formula (per INV-CROSS §5)
- [ ] 3.4 **Hard gate**: C1 test asserts `violations == 0` for [40,60]% threshold (not advisory, not frozen baseline — blocking)
- [ ] 3.5 **C2 crush monotonicity preserved**: UT gap ≥ 2 → high UT win rate ≥ 80% (existing hard gate, must not regress)
- [ ] 3.6 **C3 auxiliary exemption preserved**: Dan ≤ 7, Array ≤ 7, Qixiu ≤ 10 (existing gate)
- [ ] 3.7 **Anti-flattening diversity**: ≥ M=3 pairs with |win%-50%| ∈ [5,10] (per §15.7 frozen constants)
- [ ] 3.8 **Re-anchor tolerance**: target(UT*) ±15% hard-coded, verified against sword_immortal baseline
- [ ] 3.9 **Full green + off byte-identical + IL float zero**: No regressions
- [ ] 3.10 **Frozen constants updated**: DuelGateTests.Frozen class reflects final calibrated values

## Implementation Notes

### Prong 1: Module damage cap

In `ModuleResolver.ApplyOnUse`, add a cap to module damage contribution:

```csharp
// Current (problematic):
case EffectOpKind.PenFromResource:
    return dmg + ctx.ReadResource(Side.Attacker, m.Key!) * m.Amount / Den(m);

// Fixed:
case EffectOpKind.PenFromResource:
    int rawAdd = ctx.ReadResource(Side.Attacker, m.Key!) * m.Amount / Den(m);
    int cap = pe * 2; // PE-based cap in internal units
    return dmg + Math.Min(rawAdd, cap);
```

Where `pe` needs to be available in the context (pass through CombatContext or add to method signature). The cap must be in the same unit space as the damage — internal (Scale=100) or unscaled (dmg/Scale). Decision: apply cap in **unscaled space** (where ApplyOnUse operates), making cap = `pe / 5` (equivalent to PE×2 in internal units scaled down by Scale=100).

### Prong 2: RealmMultipliers recalibration

After capping modules, re-run BalanceMatrixDump to get new PE values per path×UT cell. Then apply analytic calibration:

```
For each combat path p at UT i:
    target(UT_i) = sword_immortal_target[i]  // anchor to sword
    BaseSum_p(i) = median(PE across stat variations) at UT i
    mul_p[i] = round(target(UT_i) * 10 / BaseSum_p(i))
```

Update RealmCurveDef.RealmMultipliers in affected path definition files.

### Scope of RealmMultiplier changes

Expect to modify RealmMultipliers for paths whose PE deviates >15% from sword_immortal anchor. Known outliers from balance-001 dump: 魂修 (PE too high), 因果修 (PE too low), 血修 (PE too high), 音修 (PE too low). Expected 5-8 paths need adjustment.

### Frozen constants update

After calibration, update `DuelGateTests.Frozen`:
- `WIN_RATE_LOW = 40` (unchanged, now enforced)
- `WIN_RATE_HIGH = 60` (unchanged, now enforced)
- Any adjusted target values for re-anchor tolerance

## Out of Scope

- Module capping for OnDefend effects (Evade, ReflectDamage) — these don't cause one-shots
- Module capping for artifact effects — deferred to combat-fullstruct epic
- Full combat path rebalancing beyond what's needed for C1 gate

## Test Evidence Requirement

**Type**: Integration — integrated tests. Post-fix C1 gate at [40,60]% with 0 violations, C2/C3 preservation, module cap boundary tests, RealmMultipliers audit, full green.

---

## 2026-07-02 defer 复核（流水线纪律 · 用户「轮次提示词」指令）

**结论：本 story 明确推迟到 Alpha 阶段。** 用户指令确立工业化流水线纪律——「游戏平衡性数值的详细调整必须推迟到 Alpha（功能完备）阶段及以后」。C1 收敛 = 典型的详细平衡调优，属过早雕琢，暂停。

**本轮已完成的一次性核验（供 Alpha 复用，不浪费）：**
- **Prong 1（模块伤害钳制）实为已落**：`ModuleResolver.CapDelta` + `DuelEngine` `ctx.ModuleDamageCap = attackerPe/4`。AC 3.1 ✅。故剩余只有 Prong 2。
- **真实缺口远大于 story 原估**：全覆盖硬闸门实测 **1094/1115** 同UT战斗对超 [40,60]%（advisory 仅采样 48 对见 47；story 原估 5-8 路 outlier）。系统性曲线发散，非局部。
- **根因确认 = 纯 PE-gap**（非 counter/模块）：全 21 路 PE 矩阵证同UT PE 跨度极大（UT=8：yinguo 486 → kuilei 2589，sword 1140），近确定性对拍把 ~10% 差放大成 100%/0%。近 PE 对确会产出 44%/68% 中间率 → 均衡 PE 后 [40,60] 可涌现。
- **可执行方案（Alpha 落地）**：`new_mul_p(realm) = old_mul_p(realm) × sword_PE(UT) / path_PE(UT)`（承 AC 3.3），按 sword 锚均衡 ~18 路 `RealmMultipliers`，保 C2/C3 单调。
- **B.3 安全**：`RealmMultipliers` 仅经 `PowerEngine.Evaluate`（SparAction/DuelEngine/Tribulation/A3，均 cultivation-on），off 逐字节不受影响（`OffByteIdenticalTests`/`OffRegressionWith21PathsTests` 守）。
- **决策点（Alpha 定）**：violations==0 可能与 counter克制/压制矩阵结构性冲突（等power下 counter 本就 >60%）→ 需 counter对拍豁免名单 or 目标带宽调整。此为设计裁决，留 Alpha。

**未提交任何实现**：本轮所写 `[40,60]%` RED 硬闸门测试已撤销（工作树恢复 1051 绿）。上述为纯分析落盘。

