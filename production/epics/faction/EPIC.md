# Epic: 门派 Faction D

**Layer**: Feature
**Status**: Designed (0 code)
**GDD**: design/gdd/xxx.md（P8 补）或 — ；深度源 同 map（C 设计含门派）+ docs/legacy-specs/specs/...地图概要与宗门排布-canonical.md
**Governing ADRs**: None yet（P8 增量补）
**Engine Risk**: LOW（.NET 8 纯整数）
**Created**: 2026-06-15（迁自 TASKS.md）

## Summary
门派 Faction D——宗门势力系统（侧表聚合 SectLedger）。

> 〔2026-06-25 校正：原 Summary "宗门/朝廷/势力" 与 design §3 不符。design C.0 真实范围 =
> 生成期布局 + **贡献驱动晋升** + 非致死夺地兑现世仇 + 被动兴衰；朝廷/任务大厅/俸禄/经营属 **C.1**（远期）。〕

## Scope

**C.0（design §3）**：membership（done story-009）+ 贡献驱动晋升（story-010）+ 非致死夺地世仇（story-011 待开）+ 被动兴衰 Pump（done story-008）。
**C.1（远期 deferred）**：朝廷、任务大厅、俸禄、小比、建筑、经营。

## Stories

| # | Story | 状态 | 证据 |
|---|-------|------|------|
| （接线）| Map/Faction 接 World + Pump tick | ✅ Done | integration story-008 `a05cd8d` |
| （接线）| 角色→门派 membership + 生命周期端到端 | ✅ Done | integration story-009 `2477435` |
| 010 | C.0 贡献驱动晋升（切磋胜→贡献度→Rank） | ✅ Done | design §3.3；切磋胜累计贡献过阈晋升 + StateSnapshot 补 Faction |
| 011 | C.0 非致死夺地兑现世仇 | Planned（未拆） | design §3 |

## Dependencies
**Unblocked by**: map-system（C 设计含门派）+ integration story-008/009（接线已通）。
**Blocks**: 无

## Definition of Done
- [x] membership + 生命周期端到端（story-009）
- [x] 贡献驱动晋升（story-010）
- [ ] 非致死夺地世仇（story-011）
- [ ] 被动兴衰 Pump（story-008 已接，C.0 完整待 010/011）

## Notes
C.0 = 生成期布局 + 贡献驱动晋升 + 非致死夺地 + 被动兴衰，不把状态机当玩法（design §3 明示）。朝廷/经营属 C.1。
