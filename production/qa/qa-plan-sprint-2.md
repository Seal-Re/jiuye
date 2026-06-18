# QA Plan — Sprint 2

**Generated**: 2026-06-18
**Sprint**: 2 (2026-06-19 → 2026-06-25)

---

## Story Test Requirements

### fullstruct-001: derived:* per-entity 真求和

| AC | Test Type | Required Evidence |
|----|-----------|-------------------|
| 4 DerivedProviders 真值 ≠ 0 | Logic | UT: DerivedProvidersTests — each provider returns non-zero with populated state |
| fleetWeighted 随 constructTier 增长 | Logic | UT: constructTier=3 → fleetWeighted > constructTier=1 |
| rosterPower 随 bond 增长 | Logic | UT: bond=80 → rosterWeighted > bond=40 |
| ghostSoldierPower 随 shaCharge 增长 | Logic | UT: shaCharge=50 → ghostSoldierPower×2 > shaCharge=0 |
| UT8 power带收敛 (验证校准不破) | Integration | PowerMatrix dump: 战斗路∈[0.7,1.3]×剑修 |

**Test File**: `tests/Jianghu.Core.Tests/Cultivation/DerivedProvidersTests.cs`

---

### fullstruct-002: SituationalEdges 克制矩阵

| AC | Test Type | Required Evidence |
|----|-----------|-------------------|
| ≥12 边在 Edges 表 | Logic | UT: SituationalEdgesDataTests — count ≥ 12 |
| 灭阴×3 对 evil tag 生效 | Logic | UT: attacker vs evil defender → adj > 0 |
| 克邪×2 对 evil tag 生效 | Logic | UT: righteous attacker(vs evil) → adj ≥ +15 |
| 元素相生克(火克木) | Logic | UT: fire attacker vs wood defender → adj = +15 |
| 反压平: 随机采样 ≥3路对 |win%−50%|∈[5,10] | Integration | DuelSim: K=12/路, 200局/对 |
| 反制: X克Y→Y不克X (环自洽) | Logic | UT: 遍历所有边, 不存在互克 |

**Test File**: `tests/Jianghu.Core.Tests/Cultivation/SituationalEdgesFullTests.cs`

---

### drama-001: 恩怨/复仇 + RelationAdjust

| AC | Test Type | Required Evidence |
|----|-----------|-------------------|
| ApplyRelation 经 CombatContext 累积 | Logic | UT: 施加 +3 关系 → ctx.GetRelationDelta 返回 +3 |
| SparAction 落地关系修改 | Integration | UT: DuelEngine.Result.RelationDeltas → IWorldMutator.AdjustRelation |
| 丹修 施丹结契→造正边+15 | Logic | UT: 技能 OnUse 含 RelationAdjust 算子 |
| 丹修 施丹施救→造正边+8 | Logic | UT: 技能 OnUse 含 RelationAdjust 算子 |

**Test File**: `tests/Jianghu.Core.Tests/Cultivation/RelationAdjustTests.cs`

---

### fullstruct-003: 辅助路 UT 锚锁

| AC | Test Type | Required Evidence |
|----|-----------|-------------------|
| 丹 UT≤7 | Logic | UT: Assert UT max ≤ 7 |
| 阵 UT≤7 | Logic | UT: Assert UT max ≤ 7 |
| 器 UT≤10 | Logic | UT: Assert UT max ≤ 10 |
| Proxy gate PASS | Integration | PowerSpread: 辅助路不在战斗路 band |

---

### fullstruct-004: dot 完整时序 + Control

| AC | Test Type | Required Evidence |
|----|-----------|-------------------|
| Dot per-tick 伤害 = op.Amount | Logic | UT: 挂 Dot(5,3tick) → r+1扣5, r+2扣5, r+3扣5 → 总扣15 |
| Dot 期满自动移除 | Logic | UT: Dot 3回合后不再扣血 |
| Control selectMove=false | Logic | UT: 被控回合 damage=0 (IsControlled=true) |
| Control 期满恢复 | Logic | UT: 控场过期后 damage>0 |

**Test File**: `tests/Jianghu.Core.Tests/Cultivation/DotControlTests.cs`

---

## Smoke Test Scope

- 全量 UT: ≥450 green
- off 逐字节: OffByteIdenticalTests PASS
- IL 浮点扫描: PASS
- PowerMatrix dump: 战斗路 UT8 spread < 3×
- DuelSim proxy: 战斗路 100% in ±25% band

---

## Risk Register

| Risk | Mitigation |
|------|-----------|
| per-entity derived 无 roster 数据结构 → 近似 | 用聚合资源 (fleetWeighted/rosterPower) 作 A.0 近似, 真 per-entity → FULLSTRUCT |
| 克制矩阵对拍需要 StatGenerator (K=12采样) | 先用 proxy gate (power band) 作快 gate, 对拍 depend on balance-cross harness |
| RelationAdjust 需 IWorldMutator 接口扩展 | 最小侵入: CombatContext 累积 + SparAction 落地, 不改变 IWorldMutator 签名 |
