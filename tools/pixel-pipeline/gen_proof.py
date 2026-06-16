# -*- coding: utf-8 -*-
"""像素层 · 薄证管线：程序化确定性生成 21 路图标 sprite + 样例 tilemap。
纯 Pillow，无随机(纹理用 pathId 哈希，可复现)。最低像素图。
out/path_icons.png(21 路图标接触表) + out/sample_map.png(九野样例 tilemap)。"""
import os, hashlib, io, sys
from PIL import Image, ImageDraw, ImageFont
sys.stdout.reconfigure(encoding="utf-8")

OUT = os.path.join(os.path.dirname(__file__), "out")
os.makedirs(OUT, exist_ok=True)
S = 32            # native sprite 像素
SCALE = 5         # 放大倍数(像素风 nearest)

# —— 调色板(确定性, 元素/门类) ——
PAL = {
    "void": (24, 22, 30, 0), "bg": (40, 38, 50, 255), "frame": (210, 200, 170, 255),
    "steel": (180, 188, 200), "red": (200, 60, 50), "fire": (230, 110, 40),
    "ice": (120, 200, 230), "thunder": (240, 210, 70), "wood": (90, 180, 90),
    "purple": (150, 90, 200), "gold": (235, 195, 90), "dark": (70, 50, 70),
    "blood": (150, 30, 40), "green": (80, 160, 110), "bone": (220, 215, 195),
    "azure": (90, 150, 220), "rose": (210, 120, 160),
}
def c(name): return PAL[name][:3]

# —— 21 路描述符: (pathId, 名, 攻击维, 主色, 辅色, 图符) ——
PATHS = [
    ("sword_immortal", "剑", "physical", "steel", "ice", "blade"),
    ("ti_xiu_hengshi", "体", "physical", "red", "bone", "fist"),
    ("fa_xiu", "法", "physical", "fire", "azure", "orb"),
    ("gui_xiu_yang_hun", "鬼", "spirit", "purple", "dark", "wisp"),
    ("dan_xiu", "丹", "economic", "fire", "gold", "flask"),
    ("qixiu_artificer", "器", "economic", "steel", "gold", "anvil"),
    ("array_formation", "阵", "physical", "azure", "wood", "grid"),
    ("soul_divine_sense", "魂", "spirit", "purple", "azure", "eye"),
    ("lei_xiu", "雷", "physical", "thunder", "steel", "bolt"),
    ("buddhist_golden_body", "佛", "physical", "gold", "red", "halo"),
    ("ming_fate_causality", "命", "fate", "azure", "gold", "star"),
    ("yu_shou", "驭兽", "physical", "wood", "bone", "paw"),
    ("ru_xiu_haoran", "儒", "spirit", "azure", "gold", "scroll"),
    ("mo_xiu_xinmo", "魔", "physical", "blood", "dark", "horns"),
    ("yao_xiu_huaxing", "妖", "physical", "green", "rose", "claw"),
    ("xue_xiu_xuesha", "血", "physical", "blood", "red", "drop"),
    ("du_gu_xiu", "毒蛊", "physical", "green", "dark", "bug"),
    ("fu_xiu_fulu", "符", "physical", "gold", "red", "talisman"),
    ("kuilei_shi", "傀儡", "physical", "bone", "steel", "puppet"),
    ("yin_xiu_yuedao", "音", "spirit", "rose", "azure", "note"),
    ("yinguo_faze", "因果", "fate", "purple", "gold", "yinyang"),
]
DIM_FRAME = {"physical": "steel", "spirit": "purple", "fate": "gold", "economic": "wood"}

def seeded(pid):
    return int.from_bytes(hashlib.sha256(pid.encode()).digest()[:4], "big")

def draw_glyph(d, g, col, ac):
    """在 32x32 画图符(像素坐标), col 主色 ac 辅色。"""
    m = c(col); a = c(ac)
    if g == "blade":
        d.line([(16, 4), (16, 24)], fill=m, width=2); d.line([(11, 22), (21, 22)], fill=a, width=2)
        d.polygon([(16, 2), (14, 6), (18, 6)], fill=m)
    elif g == "fist":
        d.rectangle([10, 12, 22, 24], fill=m); d.line([(10, 15), (22, 15)], fill=a)
        d.line([(10, 19), (22, 19)], fill=a)
    elif g == "orb":
        d.ellipse([9, 9, 23, 23], fill=m); d.ellipse([13, 12, 18, 17], fill=a)
    elif g == "wisp":
        for i, y in enumerate(range(6, 26, 3)):
            x = 16 + (3 if i % 2 else -3); d.ellipse([x-3, y-2, x+3, y+2], fill=m)
        d.ellipse([13, 6, 19, 12], fill=a)
    elif g == "flask":
        d.polygon([(13, 12), (19, 12), (23, 24), (9, 24)], fill=m); d.rectangle([14, 7, 18, 12], fill=a)
    elif g == "anvil":
        d.rectangle([8, 14, 24, 19], fill=m); d.rectangle([13, 19, 19, 24], fill=m); d.polygon([(8, 14), (4, 14), (8, 17)], fill=a)
    elif g == "grid":
        for x in (10, 16, 22): d.line([(x, 8), (x, 24)], fill=m)
        for y in (10, 16, 22): d.line([(8, y), (24, y)], fill=a)
    elif g == "eye":
        d.ellipse([7, 12, 25, 20], fill=a); d.ellipse([13, 12, 19, 20], fill=m); d.ellipse([15, 14, 17, 18], fill=(20, 20, 20))
    elif g == "bolt":
        d.line([(18, 4), (12, 16)], fill=m, width=2); d.line([(12, 16), (18, 16)], fill=m, width=2); d.line([(18, 16), (12, 28)], fill=a, width=2)
    elif g == "halo":
        d.ellipse([8, 8, 24, 24], outline=m, width=2)
        for ang in range(0, 360, 45):
            import math; x = 16 + int(11 * math.cos(math.radians(ang))); y = 16 + int(11 * math.sin(math.radians(ang))); d.point((x, y), fill=a)
        d.ellipse([13, 13, 19, 19], fill=a)
    elif g == "star":
        import math; pts = []
        for k in range(10):
            r = 11 if k % 2 == 0 else 5; ang = math.radians(-90 + k * 36)
            pts.append((16 + int(r*math.cos(ang)), 16 + int(r*math.sin(ang))))
        d.polygon(pts, fill=m); d.point((16, 16), fill=a)
    elif g == "paw":
        d.ellipse([12, 16, 20, 24], fill=m)
        for x in (11, 15, 19): d.ellipse([x, 10, x+3, 14], fill=a)
    elif g == "scroll":
        d.rectangle([9, 8, 23, 24], fill=m); d.rectangle([9, 8, 23, 11], fill=a); d.rectangle([9, 21, 23, 24], fill=a)
        d.line([(12, 14), (20, 14)], fill=a); d.line([(12, 17), (20, 17)], fill=a)
    elif g == "horns":
        d.polygon([(10, 24), (8, 8), (14, 18)], fill=m); d.polygon([(22, 24), (24, 8), (18, 18)], fill=m); d.ellipse([13, 18, 19, 24], fill=a)
    elif g == "claw":
        for x in (11, 16, 21): d.line([(x, 6), (x-2, 24)], fill=m, width=2)
        d.arc([8, 4, 24, 12], 0, 180, fill=a)
    elif g == "drop":
        d.polygon([(16, 5), (23, 20), (16, 26), (9, 20)], fill=m); d.ellipse([13, 17, 17, 22], fill=a)
    elif g == "bug":
        d.ellipse([11, 10, 21, 24], fill=m)
        for y in (13, 17, 21):
            d.line([(11, y), (5, y-2)], fill=a); d.line([(21, y), (27, y-2)], fill=a)
        d.ellipse([13, 7, 19, 12], fill=a)
    elif g == "talisman":
        d.rectangle([11, 5, 21, 27], fill=m); d.line([(16, 8), (16, 24)], fill=a)
        for y in (11, 15, 19): d.line([(13, y), (19, y)], fill=a)
    elif g == "puppet":
        d.line([(16, 6), (16, 22)], fill=m, width=2); d.line([(8, 12), (24, 12)], fill=m, width=2)
        for p in [(16, 6), (8, 12), (24, 12), (16, 22)]: d.ellipse([p[0]-2, p[1]-2, p[0]+2, p[1]+2], fill=a)
    elif g == "note":
        d.ellipse([10, 18, 17, 24], fill=m); d.line([(17, 21), (17, 7)], fill=m, width=2); d.line([(17, 7), (23, 9)], fill=a, width=2)
    elif g == "yinyang":
        d.ellipse([8, 8, 24, 24], fill=m); d.pieslice([8, 8, 24, 24], -90, 90, fill=a)
        d.ellipse([13, 9, 19, 15], fill=a); d.ellipse([13, 17, 19, 23], fill=m)

def make_icon(path):
    pid, name, dim, col, ac, g = path
    img = Image.new("RGBA", (S, S), PAL["void"])
    d = ImageDraw.Draw(img)
    d.rectangle([2, 2, S-3, S-3], fill=PAL["bg"])
    # 品阶边框(攻击维定色) + 哈希纹理点缀
    fr = c(DIM_FRAME[dim]); d.rectangle([1, 1, S-2, S-2], outline=fr, width=2)
    h = seeded(pid)
    for i in range(6):
        bit = (h >> (i*3)) & 7; px = 3 + (bit % 5); py = 3 + ((h >> (i*4)) & 3)
        img.putpixel((px, py), fr)
    draw_glyph(d, g, col, ac)
    return img

def upscale(img, k): return img.resize((img.width*k, img.height*k), Image.NEAREST)

def font(sz):
    for fp in ("C:/Windows/Fonts/msyh.ttc", "C:/Windows/Fonts/simhei.ttf"):
        if os.path.exists(fp):
            try: return ImageFont.truetype(fp, sz)
            except Exception: pass
    return ImageFont.load_default()

def contact_sheet():
    cols, rows = 7, 3; cell = S*SCALE; pad = 8; lab = 22
    W = cols*(cell+pad)+pad; H = rows*(cell+lab+pad)+pad+24
    sheet = Image.new("RGBA", (W, H), (28, 26, 34, 255))
    d = ImageDraw.Draw(sheet); f = font(18); fl = font(15)
    d.text((pad, 6), "九野 · 21 路图标 sprite(程序化确定性像素)", font=f, fill=(230, 220, 190))
    for i, p in enumerate(PATHS):
        r, col = divmod(i, cols); x = pad + col*(cell+pad); y = 30 + pad + r*(cell+lab+pad)
        sheet.paste(upscale(make_icon(p), SCALE), (x, y))
        d.text((x, y+cell+2), f"{p[1]} {p[0]}", font=fl, fill=(200, 195, 180))
    out = os.path.join(OUT, "path_icons.png"); sheet.save(out); return out, sheet.size

def sample_map():
    """九野样例 tilemap(设计 doc 样例大区): 大区色块(QiDensity) + 地标标记 + 邻接。"""
    TILE = 16; GW, GH = 30, 22; W, H = GW*TILE, GH*TILE+30
    img = Image.new("RGBA", (W, H), (30, 34, 40, 255)); d = ImageDraw.Draw(img)
    # (名, x0,y0,x1,y1 格, QiDensity 0-100) — 设计 doc 样例
    regions = [
        ("中州·学宫腹地", 10, 6, 19, 14, 45), ("东海·剑墟", 20, 7, 28, 13, 70),
        ("南疆·百蛊渊", 18, 15, 27, 21, 55), ("北漠·血煞渊", 9, 1, 18, 5, 30),
        ("西陲·镔铁坞", 1, 7, 9, 15, 35), ("塞外·异域", 1, 1, 8, 6, 60),
    ]
    def qi_color(q):
        # 薄灵(低)=棕黄 → 厚灵(高)=青绿
        t = q/100; return (int(150-70*t), int(120+50*t), int(80+90*t))
    for name, x0, y0, x1, y1, q in regions:
        d.rectangle([x0*TILE, y0*TILE, x1*TILE, y1*TILE], fill=qi_color(q), outline=(20, 20, 24), width=2)
    # 邻接(大区中心连线)
    cen = {r[0]: ((r[1]+r[3])*TILE//2, (r[2]+r[4])*TILE//2) for r in regions}
    edges = [("中州·学宫腹地", "东海·剑墟"), ("中州·学宫腹地", "南疆·百蛊渊"),
             ("中州·学宫腹地", "西陲·镔铁坞"), ("北漠·血煞渊", "中州·学宫腹地"),
             ("西陲·镔铁坞", "塞外·异域"), ("东海·剑墟", "南疆·百蛊渊")]
    for a, b in edges: d.line([cen[a], cen[b]], fill=(235, 225, 180), width=2)
    # 地标(★大门派/皇城■/秘境◆) — 样例
    marks = [("中州·学宫腹地", "皇", "sq", (235, 210, 120)), ("东海·剑墟", "剑", "star", (180, 200, 220)),
             ("南疆·百蛊渊", "蛊", "dia", (120, 180, 110)), ("北漠·血煞渊", "血", "star", (180, 50, 60)),
             ("西陲·镔铁坞", "铁", "sq", (190, 190, 200)), ("塞外·异域", "异", "dia", (170, 130, 200))]
    f = font(14)
    for rn, lbl, shp, col in marks:
        cx, cy = cen[rn]
        if shp == "star":
            import math; pts = [(cx+int((7 if k%2==0 else 3)*math.cos(math.radians(-90+k*36))), cy+int((7 if k%2==0 else 3)*math.sin(math.radians(-90+k*36)))) for k in range(10)]; d.polygon(pts, fill=col, outline=(20, 20, 20))
        elif shp == "sq": d.rectangle([cx-6, cy-6, cx+6, cy+6], fill=col, outline=(20, 20, 20))
        else: d.polygon([(cx, cy-7), (cx+7, cy), (cx, cy+7), (cx-7, cy)], fill=col, outline=(20, 20, 20))
    # 图例
    d.text((6, H-26), "九野样例 tilemap · 色块=大区(棕薄灵→青厚灵) 线=邻接 ★剑墟/血煞 ■皇城/铁坞 ◆秘境", font=f, fill=(225, 220, 195))
    for name, x0, y0, *_ in regions:
        d.text((x0*TILE+3, y0*TILE+3), name, font=font(12), fill=(30, 28, 30))
    out = os.path.join(OUT, "sample_map.png"); img.save(out); return out, img.size

if __name__ == "__main__":
    p1, s1 = contact_sheet(); print("icons:", p1, s1)
    p2, s2 = sample_map(); print("map:", p2, s2)
    print("OK 21 路图标 +样例地图 生成")
