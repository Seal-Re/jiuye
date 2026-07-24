# -*- coding: utf-8 -*-
"""九野 · 工业级 Autotile 掩码合成器 v7
彻底重写: 明确的像素阶梯圆弧, 无脏像素, 边界100%对齐。

外角 NW (左上凸圆弧): 草地从左上角凸出, 圆弧向右下弯曲。
  白区 = 左上角, 沿对角线递减。
内角 NW (左上凹圆弧): 泥土从右下角切入草地。
  黑区 = 右下角小圆弧, 其余全白。

24×24 子象限组装 48×48。
"""
import os
from PIL import Image

TILE = 48
QUAD = 24


def new_mask(fill=0):
    return Image.new("L", (QUAD, QUAD), fill)


def mask_solid():
    return new_mask(255)

def mask_empty():
    return new_mask(0)

def mask_edge_h(top_white=True):
    """水平边: 上白下黑 (top_white=True) 或上黑下白"""
    m = new_mask(0)
    px = m.load()
    boundary = QUAD // 2  # 12
    for y in range(QUAD):
        for x in range(QUAD):
            if top_white:
                if y < boundary:
                    px[x, y] = 255
                elif y == boundary:
                    px[x, y] = 255 if (x % 2 == 0) else 0
            else:
                if y >= boundary:
                    px[x, y] = 255
                elif y == boundary - 1:
                    px[x, y] = 255 if (x % 2 == 0) else 0
    return m

def mask_edge_v(left_white=True):
    """垂直边: 左白右黑 (left_white=True) 或左黑右白"""
    m = new_mask(0)
    px = m.load()
    boundary = QUAD // 2
    for y in range(QUAD):
        for x in range(QUAD):
            if left_white:
                if x < boundary:
                    px[x, y] = 255
                elif x == boundary:
                    px[x, y] = 255 if (y % 2 == 0) else 0
            else:
                if x >= boundary:
                    px[x, y] = 255
                elif x == boundary - 1:
                    px[x, y] = 255 if (y % 2 == 0) else 0
    return m

def mask_outer_corner():
    """外凸圆弧 NW: 左上角白色凸圆弧, 圆心在右下角外侧。
    白区 = 距右下角 ≤ R 的像素。
    用像素阶梯: 从左上角开始, 每行白色宽度递减。
    """
    m = new_mask(0)
    px = m.load()
    R = 14  # 圆弧半径 (24×24 子象限内)
    cx, cy = QUAD, QUAD  # 圆心 = 右下角

    for y in range(QUAD):
        for x in range(QUAD):
            dx = cx - x - 1  # 距右边的距离
            dy = cy - y - 1  # 距下边的距离
            dist = (dx * dx + dy * dy) ** 0.5
            if dist <= R:
                px[x, y] = 255
            # 圆弧边缘: 1px 抖动
            elif dist <= R + 1 and (x + y) % 2 == 0:
                px[x, y] = 255
    return m

def mask_inner_corner():
    """内凹圆弧 NW: 右下角黑色凹圆弧切入白色区域。
    白区 = 全白, 除右下角距圆心 ≤ R 的黑色区域。
    """
    m = new_mask(255)
    px = m.load()
    R = 10
    cx, cy = QUAD, QUAD

    for y in range(QUAD):
        for x in range(QUAD):
            dx = cx - x - 1
            dy = cy - y - 1
            dist = (dx * dx + dy * dy) ** 0.5
            if dist <= R:
                px[x, y] = 0
            # 圆弧边缘: 1px 抖动
            elif dist <= R + 1 and (x + y) % 2 == 1:
                px[x, y] = 0
    return m


# ═══════════════════════════════════════════════
#  变换
# ═══════════════════════════════════════════════

def rot90(m): return m.rotate(90, expand=True)
def rot180(m): return m.rotate(180, expand=True)
def rot270(m): return m.rotate(270, expand=True)
def flip_h(m): return m.transpose(Image.FLIP_LEFT_RIGHT)
def flip_v(m): return m.transpose(Image.FLIP_TOP_BOTTOM)


# ═══════════════════════════════════════════════
#  48×48 组装
# ═══════════════════════════════════════════════

def assemble(nw, ne, sw, se):
    result = Image.new("L", (TILE, TILE), 0)
    result.paste(nw, (0, 0))
    result.paste(ne, (QUAD, 0))
    result.paste(sw, (0, QUAD))
    result.paste(se, (QUAD, QUAD))
    return result


# ═══════════════════════════════════════════════
#  基础掩码实例
# ═══════════════════════════════════════════════

SOLID = mask_solid()
EMPTY = mask_empty()

# 边缘 (白区朝向中心)
EDGE_N = mask_edge_h(top_white=False)   # 上黑下白 (白在南=中心侧)
EDGE_S = mask_edge_h(top_white=True)    # 上白下黑 (白在北=中心侧)
EDGE_W = mask_edge_v(left_white=False)  # 左黑右白 (白在东=中心侧)
EDGE_E = mask_edge_v(left_white=True)   # 左白右黑 (白在西=中心侧)

# 外角 (凸圆弧朝外)
OUTER_NW = mask_outer_corner()          # 左上凸
OUTER_NE = rot90(OUTER_NW)              # 右上凸
OUTER_SE = rot180(OUTER_NW)             # 右下凸
OUTER_SW = rot270(OUTER_NW)             # 左下凸

# 内角 (凹圆弧朝内)
INNER_NW = mask_inner_corner()          # 左上凹 (黑切入)
INNER_NE = rot90(INNER_NW)
INNER_SE = rot180(INNER_NW)
INNER_SW = rot270(INNER_NW)


# ═══════════════════════════════════════════════
#  13 块 autotile 定义
#  布局: NW | NE
#        SW | SE
#  白 = 地形A (中心), 黑 = 地形B (外围)
# ═══════════════════════════════════════════════

TILES = {
    # center: 全白 (纯地形A)
    "center":            assemble(SOLID, SOLID, SOLID, SOLID),

    # edge: 一半白一半黑 (白朝中心)
    # edge-n = 北边: 上黑下白 (上面是外围, 下面是中心)
    "edge-n":            assemble(EMPTY, EMPTY, SOLID, SOLID),
    "edge-s":            assemble(SOLID, SOLID, EMPTY, EMPTY),
    "edge-w":            assemble(EMPTY, SOLID, EMPTY, SOLID),
    "edge-e":            assemble(SOLID, EMPTY, SOLID, EMPTY),

    # outer corner: 1/4凸圆弧 (地形A凸向外围)
    # NW外角: 左上角是外围, 草地从右下凸出 → SE象限=圆弧
    # 正确理解: outer-NW = 左上角缺一块(黑), 圆弧在NW
    # NW象限=外角圆弧(白在右下), NE=水平边, SW=垂直边, SE=纯白
    "corner-outer-nw":   assemble(OUTER_NW, EDGE_N,   EDGE_W,   SOLID),
    "corner-outer-ne":   assemble(EDGE_N,   OUTER_NE, SOLID,    EDGE_E),
    "corner-outer-sw":   assemble(EDGE_W,   SOLID,    OUTER_SW, EDGE_S),
    "corner-outer-se":   assemble(SOLID,    EDGE_E,   EDGE_S,   OUTER_SE),

    # inner corner: 3/4白 + 1/4凹圆弧 (地形B切入地形A)
    # NW内角: 右下角被切入 → SE象限=内角圆弧(黑在右下)
    # NW=纯白, NE=纯白, SW=纯白, SE=内角
    "corner-inner-nw":   assemble(SOLID, SOLID, SOLID, INNER_NW),
    "corner-inner-ne":   assemble(SOLID, SOLID, INNER_NE, SOLID),
    "corner-inner-sw":   assemble(SOLID, INNER_SW, SOLID, SOLID),
    "corner-inner-se":   assemble(INNER_SE, SOLID, SOLID, SOLID),
}


# ═══════════════════════════════════════════════
#  合成 + 预览
# ═══════════════════════════════════════════════

def generate_autotile_set(tex_a_path, tex_b_path, output_dir, prefix):
    tex_a = Image.open(tex_a_path).convert("RGBA").resize((TILE, TILE), Image.NEAREST)
    tex_b = Image.open(tex_b_path).convert("RGBA").resize((TILE, TILE), Image.NEAREST)
    os.makedirs(output_dir, exist_ok=True)
    generated = []
    for tile_id, mask in TILES.items():
        result = Image.composite(tex_a, tex_b, mask)
        out_path = os.path.join(output_dir, f"{prefix}-{tile_id}.png")
        result.save(out_path)
        generated.append(f"{prefix}-{tile_id}")
    return generated

def export_masks_preview(output_dir):
    os.makedirs(output_dir, exist_ok=True)
    for tile_id, mask in TILES.items():
        # 放大4倍便于查看
        big = mask.resize((TILE * 4, TILE * 4), Image.NEAREST)
        big.save(os.path.join(output_dir, f"mask-{tile_id}.png"))
    for name, m in [("solid", SOLID), ("empty", EMPTY),
                     ("edge_n", EDGE_N), ("edge_s", EDGE_S),
                     ("edge_w", EDGE_W), ("edge_e", EDGE_E),
                     ("outer_nw", OUTER_NW), ("outer_ne", OUTER_NE),
                     ("outer_sw", OUTER_SW), ("outer_se", OUTER_SE),
                     ("inner_nw", INNER_NW), ("inner_ne", INNER_NE),
                     ("inner_sw", INNER_SW), ("inner_se", INNER_SE)]:
        big = m.resize((QUAD * 4, QUAD * 4), Image.NEAREST)
        big.save(os.path.join(output_dir, f"base-{name}.png"))


if __name__ == "__main__":
    import sys
    if len(sys.argv) >= 2 and sys.argv[1] == "preview":
        export_masks_preview("out/v1_masks")
        print("Masks preview → out/v1_masks/ (4x放大)")
    elif len(sys.argv) >= 5:
        result = generate_autotile_set(sys.argv[1], sys.argv[2], sys.argv[3], sys.argv[4])
        print(f"Generated {len(result)} autotiles → {sys.argv[3]}")
    else:
        print("Usage:")
        print("  python autotile_compose.py preview")
        print("  python autotile_compose.py <tex_a> <tex_b> <output_dir> <prefix>")
