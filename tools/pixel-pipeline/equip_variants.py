# -*- coding: utf-8 -*-
"""装备品阶派生（AIGEN_TOOL §5.1）：1 件 AI 基底 → N 个品阶变体（凡/灵/仙…）。
程序化:色调偏移(凡=去饱和暗/灵=银蓝/仙=金) + 品阶光环边(realm 越高越亮)。确定性。
证"AI出种类基底,code派生品阶变体"——100件装备靠此从~20-30基底爆发。
run: python pixel/equip_variants.py <基底png> [--out-dir ...]"""
import os, sys, math
from PIL import Image
sys.stdout.reconfigure(encoding="utf-8")

# raw 产物默认落 tools/pixel-pipeline/_aigen/（已 gitignored）——锚定脚本自身位置，不随 CWD 变。
# 旧默认 ./pixel/_aigen 从仓库根运行会重建顶层 pixel/ 残留（未 gitignored），故改此。
_AIGEN = os.path.join(os.path.dirname(os.path.abspath(__file__)), "_aigen")

# 品阶定义: 名 → (色调乘子 RGB, 是否加光环边, 光环色)
TIERS = {
    "fan":  ((0.78,0.78,0.82), False, None),          # 凡品:去饱和压暗
    "ling": ((0.82,0.90,1.08), True,  (150,200,255)), # 灵器:偏银蓝 + 淡光
    "xian": ((1.12,1.02,0.72), True,  (255,220,120)), # 仙器:偏金 + 金光
}

def tint(im, mul):
    px = im.load(); w,h = im.size
    out = Image.new("RGBA",(w,h),(0,0,0,0)); opx = out.load()
    for y in range(h):
        for x in range(w):
            r,g,b,a = px[x,y]
            if a < 8: continue
            opx[x,y] = (min(255,int(r*mul[0])), min(255,int(g*mul[1])), min(255,int(b*mul[2])), a)
    return out

def glow_edge(im, color):
    """品阶光环:实心像素的透明邻居描一圈半透光色(高阶器华)。"""
    px = im.load(); w,h = im.size
    out = im.copy(); opx = out.load()
    for y in range(h):
        for x in range(w):
            if px[x,y][3] >= 8: continue
            for dx,dy in ((1,0),(-1,0),(0,1),(0,-1),(1,1),(-1,-1),(1,-1),(-1,1)):
                nx,ny=x+dx,y+dy
                if 0<=nx<w and 0<=ny<h and px[nx,ny][3]>=8:
                    opx[x,y]=(color[0],color[1],color[2],120); break
    return out

def derive(base_png, out_dir):
    os.makedirs(out_dir, exist_ok=True)
    base = Image.open(base_png).convert("RGBA")
    name = os.path.splitext(os.path.basename(base_png))[0]
    saved=[]
    for tier,(mul,glow,gcol) in TIERS.items():
        im = tint(base, mul)
        if glow: im = glow_edge(im, gcol)
        p = os.path.join(out_dir, f"{name}_{tier}.png")
        im.save(p); saved.append(p)
    return saved

if __name__=="__main__":
    import argparse
    ap=argparse.ArgumentParser(); ap.add_argument("base"); ap.add_argument("--out-dir",default=os.path.join(_AIGEN,"variants"))
    a=ap.parse_args()
    s=derive(a.base, a.out_dir)
    print(f"品阶派生: {a.base} → {len(s)} 变体({'/'.join(TIERS)}) → {a.out_dir}")
    for p in s: print("  ", os.path.basename(p))
