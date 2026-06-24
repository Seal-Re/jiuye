# Story 001: 21路道心资源表

> **Epic**: cultivation-a2
> **Status**: Not Started
> **Last Updated**: 2026-06-24
> **Layer**: Core
> **Type**: Logic
> **Estimate**: 中 (1d)
> **Depends**: cultivation-a1-rest (FSM+三劫+寿元 done)
> **ADR**: adr-0003-cultivation-off-byte-identical
> **GDD**: cultivation-system.md §3.5；A3-FINAL §3；A123 §A.2.3

## Context

A3-FINAL §3 定义了 12 路道心资源表。A123 §A.2.3 补完 9 路扩展到 21 路。每路道心有：`daoHeart_init`（初始值乘子）、`daoHeartGain` 来源（事件→道心增益）、`innerDemonSource` 来源（事件→心魔增益）。数据驱动：加路=加数据行，不改引擎。

## Acceptance Criteria

- [ ] 1.1 21 路 daoHeart_init 乘子表落地（default *2, soul/buddhist *3, body/ghost *2, 新增 9 路各有初值）
- [ ] 1.2 每路 ≥3 个 daoHeart 增益来源（事件/动作→道心增量，整数）
- [ ] 1.3 每路 ≥3 个 innerDemon 来源（事件/动作→心魔增量，整数）
- [ ] 1.4 道心表数据驱动——加路=加数据行，不改 CultivationState/引擎
- [ ] 1.5 佛修（buddhist_golden_body）破戒规则初值：vow 折半非归零
- [ ] 1.6 全路 standalone 测试（每路 Def 可独立验证 daoHeart_init > 0）
- [ ] 1.7 off 模式：道心表存在但 CultivationState.daoHeart=0（不初始化，off 守）

## Implementation Notes

**数据模型**：`DaoHeartDef` record —— PathId, InitMultiplier: int, GainSources: List<DaoHeartGain>, DemonSources: List<InnerDemonSource>。注册到 `DaoHeartRegistry`（类似 ArtifactRegistry 模式）。

**21 路初值乘子（A3 §3 + A123 §A.2.3）**：

| 路 | PathId | InitMultiplier | 注 |
|----|--------|:---:|---|
| 剑修 | sword_immortal | *2 | 剑心通明 |
| 体修 | ti_xiu_hengshi | *2 | 肉身成圣 |
| 法修 | fa_xiu | *2 | 道法自然 |
| 阵修 | array_formation | *2 | 阵道辅助（Aux，道心弱） |
| 器修 | qixiu_artificer | *2 | 器道辅助（Aux） |
| 魂修 | soul_divine_sense | *3 | 神魂澄澈 |
| 命修 | ming_fate_causality | *2 | 命理推演 |
| 丹修 | dan_xiu | *2 | 丹道辅助（Aux） |
| 鬼修 | gui_xiu_yang_hun | *2 | 阴魂凝实 |
| 佛修 | buddhist_golden_body | *3 | 佛法金刚 |
| 雷修 | lei_xiu | *2 | 雷霆正心 |
| 驭兽 | yu_shou | *2 | 兽性守心 |
| 儒修 | ru_xiu_haoran | *2 | 浩然正气 |
| 魔修 | mo_xiu_xinmo | *2 | 魔心守一（初值低，涨得慢） |
| 妖修 | yao_xiu_huaxing | *2 | 化形守灵 |
| 血修 | xue_xiu_xuesha | *2 | 血煞收心 |
| 蛊修 | du_gu_xiu | *2 | 控蛊定心 |
| 符修 | fu_xiu_fulu | *2 | 符道通玄 |
| 傀儡 | kuilei_shi | *2 | 机心通玄 |
| 音修 | yin_xiu_yuedao | *2 | 心境澄明 |
| 因果 | yinguo_faze | *2 | 勘破因果 |

**innerDemon 来源示例（A123 §A.2.3）**：
- 儒修：失德+3, 浩然枯+2
- 魔修：噬心反噬+5, 堕魔+3
- 妖修：兽性嗜杀+4, 化形失控+3
- 血修：血煞过载+5, 杀业+4
- 蛊修：蛊噬主+5, 毒侵心+3
- 符修：符箓反噬+2, 储备耗尽+2
- 傀儡：机心失控+3, 神识带宽过载+2
- 音修：心乱+3, 入魔音+3
- 因果：天谴债+5, 逆天反噬+4

**daoHeart 增益来源示例**：
- 儒修：养气+5, 教化+6, 善行+4
- 魔修：凝魔心+2, 守心+3
- 妖修：化形得道+5, 守灵台+3
- 血修：凝血神+2, 收敛杀心+3
- 蛊修：控蛊定心+4, 炼毒通玄+3
- 符修：符道通玄+5, meditate+3
- 傀儡：机心通玄+5, 人偶相照+4
- 音修：心境澄明+5, 乐道+4
- 因果：勘破因果+4, 顺天+3

## Out of Scope

- 道心/心魔的运行时 gain/loss 操作（→ story-002）
- 佛修破戒修正的完整实现（→ story-003）
- 道心进 Tribulation ResistTerms 的接线（→ story-021，已半完成）

## Test Evidence Requirement

**Type**: Logic — automated unit tests. 21 standalone tests（每路 daoHeart_init > 0）+ registry load test + off mode test.
