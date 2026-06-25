# Story 010: C.0 贡献驱动晋升（切磋胜→贡献度→Rank 最薄反馈环）

> **Epic**: faction
> **Status**: Done
> **Layer**: Core
> **Type**: Integration
> **Estimate**: 中 (1d)
> **Depends**: story-009（membership 接通，done `2477435`）
> **ADR**: adr-0001-integer-determinism（贡献度纯整数累加）, adr-0003-cultivation-off-byte-identical（off/factionOff 逐字节）
> **GDD**: docs/legacy-specs/specs/2026-06-13-v1.2-C-江湖地图与门派系统-design.md §3.3（晋升判据接角色行为产出）
> **Source**: faction EPIC §scope（修正：design §3 C.0 真实范围 = 贡献驱动晋升 + 非致死夺地；"朝廷/势力"属过时表述，朝廷不在 C.0）

## Context

design §3.3：「晋升判据接角色行为产出（非纯资历+Might roll）：切磋胜/完成门派目标→贡献度→Rank，让 Chronicle 能写『弟子甲因连胜晋升内门』。这是 C.0 最薄反馈环」。

**现状缺口（勘察实证）**：
- `SectLedger.Promote(id)`（rank+1，cap 3）**零生产调用**——同死代码模式，晋升从不发生。
- **无贡献度累加器**——`_members` 仅 `(FactionId, Rank, JoinedAt)`，无 contribution 字段。
- 贡献来源已就绪：`DuelResolved(Tick, Winner, Loser, Margin)` 每次切磋触发，`World.Project:182` 已投影该事件（写 MemoryEntry）。
- 无 `FactionPromoted` 事件类型（需新增）。

本 story 接通"切磋胜→贡献度累加→过阈晋升→Chronicle 留痕"最薄反馈环，让派系内部 Rank 随角色行为真实流动。

## Acceptance Criteria

- [x] 10.1 `SectLedger` 加贡献度累加器 `AddContribution`/`ContributionOf`（纯整数，散修不累）；进 `Clone` + 新 `CaptureState()`（补 StateSnapshot 此前 Faction 空白，retroactively 守 story-009 membership 确定性）。
- [x] 10.2 `World.Advance` 循环（DuelResolved 入册后）`ProjectFactionContribution`：胜方为成员→`AddContribution(+10+margin)`；off/factionOff（Faction==null）无操作。
- [x] 10.3 过阈（累计 ≥ (rank+1)×50）→ `Promote` + `FactionPromoted` 事件入 Chronicle（"因功勋卓著晋升为 内门弟子/核心长老/一派掌门"）；阈值=具名常量。晋升行紧随切磋行（test_promotion_line_follows_duel_line 守序）。
- [x] 10.4 Rank 单调不超 cap(3)；贡献度保留累加（不扣减，确定性）。
- [x] 10.5 **off 逐字节（B.3）**：factionOff 默认 Chronicle 一致（CLI 两跑 + determinism 绿）；faction 快照段 null 时省略。
- [x] 10.6 **确定性（B.2）**：`OnFaction_PromotionState_SameSeedIdentical`（CaptureState + Chronicle 同种子一致）+ OnMapFaction Clone 续跑。
- [x] 10.7 端到端实证：`test_contribution_promotion_end_to_end`（Chronicle 含晋升行；注：长跑死亡率高，晋升者多已逝，用 Chronicle 持久记录为证）；CLI 门派录可见（seed7 9 次晋升）。
- [x] 10.8 全量 867 绿 + IL 浮点零（FloatScan 5）+ clean rebuild 0 警告。

## Implementation Notes

- **接入点**：`World.Project:182` 的 `case DuelResolved` 分支已存在，在此追加 faction 贡献累加（null 安全：`Faction?.`）。
- **贡献度存储**：扩 `_members` tuple 或新 `Dictionary<CharacterId,int> _contribution`；进 `Clone`（story-008 已确认 Clone 深拷 members）+ `StateSnapshot`（验证 snapshot 纳入，否则晋升漂移静默——参 story-008 教训）。
- **事件投影确定性**：FactionPromoted 入 Chronicle 顺序固定（在 DuelResolved 投影内同步发，不延迟）。
- **off 纪律**：贡献/晋升全在 `if (Faction != null)` 守内，off 路径零触碰 → 逐字节。
- 纯整数、确定性、不读 daoHeart/innerDemon（B.5）。

## Test Evidence

**Required (BLOCKING — Integration)**:
- `MapFactionWiringGateTests` 或新 `FactionPromotionTests` — 10.7 端到端：factionOn 长跑后成员 Rank>0 + Chronicle 含 FactionPromoted。
- `CultivationDeterminismTests` — 10.5 off 逐字节 + 10.6 factionOn 晋升序列同种子一致 + Clone 续跑贡献度一致。
**Required (单元)**: AddContribution/过阈 Promote 逻辑单测（阈值边界）。

## Out of Scope

- 非致死夺地兑现世仇（design §3 另一 C.0 项）→ 后续 faction story-011。
- 拜师/叛门运行期事件（FactionJoined/Defected，§3.3 下半）→ 后续 story。
- 任务大厅/俸禄/小比/建筑/经营（design 明示 C.1）→ 远期。
- 朝廷系统（不在 design C.0 范围）。
