# Epic: 修炼 A.2

**Layer**: Feature
**Status**: In Progress
**GDD**: design/gdd/cultivation-system.md §3.5；深度源 A3-FINAL/A123/A2-FINAL
**Governing ADRs**: adr-0001-integer-determinism, adr-0003-cultivation-off-byte-identical
**Engine Risk**: LOW（.NET 8 纯整数）
**Created**: 2026-06-15（迁自 TASKS.md）
**Updated**: 2026-06-24（story breakdown）

## Summary
修炼 A.2——道心 / 破单调 / 奇遇 / 闭关。四子系统：21路道心表 + 4路日课微决策 + 闭关DES单点唤醒 + 奇遇storylet执行器。

## Scope
- **道心系统**（story-001~003）：21路 per-path daoHeart 初值/增益/减损表，daoHeart/innerDemon 伪资源系统（不进 EffectivePower，R3守），佛修破戒修正
- **破单调·日课微决策**（story-004~006）：4路 DailyMode（Fast/Steady/Comprehend/Roam）整数倍率，NPC 贪心算法+迟滞规则（enter 65/exit 50），INV-VARIETY/INV-NO-DOMINANT 破单调真判据
- **闭关时序**（story-007~012）：BreakAid 四法数据模型，闭关 DES 单点唤醒（skip mid ticks，Scheduler 集成），时长+收益公式，避险刷点（streak escalation），RNG 自洽
- **奇遇 storylet**（story-013~018）：最小 storylet 执行器，频率 cap 收敛（GlobalCap/ActorMinGap/CatCap），收益护栏（35% cap + salience decay），内容池最小规模（POOL_MIN~60），游历刷奇遇破上界，太吾双面教训
- **集成+硬化**（story-019~025）：A.2↔A.1 全流程集成（DailyMode→Phase/BreakAid→Breakthrough/daoHeart→Tribulation），DaoHeart-Insight 去耦实证，全不变量硬化，确定性+off 逐字节，审计终验

## Dependencies
**Unblocked by**: cultivation-a1-rest（A.1 余项：FSM + 三劫 + 寿元 已完成）
**Blocks**: cultivation-a3

## Stories

| # | Story | Phase | Priority | Est | Status |
|---|-------|-------|----------|:--:|--------|
| 001 | 21路道心资源表 | 道心基础 | Must Have | 1d | Not Started |
| 002 | 道心/心魔伪资源系统 | 道心基础 | Must Have | 1d | Not Started |
| 003 | 佛修破戒修正 | 道心基础 | Should Have | 0.5d | Not Started |
| 004 | 4路DailyMode枚举+整数倍率 | 日课 | Must Have | 1d | Not Started |
| 005 | DailyMode贪心算法+迟滞 | 日课 | Must Have | 1.5d | Not Started |
| 006 | 破单调INV-VARIETY真判据 | 日课 | Should Have | 1d | Not Started |
| 007 | BreakAid四法数据模型 | 破障 | Should Have | 0.5d | Not Started |
| 008 | 顿悟Epiphany机制 | 破障 | Should Have | 1d | Not Started |
| 009 | 闭关DES单点唤醒 | 闭关 | Must Have | 1.5d | Not Started |
| 010 | 闭关时长+收益公式 | 闭关 | Must Have | 1d | Not Started |
| 011 | 闭关避险刷点 | 闭关 | Should Have | 1d | Not Started |
| 012 | 闭关RNG自洽+Scheduler集成 | 闭关 | Should Have | 0.5d | Not Started |
| 013 | 奇遇storylet最小执行器 | 奇遇 | Should Have | 1.5d | Not Started |
| 014 | 奇遇频率cap收敛 | 奇遇 | Should Have | 1d | Not Started |
| 015 | 奇遇收益护栏+salience decay | 奇遇 | Nice to Have | 1d | Not Started |
| 016 | 奇遇内容池POOL_MIN | 奇遇 | Nice to Have | 1d | Not Started |
| 017 | 游历刷奇遇破上界 | 奇遇 | Nice to Have | 0.5d | Not Started |
| 018 | 太吾双面教训 | 奇遇 | Nice to Have | 0.5d | Not Started |
| 019 | DailyMode→Phase FSM集成 | 集成 | Must Have | 1d | Not Started |
| 020 | BreakAid→Breakthrough集成 | 集成 | Should Have | 1d | Not Started |
| 021 | daoHeart→Tribulation集成 | 集成 | Should Have | 0.5d | Not Started |
| 022 | DaoHeart-Insight去耦实证 | 集成 | Should Have | 0.5d | Not Started |
| 023 | A.2全不变量硬化 | 硬化 | Must Have | 1d | Not Started |
| 024 | A.2确定性+off逐字节 | 硬化 | Must Have | 0.5d | Not Started |
| 025 | A.2审计员终验 | 硬化 | Must Have | 0.5d | Not Started |

## Definition of Done
- [ ] 道心系统实现 + 测试（story-001~003）
- [ ] 破单调实现 + 测试（story-004~006）
- [ ] 闭关（三档时间尺度 + QBN + DES）实现 + 测试（story-007~012）
- [ ] 奇遇 storylet 实现 + 测试（story-013~018）
- [ ] A.2↔A.1 全流程集成（story-019~021）
- [ ] 不变量硬化 + 确定性守（story-022~024）
- [ ] 审计终验（story-025）

## Notes
依赖 A.1 余项（cultivation-a1-rest：FSM+三劫+寿元 已完成）。道心严禁进 EffectivePower（红线 B.5/R3）。日课不修改 RuleBrain（仅 cultivation 内部决策）。RNG 全走 Split(5) cultRng。
