# -*- coding: utf-8 -*-
"""九野 · 批量 AI 出图 v3 — 顺序调用 + ID 绑定防并发错位"""
import os, sys, subprocess, time, yaml

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
PROMPT_DIR = os.path.join(SCRIPT_DIR, "prompts", "tiles")
OUT_DIR = os.path.join(SCRIPT_DIR, "out", "tiles_ai")
GEN_SCRIPT = "C:/Users/huangjiaqi13/.claude/skills/skillhub/.claude/skills/sunshineflow-banana-alpha/scripts/generate_image.py"

os.makedirs(OUT_DIR, exist_ok=True)

PIXEL_GRID_SUFFIX = " Consistent pixel size, each pixel = 1 game unit, uniform pixel grid, no sub-pixel artifacts."

# 收集所有 prompt
all_prompts = []
for fname in ["layer0_autotile.yaml", "layer1_decoration.yaml", "layer2_building.yaml", "layer3_canopy.yaml"]:
    fpath = os.path.join(PROMPT_DIR, fname)
    if os.path.exists(fpath):
        with open(fpath, "r", encoding="utf-8") as f:
            data = yaml.safe_load(f)
        all_prompts.extend(data.get("prompts", []))
        print(f"[load] {fname}: {len(data.get('prompts', []))} prompts")

print(f"Total: {len(all_prompts)} prompts\n")

# 筛选未生成的
todo = []
for p in all_prompts:
    tid = p["id"]
    out_path = os.path.join(OUT_DIR, f"{tid}_noalpha.png")
    if not os.path.exists(out_path):
        todo.append(p)

print(f"Pending: {len(todo)}/{len(all_prompts)}\n")

# ═══════════════════════════════════════════
# 顺序调用——每次一张, 立即按 ID 命名
# ═══════════════════════════════════════════
success = 0
for i, p in enumerate(todo):
    tid = p["id"]
    layer = p.get("layer", 0)
    full_prompt = p["desc"].strip()

    if layer > 0:
        full_prompt += " Isolated on pure solid green background (#00FF00), single object centered."
    full_prompt += PIXEL_GRID_SUFFIX

    print(f"[{i+1}/{len(todo)}] {tid}...", end=" ", flush=True)

    try:
        result = subprocess.run(
            ["python", GEN_SCRIPT, "--prompt", full_prompt, "--output-dir", OUT_DIR],
            capture_output=True, text=True, timeout=120,
            env={**os.environ, "PYTHONIOENCODING": "utf-8"}
        )
        if result.returncode == 0:
            # 找最新生成的两个文件 → rename 为 {id}_alpha.png 和 {id}_noalpha.png
            gen_files = sorted(
                [f for f in os.listdir(OUT_DIR) if f.startswith("image2_")],
                key=lambda x: os.path.getmtime(os.path.join(OUT_DIR, x))
            )
            if len(gen_files) >= 2:
                recent = gen_files[-2:]
                for gf in recent:
                    suffix = "alpha" if "_alpha" in gf else "noalpha"
                    os.rename(os.path.join(OUT_DIR, gf), os.path.join(OUT_DIR, f"{tid}_{suffix}.png"))
            success += 1
            print("OK")
        else:
            print(f"FAIL")
    except Exception as e:
        print(f"ERR: {e}")

    if i < len(todo) - 1:
        time.sleep(0.5)

print(f"\nDone: {success}/{len(todo)}")
