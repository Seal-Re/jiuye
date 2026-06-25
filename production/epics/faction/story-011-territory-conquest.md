# Story 011: C.0 非致死夺地兑现世仇（territory + relation 联动）

> **Epic**: faction
> **Status**: Ready for Dev
> **Layer**: Core
> **Type**: Integration
> **Estimate**: 大 (2d)
> **Depends**: story-010（贡献晋升，done `3514a1b`）；WorldMap RegionOf/AdjacentTo/SitesInRegion（已建）
> **ADR**: adr-0001-integer-determinism（夺地结算纯整数）, adr-0003-cultivation-off-byte-identical（off/factionOff 逐字节）
> **GDD**: docs/legacy-specs/specs/2026-06-13-v1.2-C-江湖地图与门派系统-design.md §3（夺地兑现世仇）+ §3.5（Pump 性能）
> **Source**: faction C.0 最后一项（修复"世仇是无后果标签"）

## Context

design §3：「C.0 落**非致死最薄夺地结算**——攻方 Ambition 高 + Might 差 ≥ ConquestGap + 区域相邻 → `TerritoryLost` 单事件改 SiteOwnership，触发守方关系恶化（→ Grudge/Rival）。致死式灭门留 C.1。」§3.5：「夺地候选只扫相邻边界 Site，非全图；Might 脏标记缓存非重算；factionRng=Split(8) 私有流。」

**现状缺口（勘察实证）**：
- territory 基础就绪：`ControlSite`/`LoseSite`/`OwnerOf`/`ControlledSites`（SectLedger）。
- relation 就绪：`SetRelation`/`FactionRelation` + `FactionRelationKind{Ally..Enemy}`。
- 地图就绪：`RegionOf`/`AdjacentTo`/`SitesInRegion`/`RegionAt`（WorldMap）。
- **缺 `FactionDef.Ambition`**（新字段）。
- **缺 per-faction Might 聚合**——SectLedger 只持 CharacterId+Rank，无成员战力（跨系统数据流）。
- **缺 `TerritoryLost` 事件**。
- Pump 现仅做 phase + revenue，无夺地。

## 关键设计决策（Might 来源）

夺地需"攻守双方 Might"，但 SectLedger 不知 Character 战力（解耦：IFactionQuery 不依赖 Character）。三方案：
- **(a/c 推荐) World 注入**：`World.Advance` 已在 line 162 算角色 power（Force×2+Internal+Constitution），且 line 146 调 Pump。World 先算 per-faction Might 快照（Σ成员 power），传入 `Pump(clock, geo, mightOf)`。SectLedger 保持对 Character 无知（承 design 解耦）。
- (b) SectLedger 缓存 Might：需成员变动脏标记，SectLedger 仍须拿到 power → 破解耦。
- **本 story 采 (a/c)**：Pump 新增 `IReadOnlyDictionary<int,int>? factionMight` 参（null=不夺地，off 安全）。

> ⚠️ 若用户偏好其他 Might 公式（如计入 Rank/realm），在 dev 时确认；本 story 默认 Σ(成员 Force×2+Internal+Constitution)，与 RuleBrain.SelfPower 同源（一致性）。

## Acceptance Criteria

- [ ] 11.1 `FactionDef` 加 `Ambition`（int，0-100，data-driven）；DefaultFactionGenerator 确定性赋值（消费 Faction 流，不新增流编号）；所有构造点更新。
- [ ] 11.2 `World.Advance` 算 per-faction Might 快照（Σ 在役成员 power，与 RuleBrain.SelfPower 同公式）传入 `Pump`；off/factionOff 不算（null）。
- [ ] 11.3 `Pump` 夺地结算：对每个门派，**仅扫相邻边界 Site**（经 geo.AdjacentTo + OwnerOf 找邻区敌方 site），若 攻方 Ambition ≥ AmbitionThreshold 且 Might 差 ≥ ConquestGap 且 区域相邻 → 夺地。纯整数确定性，扫描有上界（§3.5 INV-PERF）。
- [ ] 11.4 夺地兑现：`LoseSite`(守方) + `ControlSite`(攻方) + 发 `TerritoryLost(Tick, Site, FromFaction, ToFaction)` 入 Chronicle；守方对攻方关系恶化（FactionRelation → Rival/Enemy 档，整数下调）。
- [ ] 11.5 非致死：夺地不杀人、不清成员；门派 site 归零可触 Decline（既有 phase 逻辑），不直接 Fallen（致死灭门留 C.1）。
- [ ] 11.6 **off 逐字节（B.3）**：factionOff 默认 Chronicle 一致（夺地仅 factionOn + Might!=null 路径）。
- [ ] 11.7 **确定性（B.2）**：factionOn 同种子两跑夺地序列一致（CaptureState + Chronicle）；Clone 续跑一致。territory 已在 CaptureState（story-010 加），relation 须确认入快照。
- [ ] 11.8 **INV-PERF（§3.5）**：夺地扫描只扫相邻边界（非全图 O(site²)），加测断言扫描规模上界。
- [ ] 11.9 端到端：factionOn 长跑后 Chronicle 含 TerritoryLost 行 + 关系恶化可观测；CLI 门派录显示夺地。
- [ ] 11.10 全量绿 + IL 浮点零 + clean rebuild 0 警告。

## Implementation Notes

- **流纪律**：Ambition 赋值复用 Faction 流（story-009 的 `Split(8).Split(1)` 或新 sub-split），off 不消费。
- **CaptureState 扩**：relation（`_relations`）已在 story-010 CaptureState 序列化（R 段）；territory 在 F 段（sites）。夺地后两者变 → 快照自动覆盖，但须加测确认。
- **TerritoryLost 渲染**：Chronicle case "「X 门派夺取 Y 门派的 Z 地，两派结怨」"。
- **性能**：候选只取 `ControlledSites(attacker)` 的 `AdjacentTo` 邻居中 `OwnerOf != attacker` 者；非全 site 对扫。
- 纯整数、确定性、不读 daoHeart/innerDemon（B.5）。

## Test Evidence

**Required (BLOCKING — Integration)**:
- `FactionConquestTests`（新）— 11.3/11.4/11.5：构造 Ambition 高 + Might 差大 + 相邻 → 夺地兑现 + 关系恶化 + 非致死。
- `MapFactionWiringGateTests` — 11.9 端到端 Chronicle 含 TerritoryLost。
- `CultivationDeterminismTests` — 11.6 off 逐字节 + 11.7 factionOn 夺地序列同种子一致。
**Required (INV-PERF)**: 11.8 扫描规模上界断言。

## Out of Scope

- 致死式灭门 / PopulationLow 涌现联动 → C.1。
- 拜师/叛门运行期事件（FactionJoined/Defected）→ 后续 story。
- A/B 在位时 SectFeud→GrudgeLedger 喂养（drama 层）→ 待 drama-engine。
- 朝廷 / 任务大厅 / 俸禄 / 经营 → C.1。
