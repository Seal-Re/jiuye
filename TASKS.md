# TASKS — 派生指针（真相已迁至 production/）

> ⚠️ **本文件不再是单一真相源。** 真相 = `production/`（红线 A.2，2026-06-15 迁移）。
> **勿在此改任务状态** —— 改 `production/sprint-status.yaml`（机器可读状态机）与 `production/epics/[slug]/EPIC.md`。

## 速览（派生）

- **单一真相源**：`production/epics/[slug]/EPIC.md` + `story-NNN-*.md` + `production/sprint-status.yaml` + `production/stage.txt`
- **看全部 epic**：`production/epics/index.md`（13 epic 总表）
- **看 sprint 进度**：`/sprint-status`（读 sprint-status.yaml）
- **采用路线图**：`docs/adoption-plan-2026-06-15.md`（增量补 GDD/ADR）
- **设计深度源**：`docs/superpowers/`（18 spec/4 plan/3 research，原地保留）

## 当前状态（派生快照，权威以 production/ 为准）

- **阶段**（stage.txt）：**Production**
- **WIP**（A.5 doing≤2）：**doing=1** = epic `combat-r2` / story-001（batch2 普通稀有档全路迁，6/21 done，**282 绿**）
- A.1 竖切已合（232 绿，`3ea18da`）；A1.4 辅助路 UT 重锚 = blocked，并入 `combat-r2/story-005`（依赖 `balance-cross`，红线 A.8 不静默）

---

## 审计记录（append-only 历史日志，迁移后保留于此）

- 2026-06-14：首次审计。建台账。发现：A1.4 静默defer（已转blocked标依赖）；任务清单#14-22 stale 已弃；WIP 超限（3 doing）；pixel v2 broken。
- 2026-06-14(第二次)：机器核验。`git log` 实证 master=`8eca0ed`、`feat/jianghu-v1.2-A1`=`19d6ead`(224绿在)、status仅`?? pixel/`。回写：EPIC-PROCESS 双挂修正(DONE 唯一+sha)；WIP警告纠为 doing=0；删工具 stale task #18/#22(superseded by EPIC-B)。确认：`.gitignore` 漏 `_research/raw3/`(22跟踪文件)与 `pixel/out/`(EPIC-TIDY 待补)。未重跑测试(已commit验过，git证)。
- 2026-06-15：日志对账审计（旧机会话 `e733e301` 06-13~06-14 全轨迹 vs TASKS.md vs git）。**发现断链**：批1框架实际已做完+提交+绿，但 TASKS.md 仍标"批1-6 待启"——日志会话 context 耗尽，止于批1转绿后、未执行红线 A.3 回写。对治：①本机装 .NET 8 SDK 8.0.422(winget MSI 失败→官方 dotnet-install.ps1 用户级)②`dotnet test` 主控独立核验 **255绿/0失败**(HEAD=`d474aab`)③回写批1✅(6 Task↔6提交 `6241a8e`→`946ea75`)+批2-6 todo+WIP状态。其余轨迹全对账无虚报；FULLSTRUCT deferred 项红线 A.8 已诚实登记。
- 2026-06-15(迁移)：**全量迁 CCGS 管理体系**（分支 `chore/ccgs-adoption`）。装 CCGS .claude 引擎(49agents/73skills/12hooks)；真相从 TASKS.md 迁入 `production/`(13 epic + sprint-status.yaml)；TASKS.md 降为本派生指针；CLAUDE.md 合并(9红线存活+新A.9主动调度skill+改A.2/B.7+CCGS Section D)；technical-preferences 诚实设 .NET8。批2 迁移(剑/体/法/鬼/丹/器)+Modules工厂(B.9)已 282 绿。后续真相看 production/，本文件仅指针。
