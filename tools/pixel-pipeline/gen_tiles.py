# -*- coding: utf-8 -*-
"""九野 · 像素瓦片程序化生成器 v2
83 种瓦片——不再是简单几何占位，而是带纹理/阴影/高光的程序化像素。
AI 素材到后替换；当前可直接用于 Godot TileMap 渲染管线测试。
"""
import os, json, math, random
from PIL import Image, ImageDraw, ImageFilter

N = 48  # 48×48 native
SCALE = 4  # 导出 192×192
OUT = os.path.join(os.path.dirname(__file__), "out", "tiles")
os.makedirs(OUT, exist_ok=True)

random.seed(42)  # 确定性

# —— 调色板 ——
PAL = {
    "grass":    [(90,160,70),(70,130,50),(110,180,90),(140,200,110),(50,100,40)],
    "grass_dry":[(160,150,100),(140,130,80),(180,170,120),(120,110,60),(100,90,50)],
    "sea":      [(30,80,160),(50,110,190),(20,60,130),(70,140,210),(10,40,100)],
    "sand":     [(210,180,120),(190,160,100),(230,200,140),(170,140,80),(150,120,60)],
    "rock_red": [(160,90,60),(130,70,40),(180,110,80),(140,80,50),(200,130,90)],
    "rock_ore": [(120,100,80),(140,110,70),(180,150,100),(200,170,60),(220,190,80)],
    "rock_lava":[(60,30,20),(80,40,25),(200,80,30),(240,120,40),(255,160,50)],
    "jungle":   [(40,90,30),(30,70,20),(60,110,40),(80,130,50),(20,50,10)],
    "jungle_miasma":[(50,70,40),(30,50,30),(100,60,100),(140,80,140),(80,40,80)],
    "mtn_green":[(70,140,60),(50,120,40),(90,160,80),(110,180,100),(40,100,30)],
    "marsh":    [(80,160,150),(60,140,130),(100,180,170),(120,200,190),(50,120,110)],
    "volcano":  [(30,15,10),(50,25,15),(220,90,30),(255,140,40),(255,180,60)],
    "snow":     [(220,230,240),(200,210,225),(235,240,248),(180,195,215),(210,220,235)],
    "ghost":    [(30,25,20),(45,40,35),(60,55,50),(80,180,80),(120,80,120)],
    "spirit":   [(180,160,70),(200,180,90),(220,200,140),(240,220,180),(160,140,60)],
    "road":     [(170,150,110),(150,130,90),(190,170,130),(140,120,80),(130,110,70)],
    "gate":     [(140,110,70),(120,95,55),(160,130,85),(180,150,100),(100,80,45)],
    "realm_gate":[(80,140,220),(100,160,240),(60,120,200),(120,180,250),(50,100,180)],
}

def ramp(c):
    r,g,b = c
    return [(max(0,r*(3+i*2)//10),max(0,g*(3+i*2)//10),max(0,b*(3+i*2)//10)) for i in range(5)]

def draw_v1(img, pal_key, variant=0):
    """基础地形: 渐变底色 + 噪声纹理 + 左上阴影"""
    d = ImageDraw.Draw(img)
    rp = PAL.get(pal_key, PAL["grass"])
    # 渐变底色
    for y in range(N):
        shade = 1.0 - y/N * 0.15 + (0.05 if variant==0 else -0.05 if variant==2 else 0)
        k = int(2 + shade * 2); k = max(0, min(len(rp)-1, k))
        c = tuple(int(v*shade) for v in rp[k])
        d.line([(0,y),(N,y)], fill=c)
    # 随机纹理斑点
    rng = random.Random(variant * 100 + 42)
    for _ in range(8 + variant * 3):
        cx, cy = rng.randint(4, N-5), rng.randint(4, N-5)
        r = rng.randint(1, 4)
        tc = tuple(max(0,min(255, v+rng.randint(-20,20))) for v in rp[2+variant%2])
        d.ellipse([cx-r,cy-r,cx+r,cy+r], fill=tc)
    # 左上高光
    for y in range(4):
        for x in range(4):
            if x+y < 5:
                px = 0 if img.mode == "RGBA" else img.getpixel((x,y))
                if isinstance(px, int): px = (px,px,px)
                bc = px[:3] if len(px) >= 3 else (100,100,100)
                hl = tuple(min(255, v+30) for v in bc)
                d.point((x,y), fill=hl)
    return img

def draw_v2(img, pal_key, features=None):
    """装饰物: 透明背景上画形状"""
    d = ImageDraw.Draw(img)
    rp = PAL.get(pal_key, PAL["grass"])
    if features == "flowers":
        for cx, cy, col in [(14,20,(255,220,200)),(30,28,(255,240,100)),(22,36,(220,200,255))]:
            d.ellipse([cx-2,cy-2,cx+2,cy+2], fill=col)
            d.point((cx,cy-3), fill=(255,255,200))
    elif features == "mushrooms":
        for cx, cy in [(16,30),(32,24)]:
            d.ellipse([cx-3,cy-6,cx+3,cy], fill=(220,60,40))
            d.rectangle([cx-1,cy,cx+1,cy+4], fill=(240,220,200))
    elif features == "cactus":
        d.rectangle([22,14,26,38], fill=(80,160,60))
        d.rectangle([14,20,22,30], fill=(80,160,60))
        d.rectangle([26,22,34,24], fill=(80,160,60))
    elif features == "bones":
        d.line([(16,30),(28,20)], fill=(220,210,190), width=3)
        d.line([(28,20),(32,26)], fill=(220,210,190), width=2)
    elif features == "crystal":
        pts = [(24,12),(28,18),(24,36),(20,18)]
        d.polygon(pts, fill=(180,200,240,200))
    elif features == "ruins":
        d.rectangle([14,20,34,38], fill=(140,130,120))
        d.rectangle([14,20,16,38], fill=(120,110,100))
        d.line([(20,20),(28,16)], fill=(100,90,80), width=2)
    elif features == "treasure":
        d.rectangle([18,22,30,36], fill=(180,140,60))
        d.rectangle([16,18,32,22], fill=(200,160,80))
        d.ellipse([22,14,26,18], fill=(240,200,60))
    elif features == "hermit_hut":
        d.rectangle([12,24,36,40], fill=(200,180,140))
        d.polygon([(10,24),(24,14),(38,24)], fill=(160,120,60))
    elif features == "spring":
        d.ellipse([16,20,32,36], fill=(100,180,220,180))
        d.ellipse([20,24,28,32], fill=(150,220,250,200))
    return img

def draw_v3(img, pal_key, shape="tower"):
    """地标建筑"""
    d = ImageDraw.Draw(img)
    rp = PAL.get(pal_key, PAL["grass"])
    if shape == "palace":
        d.rectangle([6,20,42,44], fill=rp[2])
        d.rectangle([4,12,8,20], fill=rp[0]); d.rectangle([40,12,44,20], fill=rp[0])
        d.rectangle([20,8,28,20], fill=rp[3])
        d.rectangle([8,18,40,20], fill=rp[1])
        # 金顶
        for x in range(6,43,4):
            d.ellipse([x,16,x+3,22], fill=(240,200,60))
    elif shape == "temple":
        d.rectangle([10,24,38,44], fill=rp[2])
        d.rectangle([18,10,30,24], fill=rp[3])
        d.polygon([(16,10),(24,4),(32,10)], fill=rp[4])
    elif shape == "gate_wall":
        d.rectangle([4,16,44,44], fill=rp[1])
        d.rectangle([16,24,32,44], fill=rp[0])
        d.rectangle([18,26,30,42], fill=(40,30,20))
    elif shape == "market":
        for x in [8, 24]:
            d.rectangle([x,22,x+14,42], fill=rp[2])
            d.rectangle([x+2,14,x+12,22], fill=rp[3])
    elif shape == "forge":
        d.rectangle([14,16,34,44], fill=rp[0])
        d.rectangle([20,8,28,20], fill=rp[3])
        d.ellipse([18,6,30,14], fill=(240,120,30))
    elif shape == "cave":
        d.ellipse([8,16,40,44], fill=rp[0])
        d.ellipse([16,24,32,40], fill=(20,15,10))
    elif shape == "stele":
        d.rectangle([18,12,30,44], fill=rp[1])
        d.line([(20,18),(28,18)], fill=rp[4], width=1)
        d.line([(22,22),(26,22)], fill=rp[4], width=1)
    return img


def main():
    with open(os.path.join(os.path.dirname(__file__), "tile_prompts.json"), "r", encoding="utf-8") as f:
        raw = f.read()
    lines = [l for l in raw.split("\n") if not l.strip().startswith("#")]
    prompts = json.loads("\n".join(lines))

    for p in prompts:
        tid = p["id"]
        layer = p["layer"]
        name = p["name"]
        bg = (0,0,0,0) if layer > 0 else (0,0,0,255)
        img = Image.new("RGBA", (N, N), bg)

        # Layer 0 terrain
        if tid.startswith("T01"): draw_v1(img, "grass", 0 if "A" in tid else 1 if "B" in tid else 2)
        elif tid.startswith("T02"): draw_v1(img, "sea", 0 if "A" in tid else 1 if "B" in tid else 2)
        elif tid.startswith("T03"): draw_v1(img, "sand", 0 if "A" in tid else 1 if "B" in tid else 2)
        elif tid.startswith("T04"):
            draw_v1(img, "rock_red" if "A" in tid else "rock_ore" if "B" in tid else "rock_lava", 0)
        elif tid.startswith("T05"):
            draw_v1(img, "jungle" if "A" in tid else "jungle" if "B" in tid else "jungle_miasma", 0)
        elif tid.startswith("T06"): draw_v1(img, "mtn_green", 0 if "A" in tid else 1 if "B" in tid else 2)
        elif tid.startswith("T07"): draw_v1(img, "marsh", 0 if "A" in tid else 1 if "B" in tid else 2)
        elif tid == "T08": draw_v1(img, "volcano", 0)
        elif tid == "T09": draw_v1(img, "snow", 0)
        elif tid == "T10": draw_v1(img, "ghost", 0)
        elif tid == "T11": draw_v1(img, "spirit", 0)

        # Layer 1 decor
        elif tid == "D01": draw_v2(img, "grass", "flowers")
        elif tid == "D02": draw_v2(img, "grass", "mushrooms")
        elif tid == "D03": draw_v2(img, "grass_dry", None)
        elif tid == "D07": draw_v2(img, "sand", "cactus")
        elif tid == "D08": draw_v2(img, "sand", "bones")
        elif tid == "D09": draw_v2(img, "sand", "crystal")
        elif tid == "D23": draw_v2(img, "grass_dry", "ruins")

        # Layer 2 rare
        elif tid == "R01": draw_v2(img, "grass", "treasure")
        elif tid == "R06": draw_v2(img, "mtn_green", "hermit_hut")
        elif tid == "R09": draw_v2(img, "spirit", "spring")
        elif tid == "R03": draw_v2(img, "sand", "crystal")

        # Layer 3 landmark
        elif tid == "L01": draw_v3(img, "sand", "palace")
        elif tid == "L02": draw_v3(img, "grass", "temple")
        elif tid == "L03" or tid == "L08": draw_v3(img, "rock_red", "gate_wall")
        elif tid == "L04" or tid == "L07": draw_v3(img, "mtn_green", "temple")
        elif tid == "L12" or tid == "L20": draw_v3(img, "marsh", "market")
        elif tid == "L11" or tid == "L14": draw_v3(img, "rock_ore", "forge")
        elif tid == "L15" or tid == "L17": draw_v3(img, "rock_lava", "cave")
        elif tid == "L06" or tid == "L19": draw_v3(img, "ghost", "stele")

        # Border
        elif tid.startswith("B01"): draw_v1(img, "road", 0)
        elif tid.startswith("B02"): draw_v1(img, "gate", 0)
        elif tid.startswith("B03"): draw_v1(img, "realm_gate", 0)

        path = os.path.join(OUT, f"{tid}.png")
        img.save(path)

    print(f"Generated {len(prompts)} procedural tiles → {OUT}")

if __name__ == "__main__":
    main()
