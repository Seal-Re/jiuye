# -*- coding: utf-8 -*-
"""synth 返回前端截断, 丢了前 4 库。从 journal 抽 6 个 raw draft 原文重组完整 内容补遗,
   再附 synth 的第七部一致性自检(synth value-add)。"""
import json, io, sys, re
sys.stdout.reconfigure(encoding="utf-8")  # 避 GBK stdout 崩 (∂/↔ 等)

JOURNAL = r"C:\Users\seal\.claude\projects\D--AgentWorkStation-Any-------\e733e301-5a5d-4608-8425-f27261a1e5ee\subagents\workflows\wf_4b7b907b-bd9\journal.jsonl"
OUTPUT  = r"C:\Users\seal\AppData\Local\Temp\claude\D--AgentWorkStation-Any-------\e733e301-5a5d-4608-8425-f27261a1e5ee\tasks\wc2xazqgj.output"
DST     = r"D:\AgentWorkStation\Any\武侠人设生成\docs\superpowers\specs\2026-06-14-WorldBible-九野-内容补遗-canonical.md"

# 递归找所有 {library, content} 字典 (含 JSON-string 内嵌)
found = {}
def walk(o, depth=0):
    if depth > 12:
        return
    if isinstance(o, dict):
        if "library" in o and "content" in o and isinstance(o.get("content"), str):
            lib, con = o["library"], o["content"]
            if lib not in found or len(con) > len(found[lib]):
                found[lib] = con
        for v in o.values():
            walk(v, depth + 1)
    elif isinstance(o, list):
        for v in o:
            walk(v, depth + 1)
    elif isinstance(o, str) and len(o) > 20 and ('"library"' in o and '"content"' in o):
        try:
            walk(json.loads(o), depth + 1)
        except Exception:
            pass

with io.open(JOURNAL, encoding="utf-8") as f:
    for line in f:
        line = line.strip()
        if not line:
            continue
        try:
            walk(json.loads(line))
        except Exception:
            pass

# 期望 6 库; 按业务顺序排
ORDER = ["初始势力", "地理骨架", "HistoryAnchor", "奇遇", "命名", "功法补遗"]
def rank(name):
    for i, k in enumerate(ORDER):
        if k in name:
            return i
    return 99
libs = sorted(found.items(), key=lambda kv: rank(kv[0]))

# synth 第七部一致性自检 (从 .output codex 切)
seventh = ""
try:
    with io.open(OUTPUT, encoding="utf-8") as f:
        codex = json.load(f).get("result", {}).get("codex", "")
    m = re.search(r'(?m)^## 第七部.*', codex)
    if m:
        seventh = codex[m.start():]
except Exception as e:
    seventh = f"(第七部提取失败: {e})"

# 组装
parts = [
    "# World Bible · 《九野》内容补遗（Canonical 附录）",
    "",
    "> 本文是《九野·末劫将临》World Bible 的**内容层附录**：机制以 Bible 正文为准, 本附录只填具体内容(势力名册/地理地标/历史锚点/奇遇库/命名池/功法补遗)。",
    "> 由 content-codex workflow 6 内容库 draft 重组(synth 返回前端截断, 已从 journal 恢复全 6 库原文) + synth 第七部一致性自检。",
    "> 批判提出 16 项跨库对账(blocker 3), 见 `2026-06-14-内容补遗-对账backlog.md`, spec-revision 阶段逐条消解。",
    "",
    "---",
    "",
]
NAMES = ["第一部 · 初始势力 Landscape 库", "第二部 · 地理骨架命名库", "第三部 · HistoryAnchor 历史锚点库",
         "第四部 · 奇遇 Storylet 库", "第五部 · 命名生成池库", "第六部 · 12 路功法补遗库"]
for i, (lib, con) in enumerate(libs):
    title = NAMES[i] if i < len(NAMES) else lib
    parts.append(f"# {title}")
    parts.append("")
    parts.append(con.strip())
    parts.append("")
    parts.append("---")
    parts.append("")
if seventh:
    parts.append(seventh.strip())

doc = "\n".join(parts)
with io.open(DST, "w", encoding="utf-8", newline="\n") as f:
    f.write(doc)

print("=== 恢复的 draft 库 ===")
for lib, con in libs:
    print(f"  [{rank(lib):>2}] {lib}: {len(con)} chars")
print("第七部一致性自检:", len(seventh), "chars")
print("总文档:", len(doc), "chars")
print("写入:", DST)
