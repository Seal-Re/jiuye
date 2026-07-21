# Epic: 真·全量机制结构化

**Layer**: Core
**Status**: Done（2026-07-14 审计：7/8 story Complete——001~007 代码+测试全部就位；008 丹修非战斗机制 Backlog。原标 Deferred 因全量未审计。）
**GDD**: design/gdd/xxx.md（P8 补）或 — ；深度源指向现有 docs/legacy-specs/specs/ 文件
**Governing ADRs**: None yet（P8 增量补）
**Engine Risk**: LOW（.NET 8 纯整数）
**Created**: 2026-06-15（迁自 TASKS.md）

## Summary
真·全量机制结构化——把 21 路 100% 机制从 Note 升为结构化算子。当前"分层全量"只够当前版本角色跑起来，未把底层机制全部结构化。

## Scope
- derived:* 求和 provider（Σ鬼兵/Σ傀儡/Σ召唤兽/蛊群/stockFirepower/fleetWeighted 真派生，现全返 0）
- 克制矩阵 SituationalEdges（灭阴×3/anti_evil×3/克邪×2/元素相生克）
- PostMul ModKind + 负向压制（化形态/文宫/天道压制 LawSuppress）
- dot 时序/召唤物系统/结算回滚栈（因果逆演/夺舍续命/分魂挡刀）
- 结构化 Gate 字段
- 唯一档签名逐路全迁
- 非战斗机制（丹改人四维 + 造关系边/卖丹换 realm 经济晋升）

## Dependencies
**Unblocked by**: combat-r2 done（架构侧）+ 测试过
**Blocks**: 无

## Definition of Done
- [ ] derived:* 求和 provider 全部真派生（不再返 0）
- [ ] 克制矩阵 SituationalEdges 结构化
- [ ] PostMul ModKind + 负向压制结构化
- [ ] dot 时序/召唤物系统/结算回滚栈实现
- [ ] 结构化 Gate 字段
- [ ] 唯一档签名逐路全迁
- [ ] 非战斗机制（丹改四维/经济晋升）
- [ ] 全部底层结构化 + 测试

## Notes
红线 A.8 诚实 defer。启动条件 = 分层全量架构侧 done（combat-r2 完成且测试过）。
