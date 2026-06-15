# Adoption Plan — jiuye → CCGS

> **Generated**: 2026-06-15
> **Project phase**: Production (production/stage.txt)
> **Engine**: NOT CONFIGURED (P6 将设为 .NET 8 / custom simulation)
> **Template version**: v1.0+
> **来源**: /adopt 全量审计（jiuye brownfield 采用 CCGS）。本计划 = 迁移路线图（hazy-rolling-valley 计划 P4/P8 据此执行）。

按序逐项；完成打勾。随时重跑 /adopt 查剩余 gap。

---

## Adoption Audit Summary

- Phase detected: **Production**（stage.txt override + 76 .cs / 82 测试文件）
- Engine: **NOT CONFIGURED**（P6 修复）
- GDDs audited: **0**（design/gdd/ 不存在）
- ADRs audited: **0**（docs/architecture/ 不存在）
- Stories audited: **0**（production/epics/ 空）
- 现有设计知识（非 CCGS 格式，原地保留）：docs/superpowers/specs 18 份 + TASKS.md（单一真相源）

**Gap counts:**
- BLOCKING: **0** — 尚无 systems-index，故无括号状态值陷阱
- HIGH: **3** — engine 未配（P6）/ tr-registry.yaml 缺 / control-manifest.md 缺
- MEDIUM: **3** — sprint-status.yaml 缺 / GDD 全缺 / 现有 specs 非 8 段式
- LOW: **1** — 现有审计记录为散文非结构化

> jiuye 是 brownfield（src/ 76 .cs，282 绿，活跃 combat-r2 WIP），非 fresh——不走 /start，走本采用计划。

---

## Step 1: Fix Blocking Gaps

无 BLOCKING gap（systems-index 尚未创建，故无括号状态值问题）。**注意**：P8 创建 design/gdd/systems-index.md 时，Status 列必须用合法值（`Not Started`/`In Progress`/`In Review`/`Designed`/`Approved`/`Needs Revision`）且**无括号**——否则成为 BLOCKING（破 /gate-check、/create-stories 精确匹配）。

---

## Step 2: Fix High-Priority Gaps

### 2a. Engine 未配置
- 问题：technical-preferences.md 含 `[TO BE CONFIGURED]`，ADR/engine 类 skill 失效，detect-gaps 误判 fresh。
- 修复：诚实填 `.NET 8 (custom headless simulation)` / C# / Rendering·Physics=N/A。
- 时间：5 min。**归属 hazy-rolling-valley 计划 P6。**
- [ ] technical-preferences.md engine 配置完成

### 2b. tr-registry.yaml 缺失
- 问题：无稳定 TR-ID，story 无法引用需求。
- 修复：见 Step 3a（/architecture-review bootstrap）。
- [ ] tr-registry.yaml 创建（P8）

### 2c. control-manifest.md 缺失
- 问题：无分层规则，story 无 layer 约束。
- 修复：见 Step 3b。
- [ ] control-manifest.md 创建（P8）

---

## Step 3: Bootstrap Infrastructure

### 3a. 注册既有需求（创建 tr-registry.yaml）
- 运行 /architecture-review（从 GDD/ADR bootstrap TR 注册）。前置：combat GDD + ADR 先落（P8）。
- 时间：1 session。
- [ ] tr-registry.yaml 创建

### 3b. 创建 control-manifest（含版本戳）
- 运行 /create-control-manifest。把红线 B.2（浮点禁）/B.5（daoHeart 解耦）/B.9（Modules 工厂）编入 Forbidden 列。
- 时间：30 min。
- [ ] control-manifest.md 创建

### 3c. 创建 sprint-status.yaml
- **归属 P4**（手工迁 TASKS.md 时一并建，WIP=combat-r2 doing=1）。
- [ ] sprint-status.yaml 创建（P4）

### 3d. stage.txt 权威写入
- **已于 P2 预播种** = Production。后续阶段转换由 /gate-check 写。
- [x] stage.txt = Production

---

## Step 4: Fix Medium-Priority Gaps（GDD 增量补，combat 优先）

> 策略=迁骨架+增量补文档。combat（活跃 WIP）先补，其余 defer 到各自 epic 转 doing。**归属 P8。**

- [ ] design/gdd/game-concept.md（从 WorldBible canonical 逆向）
- [ ] design/gdd/systems-index.md（系统总表，Status 无括号）
- [ ] design/gdd/combat-system.md（从 B5-R2 + 模块化 spec 逆向，8 段式，Status: Designed）— **最高优先（活跃 WIP）**
- [ ] design/gdd/cultivation-system.md（从 A.0/A.1 spec 逆向）
- [ ] ADR adr-0001-integer-determinism（B.2）
- [ ] ADR adr-0002-module-factory-effect-system（B.9，直接治理 combat-r2）
- [ ] ADR adr-0003-cultivation-off-byte-identical（B.3）

### DEFER（显式·红线 A.8 不静默）— 待各 epic 转 doing 再补 GDD/ADR
- [ ] drama-engine GDD（EPIC.md 已指向 docs/superpowers/specs/...B-戏剧引擎...）
- [ ] map-system GDD（已指向 ...C-江湖地图与门派...）
- [ ] faction GDD（同 C）
- [ ] llm-brain GDD（未设计，原始核心愿景）
- [ ] integration GDD（未设计）
- [ ] visualization GDD（spike only，B.8 分轨）

---

## Step 5: Optional Low-Priority Improvements

- [ ] 审计记录结构化（现 append-only 散文 → 可选转表格）

---

## 既有 stories 说明

jiuye 当前无 CCGS 格式 stories。P4 将从 TASKS.md 手工建 combat-r2 的 batch2-6 stories。
**batch2 在制（doing），P8 用 /create-stories regenerate 时必须排除在制 story**（CCGS 纪律：不重生成进行中的 story）。
