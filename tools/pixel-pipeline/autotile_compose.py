# -*- coding: utf-8 -*-
"""九野 · Alpha 掩码 Autotile 合成器 (v5)
AI 只生成 100% seamless 纯纹理 (草地/泥土/沙地/石头...)
Python 用 16 张预制 48×48 像素掩码合成 autotile 套件。

工作流:
  AI 生成纯草地 texture (48×48 seamless)
  AI 生成纯泥土 texture (48×48 seamless)
  → Python Image.composite + 16 张掩码 → 16 块 autotile
"""
import os
from PIL import Image

TILE = 48

# ═══════════════════════════════════════════════
#  16 种 autotile 掩码生成 (代码定义, 不依赖AI)
#  白=保留地形A(如草地), 黑=显露地形B(如泥土)
#  交界处用 1px 抖动过渡
# ═══════════════════════════════════════════════

def make_mask_center():
    """中心: 全白 (纯地形A)"""
    return Image.new("L", (TILE, TILE), 255)

def make_mask_edge(direction):
    """边缘: 一半白一半黑
    direction: n/s/e/w
    n=上半白下半黑, s=下半白上半黑, e=右半白左半黑, w=左半白右半黑
    """
    mask = Image.new("L", (TILE, TILE), 0)
    pixels = mask.load()
    for y in range(TILE):
        for x in range(TILE):
            if direction == "n" and y < TILE // 2: pixels[x, y] = 255
            elif direction == "s" and y >= TILE // 2: pixels[x, y] = 255
            elif direction == "w" and x < TILE // 2: pixels[x, y] = 255
            elif direction == "e" and x >= TILE // 2: pixels[x, y] = 255
            # 交界处抖动
            boundary = TILE // 2
            if direction in ("n", "s") and abs(y - boundary) <= 1:
                pixels[x, y] = 255 if (x + y) % 2 == 0 else 0
            elif direction in ("w", "e") and abs(x - boundary) <= 1:
                pixels[x, y] = 255 if (x + y) % 2 == 0 else 0
    return mask

def make_mask_corner_outer(corner):
    """外角: 只有四分之一白 (地形A的圆角)
    corner: nw/ne/sw/se
    nw=左上白, ne=右上白, sw=左下白, se=右下白
    """
    mask = Image.new("L", (TILE, TILE), 0)
    pixels = mask.load()
    cx, cy = TILE // 2, TILE // 2
    for y in range(TILE):
        for x in range(TILE):
            # 四分之一圆
            if corner == "nw" and x < cx and y < cy: pixels[x, y] = 255
            elif corner == "ne" and x >= cx and y < cy: pixels[x, y] = 255
            elif corner == "sw" and x < cx and y >= cy: pixels[x, y] = 255
            elif corner == "se" and x >= cx and y >= cy: pixels[x, y] = 255
            # 圆弧抖动
            dx, dy = x - cx, y - cy
            dist = (dx * dx + dy * dy) ** 0.5
            if 0 < dist <= cx + 1 and dist >= cx - 1:
                in_quarter = (
                    (corner == "nw" and x < cx and y < cy) or
                    (corner == "ne" and x >= cx and y < cy) or
                    (corner == "sw" and x < cx and y >= cy) or
                    (corner == "se" and x >= cx and y >= cy)
                )
                if in_quarter:
                    pixels[x, y] = 255 if (x + y) % 2 == 0 else 0
    return mask

def make_mask_corner_inner(corner):
    """内角: 四分之三白 (地形A的L形)
    corner: nw/ne/sw/se
    nw=右下3/4白(左上1/4黑), 以此类推
    """
    mask = Image.new("L", (TILE, TILE), 255)
    pixels = mask.load()
    cx, cy = TILE // 2, TILE // 2
    for y in range(TILE):
        for x in range(TILE):
            if corner == "nw" and x < cx and y < cy: pixels[x, y] = 0
            elif corner == "ne" and x >= cx and y < cy: pixels[x, y] = 0
            elif corner == "sw" and x < cx and y >= cy: pixels[x, y] = 0
            elif corner == "se" and x >= cx and y >= cy: pixels[x, y] = 0
            # L形交界抖动
            dx, dy = x - cx, y - cy
            if corner == "nw" and x < cx and y < cy:
                dist = max(cx - x, cy - y)
                if dist <= 2: pixels[x, y] = 255 if (x + y) % 2 == 0 else 0
            elif corner == "ne" and x >= cx and y < cy:
                dist = max(x - cx + 1, cy - y)
                if dist <= 2: pixels[x, y] = 255 if (x + y) % 2 == 0 else 0
            elif corner == "sw" and x < cx and y >= cy:
                dist = max(cx - x, y - cy + 1)
                if dist <= 2: pixels[x, y] = 255 if (x + y) % 2 == 0 else 0
            elif corner == "se" and x >= cx and y >= cy:
                dist = max(x - cx + 1, y - cy + 1)
                if dist <= 2: pixels[x, y] = 255 if (x + y) % 2 == 0 else 0
    return mask


# ═══════════════════════════════════════════════
#  生成 16 块 autotile (标准 47-tile 子集)
# ═══════════════════════════════════════════════
SLICE_MAP = {
    "center":            make_mask_center,
    "edge-n":            lambda: make_mask_edge("n"),
    "edge-s":            lambda: make_mask_edge("s"),
    "edge-e":            lambda: make_mask_edge("e"),
    "edge-w":            lambda: make_mask_edge("w"),
    "corner-outer-nw":   lambda: make_mask_corner_outer("nw"),
    "corner-outer-ne":   lambda: make_mask_corner_outer("ne"),
    "corner-outer-sw":   lambda: make_mask_corner_outer("sw"),
    "corner-outer-se":   lambda: make_mask_corner_outer("se"),
    "corner-inner-nw":   lambda: make_mask_corner_inner("nw"),
    "corner-inner-ne":   lambda: make_mask_corner_inner("ne"),
    "corner-inner-sw":   lambda: make_mask_corner_inner("sw"),
    "corner-inner-se":   lambda: make_mask_corner_inner("se"),
}


def generate_autotile_set(tex_a_path, tex_b_path, output_dir, prefix):
    """从两张纯纹理生成完整 autotile 套件

    tex_a_path: 地形A (如草地) — 48×48 seamless
    tex_b_path: 地形B (如泥土) — 48×48 seamless
    output_dir: 输出目录
    prefix: 文件前缀 (如 "grass")
    """
    tex_a = Image.open(tex_a_path).convert("RGBA").resize((TILE, TILE), Image.NEAREST)
    tex_b = Image.open(tex_b_path).convert("RGBA").resize((TILE, TILE), Image.NEAREST)

    os.makedirs(output_dir, exist_ok=True)
    generated = []

    for tile_id, mask_fn in SLICE_MAP.items():
        mask = mask_fn()
        # composite: 白区显 tex_a, 黑区显 tex_b
        result = Image.composite(tex_a, tex_b, mask)
        out_path = os.path.join(output_dir, f"{prefix}-{tile_id}.png")
        result.save(out_path)
        generated.append(f"{prefix}-{tile_id}")

    return generated


if __name__ == "__main__":
    import sys
    if len(sys.argv) < 4:
        print("Usage: python autotile_compose.py <tex_a> <tex_b> <output_dir> <prefix>")
        print("Example: python autotile_compose.py grass.png dirt.png out/v1_tiles grass")
        sys.exit(1)
    tex_a, tex_b, out_dir, prefix = sys.argv[1], sys.argv[2], sys.argv[3], sys.argv[4]
    result = generate_autotile_set(tex_a, tex_b, out_dir, prefix)
    print(f"Generated {len(result)} autotiles → {out_dir}")
    for r in result:
        print(f"  {r}.png")
