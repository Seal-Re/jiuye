# Epic: NPC 交互系统 (npc-interaction)

**Layer**: View (Godot) + Core 桥接
**Status**: Planned
**Depends**: gh-004 (命令端口), play-click-world (点击交互)
**Created**: 2026-07-23

## Summary

让 NPC "活过来"——玩家点击 NPC → 动态对话（上下文填充 + BG3 式检定）→ 结果写回 Core 事件流。
核心闭环：玩家从"旁观者"变为"参与者"——对话/交涉/委托直接改变世界状态。

## 架构

```
玩家点击 NPC
  → Area2D 检测 → InteractState 暂停移动
  → DialoguePanel 弹出
  → ContextBuilder 收集上下文:
      NPC好感度 / 门派 / 当前恩怨 / 玩家声望 / 玩家境界
  → 动态对话树生成:
      选项1: 礼节性交谈 (低风险, 获取信息)
      选项2: 说服/委托 (检定: 境界+声望)
      选项3: 威吓/挑衅 (检定: 战力+境界)
      选项4: 欺瞒/套话 (检定: 悟性+声望)
  → 检定成功/失败 → NPC 反应文本
  → CommandIntent 写回 Core:
      RelationAdjust(+/-N) / GrudgeFormed / FactionReputationChange / QuestAccepted
```

## 检定系统 (BG3-Style Checks)

| 检定类型 | 对应武侠概念 | 检定属性 | DC 范围 |
|---|---|---|---|
| 说服 Persuade | 以理服人/名望压人 | 声望 + 悟性/2 | DC 10-25 |
| 威吓 Intimidate | 以力压人/杀意威慑 | 战力/10 + 境界×5 | DC 12-25 |
| 欺瞒 Deceive | 巧言令色/江湖骗术 | 悟性 + 声望/2 | DC 12-22 |
| 感召 Charm | 侠名远播/正道楷模 | 声望 + 道心/2 | DC 10-20 |

## Stories

### Must Have (Sprint 26-27)

- **npc-001** NPC 交互状态机 + Area2D 检测 (1d)
  - Character2D 新增 `InteractState`：暂停移动/清空 path
  - Area2D 30px 检测 → 玩家靠近 NPC → 显示交互提示 "[E] 交谈"
  - 按 E 键 → 进入对话模式 → 弹出 DialoguePanel
  - 对话期间 Core Tick 暂停 (Paused=true)

- **npc-002** ContextBuilder — 对话上下文收集 (1d)
  - 从 Core 侧查询: NPC好感度 / NPC门派 / 当前恩怨 / 玩家声望
  - 从玩家侧: 境界 / 战力 / 道心 / 所属门派
  - 输出结构化 JSON 上下文 → 供 LLM 或模板引擎消费
  - 确定性：同 NPC+同玩家状态 → 同上下文 (可缓存/可重放)

- **npc-003** DialoguePanel — 动态对话 UI (1.5d)
  - 古风卷轴对话面板: NPC 名字+头像占位+对话文本
  - 4 个选项按钮 (对应 4 种检定类型)
  - 检定投骰动画: 1d20 + 属性加值 vs DC → 成功/失败文本
  - 对话历史滚动 (本次对话内)

- **npc-004** 检定→CommandIntent 写回 (1d)
  - 检定成功/失败 → 生成 CommandIntent:
    - 说服成功 → RelationAdjust(+N) + 信息解锁(秘境线索/功法线索)
    - 威吓成功 → RelationAdjust(-N 但短期服从) + 物品获取
    - 欺瞒成功 → 虚假信息注入 + 无 Relation 变化
    - 检定失败 → RelationAdjust(-N) 或 GrudgeFormed (威吓失败)
  - WorldBridge.QueueDialogueOutcome() → CommandIntent 入队

### Should Have (Sprint 28)

- **npc-005** ~~LLM 对话生成接入~~ **(废稿——2026-07-23 用户裁定移除)**
  - 理由：纯规则模板引擎已能满足 NPC 人格一致性需求，LLM 调用增加延迟/成本/复杂度，不符合"确定性涌现"内核设计。
  - 替代方案：ContextBuilder 模板引擎 + 人格词库 → 确定性对话生成

- **npc-005 (替代)** 规则模板对话引擎 (1d)

- **npc-006** 委托/任务系统骨架 (1.5d)
  - NPC 可发布简单委托: 送信/采集/讨伐/护送
  - QuestLog 侧表: 委托状态 (Accepted/InProgress/Completed/Failed)
  - 完成委托 → 声望变化 + 关系变化 + 奖励
  - 委托超时未完成 → 关系恶化
