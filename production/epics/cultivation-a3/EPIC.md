# Epic: 修炼 A.3

**Layer**: Feature
**Status**: In Progress
**GDD**: design/gdd/cultivation-system.md §3.6；深度源 A123 §A.3；registry-research §4
**Governing ADRs**: adr-0001-integer-determinism, adr-0003-cultivation-off-byte-identical
**Engine Risk**: LOW（.NET 8 纯整数）
**Created**: 2026-06-15（迁自 TASKS.md）
**Updated**: 2026-06-25（story breakdown）

## Summary
修炼 A.3——转职 / 觉醒 / 双修 + RiskModifier 反噬系统 + 正邪分叉。三种跃迁形态均挂 A.2 奇遇框架（storylet executor），数据驱动。

## Scope
- **转职 Transmute**（story-001~003）：TransitionDef 数据模型，PathId 迁移，carryover 规则，realm 映射
- **觉醒 Awaken**（story-004~006）：血统/体质觉醒触发器，同一路线高阶解锁，废材逆转
- **双修 Dual**（story-007~009）：第二路线绑定，slotCap/神识带宽，兼容矩阵 excludes，战力加成
- **RiskModifier**（story-010~011）：通用反噬系统，probability_permille 整数，cooldown
- **正邪分叉**（story-012~013）：道德阈值→善恶反派对偶，复用奇遇框架
- **集成+硬化**（story-014~015）：Transition→A.2 奇遇集成，确定性+off 逐字节，审计终验

## Dependencies
**Unblocked by**: cultivation-a2（A.2 道心/奇遇 已完成）
**Blocks**: 无

## Stories

| # | Story | Phase | Priority | Est | Status |
|---|-------|-------|----------|:--:|--------|
| 001 | TransitionDef 数据模型 | 转职 | Must Have | 0.3d | Not Started |
| 002 | PathId 迁移+CultivationState 改造 | 转职 | Must Have | 0.3d | Not Started |
| 003 | 标准转职路线数据（剑修→剑仙,武夫→修真等） | 转职 | Should Have | 0.3d | Not Started |
| 004 | AwakeningDef 数据模型 | 觉醒 | Must Have | 0.3d | Not Started |
| 005 | 觉醒触发器（濒死/秘境/血统法器） | 觉醒 | Must Have | 0.3d | Not Started |
| 006 | 觉醒→功法/战力解锁 | 觉醒 | Should Have | 0.3d | Not Started |
| 007 | DualPathDef 数据模型+slotCap | 双修 | Must Have | 0.3d | Not Started |
| 008 | 双修兼容矩阵+bandwidth | 双修 | Must Have | 0.3d | Not Started |
| 009 | 双修战力公式+反噬 | 双修 | Should Have | 0.5d | Not Started |
| 010 | RiskModifier 反噬系统 | 反噬 | Must Have | 0.3d | Not Started |
| 011 | RiskModifier 数据+cooldown | 反噬 | Should Have | 0.2d | Not Started |
| 012 | 正邪分叉框架 | 分叉 | Should Have | 0.3d | Not Started |
| 013 | 正邪→天劫强化/正道围剿 | 分叉 | Nice to Have | 0.3d | Not Started |
| 014 | A.3 不变量硬化+确定性 | 硬化 | Must Have | 0.5d | Not Started |
| 015 | A.3 审计员终验 | 硬化 | Must Have | 0.3d | Not Started |

## Definition of Done
- [ ] 转职系统实现 + 测试（story-001~003）
- [ ] 觉醒系统实现 + 测试（story-004~006）
- [ ] 双修系统实现 + 测试（story-007~009）
- [ ] RiskModifier 实现 + 测试（story-010~011）
- [ ] 正邪分叉实现 + 测试（story-012~013）
- [ ] 不变量硬化 + 确定性守（story-014）
- [ ] 审计终验（story-015）

## Notes
依赖 A.2 完成（cultivation-a2：道心+日课+奇遇引擎）。转职/觉醒/双修均挂 A.2 StoryletExecutor 框架。INV-CROSS 平衡标定归 balance-003（已推迟）。
