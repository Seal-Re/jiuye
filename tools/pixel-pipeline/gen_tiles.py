# -*- coding: utf-8 -*-
"""九野 · 瓦片占位生成器 (mv-001 Pillow fallback)
83 种瓦片的纯色几何占位——供 MapGenerator.gd 渲染管线测试用。
AI 素材到齐后替换为真实像素图。

用法: python tools/pixel-pipeline/gen_tiles.py
输出: tools/pixel-pipeline/out/tiles/ (83 PNG, 48×48 each)
"""

import os, json, math
from PIL import Image, ImageDraw

N = 48  # 48×48 native tile size
OUT = os.path.join(os.path.dirname(__file__), "out", "tiles")
os.makedirs(OUT, exist_ok=True)

# —— 调色板 (每 terrain 的占位颜色) ——
PALETTE = {
    # Layer 0 — 基础地形
    "T01": [(100,160,80),(80,140,60),(140,160,100)],     # 草地: 翠绿/深绿/枯黄
    "T02": [(40,100,180),(60,130,200),(30,70,150)],       # 海域: 深蓝/蓝/暗蓝
    "T03": [(210,180,120),(190,160,100),(180,150,130)],   # 荒漠: 金黄/沙/龟裂
    "T04": [(160,90,60),(130,100,80),(80,50,40)],         # 山岳火: 红褐/矿脉/暗岩
    "T05": [(50,100,40),(40,80,30),(60,70,50)],          # 林莽: 暗绿/深绿/瘴紫
    "T06": [(80,140,60),(60,100,40),(100,120,80)],       # 山峦: 翠绿/古树/雾霭
    "T07": [(80,160,160),(60,140,150),(100,150,120)],    # 水泽: 青绿/莲花/芦苇
    # Layer 0 — 特殊
    "T08": [(40,20,10),(200,80,20),(255,150,30)],         # 火山: 黑/橙红/亮橙
    "T09": [(220,230,240),(180,200,220),(200,210,230)],   # 雪原: 白/浅蓝/灰白
    "T10": [(30,25,20),(50,60,40),(180,170,160)],         # 鬼域: 焦黑/幽绿/骨白
    "T11": [(200,180,80),(100,160,80),(240,230,200)],     # 灵脉: 金/翠绿/玉白
    # Layer 1 — 装饰 (亮色，透明背景用实色替代)
    "D01-D10": [(255,200,200),(200,100,100),(200,200,100),(255,220,180),
                (200,200,220),(180,160,140),(100,200,100),(220,220,200),
                (200,200,240),(255,200,100)],
    "D11-D20": [(180,160,140),(160,100,200),(100,140,60),(180,120,180),
                (140,100,60),(200,100,80),(200,210,230),(80,160,100),
                (100,180,200),(220,220,240)],
    "D21-D24": [(100,80,60),(120,110,100),(140,130,120),(160,100,80)],
    # Layer 2 — 稀有 (高亮/特殊)
    "R01-R10": [(200,160,40),(100,80,60),(160,180,200),(240,120,40),
                (100,40,60),(120,160,100),(140,120,100),(80,60,80),
                (100,180,200),(60,40,30)],
    # Layer 3 — 地标
    "L01-L10": [(200,160,40),(160,200,180),(140,120,100),(100,140,200),
                (180,160,120),(120,100,140),(200,180,100),(180,140,100),
                (100,80,60),(160,60,40)],
    "L11-L21": [(200,120,40),(220,180,100),(200,100,40),(140,100,80),
                (160,80,120),(100,160,60),(80,120,180),(180,160,80),
                (160,120,60),(200,200,180),(100,140,160)],
    # 边境
    "B01-B03": [(180,160,100),(140,120,80),(100,140,200)],
}

def ramp(base_color, steps=5):
    """从 base 生成 5 阶明度 ramp (暗→亮)"""
    r,g,b = base_color
    return [(
        max(0, min(255, r * (3 + i*2) // 10)),
        max(0, min(255, g * (3 + i*2) // 10)),
        max(0, min(255, b * (3 + i*2) // 10)),
    ) for i in range(steps)]

def draw_variant(img, colors, variant_idx):
    """在 48×48 上画简单的几何区分 (3 变体: 纯色/条纹/斑点)"""
    d = ImageDraw.Draw(img)
    rp = ramp(colors[variant_idx % len(colors)])
    if variant_idx == 0:  # 纯色渐变
        for y in range(N):
            k = 2 + (y * 3 // N) - 1
            k = max(0, min(4, k))
            d.line([(0, y), (N, y)], fill=rp[k])
    elif variant_idx == 1:  # 对角条纹
        for i in range(-N, N, 4):
            d.line([(i, 0), (i+N, N)], fill=rp[2 + (i%3)-1], width=2)
    else:  # 斑点
        for y in range(0, N, 8):
            for x in range(0, N, 8):
                d.ellipse([x, y, x+3, y+3], fill=rp[3])

def draw_decor(img, color_idx, palette_list):
    """装饰物: 简单形状"""
    d = ImageDraw.Draw(img)
    c = palette_list[color_idx % len(palette_list)]
    cx, cy = N//2, N//2
    d.ellipse([cx-6, cy-6, cx+6, cy+6], fill=c, outline=tuple(max(0,x-40) for x in c))
    d.ellipse([cx-12, cy-2, cx-4, cy+6], fill=c)

def draw_landmark(img, color_idx, palette_list):
    """地标: 矩形建筑占位"""
    d = ImageDraw.Draw(img)
    c = palette_list[color_idx % len(palette_list)]
    rp = ramp(c)
    # 建筑体
    d.rectangle([8, 16, 40, 44], fill=rp[2], outline=rp[0])
    d.rectangle([20, 6, 28, 16], fill=rp[3], outline=rp[0])  # 塔/尖顶
    # 门
    d.rectangle([20, 32, 28, 44], fill=rp[0])

def main():
    # 读取 prompt 文件，跳过注释行 (# 开头) 和空行
    with open(os.path.join(os.path.dirname(__file__), "tile_prompts.json"), "r", encoding="utf-8") as f:
        raw = f.read()
    # 去掉 // 和 # 注释行
    lines = []
    for line in raw.split("\n"):
        stripped = line.strip()
        if stripped.startswith("#") or stripped.startswith("//"):
            continue
        lines.append(line)
    clean_json = "\n".join(lines)
    prompts = json.loads(clean_json)

    for p in prompts:
        tid = p["id"]
        layer = p["layer"]
        img = Image.new("RGBA", (N, N), (0,0,0,0) if layer > 0 else (0,0,0,255))
        name = p["name"]

        # 选色逻辑
        if tid.startswith("T0"):  # Layer 0 terrain
            key = tid[:3]
            colors = PALETTE.get(key, [(128,128,128)])
            variant = int(tid[-1]) - 1 if tid[-1].isdigit() else 0  # A=0, B=1, C=2
            if not img.getpixel((0,0))[3]:  # 透明底? no——Layer0 不透明
                pass
            draw_variant(img, colors, variant if tid[-1] in "ABC" else 0)
        elif tid.startswith("T08"): draw_variant(img, PALETTE["T08"], 0)
        elif tid.startswith("T09"): draw_variant(img, PALETTE["T09"], 0)
        elif tid.startswith("T10"): draw_variant(img, PALETTE["T10"], 0)
        elif tid.startswith("T11"): draw_variant(img, PALETTE["T11"], 0)
        elif tid.startswith("D"):
            idx = int(tid[1:]) - 1
            if idx < 10: draw_decor(img, idx, PALETTE["D01-D10"])
            elif idx < 20: draw_decor(img, idx-10, PALETTE["D11-D20"])
            else: draw_decor(img, idx-20, PALETTE["D21-D24"])
        elif tid.startswith("R"):
            idx = int(tid[1:]) - 1
            draw_decor(img, idx, PALETTE["R01-R10"])
        elif tid.startswith("L"):
            idx = int(tid[1:]) - 1
            if idx < 10: draw_landmark(img, idx, PALETTE["L01-L10"])
            else: draw_landmark(img, idx-10, PALETTE["L11-L21"])
        elif tid.startswith("B"):
            idx = int(tid[1:]) - 1
            draw_variant(img, PALETTE["B01-B03"], idx)

        path = os.path.join(OUT, f"{tid}.png")
        img.save(path)

    print(f"Generated {len(prompts)} placeholder tiles → {OUT}")

if __name__ == "__main__":
    main()
