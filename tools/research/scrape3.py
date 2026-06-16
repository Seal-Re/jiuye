# -*- coding: utf-8 -*-
"""Headless (no-window) scraper v3 — 网文套路/情节 for v1.2 戏剧系统.
Discovery (Bing+DDG) -> harvest gated 中文 URLs (知乎/龙空/起点/简书) -> stealth extract."""
import sys, os, re, json
from urllib.parse import quote, urlparse, parse_qs, unquote
sys.stdout.reconfigure(encoding="utf-8")
from playwright.sync_api import sync_playwright

ROOT = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(ROOT, "raw3"); os.makedirs(OUT, exist_ok=True)
UA = ("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 "
      "(KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36")
STEALTH = """
Object.defineProperty(navigator,'webdriver',{get:()=>undefined});
window.chrome={runtime:{}};
Object.defineProperty(navigator,'languages',{get:()=>['zh-CN','zh','en']});
Object.defineProperty(navigator,'plugins',{get:()=>[1,2,3,4,5]});
"""

QUERIES = [
    (1, "网文 套路 桥段 大全 情节 模板"),
    (1, "武侠 小说 情节 套路 复仇 夺嫡 恩怨"),
    (1, "修仙 网文 套路 扮猪吃虎 废柴逆袭 打脸"),
    (2, "武侠 桥段 奇遇 秘境 比武招亲 英雄救美"),
    (2, "修仙 小说 机缘 奇遇 走火入魔 闭关突破 桥段"),
    (3, "雪中悍刀行 剑来 情节 结构 恩怨 人物关系"),
    (3, "天龙八部 凡人修仙传 诛仙 冲突 人物关系网"),
    (4, "网文 冲突 设计 人物弧光 爽点 拆解"),
    (4, "龙空 起点 套路 桥段 讨论 网文 结构"),
    (4, "知乎 网文 套路 拆解 情节 设计"),
    (5, "故事 冲突 结构 起承转合 复仇 三幕 套路"),
    (5, "门派 师承 恩怨 江湖 设定 人物关系 网文"),
]

ALLOW = ("zhihu.com","zhuanlan.zhihu.com","lkong.com","lkong.net","qidian.com","yuewen.com",
         "jianshu.com","douban.com","baike.baidu.com","wikipedia.org","tvtropes.org",
         "sspai.com","36kr.com","sina.com","zongheng.com","tieba.baidu.com","woshipm.com",
         "cnblogs.com","csdn.net","163.com","qq.com","sohu.com","ifeng.com","gcores.com")

def host(u):
    try: return urlparse(u).netloc.lower()
    except Exception: return ""
def allowed(u):
    h = host(u); return any(a in h for a in ALLOW)

def bing(page, q):
    out = []
    try:
        page.goto("https://www.bing.com/search?ensearch=0&setlang=zh-cn&q=" + quote(q),
                  wait_until="domcontentloaded", timeout=25000)
        page.wait_for_timeout(1100)
        out = page.evaluate("""() => {const r=[];document.querySelectorAll('li.b_algo').forEach(li=>{const a=li.querySelector('h2 a');if(!a)return;const sn=li.querySelector('.b_caption p,.b_algoSlug,p');r.push({url:a.href,title:a.innerText,snippet:sn?sn.innerText:''});});return r;}""")
    except Exception as e: print("  bing err", repr(e)[:100])
    return out

def ddg(page, q):
    out = []
    try:
        page.goto("https://html.duckduckgo.com/html/?kl=cn-zh&q=" + quote(q),
                  wait_until="domcontentloaded", timeout=25000)
        page.wait_for_timeout(900)
        raw = page.evaluate("""() => {const r=[];document.querySelectorAll('.result__body,.web-result').forEach(d=>{const a=d.querySelector('a.result__a');if(!a)return;const sn=d.querySelector('.result__snippet');r.push({href:a.href,title:a.innerText,snippet:sn?sn.innerText:''});});return r;}""")
        for r in raw:
            u = r["href"]; m = parse_qs(urlparse(u).query).get("uddg")
            if m: u = unquote(m[0])
            out.append({"url":u,"title":r["title"],"snippet":r["snippet"]})
    except Exception as e: print("  ddg err", repr(e)[:100])
    return out

def dismiss(page):
    try: page.keyboard.press("Escape")
    except Exception: pass
    try:
        page.evaluate("""() => {['.Modal-closeButton','button.Button.Modal-closeButton','[aria-label="关闭"]','.SignFlow-close'].forEach(s=>{const e=document.querySelector(s);if(e){try{e.click();}catch(_){}}});document.querySelectorAll('.Modal-wrapper,.Modal-backdrop,.signFlowModal,.Modal,div[role="dialog"]').forEach(e=>{try{e.remove();}catch(_){}});document.documentElement.style.overflow='auto';document.body.style.overflow='auto';}""")
    except Exception: pass

def extract(page):
    return page.evaluate("""() => {const pick=(s)=>{for(const x of s){const n=document.querySelector(x);if(n&&n.innerText&&n.innerText.length>120)return n.innerText;}return'';};let t=pick(['.QuestionAnswer-content','.Post-RichTextContainer','.RichText','.AnswerCard','.d_post_content_main','#content_html','.article-content','article','main','#mw-content-text']);if(!t||t.length<200)t=document.body.innerText;return t;}""")

def clean(t):
    t = re.sub(r"\r","",t); t = re.sub(r"\n{3,}","\n\n",t)
    bad = ("出了一点问题","我们正在解决","点击按钮开始验证","系统检测到您的","请输入验证码")
    if sum(b in t for b in bad)>=1 and len(t)<400: return ""
    return t.strip()

def main():
    discovered = {}; digest = []
    with sync_playwright() as p:
        b = p.chromium.launch(headless=True, args=["--disable-blink-features=AutomationControlled","--no-sandbox"])
        ctx = b.new_context(user_agent=UA, locale="zh-CN", viewport={"width":1366,"height":900},
                            extra_http_headers={"Accept-Language":"zh-CN,zh;q=0.9,en;q=0.8"})
        ctx.add_init_script(STEALTH)
        ctx.route(re.compile(r"\.(png|jpg|jpeg|gif|webp|svg|ico|mp4|woff2?|ttf)(\?|$)"), lambda r: r.abort())
        page = ctx.new_page()
        print("=== PHASE1 discovery ===", flush=True)
        for angle, q in QUERIES:
            hits = bing(page, q)
            if len(hits) < 4: hits += ddg(page, q)
            kept = 0
            for h in hits:
                u = h.get("url","")
                if not u or not u.startswith("http") or not allowed(u) or u in discovered: continue
                discovered[u] = {"title":h.get("title","")[:200],"snippet":h.get("snippet","")[:600],"angle":angle,"q":q}; kept += 1
            print(f"[A{angle}] '{q[:22]}' hits={len(hits)} kept={kept}", flush=True)
        with open(os.path.join(OUT,"_discovery.json"),"w",encoding="utf-8") as f:
            json.dump(discovered, f, ensure_ascii=False, indent=2)
        print(f"discovered {len(discovered)} urls", flush=True)

        def prio(it):
            h = host(it[0]); sc = 0
            for i,k in enumerate(("zhihu.com","lkong","jianshu","douban","tvtropes","baike.baidu","qidian","woshipm","wikipedia")):
                if k in h: sc = 100 - i*5
            return -sc
        items = sorted(discovered.items(), key=prio)[:20]
        print("=== PHASE2 extraction ===", flush=True)
        for i,(u,meta) in enumerate(items):
            rec = {"url":u,"angle":meta["angle"],"title":meta["title"]}
            try:
                page.goto(u, wait_until="domcontentloaded", timeout=28000)
                page.wait_for_timeout(1400); dismiss(page)
                for _ in range(3): page.mouse.wheel(0,2600); page.wait_for_timeout(400)
                dismiss(page)
                txt = clean(extract(page)); rec["chars"] = len(txt)
                fp = os.path.join(OUT, f"p{i:02d}_{host(u).replace('.','_')}.md")
                body = txt if txt else "[GATED] snippet:\n"+meta.get("snippet","")
                with open(fp,"w",encoding="utf-8") as f:
                    f.write(f"# A{meta['angle']} {meta['title']}\nURL:{u}\nQ:{meta['q']}\n\n{body}")
                rec["file"]=fp; rec["gated"]=not txt
            except Exception as e: rec["error"]=repr(e)[:160]
            print(f"  [{i:02d}] A{rec['angle']} {host(u)[:24]} chars={rec.get('chars','-')} gated={rec.get('gated','-')} {rec.get('error','')}", flush=True)
            digest.append(rec)
        b.close()
    with open(os.path.join(OUT,"_digest.json"),"w",encoding="utf-8") as f:
        json.dump(digest, f, ensure_ascii=False, indent=2)
    print("\nDONE ->", OUT, flush=True)

if __name__ == "__main__":
    main()
