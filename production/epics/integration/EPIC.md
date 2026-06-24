# Epic: 系统集成层

**Layer**: Core
**Status**: In Progress
**Created**: 2026-06-15
**Updated**: 2026-06-25（设计完成，故事拆解）
**Governing ADRs**: adr-0001-integer-determinism, adr-0003-cultivation-off-byte-identical

## Summary
定义所有子系统（A.0-A.3 修炼、B 戏剧、C 地图、D 门派）如何在一个 `World.Tick` 内编排执行。核心合约：

1. **IBrain 边界**：所有决策经 IBrain 接口——RuleBrain（确定性回退）或 LLM Brain（可选增强，可接入可不接入）。集成层不关心哪个实现
2. **可插拔系统**：Map + Faction 是 World 上的可空字段——off 模式下不激活，零性能影响
3. **决策上下文扩展**：DecisionContext 以向后兼容的方式增长（默认值），以承载 Map/Faction 信息
4. **事件溯源**：所有系统输出经 DomainEvent → Chronicle + StateSnapshot
5. **确定性**：每条管线阶段确定性排序——无竞态，无浮点

## Stories

| # | Story | Phase | Priority | Est |
|---|-------|-------|----------|:--:|
| 001 | World.Tick 编排契约 | 核心 | Must Have | 0.3d |
| 002 | DecisionContext 扩展 (Reachable + FactionInfo) | 核心 | Must Have | 0.3d |
| 003 | IGeoQuery 接口定义 | 地图 | Must Have | 0.2d |
| 004 | IFactionQuery 接口定义 | 门派 | Must Have | 0.2d |
| 005 | 管线阶段注册表 (IPipelineStage) | 核心 | Should Have | 0.3d |
| 006 | 集成测试——全管线确定性 | 硬化 | Must Have | 0.5d |
| 007 | 集成层审计员终验 | 硬化 | Must Have | 0.3d |

## Definition of Done
- [ ] World.Tick 管线合约已文档化 + 测试验证
- [ ] DecisionContext 已扩展——向后兼容（默认 null → 不破坏现有代码）
- [ ] IGeoQuery + IFactionQuery 接口已定义——下游系统可实现
- [ ] 管线阶段可插拔——新系统注册无需修改核心循环
- [ ] 全管线确定性可复现 + off 模式零影响

## Notes
LLM 脑是可选的（RuleBrain 默认）。Map + Faction 是可选的（默认 null）。集成层定义"如何编排"——所有系统实现细节仍在各自史诗中。NPC 和世界在不接入 LLM 的情况下也必须正常运转。
