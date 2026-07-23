# -*- coding: utf-8 -*-
"""修复版批量重命名——按时间戳排序映射到 tile ID，不再依赖文件名解析"""
import os, json, glob

OUT_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)), "out", "tiles_ai")

with open(os.path.join(os.path.dirname(__file__), "tile_prompts.json"), "r", encoding="utf-8") as f:
    raw = f.read()
lines = [l for l in raw.split("\n") if not l.strip().startswith("#")]
prompts = json.loads("\n".join(lines))

# 按文件修改时间排序所有 PNG
files = glob.glob(os.path.join(OUT_DIR, "*.png"))
files.sort(key=os.path.getmtime)

# 取 noalpha 版本——每组时间戳的第一个 noalpha 文件
# SunshineFlow 生成: alpha先, noalpha后。按 (时间戳, noalpha优先) 分组
from collections import OrderedDict
groups = OrderedDict()
for f in files:
    fname = os.path.basename(f)
    # 提取时间戳: image2_YYYYMMDD_HHMMSS_
    if not fname.startswith("image2_"):
        continue
    ts = fname.split("_")[1]  # YYYYMMDD
    time = fname.split("_")[2]  # HHMMSS
    key = ts + "_" + time
    if key not in groups:
        groups[key] = []
    groups[key].append(f)

print("时间戳组数: %d (期望 83)" % len(groups))

# 每个组取 noalpha 版本, 按时间排序映射到 tile ID
tile_ids = [p["id"] for p in prompts]
renamed = 0
for idx, (key, fnames) in enumerate(groups.items()):
    if idx >= len(tile_ids):
        break
    # 优先 noalpha, 否则第一个
    noalpha = [f for f in fnames if "noalpha" in os.path.basename(f)]
    picked = noalpha[0] if noalpha else fnames[0]
    dst = os.path.join(OUT_DIR, "%s.png" % tile_ids[idx])
    if picked != dst:
        os.rename(picked, dst)
        renamed += 1

print("重命名: %d/%d" % (renamed, len(groups)))

# 清理残留 (alpha版和未映射的)
for f in glob.glob(os.path.join(OUT_DIR, "*.png")):
    fname = os.path.basename(f)
    if fname.startswith("image2_") or "_alpha" in fname or "_noalpha" in fname:
        os.remove(f)
        print("  清理: %s" % fname[:60])

# 最终统计
final = glob.glob(os.path.join(OUT_DIR, "*.png"))
print("\n最终: %d 张瓦片" % len(final))
missing = [tid for tid in tile_ids if not os.path.exists(os.path.join(OUT_DIR, "%s.png" % tid))]
if missing:
    print("缺失 (%d): %s" % (len(missing), ", ".join(missing[:10])))
else:
    print("全 83 张就绪!")
