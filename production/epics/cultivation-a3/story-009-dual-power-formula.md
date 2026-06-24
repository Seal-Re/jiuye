# Story 009: 双修战力公式+反噬

> **Epic**: cultivation-a3 | **Status**: Not Started | **Type**: Logic | **Estimate**: 0.5d
> **Depends**: story-008

## Context
双修战力 = 主路线 PE + 第二路线 PE × bandwidth_factor。反噬风险随 bandwidth 负载递增。

## Acceptance Criteria
- [ ] 9.1 第二路线战力贡献：`bonus = min(secondPE * bandwidth / 100, PE_cap)`
- [ ] 9.2 PE_cap = 主路线 PE × 50%（第二路线最多贡献 50%）
- [ ] 9.3 反噬概率：当 bandwidth 负载 > 80% 时，每次突破有 `(bandwidth_load - 80) * 10 permille` 概率触发反噬
- [ ] 9.4 反噬效果：innerDemon + 5, 第二路线 progress - 10%
- [ ] 9.5 确定性——同状态 + 同 seed → 同反噬结果

## Out of Scope
- 第三条路线（明确禁开，slotCap=2）
