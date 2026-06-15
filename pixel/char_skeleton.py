# -*- coding: utf-8 -*-
"""九野像素角色 · 骨架驱动 v1（PIXEL_RULES §8 + AIGEN_TOOL §6.1）。
角色本体=程序化骨架(关节)驱动,逐帧一致可动画(idle/walk),非AI(AI逐帧骨架会漂移)。
部件(头/躯干/四肢/装备)按关节位置绘制 → 换姿势=换关节角度,骨架不变 → 动画一致。
palette-swap 门派色 + 装备挂载点(手/背)。确定性:同(persona/path/realm/seed/frame)同输出。
32×48 native,锚点底中,左上光源。run: python pixel/char_skeleton.py"""
import os, math, hashlib, sys
from PIL import Image, ImageDraw, ImageFont
sys.stdout.reconfigure(encoding="utf-8")
OUT = os.path.join(os.path.dirname(__file__), "out"); os.makedirs(OUT, exist_ok=True)
W, H, SCALE = 32, 48, 8

RAMP = {
    "skin":   [(120,72,54),(168,108,80),(206,150,116),(232,190,158),(248,222,196)],
    "black":  [(16,16,22),(40,40,52),(70,70,86),(104,104,124),(150,150,172)],
    "steel":  [(34,38,50),(78,86,104),(126,138,158),(178,190,208),(224,232,244)],
    "azure":  [(22,40,70),(40,84,140),(72,134,200),(126,182,228),(190,222,248)],
    "fire":   [(70,20,18),(150,46,28),(214,86,32),(240,150,52),(252,210,120)],
    "gold":   [(74,48,14),(150,104,28),(214,160,48),(244,202,96),(252,234,168)],
    "ink":    [(24,24,30),(52,52,62),(86,86,100),(132,132,150),(186,186,202)],
    "jade":   [(20,56,48),(36,104,90),(64,156,132),(120,200,172),(196,238,222)],
    "blood":  [(54,12,18),(112,24,32),(166,40,48),(206,78,80),(236,140,134)],
}
def rp(m): return RAMP[m]

class Spr:
    def __init__(s): s.img=Image.new("RGBA",(W,H),(0,0,0,0)); s.px=s.img.load()
    def put(s,x,y,c,a=255):
        x,y=int(round(x)),int(round(y))
        if 0<=x<W and 0<=y<H: s.px[x,y]=(c[0],c[1],c[2],a)
    def solid(s,x,y):
        x,y=int(x),int(y); return 0<=x<W and 0<=y<H and s.px[x,y][3]>0
    def disk(s,cx,cy,r,m,base=2):
        for y in range(int(cy-r-1),int(cy+r+2)):
            for x in range(int(cx-r-1),int(cx+r+2)):
                d=math.hypot(x-cx,y-cy)
                if d<=r:
                    nx,ny=(x-cx)/(r+1e-6),(y-cy)/(r+1e-6); lit=-(nx+ny)/1.414
                    k=max(0,min(4,base+round(lit*1.4)))
                    if d>r-0.8:k=max(0,k-1)
                    s.put(x,y,rp(m)[k])
    def limb(s,x0,y0,x1,y1,m,w=2,base=2):
        """画一节肢体(关节到关节的粗线),带光影。w=粗细。"""
        steps=max(abs(x1-x0),abs(y1-y0),1)
        for i in range(steps+1):
            x=x0+(x1-x0)*i/steps; y=y0+(y1-y0)*i/steps
            for dx in range(-(w//2),w//2+1):
                for dy in range(-(w//2),w//2+1):
                    k=base + (1 if dx<0 else (-1 if dx>0 else 0))  # 左亮右暗
                    s.put(x+dx,y+dy,rp(m)[max(0,min(4,k))])
    def selout(s):
        out=[]
        for y in range(H):
            for x in range(W):
                if not s.solid(x,y):
                    for dx,dy in((1,0),(-1,0),(0,1),(0,-1)):
                        if s.solid(x+dx,y+dy): out.append((x,y));break
        for x,y in out: s.put(x,y,(14,12,18))

def seeded(t): return int.from_bytes(hashlib.sha256(t.encode()).digest()[:4],"big")

# ════════════════════════════════════════════════════════════════════════
# 骨架（关节位置，相对 32×48 画布；锚点底中 脚=(16,46)）。
# 一个 pose = 一组关节坐标。动画 = 多个 pose 帧。骨架拓扑/比例固定 → 帧间一致。
# ════════════════════════════════════════════════════════════════════════
def skeleton(frame, build):
    """返回该帧的关节坐标 dict。frame: idle/walk 的相位。build 微调肩宽。"""
    cx = 16
    shoulder_w = 6 if build=="brawny" else 5
    hip_w = 4 if build=="brawny" else 3
    # idle 微呼吸 / walk 摆动(确定性,无随机)——靠 frame 相位
    sway = [0, -1, 0, 1][frame % 4]          # 躯干微摆
    leg_swing = [0, 2, 0, -2][frame % 4]     # 腿前后摆(walk)
    arm_swing = [0, -2, 0, 2][frame % 4]     # 臂反向摆
    return {
        "head":     (cx, 8),
        "neck":     (cx, 12),
        "chest":    (cx+sway, 16),
        "pelvis":   (cx+sway, 28),
        "shoulderL":(cx-shoulder_w+sway, 16), "shoulderR":(cx+shoulder_w+sway, 16),
        "handL":    (cx-shoulder_w-1+sway, 26+arm_swing), "handR":(cx+shoulder_w+1+sway, 26-arm_swing),
        "hipL":     (cx-hip_w, 28), "hipR":(cx+hip_w, 28),
        "footL":    (cx-hip_w, 44+leg_swing), "footR":(cx+hip_w, 44-leg_swing),
    }

# —— 按骨架画本体（部件相对关节，骨架变→姿势变，比例不变）——
def draw_body(spr, sk, skin, robe, hair_v, ac):
    # 腿(髋→脚)
    spr.limb(*sk["hipL"], *sk["footL"], robe, w=3, base=1)
    spr.limb(*sk["hipR"], *sk["footR"], robe, w=3, base=1)
    # 躯干(胸→盆,袍服主体)
    cx0=min(sk["chest"][0],sk["pelvis"][0]);
    spr.limb(sk["chest"][0],sk["chest"][1],sk["pelvis"][0],sk["pelvis"][1],robe,w=7,base=2)
    # 下摆张开
    for y in range(28,40):
        ww=4+(y-28)//2
        for x in range(sk["pelvis"][0]-ww, sk["pelvis"][0]+ww): spr.put(x,y,rp(robe)[1])
    # 臂(肩→手)
    spr.limb(*sk["shoulderL"], *sk["handL"], robe, w=2, base=2)
    spr.limb(*sk["shoulderR"], *sk["handR"], robe, w=2, base=2)
    spr.disk(*sk["handL"],1.4,skin,base=3); spr.disk(*sk["handR"],1.4,skin,base=3)
    # 腰带
    for x in range(sk["pelvis"][0]-5,sk["pelvis"][0]+5): spr.put(x,27,rp(ac)[3])
    # 头+发+脸
    hx,hy=sk["head"]
    spr.disk(hx,hy,4,skin,base=3)
    # 发(按 hair_v)
    spr.put(hx-4,hy-3,rp("black")[1]) ;
    for x in range(hx-4,hx+4): spr.put(x,hy-4,rp("black")[1]); spr.put(x,hy-3,rp("black")[2])
    if hair_v%2==0:
        for y in range(hy-4,hy+4): spr.put(hx-4,y,rp("black")[1]); spr.put(hx+3,y,rp("black")[1])  # 长发垂
    spr.disk(hx,hy-6,1.4,"black",base=2)  # 发髻
    spr.put(hx-2,hy,(20,18,24)); spr.put(hx+1,hy,(20,18,24))  # 眼

# —— 装备挂载点（手/背 = 骨架关节,装备跟着动）——
def mount_weapon(spr, sk, kind, ac):
    hx,hy = sk["handR"]   # 右手持武器
    if kind=="sword":
        spr.limb(hx,hy, hx+1,hy-14, "steel", w=1, base=4)   # 剑身从手向上
        for x in range(hx-2,hx+3): spr.put(x,hy,rp(ac)[3])  # 护手
    elif kind=="staff":
        spr.limb(hx,hy-8, hx,hy+10, "ink", w=1, base=2)
    elif kind=="none": pass
def mount_back(spr, sk, kind):
    cx,cy = sk["chest"]
    if kind=="gourd":
        spr.disk(cx+6,cy+12,2.4,"fire",base=3)              # 腰侧葫芦
    elif kind=="artifact":
        spr.disk(cx-7,cy-4,2.4,"gold",base=3)               # 浮空法宝
def aura(spr, sk, realm, ac):
    if realm<3: return
    n=min(realm,8)
    for a in range(0,360,max(20,60-n*5)):
        x=16+18*math.cos(math.radians(a)); y=24+15*math.sin(math.radians(a))
        spr.put(x,y,rp(ac)[4],a=150)

def render(pid,name,build,robe,ac,weapon,back,realm,seedkey,frame=0):
    seed=seeded(seedkey)
    spr=Spr()
    sk=skeleton(frame,build)
    aura(spr,sk,realm,ac)
    mount_back(spr,sk,back)        # 背挂(身后)
    draw_body(spr,sk,"skin",robe,seed%2,ac)
    mount_weapon(spr,sk,weapon,ac) # 手持(身前)
    spr.selout()
    return spr.img

CHARS=[
    ("sword_immortal","剑修","lean","azure","steel","sword","none",5,"sk-sword"),
    ("dan_xiu","丹修","brawny","fire","gold","none","gourd",4,"sk-dan"),
    ("qixiu_artificer","器修","lean","jade","gold","none","artifact",6,"sk-qi"),
]
def font(sz):
    for fp in ("C:/Windows/Fonts/msyh.ttc","C:/Windows/Fonts/simhei.ttf"):
        if os.path.exists(fp):
            try: return ImageFont.truetype(fp,sz)
            except: pass
    return ImageFont.load_default()

def sheet():
    """每角色一行,横排 4 帧(idle/walk 动画帧)→ 证骨架驱动逐帧一致。"""
    cw,ch=W*SCALE,H*SCALE; pad=12; lab=20; frames=4
    cols=frames; rows=len(CHARS)
    sw=pad+cols*(cw+pad); sh_h=40+rows*(ch+lab+pad)
    sh=Image.new("RGBA",(sw,sh_h),(28,26,34,255)); d=ImageDraw.Draw(sh)
    d.text((pad,10),"九野 · 骨架驱动角色 v1（程序化本体·逐帧一致·可动画 / AI只出静态挂件立绘）",font=font(15),fill=(232,224,196))
    for r,c in enumerate(CHARS):
        for f in range(frames):
            x=pad+f*(cw+pad); y=40+r*(ch+lab+pad)
            ic=render(*c,frame=f).resize((cw,ch),Image.NEAREST); sh.paste(ic,(x,y),ic)
            d.text((x,y+ch+2),f"{c[1]} f{f}",font=font(12),fill=(200,196,182))
    out=os.path.join(OUT,"char_skeleton_v1.png"); sh.save(out); return out,sh.size

if __name__=="__main__":
    o,s=sheet(); print("skeleton char:",o,s)
