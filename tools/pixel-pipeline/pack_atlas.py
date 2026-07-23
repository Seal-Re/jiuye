# -*- coding: utf-8 -*-
"""九野 · 瓦片 Atlas 打包器
将 83 张 48x48 tile PNG 拼成一张 TileSet Atlas sheet。
输出: out/tileset_atlas.png (10列×9行, 480×432, 含边距)
同时输出 Godot TileSet .tres 资源配置文件。
"""
import os, json
from PIL import Image

N = 48           # tile size
COLS = 10        # columns in atlas
MARGIN = 2       # margin between tiles
SCALE = 4        # 导出缩放

OUT_DIR = os.path.join(os.path.dirname(__file__), "out")
TILE_DIR = os.path.join(OUT_DIR, "tiles")
ATLAS_PATH = os.path.join(OUT_DIR, "tileset_atlas.png")


def main():
    # 读取 prompt 列表确定顺序
    with open(os.path.join(os.path.dirname(__file__), "tile_prompts.json"), "r", encoding="utf-8") as f:
        raw = f.read()
    lines = [l for l in raw.split("\n") if not l.strip().startswith("#")]
    prompts = json.loads("\n".join(lines))

    tile_count = len(prompts)
    rows = (tile_count + COLS - 1) // COLS

    # 计算 atlas 尺寸
    cell = N + MARGIN * 2
    atlas_w = COLS * cell
    atlas_h = rows * cell
    atlas = Image.new("RGBA", (atlas_w, atlas_h), (0, 0, 0, 0))

    # ID→Atlas坐标映射
    atlas_map = {}

    for i, p in enumerate(prompts):
        tid = p["id"]
        tile_path = os.path.join(TILE_DIR, f"{tid}.png")
        if not os.path.exists(tile_path):
            print(f"  SKIP {tid} — file not found")
            continue

        col = i % COLS
        row = i // COLS
        x = col * cell + MARGIN
        y = row * cell + MARGIN

        tile = Image.open(tile_path)
        if tile.size != (N, N):
            tile = tile.resize((N, N), Image.NEAREST)

        atlas.paste(tile, (x, y))
        atlas_map[tid] = {"col": col, "row": row, "x": x, "y": y}

    # 缩放导出
    atlas.save(ATLAS_PATH)

    # 导出 Godot .tres TileSet 资源 (基础配置)
    tres_path = os.path.join(OUT_DIR, "tileset.tres")
    with open(tres_path, "w", encoding="utf-8") as f:
        f.write(f"""[gd_resource type="TileSet" load_steps=1 format=3]

[resource]
# 九野 · 瓦片图集 (83 tiles, {COLS}×{rows})
# 每个 tile 48×48, margin={MARGIN}px
# 在 Godot 编辑器中: 新建 TileSet → 加载本图集 → 设置 tile_size=48, separation={MARGIN*2}
tile_size = Vector2i(48, 48)
""")

    # 导出 JSON 映射表 (供 MapGenerator.gd 加载)
    map_path = os.path.join(OUT_DIR, "atlas_map.json")
    with open(map_path, "w", encoding="utf-8") as f:
        json.dump(atlas_map, f, indent=2, ensure_ascii=False)

    print(f"Atlas: {atlas_w}×{atlas_h} ({tile_count} tiles, {COLS}×{rows})")
    print(f"  → {ATLAS_PATH}")
    print(f"  → {tres_path}")
    print(f"  → {map_path}")

if __name__ == "__main__":
    main()
