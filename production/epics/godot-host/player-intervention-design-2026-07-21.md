# 玩家介入设计方案

**Date**: 2026-07-21 | **Status**: Design | **依赖**: gh-004 命令端口

## 现状

- Core 完备：World.CreateInitial → World.Advance → RuleBrain AI 自驱动
- Godot 轨就位：WorldBridge + 累加器 + 命令端口 + View 层 9 项
- 命令端口 (gh-004)：`CommandIntent` 录制/重放已通，World.SetReplay 可替代 RuleBrain

## 玩家介入交互流

```
玩家创建角色
  → WorldBridge 注入新 Character（WorldFactory.CreateTypicalChar）
  → 选择路径（21路选1）→ PathAssigner.Assign

每 Tick 决策
  → 暂停累加器（玩家决策未完不 Advance）
  → 展示可选意图：[修炼] [游历] [切磋:目标] [探索]
  → 玩家选择/点击 → CommandIntent
  → 命令端口进入队列 → Advance 1 步 → 事件产出
  → View 更新（QTE 帧窗 / 伤害飘字）
```

## 实现分阶段

### P0 — 最小可玩（1 sprint）
- 角色创建面板（Control 节点：选名/选路/确认）
- 暂停/继续按钮（累加器开关）
- 点击节点→Travel / 点击角色→Spar（WorldView 已有 click 检测）
- Train 自动（或按钮）

### P1 — 意图面板（1 sprint）
- 底部意图栏：4 按钮（修炼/游历/切磋/探索）
- 切磋时 QTE 帧窗交互（鼠标点击窗口→成功判定）
- 事件反馈增强（伤害飘字 / 破境特效）

### P2 — 完整玩家循环（2 sprints）
- 角色死亡→继承/新角色
- 背包/功法/战技管理 UI
- 地图探索（点击相邻节点 Travel）
- 存档/读档（命令序列重放）

## 裁决

**P0 就绪可开工**。基础设施（命令端口 + View 点击 + 累加器暂停）全部就位。
建议 Sprint 20 启动 P0 实现。
