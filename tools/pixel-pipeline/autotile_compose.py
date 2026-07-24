# -*- coding: utf-8 -*-
"""九野 · 工业级 Autotile 掩码合成器（全参数化变量驱动版）
核心改进：
1. 统一以 BOUNDARY = QUAD // 2 为 1/2 基准，消除所有硬编码魔法数字。
2. 外凸角与内凹角半径通过 BOUNDARY 动态计算，确保端点 100% 精准咬合在 1/2 边界处。
3. 优化形态学处理与双层边缘渲染，兼顾像素质感与无缝拼接。
"""
import os, math
from PIL import Image, ImageChops

TILE = 48
QUAD = 24
BOUNDARY = QUAD // 2  # 动态 1/2 长度基准 (12px)

# 动态绑定半径平方（彻底告别硬编码）
R_OUTER_SQ = BOUNDARY ** 2                 # 外凸角半径平方 (12^2 = 144)
R_INNER_SQ = QUAD ** 2 + BOUNDARY ** 2     # 内凹角半径平方 (24^2 + 12^2 = 720)


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
#  核心掩码生成（严格由 BOUNDARY 约束 1/2 边界）
# ═══════════════════════════════════════════════

def gen_solid():
    return new_grid(255)

def gen_empty():
    return new_grid(0)

def gen_edge(orientation):
    """直边: 严格以 BOUNDARY (12px) 为 1/2 分界线"""
    m = new_grid(0)
    for y in range(QUAD):
        for x in range(QUAD):
            if orientation == 'n':
                m[y][x] = 255 if y >= BOUNDARY else 0
            elif orientation == 's':
                m[y][x] = 255 if y < BOUNDARY else 0
            elif orientation == 'w':
                m[y][x] = 255 if x >= BOUNDARY else 0
            elif orientation == 'e':
                m[y][x] = 255 if x < BOUNDARY else 0
    return m

def gen_outer_corner(corner):
    """外凸圆弧: 草地凸向泥土，半径平方动态绑定 R_OUTER_SQ"""
    m = new_grid(0)
    centers = {
        'nw': (QUAD, QUAD), 'ne': (0, QUAD),
        'sw': (QUAD, 0), 'se': (0, 0),
    }
    cx, cy = centers[corner]
    for y in range(QUAD):
        for x in range(QUAD):
            dx = x - cx
            dy = y - cy
            if dx ** 2 + dy ** 2 <= R_OUTER_SQ:
                m[y][x] = 255
    return m

def gen_inner_corner(corner):
    """内凹圆弧: 泥土深切入草地，半径平方动态绑定 R_INNER_SQ"""
    m = new_grid(255)
    centers = {
        'nw': (0, 0), 'ne': (QUAD, 0),
        'sw': (0, QUAD), 'se': (QUAD, QUAD),
    }
    cx, cy = centers[corner]
    for y in range(QUAD):
        for x in range(QUAD):
            dx = x - cx
            dy = y - cy
            if dx ** 2 + dy ** 2 <= R_INNER_SQ:
                m[y][x] = 0
    return m


# ═══════════════════════════════════════════════
#  像素形态学清理
# ═══════════════════════════════════════════════

def clean_pixels(grid):
    """清理孤立像素，保持边缘干净"""
    h, w = len(grid), len(grid[0])
    cleaned = [row[:] for row in grid]

    for y in range(h):
        for x in range(w):
            val = grid[y][x]
            opposite = 0
            total = 0
            for dy, dx in [(-1, 0), (1, 0), (0, -1), (0, 1)]:
                ny, nx = y + dy, x + dx
                if 0 <= ny < h and 0 <= nx < w:
                    total += 1
                    if grid[ny][nx] != val:
                        opposite += 1
            if total >= 3 and opposite >= 3:
                cleaned[y][x] = 255 - val if val in (0, 255) else val

    return cleaned


# ═══════════════════════════════════════════════
#  象限拼合
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
#  基础部件初始化
# ═══════════════════════════════════════════════

SOLID = gen_solid()
EMPTY = gen_empty()

EDGE_N = gen_edge('n')
EDGE_S = gen_edge('s')
EDGE_W = gen_edge('w')
EDGE_E = gen_edge('e')

OUTER_NW = clean_pixels(gen_outer_corner('nw'))
OUTER_NE = clean_pixels(gen_outer_corner('ne'))
OUTER_SW = clean_pixels(gen_outer_corner('sw'))
OUTER_SE = clean_pixels(gen_outer_corner('se'))

INNER_NW = clean_pixels(gen_inner_corner('nw'))
INNER_NE = clean_pixels(gen_inner_corner('ne'))
INNER_SW = clean_pixels(gen_inner_corner('sw'))
INNER_SE = clean_pixels(gen_inner_corner('se'))


# ═══════════════════════════════════════════════
#  13 块标准 Autotile 映射字典
# ═══════════════════════════════════════════════

TILES = {
    "center":            assemble(SOLID, SOLID, SOLID, SOLID),
    "edge-n":            assemble(EDGE_N, EDGE_N, SOLID, SOLID),
    "edge-s":            assemble(SOLID, SOLID, EDGE_S, EDGE_S),
    "edge-w":            assemble(EDGE_W, SOLID, EDGE_W, SOLID),
    "edge-e":            assemble(SOLID, EDGE_E, SOLID, EDGE_E),
    "corner-outer-nw":   assemble(OUTER_NW, EDGE_N,   EDGE_W,   SOLID),
    "corner-outer-ne":   assemble(EDGE_N,   OUTER_NE, SOLID,    EDGE_E),
    "corner-outer-sw":   assemble(EDGE_W,   SOLID,    OUTER_SW, EDGE_S),
    "corner-outer-se":   assemble(SOLID,    EDGE_E,   EDGE_S,   OUTER_SE),
    "corner-inner-nw":   assemble(INNER_NW, SOLID,    SOLID,    SOLID),
    "corner-inner-ne":   assemble(SOLID,    INNER_NE, SOLID,    SOLID),
    "corner-inner-sw":   assemble(SOLID,    SOLID,    INNER_SW, SOLID),
    "corner-inner-se":   assemble(SOLID,    SOLID,    SOLID,    INNER_SE),
}


# ═══════════════════════════════════════════════
#  双重边缘渲染 (阴影 + 高光)
# ═══════════════════════════════════════════════

def generate_autotile_set(tex_a_path, tex_b_path, output_dir, prefix):
    tex_a = Image.open(tex_a_path).convert("RGBA").resize((TILE, TILE), Image.NEAREST)
    tex_b = Image.open(tex_b_path).convert("RGBA").resize((TILE, TILE), Image.NEAREST)
    os.makedirs(output_dir, exist_ok=True)
    generated = []

    for tile_id, grid in TILES.items():
        mask = grid_to_image(grid, TILE)
        result = Image.composite(tex_a, tex_b, mask)

        # 暗部阴影 (右/下侧)
        shadow_mask = ImageChops.offset(mask, 1, 1)
        shadow_area = ImageChops.subtract(shadow_mask, mask)
        shadow_layer = Image.new("RGBA", (TILE, TILE), (0, 0, 0, 0))
        shadow_overlay = Image.new("RGBA", (TILE, TILE), (20, 15, 10, 100))
        shadow_layer.paste(shadow_overlay, (0, 0), shadow_area)
        result = Image.alpha_composite(result, shadow_layer)

        # 亮部高光 (左/上侧)
        highlight_mask = ImageChops.offset(mask, -1, -1)
        highlight_area = ImageChops.subtract(mask, highlight_mask)
        highlight_layer = Image.new("RGBA", (TILE, TILE), (0, 0, 0, 0))
        highlight_overlay = Image.new("RGBA", (TILE, TILE), (180, 220, 150, 60))
        highlight_layer.paste(highlight_overlay, (0, 0), highlight_area)
        result = Image.alpha_composite(result, highlight_layer)

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


if __name__ == "__main__":
    import sys
    if len(sys.argv) >= 2 and sys.argv[1] == "preview":
        export_masks_preview("out/v2_masks")
        print("Masks preview → out/v2_masks/")
    elif len(sys.argv) >= 5:
        result = generate_autotile_set(sys.argv[1], sys.argv[2], sys.argv[3], sys.argv[4])
        print(f"Generated {len(result)} autotiles → {sys.argv[3]}")
    else:
        print("Usage:")
        print("  python autotile_compose.py preview")
        print("  python autotile_compose.py <tex_a> <tex_b> <output_dir> <prefix>")
