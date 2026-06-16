# -*- coding: utf-8 -*-
"""Headless (no-window) research scraper v2.
Phase1 discovery: Bing + DuckDuckGo (bot-tolerant) -> harvest real content URLs + snippets.
Phase2 extraction: stealth-visit top URLs, dismiss modals, extract text.
Snippets alone are saved (signal even if a page stays gated)."""
import sys, os, re, json
from urllib.parse import quote, urlparse, parse_qs, unquote
sys.stdout.reconfigure(encoding="utf-8")
from playwright.sync_api import sync_playwright

ROOT = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(ROOT, "raw2"); os.makedirs(OUT, exist_ok=True)
UA = ("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 "
      "(KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36")
STEALTH = """
Object.defineProperty(navigator,'webdriver',{get:()=>undefined});
window.chrome={runtime:{}};
Object.defineProperty(navigator,'languages',{get:()=>['zh-CN','zh','en']});
Object.defineProperty(navigator,'plugins',{get:()=>[1,2,3,4,5]});
"""

# angle -> queries
QUERIES = [
    (1, "武侠 文字游戏 设计 玩法 框架"),
    (1, "MUD 武侠 文字 网游 北大侠客行 设计"),
    (2, "武侠 跑团 TRPG 规则书 门派 武功 属性"),
    (2, "江湖 桌游 角色卡 骰子 机制"),
    (3, "武侠 网文 江湖 门派 功法 世界观 套路 设定"),
    (3, "雪中悍刀行 剑来 武功 体系 设定"),
    (4, "百度贴吧 武侠 文字游戏 角色扮演 玩法"),
    (5, "太吾绘卷 角色 随机生成 机制 数值"),
    (5, "鬼谷八荒 命格 随机 属性 上限 设计"),
    (5, "侠客风云传 属性 数值 设计 随机 上限"),
    (6, "生成式智能体 generative agents NPC 游戏 论文"),
    (6, "大模型 LLM 驱动 NPC 角色扮演 人设 prompt 框架"),
]

ALLOW = ("zhihu.com","tieba.baidu.com","baike.baidu.com","wikipedia.org",
         "gcores.com","indienova.com","bilibili.com","github.com","csdn.net",
         "juejin","sina.com","36kr.com","sspai.com","gamersky.com","ali213",
         "zhuanlan.zhihu.com","jianshu.com","cnblogs.com","arxiv.org","qq.com")

def host(u):
    try: return urlparse(u).netloc.lower()
    except Exception: return ""

def allowed(u):
    h = host(u)
    return any(a in h for a in ALLOW)

def bing_search(page, q):
    out = []
    try:
        page.goto("https://www.bing.com/search?ensearch=0&setlang=zh-cn&q="+quote(q),
                  wait_until="domcontentloaded", timeout=25000)
        page.wait_for_timeout(1200)
        out = page.evaluate("""() => {
            const res=[];
            document.querySelectorAll('li.b_algo').forEach(li=>{
                const a=li.querySelector('h2 a'); if(!a) return;
                const sn=li.querySelector('.b_caption p, .b_algoSlug, p');
                res.push({url:a.href, title:a.innerText, snippet:sn?sn.innerText:''});
            });
            return res;
        }""")
    except Exception as e:
        print("  bing err", repr(e)[:120])
    return out

def ddg_search(page, q):
    out = []
    try:
        page.goto("https://html.duckduckgo.com/html/?kl=cn-zh&q="+quote(q),
                  wait_until="domcontentloaded", timeout=25000)
        page.wait_for_timeout(1000)
        raw = page.evaluate("""() => {
            const res=[];
            document.querySelectorAll('.result__body, .web-result').forEach(d=>{
                const a=d.querySelector('a.result__a'); if(!a) return;
                const sn=d.querySelector('.result__snippet');
                res.push({href:a.href, title:a.innerText, snippet:sn?sn.innerText:''});
            });
            return res;
        }""")
        for r in raw:
            u = r["href"]
            m = parse_qs(urlparse(u).query).get("uddg")
            if m: u = unquote(m[0])
            out.append({"url":u, "title":r["title"], "snippet":r["snippet"]})
    except Exception as e:
        print("  ddg err", repr(e)[:120])
    return out

def dismiss(page):
    try: page.keyboard.press("Escape")
    except Exception: pass
    try:
        page.evaluate("""() => {
            ['.Modal-closeButton','button.Button.Modal-closeButton','[aria-label="关闭"]','.SignFlow-close']
              .forEach(s=>{const e=document.querySelector(s); if(e){try{e.click();}catch(_){}}});
            document.querySelectorAll('.Modal-wrapper,.Modal-backdrop,.signFlowModal,.Modal,div[role="dialog"]')
              .forEach(e=>{try{e.remove();}catch(_){}});
            document.documentElement.style.overflow='auto'; document.body.style.overflow='auto';
        }""")
    except Exception: pass

def extract(page):
    return page.evaluate("""() => {
        const pick=(sels)=>{for(const s of sels){const n=document.querySelector(s); if(n&&n.innerText&&n.innerText.length>120) return n.innerText;} return '';};
        let t=pick(['.QuestionAnswer-content','.Post-RichTextContainer','.RichText','.AnswerCard',
                    '.d_post_content_main','#content_html','.article-content','.main-content',
                    '#mw-content-text','.lemma-summary','article','main']);
        if(!t||t.length<200) t=document.body.innerText;
        return t;
    }""")

def clean(t):
    t=re.sub(r"\r","",t); t=re.sub(r"\n{3,}","\n\n",t)
    bad=("出了一点问题","我们正在解决","点击按钮开始验证","去往首页","系统检测到您的","请输入验证码")
    if sum(b in t for b in bad)>=1 and len(t)<400: return ""  # bot page
    return t.strip()

def main():
    discovered={}  # url -> {title,snippet,angle,src}
    digest=[]
    with sync_playwright() as p:
        browser=p.chromium.launch(headless=True, args=["--disable-blink-features=AutomationControlled","--no-sandbox"])
        ctx=browser.new_context(user_agent=UA, locale="zh-CN", viewport={"width":1366,"height":900},
                                extra_http_headers={"Accept-Language":"zh-CN,zh;q=0.9,en;q=0.8"})
        ctx.add_init_script(STEALTH)
        ctx.route(re.compile(r"\.(png|jpg|jpeg|gif|webp|svg|ico|mp4|woff2?|ttf)(\?|$)"), lambda r: r.abort())
        page=ctx.new_page()

        # ---- Phase 1: discovery ----
        print("=== PHASE1 discovery ===", flush=True)
        for angle,q in QUERIES:
            hits = bing_search(page, q)
            if len(hits) < 3:
                hits += ddg_search(page, q)
            kept=0
            for h in hits:
                u=h.get("url","")
                if not u or not u.startswith("http"): continue
                if not allowed(u): continue
                if u in discovered: continue
                discovered[u]={"title":h.get("title","")[:200],"snippet":h.get("snippet","")[:600],
                               "angle":angle,"q":q}
                kept+=1
            print(f"[A{angle}] '{q[:24]}' bing+ddg={len(hits)} kept={kept}", flush=True)

        # save discovery (snippets = signal even if pages gated)
        with open(os.path.join(OUT,"_discovery.json"),"w",encoding="utf-8") as f:
            json.dump(discovered,f,ensure_ascii=False,indent=2)
        print(f"discovered {len(discovered)} urls", flush=True)

        # ---- Phase 2: extraction (cap, prioritize zhihu/tieba/gcores/indienova/baike/wiki) ----
        def prio(item):
            u=item[0]; h=host(u)
            score=0
            for i,k in enumerate(("zhihu.com","gcores.com","indienova.com","tieba.baidu.com",
                                  "baike.baidu.com","wikipedia.org","sspai.com","arxiv.org","github.com")):
                if k in h: score=100-i*5
            return -score
        items=sorted(discovered.items(), key=prio)[:18]
        print("=== PHASE2 extraction ===", flush=True)
        for i,(u,meta) in enumerate(items):
            rec={"url":u,"angle":meta["angle"],"title":meta["title"]}
            try:
                page.goto(u, wait_until="domcontentloaded", timeout=28000)
                page.wait_for_timeout(1500); dismiss(page)
                for _ in range(3): page.mouse.wheel(0,2500); page.wait_for_timeout(400)
                dismiss(page)
                txt=clean(extract(page))
                rec["chars"]=len(txt); rec["final"]=page.url
                fp=os.path.join(OUT,f"p{i:02d}_{host(u).replace('.','_')}.md")
                body = txt if txt else "[GATED/empty page] snippet fallback:\n"+meta.get("snippet","")
                with open(fp,"w",encoding="utf-8") as f:
                    f.write(f"# A{meta['angle']} {meta['title']}\nURL:{u}\nQ:{meta['q']}\n\n{body}")
                rec["file"]=fp; rec["gated"]= not txt
            except Exception as e:
                rec["error"]=repr(e)[:200]
            print(f"  [{i:02d}] A{rec['angle']} {host(u)[:24]} chars={rec.get('chars','-')} gated={rec.get('gated','-')} {rec.get('error','')}", flush=True)
            digest.append(rec)
        browser.close()
    with open(os.path.join(OUT,"_digest.json"),"w",encoding="utf-8") as f:
        json.dump(digest,f,ensure_ascii=False,indent=2)
    print("\nDONE phase2 files in", OUT, flush=True)

if __name__=="__main__":
    main()
