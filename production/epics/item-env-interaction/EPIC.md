# Epic: 物品系统 + 环境交互 (item-env-interaction)

**Layer**: View (Godot) + Core 数据
**Status**: Planned
**Depends**: npc-interaction EPIC (对话闭环)
**Created**: 2026-07-23

## Summary

博德之门式交互深化的第二步：物品可拾取/使用/装备/合成 + 环境可推拉/攀爬/破坏/元素反应。
目标：玩家在世界上不只是"走"和"打"——而是能动**东西**，动**世界**。

## 物品系统

### Stories

- **item-001** 物品数据模型 + 世界掉落 (1d)
  - `ItemDef` 数据行: Id/Name/Kind/Rarity/Tier/Effects/StackSize/Description
  - `ItemKind`: Weapon/Armor/Pill/Talisman/Material/Quest/Key/SkillBook
  - 世界掉落: 战斗后 NPC 概率掉落 + 秘境宝箱 + 采集点
  - Core 侧 `ItemInventory` 侧表 (per-Character 背包，独立于 Resources)

- **item-002** 背包 UI 升级 (1d)
  - 扩展现有 BackpackPanel: 6格→20格 + 分类标签(全部/武器/丹药/材料)
  - 拖拽装备到装备槽 (武器/护甲/法宝/饰品)
  - 右键菜单: 使用/装备/丢弃/查看详情
  - 物品 Tooltip: 名称+品质色+属性+描述

- **item-003** 使用/装备效果系统 (1d)
  - 丹药使用 → 恢复 HP/临时 Buff (ModifyStat)
  - 装备穿戴 → 永久属性加成 (GrantPassive)
  - 法宝激活 → 战斗特殊效果 (AddSituationalAdj)
  - 功法书使用 → 学会新战技

- **item-004** 合成/炼制系统 (1.5d)
  - 丹修炼制: 材料×N → 丹药 (pillStock 消耗+flameTier 门槛)
  - 器修炼宝: 矿石+灵石 → 法宝
  - 配方表: RecipeDef (材料清单+产物+成功率)
  - 合成 UI: 选择配方→放入材料→炼制 (投骰判定成功/失败/大成功)

## 环境交互

### Stories

- **env-001** 环境对象交互 (1d)
  - 可交互对象: 宝箱/门/机关/石碑/篝火
  - Area2D 检测 + 按 E 触发
  - 对象状态机: Closed→Opening→Open (宝箱), Inactive→Active (机关)
  - 推拉对象: CharacterBody2D 碰撞推动 + velocity 叠加

- **env-002** 元素表面系统 (1.5d)
  - 4 种元素表面: 火焰地面(灼烧)/冰面(滑行+减速)/水面(减速)/电击地面(麻痹)
  - 元素反应: 火+冰=水, 水+电=范围电击, 火+水=蒸汽(遮蔽视野)
  - 表面生成: 战技/道具/地形自然产生
  - 表面持续时间: 基于 tick 的衰减

- **env-003** 攀爬/跳跃/潜行 (1d)
  - 跳跃: 按空格→velocity.y = -jump_speed → Platformer 物理
  - 攀爬: 贴墙时按方向键 → 沿墙缓慢上升 (消耗体力)
  - 潜行: Shift 切换 → speed*0.5 + NPC 检测距离减半

### Should Have

- **env-004** 陷阱/机关系统 (1d)
  - 陷阱类型: 地刺/毒雾/落石/传送阵
  - 检测: 悟性≥阈值 → 陷阱可见 (红色高亮)
  - 解除: 器修/阵修 可使用技能解除 (检定)
  - 触发: 踩上 → 伤害+debuff
