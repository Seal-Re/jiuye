# -*- coding: utf-8 -*-
"""AI出图后处理（PARTS_CONTRACT §5）：高清 AI 图(如 1254² RGBA) → 32×48 像素部件。
流程: 抠残留底 → trim 透明边裁主体 → downscale 到目标格(保比例) → 调色板量化(贴 RAMP) →
      锚点底中对齐贴到 32×48 透明画布。供 char_gen 部件库用。
run: python pixel/postprocess.py <输入图> [--out <输出> --w 32 --h 48 --anchor bottom|center --ramp <名>]"""
import os, sys, argparse, math
from PIL import Image

# 复用 char_gen 的 RAMP（量化目标调色板，保成套统一）
sys.path.insert(0, os.path.dirname(__file__))
try:
    from char_gen import RAMP
except Exception:
    RAMP = {"steel":[(34,38,50),(78,86,104),(126,138,158),(178,190,208),(224,232,244)]}

def alpha_or_keycolor(im, bg_tol=18):
    """若图已有有效 alpha 用之；否则把近白/近黑边缘当背景抠掉(AI 常出白底/黑底)。"""
    im = im.convert("RGBA"); px = im.load(); w,h = im.size
    # 检测是否已有透明
    has_alpha = any(px[x,y][3] < 250 for x in range(0,w,7) for y in range(0,h,7))
    if has_alpha: return im
    # 取四角众色作背景键（白底/黑底/纯色底）
    corners = [px[0,0],px[w-1,0],px[0,h-1],px[w-1,h-1]]
    bg = max(set(corners), key=corners.count)
    out = Image.new("RGBA",(w,h),(0,0,0,0)); opx = out.load()
    for y in range(h):
        for x in range(w):
            r,g,b,a = px[x,y]
            if abs(r-bg[0])+abs(g-bg[1])+abs(b-bg[2]) <= bg_tol*3: continue  # 背景→透明
            opx[x,y] = (r,g,b,255)
    return out

def trim(im):
    """裁掉四周全透明边，留主体 bbox。"""
    bbox = im.getbbox()
    return im.crop(bbox) if bbox else im

def downscale_fit(im, tw, th, pad=2):
    """等比缩放进 (tw-pad, th-pad) 框，NEAREST 保像素感（先大图高质量缩再 NEAREST 定格）。"""
    w,h = im.size; bw,bh = tw-pad, th-pad
    scale = min(bw/w, bh/h)
    nw,nh = max(1,round(w*scale)), max(1,round(h*scale))
    # 两步：LANCZOS 缩到接近目标(保细节)，再确保整数格
    small = im.resize((nw,nh), Image.LANCZOS)
    return small

def quantize_to_ramp(im, ramp_name):
    """把每像素按亮度映射到指定 RAMP 5 阶（palette-swap 友好/成套统一）。alpha 保留。"""
    if ramp_name not in RAMP: return im
    cols = RAMP[ramp_name]; px = im.load(); w,h = im.size
    out = Image.new("RGBA",(w,h),(0,0,0,0)); opx = out.load()
    for y in range(h):
        for x in range(w):
            r,g,b,a = px[x,y]
            if a < 8: continue
            lum = (r*30+g*59+b*11)//100
            k = min(4, lum*5//256)
            opx[x,y] = cols[k]+(255,)
    return out

def quantize_kmeans(im, k=12):
    """不指定 ramp 时：Pillow 自带量化减色(保留原色相,k 色)。"""
    rgb = Image.new("RGBA", im.size, (0,0,0,0)); rgb.paste(im,(0,0),im)
    q = im.convert("RGB").quantize(colors=k, method=Image.MEDIANCUT).convert("RGBA")
    # 把原 alpha 贴回
    qpx=q.load(); apx=im.load(); w,h=im.size
    out=Image.new("RGBA",(w,h),(0,0,0,0)); opx=out.load()
    for y in range(h):
        for x in range(w):
            if apx[x,y][3] >= 8: opx[x,y]=qpx[x,y][:3]+(255,)
    return out

def compose(part, tw, th, anchor):
    """把处理后的小图贴到 tw×th 透明画布，锚点对齐。"""
    canvas = Image.new("RGBA",(tw,th),(0,0,0,0))
    pw,ph = part.size
    x = (tw-pw)//2
    y = (th-ph) if anchor=="bottom" else (th-ph)//2
    canvas.paste(part,(x,max(0,y)),part)
    return canvas

def process(inp, out, tw, th, anchor, ramp):
    im = Image.open(inp)
    im = alpha_or_keycolor(im)
    im = trim(im)
    im = downscale_fit(im, tw, th)
    im = quantize_to_ramp(im, ramp) if ramp else quantize_kmeans(im)
    im = compose(im, tw, th, anchor)
    im.save(out)
    return im.size

if __name__=="__main__":
    ap = argparse.ArgumentParser()
    ap.add_argument("input")
    ap.add_argument("--out", default=None)
    ap.add_argument("--w", type=int, default=32)
    ap.add_argument("--h", type=int, default=48)
    ap.add_argument("--anchor", default="bottom", choices=["bottom","center"])
    ap.add_argument("--ramp", default=None, help="量化到 char_gen RAMP 某阶(如 steel/azure);留空=保色相 k-means 减色")
    a = ap.parse_args()
    out = a.out or os.path.splitext(a.input)[0] + f"_pp{a.w}x{a.h}.png"
    sz = process(a.input, out, a.w, a.h, a.anchor, a.ramp)
    sys.stdout.reconfigure(encoding="utf-8")
    print(f"后处理: {a.input} -> {out}  ({sz[0]}x{sz[1]}, anchor={a.anchor}, ramp={a.ramp or 'kmeans'})")
