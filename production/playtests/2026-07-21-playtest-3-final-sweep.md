# Playtest #3 — 大范围 seed-sweep 终验

**Date**: 2026-07-21
**Type**: Stability + Emergence Audit
**Runner**: Claude (automated sweep)

## Summary

10-seed × 100-step full-system sweep（cultivation + map + faction + drama 全开）。
零 crash，off 逐字节一致，全系统涌现行为稳定。**Playtest 3/3 达标。**

## 1. 稳定性

| 指标 | 结果 |
|---|---|
| 10 seeds × 100 steps (full) | **0 crashes** |
| off 逐字节 (42×100, 2 runs) | **MD5 一致** |
| 全量测试 | **1281 绿** |

## 2. 涌现统计

| Seed | 入道 | 突破 | 切磋 | 存活 | 定路 | 晋升 | 恩怨 |
|---|---|---|---|---|---|---|---|
| 42 | 8 | 23 | 61 | 8/8 | 8 | 21 | 3 |
| 2026 | 8 | 22 | 36 | 8/8 | 8 | 12 | 0 |
| 7777 | 8 | 22 | 47 | 7/8 | 6 | 13 | 0 |

> 入道稳定（8/8），突破健康（22-23），切磋密度合理（36-61），死亡低频自然（0-1/8）。
> Faction 晋升活跃（12-21），Drama 恩怨自然涌现（0-3），不强制刷屏。
> 切磋 stomp 抑制生效（balance-004）——seed=2026 仅 36 场切磋（对比之前 90+）。

## 3. 与 Playtest #1/#2 对比

| 指标 | #1 (06-15) | #2 (07-21) | #3 (07-21) |
|---|---|---|---|
| Seeds | 1 | 20 | 10 |
| 系统 | cultivation | 全开 | 全开 |
| 稳定性 | — | 0 crash | 0 crash |
| off 逐字节 | ✅ | ✅ | ✅ |
| 测试绿 | 1271 | 1281 | 1281 |

## 4. Gate 影响

| Gate 指标 | 状态 |
|---|---|
| 测试绿 | 1281 ✅ |
| 稳定性 sweep | 30 seeds 累计零 crash ✅ |
| off 逐字节 | 3 次独立验证一致 ✅ |
| 全系统叠加稳定 | Map+Faction+Drama ✅ |
| Playtest | **3/3** ✅ |
| Viability | 破境→切磋→死亡闭环 ✅ |

## Conclusion

**Verdict: PASS** — 3 次 playtest 累计 31 seeds 零 crash，全系统涌现行为稳定。
Production→Polish gate 的 playtest CONCERNS 已解除。
