# -*- coding: utf-8 -*-
"""九野 · 大图切片 + 调色板量化管线 (v4)
1. 读取 AI 生成的 192×192 Terrain Group Sheet
2. 按 48×48 网格切片 (4×4=16块)
3. 全局调色板量化 (32色 LUT, K-Means, NO dither)
4. 输出到 v1_tiles/

布局映射 (4×4 grid, row-major):
  [0]NW-outer  [1]N-edge   [2]NE-outer  [3]N-edge2
  [4]W-edge    [5]CENTER   [6]E-edge    [7]CENTER2
  [8]SW-outer  [9]S-edge   [10]SE-outer [11]S-edge2
  [12]W-edge2  [13]CENTER3 [14]E-edge2  [15]CENTER4
"""
import os, sys, glob
from PIL import Image

TILE = 48
GRID = 4
SHEET = TILE * GRID  # 192

# 切片映射: grid_index → tile_id_suffix
SLICE_MAP = {
    0: "corner-outer-nw", 1: "edge-n", 2: "corner-outer-ne", 3: "edge-n2",
    4: "edge-w",          5: "center", 6: "edge-e",          7: "center2",
    8: "corner-outer-sw", 9: "edge-s", 10: "corner-outer-se",11: "edge-s2",
    12:"edge-w2",         13:"center3",14: "edge-e2",        15:"center4",
}

# 调色板: 从 master 图提取 32 色
PALETTE_COLORS = 32


def extract_palette(master_img):
    """从主图提取 32 色调色板 (K-Means via PIL quantize)"""
    quantized = master_img.convert("RGB").quantize(colors=PALETTE_COLORS, method=Image.MEDIANCUT)
    palette = quantized.getpalette()[:PALETTE_COLORS * 3]
    return palette


def apply_palette(img, palette):
    """将图片强制映射到指定调色板 (NO dither, 硬边缘)"""
    p_img = Image.new("P", (1, 1))
    p_img.putpalette(palette)
    if img.mode != "RGBA":
        img = img.convert("RGBA")
    # 分离 alpha
    alpha = img.split()[3]
    rgb = img.convert("RGB").quantize(palette=p_img, dither=Image.Dither.NONE)
    rgb = rgb.convert("RGBA")
    rgb.putalpha(alpha)
    return rgb


def slice_sheet(sheet_path, terrain_prefix, output_dir, master_palette=None):
    """切片一张 192×192 大图 → 16 个 48×48 瓦片"""
    img = Image.open(sheet_path)
    if img.size != (SHEET, SHEET):
        # 缩放到标准尺寸
        img = img.resize((SHEET, SHEET), Image.NEAREST)

    if img.mode != "RGBA":
        img = img.convert("RGBA")

    # 提取本组调色板 (如无 master)
    if master_palette is None:
        master_palette = extract_palette(img)

    sliced = []
    for idx in range(GRID * GRID):
        row = idx // GRID
        col = idx % GRID
        box = (col * TILE, row * TILE, (col + 1) * TILE, (row + 1) * TILE)
        tile = img.crop(box)

        # 调色板量化
        tile = apply_palette(tile, master_palette)

        suffix = SLICE_MAP.get(idx, f"tile{idx}")
        tile_id = f"{terrain_prefix}-{suffix}"
        out_path = os.path.join(output_dir, f"{tile_id}.png")
        tile.save(out_path)
        sliced.append(tile_id)

    return sliced, master_palette


def process_all_sheets(sheets_dir, tiles_dir):
    """处理所有大图"""
    sheets = sorted(glob.glob(os.path.join(sheets_dir, "*_noalpha.png")))
    if not sheets:
        sheets = sorted(glob.glob(os.path.join(sheets_dir, "*.png")))

    print(f"Found {len(sheets)} sheets")

    # 全局 master 调色板 (从第一张提取, 或可指定 master)
    master_palette = None
    all_sliced = []

    for sheet_path in sheets:
        fname = os.path.basename(sheet_path)
        # 提取 terrain_prefix: grass_sheet_noalpha.png → grass
        prefix = fname.replace("_sheet_noalpha.png", "").replace("_sheet.png", "").replace(".png", "")
        # 去除 image2_ 前缀
        if prefix.startswith("image2_"):
            prefix = prefix[6:]

        print(f"  slicing {fname} → prefix={prefix}")
        sliced, master_palette = slice_sheet(sheet_path, prefix, tiles_dir, master_palette)
        all_sliced.extend(sliced)
        print(f"    → {len(sliced)} tiles")

    print(f"\nTotal: {len(all_sliced)} tiles → {tiles_dir}")
    return all_sliced


if __name__ == "__main__":
    sheets_dir = sys.argv[1] if len(sys.argv) > 1 else "out/v1_sheets"
    tiles_dir = sys.argv[2] if len(sys.argv) > 2 else "out/v1_tiles"
    os.makedirs(tiles_dir, exist_ok=True)
    process_all_sheets(sheets_dir, tiles_dir)
