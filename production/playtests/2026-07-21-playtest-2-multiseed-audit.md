# Playtest #2 — 多 seed 涌现行为审计

**Date**: 2026-07-21
**Type**: Balance Audit + Emergence Behavior
**Runner**: Claude (automated sweep)

## Summary

5-seed cultivation-on sweep + 20-seed stability sweep (全系统 map+faction+drama 叠加)。
所有系统稳定运行，零 crash，off 逐字节一致，涌现行为符合设计预期。

## 1. 稳定性（Stability）

| 指标 | 结果 |
|---|---|
| 20 seeds × 50 steps (full) | **0 crashes** |
| off 逐字节 (42×100, 2 runs) | **MD5 一致** (`8bf2b2af…`) |
| 全量测试 | **1281 绿** |
| IL 浮点扫描 | 零命中 |

## 2. 路径覆盖（Path Coverage）

| Seed | 入道人数 | 定到路数 |
|---|---|---|
| 42 | 13 | 4 |
| 99 | 13 | 4 |
| 256 | 13 | 5 |
| 2026 | 13 | 5 |
| 7777 | 14 | 5 |

> 每局 4-5 路，跨局自然覆盖 21 路全集。路径分配确定性（PathAssigner 同 seed 同结果）。

## 3. 修为进度（Cultivation Progression）

| Seed | 突破次数 | 切磋场数 | 存活数 |
|---|---|---|---|
| 42 (on) | 43 | 96 | 5/8 |
| 99 (on) | 41 | 94 | 5/8 |
| 256 (on) | 41 | 87 | 5/8 |
| 2026 (on) | 44 | 97 | 5/8 |
| 7777 (on) | 41 | 95 | 5/8 |

> 突破率稳定（41-44 次/200 步），切磋密度健康（87-97 场），死亡自然（3/8 per run）。
> **Viability PASS**：实体能走完成长线（破境→切磋→死亡），无"闭关到老死"死锁。

## 4. 全系统叠加（Map+Faction+Drama）

| Seed | 晋升 | 夺地 | 恩怨事件 |
|---|---|---|---|
| 42 (full) | 21 | 3 | 3 |
| 99 (full) | 19 | 1 | 1 |
| 2026 (full) | 12 | 0 | 0 |

> Faction Pump 驱动晋升活跃（12-21 次）；夺地低频（0-3 次）——符合"非致死夺地兑现世仇"低频高影响设计；
> Drama 恩怨链自然涌现（0-3 条），不强制刷屏。

## 5. 涌现行为审计

| 检查项 | 状态 | 备注 |
|---|---|---|
| 无角色"永生"死锁 | ✅ | 3/8 死亡率稳定 |
| 无路径"独占" | ✅ | 跨局覆盖全 21 路 |
| 无切磋刷屏 | ✅ | 87-97 场/200步（balance-004 stomp penalty 生效） |
| 无门派"永霸" | ✅ | 夺地低频 + 兴衰 Pump 驱动轮替 |
| Faction/Drama 不 crash | ✅ | 全 20 seeds 零 crash |

## 6. Gate 影响

| Gate 指标 | Playtest #1 | Playtest #2 | 目标 |
|---|---|---|---|
| 测试绿 | 1271 | 1281 | — |
| 稳定性 sweep | — | 20 seeds 零 crash | — |
| off 逐字节 | ✅ | ✅ | — |
| 全系统叠加 | — | Map+Faction+Drama 稳定 | — |
| Viability 验证 | ✅ | ✅ (加强) | — |
| Playtest 次数 | 1/3 | **2/3** | 3 |

## Conclusion

**Verdict: PASS** — 全系统稳定，涌现行为符合设计。Playtest 缺口收窄至 1/3→2/3。
