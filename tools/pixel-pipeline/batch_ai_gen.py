# -*- coding: utf-8 -*-
"""SunshineFlow 批量出图脚本
读取 tile_prompts.json → 逐张调用 SunshineFlow API 生成像素瓦片
输出到 out/tiles_ai/ 目录

用法:
  python tools/pixel-pipeline/batch_ai_gen.py          # 全部 83 张
  python tools/pixel-pipeline/batch_ai_gen.py --range 0 10  # 前 10 张
  python tools/pixel-pipeline/batch_ai_gen.py --ids T01-A,T08,T10  # 指定 ID

环境变量:
  SUNSHINEFLOW_API_KEY    SunshineFlow API Key
  SUNSHINEFLOW_MODEL      模型名 (默认: banana-alpha)
"""

import os, sys, json, time, argparse, subprocess

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
PROMPT_FILE = os.path.join(SCRIPT_DIR, "tile_prompts.json")
OUT_DIR = os.path.join(SCRIPT_DIR, "out", "tiles_ai")
DRY_RUN = "--dry-run" in sys.argv

# —— SunshineFlow 配置 ——
API_KEY = os.environ.get("SUNSHINEFLOW_API_KEY", "")
MODEL = os.environ.get("SUNSHINEFLOW_MODEL", "banana-alpha")

# —— 通用 prompt 后缀 (附加到每个 prompt 后面) ——
PROMPT_SUFFIX = (
    "。48x48像素,俯视视角,游戏瓦片,左上光源。"
    "16-bit RPG像素风格,清晰边缘,无抗锯齿,不超出画布。"
    "纯色背景(Layer0)或透明背景(Layer1/2/3)。"
    "输出为PNG格式,单张图片,无文字,无水印。"
)


def load_prompts():
    with open(PROMPT_FILE, "r", encoding="utf-8") as f:
        raw = f.read()
    lines = [l for l in raw.split("\n") if not l.strip().startswith("#")]
    return json.loads("\n".join(lines))


def call_sunshineflow(prompt_text, output_path, tile_id):
    """调用 SunshineFlow API 生成单张图片"""
    if DRY_RUN:
        print(f"  [DRY-RUN] {tile_id}: {prompt_text[:60]}...")
        return True

    # SunshineFlow CLI 调用 (假设已安装 sunshineflow 命令)
    cmd = [
        "sunshineflow", "generate",
        "--model", MODEL,
        "--prompt", prompt_text,
        "--output", output_path,
        "--width", "48",
        "--height", "48",
        "--api-key", API_KEY,
    ]

    try:
        result = subprocess.run(cmd, capture_output=True, text=True, timeout=120)
        if result.returncode == 0:
            print(f"  ✓ {tile_id}")
            return True
        else:
            print(f"  ✗ {tile_id}: {result.stderr[:100]}")
            return False
    except subprocess.TimeoutExpired:
        print(f"  ⏱ {tile_id}: timeout")
        return False
    except FileNotFoundError:
        print(f"  ⚠ sunshineflow CLI not found. Install: pip install sunshineflow")
        return False


def export_prompt_txt(prompts):
    """导出纯文本 prompt 列表 (供手动复制到 SunshineFlow GUI)"""
    txt_path = os.path.join(SCRIPT_DIR, "out", "all_prompts.txt")
    with open(txt_path, "w", encoding="utf-8") as f:
        for p in prompts:
            full_prompt = p["desc"] + PROMPT_SUFFIX
            f.write(f"# {p['id']} — {p['name']} (Layer {p['layer']})\n")
            f.write(f"{full_prompt}\n\n")
    print(f"Prompt list exported → {txt_path}")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--range", nargs=2, type=int, help="Start and end index")
    parser.add_argument("--ids", type=str, help="Comma-separated tile IDs")
    parser.add_argument("--dry-run", action="store_true", help="Print prompts without generating")
    args = parser.parse_args()

    prompts = load_prompts()

    # 筛选
    if args.ids:
        id_set = set(args.ids.split(","))
        prompts = [p for p in prompts if p["id"] in id_set]
    elif args.range:
        start, end = args.range
        prompts = prompts[start:end]

    os.makedirs(OUT_DIR, exist_ok=True)

    # 先导出 prompt 文本 (无论是否 dry-run)
    export_prompt_txt(prompts)

    if DRY_RUN or args.dry_run:
        print(f"\n[DRY-RUN] {len(prompts)} prompts exported to all_prompts.txt")
        print("Run without --dry-run to actually generate images.")
        return

    # 批量生成
    print(f"\nGenerating {len(prompts)} tiles via SunshineFlow ({MODEL})...")
    success = 0
    for i, p in enumerate(prompts):
        tid = p["id"]
        full_prompt = p["desc"] + PROMPT_SUFFIX
        out_path = os.path.join(OUT_DIR, f"{tid}.png")

        if os.path.exists(out_path):
            print(f"  · {tid} already exists, skip")
            success += 1
            continue

        if call_sunshineflow(full_prompt, out_path, tid):
            success += 1

        # Rate limit: 1 req/sec
        if i < len(prompts) - 1:
            time.sleep(1.1)

    print(f"\nDone: {success}/{len(prompts)} generated → {OUT_DIR}")


if __name__ == "__main__":
    main()
