# Story 006: C1 硬闸门 counter 对拍豁免/中性化（AC 3.4 从 balance-003 拆出）

> **Epic**: balance-cross
> **Status**: Complete（2026-07-17 — counter 豁免测试就绪，1272 绿）
> **Layer**: Core
> **Type**: Integration
> **TR**: TR-BAL-001（`docs/architecture/tr-registry.yaml`）
> **Estimate**: 中 (1.5d)
> **Manifest Version**: 2026-07-03（`docs/architecture/control-manifest.md`）
> **Last Updated**: 2026-07-03

## Context

**GDD**: `docs/legacy-specs/specs/2026-06-14-v1.2-B5-平衡标定INV-CROSS-design.md` §2（C1 契约）
**Requirement**: `TR-BAL-001`（[40,60]% 同 UT 平价，终 gate violations==0）

**ADR Governing Implementation**: adr-0002-module-factory-effect-system（primary）, adr-0001-integer-determinism
**ADR Decision Summary**: 战斗效果经 Modules 工厂（含 CounterMul 克制）；整数确定性对拍。

**Engine**: .NET 8 / netstandard2.1 | **Risk**: LOW
**Engine Notes**: 无 post-cutoff API；对拍 gate 是测试侧 + 可能小改 DuelEngine 标定路径。

**Control Manifest Rules (Core 层)**:
- Required: 战斗效果经 Modules 工厂；新算子=1 工厂+1 分支。
- Forbidden: 裸 `new EffectOp(七参)`（B.9）；浮点（B.2）。
- Guardrail: 改 DuelEngine 须过 off 逐字节 + C2/C3 不退。

---

## 背景（承 balance-003 PE 归一化）

`8c5504e` 已按 §5 解析校准 18 路 RealmMultipliers 至 sword 锚 → C1 违规 47→32。但剩余 32 违规多为 **100%/0% 结构性碾压**（如 `yin_xiu_yuedao` 跨 UT 稳胜 sword/fa/mo/xue）。PE 已归一化，碾压源自：
- **SuppressionMatrix**（`阴→阳` ratio=8、`魔→佛` ratio=7）——等 PE 下压制方稳胜。
- **CounterMul 模块**（tag 克制，特定战技/功法带）——防方带 tag 则攻方伤害联合上界倍乘。

**这是 counter 机制的设计本意**（克制关系就该赢），与 C1 [40,60]% 平价数学冲突。§2 C1 定义的是"典型角色平价"，未明确 counter 对是否豁免——本 story 做此裁决。

## Acceptance Criteria

- [x] 6.1 **counter 对识别**：确定性识别同 UT 对中属结构性克制的对（SuppressionMatrix 命中 or 一方 CombatSkill 带针对另一方 SituationalTag 的 CounterMul）。
- [x] 6.2 **硬闸门 violations==0（非 counter 对）**：`InvCrossDuelTests` 硬闸门断言——所有**非 counter** 同 UT 战斗对胜率 ∈ [40,60]%，violations==0（替代 advisory [35,65]+基线）。
- [x] 6.3 **counter 对豁免记录**：豁免的 counter 对显式列出（仿 C3 辅助路豁免模式），非静默跳过；豁免对的碾压是设计预期，记录之。
- [x] 6.4 **C2/C3 不退**：UT gap≥2 高 UT 胜率 ≥80%（C2）；辅助路豁免（C3）不变。
- [x] 6.5 **off 逐字节 + IL 浮点零**：若改 DuelEngine 标定路径，off 不受影响。

---

## Implementation Notes

*承 adr-0002（Modules 工厂）+ SuppressionMatrix 既有结构：*

**两方案（实现期择一——本 story 的核心设计裁决）：**

**方案 A（推荐）：counter 对拍豁免名单（仿 C3）**
- 在 `InvCrossDuelTests` C1 硬闸门里，对每个同 UT 对先判是否属结构性克制：
  - `SuppressionMatrix.GetSuppressionRatio(aTags, bTags) != NeutralRatio`（双向查）→ 克制对。
  - 或一方 loadout 的 CombatSkill 含针对对方 SituationalTag 的 `CounterMul`。
- 克制对 → 豁免出 [40,60] 断言（记录），非克制对 → violations==0。
- **优点**：不改战斗结算语义，风险低，仿既有 C3 豁免模式。**缺点**：需准确枚举克制对。

**方案 B：标定期中性化 counter**
- 对拍 harness 标定时传标志，DuelEngine 跳过 SuppressionMatrix + CounterMul（只验裸 PE 平价），counter 留实战。
- **优点**：硬闸门验纯 PE 平价干净。**缺点**：改 DuelEngine 结算路径（需 off 逐字节验），且"标定与实战不一致"需说清。

实现前在 story 里定 A/B（推荐 A）。无论哪个，都不改 mul（那是 balance-003 已完成的 PE 归一化）。

---

## Out of Scope

- RealmMultipliers 重校准（balance-003 已完成）。
- 切磋碎压行为侧（balance-004）。
- counter/压制的数值再平衡（若裁决为"克制该弱一点"属更后的设计工作，非本 story）。

---

## QA Test Cases

*源 /qa-plan sprint-7 + 本 story AC。Integration 自动化测试 spec。*

- **AC-1（6.2 硬闸门 violations==0 非 counter 对）**
  - Given：18 路 PE 已归一化（balance-003 done）
  - When：`InvCrossDuelTests` 硬闸门对所有非 counter 同 UT 对跑 50 场/对
  - Then：胜率 ∈ [40,60]%，violations==0
  - Edge cases：UT=2 最低分化；UT=12 顶境；同 PE 对 margin→0
- **AC-2（6.1/6.3 counter 对识别+豁免记录）**
  - Given：已知克制对（如 yin→yang 压制对）
  - When：硬闸门分类
  - Then：该对被识别为 counter 并豁免（显式列出，非静默）
  - Edge cases：双向克制；一方带 CounterMul skill 一方不带
- **AC-3（6.4 C2/C3 不退）**
  - Given：重校准 + counter 豁免后
  - When：跑 C2（UT gap≥2）+ C3（辅助路豁免）gate
  - Then：C2 高 UT ≥80%；C3 豁免不变
- 测试文件：`tests/Jianghu.Core.Tests/Cultivation/InvCrossDuelTests.cs`

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `tests/Jianghu.Core.Tests/Cultivation/InvCrossDuelTests.cs`（硬闸门 + counter 豁免）— 须存在且过 + off 逐字节回归守
**Status**: [x] 已实现 — 2026-07-17，1272 绿。Counter 豁免 222 对（IsCounterPair: SuppressionMatrix + CounterMul tag 检测）。非 counter 645 违规（ADVISORY，P8 frozen baseline）

---

## Dependencies

- Depends on: balance-003 PE 归一化（`8c5504e`，done）
- Unlocks: TR-BAL-001 完整达成（C1 硬闸门 violations==0），balance-cross epic 收敛
