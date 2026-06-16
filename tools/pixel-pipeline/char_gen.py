# -*- coding: utf-8 -*-
"""九野像素角色拼装器 v1（PIXEL_RULES §8）：Z序分层合成 + palette-swap + 确定性派生。
32×48 native 立姿，锚点底中。装备层=path 派生（剑修背剑/丹修挂葫芦/器修浮法宝）。
body 用占位几何件（有机部件待 AI/手绘库，§0 边界）。纯整数确定性：同 (persona/path/realm/seed) 同输出。
左上光源（§3）。run: python pixel/char_gen.py"""
import os, math, hashlib, sys
from PIL import Image, ImageDraw, ImageFont
sys.stdout.reconfigure(encoding="utf-8")
OUT = os.path.join(os.path.dirname(__file__), "out"); os.makedirs(OUT, exist_ok=True)
W, H, SCALE = 32, 48, 8   # native 32×48, 放大 ×8 看清

# —— 调色板 ramp（暗→亮，5 阶），与 icon_gen 同源 ——
RAMP = {
    "steel":  [(34,38,50),(78,86,104),(126,138,158),(178,190,208),(224,232,244)],
    "skin":   [(120,72,54),(168,108,80),(206,150,116),(232,190,158),(248,222,196)],
    "black":  [(16,16,22),(40,40,52),(70,70,86),(104,104,124),(150,150,172)],
    "azure":  [(22,40,70),(40,84,140),(72,134,200),(126,182,228),(190,222,248)],
    "fire":   [(70,20,18),(150,46,28),(214,86,32),(240,150,52),(252,210,120)],
    "gold":   [(74,48,14),(150,104,28),(214,160,48),(244,202,96),(252,234,168)],
    "blood":  [(54,12,18),(112,24,32),(166,40,48),(206,78,80),(236,140,134)],
    "wood":   [(26,54,32),(48,104,58),(82,152,82),(132,192,118),(196,228,168)],
    "purple": [(38,24,56),(78,46,108),(122,80,164),(168,128,202),(212,186,236)],
    "bone":   [(60,56,48),(118,112,98),(170,164,148),(210,206,192),(240,238,228)],
    "jade":   [(20,56,48),(36,104,90),(64,156,132),(120,200,172),(196,238,222)],
    "ink":    [(24,24,30),(52,52,62),(86,86,100),(132,132,150),(186,186,202)],
}
def rp(m): return RAMP[m]

class Spr:
    def __init__(s, w=W, h=H):
        s.w, s.h = w, h; s.img = Image.new("RGBA",(w,h),(0,0,0,0)); s.px = s.img.load()
    def put(s,x,y,c,a=255):
        x,y=int(round(x)),int(round(y))
        if 0<=x<s.w and 0<=y<s.h: s.px[x,y]=(c[0],c[1],c[2],a)
    def solid(s,x,y):
        x,y=int(x),int(y)
        return 0<=x<s.w and 0<=y<s.h and s.px[x,y][3]>0
    # 竖向填充块（带左上亮右下暗的简单光影，§3）
    def fillrect(s,x0,y0,x1,y1,m,base=2):
        for y in range(int(y0),int(y1)+1):
            for x in range(int(x0),int(x1)+1):
                k=base
                if x==x0: k=min(4,base+1)      # 左缘亮
                if x==x1: k=max(0,base-1)       # 右缘暗
                if y==y0: k=min(4,k+1)          # 上缘亮
                if y==y1: k=max(0,k-1)          # 下缘暗
                s.put(x,y,rp(m)[max(0,min(4,k))])
    def disk(s,cx,cy,r,m,base=2):
        for y in range(int(cy-r-1),int(cy+r+2)):
            for x in range(int(cx-r-1),int(cx+r+2)):
                d=math.hypot(x-cx,y-cy)
                if d<=r:
                    nx,ny=(x-cx)/(r+1e-6),(y-cy)/(r+1e-6)
                    lit=-(nx+ny)/1.414
                    k=base+round(lit*1.4); k=max(0,min(4,int(k)))
                    if d>r-0.8: k=max(0,k-1)
                    s.put(x,y,rp(m)[k])
    def line(s,x0,y0,x1,y1,m,k=2,w=1):
        steps=max(abs(x1-x0),abs(y1-y0),1)
        for i in range(steps+1):
            x=x0+(x1-x0)*i/steps; y=y0+(y1-y0)*i/steps
            for dx in range(w):
                for dy in range(w): s.put(x+dx,y+dy,rp(m)[k])
    def selout(s):
        out=[]
        for y in range(s.h):
            for x in range(s.w):
                if not s.solid(x,y):
                    for dx,dy in((1,0),(-1,0),(0,1),(0,-1)):
                        if s.solid(x+dx,y+dy):
                            c=s.px[x+dx,y+dy]; out.append((x,y)); break
        for x,y in out: s.put(x,y,(14,12,18))

def seeded(t): return int.from_bytes(hashlib.sha256(t.encode()).digest()[:4],"big")

# —— 部件层（几何占位 body + path 装备件；锚点底中 cx=16, 脚 y≈44）——
CX = 16
def layer_body(spr, build):
    # 占位人体：头(disk) + 躯干(rect) + 腿。build 影响宽窄(武痴壮/游侠瘦)
    w = 5 if build=="brawny" else 4
    spr.disk(CX, 9, 4, "skin", base=3)             # 头
    spr.fillrect(CX-2, 13, CX+1, 14, "skin")       # 脖
    spr.fillrect(CX-w, 15, CX+w-1, 30, "bone")     # 躯干(占位,会被袍服盖)
    spr.fillrect(CX-w, 31, CX-1, 44, "bone")       # 左腿
    spr.fillrect(CX+1, 31, CX+w-1, 44, "bone")     # 右腿
def layer_hair(spr, seed):
    v = seed % 3
    spr.fillrect(CX-4, 5, CX+3, 7, "black")        # 顶发
    if v==0: spr.fillrect(CX-4,7,CX-4,11,"black"); spr.fillrect(CX+3,7,CX+3,11,"black")  # 鬓
    elif v==1: spr.fillrect(CX-4,7,CX+3,8,"black") # 短
    else: spr.fillrect(CX-5,6,CX-4,14,"black"); spr.fillrect(CX+3,6,CX+4,14,"black")     # 长发
def layer_face(spr):
    spr.put(CX-2,9,(20,18,24)); spr.put(CX+1,9,(20,18,24))   # 眼
def layer_robe(spr, robe_ramp):
    # 袍服盖躯干 + 下摆（门类配色 palette-swap）
    spr.fillrect(CX-5, 15, CX+4, 32, robe_ramp)
    spr.fillrect(CX-6, 32, CX+5, 40, robe_ramp, base=1)      # 下摆张开
    spr.line(CX, 16, CX, 39, robe_ramp, k=4)                # 衣襟高光
    spr.fillrect(CX-6, 16, CX-5, 26, robe_ramp, base=1)     # 左袖
    spr.fillrect(CX+5, 16, CX+6, 26, robe_ramp, base=1)     # 右袖
def layer_belt(spr, ac):
    spr.fillrect(CX-5, 27, CX+4, 28, ac)

# —— 装备层（path 派生，Z 序在 body/robe 之上）——
def equip_sword(spr, ac):       # 剑修：背剑（斜挂背后→画在身侧）
    spr.line(CX+6, 12, CX+9, 30, "steel", k=4, w=1)   # 剑身
    spr.line(CX+6, 13, CX+9, 31, "steel", k=2, w=1)
    spr.fillrect(CX+5, 29, CX+8, 30, ac)              # 护手
    spr.put(CX+9,30,rp(ac)[4]); spr.put(CX+6,12,rp("steel")[4])
def equip_gourd(spr, ac):       # 丹修：腰挂葫芦
    spr.disk(CX+7, 30, 3, "fire", base=3)
    spr.disk(CX+7, 25, 1.6, "fire", base=3)
    spr.line(CX+7, 23, CX+7, 22, "wood", k=1)
def equip_artifact(spr, ac):    # 器修：浮空法宝(头侧旋)
    spr.disk(CX-7, 12, 2.6, "gold", base=3)
    for a in (0,120,240):
        x=CX-7+round(4*math.cos(math.radians(a))); y=12+round(4*math.sin(math.radians(a)))
        spr.put(x,y,rp("gold")[4])
def equip_none(spr, ac): pass

# —— realm 光环（境界越高越炫，§8）——
def aura(spr, realm, ac):
    if realm < 3: return
    n = min(realm, 8)
    for a in range(0, 360, max(20, 60-n*5)):
        r = 17 + (1 if (a//30)%2 else 0)
        x = CX + r*math.cos(math.radians(a)); y = 24 + r*0.8*math.sin(math.radians(a))
        spr.put(x, y, rp(ac)[4], a=160)

# —— 角色档（pid, 名, 体型, 袍色, accent, 装备件, realm, seed名）——
EQUIP = {"sword":equip_sword, "gourd":equip_gourd, "artifact":equip_artifact, "none":equip_none}

# ════════════════════════════════════════════════════════════════════════
# 部件库加载（PARTS_CONTRACT.md）：parts/<层>/<款>_v<变体>.png 存在则加载,否则回退几何占位件。
# AI 出的部件按契约丢进 pixel/parts/ → 零改代码即被拼装。grayscale 袍服件运行时 palette-swap。
# ════════════════════════════════════════════════════════════════════════
PARTS = os.path.join(os.path.dirname(__file__), "parts")

def _find_part(layer_dir, style, seed):
    """在 parts/<layer_dir>/ 找 <style>_v*.png；多变体按 seed 确定性选。无→None(回退占位)。"""
    d = os.path.join(PARTS, layer_dir)
    if not os.path.isdir(d): return None
    cands = sorted(f for f in os.listdir(d) if f.startswith(style + "_v") and f.endswith(".png"))
    if not cands: return None
    return os.path.join(d, cands[seed % len(cands)])

def _paste_part(spr, path, recolor_ramp=None):
    """把部件 PNG 叠到 32×48 画布(契约同尺寸,锚点底中已对齐)。recolor_ramp!=None→灰阶映射到该 ramp(palette-swap)。"""
    part = Image.open(path).convert("RGBA")
    if part.size != (W, H):
        part = part.resize((W, H), Image.NEAREST)   # 契约要求 32×48,容错缩放
    ppx = part.load()
    rampcols = rp(recolor_ramp) if recolor_ramp else None
    for y in range(H):
        for x in range(W):
            r, g, b, a = ppx[x, y]
            if a < 8: continue
            if rampcols:                              # palette-swap: 灰度→ramp 5 阶
                lum = (r*30 + g*59 + b*11) // 100
                k = min(4, lum * 5 // 256)
                c = rampcols[k]
                spr.put(x, y, c, a)
            else:
                spr.put(x, y, (r, g, b), a)

def assemble(pid, name, build, robe, ac, equip, realm, seedkey):
    """优先加载 parts/ 库件(AI出料);缺件回退几何占位件。Z序合成,确定性。"""
    seed = seeded(seedkey)
    spr = Spr()
    aura(spr, realm, ac)                              # [0] 光环(程序生成,AI可选替换)
    # path→款式映射(契约 §3):款由 path 定,变体由 seed 定。
    robe_style = {"sword_immortal":"robe_sword","dan_xiu":"robe_alchemist",
                  "qixiu_artificer":"robe_artificer"}.get(pid, "robe_sword")
    # [1]body
    p = _find_part("1_body", "body_"+build, seed)
    if p: _paste_part(spr, p)
    else: layer_body(spr, build)
    # [3]robe(grayscale 件→palette-swap 套袍色)
    p = _find_part("3_robe", robe_style, seed)
    if p: _paste_part(spr, p, recolor_ramp=robe)
    else: (layer_robe(spr, robe), layer_belt(spr, ac))
    # [4]hair
    p = _find_part("4_hair", "hair", seed)
    if p: _paste_part(spr, p)
    else: layer_hair(spr, seed)
    # [5]face
    p = _find_part("5_face", "face", seed)
    if p: _paste_part(spr, p)
    else: layer_face(spr)
    # [7/8]装备(path派生);库件缺→几何占位
    p = _find_part("7_weapon", "weapon_"+equip, seed) or _find_part("8_accessory", "acc_"+equip, seed)
    if p: _paste_part(spr, p)
    else: EQUIP[equip](spr, ac)
    spr.selout()
    return spr.img

def _assemble_geometric(pid, name, build, robe, ac, equip, realm, seedkey):
    """纯几何占位件版本(无 parts/ 库时的当前画法,留作对照/回退基准)。"""
    seed = seeded(seedkey)
    spr = Spr()
    aura(spr, realm, ac)             # [9] 光环(底,半透)
    layer_body(spr, build)           # [0] 体型
    layer_robe(spr, robe)            # [2] 袍服
    layer_belt(spr, ac)              # 腰带
    layer_hair(spr, seed)            # [3] 头发
    layer_face(spr)                  # [4] 面部
    EQUIP[equip](spr, ac)            # [6/7/8] 装备(path派生)
    spr.selout()                     # 选择性轮廓(§4)
    return spr.img

# —— 样品角色(3 路 × 展示装备显示 + palette-swap) ——
CHARS = [
    ("sword_immortal","剑修·清逸","lean","azure","steel","sword",5,"char-sword-A"),
    ("dan_xiu","丹修·药翁","brawny","fire","gold","gourd",4,"char-dan-A"),
    ("qixiu_artificer","器修·御宝","lean","gold","steel","artifact",6,"char-qi-A"),
    # 同 path 换色/换 seed → 证 palette-swap 批量变体
    ("sword_immortal","剑修·墨衣","lean","ink","blood","sword",3,"char-sword-B"),
    ("sword_immortal","剑修·碧霄","brawny","jade","gold","sword",7,"char-sword-C"),
]

def font(sz):
    for fp in ("C:/Windows/Fonts/msyh.ttc","C:/Windows/Fonts/simhei.ttf"):
        if os.path.exists(fp):
            try: return ImageFont.truetype(fp, sz)
            except Exception: pass
    return ImageFont.load_default()

def sheet():
    cell_w, cell_h = W*SCALE, H*SCALE; pad=14; lab=22
    cols=len(CHARS)
    sheet_w = cols*(cell_w+pad)+pad
    sheet_h = cell_h+lab+pad+40
    sh=Image.new("RGBA",(sheet_w,sheet_h),(28,26,34,255)); d=ImageDraw.Draw(sh)
    d.text((pad,10),"九野 · 像素角色拼装 v1（分层合成 + 装备显示 + palette-swap，32×48 native ×8）",font=font(16),fill=(232,224,196))
    for i,c in enumerate(CHARS):
        x=pad+i*(cell_w+pad); y=40
        ic=assemble(*c).resize((cell_w,cell_h),Image.NEAREST)
        sh.paste(ic,(x,y),ic)
        d.text((x,y+cell_h+2),f"{c[1]}",font=font(14),fill=(214,208,190))
        d.text((x,y+cell_h+18),f"realm{c[6]} 装备:{c[5]}",font=font(11),fill=(170,166,152))
    out=os.path.join(OUT,"char_assembly_v1.png"); sh.save(out); return out,sh.size

if __name__=="__main__":
    o,s=sheet(); print("char assembly:",o,s)
