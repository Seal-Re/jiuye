# TASKS — 任务台账（单一真相源）

> 全项目任务唯一真相源（红线 A.2）。状态 ∈ {todo, doing, review, done, blocked}。
> `done` 必须 DoD 清单全勾 + 证据（测试计数/git sha）。`blocked` 必记阻塞点+原因+依赖。
> 会话开始读此、结束前回写。**WIP(doing) ≤ 2**。审计 cadence：每阶段边界/~5-8 步对账。
> 最后审计：2026-06-14。

## 当前 WIP 状态（红线 A.5：doing≤2）
**doing = 1** → EPIC-A1-收尾（A1.3 命名 impl 在制）。停泊：pixel/ spike 未提交（EPIC-TIDY）。

## 阶段地图（宏观·简要）
> 派生视图，真相仍在下方任务块。✅完成 / 🔨在制 / 📐设计完未建 / ❌未设计 / 🧪spike

| # | 阶段 | 态 | 证据/分支 |
|---|---|---|---|
| 0 | 世界观地基（九野WorldBible+21路+地图/宗门） | ✅ | canonical docs |
| 1 | 内核竖切 v1.0（事件驱动/Pcg32/RuleBrain/生老死） | ✅ | 38测试 master |
| 2 | 修炼骨架 A.0（引擎+21路全入册+软战斗+基础realm+CLI） | ✅ | 204测试 master `579659f` |
| **3** | **境界深化 A.1（大小境界双层/三轴/UT平衡/命名）** | 🔨**当前** | feat/jianghu-v1.2-A1 224绿 |
| 4 | 修炼深度层（A.1余10态/劫/寿元 + A.2道心/奇遇/闭关 + A.3转职/双修） | 📐 | 设计完 |
| 5 | 平衡标定 INV-CROSS（跨路战力当量）🔴最大功能缺口 | 📐 | 方法设计完·零实现 |
| 6 | 系统三件（B戏剧 / C地图 / D门派） | 📐 | 设计完·零代码 |
| 7 | LLM脑 v1.1（黑盒API多智能体涌现）🔴原始核心 | ❌ | 未设计未建 |
| 8 | 系统集成（各系统合成一局江湖） | ❌ | 未设计 |
| 9 | 可视化（像素=游戏世界 / 古风UI=界面） | 🧪 | spike+规则doc |

**阶段3（A.1境界）分任务进度**：
| 子任务 | 态 | 证据 |
|---|---|---|
| A1.0 RealmCurveDef 加三列 schema | ✅ | `daf4363` |
| A1.1 Validate 扩多列校验 | ✅ | `daf4363` |
| A1.2 前缀和投影+INV-REALM-1+4偏离路UT迁移 | ✅ | `18c521a`/`19d6ead` 224绿 |
| A1.3 统一双轨命名 | 🔨 设计✅`573aabe` / impl在制 | — |
| A1.4 辅助路 UT 战斗当量重锚 | ⛔ blocked | 依赖阶段5 BALANCE |
| A1.5 三轴查询API+大小境界渲染 | ⬜ todo | 含 MajorRealmNames 决策 |
| A1.6 境界竖切 gate | ⬜ todo | INV-REALM/UT-MONO/off逐字节/auditor |

> 阶段3 进度：schema+投影+迁移层 done(A1.0-1.2)；命名设计done·impl在制(A1.3)；A1.4阻塞、A1.5/1.6待。

---

## DONE（已验证，带证据）

- [x] **v1.0 内核**（事件驱动/Pcg32/RuleBrain/生老死）— 证据：38 测试绿，master
- [x] **修炼 A.0**（引擎+21路全入册+软情境战斗+基础realm+CLI）— 证据：204 测试绿，merged master `579659f`，审计无blocker
- [x] **World Bible《九野》+ 21路深度 + 地图/宗门 设计** — canonical docs committed
- [x] **A.1/2/3 设计**（境界+对齐+A2/A3-FINAL）— 两轮审计，docs `1d66d16`/`a3b7ef1`
- [x] **A.1 境界竖切 A1.0-A1.2**（三列schema+Validate+投影INV-REALM-1+4偏离路UT迁移）— 证据：224 绿，branch `19d6ead`
- [x] **EPIC-PROCESS 流程红线**（CLAUDE.md + TASKS.md台账 + PROJECT-STATUS.md审计 + 记忆）— 据联网研究AI-agent最佳实践确立，证据：master `8eca0ed`，DoD全勾

---

## DOING（在制，≤2）

- [ ] **EPIC-A1-收尾：境界竖切剩余**（分支 feat/jianghu-v1.2-A1）— doing
  - [ ] **A1.3 统一双轨命名** — 设计稿 ✅ written（`specs/...A1.3-统一双轨命名-design.md`），**待用户审** → 审过转 impl
    - 设计要点：双轨参照(修士轨‖武夫轨) + 21路命名总表 + 规整(魔修UT错位bug/雷错字/体名实不符) + 小境界两层模型 + determinism安全
    - impl 待办：safe rename §3标▲路(魔8名全改/雷2/体顶/法顶/阵顶/鬼2/毒蛊2/符2) + 跨路命名一致性测试(护栏魔bug) + off逐字节
  - [ ] A1.5 三轴查询 API + Chronicle 大小境界渲染（含 MajorRealmNames 存储决策、命/体大境界名落定）
  - [ ] A1.6 境界竖切 gate（INV-REALM-1/INV-UT-MONO/off逐字节/auditor）

---

## TODO（待办，按优先）

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
- 2026-06-14(第二次)：机器核验。`git log` 实证 master=`8eca0ed`、`feat/jianghu-v1.2-A1`=`19d6ead`(224绿在)、status仅`?? pixel/`。回写：EPIC-PROCESS 双挂修正(DONE 唯一+sha)；WIP警告纠为 doing=0；删工具 stale task #18/#22(superseded by EPIC-B)。确认：`.gitignore` 漏 `_research/raw3/`(22跟踪文件)与 `pixel/out/`(EPIC-TIDY 待补)。未重跑测试(已commit验过，git证)。
