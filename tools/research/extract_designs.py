# -*- coding: utf-8 -*-
"""从 workflow 任务输出 JSON 提取设计 markdown，写入 repo docs（持久化、版本化）。"""
import sys, os, json
sys.stdout.reconfigure(encoding="utf-8")

TASKS = r"C:\Users\seal\AppData\Local\Temp\claude\D--AgentWorkStation-Any-------\e733e301-5a5d-4608-8425-f27261a1e5ee\tasks"
ROOT = r"D:\AgentWorkStation\Any\武侠人设生成"

def load(task_id):
    p = os.path.join(TASKS, task_id + ".output")
    with open(p, "r", encoding="utf-8") as f:
        return json.load(f)

def result_of(data):
    # 兼容 {result:{...}} 与顶层
    return data.get("result", data)

def write(path, text):
    full = os.path.join(ROOT, path)
    os.makedirs(os.path.dirname(full), exist_ok=True)
    with open(full, "w", encoding="utf-8") as f:
        f.write(text)
    print("wrote", path, len(text), "chars")

# ---- 1. 圆桌：v1.2-B 戏剧引擎设计 ----
try:
    d = result_of(load("wauzpeivc"))
    integ = d["integration"]
    out = []
    out.append("# v1.2-B 戏剧引擎 设计（恩怨/复仇 walking-skeleton）\n")
    out.append("> 来源：5-agent 圆桌（4 设计镜 + 1 集成官，opus），集成官实读 v1.0 全部源码后综合。日期 2026-06-13。")
    out.append("> 状态：设计完成，待 v1.2-A(修炼路线) 之后建。R-ID/确定性约束见正文。\n")
    out.append("---\n")
    out.append(integ["design"])
    out.append("\n\n---\n\n## v1.2.0 walking-skeleton 步骤（scopeV120）\n")
    for i, s in enumerate(integ.get("scopeV120", [])):
        out.append(f"{i}. {s}")
    out.append("\n## 冲突裁决（conflictsResolved）\n")
    for s in integ.get("conflictsResolved", []):
        out.append(f"- {s}")
    out.append("\n## 风险（risks）\n")
    for s in integ.get("risks", []):
        out.append(f"- {s}")
    out.append("\n## 强项（strengths）\n")
    for s in integ.get("strengths", []):
        out.append(f"- {s}")
    out.append("\n## 开放问题（openQuestions）\n")
    for s in integ.get("openQuestions", []):
        out.append(f"- {s}")
    write(r"docs\superpowers\specs\2026-06-13-v1.2-B-戏剧引擎-design.md", "\n".join(out))
except Exception as e:
    print("圆桌 extract FAIL:", repr(e)[:300])

# ---- 2. fleet1：v1.2-A 修炼路线 registry 调研 ----
try:
    d = result_of(load("wx4spm6sd"))
    syn = d["synthesis"]
    out = []
    out.append("# v1.2-A 修炼路线 注册表 — 调研与设计输入\n")
    out.append("> 来源：研读 fleet（7 部作品戏剧抽取 + 4 域修炼路线编目 + 合成，opus）。日期 2026-06-13。")
    out.append(f"> 转职道路数 pathCount = {syn.get('pathCount')}（≥20）。\n")
    out.append("---\n")
    out.append("## 数据驱动可扩展注册表设计\n")
    out.append(syn.get("extensibilityDesign", "(missing)"))
    out.append("\n\n---\n\n## 克制关系网\n")
    out.append(syn.get("counterNetwork", "(missing)"))
    out.append("\n\n---\n\n## 21 转职道路目录（结构化）\n")
    for p in syn.get("cultivationCatalog", []):
        out.append(f"\n### {p.get('name','?')} — {p.get('domain','')}")
        out.append(f"- 特征：{p.get('feature','')}")
        out.append(f"- 入门：{p.get('entry','')}")
        out.append(f"- 克制：{p.get('counter','')}")
        br = p.get("branches", [])
        if br: out.append("- 分支：" + " / ".join(br))
        ex = p.get("exemplars", [])
        if ex: out.append("- 代表：" + " / ".join(ex))
    out.append("\n\n---\n\n## 缺口（gaps，实现期收敛）\n")
    for s in syn.get("gaps", []):
        out.append(f"- {s}")
    write(r"docs\superpowers\research\2026-06-13-v1.2-A-修炼路线-registry-research.md", "\n".join(out))
except Exception as e:
    print("fleet1 extract FAIL:", repr(e)[:300])

# ---- 3. per-path fleet：v1.2-A 每路深度设计 ----
try:
    d = result_of(load("wflk0s47z"))
    syn = d.get("synthesis", {})
    paths = d.get("pathDesigns", [])
    out = []
    out.append("# v1.2-A 修炼路线 — 每路深度设计（功法体系/战力公式/曲线/战技）\n")
    out.append("> 来源：per-path 设计 fleet（12 路各一 opus agent 调研+设计整套 + 合成）。日期 2026-06-13。")
    out.append(f"> pathCount = {syn.get('pathCount')}。每路有独立机制、命名各异的功法类目、独立战力衡量与曲线。\n")
    out.append("---\n")
    out.append("## 统一升级 schema（数据驱动 CultivationPath）\n")
    out.append(syn.get("enrichedSchema", "(missing)"))
    out.append("\n\n---\n\n## 声明式 per-path 整数战力公式模型\n")
    out.append(syn.get("powerFormulaModel", "(missing)"))
    out.append("\n\n---\n\n## 跨路战力平衡\n")
    out.append(syn.get("balanceNotes", "(missing)"))
    out.append("\n\n---\n\n## 12 路逐路设计（结构化全文）\n")
    for p in paths:
        out.append(f"\n### {p.get('name','?')}  `{p.get('pathId','')}`  [{p.get('domain','')} · {p.get('attackDimension','')}]")
        out.append(f"- **特色机制**：{p.get('signatureFlavor','')}")
        pm = p.get("powerMetric", {})
        out.append(f"- **战力衡量**：{pm.get('description','')}")
        terms = " ; ".join(f"{t.get('source')}×{t.get('weight')}" + (f"({t.get('note')})" if t.get('note') else "") for t in pm.get("terms", []))
        out.append(f"  - terms: {terms}")
        pc = p.get("powerCurve", {})
        out.append(f"- **战力曲线**：{pc.get('shape','')} | realmMul={pc.get('realmMultipliers',[])}" + (f" — {pc.get('note','')}" if pc.get('note') else ""))
        out.append(f"- **修炼途径**：{p.get('cultivationRoute','')}")
        out.append("- **功法类目**：")
        for c in p.get("artCategories", []):
            out.append(f"  - **{c.get('categoryName','')}**（{c.get('role','')}）")
            for a in c.get("arts", []):
                out.append(f"    - {a.get('name','')} [t{a.get('tier','')}]：{a.get('effect','')}")
        out.append("- **战技**：")
        for s in p.get("combatSkills", []):
            out.append(f"  - {s.get('name','')}" + (f" [t{s.get('tier')}]" if s.get('tier') else "") + f"：{s.get('effect','')}" + (f"（{s.get('cost')}）" if s.get('cost') else ""))
        out.append(f"- **选取规则**：{p.get('selectionRule','')}")
    out.append("\n\n---\n\n## 缺口（gaps）\n")
    for s in syn.get("gaps", []):
        out.append(f"- {s}")
    write(r"docs\superpowers\research\2026-06-13-v1.2-A-修炼路线-每路深度设计.md", "\n".join(out))
except Exception as e:
    print("per-path extract FAIL:", repr(e)[:300])

# ---- 4. w0e13z3dq fixed: 修炼大小境界+全流程 FINAL 收敛 ----
try:
    d = result_of(load("w0e13z3dq"))
    fx = d["fixed"]
    out = []
    out.append("# v1.2-A2 修炼大小境界双层 + 完整修炼全流程 — FINAL 收敛设定集（唯一真相源）\n")
    out.append("> 来源：补设定(4 设计) → 批判性提问员(5) → 修复集成 fleet (w0e13z3dq, opus)。批判员实读 v1.0 源码，纠正多处真错(Σ=80 运行期伪不变量 / Lifespan 不可变 / RNG 流号 / 延寿量级压制人口)。日期 2026-06-13。")
    out.append("> 前 4 份 raw 设定(A2/C/A.2/A.0)已作废，以本收敛版为准。\n")
    out.append("---\n")
    out.append(fx.get("completeSetting", "(missing)"))
    out.append("\n\n---\n\n## 对 v1.2-A spec 的修订点（specRevisions，世界观合并期应用）\n")
    for s in fx.get("specRevisions", []): out.append(f"- {s}")
    out.append("\n## 已修复的批判问题（resolvedProblems）\n")
    for s in fx.get("resolvedProblems", []): out.append(f"- {s}")
    out.append("\n## 仍开放（remainingOpen → 雕琢轮 / 标定期）\n")
    for s in fx.get("remainingOpen", []): out.append(f"- {s}")
    out.append(f"\n## 体量裁决（bodySizeVerdict）\n{fx.get('bodySizeVerdict','')}")
    write(r"docs\superpowers\specs\2026-06-13-v1.2-A2-修炼大小境界与全流程-FINAL-design.md", "\n".join(out))
except Exception as e:
    print("w0e13z3dq fixed extract FAIL:", repr(e)[:300])

# ---- 5. wszfghsqq synthesis: 地图+门派 worldview canonical ----
try:
    d = result_of(load("wszfghsqq"))
    sy = d["synthesis"]
    out = []
    out.append("# v1.2-C/D 江湖地图 + 门派系统 — 修复后世界观设定（canonical）\n")
    out.append("> 来源：地图门派 fleet (wszfghsqq, 3 设计 + 2 批判性提问员 + 1 集成, opus)。批判员实读源码，与修炼批判独立印证同一真相(Σ运行期≠80 / RuleBrain.Travel 非拓扑须改 / DeterminismTests 只比 Chronicle 须建全状态快照 / RngStreamIds: cult=5/drama=6/map=7/faction=8)。日期 2026-06-13。")
    out.append("> 三份 raw 草案已降级 alternatives，以本收敛版为准。\n")
    out.append("---\n")
    out.append(sy.get("worldviewSetting", "(missing)"))
    out.append("\n\n---\n\n## 对 v1.0/v1.2 的改动点（specImpact，合并期应用）\n")
    for s in sy.get("specImpact", []): out.append(f"- {s}")
    out.append("\n## walking-skeleton 拆刀（C 地图先 → D 门派）\n")
    for s in sy.get("scopeWalkingSkeleton", []): out.append(f"- {s}")
    out.append("\n## 已修复批判问题（resolvedProblems）\n")
    for s in sy.get("resolvedProblems", []): out.append(f"- {s}")
    out.append("\n## 仍开放（remainingOpen）\n")
    for s in sy.get("remainingOpen", []): out.append(f"- {s}")
    write(r"docs\superpowers\specs\2026-06-13-v1.2-C-江湖地图与门派系统-design.md", "\n".join(out))
except Exception as e:
    print("wszfghsqq synthesis extract FAIL:", repr(e)[:300])

# ---- 6. w9gs3jife synthesis: 破单调/奇遇/闭关/道心 增补 ----
try:
    d = result_of(load("w9gs3jife"))
    sy = d["synthesis"]
    out = []
    out.append("# v1.2-A3 破单调·奇遇微决策·闭关时序·12路道心 — FINAL 增补设定\n")
    out.append("> 来源：修炼雕琢 fleet (w9gs3jife, 3 雕琢 + 2 批判性提问员 + 1 集成, opus)。日期 2026-06-13。")
    out.append("> 三档时间尺度破单调(日课微决策/QBN奇遇storylet/突破劫) + 闭关DES单点wake(不退Scheduler) + 12路道心(daoHeart/comprehension) + RNG裁决(修炼Split5优先,戏剧顺延Split6)。建在 A2-FINAL 之上,复用其 CultivationState。\n")
    out.append("---\n")
    out.append(sy.get("refinedSystem", "(missing)"))
    out.append("\n\n## specImpact（合并期应用）\n")
    for s in sy.get("specImpact", []): out.append(f"- {s}")
    out.append("\n## resolvedProblems\n")
    for s in sy.get("resolvedProblems", []): out.append(f"- {s}")
    out.append("\n## remainingOpen\n")
    for s in sy.get("remainingOpen", []): out.append(f"- {s}")
    write(r"docs\superpowers\specs\2026-06-13-v1.2-A3-破单调奇遇闭关道心-FINAL-design.md", "\n".join(out))
except Exception as e:
    print("w9gs3jife extract FAIL:", repr(e)[:300])

print("DONE")
