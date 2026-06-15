# 文生图工具接入 · 美术素材生成管线（永久知识）

> **本文档是 jiuye 美术"出料"环节的永久记录**：用什么文生图工具、怎么配、怎么和已有像素管线衔接。
> 跨会话有效。配套：`PARTS_CONTRACT.md`（部件契约）+ `AI_PROMPTS.md`（prompt 清单）+ `char_gen.py`（拼装器）+ `PIXEL_RULES.md`（像素规则）。

## 1. 选定工具：SunshineFlow（网易 DreamMaker 文生图）

- **来源**：NetEase Agent Skills Hub，`/skillhub` 装入，skill id `skill_e6cb77cb36b6`（slug `sunshineflow-banana-alpha`）。
- **位置**：`E:/Seal/jiuye/.claude/skills/sunshineflow-banana-alpha/`（项目级，jiuye 可 `/sunshineflow-banana-alpha` 调用）。
- **为何选它**（评估 18 个候选后）：
  - ✅ **原生 alpha 透明通道**——直接满足部件契约 §1「透明背景」硬要求，省抠底。
  - ✅ **像素专精蓝图** `pixel-art-fs`（sprite sheet/像素角色）+ **通用 alpha 蓝图** `image2`（图标/物品/立绘）。两个蓝图正好覆盖 jiuye 两类需求（角色部件 + 图标优化）。
  - ✅ **网易自家 API**（环境契合，auth 走网易 console，非第三方付费 key）。
  - ✅ 13 invoke 真实使用、安全扫描 clean。
  - 对比落选：nano-banana/qwen-image（通用但需自配 inference.sh、非像素专精、无原生 alpha）；game-sprite-generator（0iv、Godot 向）。

## 2. 两个蓝图（按需选）

| 蓝图 | 用途 | 触发词 | 输出 | jiuye 用在 |
|---|---|---|---|---|
| `image2` ⭐默认 | 带 alpha 通用生图：图标/物品/立绘/贴纸/概念图 | alpha/透明背景/图标/物品图/立绘 | 2 张（带alpha主产物 + 不带alpha备份） | **图标优化**（21路功法图标）、**可染色袍服部件**（出灰阶） |
| `pixel-art-fs` | 像素游戏资产：sprite sheet/像素角色/序列帧/场景贴图 | sprite sheet/像素角色/序列帧/chibi/场景贴图 | 1 张 | **角色部件**（body/hair/weapon）、**地形 tile**、**动画帧** |

- 不加 `--blueprint` → 脚本按 prompt 关键词自动路由；想强制加 `--blueprint pixel-art-fs`。
- 两蓝图都支持**图生图**（`--reference_textures <url或本地路径>`）→ 可用参考图保风格统一/出变体。

## 3. 一次性配置（需用户网易 token）

脚本要 `.env`（3 字段），放 `<skill>/config/.env`（**已 gitignore，绝不入库**）或 `~/.config/sunshineflow/.env`：
```env
SUNSHINEFLOW_AUTH_TOKEN=...   # https://console-auth.nie.netease.com/ 登录取 token
SUNSHINEFLOW_AUTH_USER=...    # 用户名(企业邮箱前缀)
SUNSHINEFLOW_APP_CODE=...     # DreamMaker 项目码,形如 _dm_prod_xxxxxxx
```
> **⚠️ 实测踩坑（2026-06-15 验证通过，永久记录）**：
> - **AUTH_TOKEN 不要带 `v2:` 前缀**——console 页面显示的是 `v2:eyJ...`，但填进 .env 只取 `eyJ...` 的 JWT 本体；带 `v2:` 会 401 `token is invalid`。
> - **APP_CODE = DreamMaker 项目码**（`_dm_prod_xxx`），**不是** authkey。authkey（`X-Auth-API-key`）是另一回事，本公开蓝图无需配（蓝图 key 已内置 blueprints.json）。
> - 字段→请求头映射：AUTH_TOKEN→`X-Access-Token`，AUTH_USER→`X-Auth-User`，APP_CODE→`X-Aigw-App`。
> - **APP_CODE 用「个人体验用户组」的码**（形如 `_dm_prod_<用户名>`，如 `_dm_prod_huangjiaqi13`）——这个组有体验积分；项目级码（`_dm_prod_cza9bi0zed` 等）认证过但可能 0 积分报「剩余积分不足」。app_code 在 https://dreammaker.netease.com/permission 查（需登录，按用户组找）。
> - **2026-06-15 全链路实测成功**：`_dm_prod_huangjiaqi13` 出图通，43s/张，自动下载本地（带 alpha + 不带 alpha 两版）。质量=精致像素插画（远超程序化几何件）。
> - token 有有效期（JWT exp 2026-06-22），过期重取（401 时）。
> - **账号需有 DreamMaker 积分**：认证过但积分不足会报 `剩余积分不足，请联系值班`——属账号配额，需充值/联系值班，非配置问题。
> - token 有有效期（JWT exp），过期重取（401 时）。
- Token 会过期 → 脚本报 401/403 时重新取。
- `SUNSHINEFLOW_API_KEY` 用公开蓝图 key（已内置），无需配。
- 依赖：`requests`（已装）。

## 4. 调用命令

```bash
SKILL=.claude/skills/sunshineflow-banana-alpha/scripts/generate_image.py
# 列蓝图
python $SKILL --list-blueprints
# 文生图(默认 image2 带 alpha) → 出图标/可染色部件
python $SKILL --prompt "<AI_PROMPTS.md 里的 prompt>" --output-dir "./pixel/_aigen"
# 像素角色部件(强制 pixel 蓝图)
python $SKILL --blueprint pixel-art-fs --prompt "pixel art sprite ..." --output-dir "./pixel/_aigen"
# 图生图(保风格/出变体)
python $SKILL --prompt "..." --reference_textures "./pixel/parts/3_robe/robe_sword_v1.png"
```

## 5. 完整美术出料管线（端到端）

```
① 取 prompt   ← AI_PROMPTS.md（逐部件已写好，带通用前缀+负向）
② 生图        ← SunshineFlow（image2 出 alpha 部件 / pixel-art-fs 出像素资产）
③ 后处理      ← 对齐 32×48 像素格 + 抠透明底 + 量化调色板（AI直出难精确踩格，需此步；
                可写对齐脚本辅助，见 PARTS_CONTRACT §5）
                ⚠️ 实测：image2 直出约 **1254×1254 RGBA 高清像素插画**（非 32×48 网格）。
                → 当**图标/物品/立绘**可直接用(高清)；进 **char_gen 部件**需先 downscale 到 32×48 + 调色板量化(后处理脚本)。
④ 入库        ← 按 PARTS_CONTRACT 命名丢 pixel/parts/<层>/
⑤ 拼装        ← python pixel/char_gen.py（零改代码加载新件 + palette-swap 染色 + 装备层 + 光环）
```

- **图标优化路径**（你提的）：用 `image2` 蓝图按门类母题出 21 路功法图标（带 alpha），替换 icon_gen 的几何母题 → 图标从"程序化几何"升到"AI 精绘"。
- **角色部件路径**：`AI_PROMPTS.md` 的 body/robe/hair/weapon prompt → 出料 → 入 `parts/` → char_gen 拼装。**引擎不动，喂好料即跳画质**（已实证 loader 通）。

## 6. 边界与纪律

- **红线 B.1**：SunshineFlow 是 headless API 调用（HTTP POST 到网易 endpoint），合规——非前台浏览器窗口。
- **红线 B.8**：游戏世界（角色/tile/物品）=像素轨；UI=古风轨。SunshineFlow 两轨都能出（pixel-art-fs 出像素、image2 出 UI 元素）。
- **AI 出图是"原创基础件"那一层**（PIXEL_RULES §0：算法做不出语义+审美原创 → 交 AI）；code 仍负责变换/派生/拼装/批量。
- **确定性边界**：AI 出料**不确定**（同 prompt 不同图），属"一次性出基础件"；入库后 char_gen 的拼装/换色/选件**确定**（红线 B.2）。即：料随机出一次，组合确定可复现。
- 出的 raw 图建议放 `pixel/_aigen/`（gitignore），精修入库的才进 `pixel/parts/`。
