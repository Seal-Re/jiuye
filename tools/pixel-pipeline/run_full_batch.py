# -*- coding: utf-8 -*-
"""批量 AI 出图——83 张全部生成"""
import json, os, sys, subprocess, time

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
PROMPT_FILE = os.path.join(SCRIPT_DIR, "prompts", "tiles", "83_tiles.yaml")
OUT_DIR = os.path.join(SCRIPT_DIR, "out", "tiles_ai")
GEN_SCRIPT = "C:/Users/huangjiaqi13/.claude/skills/skillhub/.claude/skills/sunshineflow-banana-alpha/scripts/generate_image.py"
CONFIG_FILE = os.path.join(SCRIPT_DIR, "prompts", "pipeline_config.yaml")

os.makedirs(OUT_DIR, exist_ok=True)

# 读取 pipeline config——负向提示词
NEGATIVE_PROMPT = ""
if os.path.exists(CONFIG_FILE):
    import yaml as yamllib
    try:
        with open(CONFIG_FILE, "r", encoding="utf-8") as f:
            config = yamllib.safe_load(f)
        NEGATIVE_PROMPT = config.get("global_negative_prompt", "").strip()
        print(f"[pipeline] negative_prompt loaded ({len(NEGATIVE_PROMPT)} chars)")
    except Exception:
        pass  # yaml not available, use empty

# 读取所有 prompt
with open(PROMPT_FILE, "r", encoding="utf-8") as f:
    raw = f.read()
lines = [l for l in raw.split("\n") if not l.strip().startswith("#")]
prompts_data = yamllib.safe_load("\n".join(lines)) if os.path.exists(CONFIG_FILE) else None
if prompts_data is None:
    print("ERROR: cannot parse prompts")
    sys.exit(1)
prompts = prompts_data["prompts"]

# 像素网格约束后缀 (每条 prompt 自动拼接)
PIXEL_GRID_SUFFIX = (
    " Consistent pixel size, each pixel = 1 game unit, " +
    "uniform pixel grid across entire image, no sub-pixel artifacts."
)

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
    full_prompt = p["desc"].strip()

    # Layer 1/2 需要透明背景
    if layer > 0:
        full_prompt += " Transparent background."

    # 像素网格约束
    full_prompt += PIXEL_GRID_SUFFIX

    # 负向提示词 (通过 --negative_prompt 参数或拼入 prompt)
    # SunshineFlow 如不支持 negative prompt 参数，拼入主 prompt 末尾

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
