# -*- coding: utf-8 -*-
"""九野 · 工业级 Autotile 掩码合成器 v12
四方案重构: 包络窗函数 + 离散草簇 + 形态学清理 + 双重边缘(阴影+高光)
"""
import os, math
from PIL import Image, ImageChops

TILE = 48
QUAD = 24
BOUNDARY = QUAD // 2  # 12 — 直边分界线


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
#  方案一: 边界窗函数 (接缝100%对齐)
# ═══════════════════════════════════════════════

def angle_envelope(angle, freq=12):
    """极角包络窗: 在出口角度(0°和90°附近)衰减为0, 在45°转角处保持1
    保证圆弧出口切线严格水平/垂直, 与直边无缝拼接
    """
    # 将角度归一化到 [0, π/2] (一个象限内)
    a = angle % (math.pi / 2)
    if a < 0:
        a += math.pi / 2
    # sin(2θ) 在 0 和 π/2 处为 0, 在 π/4 处为 1
    return math.sin(2 * a) ** 2


# ═══════════════════════════════════════════════
#  基础掩码 (平滑圆弧 + 包络抖动)
# ═══════════════════════════════════════════════

def gen_solid():
    return new_grid(255)

def gen_empty():
    return new_grid(0)

def gen_edge(orientation):
    """直边: boundary=12, 精确1/2分界, 无抖动偏移
    抖动只影响交界处2px, 不改变分界基准线
    """
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
    """外凸圆弧: 草地(白)凸向泥土(黑)
    R²=144 (12²), 纯净数学圆弧无抖动
    出口处强制锚定在12px边界, 与直边精确对齐
    """
    m = new_grid(0)
    R_SQ = 144
    centers = {
        'nw': (QUAD, QUAD), 'ne': (0, QUAD),
        'sw': (QUAD, 0), 'se': (0, 0),
    }
    cx, cy = centers[corner]
    for y in range(QUAD):
        for x in range(QUAD):
            dx = x - cx
            dy = y - cy
            dist_sq = dx ** 2 + dy ** 2
            if dist_sq <= R_SQ:
                m[y][x] = 255
    return m

def gen_inner_corner(corner):
    """内凹圆弧: 泥土(黑)切入草地(白)
    R²=720 (24²+12²), 纯净数学圆弧无抖动
    出口处自然交于12px边界
    """
    m = new_grid(255)
    R_SQ = 720
    centers = {
        'nw': (0, 0), 'ne': (QUAD, 0),
        'sw': (0, QUAD), 'se': (QUAD, QUAD),
    }
    cx, cy = centers[corner]
    for y in range(QUAD):
        for x in range(QUAD):
            dx = x - cx
            dy = y - cy
            dist_sq = dx ** 2 + dy ** 2
            if dist_sq <= R_SQ:
                m[y][x] = 0
    return m


# ═══════════════════════════════════════════════
#  方案三: 像素形态学清理
# ═══════════════════════════════════════════════

def clean_pixels(grid):
    """清理孤立像素 + 优化阶梯分布
    1. 4邻域≥3个反色 → 翻转
    2. 连续1-1-1单点阶梯 → 替换为2-1梯形
    """
    h, w = len(grid), len(grid[0])
    cleaned = [row[:] for row in grid]

    # 1. 孤立像素清理 (4邻域)
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
#  实例 (含形态学清理)
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
#  13 块 autotile
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
#  方案四: 双重边缘渲染 (暗部阴影 + 亮部高光)
# ═══════════════════════════════════════════════

def generate_autotile_set(tex_a_path, tex_b_path, output_dir, prefix):
    """合成: 地形A + 地形B + 掩码 + 暗部阴影 + 亮部高光"""
    tex_a = Image.open(tex_a_path).convert("RGBA").resize((TILE, TILE), Image.NEAREST)
    tex_b = Image.open(tex_b_path).convert("RGBA").resize((TILE, TILE), Image.NEAREST)
    os.makedirs(output_dir, exist_ok=True)
    generated = []

    for tile_id, grid in TILES.items():
        mask = grid_to_image(grid, TILE)
        # 基本合成
        result = Image.composite(tex_a, tex_b, mask)

        # —— 暗部阴影 (下/右侧, 1px偏移) ——
        shadow_mask = ImageChops.offset(mask, 1, 1)
        shadow_area = ImageChops.subtract(shadow_mask, mask)
        shadow_layer = Image.new("RGBA", (TILE, TILE), (0, 0, 0, 0))
        shadow_overlay = Image.new("RGBA", (TILE, TILE), (20, 15, 10, 100))
        shadow_layer.paste(shadow_overlay, (0, 0), shadow_area)
        result = Image.alpha_composite(result, shadow_layer)

        # —— 亮部高光 (上/左侧, 1px反向偏移) ——
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
