# -*- coding: utf-8 -*-
"""提取 content-codex workflow 的返回: codex → canonical 附录文档; issues → 对账 backlog。"""
import json, re, io, sys

SRC = r"C:\Users\seal\AppData\Local\Temp\claude\D--AgentWorkStation-Any-------\e733e301-5a5d-4608-8425-f27261a1e5ee\tasks\wc2xazqgj.output"
DST_CODEX = r"D:\AgentWorkStation\Any\武侠人设生成\docs\superpowers\specs\2026-06-14-WorldBible-九野-内容补遗-canonical.md"
DST_BACKLOG = r"D:\AgentWorkStation\Any\武侠人设生成\docs\superpowers\specs\2026-06-14-内容补遗-对账backlog.md"

with io.open(SRC, encoding="utf-8") as f:
    data = json.load(f)

# 结果可能在顶层或 result.* 下
res = data.get("result", data)
codex = res["codex"]
issues = res.get("issues", [])

with io.open(DST_CODEX, "w", encoding="utf-8", newline="\n") as f:
    f.write(codex)

# 落对账 backlog (按 severity 分组)
order = {"blocker": 0, "major": 1, "minor": 2}
issues_sorted = sorted(issues, key=lambda i: order.get(i.get("severity", "minor"), 9))
lines = ["# 内容补遗 · 跨库对账 backlog（content-codex 批判产出）", "",
         "> 这些是批判性提问员对 6 内容库提的真问题。**不静默丢** — spec-revision / writing-plans 阶段逐条消解或显式取舍。",
         f"> 总 {len(issues)} 条：blocker {sum(1 for i in issues if i.get('severity')=='blocker')} / "
         f"major {sum(1 for i in issues if i.get('severity')=='major')} / "
         f"minor {sum(1 for i in issues if i.get('severity')=='minor')}", ""]
for sev in ["blocker", "major", "minor"]:
    grp = [i for i in issues_sorted if i.get("severity") == sev]
    if not grp:
        continue
    lines.append(f"## {sev.upper()} ({len(grp)})")
    lines.append("")
    for n, i in enumerate(grp, 1):
        lines.append(f"### {sev[0].upper()}{n}. {i.get('where','(无位置)')}")
        lines.append(f"- **问题**: {i.get('problem','')}")
        lines.append(f"- **修**: {i.get('fix','')}")
        lines.append("")
with io.open(DST_BACKLOG, "w", encoding="utf-8", newline="\n") as f:
    f.write("\n".join(lines))

# 验证输出 (不污染上下文, 只打印摘要)
heads = re.findall(r'(?m)^#{1,3} .+$', codex)
print("=== CODEX ===")
print("codex chars:", len(codex))
print("headers:", len(heads))
for h in heads:
    print("  ", h)
print("=== ISSUES ===")
print("total:", len(issues),
      "blocker:", sum(1 for i in issues if i.get('severity') == 'blocker'),
      "major:", sum(1 for i in issues if i.get('severity') == 'major'),
      "minor:", sum(1 for i in issues if i.get('severity') == 'minor'))
print("=== WROTE ===")
print(DST_CODEX)
print(DST_BACKLOG)
