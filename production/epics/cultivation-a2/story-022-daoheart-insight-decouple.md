# Story 022: DaoHeart-Insight去耦实证

> **Epic**: cultivation-a2
> **Status**: Complete （A.6 审计 2026-06-26 订正：git/代码证据证实已实现，台账滞后）
> **Last Updated**: 2026-06-24
> **Layer**: Core
> **Type**: Logic
> **Estimate**: 小 (0.5d)
> **Depends**: story-001
> **ADR**: adr-0001-integer-determinism
> **GDD**: A3-FINAL §3.2

## Context

A3-FINAL §3.2 定义了 INV-DECOUPLE 不变量：daoHeart 与 Insight 的相关性（corr）必须 < 0.7。两者不应线性绑定——高 Insight 角色不应自动获得高 daoHeart（反之亦然），保证角色多样性。

## Acceptance Criteria

- [ ] 22.1 `INV-DECOUPLE`：在 100+ NPC 模拟中，`corr(daoHeart, Insight) < 0.7`（皮尔逊/斯皮尔曼简化为秩相关）
- [ ] 22.2 daoHeart 增益来源不含 `Insight` 直接映射（daoHeart 来自事件选择，非 stat 推导）
- [ ] 22.3 Insight 用于 Epiphany 概率，但不直接影响 daoHeart（epiphany 可能给 daoHeart+5，但不保证）
- [ ] 22.4 实证测试：生成 100 NPC，运行 200 tick，计算相关性
- [ ] 22.5 off 模式不适用（daoHeart 恒为 0）

## Implementation Notes

**相关性测试**（非正式统计，简化为工程判断）：
```
Generate 100 chars with varied stats
Run 200 ticks with DailyMode + encounters
Compute rank correlation between daoHeart and Insight
Assert corr < 0.7
```

此测试是**诊断性**的——不修改任何代码，仅验证设计不变量在实现中得到保持。

## Out of Scope

- 正式的统计测试套件——仅做工程级别的合理性检查

## Test Evidence Requirement

**Type**: Integration — integrated test. 100 NPC × 200 tick correlation test, off mode skip.
