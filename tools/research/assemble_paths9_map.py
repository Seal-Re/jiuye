# -*- coding: utf-8 -*-
"""抽 paths9-and-map workflow 结构化返回: 9 余路→深度doc; mapDoc→地图doc; issues→backlog2。"""
import json, io, sys
sys.stdout.reconfigure(encoding="utf-8")

SRC = r"C:\Users\seal\AppData\Local\Temp\claude\D--AgentWorkStation-Any-------\e733e301-5a5d-4608-8425-f27261a1e5ee\tasks\w30d7hsry.output"
DST_PATHS = r"D:\AgentWorkStation\Any\武侠人设生成\docs\superpowers\research\2026-06-14-修炼-余9路深度设计-canonical.md"
DST_MAP   = r"D:\AgentWorkStation\Any\武侠人设生成\docs\superpowers\specs\2026-06-14-九野-地图概要与宗门排布-canonical.md"
DST_BL2   = r"D:\AgentWorkStation\Any\武侠人设生成\docs\superpowers\specs\2026-06-14-余9路与地图-对账backlog.md"

with io.open(SRC, encoding="utf-8") as f:
    data = json.load(f)
res = data.get("result", data)
paths = res.get("paths", [])
mapDoc = res.get("mapDoc", "")
issues = res.get("issues", [])

# 余 9 路深度 doc
parts = [
    "# 修炼 · 余 9 路深度设计（Canonical）— 凑齐 21 路，不舍弃任何路径",
    "",
    "> 补深 registry 21 路中尚未深设计的 9 路(儒/魔/妖/血/毒蛊/符/傀儡/音/因果时空)到既有 12 路同等深度标准。",
    "> 与 `2026-06-13-v1.2-A-修炼路线-每路深度设计.md`(深 12 路) 合并即 21 路全量, writing-plans 取数据照搬。",
    "> 对账见 `2026-06-14-余9路与地图-对账backlog.md`。", "", "---", "",
]
for p in paths:
    parts.append(f"# {p.get('name','?')} `{p.get('pathId','?')}`")
    parts.append("")
    parts.append((p.get("content", "") or "").strip())
    parts.append("")
    parts.append("---")
    parts.append("")
with io.open(DST_PATHS, "w", encoding="utf-8", newline="\n") as f:
    f.write("\n".join(parts))

# 地图 doc
with io.open(DST_MAP, "w", encoding="utf-8", newline="\n") as f:
    f.write("# 九野 · 地图概要与宗门排布（Canonical）\n\n"
            "> World Bible §5 地理 + 内容补遗 GeoCanon/势力库 的具体落地：全图大区概览 + 邻接骨架 + 宗门/势力排布表 + geo↔faction reconcile。\n\n---\n\n"
            + (mapDoc or "").strip() + "\n")

# backlog round2
order = {"blocker": 0, "major": 1, "minor": 2}
issues_sorted = sorted(issues, key=lambda i: order.get(i.get("severity", "minor"), 9))
bl = ["# 余9路与地图 · 对账 backlog（批判产出）", "",
      f"> 总 {len(issues)} 条：blocker {sum(1 for i in issues if i.get('severity')=='blocker')} / "
      f"major {sum(1 for i in issues if i.get('severity')=='major')} / "
      f"minor {sum(1 for i in issues if i.get('severity')=='minor')}。spec-revision 逐条消解。", ""]
for sev in ["blocker", "major", "minor"]:
    grp = [i for i in issues_sorted if i.get("severity") == sev]
    if not grp:
        continue
    bl.append(f"## {sev.upper()} ({len(grp)})")
    bl.append("")
    for n, i in enumerate(grp, 1):
        bl.append(f"### {sev[0].upper()}{n}. {i.get('where','(无位置)')}")
        bl.append(f"- **问题**: {i.get('problem','')}")
        bl.append(f"- **修**: {i.get('fix','')}")
        bl.append("")
with io.open(DST_BL2, "w", encoding="utf-8", newline="\n") as f:
    f.write("\n".join(bl))

print("=== 余 9 路 ===")
for p in paths:
    print(f"  {p.get('pathId','?'):<18} {p.get('name','?'):<22} {len(p.get('content','') or '')} chars")
print("count:", len(paths), "(应为 9)")
print("=== 地图 ===")
print("mapDoc:", len(mapDoc), "chars")
print("=== issues ===")
print("total:", len(issues),
      "blocker:", sum(1 for i in issues if i.get('severity') == 'blocker'),
      "major:", sum(1 for i in issues if i.get('severity') == 'major'),
      "minor:", sum(1 for i in issues if i.get('severity') == 'minor'))
print("WROTE:", DST_PATHS, "|", DST_MAP, "|", DST_BL2)
