# Gate Check: Pre-Production → Production

**Date**: 2026-07-03
**Mode**: lean（4 director 面板）
**Checked by**: `/gate-check production`（红线 A.10 阶段坐实）
**HEAD 基线**: `6f3220d`（分支 feat/cultivation-a2）｜`dotnet test` = **1051 绿 / 0 失败**（主控重跑核实）

---

## 目的

用户指令「机器证据坐实阶段」。上轮据圆桌 Seat C + CCGS 闸门判据 + 用户流水线指令，将 `stage.txt` 从误标的 `Production` 订正为 `Pre-Production`。本次跑正式闸门，**用机器/面板证据验证该订正**——若确在 Pre-Production，则 Pre-Production→Production 闸门应过不了。

## Director 面板 — 一致 CONCERNS（0 READY / 0 NOT READY）

| Director | Verdict | 核心发现 |
|---|---|---|
| Creative | CONCERNS | 核心幻想（观看涌现江湖）functional 但**未验证 fun**；唯一定性读（圆桌）评"活得机械/模板化"。建议零代码 CD-PLAYTEST（人读 5 份 chronicle 评"好看吗"） |
| Technical | CONCERNS | 地基被机器守（确定性/off逐字节/module factory），高于多数项目。缺 architecture.md/control-manifest = 文档债非结构阻断。3 实质条件：llm-brain async ADR（B.2/B.3 风险）、balance 收敛、traceability |
| Producer | CONCERNS | **build order 问题非 build quality**：无 VS/playtest 却已建 5 epic substrate。建议首个 Production 里程碑 = Vertical Slice；`production/milestones/` 为空 |
| Art | CONCERNS | 当前 Production 波全是 Core/sim 层，零 Presentation story；B.8 分轨把可视化正确排在 integration 后。art-bible 缺属重定位非从零 |

## Required Artifacts: ~5/13

- ✅ sprint plans（sprint-2..6）· 3 ADRs（全 **Accepted**，主控核实）· 10 epics + stories · MVP GDDs(4) + systems-index
- ❌ **architecture.md**（required·硬缺）· **control-manifest.md**（required·硬缺）· art-bible · UX specs · HUD · entity-inventory
- ❌ playtests（production/playtests/ 缺）· ⚠️ Vertical Slice 缺 —— gate 明标"recommended, **not blocking**" → 该项计 CONCERNS

## Quality Checks

- ✅ 测试 1051 绿（Chain-of-Verification 重跑核实）· ✅ CLI build 跑通（`dotnet run` exit 0，产 175 条编年史）
- ❌ **core-loop fun 未验证**（无 playtester）· ❌ core fantasy 未验证交付

## Chain-of-Verification（§5a）：5 问，≥2 工具动作 — **verdict 由 CONCERNS 上调 FAIL**

- Q2 [TOOL] 重扫硬缺 artifact：`architecture.md`/`control-manifest.md` 是 gate 的**无条件 required**（不同于 VS 明标 not-blocking）→ 按字面 required 缺失 = FAIL。**我险些软化成 CONCERNS，CoV 纠正之**（红线 A.3 精神：不为过闸顺畅而软化）。
- Q3 [TOOL] 测试真绿？重跑 = 1051/0 ✓（非靠记忆）。
- ADR 全 Accepted ✓（story 解锁前提满足）。
- VS 缺属 CONCERNS-not-FAIL（gate 规则）✓。
- 结论一致上调：**FAIL**。

## Verdict: **FAIL**（"文档债 + 验证缺"型，非"build 坏"型）

> 关键区分：4 位 director 一致认为**地基质量过硬**（1051 绿 / 确定性机器守 / EviBound 治理）。FAIL 纯因 (a) 两个 required 文档未合并 + (b) 核心循环 fun 未经人验证 + (c) 无 VS/playtest。
>
> **本次闸门的意义 = 坐实 `stage.txt = Pre-Production` 的订正正确**：我们机器证据地**过不了** Production 闸门，故当前确在 Pre-Production。红线 A.10 的阶段锚由此坐实，非推断。

## 最短过闸路径（若目标是正式进 Production）

1. 偿文档债：合并 `architecture.md` + `control-manifest.md`（内容已散在 CLAUDE.md §F / 3 ADR / agent-guide，TD 评"低成本"）——消除两个硬 FAIL 条件。
2. 验证 fun：CD 的零代码 CD-PLAYTEST（人读 5 份 chronicle 评"好看吗"）——把 substrate 从 functional 转 validated。
3. （可选）建 Vertical Slice + ≥1 playtest —— 消除 VS CONCERNS。
4. llm-brain 若上船：先出 async-vs-determinism ADR（TD 条件 1）。

## 附带修正（A.2 单一真相）

`production/epics/index.md:28` 陈旧仍称"stage=Production"（上轮改 stage.txt 时漏同步镜像，PR+TD 两席独立揪出）→ 本轮订正为 Pre-Production，附红线 A.10 指针。

---

*面板出席：Creative / Technical / Producer / Art（均 Opus，红线 B.7）+ 主控 CoV 综合。gate-check 协议：FAIL → stage.txt 不动（已是正确 Pre-Production）。*
