# -*- coding: utf-8 -*-
"""九野 · 工业级 Autotile 掩码合成器 v9
修正: 圆弧方向 + 边缘用抖动掩码 + 孤立噪点清理 + 阴影层。

核心逻辑:
  白(255) = 地形A (草地, 中心)
  黑(0)   = 地形B (泥土, 外围)

  外角 NW: 左上=白(草地凸出), 右下=黑(外围). 圆心=(0,0), R内白外黑.
  内角 NW: 左上=黑(外围切入), 右下=白(草地). 圆心=(0,0), R内黑外白.
  边缘 N:  上=黑(外围), 下=白(中心). 水平线.
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
#  基础掩码 (24×24, 纯数学)
# ═══════════════════════════════════════════════

def gen_solid():
    return new_grid(255)

def gen_empty():
    return new_grid(0)

def gen_edge(orientation):
    """有机锯齿直边
    n: 上黑下白  s: 上白下黑  w: 左黑右白  e: 左白右黑
    """
    m = new_grid(0)
    boundary = QUAD // 2  # 12
    # 有机抖动: 每列偏移 [-1, 0, 1] 循环
    jitter = [0, 1, -1, 0, 1, -1, 0, 1, -1, 0, 1, -1,
              0, 1, -1, 0, 1, -1, 0, 1, -1, 0, 1, -1]

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
    """外凸圆弧: 地形A(白)凸向地形B(黑)
    nw外角: 草地在右下凸出, 圆心=(0,0)左上角, 距圆心≤R=白
    ne外角: 草地在左下凸出, 圆心=(QUAD-1,0)右上角
    sw外角: 草地在右上凸出, 圆心=(0,QUAD-1)左下角
    se外角: 草地在左上凸出, 圆心=(QUAD-1,QUAD-1)右下角

    即: 圆心=凸角(外围角), 白色从对角(草地侧)凸出
    """
    m = new_grid(0)
    R = 17
    # 圆心 = 外围角 (草地从对角凸出)
    centers = {
        'nw': (0, 0),           # 左上外围 → 草地从右下凸
        'ne': (QUAD - 1, 0),    # 右上外围 → 草地从左下凸
        'sw': (0, QUAD - 1),    # 左下外围 → 草地从右上凸
        'se': (QUAD - 1, QUAD - 1), # 右下外围 → 草地从左上凸
    }
    cx, cy = centers[corner]
    for y in range(QUAD):
        for x in range(QUAD):
            dist = ((x - cx) ** 2 + (y - cy) ** 2) ** 0.5
            if dist <= R:
                m[y][x] = 255
            elif dist <= R + 1 and (x * 3 + y * 5) % 3 != 0:
                m[y][x] = 255
    return m

def gen_inner_corner(corner):
    """内凹圆弧: 地形B(黑)切入地形A(白)
    nw: 左上黑切入, 圆心(0,0), 距圆心≤R=黑
    """
    m = new_grid(255)
    R = 11
    centers = {
        'nw': (0, 0), 'ne': (QUAD - 1, 0),
        'sw': (0, QUAD - 1), 'se': (QUAD - 1, QUAD - 1),
    }
    cx, cy = centers[corner]
    for y in range(QUAD):
        for x in range(QUAD):
            dist = ((x - cx) ** 2 + (y - cy) ** 2) ** 0.5
            if dist <= R:
                m[y][x] = 0
            elif dist <= R + 1 and (x * 3 + y * 5) % 3 == 0:
                m[y][x] = 0
    return m


def clean_isolated_pixels(grid):
    """清理孤立1×1噪点: 如果一个像素与8邻域都不同, 则抹平"""
    h, w = len(grid), len(grid[0])
    cleaned = [row[:] for row in grid]
    for y in range(h):
        for x in range(w):
            val = grid[y][x]
            neighbors = []
            for dy in (-1, 0, 1):
                for dx in (-1, 0, 1):
                    if dy == 0 and dx == 0:
                        continue
                    ny, nx = y + dy, x + dx
                    if 0 <= ny < h and 0 <= nx < w:
                        neighbors.append(grid[ny][nx])
            # 如果所有邻居都和当前像素相反 → 孤立点
            if neighbors and all(n != val for n in neighbors):
                # 取多数邻居的值
                cleaned[y][x] = 255 if sum(n > 128 for n in neighbors) > len(neighbors) // 2 else 0
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
#  实例
# ═══════════════════════════════════════════════

SOLID = gen_solid()
EMPTY = gen_empty()

EDGE_N = gen_edge('n')  # 上黑下白
EDGE_S = gen_edge('s')  # 上白下黑
EDGE_W = gen_edge('w')  # 左黑右白
EDGE_E = gen_edge('e')  # 左白右黑

OUTER_NW = clean_isolated_pixels(gen_outer_corner('nw'))
OUTER_NE = clean_isolated_pixels(gen_outer_corner('ne'))
OUTER_SW = clean_isolated_pixels(gen_outer_corner('sw'))
OUTER_SE = clean_isolated_pixels(gen_outer_corner('se'))

INNER_NW = clean_isolated_pixels(gen_inner_corner('nw'))
INNER_NE = clean_isolated_pixels(gen_inner_corner('ne'))
INNER_SW = clean_isolated_pixels(gen_inner_corner('sw'))
INNER_SE = clean_isolated_pixels(gen_inner_corner('se'))


# ═══════════════════════════════════════════════
#  13 块 autotile
#  布局: NW | NE
#        SW | SE
#  白=地形A(中心/草地), 黑=地形B(外围/泥土)
# ═══════════════════════════════════════════════

TILES = {
    # center: 全白
    "center":            assemble(SOLID, SOLID, SOLID, SOLID),

    # edge: 直边, 白朝中心
    # edge-n: 北边=上黑下白 (北是外围, 南是中心)
    "edge-n":            assemble(EDGE_N, EDGE_N, SOLID, SOLID),
    "edge-s":            assemble(SOLID, SOLID, EDGE_S, EDGE_S),
    "edge-w":            assemble(EDGE_W, SOLID, EDGE_W, SOLID),
    "edge-e":            assemble(SOLID, EDGE_E, SOLID, EDGE_E),

    # outer corner: 外围在角, 草地从对角凸出
    # NE外角: 右上是外围, 草地从左下凸 →
    #   NW=EMPTY(外围) NE=EMPTY(外围)
    #   SW=SOLID(草地) SE=OUTER_NE(圆弧过渡)
    # 圆弧象限 = 凸角的对角象限
    "corner-outer-nw":   assemble(OUTER_NW, EMPTY,     EMPTY,     SOLID),
    "corner-outer-ne":   assemble(EMPTY,    OUTER_NE,  SOLID,     EMPTY),
    "corner-outer-sw":   assemble(EMPTY,    SOLID,     OUTER_SW,  EMPTY),
    "corner-outer-se":   assemble(SOLID,    EMPTY,     EMPTY,     OUTER_SE),

    # inner corner: 外围切入中心, 凹角在命名方向
    # NW内角: 左上=外围切入(黑), 其余=草地(白) → NW象限=内角
    "corner-inner-nw":   assemble(INNER_NW, SOLID,    SOLID,    SOLID),
    "corner-inner-ne":   assemble(SOLID,    INNER_NE, SOLID,    SOLID),
    "corner-inner-sw":   assemble(SOLID,    SOLID,    INNER_SW, SOLID),
    "corner-inner-se":   assemble(SOLID,    SOLID,    SOLID,    INNER_SE),
}


# ═══════════════════════════════════════════════
#  合成 (含阴影层)
# ═══════════════════════════════════════════════

def generate_autotile_set(tex_a_path, tex_b_path, output_dir, prefix):
    """合成 autotile: 地形A + 地形B + 掩码 + 阴影"""
    tex_a = Image.open(tex_a_path).convert("RGBA").resize((TILE, TILE), Image.NEAREST)
    tex_b = Image.open(tex_b_path).convert("RGBA").resize((TILE, TILE), Image.NEAREST)
    os.makedirs(output_dir, exist_ok=True)
    generated = []

    for tile_id, grid in TILES.items():
        mask = grid_to_image(grid, TILE)
        # 基本合成: 白区=地形A, 黑区=地形B
        result = Image.composite(tex_a, tex_b, mask)

        # 阴影层: mask 偏移1px → 提取阴影区 → 叠加暗色
        shadow_mask = ImageChops.offset(mask, 1, 1)  # 往右下偏移
        shadow_area = ImageChops.subtract(shadow_mask, mask)  # 阴影=偏移后减原mask
        # 在阴影区叠加暗色 (半透明黑)
        shadow_overlay = Image.new("RGBA", (TILE, TILE), (0, 0, 0, 80))
        result = Image.composite(shadow_overlay, result, shadow_area)

        out_path = os.path.join(output_dir, f"{prefix}-{tile_id}.png")
        result.save(out_path)
        generated.append(f"{prefix}-{tile_id}")

    return generated

def export_masks_preview(output_dir, scale=4):
    os.makedirs(output_dir, exist_ok=True)
    for tile_id, grid in TILES.items():
        # grid 已经是 48×48, 直接转图
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
