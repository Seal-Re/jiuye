# -*- coding: utf-8 -*-
"""Headless (no-window) scraper for gated 中文 sources (知乎/贴吧).
Dismisses 知乎 login modal, extracts text, saves to raw/, prints digest."""
import sys, os, re, json, time
from urllib.parse import quote
sys.stdout.reconfigure(encoding="utf-8")
from playwright.sync_api import sync_playwright

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "raw")
os.makedirs(OUT, exist_ok=True)

UA = ("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 "
      "(KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36")

# (label, angle#, url)
def zs(q):  # zhihu content search
    return "https://www.zhihu.com/search?type=content&q=" + quote(q)
def ts(q):  # tieba search
    return "https://tieba.baidu.com/f/search/res?ie=utf-8&qw=" + quote(q)

TARGETS = [
    ("zhihu_taiwu_randgen", 5, zs("太吾绘卷 角色 随机生成 机制")),
    ("zhihu_wuxia_textgame_design", 1, zs("武侠 文字游戏 设计 框架 玩法")),
    ("zhihu_guigu_mingge_random", 5, zs("鬼谷八荒 命格 随机 属性 上限")),
    ("zhihu_wuxia_trpg_rules", 2, zs("武侠 跑团 TRPG 规则 门派 武功 属性")),
    ("zhihu_xiake_attr_design", 5, zs("侠客风云传 属性 数值 设计 随机")),
    ("zhihu_generative_agents_npc", 6, zs("生成式智能体 generative agents 游戏 NPC 角色")),
    ("zhihu_wuxia_worldbuild", 3, zs("武侠 江湖 门派 功法 世界观 设定 套路")),
    ("zhihu_llm_npc_persona", 6, zs("大模型 LLM 驱动 NPC 角色扮演 人设 prompt")),
    ("tieba_wuxia_textgame", 4, ts("武侠 文字游戏 跑团")),
    ("tieba_wenzi_youxi", 4, ts("文字游戏 江湖 角色 随机")),
]

def dismiss_modal(page):
    # press Escape first
    try: page.keyboard.press("Escape")
    except Exception: pass
    page.evaluate("""() => {
        const clickSel = ['.Modal-closeButton','button.Button.Modal-closeButton',
            '[aria-label="关闭"]','.Modal-closeButton.Button--plain','.SignFlow-close'];
        for (const s of clickSel){ const el=document.querySelector(s); if(el){ try{el.click();}catch(e){} } }
        // nuke any leftover modal + mask, restore scroll
        document.querySelectorAll('.Modal-wrapper,.Modal-backdrop,.signFlowModal,.Modal,.Modal-enter,.Modal-content,div[role="dialog"]').forEach(e=>{try{e.remove();}catch(_){}});
        document.documentElement.style.overflow='auto';
        document.body.style.overflow='auto';
    }""")

def extract(page, kind):
    return page.evaluate("""() => {
        const pick = (sels) => { for (const s of sels){ const n=document.querySelector(s); if(n && n.innerText && n.innerText.length>80) return n.innerText; } return ''; };
        let t = pick(['.SearchMain','.Search-container','.RichContent','#content_html','.d_post_content_main','.p_postlist','main','#root']);
        if (!t || t.length < 200) t = document.body.innerText;
        return t;
    }""")

def clean(t):
    t = re.sub(r"\n{3,}", "\n\n", t)
    # drop obvious nav/login noise lines
    drop = ("登录","注册","下载知乎","知乎，让每一次","为你推荐","切换模式","写文章","提问","验证码","扫码")
    lines = [ln.rstrip() for ln in t.splitlines()]
    out = []
    for ln in lines:
        s = ln.strip()
        if not s:
            out.append(""); continue
        if len(s) <= 6 and any(d in s for d in drop):
            continue
        out.append(ln)
    return re.sub(r"\n{3,}", "\n\n", "\n".join(out)).strip()

def main():
    digest = []
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True, args=["--disable-blink-features=AutomationControlled"])
        ctx = browser.new_context(user_agent=UA, locale="zh-CN", viewport={"width":1366,"height":900})
        # block heavy assets for speed
        ctx.route(re.compile(r"\.(png|jpg|jpeg|gif|webp|svg|mp4|woff2?|ttf|css)(\?|$)"), lambda r: r.abort())
        page = ctx.new_page()
        for label, angle, url in TARGETS:
            rec = {"label": label, "angle": angle, "url": url}
            try:
                page.goto(url, wait_until="domcontentloaded", timeout=30000)
                page.wait_for_timeout(1500)
                dismiss_modal(page)
                page.wait_for_timeout(800)
                # scroll to load a few cards
                for _ in range(4):
                    page.mouse.wheel(0, 2500); page.wait_for_timeout(500)
                dismiss_modal(page)
                txt = clean(extract(page, label))
                rec["final_url"] = page.url
                rec["chars"] = len(txt)
                fp = os.path.join(OUT, label + ".md")
                with open(fp, "w", encoding="utf-8") as f:
                    f.write(f"# {label}  (angle {angle})\nURL: {url}\nFINAL: {page.url}\n\n{txt}")
                rec["file"] = fp
                rec["head"] = txt[:600]
            except Exception as e:
                rec["error"] = repr(e)[:300]
            digest.append(rec)
            print(f"[{label}] angle{angle} chars={rec.get('chars','-')} err={rec.get('error','')}", flush=True)
        browser.close()
    with open(os.path.join(OUT, "_digest.json"), "w", encoding="utf-8") as f:
        json.dump(digest, f, ensure_ascii=False, indent=2)
    print("\n=== DIGEST HEADS ===")
    for r in digest:
        if r.get("head"):
            print(f"\n----- {r['label']} (angle {r['angle']}) [{r['chars']} chars] -----")
            print(r["head"])
    print("\nDONE ->", os.path.join(OUT, "_digest.json"))

if __name__ == "__main__":
    main()
