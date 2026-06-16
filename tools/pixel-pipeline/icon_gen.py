# -*- coding: utf-8 -*-
"""九野像素图标生成器 v2（遵 PIXEL_RULES §2-6）：调色板 ramp + 定向光影(左上) +
选择性轮廓 selout + 斜角卡框 bevel + 抖动 + 精修多件母题。48×48 native。确定性。"""
import os, math, hashlib, sys
from PIL import Image, ImageDraw, ImageFont
sys.stdout.reconfigure(encoding="utf-8")
OUT = os.path.join(os.path.dirname(__file__), "out"); os.makedirs(OUT, exist_ok=True)
N = 48; SCALE = 4

# —— 调色板 ramp(暗→亮, hue-shift: 暗偏冷降明, 亮偏暖升明) ——
RAMP = {
    "steel":  [(34,38,50),(78,86,104),(126,138,158),(178,190,208),(224,232,244)],
    "iron":   [(40,38,44),(86,80,90),(132,128,140),(180,178,190),(222,222,230)],
    "fire":   [(70,20,18),(150,46,28),(214,86,32),(240,150,52),(252,210,120)],
    "ice":    [(24,52,74),(40,104,140),(78,158,198),(140,206,232),(206,240,250)],
    "thunder":[(70,52,12),(150,116,22),(216,176,40),(244,216,86),(252,242,170)],
    "wood":   [(26,54,32),(48,104,58),(82,152,82),(132,192,118),(196,228,168)],
    "purple": [(38,24,56),(78,46,108),(122,80,164),(168,128,202),(212,186,236)],
    "gold":   [(74,48,14),(150,104,28),(214,160,48),(244,202,96),(252,234,168)],
    "blood":  [(54,12,18),(112,24,32),(166,40,48),(206,78,80),(236,140,134)],
    "bone":   [(60,56,48),(118,112,98),(170,164,148),(210,206,192),(240,238,228)],
    "rose":   [(70,28,46),(140,52,86),(196,92,132),(228,144,176),(248,200,220)],
    "dark":   [(20,16,24),(48,40,56),(82,70,92),(120,106,134),(160,148,176)],
    "azure":  [(22,40,70),(40,84,140),(72,134,200),(126,182,228),(190,222,248)],
}
def ramp(m): return RAMP[m]

# —— Sprite 缓冲 + 光影原语 ——
class Spr:
    def __init__(s, n=N):
        s.n = n; s.img = Image.new("RGBA", (n, n), (0,0,0,0)); s.px = s.img.load()
    def put(s, x, y, col):
        if 0 <= x < s.n and 0 <= y < s.n: s.px[x, y] = col + (255,) if len(col)==3 else col
    def get(s, x, y):
        if 0 <= x < s.n and 0 <= y < s.n: return s.px[x, y]
        return (0,0,0,0)
    def solid(s, x, y): return s.get(x, y)[3] > 0
    def disk(s, cx, cy, r, m):
        rp = ramp(m)
        for y in range(int(cy-r-1), int(cy+r+2)):
            for x in range(int(cx-r-1), int(cx+r+2)):
                d = math.hypot(x-cx, y-cy)
                if d <= r:
                    # 左上亮右下暗: 按法线·光向(-1,-1)选 ramp 阶
                    nx, ny = (x-cx)/(r+1e-6), (y-cy)/(r+1e-6)
                    lit = -(nx+ny)/1.414
                    k = 2 + round(lit*1.6)  # mid±
                    k = max(1, min(4, k))
                    if d > r-1: k = max(0, k-1)  # 边缘压暗
                    s.put(x, y, rp[k])
    def rect(s, x0, y0, x1, y1, m, shade=True):
        rp = ramp(m)
        for y in range(y0, y1+1):
            for x in range(x0, x1+1):
                k = 2
                if shade:
                    if x==x0 or y==y0: k = 3
                    if x==x1 or y==y1: k = 1
                    if x==x0 and y==y0: k = 4
                    if x==x1 and y==y1: k = 0
                s.put(x, y, rp[max(0,min(4,k))])
    def line(s, x0, y0, x1, y1, m, k=2, w=1):
        rp = ramp(m); steps = max(abs(x1-x0), abs(y1-y0), 1)
        for i in range(steps+1):
            x = round(x0+(x1-x0)*i/steps); y = round(y0+(y1-y0)*i/steps)
            for dx in range(w):
                for dy in range(w): s.put(x+dx, y+dy, rp[k])
    def selout(s):
        # 选择性轮廓: 实心像素的空心邻居 → 该处材质最暗色(带色相)
        outline = []
        for y in range(s.n):
            for x in range(s.n):
                if not s.solid(x, y):
                    for dx,dy in ((1,0),(-1,0),(0,1),(0,-1)):
                        if s.solid(x+dx, y+dy):
                            c = s.get(x+dx, y+dy); outline.append((x,y,c)); break
        for x,y,c in outline:
            # 取近似最暗(直接压暗该邻色)
            s.put(x, y, (max(0,c[0]//3), max(0,c[1]//3), max(0,c[2]//3)))

def seeded(t): return int.from_bytes(hashlib.sha256(t.encode()).digest()[:4],"big")

# —— 卡框(斜角 bevel + 品阶宝石) ——
def frame(spr, accent, tier):
    rp = ramp(accent); bg = (38,36,48)
    for y in range(N):
        for x in range(N):
            spr.put(x, y, bg)
    # bevel: 外暗→内亮(左上) / 内暗(右下)
    for i,(kl,kr) in enumerate([(0,0),(3,1)]):
        x0=1+i; x1=N-2-i; y0=1+i; y1=N-2-i
        spr.line(x0,y0,x1,y0,accent,kl); spr.line(x0,y0,x0,y1,accent,kl)
        spr.line(x1,y0,x1,y1,accent,kr); spr.line(x0,y1,x1,y1,accent,kr)
    # 内陷画布
    spr.rect(4,4,N-5,N-5,"dark",shade=False)
    for y in range(5,N-5):
        for x in range(5,N-5): spr.put(x,y,(30,28,38))
    # 角饰
    for cx,cy in ((3,3),(N-4,3),(3,N-4),(N-4,N-4)): spr.put(cx,cy,rp[4])
    # 品阶宝石(底中, tier 1-5 → 数量+亮)
    t = max(1,min(5,tier)); gx = N//2 - (t-1)*3
    for i in range(t):
        x = gx + i*6; y = N-6
        spr.disk(x, y, 2.2, accent)

# —— 母题(精修, 多件带光影; 形由 archetype, 色由 ramp) ——
def motif(spr, arch, m, ac):
    cx = N//2
    if arch == "blade":
        spr.rect(cx-2,8,cx+1,32,m)                       # 刃
        spr.line(cx-2,8,cx-2,32,m,4); spr.line(cx+1,8,cx+1,32,m,1)  # fuller 光暗
        spr.line(cx,9,cx,31,m,4)
        spr.rect(cx-6,32,cx+5,35,ac)                     # 护手
        spr.rect(cx-1,35,cx,42,"iron")                   # 柄
        spr.disk(cx-0.5,43,2,ac)                         # 柄首
        spr.line(cx-1,8,cx,7,m,4)                        # 尖
    elif arch == "fist":
        spr.rect(cx-8,18,cx+7,32,m)
        for i in range(4): spr.line(cx-8,20+i*3,cx+7,20+i*3,m,1)  # 指节
        spr.rect(cx-8,18,cx-8,32,m); spr.rect(cx-8,18,cx+7,18,m)
        spr.disk(cx-4,16,2,ac)
    elif arch == "orb":
        spr.disk(cx,24,11,m); spr.disk(cx-3,20,3.2,ac)   # 球 + 高光
        for a in range(0,360,90):
            spr.put(cx+round(13*math.cos(math.radians(a))), 24+round(13*math.sin(math.radians(a))), ramp(ac)[4])
    elif arch == "wisp":
        for i,y in enumerate(range(10,38,3)):
            x = cx + (4 if i%2 else -4); spr.disk(x,y,3.2-i*0.15,m)
        spr.disk(cx,10,4,ac); spr.put(cx-1,9,(20,20,24)); spr.put(cx+2,9,(20,20,24))  # 鬼面
    elif arch == "flask":
        spr.rect(cx-3,10,cx+2,16,"ice")                  # 颈
        for y in range(16,36):
            w = 4 + (y-16)//2
            spr.line(cx-w,y,cx+w,y,m,2)
        spr.disk(cx,28,5,ac); spr.line(cx-7,34,cx+6,34,m,1)
    elif arch == "anvil":
        spr.rect(cx-10,20,cx+9,26,m); spr.rect(cx-5,26,cx+4,34,m)
        spr.line(cx-10,20,cx-15,18,m,3); spr.disk(cx+11,22,2,ac)  # 角+火星
    elif arch == "grid":
        for x in (cx-9,cx-3,cx+3,cx+9): spr.line(x,10,x,38,m,3 if x<cx else 1)
        for y in (12,20,28,36): spr.line(cx-10,y,cx+10,y,ac,2)
        spr.disk(cx,24,2.5,ac)                            # 阵眼
    elif arch == "eye":
        spr.disk(cx,24,12,ac)
        for x in range(cx-12,cx+13):                      # 杏眼裁切
            for y in range(cx-12,cx+13): pass
        spr.disk(cx,24,6,m); spr.disk(cx,24,3,(18,16,22)); spr.disk(cx-1,22,1.2,(230,230,240))
    elif arch == "bolt":
        spr.line(cx+4,8,cx-4,24,m,4,2); spr.line(cx-4,24,cx+3,24,m,3); spr.line(cx+3,24,cx-5,40,m,2,2)
        spr.put(cx+4,8,ramp(ac)[4])
    elif arch == "halo":
        for a in range(0,360,30):
            x=cx+round(15*math.cos(math.radians(a))); y=24+round(15*math.sin(math.radians(a))); spr.disk(x,y,1.3,ac)
        spr.disk(cx,24,9,m); spr.disk(cx,24,6,"gold"); spr.disk(cx-2,21,2,ramp("gold")[4])  # 金身
    elif arch == "star":
        pts=[]
        for k in range(10):
            r=13 if k%2==0 else 6; a=math.radians(-90+k*36); pts.append((cx+r*math.cos(a),24+r*math.sin(a)))
        # 填充(扫描) + 中心宝石
        xs=[p[0] for p in pts]; ys=[p[1] for p in pts]
        d=ImageDraw.Draw(spr.img); d.polygon([(round(x),round(y)) for x,y in pts], fill=ramp(m)[2])
        spr.disk(cx,24,3,ac)
        spr.line(round(pts[0][0]),round(pts[0][1]),cx,24,m,4)
    elif arch == "paw":
        spr.disk(cx,28,6,m)
        for x in (cx-6,cx-1,cx+4): spr.disk(x,18,2.6,ac)
        spr.disk(cx+7,22,2,ac)
    elif arch == "scroll":
        spr.rect(cx-9,12,cx+8,34,"bone")
        spr.rect(cx-10,10,cx+9,13,m); spr.rect(cx-10,33,cx+9,36,m)
        for y in (18,22,26,30): spr.line(cx-6,y,cx+5,y,ac,2)
    elif arch == "horns":
        spr.line(cx-3,30,cx-9,8,m,3,2); spr.line(cx-9,8,cx-5,16,m,2)
        spr.line(cx+3,30,cx+9,8,m,3,2); spr.line(cx+9,8,cx+5,16,m,2)
        spr.disk(cx,28,5,ac); spr.put(cx-2,27,(220,60,60)); spr.put(cx+2,27,(220,60,60))
    elif arch == "claw":
        for i,x in enumerate((cx-7,cx-1,cx+5)):
            spr.line(x,8,x-3,34,m,3,2); spr.disk(x-3,34,1.6,ac)
        spr.line(cx-9,7,cx+7,7,ac,2)
    elif arch == "drop":
        d=ImageDraw.Draw(spr.img); d.polygon([(cx,8),(cx+11,28),(cx,40),(cx-11,28)], fill=ramp(m)[2])
        spr.disk(cx,30,5,m); spr.disk(cx-3,26,2,ramp(ac)[4])
    elif arch == "bug":
        spr.disk(cx,26,8,m); spr.disk(cx,16,4,m)
        for y in (20,26,32):
            spr.line(cx-7,y,cx-14,y-3,ac,2); spr.line(cx+7,y,cx+14,y-3,ac,2)
        spr.put(cx-2,14,(230,60,60)); spr.put(cx+2,14,(230,60,60))
    elif arch == "talisman":
        spr.rect(cx-7,6,cx+6,40,"bone"); spr.line(cx,9,cx,37,m,1,2)
        for y in (14,20,26,32): spr.line(cx-4,y,cx+4,y,m,1)
        spr.disk(cx,12,2,ac)
    elif arch == "puppet":
        spr.line(cx,8,cx,30,"bone",3,2); spr.line(cx-11,14,cx+11,14,"bone",3,2)
        for p in ((cx,8),(cx-11,14),(cx+11,14),(cx,30)): spr.disk(p[0],p[1],2.4,ac)
        for p in ((cx,8),(cx-11,14),(cx+11,14)): spr.line(p[0],0,p[0],p[1],m,1)  # 线
    elif arch == "note":
        spr.disk(cx-5,32,5,m); spr.line(cx,30,cx,10,m,4,2); spr.line(cx,10,cx+8,13,ac,3,2)
        spr.disk(cx+9,26,3,m)
    elif arch == "yinyang":
        spr.disk(cx,24,12,m)
        d=ImageDraw.Draw(spr.img); d.pieslice([cx-12,12,cx+12,36],-90,90,fill=ramp(ac)[2])
        spr.disk(cx,18,6,ac); spr.disk(cx,30,6,m); spr.disk(cx,18,2,m); spr.disk(cx,30,2,ac)

PATHS = [
    ("sword_immortal","剑","blade","steel","ice",5),("ti_xiu_hengshi","体","fist","blood","bone",4),
    ("fa_xiu","法","orb","fire","azure",4),("gui_xiu_yang_hun","鬼","wisp","purple","blood",3),
    ("dan_xiu","丹","flask","fire","gold",4),("qixiu_artificer","器","anvil","steel","gold",4),
    ("array_formation","阵","grid","azure","wood",3),("soul_divine_sense","魂","eye","purple","azure",4),
    ("lei_xiu","雷","bolt","thunder","steel",5),("buddhist_golden_body","佛","halo","gold","fire",5),
    ("ming_fate_causality","命","star","azure","gold",4),("yu_shou","驭兽","paw","wood","bone",3),
    ("ru_xiu_haoran","儒","scroll","azure","gold",4),("mo_xiu_xinmo","魔","horns","blood","dark",4),
    ("yao_xiu_huaxing","妖","claw","wood","rose",3),("xue_xiu_xuesha","血","drop","blood","fire",4),
    ("du_gu_xiu","毒蛊","bug","wood","dark",3),("fu_xiu_fulu","符","talisman","gold","blood",3),
    ("kuilei_shi","傀儡","puppet","bone","steel",3),("yin_xiu_yuedao","音","note","rose","azure",3),
    ("yinguo_faze","因果","yinyang","purple","gold",5),
]
DIM = {"sword_immortal":"steel","ti_xiu_hengshi":"steel","fa_xiu":"steel","array_formation":"steel",
       "lei_xiu":"steel","buddhist_golden_body":"gold","yu_shou":"wood","mo_xiu_xinmo":"steel",
       "yao_xiu_huaxing":"steel","xue_xiu_xuesha":"steel","du_gu_xiu":"steel","fu_xiu_fulu":"steel",
       "kuilei_shi":"steel","gui_xiu_yang_hun":"purple","soul_divine_sense":"purple","ru_xiu_haoran":"purple",
       "yin_xiu_yuedao":"purple","ming_fate_causality":"gold","yinguo_faze":"gold","dan_xiu":"wood","qixiu_artificer":"wood"}

def make(p):
    pid,name,arch,m,ac,tier = p
    spr = Spr()
    frame(spr, DIM[pid], tier)
    motif(spr, arch, m, ac)
    spr.selout()
    return spr.img

def font(sz):
    for fp in ("C:/Windows/Fonts/msyh.ttc","C:/Windows/Fonts/simhei.ttf"):
        if os.path.exists(fp):
            try: return ImageFont.truetype(fp, sz)
            except Exception: pass
    return ImageFont.load_default()

def sheet():
    cols,rows=7,3; cell=N*SCALE; pad=10; lab=20
    W=cols*(cell+pad)+pad; H=rows*(cell+lab+pad)+pad+30
    sh=Image.new("RGBA",(W,H),(26,24,32,255)); d=ImageDraw.Draw(sh)
    d.text((pad,8),"九野 · 21 路图标 v2(ramp+定向光影+selout+斜角框+精修母题)",font=font(18),fill=(232,224,196))
    for i,p in enumerate(PATHS):
        r,c=divmod(i,cols); x=pad+c*(cell+pad); y=36+r*(cell+lab+pad)
        ic=make(p).resize((cell,cell),Image.NEAREST); sh.paste(ic,(x,y),ic)
        d.text((x+2,y+cell+1),f"{p[1]} {p[0]}",font=font(14),fill=(198,194,180))
    out=os.path.join(OUT,"path_icons_v2.png"); sh.save(out); return out,sh.size

if __name__=="__main__":
    o,s=sheet(); print("v2 icons:",o,s)
