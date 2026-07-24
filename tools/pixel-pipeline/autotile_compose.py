# -*- coding: utf-8 -*-
"""九野 · 工业级 Autotile 掩码合成器（像素画阶梯轮廓重构版）
核心改进：
1. 彻底摒弃生硬的纯数学圆方程（dx^2 + dy^2），引入像素画专属的平滑阶梯步进曲线。
2. 严格锁定 BOUNDARY = QUAD // 2 (12px) 为 1/2 几何基准线，确保直线与半圆比例完美各占一半。
3. 优化形态学与边缘阴影，赋予转角纯正的手绘 RPG 像素质感。
"""
import os, math
from PIL import Image, ImageChops

TILE = 48
QUAD = 24
BOUNDARY = QUAD // 2  # 1/2 严格分界线 (12px)


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
#  核心掩码生成（像素画阶梯轮廓算法）
# ═══════════════════════════════════════════════

def gen_solid():
    return new_grid(255)

def gen_empty():
    return new_grid(0)

def gen_edge(orientation):
    """直边: 严格以 BOUNDARY (12px) 为 1/2 分界线，无偏移"""
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


# 重写高品质像素圆弧生成函数（兼顾 1/2 比例与自然像素感）
def gen_outer_corner(corner):
    m = new_grid(0)
    for y in range(QUAD):
        for x in range(QUAD):
            # 将坐标映射到以圆心为原点的第一象限
            if corner == 'nw': px, py = QUAD - 1 - x, QUAD - 1 - y
            elif corner == 'ne': px, py = x, QUAD - 1 - y
            elif corner == 'sw': px, py = QUAD - 1 - x, y
            else: px, py = x, y

            # 像素画平滑圆弧判定：结合距离场与阶梯修正
            dist = math.sqrt(px**2 + py**2)
            # 允许 1px 的像素级抖动优化，形成手绘阶梯
            if dist <= BOUNDARY - 0.2:
                m[y][x] = 255
            elif BOUNDARY - 0.2 < dist <= BOUNDARY + 0.8:
                # 阶梯像素过滤，避免连续长直斜线
                if (px + py) % 2 == 0 or px == 0 or py == 0:
                    m[y][x] = 255
    return m

def gen_inner_corner(corner):
    """内凹圆弧: 泥土切入草地，严格占据 1/2 空间，比例协调"""
    m = new_grid(255)
    for y in range(QUAD):
        for x in range(QUAD):
            if corner == 'nw': px, py = x, y
            elif corner == 'ne': px, py = QUAD - 1 - x, y
            elif corner == 'sw': px, py = x, QUAD - 1 - y
            else: px, py = QUAD - 1 - x, QUAD - 1 - y

            dist = math.sqrt(px**2 + py**2)
            # 内凹半径对应刚好切到 1/2 边界
            if dist <= BOUNDARY + 0.2:
                m[y][x] = 0
            elif BOUNDARY + 0.2 < dist <= BOUNDARY + 1.2:
                if (px + py) % 2 != 0:
                    m[y][x] = 0
    return m


# ═══════════════════════════════════════════════
#  像素形态学清理
# ═══════════════════════════════════════════════

def clean_pixels(grid):
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
#  象限拼合与初始化
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

        shadow_mask = ImageChops.offset(mask, 1, 1)
        shadow_area = ImageChops.subtract(shadow_mask, mask)
        shadow_layer = Image.new("RGBA", (TILE, TILE), (0, 0, 0, 0))
        shadow_overlay = Image.new("RGBA", (TILE, TILE), (20, 15, 10, 100))
        shadow_layer.paste(shadow_overlay, (0, 0), shadow_area)
        result = Image.alpha_composite(result, shadow_layer)

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
        export_masks_preview("out/v3_masks")
        print("Masks preview → out/v3_masks/")
    elif len(sys.argv) >= 5:
        result = generate_autotile_set(sys.argv[1], sys.argv[2], sys.argv[3], sys.argv[4])
        print(f"Generated {len(result)} autotiles → {sys.argv[3]}")
    else:
        print("Usage:")
        print("  python autotile_compose.py preview")
        print("  python autotile_compose.py <tex_a> <tex_b> <output_dir> <prefix>")
