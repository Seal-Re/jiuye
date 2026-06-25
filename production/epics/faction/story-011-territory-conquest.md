# Story 011: C.0 非致死夺地兑现世仇（territory + relation 联动）

> **Epic**: faction
> **Status**: Done
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

- [x] 11.1 `FactionDef` 加 `Ambition`（init 属性，既有 11 构造点默认 0 不破）；DefaultFactionGenerator `rng.NextInt(101)` 确定性赋值（消费 Faction 流）。
- [x] 11.2 `World.BuildFactionMight`（Σ 在役成员 Force×2+Int+Con，与 RuleBrain.SelfPower 同公式）注入 `Pump(clock, geo, factionMight)`；off/factionOff 不算（null）。
- [x] 11.3 `ResolveConquest` 只扫相邻边界（攻方领地的 AdjacentTo 中敌方有主 site）；Ambition≥60 + Might 差≥30 → 夺地。攻方 Id 升序 + 候选 SortedSet 升序裁决（确定性）。
- [x] 11.4 夺地兑现：`LoseSite`+`ControlSite`+`TerritoryLost` 事件入 Chronicle（"门派#X 攻取门派#Y 的 Z 号地，两派结怨"）+ 双向关系恶化（-40，下限 Enemy）。
- [x] 11.5 非致死：不杀人/不清成员（test 验守方成员仍在）；site 归零经既有 phase 逻辑可触 Decline，不直接 Fallen。
- [x] 11.6 **off 逐字节（B.3）**：夺地仅 geo+Might 双非 null 路径；CLI off 两跑一致 + determinism 绿。
- [x] 11.7 **确定性（B.2）**：`OnMapFaction_ConquestState_SameSeedIdentical`（CaptureState territory+relation + Chronicle 同种子一致）。
- [x] 11.8 **INV-PERF（§3.5）**：`test_conquest_scans_only_boundary_not_full_graph`——2 领地 → AdjacentTo 恰 2 次，与 NodeCount(1000) 解耦。
- [x] 11.9 端到端：`test_territory_conquest_end_to_end`（Chronicle 含攻取行）；CLI 门派录·夺地显示（seed7 实证）。
- [x] 11.10 全量 874 绿 + IL 浮点零（FloatScan 5）+ clean rebuild 0 警告。

## 设计精化（实现期 TDD 催生）

- **夺地仅取敌方有主地**（defender!=0）：初版允许夺无主地，被 `test_conquest_low_might_gap_no_take` 证伪（攻方夺了无主邻居 site-1）。回归 design §3「兑现**世仇**」本义=取敌方地；无主地"拓土"属另一机制，**本 story 不含**（移除"开疆"Chronicle 分支）。
- **初始领地**：`SectLedgerFactory` 此前不 ControlSite → 门派无地 → 夺地无候选（死代码）。补 `ControlSite(f.Id, f.HomeSite)`（design §3 门派据 HomeSite 占地利），夺地始可触发。

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
