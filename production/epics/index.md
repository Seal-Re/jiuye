# Epics Index — jiuye（武侠人设生成 / 江湖涌现模拟）

> 真相源（红线 A.2）。由 TASKS.md 迁入（2026-06-15）。状态枚举 = {Planned, In Progress, In Review, Done, Deferred, Blocked, **Built not wired**, **Wired**, **Partially wired**}。
> 机器可读状态在 `production/sprint-status.yaml`；本表为人读总览。WIP(In Progress)≤2，当前=**1**（#15 combat-variance，adr-0008 落地；cv-001 Ready 待 /dev-story。〔A.6 审计 2026-06-26：#5 cultivation-a2 陈旧 In Progress→Done，全 26 story git 证实；此前 #1 combat-r2 同样订正〕）。
> **Built not wired** = 代码已建+单测绿，但未接生产主循环（死代码）；属 Deferred 子态，待 wiring story。源 CR-2026-06-25 C-1（R-1(b) 决策）。
> **Wired** = 已接生产主循环可激活；**Partially wired** = 部分接线（如 story-008 接 Map/Faction 但 membership 待 story-009）。

| # | Epic | slug | Layer | Status | 证据/备注 |
|---|---|---|---|---|---|
| — | v1.0 内核（事件驱动/Pcg32/RuleBrain/生老死） | — | Foundation | Done | 38 测试 master（历史，未建 epic 目录） |
| — | 修炼 A.0（21 路引擎） | — | Core | Done | 204 测试 master `579659f`（历史） |
| — | A.1 境界竖切 | — | Core | Done | 232 绿 `3ea18da`，auditor 过（历史；A1.4→combat-r2/cultivation-a1-rest） |
| — | EPIC-PROCESS 流程红线（CLAUDE.md+台账+审计+记忆） | — | meta | Done | master `8eca0ed`，DoD 全勾（历史；流程纪律本身，现升级为 CCGS 骨架） |
| 1 | 战斗系统 R2 + 平衡（模块化） | `combat-r2` | Core | **Done** | batch1-6 全落（ResolveR2/法宝/硬化gate）；EPIC header Done @ `7920044`；850 绿。〔A.6 审计 2026-06-25 订正陈旧 In Progress 标记〕 |
| 2 | 真·全量机制结构化 | `combat-fullstruct` | Core | Deferred | 依赖 combat-r2 done；derived 求和/克制矩阵/召唤物/唯一签名全迁 |
| 3 | 平衡标定 INV-CROSS | `balance-cross` | Core | Designed | 🔴 最大功能缺口；设计 `336280d` |
| 4 | 修炼 A.1 余项（10 态/4 劫/5 失败/寿元） | `cultivation-a1-rest` | Core | Designed | 设计完；含 A1.4 blocked（依赖 balance-cross） |
| 5 | 修炼 A.2（道心/破单调/奇遇/闭关） | `cultivation-a2` | Feature | **Done** | 全 26 story 实现（道心/日课/闭关/奇遇/集成硬化）；〔A.6 审计 2026-06-26 订正陈旧 In Progress→Done，git 证据 + 874 绿〕 |
| 6 | 修炼 A.3（转职/觉醒/双修） | `cultivation-a3` | Feature | Designed | A3-FINAL |
| 7 | 戏剧引擎 B | `drama-engine` | Feature | **Done** | GDD ✅ + drama-003~013 全实现（恩怨账本/复仇弧 5 态机/跨代继承/storylet/受控耦合 RuleBrain 零改）；AC-1~10 全过；1051 绿；--drama/dramaSeedFeuds 激活。off 逐字节铁律守住 |
| 8 | 地图系统 C | `map-system` | Feature | **Wired** | WorldMap/Kruskal/Factory 已接 WorldFactory/Advance（story-008 `a05cd8d`，--map 激活）；懒加载待后续 map story |
| 9 | 门派 Faction D | `faction` | Feature | **C.0 Done** | C.0 全实现：接线+membership+生命周期（008/009）+ 贡献晋升（010）+ 夺地世仇（011）；C.1 朝廷/经营=远期 |
| 10 | LLM 脑 v1.1（黑盒 API 多智能体涌现） | `llm-brain` | Feature | Not designed | 🔴 原始核心愿景未设计未建（R-5 sync-over-async 全链路 async 绑此 epic） |
| 11 | 系统集成层 | `integration` | Feature | **Partially wired** | 001-007 合约 + **story-008 接线闭 C-1**（done `a05cd8d`）；**story-009** membership 接线 (ready-for-dev，未启动) |
| 12 | 可视化（像素 tile / 古风 UI） | `visualization` | Presentation | Spike only | spike + 规则 doc（B.8 分轨） |
| 13 | 仓库整理 | `repo-tidy` | chore | Todo | _research/raw3、pixel 决断、icon_gen bug |
| 14 | Godot 宿主层（View/Host） | `godot-host` | Presentation | **Planned** | 引擎目标 Unity→**Godot 4.x .NET**（2026-07-03，[adr-0004](../../docs/architecture/adr-0004-godot-view-host-boundary.md) Accepted）。边界已立：Model→View 单向流+`WorldBridge`→`[Signal]`、固定时间步累加器（delta 不进 Core）、**2D 等距 TileMap iso 坐标红线（预留 0 代码）**。Phase-2 盘点证 Core 零冲突（1062 绿）。**待接入闸口**（无头日志证核心无死锁）+ 未来 sprint 拆 story。 |
| 15 | 方差 + 反应式 QTE 战斗模型 | `combat-variance` | Core | **In Progress** | [adr-0008](../../docs/architecture/adr-0008-variance-reactive-combat-model.md) Accepted @2026-07-06。Margin→概率映射（整数 permille 查表，非浮点 Sigmoid）替代纯 PE 差碾压；只切主动交锋，DoT/压制/法宝原样保留。cv-001 主动交锋概率拦截 **Ready**（详文已展开）；cv-002~005（削韧/标签门控+Chip/溢出+防守帧钩子/[40,60]%重标定）列 EPIC.md 条目。解锁 balance-006 判死的 Fairness 硬闸门。**触 B.2/B.3/PRNG/平衡 = 旗舰档 + A.3 核验**。 |

> 阶段（production/stage.txt）= **Production**（= AAA Alpha，CCGS 七阶映射见 `docs/agent-guide/开发流水线纪律.md` §1）。2026-07-03 推进：core-loop-fun = **validated**（Viability 实质证明：破境 UT0→8 纵深 + 19 条"立誓-寻仇-手刃"恩怨链，见 `production/playtests/2026-07-03-cd-playtest-emergence.md`）；结束 Pre-Production 内核验证期。jiuye 0-9 细分阶序保留为本表 epic 排序（0-3 done 历史，5=combat 当前，6-9 future）。
> 设计深度源仍在 `docs/legacy-specs/specs/`（18 份，原地保留，P8 增量逆向补 GDD）。
> **2026-07-03 引擎切换**：表现层目标 Unity→**Godot 4.x (.NET)**（[adr-0004](../../docs/architecture/adr-0004-godot-view-host-boundary.md) Accepted）。Core 纯逻辑零引擎依赖 → 切换对现存代码**零改动**（Phase-2 盘点全 Skip，1062 绿）。**分工厘清**：epic #8 `map-system` = **Core 侧整数图拓扑**（`WorldMap`/Kruskal，已 Wired，无空间坐标）；未来 **2D 等距 TileMap 渲染 = epic #14 `godot-host` 的 View 职责**（iso 投影浮点/像素只属宿主，红线见 adr-0004 §9.3）。二者勿混。
