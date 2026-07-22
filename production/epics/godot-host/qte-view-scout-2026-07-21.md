# QTE 战斗 View 范围界定

**Date**: 2026-07-21 | **Status**: Scout

## 现状

- Core: `DefenseFrameHook` 已就位（cv-004）— 提供防御帧窗数据（windowStart/windowEnd/frameType）
- Core: `CombatExchangeResult` 已就位（cv-009）— 每次 exchange 产出 Dmg/Reflect/Poise/ChipImmune/FrameHook
- View: `WorldView` + `EventLogPanel` 就位，可接收 OnDomainEvent

## QTE View 范围

### P0 — 防御帧窗 QTE 条
- 战斗双方头顶显示防御帧窗进度条（windowStart→windowEnd）
- `FrameHook.FrameType` 决定颜色：Block=蓝, Evade=绿, Counter=红
- 成功窗口内点击 → ChipImmune / Counter 生效

### P1 — 伤害飘字
- `CombatExchangeResult.DmgToDefender` → 红字飘上
- `ReflectToAttacker` → 橙色反弹字
- `PoiseBreakBonus` → 金色破韧特效

### P2 — 战斗动画
- 攻方→防方冲刺/后撤动画
- 硬直状态（Poise≤0）抖动特效
- Margin→概率的可视化（百分比数字飘出）

## 依赖
- `DefenseFrameHook` ✅ (cv-004)
- `CombatExchangeResult` ✅ (cv-009)
- `WorldBridge.OnDomainEvent` ✅ (gh-002)
- Godot 像素角色 ✅ (Sprint 16)

## 裁决
**Ready for Sprint 18** — 基础设施全就位，纯 View 层实现，不改 Core。
建议先 P0（防御帧窗条），再 P1（飘字），P2（动画）后续。
