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
    """外凸圆弧: 草地(白)从内部角凸向外围角
    圆心 = 草地内部接壤角, R=12 与直边无缝衔接
    nw: 草地在右下, 圆心=(QUAD-1,QUAD-1)
    ne: 草地在左下, 圆心=(0,QUAD-1)
    sw: 草地在右上, 圆心=(QUAD-1,0)
    se: 草地在左上, 圆心=(0,0)
    """
    m = new_grid(0)
    R = 12
    centers = {
        'nw': (QUAD - 1, QUAD - 1),
        'ne': (0, QUAD - 1),
        'sw': (QUAD - 1, 0),
        'se': (0, 0),
    }
    cx, cy = centers[corner]
    for y in range(QUAD):
        for x in range(QUAD):
            dist = ((x - cx) ** 2 + (y - cy) ** 2) ** 0.5
            if dist <= R:
                m[y][x] = 255
    return m

def gen_inner_corner(corner):
    """内凹圆弧: 泥土(黑)切入草地(白)
    圆心 = 外围角, R=12 与直边无缝衔接
    nw: 泥土从左上切入, 圆心=(0,0)
    ne: 泥土从右上切入, 圆心=(QUAD-1,0)
    sw: 泥土从左下切入, 圆心=(0,QUAD-1)
    se: 泥土从右下切入, 圆心=(QUAD-1,QUAD-1)
    """
    m = new_grid(255)
    R = 12
    centers = {
        'nw': (0, 0),
        'ne': (QUAD - 1, 0),
        'sw': (0, QUAD - 1),
        'se': (QUAD - 1, QUAD - 1),
    }
    cx, cy = centers[corner]
    for y in range(QUAD):
        for x in range(QUAD):
            dist = ((x - cx) ** 2 + (y - cy) ** 2) ** 0.5
            if dist <= R:
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
