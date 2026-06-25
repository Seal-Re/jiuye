# Story 009: 角色→门派 membership 接线（派系生命周期端到端）

> **Epic**: integration
> **Status**: Ready for Dev
> **Layer**: Core
> **Type**: Integration
> **Estimate**: 中 (1d)
> **Depends**: story-008（Map/Faction 接线，done @ a05cd8d）
> **ADR**: adr-0001-integer-determinism（成员分配消费 Faction 流纯整数确定性）, adr-0003-cultivation-off-byte-identical（off/factionOff 路径逐字节不变）
> **GDD**: docs/legacy-specs/specs/2026-06-13-v1.2-C-江湖地图与门派系统-design.md ; integration EPIC §合约 2
> **Source**: story-008 实现暴露的 membership 缺口（AC 8.5 收窄记录）

## Context

story-008 接通了 Map/Faction 的"工厂构造 + tick-hook 管线"，但暴露更深一层缺口：**角色从未被分配到门派**。

**缺口实证**（勘察 file:line）：
- `SectLedger.Join` **零生产调用**（仅 def 存在）→ `FactionOf(id)` 恒返 0，门派恒空。
- `SectLedgerFactory.Create` 仅 `RegisterFaction` + `InitPhase`，**不 Join 任何成员**。
- `DefaultFactionGenerator.Generate` 产出 `FactionDef` + `HomeRegions`，**无成员分配数据**。
- 后果：派系 `memberCount` 恒 0 → 生命周期永卡 `Founding`（Growth 需 memberCount≥2）；`Pump` 被驱动但无状态推进（story-008 AC 8.5 据此收窄断言）。
- 附带死代码：`DefaultFactionGenerator.cs:66-79` 关系计算循环**算了即弃**（注释自承 "factory just returns defs"）；真关系设置在 `SectLedgerFactory.cs:42-43`。该循环应删。

本 story 接通角色→门派分配，让派系生命周期端到端跑通。

## Acceptance Criteria

- [ ] 9.1 `WorldFactory.CreateInitial`（factionOn 时）在角色 spawn 后，**确定性**地将角色分配到门派（经 `SectLedger.Join`），消费 Faction 子流（`root.Split(RngStreamIds.Faction)` 既有流，不新增流编号）。分配策略：按对齐/就近 home site 或简单轮转（设计可选，纯整数确定性）。
- [ ] 9.2 分配后 `FactionOf(id)` 对已分配角色返非 0；至少部分门派 `memberCount ≥ 2`。
- [ ] 9.3 **派系生命周期端到端**：`factionOn` 跑 200+ 步后，至少一个门派脱离 `FactionPhase.Founding`（age>200 + memberCount≥2 → Growth）。**去 story-008 AC 8.5 的收窄注释**，接线门补强为真实生命周期断言。
- [ ] 9.4 删 `DefaultFactionGenerator.cs:66-79` 死关系循环（关系已由 SectLedgerFactory 设置）；或将其结果真正用于 result（择一，消除死代码）。
- [ ] 9.5 **off 逐字节铁律（B.3）**：`factionOn=false`（默认）时 Chronicle 与接线前逐字节一致——成员分配仅在 factionOn 消费 Faction 流，off 不碰。
- [ ] 9.6 **确定性（B.2）**：factionOn 同种子两跑成员分配一致；Clone 续跑 == 不中断（成员状态进快照）。
- [ ] 9.7 全量绿 + IL 浮点零 + clean rebuild 0 警告。

## Implementation Notes

- **流纪律**：成员分配复用既有 `RngStreamIds.Faction` 子流（story-008 已开），不新增流编号；off 不消费 → 保 off 逐字节。
- **分配确定性**：纯整数、确定性、无浮点；同种子→同分配。
- **R-3 续接（可选）**：membership 通后，`NearbyFellows` 的 `maxDistance` 地理过滤具备实现条件（角色→节点位置 + IGeoQuery 距离），可在此 story 或后续接。
- 成员状态须进 `SectLedger.Clone` + `StateSnapshot`（B.2 续跑不发散）——验证 Clone 已含 `_members`（story-008 已确认 Clone 深拷 members）。

## Test Evidence

**Required (BLOCKING — Integration)**:
- `MapFactionWiringGateTests` — 9.3 端到端：factionOn 跑 200 步后断言派系脱离 Founding（替换 story-008 的收窄断言）。
- `CultivationDeterminismTests` — 9.5 off 逐字节 + 9.6 factionOn Clone 续跑成员一致。
**Required (R-3 若接)**: NearbyFellows 距离过滤单测。

## Out of Scope

- 门派朝廷/势力扩展系统（faction EPIC §Scope 其余项）→ 后续 faction story。
- 角色主动入派/叛派的运行期决策（Brain 层）→ 后续 drama/faction story。
- LLM 脑接入 → llm-brain epic。
- 地图无缝懒加载 → map-system 后续 story。
