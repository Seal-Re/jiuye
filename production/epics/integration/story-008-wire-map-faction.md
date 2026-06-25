# Story 008: Map/Faction 接线进 World 主循环（闭 C-1）

> **Epic**: integration
> **Status**: Ready for Dev
> **Layer**: Core
> **Type**: Integration
> **Estimate**: 中 (1.5d)
> **Depends**: map-system 代码就位（WorldMap/WorldMapFactory 已建）, faction 代码就位（SectLedger/SectLedgerFactory/Pump 已建）
> **ADR**: adr-0001-integer-determinism（接线消费 Split(7/8) 纯整数确定性）, adr-0003-cultivation-off-byte-identical（off/legacy 路径逐字节不变）
> **GDD**: docs/legacy-specs/specs/2026-06-13-v1.2-C-江湖地图与门派系统-design.md ; integration EPIC §合约 2（可插拔系统）
> **Source**: docs/reports/代码复审报告-2026-06-25.md C-1 / R-1(a) / R-3 / R-4

## Context

复审发现 C-1（CR-2026-06-25）：Map(C)/Faction(D) 子系统**已建完 + 21 单测绿**，但 `WorldFactory.CreateInitial` 从不构造它们、`World.Advance` 从不 tick `Faction.Pump` → 生产中是死代码。`epics/index.md` 现标 "Built, not wired"，`MapFactionWiringGateTests` 有 `[Fact(Skip)]` 占位待本 story 去 Skip。

**基础设施已就绪**（勘察实证）：
- `RngStreamIds` 已冻结预留 `Map=7, Faction=8`（append-only）→ 接线消费 `Split(7/8)`，**不碰 off 路径的 Split(1..4) 编号**，逐字节风险≈0（与 cultivation 当年同范式）。
- `MapConfig.Default` / `FactionConfig.Default` 均存在（无参可调用）。
- `WorldMapFactory.Create` / `SectLedgerFactory.Create` 均有灾备 fallback（最小拓扑 / 空账本），生成失败不崩 sim。
- `World.BuildContext`(L.153/158) 已有 `if (Map != null)` / `if (Faction != null)` 填充逻辑；`World.Clone`(L.262/263) 已有深拷分支——接线后这些路径才被生产覆盖。

本 story 把"建完未接线"转为"接线并运行"，并折入两项相关打磨（R-3/R-4）。

## Acceptance Criteria

- [ ] 8.1 `WorldFactory.CreateInitial` 增 `bool mapOn=false, bool factionOn=false` 参（仿 `cultivation` 范式 L.27）：`mapOn` 时 `world.Map = WorldMapFactory.Create(MapConfig.Default, root.Split(RngStreamIds.Map))`；`factionOn` 时 `world.Faction = SectLedgerFactory.Create(FactionConfig.Default, root.Split(RngStreamIds.Faction), nodeCount)`。
- [ ] 8.2 `World.Advance` 主循环加 `Faction?.Pump(Clock)` tick hook（位置：MaybeSpawn 前后择一，确定性排序固定）。Map 为不可变拓扑无需 tick。
- [ ] 8.3 **off 逐字节铁律（B.3）**：`mapOn=false && factionOn=false`（默认）时，`Determinism/CultivationDeterminismTests` 全绿，Chronicle 与接线前逐字节一致——证明 `Split(1..4)` 流编号未被扰动。
- [ ] 8.4 **on 确定性（B.2）**：`mapOn=true, factionOn=true` 下，同种子两跑 Chronicle 逐字节；N 步 Clone 续跑 == 不中断（StateSnapshot 双重，含 Map/Faction 状态）。
- [ ] 8.5 接线生效实证：`mapOn+factionOn` 跑 200+ 步后，至少一个派系脱离 `FactionPhase.Founding`（Pump 被主循环驱动）；`MapFactionWiringGateTests` 去 `[Fact(Skip)]` 并补全断言，转绿。
- [ ] 8.6 **R-4 折入**：硬编码外提 config——`WorldMap.cs:60` 秘境门槛（`20 + DangerTier*5`）+ `ScoreNode` 旅行权重（100/80/60/20）→ `MapConfig` 新字段；消除 C#/Python `map_data.py` 资源产量魔数双写。
- [ ] 8.7 **R-3 折入**：`SectLedger.NearbyFellows(id, maxDistance)` 实现距离过滤（经 IGeoQuery 查节点距离）**或**删 `maxDistance` 参 + 同步 `IFactionQuery` 接口；消除误导签名。
- [ ] 8.8 CLI 加 `--map` / `--faction` flag（薄壳传参到 CreateInitial）；全量绿 + IL 浮点零。

## Implementation Notes

- **流编号纪律**：`Split(7/8)` 仅在 `mapOn/factionOn` 时构造（off 绝不调，保 Split 编号不变）——严格对齐 `cultRng = cultivation ? root.Split(5) : null` 范式。
- **Pump 位置确定性**：tick hook 插入点一旦定下即冻结（影响事件顺序 → Chronicle 字节）；选 MaybeSpawn 之后，与现有 lifecycle 顺序一致。
- **R-3 取舍**：优先删 `maxDistance` 参（当前零生产调用，YAGNI）；若设计需就近语义，再经 IGeoQuery 实现（成本更高）。
- 约束：纯整数、确定性、不读 daoHeart/innerDemon（B.5）、Map/Faction 状态进 Clone（B.2 续跑不发散）。

## Test Evidence

**Required (BLOCKING — Integration)**:
- `tests/Jianghu.Core.Tests/Sim/MapFactionWiringGateTests.cs` — 去 Skip，断言 8.5（接线后 Map 非 null + Faction 脱离 Founding）。
- `tests/Jianghu.Core.Tests/Determinism/CultivationDeterminismTests.cs` — 8.3 off 逐字节 + 8.4 on Clone 续跑（新增 mapOn/factionOn 用例）。
**Required (R-4)**: `MapAndFactionTests` 补 config 化后的门槛/权重断言。

## Out of Scope

- 真 per-instance derived 聚合（逐傀/逐兽/逐鬼）→ combat-fullstruct story-001。
- Faction 朝廷/势力扩展系统（EPIC.md §Scope 其余项）→ 后续 faction story。
- LLM 脑接入（R-5 sync-over-async 全链路 async）→ llm-brain epic。
- 地图无缝懒加载（map-system EPIC §Scope）→ 后续 map story（worldmap design v2 已设计）。
