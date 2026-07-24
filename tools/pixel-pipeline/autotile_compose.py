# -*- coding: utf-8 -*-
"""九野 · 工业级 Autotile 掩码合成器 v6
24×24 子象限拆解法: 5 张基础掩码 → 镜像/旋转 → 13 块 48×48 autotile

基础掩码 (24×24):
  solid        全白 (纯地形A)
  empty        全黑 (纯地形B)
  edge_h       水平边 (上半白下半黑, 交界2px抖动)
  outer_corner 外凸圆弧 (左上1/4圆, R=10px阶梯)
  inner_corner 内凹圆弧 (左上3/4圆, R=10px阶梯)

48×48 组合逻辑 (4象限 NW/NE/SW/SE):
  center            = solid × 4
  edge-n            = empty+empty / solid+solid
  edge-s            = solid+solid / empty+empty
  edge-w            = empty+solid / empty+solid
  edge-e            = solid+empty / solid+empty
  corner-outer-nw   = outer(0°) / edge_h / edge_v / solid
  corner-outer-ne   = edge_h / outer(90°) / solid / edge_v
  corner-outer-sw   = edge_v / solid / outer(270°) / edge_h
  corner-outer-se   = solid / edge_v / edge_h / outer(180°)
  corner-inner-nw   = inner(0°) / solid / solid / solid
  ...
"""
import os
from PIL import Image

TILE = 48
QUAD = 24  # 子象限尺寸
RADIUS = 10  # 圆弧半径


# ═══════════════════════════════════════════════
#  5 张基础 24×24 掩码
# ═══════════════════════════════════════════════

def mask_solid():
    """全白 (纯地形A)"""
    return Image.new("L", (QUAD, QUAD), 255)

def mask_empty():
    """全黑 (纯地形B)"""
    return Image.new("L", (QUAD, QUAD), 0)

def mask_edge_h():
    """水平边: 上白下黑, 交界2px抖动"""
    m = Image.new("L", (QUAD, QUAD), 0)
    px = m.load()
    for y in range(QUAD):
        for x in range(QUAD):
            if y < QUAD // 2 - 1:
                px[x, y] = 255
            elif y < QUAD // 2 + 1:
                # 交界抖动
                px[x, y] = 255 if (x + y) % 2 == 0 else 0
    return m

def mask_edge_v():
    """垂直边: 左白右黑, 交界2px抖动"""
    m = Image.new("L", (QUAD, QUAD), 0)
    px = m.load()
    for y in range(QUAD):
        for x in range(QUAD):
            if x < QUAD // 2 - 1:
                px[x, y] = 255
            elif x < QUAD // 2 + 1:
                px[x, y] = 255 if (x + y) % 2 == 0 else 0
    return m

def mask_outer_corner():
    """外凸圆弧: 左上1/4为白(地形A), 右下为黑(地形B)
    圆弧用像素阶梯: 4→2→2→1→1→1 递减
    """
    m = Image.new("L", (QUAD, QUAD), 0)
    px = m.load()
    cx, cy = QUAD, QUAD  # 圆心在右下角
    for y in range(QUAD):
        for x in range(QUAD):
            dx = cx - x - 1
            dy = cy - y - 1
            dist = (dx * dx + dy * dy) ** 0.5
            if dist <= RADIUS:
                px[x, y] = 255
            elif dist <= RADIUS + 1.5:
                # 圆弧边缘抖动
                px[x, y] = 255 if (x + y) % 2 == 0 else 0
    return m

def mask_inner_corner():
    """内凹圆弧: 左上3/4为白(地形A), 右下1/4圆为黑(地形B)
    圆弧切入地形A
    """
    m = Image.new("L", (QUAD, QUAD), 255)
    px = m.load()
    cx, cy = QUAD, QUAD  # 圆心在右下角
    for y in range(QUAD):
        for x in range(QUAD):
            dx = cx - x - 1
            dy = cy - y - 1
            dist = (dx * dx + dy * dy) ** 0.5
            if dist <= RADIUS - 1:
                px[x, y] = 0
            elif dist <= RADIUS + 0.5:
                px[x, y] = 0 if (x + y) % 2 == 0 else 255
    return m


# ═══════════════════════════════════════════════
#  象限变换 (旋转/镜像)
# ═══════════════════════════════════════════════

def rotate_90(mask):
    return mask.rotate(90, expand=True)

def rotate_180(mask):
    return mask.rotate(180, expand=True)

def rotate_270(mask):
    return mask.rotate(270, expand=True)

def flip_h(mask):
    return mask.transpose(Image.FLIP_LEFT_RIGHT)

def flip_v(mask):
    return mask.transpose(Image.FLIP_TOP_BOTTOM)


# ═══════════════════════════════════════════════
#  48×48 组装: 4 个 24×24 象限拼接
# ═══════════════════════════════════════════════

def assemble(q_nw, q_ne, q_sw, q_se):
    """4 个象限 → 48×48"""
    result = Image.new("L", (TILE, TILE), 0)
    result.paste(q_nw, (0, 0))
    result.paste(q_ne, (QUAD, 0))
    result.paste(q_sw, (0, QUAD))
    result.paste(q_se, (QUAD, QUAD))
    return result


# ═══════════════════════════════════════════════
#  13 块 autotile 定义
# ═══════════════════════════════════════════════

SOLID = mask_solid()
EMPTY = mask_empty()
EDGE_H = mask_edge_h()
EDGE_V = mask_edge_v()
OUTER = mask_outer_corner()
INNER = mask_inner_corner()

# 派生: 旋转/镜像
EDGE_H_INV = flip_v(EDGE_H)      # 上黑下白
EDGE_V_INV = flip_h(EDGE_V)      # 左黑右白

OUTER_NE = rotate_90(OUTER)      # 右上外角
OUTER_SE = rotate_180(OUTER)     # 右下外角
OUTER_SW = rotate_270(OUTER)     # 左下外角

INNER_NE = rotate_90(INNER)      # 右上内角
INNER_SE = rotate_180(INNER)     # 右下内角
INNER_SW = rotate_270(INNER)     # 左下内角

TILES = {
    # 中心
    "center":            assemble(SOLID, SOLID, SOLID, SOLID),

    # 边缘 (地形A在中心侧)
    "edge-n":            assemble(EMPTY, EMPTY, SOLID, SOLID),   # 上黑下白
    "edge-s":            assemble(SOLID, SOLID, EMPTY, EMPTY),   # 上白下黑
    "edge-w":            assemble(EMPTY, SOLID, EMPTY, SOLID),   # 左黑右白
    "edge-e":            assemble(SOLID, EMPTY, SOLID, EMPTY),   # 左白右黑

    # 外角 (地形A凸向地形B, 1/4圆)
    "corner-outer-nw":   assemble(OUTER,    EDGE_H,   EDGE_V,   SOLID),
    "corner-outer-ne":   assemble(EDGE_H,   OUTER_NE, SOLID,    EDGE_V),
    "corner-outer-sw":   assemble(EDGE_V,   SOLID,    OUTER_SW, EDGE_H),
    "corner-outer-se":   assemble(SOLID,    EDGE_V,   EDGE_H,   OUTER_SE),

    # 内角 (地形B切入地形A, 3/4圆)
    "corner-inner-nw":   assemble(INNER,    SOLID,    SOLID,    SOLID),
    "corner-inner-ne":   assemble(SOLID,    INNER_NE, SOLID,    SOLID),
    "corner-inner-sw":   assemble(SOLID,    SOLID,    INNER_SW, SOLID),
    "corner-inner-se":   assemble(SOLID,    SOLID,    SOLID,    INNER_SE),
}


# ═══════════════════════════════════════════════
#  合成
# ═══════════════════════════════════════════════

def generate_autotile_set(tex_a_path, tex_b_path, output_dir, prefix):
    """从两张纯纹理生成完整 13 块 autotile 套件

    tex_a: 地形A (如草地) — 被掩码白区保留
    tex_b: 地形B (如泥土) — 被掩码黑区显露
    """
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


# ═══════════════════════════════════════════════
#  调试: 导出掩码预览 (检查圆弧)
# ═══════════════════════════════════════════════

def export_masks_preview(output_dir):
    """导出 13 张掩码预览 (黑白图)"""
    os.makedirs(output_dir, exist_ok=True)
    for tile_id, mask in TILES.items():
        mask.save(os.path.join(output_dir, f"mask-{tile_id}.png"))
    # 也导出5张基础
    for name, m in [("solid", SOLID), ("empty", EMPTY),
                     ("edge_h", EDGE_H), ("edge_v", EDGE_V),
                     ("outer", OUTER), ("inner", INNER)]:
        m.save(os.path.join(output_dir, f"base-{name}.png"))


if __name__ == "__main__":
    import sys
    if len(sys.argv) >= 4:
        tex_a, tex_b, out_dir, prefix = sys.argv[1], sys.argv[2], sys.argv[3], sys.argv[4]
        result = generate_autotile_set(tex_a, tex_b, out_dir, prefix)
        print(f"Generated {len(result)} autotiles → {out_dir}")
    elif len(sys.argv) >= 2 and sys.argv[1] == "preview":
        export_masks_preview("out/v1_masks")
        print("Masks preview → out/v1_masks/")
    else:
        print("Usage:")
        print("  python autotile_compose.py <tex_a> <tex_b> <output_dir> <prefix>")
        print("  python autotile_compose.py preview  # 导出掩码预览")
