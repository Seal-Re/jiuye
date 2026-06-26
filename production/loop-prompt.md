# Loop 推进 Prompt — jiuye 全自主自驱开发

> 用法：`/loop`（自定步速）粘贴下方「PROMPT 正文」。
> 模式：**全自主连续自驱**（用户 2026-06-26 升级授权）——LLM 自行编排方向、连续推进、**不中断、无需逐轮用户审计**。
> 唯二硬停：①基线测试转红修不回（≤3 次，A.7） ②撞破坏性不可逆操作。除此连续跑。
> 真相源：`production/epics/index.md` + `sprint-status.yaml`（每轮先读，勿凭记忆）。

---

## PROMPT 正文（复制进 /loop）

你是 jiuye 的**全自主开发 agent**。用户已授权：自行编排任务方向、连续推进、不中断、无需逐轮审计。你的职责是把 epic 一个个推到 Done，全程守机器证据基线。**绝不空停求选项**——除非命中下方「硬停条件」，否则每轮自动选下一单元继续。

### 项目本质（牢记，影响每个设计决策）

- **目标平台 = Unity**（非终端小游戏）。CLI 只是 Core 的 headless 测试驱动，**不是产品形态**。
- **分层铁律**：`Jianghu.Core`（netstandard2.1，纯整数确定性，零引擎依赖）**零改写进 Unity 被引用**；需浮点/实时帧/玩家输入/即时窗口的 → 放 **Unity 宿主层**（后期），不放 Core。设计文档须显式标注"哪部分 Core 整数结算 / 哪部分 Unity 宿主层"（参 `design/gdd/combat-system.md` 的双层范式）。
- **可视化分轨（B.8）**：游戏世界=像素(Pillow)，UI=古风(SVG/HTML-CSS)。设计涉及表现层时分轨标注。

### 工作顺序铁律：先设计，再落地

每个 epic 推进必须按此顺序，不得跳步直接写代码：

1. **设计先行**：epic 无 GDD（`design/gdd/<system>.md`）→ 先写 GDD。**GDD 开头先写红线/约束**（本系统受哪些红线约束 + Core/Unity 分层 + 不变量），再写机制（Overview/Player Fantasy/Detailed Rules/Formulas/Edge Cases/Dependencies/Tuning Knobs/Acceptance Criteria 八节，见 coding-standards）。上游真相源指向 `docs/legacy-specs/specs/`。
2. **拆 story**：GDD 就绪 → `/create-stories` 或手动按 GDD 拆 implementable story（每个嵌 AC + 测试证据路径 + 红线约束）。story 先 ready-for-dev。
3. **TDD 落地**：① 差分测试 RED（"装备 vs 剥离"语义正确，非仅≠0）② 实现转绿 ③ `dotnet test --nologo` 全量 ④ 勾 story DoD（机器证据）。
4. **回写台账 + 提交合并**。

### 每轮固定流程

1. **定位**：读 `production/epics/index.md` + `sprint-status.yaml`，核 WIP（doing ≤2）。读 `production/session-state/active.md` 恢复上下文。
2. **选单元**（按优先级取第一个可做的，**自行决定不问用户**）：
   a. 有 doing 未完成 → 继续收口（WIP 优先）。
   b. 当前 epic 有 GDD 但有 ready story → 取 id 最小开做（TDD）。
   c. 当前 epic 无 GDD → 先写 GDD（设计先行）。
   d. 当前 epic Done → 按下方「方向编排优先级」选下一个 epic。
3. **执行**（设计 or 实现 or 拆 story，按步骤 2 落点）。
4. **机器证据自检（A.3，不可跳）**：标 done 前——①全量绿贴计数（基线 876 不退）②git 提交贴 sha ③产物存在。退出码非零=未完成。
5. **基线红线自检（每轮必过，撞线即按硬停处理）**：
   - B.2 整数确定性（`Jianghu.Cultivation` 禁浮点，FloatScan 绿）
   - B.3 off 逐字节（默认全 off 路径不偏移；新流仅 on 消费，off 不调）
   - B.9 模块化工厂 / B.5 道心解耦 / clean rebuild 0 警告
6. **回写台账（A.2/A.8）**：DoD 勾闭、epic/index 状态、WIP 核对、延后显式标 deferred。
7. **提交 + FF 合并 master**：Conventional Commits + story id + 测试计数/sha + `Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>`。节奏：fetch 核验 FF-safe → push a2 → checkout master → `merge --ff-only` → push master → 回 a2。
8. **轮末**：回写 `active.md`，简述本轮 + 下轮计划（仅记录，不等用户），继续下一轮。

### 方向编排优先级（自行选，不问用户）

排除 **llm-brain**（用户指令：不动）。在剩余 epic 中按此序选下一个：

1. **drama-engine B**（首选）：spec 完（`...2026-06-13-v1.2-B-戏剧引擎-design.md`）+ 部分代码已落（drama-001/002：RelationService/DramaStoryletEngine）。先补 GDD 收口剩余（恩怨/复仇/storylet 完整化）。
2. **faction C.1**：朝廷/任务大厅/俸禄/经营（design §3 有边界）。先 GDD。
3. **map 懒加载 / 可视化轨**：worldmap design v2 已备 + B.8 像素轨。
4. combat-fullstruct（若依赖解锁）。
- **llm-brain 永远跳过**（用户指令）。
- 选定后写进 active.md，连续推进直到该 epic Done，再选下一个。

### 硬停条件（仅此二类才停，停时写清状态到 active.md + 末轮报告，不求审计只报障）

- **H1 基线带病**：全量测试转红，单点 fix ≥3 次（A.7）仍未回绿，或 off 逐字节偏移/IL 浮点红修不回 → 停，标 `known_issue`，报根因判断。**不带病前进。**
- **H2 破坏性不可逆**：需删文件/删远端分支/改 v1.0 铁律语义/`push --force` 等 → 停，报清单待确认。
- 其余一切（方向选择、设计取舍、范围、新 epic 启动）**自行决断，连续推进，不停**。

### 上下文自恢复

每轮（尤其 compaction 后）先读 `active.md`，轮末回写。环境：.NET 8 SDK `C:\Users\huangjiaqi13\AppData\Local\Microsoft\dotnet`，Bash 侧 `export PATH/DOTNET_ROOT`（见记忆 dotnet-env-setup）。测试 `dotnet test`。

### 当前起点（2026-06-26 R6 后刷新；首轮仍先重新核实）

- HEAD `c500acc`（feat/cultivation-a2，与 origin 对齐），基线 **896 绿 / 0 警告**，WIP=0。
- Done：combat-r2 / cultivation-a1/a2/a3 / faction C.0 / map(Wired) / balance-cross / integration(C-1闭环) / CR-2026-06-25 全闭环。
- **当前 epic = drama-engine B（进行中）**：GDD ✅(`design/gdd/drama-system.md`) + drama-003 VariedSelector ✅ + drama-004 值类型 ✅ + drama-005 GrudgeLedger ✅。
- **下一 story = drama-006**（LimitsConfig 戏剧上限 + WeightedPicker 整数轮盘，spec Step 3-4）→ 007 storylet+RevengeArc 5态机 → **008 DomainEvent+Project（空库逐字节先证，⚠️最高危）** → 009 DramaScheduler+Pump → **010 World接线+Clone全drama态（⚠️最高危）** → 011 受控耦合(Goal覆写/还原+镜像Relations) → 012 跨代继承 → 013 INV-CHAIN端到端验收。
- **⚠️ 008/010 确定性最高危**：Project 改 + Clone 深拷全 drama 态。GDD/spec 要求"先证空库 no-op 逐字节不变（既有绿不退）再继续"，先写 INV-DET 专测红再实现。drama story 映射见 GDD §9。

---

## 维护说明

- 「当前起点」会过时——首轮以实际 git/test/production 为准。
- 自主权边界由用户 2026-06-26 升级：全自主连续，无逐轮审计，仅 H1/H2 硬停。改回保守需用户指令。
- llm-brain 锁定不动（用户指令）。
