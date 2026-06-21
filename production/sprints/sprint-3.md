# Sprint 3 — combat-fullstruct 收尾 + balance-cross 启动

## Sprint Goal
combat-fullstruct DoD 收尾（PostMul/负向压制/Gate字段/唯一档迁完）+ balance-cross INV-CROSS 启动（BalanceMatrixDump harness）

**Start**: 2026-06-22
**End**: 2026-06-28 (7 days)
**Capacity**: 5.6d (7d - 1.4d buffer)

---

## Stories

### Must Have (Critical Path)

| ID | Story | Epic | Est | Depends | AC |
|----|-------|------|:--:|---------|-----|
| fullstruct-005 | PostMul ModKind + 负向压制 (LawSuppress/化形态/文宫/天道压制) | combat-fullstruct | 1.5d | fullstruct-001 | PostMul ModKind≥3种, 负向压制≥2种, 全UT测试带 |
| fullstruct-006 | 结构化 Gate 字段 + 唯一档签名逐路全迁 | combat-fullstruct | 1d | combat-r2 done | gate字段≥5, 唯一档SpecialModule逐路注册完成, 全量绿 |
| balance-001 | BalanceMatrixDump harness: 全21路×UT0-12 战力矩阵 dump | balance-cross | 1.5d | combat-r2 done | 21×9 矩阵dump, pe范围/均值/方差可读, 单路胜率≥2路对拍实证 |

### Should Have

| ID | Story | Epic | Est | Depends | AC |
|----|-------|------|:--:|---------|-----|
| fullstruct-007 | 结算回滚栈 (因果逆演/夺舍续命/分魂挡刀) | combat-fullstruct | 1d | fullstruct-001 | 逆演栈≥3操作回滚, 夺舍/分魂有测试, off逐字节守 |
| balance-002 | INV-CROSS 对拍胜率实证 (同UT胜率∈[40,60]% gate) | balance-cross | 1d | balance-001 | 全路组合对拍≥N场, 胜率分布[35,65]%可接受到收敛 |

### Nice to Have

| ID | Story | Epic | Est | Depends | AC |
|----|-------|------|:--:|---------|-----|
| fullstruct-008 | 非战斗机制 (丹改四维 + 卖丹经济晋升) | combat-fullstruct | 0.5d | combat-r2 | DanModifyStat effect落地, 经济晋升通过realm gate |
| cultivation-a1-001 | 10态流程状态机 (CultivationPhase 骨架) | cultivation-a1-rest | 1d | balance-001 | 10态枚举+int守卫, 测试覆盖≥6条主转移 |

---

## Carryover from Sprint 2

| Task | Reason | New Estimate |
|------|--------|:-----------:|
| Code review sprint 2 commits | DoD 未勾 | 0.3d (inline) |

---

## Risks

| Risk | Probability | Impact | Mitigation |
|-------|------------|--------|------------|
| balance-001 dump暴露极端失衡(差24-70×) | High | Low | 仅dump不修—先看清全貌, 收敛迭代归 balance-002+ |
| fullstruct-007 回滚栈改动DuelEngine面大 | Medium | Medium | 逆演栈为DuelEngine附加层, 不改ResolveR2核心 |
| cultivation-a1-001 依赖balance-001 | Low | Medium | 标blocked until balance-001 done |

---

## Definition of Done for this Sprint

- [ ] All Must Have stories completed
- [ ] 全量测试绿 (≥430)
- [ ] off 逐字节守
- [ ] IL 浮点零
- [ ] Code review sprint 2 + sprint 3
- [ ] Story 文件齐全 + completion notes

> ⚠️ **No QA Plan**: This sprint was started without a QA plan. Run `/qa-plan sprint`
> before the last story is implemented. The Production → Polish gate requires a QA
> sign-off report, which requires a QA plan.
