# TASKS — 任务台账（单一真相源）

> 全项目任务唯一真相源（红线 A.2）。状态 ∈ {todo, doing, review, done, blocked}。
> `done` 必须 DoD 清单全勾 + 证据（测试计数/git sha）。`blocked` 必记阻塞点+原因+依赖。
> 会话开始读此、结束前回写。**WIP(doing) ≤ 2**。审计 cadence：每阶段边界/~5-8 步对账。
> 最后审计：2026-06-14。

## ⚠️ 当前 WIP 违规（红线 A.5：doing≤2）
同时挂着：A.1 境界竖切、pixel、仓库整理 → **超 WIP**。收敛：先做完一个再开下一个。

---

## DONE（已验证，带证据）

- [x] **v1.0 内核**（事件驱动/Pcg32/RuleBrain/生老死）— 证据：38 测试绿，master
- [x] **修炼 A.0**（引擎+21路全入册+软情境战斗+基础realm+CLI）— 证据：204 测试绿，merged master `579659f`，审计无blocker
- [x] **World Bible《九野》+ 21路深度 + 地图/宗门 设计** — canonical docs committed
- [x] **A.1/2/3 设计**（境界+对齐+A2/A3-FINAL）— 两轮审计，docs `1d66d16`/`a3b7ef1`
- [x] **A.1 境界竖切 A1.0-A1.2**（三列schema+Validate+投影INV-REALM-1+4偏离路UT迁移）— 证据：224 绿，branch `19d6ead`
- [x] **流程红线 + CLAUDE.md + PROJECT-STATUS.md + TASKS.md** — 据研究确立，本批

---

## DOING（在制，≤2）

- [ ] **EPIC-PROCESS：流程红线落地** — doing
  - [x] 联网研究流程最佳实践
  - [x] CLAUDE.md 红线（A流程+B技术）
  - [x] TASKS.md 台账（本文）
  - [ ] 存记忆（流程红线跨会话）
  - [ ] git 提交（CLAUDE.md/TASKS.md/PROJECT-STATUS.md）— DoD：sha 落

---

## TODO（待办，按优先）

- [ ] **EPIC-A1-收尾：境界竖切剩余** — 依赖：无（分支 feat/jianghu-v1.2-A1）
  - [ ] A1.3 统一双轨命名替换（21路 RealmNames，改名不破determinism）
  - [ ] A1.5 三轴查询 API + Chronicle 大小境界渲染
  - [ ] A1.6 境界竖切 gate（INV-REALM-1/INV-UT-MONO/off逐字节/auditor）
  - DoD：全量绿+0警告+IL浮点零+off逐字节worktree实证+auditor过
- [ ] **EPIC-TIDY：仓库整理**
  - [ ] 清 `_research/raw3/`（抓取html）：.gitignore + git rm --cached
  - [ ] pixel `icon_gen.py` disk bug 修复 + 验证渲染
  - [ ] pixel/ 提交决断（独立分支 or master，与境界分离）
  - [ ] .gitignore（pixel/out/、bin/、obj/）
- [ ] **EPIC-A1-余项**（10态流程/4劫/5失败/寿元）— 设计完（A2-FINAL/A123）
- [ ] **EPIC-A2**（道心/破单调/奇遇自建storylet/闭关）— 设计完（A3-FINAL/A123）
- [ ] **EPIC-A3**（转职/觉醒/双修）
- [ ] **EPIC-BALANCE：平衡标定 INV-CROSS** 🔴 — 设计完，**最大功能缺口**（同UT 24-70×失衡）
- [ ] **EPIC-B 戏剧引擎** — 设计完零代码
- [ ] **EPIC-C 地图系统** — 设计完零代码
- [ ] **EPIC-D 门派 Faction** — 设计完零代码
- [ ] **EPIC-LLM 脑 v1.1**（黑盒API多智能体涌现）🔴 — **原始核心愿景未设计未建**
- [ ] **EPIC-INTEGRATION 系统集成层** — **各系统如何合成一局江湖，未设计**
- [ ] **EPIC-VIS 可视化**（像素tile/角色模块/地图viewer + 古风UI轨）— 仅spike+规则doc

---

## BLOCKED

- [ ] **A1.4 辅助路 UT 战斗当量重锚** — blocked
  - 阻塞点：境界竖切 plan 列为 task，被静默 defer 到"标定阶段"（红线 A.8 违规：未显式标）
  - 原因：辅助路真实战斗当量 UT 需 INV-CROSS 对拍测得（属 EPIC-BALANCE）
  - 依赖：EPIC-BALANCE（标定）；**且 EPIC-A1-余项 寿元/劫 依赖本项**（丹UT12吃UT12寿元荒谬，P2）
  - 裁决待定：A1.4 先给"战斗当量初值"在竖切落，还是整体并入 EPIC-BALANCE

---

## KNOWN-ISSUE

- pixel `icon_gen.py` v2：disk() 收原始RGB元组致 KeyError（修复已写未落，fix 第1次）

---

## 审计记录

- 2026-06-14：首次审计。建台账。发现：A1.4 静默defer（已转blocked标依赖）；任务清单#14-22 stale 已弃；WIP 超限（3 doing）；pixel v2 broken。
