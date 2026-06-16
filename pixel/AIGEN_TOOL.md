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
- **角色路径**：见 §6.1 —— **角色本体不用 AI**，AI 只出静态挂件/立绘。

### 5.1 ⚡ 网格批量出料（省额度核心，100 件装备的解法）

> **问题**：脚本无 `--count`，逐件出 100 件装备 = 100 次额度 + 慢。**解法**：一张图 prompt 成 N×N 网格、本地切片。
> **2026-06-15 实证**：1 张 4×4 网格 prompt → 切出 12-16 件干净 48² 独立装备 → **1 次调用 ≈ 12-16 件**，100 件仅 ~8-9 次调用，**省 ~92% 额度**。

```bash
# ① AI 出网格图(prompt 要求 NxN grid, each centered, transparent, clean grid lines)
python $SKILL --prompt "pixel art equipment icons, a 4x4 grid of 16 different ... each centered in its own cell, transparent background, consistent style, clean dark grid lines" --output-dir ./pixel/_aigen
# ② 本地切片成 N 独立件(48² 游戏尺寸)
python pixel/postprocess.py grid <网格图> --rows 4 --cols 4 --cell 48 --names jian,dao,spear,... --out-dir pixel/_aigen/equip_sliced
# ③ 精修挑用的入库 pixel/parts/ 或挂 char_skeleton 装备点
```

**100 件装备生产策略（评估定，承用户②）**：
- AI 网格批量出 **~20-30 件"种类基底"**（2 张网格图）——不逐件出。
- `postprocess grid` 切片 + 缩 48²。
- **palette-swap**（染金/银/玄铁/血红…品阶色）+ **程序化叠饰**（realm 流光/宝石/符文）→ 派生到 100+ 件变体。
- **= AI 只出"种类"，code 派生"品阶/染色/特效变体"**（承 PIXEL_RULES §0：AI 出基础件 + code 批量派生）。

### 5.2 尺寸纪律（承用户②"像素太高没用"）

- 装备图标 **48²**、角色本体 **32×48**（char_skeleton）——游戏内尺寸，不追高像素。
- AI 直出高清（image2≈1254²/网格每格≈150²）→ postprocess 缩到目标格，高像素只是"出图中转"不入库。

## 6. 边界与纪律

### 6.1 ⚠️ AI vs 程序化分工（2026-06-15 定，按"是否要逐帧动画/骨架一致"切）


> **关键约束**：角色要移动动画（idle/walk/attack），靠**骨架驱动**——每帧关节位置必须一致。**AI 逐帧生成无法保证骨架一致**（同角色第2帧手臂/肩宽/头身比/脚位漂移→动画抖动变形），这是 AI 生图根本缺陷，prompt 修不了。**故角色本体（要动画的）不用 AI 绘制。**

| 类别 | 要动画/骨架一致? | 谁做 |
|---|---|---|
| **角色本体**（体/四肢/动画帧） | ✅ 要 | **程序化骨架驱动**（char_gen：关节定义+帧插值+部件挂骨架+palette-swap），逐帧严格一致、确定可复现 |
| **武器/法宝/披风 装备叠层** | ◐ 刚体挂载 | 程序化挂点；简单静态件可 AI 出再挂骨架手/背挂点 |
| **静态资源**：功法图标/物品图/UI/头像立绘/图鉴/地形tile | ❌ 不动 | **AI 出最香**（实测那把剑、蓝袍剑客作图鉴立绘完美） |

> **一刀切**：要动的→程序化骨架；不动的→AI。AI 出的"漂亮整角色"= 静态立绘/图鉴/头像用，**不是可动游戏本体**。
> 此分工承 PIXEL_RULES §0 边界 + 红线 B.8。**之前"AI整角色基底"路线已废**（整图不能动）。

### 6.1b ⚠️ 角色本体几何渲染 = 过渡占位（2026-06-16 定，勿再美化）

> `char_skeleton.py` 的几何渲染是**当前阶段占位**，不投精力美化。原因：
> - 几何 limb 拼的小人**到不了正式品质**（已实测，矮胖/手臂糊/轮廓锯齿），调参也只是"精致积木人"。
> - **Unity 阶段角色动画走 Spine/DragonBones 骨骼绑定 + 美术部件**——这张几何 PNG **Unity 用不到、不带走**。精修它=投入打水漂。
> - 角色"好看"是 **Unity 阶段美术+Spine** 的事，非当前 headless 脚本。鬼谷八荒的角色也是美术画的，不是程序生成的。
>
> **char_skeleton 保留价值**：① CLI/调试看角色长相 ② 验"装备挂载点=骨架关节"逻辑。**仅此，不美化。**
>
> **这次美术探索的长期资产（Unity 复用）**：①AI 网格出料/切片/品阶派生(装备/图标/法宝) ②SunshineFlow 工具+管线+经济性 ③静态图标/立绘/图鉴。**只有"角色本体几何渲染"是过渡占位，其余全是长期资产。**

### 6.2 其他

- **红线 B.1**：SunshineFlow 是 headless API 调用（HTTP POST 到网易 endpoint），合规——非前台浏览器窗口。
- **红线 B.8**：游戏世界（角色/tile/物品）=像素轨；UI=古风轨。
- **AI 出图是"原创基础件/静态展示"那一层**（PIXEL_RULES §0：算法做不出语义+审美原创 → 交 AI）；code 负责骨架/动画/拼装/批量。
- **确定性边界**：AI 出料**不确定**（同 prompt 不同图），属"一次性出静态件"；char_gen 的骨架/拼装/换色**确定**（红线 B.2）。
- 出的 raw 图建议放 `pixel/_aigen/`（gitignore），精修入库的才进 `pixel/parts/`。
