# Story 008: turns=1 控制哑弹裁决（tick 时序 off-by-one vs 轻控设计意图）

> **Epic**: balance-cross
> **Status**: Ready-for-Decision（阻塞在设计裁决 — 非 Ready-for-Dev）
> **Layer**: Core
> **Type**: Logic
> **TR**: TR-BAL-001（承 balance-007 机制侧；控制经济健康化）
> **Risk**: HIGH（触 DuelEngine tick 时序 + 影响 21 路 4 招签名控制 + off 逐字节 B.3）
> **Created**: 2026-07-06（balance-007 核验副产物，A.8 不静默丢）
> **Last Updated**: 2026-07-06

## 问题陈述（机器证据，2026-07-06 主控实测）

`mihun`(迷魂引)/`soulLock`(定身)/`lawPrison`(律狱)/`voidPrison`(时空囚) —— 21 路中 4 条路的签名控制招，全为 `Modules.Control(key, 1)`（turns=1）。因 `DuelEngine` 的 tick 时序：

- `IsControlled` 在回合**开始**查（`ResolveR2` L102-103）；
- `TickDots` 在回合**末**递减+移除控制（L199 → L421-427）。

→ turns=1 的控制在回合 N 中挂上（`ResolveExchange` Control 分支），回合 N 末即递减到 0 移除，**回合 N+1 开始的 start-check 从未见到它** → **从不拒止任何一次行动**。

**实测证据**（等 PE 对拍、攻方唯一优势=控制）：
| control turns | 结果 | 解读 |
|---|---|---|
| turns=1 | `margin=0`（平局 tiebreak） | 控制**完全无效**（哑弹） |
| turns=2 | `margin=72`（攻方碾压） | 锁人生效 |
| turns=3 | `margin=72`（攻方碾压） | 锁人生效 |

**通用公式**（balance-007 钉死）：turns=N 实际拒止 **N−1** 回合。故 turns=1 → 拒止 0 回合。

## GDD 交叉证据（design/gdd/combat-system.md:83-88）

combat GDD 按**时机窗口类型**分类控制：
- `困/定身 stun` = **即时**, ★★★★（例：勾魂索命/须弥困界）
- `打断蓄招 interrupt` = **即时**, ★★★★（例：金身震/五雷轰顶）
- `封印/沉默`、`减速/拉距`、`心智控制` = 回合choice

⚠️ **关键**：GDD 表只分类"时机窗口类型"（即时 vs 回合choice），**未钉控制应锁几回合**（duration 语义）。故 turns=1 是否该拒止行动 = **设计空白，需裁决**，非纯 bug。"迷魂/勾魂"究竟是"即时打断（仅扰动当回合）"还是"定身（跨回合锁）"，决定 turns=1 的正确行为。

## 裁决备选（交用户 — 不自作主张定方案）

| 方案 | 语义定性 | turns=1 行为 | 实现 | 影响面 | off 逐字节(B.3) |
|---|---|---|---|---|---|
| **A 维持现状（合理化）** | turns=1 = "即时打断当回合出招"（贴 GDD interrupt 语义），无跨回合锁 | 哑弹→显式设计：轻控仅当回合扰动 | 0 代码改；补语义文档 + 测试固化"turns=1 不跨回合锁" | 零 | 天然守 |
| **B 修时序** | turns=N 应实际拒止 N 回合（挂控下一回合起锁 N 回合） | turns=1 锁 1 回合 | 改 tick 顺序 or 挂控 +1；**动全部控制**（gouhun 2→实际锁 2、arrayLock 3→锁 3） | 高：on 对拍全轨迹变，需重验 balance-007 CD/DR + 全 21 路差分 + C1/C2 | off 不调 DuelEngine 天然守；**但 on 对拍轨迹全变，需重跑差分核"预期改善"非"回归"** |
| **C 分级语义** | 显式区分 `interrupt`(当回合) vs `stun`(跨回合)；4 招按 GDD 归"即时打断"档 | turns=1=打断（当回合），turns≥2=定身（跨回合） | 加语义标记/分类；21 路控制招归档 | 中：语义分类 + 4 招确认归档 | 天然守（不改 turns≥2 现有轨迹） |

**主控倾向（供参考，非裁决）**：从 GDD"困/定身/打断均列**即时**窗口"看，方案 **A 或 C** 更可能贴原设计（迷魂/勾魂作即时扰动，非长控）。方案 **B** 风险最高——动确定性对拍全轨迹，且与刚落地的 balance-007 CD/DR 交互需全面重验。**但语义定性是设计大方向，交用户裁。**

## Acceptance Criteria（占位 — 随裁决细化）

- [ ] 8.1 **设计裁决记录**：选定方案（A/B/C）+ 理由，落 story + 视需要立 ADR（若改 tick 语义=架构级）。
- [ ] 8.2 **语义固化测试**：turns=1 行为符合裁决（A：断言不跨回合锁；B：断言锁 1 回合；C：断言按档位）。
- [ ] 8.3 **21 路 4 招一致性**：`mihun`/`soulLock`/`lawPrison`/`voidPrison` 语义与裁决一致（B.4 不漏路）。
- [ ] 8.4 **若选 B**：off 逐字节 + balance-007 CD/DR（`ControlCooldownTests`）+ C2 碾压单调 + C3 辅助豁免全绿不退；on 对拍轨迹变化经差分核为"预期改善"。
- [ ] 8.5 **全量绿 + 浮点扫描**（B.2）：新增逻辑纯整数，全量测试绿贴计数。

## Out of Scope

- turns≥2 控制的 CD/DR（已由 balance-007 done 治理）。
- 即时窗口的 Godot 宿主浮点版（combat GDD §即时窗口模型 Core 退化版之外，属 View 层后期）。
- 新增控制类型/新招（本 story 只裁既有 4 招 turns=1 语义）。

## Dependencies

- Depends on: balance-007（CD/DR done，本 story 是其核验副产物）；combat-system.md GDD（控制分类源）。
- Blocks: 若选 B → 影响所有含控制招的对拍平衡观测。
- Decision owner: 用户（设计意图裁决）。

## Test Evidence

**Story Type**: Logic
**Required evidence**: 随裁决定 —— 方案 A/C：语义固化单测；方案 B：off 逐字节 + 全差分回归。
**Status**: [ ] 阻塞在 8.1 设计裁决，未进实现。
