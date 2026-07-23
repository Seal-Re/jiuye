# -*- coding: utf-8 -*-
"""批量 AI 出图——83 张全部生成"""
import json, os, sys, subprocess, time

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
PROMPT_FILE = os.path.join(SCRIPT_DIR, "tile_prompts.json")
OUT_DIR = os.path.join(SCRIPT_DIR, "out", "tiles_ai")
GEN_SCRIPT = "C:/Users/huangjiaqi13/.claude/skills/skillhub/.claude/skills/sunshineflow-banana-alpha/scripts/generate_image.py"

os.makedirs(OUT_DIR, exist_ok=True)

with open(PROMPT_FILE, "r", encoding="utf-8") as f:
    raw = f.read()
lines = [l for l in raw.split("\n") if not l.strip().startswith("#")]
prompts = json.loads("\n".join(lines))

# 只处理还没生成的
todo = []
for p in prompts:
    out_path = os.path.join(OUT_DIR, f"{p['id']}.png")
    if not os.path.exists(out_path):
        todo.append(p)

print(f"待生成: {len(todo)}/{len(prompts)}")

success = 0
for i, p in enumerate(todo):
    tid = p["id"]
    layer = p["layer"]
    full_prompt = p["desc"]

    # Layer 1/2 需要透明背景
    if layer > 0:
        full_prompt += "。透明背景。"
    else:
        full_prompt += "。纯色背景。"

    print(f"[{i+1}/{len(todo)}] {tid} ({p['name']})...", end=" ", flush=True)

    try:
        result = subprocess.run(
            ["python", GEN_SCRIPT, "--prompt", full_prompt, "--output-dir", OUT_DIR],
            capture_output=True, text=True, timeout=120,
            env={**os.environ, "PYTHONIOENCODING": "utf-8"}
        )
        if result.returncode == 0:
            success += 1
            print("OK")
        else:
            err = result.stderr[:80] if result.stderr else "unknown"
            print(f"FAIL: {err}")
    except subprocess.TimeoutExpired:
        print("TIMEOUT")
    except Exception as e:
        print(f"ERR: {e}")

    # 限速
    if i < len(todo) - 1:
        time.sleep(0.5)

print(f"\n完成: {success}/{len(todo)}")
