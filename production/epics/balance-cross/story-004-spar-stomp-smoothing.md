# Story 004: 切磋碎压平滑 — 无势均对手则降切磋意愿

> **Epic**: balance-cross
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **TR**: TR-BAL-001（`docs/architecture/tr-registry.yaml`）
> **Estimate**: 中 (1d)
> **Manifest Version**: 2026-07-03（`docs/architecture/control-manifest.md`）
> **Last Updated**: 2026-07-03

## Context

**GDD**: `docs/legacy-specs/specs/2026-06-14-v1.2-B5-平衡标定INV-CROSS-design.md`（§2 C1 平价的行为侧）
**Requirement**: `TR-BAL-001`（同 UT 平价——避免无意义碾压对拍，观察者可读性）

**ADR Governing Implementation**: adr-0001-integer-determinism（primary）
**ADR Decision Summary**: 决策层纯整数确定性；`Jianghu.Decide` 禁浮点、同种子逐字节复现。

**Engine**: .NET 8 / netstandard2.1 | **Risk**: LOW
**Engine Notes**: 无 post-cutoff API；纯 RuleBrain 效用整数运算。

**Control Manifest Rules (Core 层)**:
- Required: 决策整数确定性；RuleBrain 决策逻辑改动须过 RuleBrain 决定性测 + off 逐字节。
- Forbidden: 浮点进决策路径（B.2）；System.Random（BannedApi）。
- Guardrail: off 模式（无修为）行为逐字节不变（SelfPower=0 回退路径已在 `16dc54c` 建立）。

---

## 背景（承 balance-003）

`16dc54c` 已让 `RuleBrain` 切磋对手选择的战力度量对齐 DuelEngine（on 用 PE）。但实测：某节点若**只有悬殊对手**在场，brain 仍挑"最不悬殊"的那个切磋 → 碾压刷屏（`独孤求败 差999` 反复）。度量对了，但"无势均对手时是否该切磋"这个决策没做——brain 宁可打碾压架也不转修炼/游历。

## Acceptance Criteria

- [ ] 4.1 **无势均则降意愿**：当所有 nearby 对手的 |SelfPower − target.Power| 均超阈值（悬殊）时，Spar 效用降到低于 Train/Travel → brain 转修炼/游历，不打无意义碾压架。
- [ ] 4.2 **有势均仍切磋**：存在势均对手（gap 在阈内）时，Spar 效用正常（不误伤 `16dc54c` 已修的势均选择）。
- [ ] 4.3 **碾压占比降**：多 seed 长跑，切磋 margin≥999 碾压占比显著降（目标 <25%，实测全体分布非 CLI 末6采样）。
- [ ] 4.4 **off 逐字节**：off 模式（无修为，SelfPower=0）行为不变（回退 raw stats 路径）。
- [ ] 4.5 **确定性**：RuleBrain 决策逻辑改动纯整数、同种子逐字节复现。

---

## Implementation Notes

*承 adr-0001（整数确定性）+ RuleBrain 既有效用结构：*

`RuleBrain.Evaluate` 的 `ActionType.Spar` 分支（`src/Jianghu.Core/Decide/RuleBrain.cs`）当前：`rival = max(0, 600 − 15×gap)`——gap 越大 rival 越低，但**下限 0 后 Spar 仍可能因 archMartial 基数被选中**。改法（受控，逻辑最小改）：
- 当 `BestSparTarget` 的 gap 超"势均阈值"（如 gap > SelfPower/2，整数）时，Spar 效用再减一个"无意义碾压"惩罚（使其稳低于 Train）。
- 阈值用整数常量或 LimitsConfig 旋钮（承数据驱动，勿硬编码魔数——可加 `LimitsConfig.SparRivalGapCap`）。
- **RuleBrain 决策逻辑保持 gap-based 结构**（承"RuleBrain UNCHANGED"传统），仅加"无势均降意愿"一层。

---

## Out of Scope

- Story 006：C1 硬闸门 counter 对拍豁免（这是对拍 gate 侧，非 brain 行为侧）。
- 重名显示（郑寻欢与郑寻欢）：表现层，接 Unity 时处理（用户归属）。
- counter/压制的数值再平衡：属 balance-006 / 更后。

---

## QA Test Cases

*源 /qa-plan sprint-7（2026-07-03）+ 本 story AC。Logic 自动化测试 spec。*

- **AC-1（4.1 无势均降意愿）**
  - Given：一个 cultivation-on 世界，某角色节点仅有悬殊对手（|PE gap| 均 > 阈值）
  - When：该角色 `RuleBrain.DecideAsync`
  - Then：选择的 ActionType ≠ Spar（转 Train 或 Travel）
  - Edge cases：节点仅 1 悬殊对手；节点无人（nearby=0，Spar 本就非法）
- **AC-2（4.2 有势均仍切磋）**
  - Given：节点有势均对手（gap 在阈内）
  - When：DecideAsync
  - Then：Spar 效用正常，势均对手可被选为切磋对象
  - Edge cases：势均 + 悬殊混合（应选势均切磋）
- **AC-3（4.3 碾压占比降）**
  - Given：多 seed（≥3）长跑 800 步 cultivation-on 世界
  - When：统计全体 DuelResolved margin
  - Then：margin≥999 占比 < 25%
- **AC-4（4.4 off 逐字节）**
  - Given：off 模式世界（无修为）
  - When：跑 200 步
  - Then：与既有 OffByteIdentical 基线逐字节一致
- 测试文件：`tests/Jianghu.Core.Tests/Sim/SparTargetPowerMetricTests.cs`（扩）或 `Decide/RuleBrainTests.cs`

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/Jianghu.Core.Tests/Sim/SparTargetPowerMetricTests.cs`（扩）— 须存在且过 + off 逐字节回归守
**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: balance-003 PE 归一化（`16dc54c` 度量对齐 + `8c5504e` mul 校准，done）
- Unlocks: 无（切磋行为侧收尾）
