# -*- coding: utf-8 -*-
"""九野 · 工业级 Autotile 掩码合成器 v11
修正: 圆心极性 + R=12匹配直边 + alpha_composite阴影 + 移除伪随机噪点。
"""
import os
from PIL import Image, ImageChops

TILE = 48
QUAD = 24


def new_grid(fill=0):
    return [[fill] * QUAD for _ in range(QUAD)]


def grid_to_image(grid, size=TILE):
    img = Image.new("L", (size, size), 0)
    px = img.load()
    for y in range(len(grid)):
        for x in range(len(grid[0])):
            px[x, y] = grid[y][x]
    return img


def save_grid(grid, path, scale=4):
    img = grid_to_image(grid, QUAD)
    if scale > 1:
        img = img.resize((QUAD * scale, QUAD * scale), Image.NEAREST)
    img.save(path)


# ═══════════════════════════════════════════════
#  基础掩码 (24×24, 纯数学, R=12 匹配直边)
# ═══════════════════════════════════════════════

def gen_solid():
    return new_grid(255)

def gen_empty():
    return new_grid(0)

def gen_edge(orientation):
    """有机锯齿直边, boundary=12, 抖动[-1,0,1]循环"""
    m = new_grid(0)
    boundary = QUAD // 2  # 12
    jitter = [0, 1, -1] * 8  # 24元素循环

    for y in range(QUAD):
        for x in range(QUAD):
            if orientation == 'n':
                eff = boundary + jitter[x]
                m[y][x] = 255 if y > eff else 0
            elif orientation == 's':
                eff = boundary + jitter[x]
                m[y][x] = 255 if y < eff else 0
            elif orientation == 'w':
                eff = boundary + jitter[y]
                m[y][x] = 255 if x > eff else 0
            elif orientation == 'e':
                eff = boundary + jitter[y]
                m[y][x] = 255 if x < eff else 0
    return m

def gen_outer_corner(corner):
    """外凸圆弧: 草地(白)凸向泥土(黑)
    端点精准交于两条外边的1/2(12px)处, R²=720 (24²+12²)
    引入角度抖动增加草叶咬合感
    """
    import math
    m = new_grid(0)
    R_SQ = 720  # 24^2 + 12^2

    centers = {
        'nw': (QUAD, QUAD),   # 草地从右下凸向左上
        'ne': (0, QUAD),      # 草地从左下凸向右上
        'sw': (QUAD, 0),      # 草地从右上凸向左下
        'se': (0, 0),         # 草地从左上凸向右下
    }
    cx, cy = centers[corner]

    for y in range(QUAD):
        for x in range(QUAD):
            dx = x - cx
            dy = y - cy
            dist_sq = dx ** 2 + dy ** 2

            # 角度抖动 (草叶咬合感)
            angle = math.atan2(dy, dx)
            jitter = 1.5 * math.sin(angle * 10)
            eff_r_sq = R_SQ + 2 * 26.83 * jitter

            if dist_sq <= eff_r_sq:
                m[y][x] = 255
    return m

def gen_inner_corner(corner):
    """内凹圆弧: 泥土(黑)切入草地(白)
    端点精准交于两条外边的1/2(12px)处, R²=720
    """
    import math
    m = new_grid(255)
    R_SQ = 720

    centers = {
        'nw': (0, 0),               # 泥土从左上切入
        'ne': (QUAD, 0),            # 泥土从右上切入
        'sw': (0, QUAD),            # 泥土从左下切入
        'se': (QUAD, QUAD),         # 泥土从右下切入
    }
    cx, cy = centers[corner]

    for y in range(QUAD):
        for x in range(QUAD):
            dx = x - cx
            dy = y - cy
            dist_sq = dx ** 2 + dy ** 2

            angle = math.atan2(dy, dx)
            jitter = 1.5 * math.sin(angle * 10)
            eff_r_sq = R_SQ + 2 * 26.83 * jitter

            if dist_sq <= eff_r_sq:
                m[y][x] = 0
    return m


# ═══════════════════════════════════════════════
#  组装
# ═══════════════════════════════════════════════

def assemble(nw, ne, sw, se):
    result = [[0] * TILE for _ in range(TILE)]
    for y in range(QUAD):
        for x in range(QUAD):
            result[y][x] = nw[y][x]
            result[y][x + QUAD] = ne[y][x]
            result[y + QUAD][x] = sw[y][x]
            result[y + QUAD][x + QUAD] = se[y][x]
    return result


# ═══════════════════════════════════════════════
#  实例
# ═══════════════════════════════════════════════

SOLID = gen_solid()
EMPTY = gen_empty()

EDGE_N = gen_edge('n')
EDGE_S = gen_edge('s')
EDGE_W = gen_edge('w')
EDGE_E = gen_edge('e')

OUTER_NW = gen_outer_corner('nw')
OUTER_NE = gen_outer_corner('ne')
OUTER_SW = gen_outer_corner('sw')
OUTER_SE = gen_outer_corner('se')

INNER_NW = gen_inner_corner('nw')
INNER_NE = gen_inner_corner('ne')
INNER_SW = gen_inner_corner('sw')
INNER_SE = gen_inner_corner('se')


# ═══════════════════════════════════════════════
#  13 块 autotile
#  布局: NW | NE
#        SW | SE
#  白=地形A(中心/草地), 黑=地形B(外围/泥土)
# ═══════════════════════════════════════════════

TILES = {
    "center":            assemble(SOLID, SOLID, SOLID, SOLID),

    "edge-n":            assemble(EDGE_N, EDGE_N, SOLID, SOLID),
    "edge-s":            assemble(SOLID, SOLID, EDGE_S, EDGE_S),
    "edge-w":            assemble(EDGE_W, SOLID, EDGE_W, SOLID),
    "edge-e":            assemble(SOLID, EDGE_E, SOLID, EDGE_E),

    # 凸角: 外转角象限 + 两个直边过渡 + 一个纯色内部
    "corner-outer-nw":   assemble(OUTER_NW, EDGE_N,   EDGE_W,   SOLID),
    "corner-outer-ne":   assemble(EDGE_N,   OUTER_NE, SOLID,    EDGE_E),
    "corner-outer-sw":   assemble(EDGE_W,   SOLID,    OUTER_SW, EDGE_S),
    "corner-outer-se":   assemble(SOLID,    EDGE_E,   EDGE_S,   OUTER_SE),

    # 凹角: 内转角象限 + 三个纯色内部
    "corner-inner-nw":   assemble(INNER_NW, SOLID,    SOLID,    SOLID),
    "corner-inner-ne":   assemble(SOLID,    INNER_NE, SOLID,    SOLID),
    "corner-inner-sw":   assemble(SOLID,    SOLID,    INNER_SW, SOLID),
    "corner-inner-se":   assemble(SOLID,    SOLID,    SOLID,    INNER_SE),
}


# ═══════════════════════════════════════════════
#  合成 (含正确 Alpha 阴影)
# ═══════════════════════════════════════════════

def generate_autotile_set(tex_a_path, tex_b_path, output_dir, prefix):
    tex_a = Image.open(tex_a_path).convert("RGBA").resize((TILE, TILE), Image.NEAREST)
    tex_b = Image.open(tex_b_path).convert("RGBA").resize((TILE, TILE), Image.NEAREST)
    os.makedirs(output_dir, exist_ok=True)
    generated = []

    for tile_id, grid in TILES.items():
        mask = grid_to_image(grid, TILE)
        result = Image.composite(tex_a, tex_b, mask)

        # 阴影: mask偏移1px → subtract → alpha_composite叠加
        shadow_mask = ImageChops.offset(mask, 1, 1)
        shadow_area = ImageChops.subtract(shadow_mask, mask)

        shadow_layer = Image.new("RGBA", (TILE, TILE), (0, 0, 0, 0))
        shadow_overlay = Image.new("RGBA", (TILE, TILE), (0, 0, 0, 80))
        shadow_layer.paste(shadow_overlay, (0, 0), shadow_area)

        result = Image.alpha_composite(result, shadow_layer)

        out_path = os.path.join(output_dir, f"{prefix}-{tile_id}.png")
        result.save(out_path)
        generated.append(f"{prefix}-{tile_id}")

    return generated

def export_masks_preview(output_dir, scale=4):
    os.makedirs(output_dir, exist_ok=True)
    for tile_id, grid in TILES.items():
        img = grid_to_image(grid, TILE)
        if scale > 1:
            img = img.resize((TILE * scale, TILE * scale), Image.NEAREST)
        img.save(os.path.join(output_dir, f"mask-{tile_id}.png"))

    bases = {
        "solid": SOLID, "empty": EMPTY,
        "edge_n": EDGE_N, "edge_s": EDGE_S, "edge_w": EDGE_W, "edge_e": EDGE_E,
        "outer_nw": OUTER_NW, "outer_ne": OUTER_NE,
        "outer_sw": OUTER_SW, "outer_se": OUTER_SE,
        "inner_nw": INNER_NW, "inner_ne": INNER_NE,
        "inner_sw": INNER_SW, "inner_se": INNER_SE,
    }
    for name, grid in bases.items():
        save_grid(grid, os.path.join(output_dir, f"base-{name}.png"), scale)


if __name__ == "__main__":
    import sys
    if len(sys.argv) >= 2 and sys.argv[1] == "preview":
        export_masks_preview("out/v1_masks")
        print("Masks preview → out/v1_masks/")
    elif len(sys.argv) >= 5:
        result = generate_autotile_set(sys.argv[1], sys.argv[2], sys.argv[3], sys.argv[4])
        print(f"Generated {len(result)} autotiles → {sys.argv[3]}")
    else:
        print("Usage:")
        print("  python autotile_compose.py preview")
        print("  python autotile_compose.py <tex_a> <tex_b> <output_dir> <prefix>")
