---
name: sunshineflow-banana-alpha
description: 使用网易 DreamMaker SunshineFlow API 进行带 alpha 通道的像素图生成任务。支持生成 sprite sheet 序列帧、像素角色动画、HD-2D 场景贴图、魔法世界风格环境素材等。只要用户提到生图、生成图片、画图、出图、文生图、图生图、像素图、序列帧、sprite sheet、场景贴图、环境素材、平视贴图、开始界面素材、title screen、帮我生成一张图等相关需求，都应优先使用本技能。
metadata:
  version: 1.5.1
disable-model-invocation: true
---
# SunshineFlow Image Generator

本技能通过网易 DreamMaker 的 SunshineFlow API 完成生图任务（文生图 / 图生图）。**支持多蓝图共存**，根据 prompt 自动路由到合适的生图蓝图。

## 蓝图（Blueprint）机制

不同的图像生成需求对应不同的 SunshineFlow 蓝图（BASEURL + api_key + 输入输出字段都不同）。所有蓝图集中定义在 `scripts/blueprints.json` 注册表里，脚本运行时按以下规则选蓝图：

1. **显式指定**：`--blueprint <id>` 参数 → 强制用该蓝图
2. **关键词路由**：根据 prompt 里的关键词匹配 `trigger_keywords`，按命中数 + priority 排序，最优者胜
3. **兜底默认**：都不命中时用 `default` 字段指定的蓝图

### 当前已注册蓝图

| 蓝图 ID | 适用场景 | 触发关键词 | 输出 |
|---|---|---|---|
| `image2` ⭐ default | **通用高质量生图**（文生图 / 图生图，带 alpha 透明通道）：插画、UI 图标、角色立绘、商品/物品图、贴纸、装饰素材、概念图。**绝大多数生图请求都走这里。** | alpha / 透明背景 / 立绘 / UI / 图标 / 贴纸 / 物品图 / 插画 / 概念图 | 2 张图：带 alpha + 不带 alpha |
| `pixel-art-fs` | **像素游戏资产专用**：sprite sheet 序列帧、像素角色动画、chibi 像素角色、HD-2D 场景贴图、平视贴图。**仅在明确要生成像素游戏资产时才用。** | 序列帧 / sprite sheet / 像素角色 / 像素游戏资产 / 角色动画 / chibi / 平视贴图 / 场景贴图 | 1 张图 |

> **Agent 路由职责**：拿到生图请求后先判断场景。
> - **默认 / 普通生图**（没明确提"像素游戏资产/序列帧/sprite sheet"）→ 一律走 `image2`。
> - **仅当**用户明确要"像素游戏资产 / 序列帧 / sprite sheet / 像素角色动画 / 场景贴图 / 平视贴图"时 → 才走 `pixel-art-fs`。
> - "像素风插画/像素风背景图"这类一般审美需求，**不算**像素游戏资产，仍走 `image2`。
> - 如果用户没明确说"用哪个蓝图"，让脚本按关键词自动匹配；想强制某个蓝图加 `--blueprint <id>`。
>
> 查看完整蓝图列表（含描述、关键词、URL）：
> ```bash
> python <SKILL_DIR>/scripts/generate_image.py --list-blueprints
> ```

> 注意：以下规则是强制执行规则，Agent 不应跳过。
>
> 1. 当 `.env` 不存在或字段缺失时，必须逐项告知用户每个字段的获取方式和对应链接，不能只列字段名。
> 2. 不要直接向用户索要认证信息而不说明获取方式。
> 3. 如果脚本返回 HTTP 401/403，或返回内容包含 `token`、`expired`、`unauthorized`、`auth` 等关键词，必须按"Token 过期引导"提醒用户重新获取 Token。
> 4. `image2` 与 `pixel-art-fs` **都支持图生图**（可选参考图输入）。`image2` 的图片输入节点 key 为 `输入图片`（单图片集合）。


## 工作流程

### 第一步：检查认证配置

在执行任何生图操作之前，需要确认 `.env` 文件存在且包含以下必填字段：

```env
SUNSHINEFLOW_AUTH_TOKEN=...
SUNSHINEFLOW_AUTH_USER=...
SUNSHINEFLOW_APP_CODE=...
```

#### `.env` 查找优先级（脚本会按顺序找，找到即停）

| 顺序 | 路径 | 用途 |
|---|---|---|
| 1 | `--env <path>` 显式指定 | 临时覆盖 |
| 2 | `./.env`（当前工作目录） | 工程级覆盖 |
| 3 | `<skill_dir>/config/.env` | ⭐ **推荐**：skill 自带的私有配置目录，跟 skill 走，所有工程共用，push 到云端时自动排除整个 `config/` 目录，永不会泄漏密钥 |
| 4 | `~/.config/sunshineflow/.env`（Windows: `C:\Users\<你>\.config\sunshineflow\.env`） | 兜底：完全脱离 skill 的全局配置 |

> **强烈建议**：把 `.env` 放在第 3 个位置（`<skill>/config/.env`）。这是 skill 自带的私有配置区，约定上不会被上传，多个工程都能共用同一份配置。

Agent 检查时按以下逻辑：
- 如果 `<skill>/config/.env` 已存在且字段完整 → 直接进入第二步生图
- 如果当前工作目录 `./.env` 字段完整 → 也直接进入第二步
- 都没有或字段缺失 → 必须执行下面的"首次配置引导"

如果 `.env` 不存在或字段缺失，必须执行下面的"首次配置引导"，逐项引导用户获取信息。

### 首次配置引导

当用户首次使用且缺少配置时，请按下面的信息引导用户：

> 我将通过网易 DreamMaker 的 SunshineFlow 接口为你生成图片，需要先完成一次认证配置。请按以下步骤提供三项信息（只需配置一次，后续会自动读取）：
>
> **1. AUTH_TOKEN（用户认证 Token）**
> - 打开：https://console-auth.nie.netease.com/
> - 登录后，复制页面上的 **v2 token**
>
> **2. AUTH_USER（用户名）**
> - 就是你的网易企业邮箱 `@` 前面的部分。
> - 例如邮箱是 `zhangsan01@corp.netease.com`，则填写 `zhangsan01`
>
> **3. APP_CODE（用户组 App Code）**
> - 打开：https://dreammaker.netease.com/permission
> - 在用户组管理页面，找到你所在用户组对应的 `app_code`，格式通常类似 `_dm_prod_xxxxxxxxxxxxxxxx`

当用户提供以上三项信息后，**默认写入 skill 自带的私有配置目录**（推荐位置）：

```
<skill_dir>/config/.env
```

例如本地路径：`C:\Users\<用户名>\.agents\skills\sunshineflow-banana-alpha\config\.env`

这个 `config/` 目录是 skill 的私有配置区：
- ✅ push 到云端 Hub 时会被 `skills_hub_api.py` **自动整目录排除**，不会泄漏
- ✅ 所有工程都能共用，无需在每个工程内单独配置

```env
SUNSHINEFLOW_AUTH_TOKEN=用户提供的 token
SUNSHINEFLOW_AUTH_USER=用户提供的用户名
SUNSHINEFLOW_APP_CODE=用户提供的 app_code
```

如目录不存在需先创建。仅当用户明确要求"只在这个工程用"时，才写入工作目录的 `./.env`，并提醒：`.env` 文件包含敏感信息，请勿提交到 git 仓库，建议加入 `.gitignore`。

### 第二步：解析用户意图

从用户描述中提取生图参数：

| 参数 | 说明 | 是否必填 |
|------|------|----------|
| `prompt` | 图片描述提示词 | 是 |
| `reference_textures` | 参考图来源列表，支持 URL、本地文件路径或 data URL | 否 |

如果用户没有提供 `prompt`，先询问用户想要生成什么样的图片。

#### 像素序列帧模板（Sprite Sheet Template）

> ⚠️ 仅当用户明确要**像素游戏资产里的序列帧 / sprite sheet / 角色动画帧**时才套用本模板并走 `pixel-art-fs`。普通"画个角色"不套此模板，走 `image2`。

当用户的需求明确涉及**序列帧、sprite sheet、角色动画帧**时，Agent 必须自动套用以下默认提示词模板，用户只需提供角色描述部分（如"法师"、"蝙蝠"、"史莱姆"等）。

**默认模板：**

```
pixel art sprite sheet, 4x4 grid layout, 16 frames total, square layout, pure white background, no grid lines, no borders, no dividers between frames, frames placed seamlessly side by side with no separating lines, facing right, pixel art cute chibi style, adorable, game asset, consistent character design across all frames. Top 2 rows: 8-frame walk cycle animation. Bottom 2 rows: 8-frame continuous attack animation sequence: frame 1-2 wind up and prepare to attack, frame 3-5 swing weapon and strike forward with impact, frame 6-8 recover and return to idle stance. Character: {用户角色描述}
```

**拼接规则：**
1. 将模板中的 `{用户角色描述}` 替换为用户提供的角色描述，如 `big hat chibi mage holding a crystal staff`
2. 如果用户额外指定了帧数、排版方式等参数，用用户指定的值覆盖模板默认值
3. 如果用户没有指定方向，默认为 `facing right`
4. 如果用户没有指定风格，默认为 `pixel art cute chibi style, adorable`

**示例：**
- 用户说："帮我生成法师的序列帧"
  - prompt = `pixel art sprite sheet, 4x4 grid layout, 16 frames total, square layout, pure white background, no grid lines, no borders, no dividers between frames, frames placed seamlessly side by side with no separating lines, facing right, pixel art cute chibi style, adorable, game asset, consistent character design across all frames. Top 2 rows: 8-frame walk cycle animation. Bottom 2 rows: 8-frame continuous attack animation sequence: frame 1-2 wind up and prepare to attack, frame 3-5 swing weapon and strike forward with impact, frame 6-8 recover and return to idle stance. Character: cute chibi mage with big wizard hat, holding a glowing crystal staff`
- 用户说："生成一个史莱姆的序列帧"
  - prompt = `pixel art sprite sheet, 4x4 grid layout, 16 frames total, square layout, pure white background, no grid lines, no borders, no dividers between frames, frames placed seamlessly side by side with no separating lines, facing right, pixel art cute chibi style, adorable, game asset, consistent character design across all frames. Top 2 rows: 8-frame walk cycle animation. Bottom 2 rows: 8-frame continuous attack animation sequence: frame 1-2 wind up and prepare to attack, frame 3-5 swing weapon and strike forward with impact, frame 6-8 recover and return to idle stance. Character: cute slime monster, bouncy, jelly-like body`
- 用户说："蝙蝠序列帧"
  - prompt = `pixel art sprite sheet, 4x4 grid layout, 16 frames total, square layout, pure white background, no grid lines, no borders, no dividers between frames, frames placed seamlessly side by side with no separating lines, facing right, pixel art cute chibi style, adorable, game asset, consistent character design across all frames. Top 2 rows: 8-frame fly cycle animation. Bottom 2 rows: 8-frame continuous attack animation sequence: frame 1-2 wind up and prepare to attack, frame 3-5 swing weapon and strike forward with impact, frame 6-8 recover and return to idle stance. Character: cute chibi bat with small wings, dark purple`

#### 魔法世界平视场景贴图模板（Magic World Side-View Scene Asset Template）

> ⚠️ 本模板属于**像素游戏资产**，走 `pixel-art-fs`。仅当用户明确要"场景贴图 / 平视贴图 / HD-2D 场景素材 / 游戏环境素材"时才套用。

当用户的需求明确涉及**场景贴图、环境素材、场景元素、平视贴图、side view、HD-2D 场景、开始界面素材、title screen asset**时，Agent 必须自动套用以下默认提示词模板，用户只需提供具体物体描述部分（如"松树"、"路牌"、"木桶"、"植被"等）。

**默认模板：**

```
HD-2D pixel art game asset, single isolated object on pure white background, no ground, no shadow, 256x256 pixel art, hand-crafted detailed pixel work with painterly lighting, sharp pixel edges, Harry Potter magical wizarding world atmosphere, warm mysterious tone. CRITICAL: strict side view only, camera positioned at object center height, absolutely no top-down angle, no bird eye view, no 3/4 view, no isometric, no perspective distortion, 2D platformer style orthographic flat view, horizon line at vertical center of frame, object shown from pure horizontal eye level like a classic 2D game sprite. Subject: {用户物体描述}
```

**拼接规则：**
1. 将模板中的 `{用户物体描述}` 替换为用户提供的物体描述
2. 如果用户额外指定了尺寸（如 512x512），用用户指定的值覆盖模板默认的 256x256
3. 如果用户指定了其他风格（如"蒸汽朋克"、"赛博朋克"），替换掉 "Harry Potter magical wizarding world atmosphere, warm mysterious tone" 部分
4. 如果用户明确要求俯视或 3/4 视角，移除 "CRITICAL: strict side view only..." 整段约束
5. 默认输出带透明背景（alpha 通道），适合直接叠加到游戏场景

**示例：**
- 用户说："帮我生成一棵魔法松树的场景贴图"
  - prompt = `HD-2D pixel art game asset, single isolated object on pure white background, no ground, no shadow, 256x256 pixel art, hand-crafted detailed pixel work with painterly lighting, sharp pixel edges, Harry Potter magical wizarding world atmosphere, warm mysterious tone. CRITICAL: strict side view only, camera positioned at object center height, absolutely no top-down angle, no bird eye view, no 3/4 view, no isometric, no perspective distortion, 2D platformer style orthographic flat view, horizon line at vertical center of frame, object shown from pure horizontal eye level like a classic 2D game sprite. Subject: a tall magical pine tree with dense dark green needles, thick bark with faint glowing amber runes, mystical forbidden forest conifer, pure side profile view`
- 用户说："生成一个魔法路牌"
  - prompt = `HD-2D pixel art game asset, single isolated object on pure white background, no ground, no shadow, 256x256 pixel art, hand-crafted detailed pixel work with painterly lighting, sharp pixel edges, Harry Potter magical wizarding world atmosphere, warm mysterious tone. CRITICAL: strict side view only, camera positioned at object center height, absolutely no top-down angle, no bird eye view, no 3/4 view, no isometric, no perspective distortion, 2D platformer style orthographic flat view, horizon line at vertical center of frame, object shown from pure horizontal eye level like a classic 2D game sprite. Subject: an old wooden signpost with weathered oak post, two hand-carved direction arrow boards pointing different ways, engraved gothic letters, small wrought-iron lantern with warm amber glow, ivy climbing the post, brass nails, Hogsmeade village style, pure side profile view`
- 用户说："平视植被贴图"
  - prompt = `HD-2D pixel art game asset, single isolated object on pure white background, no ground, no shadow, 256x256 pixel art, hand-crafted detailed pixel work with painterly lighting, sharp pixel edges, Harry Potter magical wizarding world atmosphere, warm mysterious tone. CRITICAL: strict side view only, camera positioned at object center height, absolutely no top-down angle, no bird eye view, no 3/4 view, no isometric, no perspective distortion, 2D platformer style orthographic flat view, horizon line at vertical center of frame, object shown from pure horizontal eye level like a classic 2D game sprite. Subject: a cluster of magical wild plants, tall green grass with glowing sparkle particles, broad fern fronds, small purple wildflowers, luminescent blue mushrooms at base, enchanted forest undergrowth, pure side profile view`
- 用户说："蒸汽朋克风格的废弃矿井贴图"
  - prompt = `HD-2D pixel art game asset, single isolated object on pure white background, no ground, no shadow, 256x256 pixel art, hand-crafted detailed pixel work with painterly lighting, sharp pixel edges, steampunk post-apocalyptic wasteland atmosphere, industrial gritty tone with brass gears and rusted metal. CRITICAL: strict side view only, camera positioned at object center height, absolutely no top-down angle, no bird eye view, no 3/4 view, no isometric, no perspective distortion, 2D platformer style orthographic flat view, horizon line at vertical center of frame, object shown from pure horizontal eye level like a classic 2D game sprite. Subject: an abandoned mine entrance carved into rocky hillside, dark cave opening, weathered wooden support beams, rusted rails leading into darkness, scattered broken planks and old lantern, dim orange glow from inside, pure side profile view`

**常用物体描述参考词库：**
| 类别 | 参考描述 |
|------|----------|
| 树木 | pine tree, oak tree, willow tree, dead tree, magical glowing tree |
| 植被 | grass cluster, fern fronds, wildflowers, glowing mushrooms, ivy vines, moss-covered rocks |
| 建筑元素 | wooden signpost, stone well, lamp post, wooden barrel, crate, stone pillar, archway |
| 道路元素 | milestone, road bollard, cobblestone path segment, wooden fence |
| 魔法元素 | enchanted lantern, magical crystal, runic stone, potion bottle, cauldron |

### 第三步：执行生图脚本

调用本 skill 目录下的 `scripts/generate_image.py` 完成生图，脚本会按"第一步"中描述的优先级查找 `.env`（推荐放在 `~/.config/sunshineflow/.env`）。

路径规则：`scripts/generate_image.py` 是相对于本 `SKILL.md` 的路径，调用时必须解析成绝对路径。

示例：

```bash
# 文生图（默认走 image2）
python <SKILL_DIR>/scripts/generate_image.py \
  --prompt "一只在星空下奔跑的狐狸，油画风格"

# 图生图（参考图 URL，默认走 image2，可选图片输入）
python <SKILL_DIR>/scripts/generate_image.py \
  --prompt "将这张图转化为水墨画风格" \
  --reference_textures "https://example.com/ref1.jpg"

# 图生图（本地附件路径）
python <SKILL_DIR>/scripts/generate_image.py \
  --prompt "保持构图，把背景换成黄昏天空" \
  --reference_textures "C:/temp/uploaded-reference.png"

# 像素游戏资产（明确序列帧才用 pixel-art-fs）
python <SKILL_DIR>/scripts/generate_image.py \
  --blueprint pixel-art-fs \
  --prompt "pixel art sprite sheet ..."

# 用户要求"存下来"时：自动下载到本地
python <SKILL_DIR>/scripts/generate_image.py \
  --prompt "森林法师小姐姐" \
  --output-dir "./generated_images"
```

#### 何时使用 `--output-dir`（保存到本地）

**默认不传，只返回 URL**。仅当用户的请求里出现以下信号之一时，Agent 才应该加 `--output-dir`：

| 用户表达 | 处理 |
|---|---|
| "存下来" / "保存" / "下载" / "存到本地" / "save" | ✅ 加 `--output-dir`，路径默认 `./generated_images`（当前工作目录） |
| "存到 xxx 文件夹" / "保存到 xxx" / "新建一个 xxx 目录存" | ✅ 加 `--output-dir <用户指定的目录>` |
| 其他普通生图请求 | ❌ 不加，只返回 URL |

文件命名规则（脚本自动）：`<blueprint>_<YYYYMMDD_HHMMSS>_<prompt slug>_<idx>[_<label>].<ext>`
- 例：`image2_20260512_172850_森林法师小姐姐_0_alpha.png`
- 例：`pixel-art-fs_20260512_180000_cute_slime_0.png`

### Attachment Image Rules

当用户直接在聊天中上传参考图时，不要强行寻找公网 URL。Agent 应优先复用宿主平台提供的附件本地路径或临时文件路径，并直接作为 `reference_textures` 传给脚本。

处理规则：
1. 如果用户上传的是聊天附件，优先寻找宿主平台暴露出来的本地文件路径。
2. 如果拿到了本地路径，直接把这个路径传给 `reference_textures`，不要再要求用户补 URL。
3. 如果只拿到了 URL，就按 URL 方式处理。
4. 如果既没有路径也没有 URL，再向用户索要图片或链接；不要静默退回文生图。

### 第四步：返回结果给用户

脚本成功后，直接向用户展示生成结果，不要附加多余解释。

- 如果输出包含图片 URL，直接展示图片
- 如果有多张图，逐张展示
- **如果用 `--output-dir` 下载了本地副本**：返回 JSON 中会带 `local_paths` 和 `output_dir` 字段，告诉用户文件已保存到哪里，并列出每个本地路径

#### 不同蓝图的输出说明

| 蓝图 | output_textures 内容 | 展示策略 |
|---|---|---|
| `image2` | 2 张图：①带 alpha 通道版（主产物） + ②不带 alpha 通道版（备份/对比） | 默认两张都展示，并明确标注哪张是「带 alpha」哪张是「不带 alpha」。如果用户只问要透明背景版本，重点突出第一张。output_textures 数组顺序固定：`[带alpha, 不带alpha]` |
| `pixel-art-fs` | 1 张图 | 直接展示 |

- 如果生图失败，简洁告知失败原因

## Fail-Fast Rules

在调用 `scripts/generate_image.py` 之前，Agent 必须额外遵守以下规则：

1. 任何前置步骤如果失败，都必须停止，不得继续提交生图任务。
2. 如果上游提示词润色或改写失败，优先回退到“用户原始描述”作为 prompt；只有当原始描述本身也为空时，才允许报错退出。
3. 调用脚本前，必须先确认 `prompt.strip()` 非空。
4. 如果脚本返回 `error=rate_limited`，应直接告知用户“服务繁忙，请稍后重试”。
5. 如果脚本返回 `error=auth_error`，应按本文档里的 Token 过期引导处理。

## Context Image Reuse Rules

当用户说“把刚才生成的这张图变成……”、“基于上一张图继续改”、“把这张图换成……风格”等类似表达时，默认应判定为图生图，而不是重新走文生图。

1. 如果最近一轮由本 skill 成功生成过图片，并且 `output_textures` 中存在可用 URL，那么把最近一张图的 URL 自动写入 `reference_textures`。
2. 当用户没有显式提供新图片、但明确引用“刚才 / 上一张 / 这张生成结果”时，不要丢掉上下文，也不要回退成纯文生图。
3. 如果上一轮返回了多张图，而用户没有说明是哪一张，先让用户选择；不要擅自挑选。
4. 如果当前会话里没有可复用的上一张图 URL，再向用户索要图片或 URL；不要静默提交没有参考图的任务。

## Image Input Mapping

各蓝图的图片输入节点 key 不同，调用方不能直接把图片 URL 字符串当普通文本传，必须经过转换并写到正确的图片变量上。

各蓝图图片输入 key：
- `image2`：单图片集合，key 为 `输入图片`（第一张参考图写入这里）
- `pixel-art-fs`：`image01`、`image02` … `image06`

图生图调用规则：
1. `reference_textures` 是上层输入语义，表示"参考图来源列表"。
2. 参考图来源可以是图片 URL、本地文件路径，或者已经存在的 data URL。
3. 脚本会自动把这些来源转换为 `data:image/<type>;base64,...`，再按蓝图 `image_input_keys` 顺序写入对应图片变量。
4. 不要再把纯 URL 直接传给图片类型节点；统一通过 `--reference_textures` 传，由脚本映射。
5. `image2` 仅有一个图片输入位（`输入图片`），多于 1 张参考图时只取第一张。

## 注意事项

- `.env` 查找顺序：`--env` 指定 → `./.env` → `<skill>/config/.env` → `~/.config/sunshineflow/.env`。详见"第一步"，推荐放在 `<skill>/config/.env`。
- `BASEURL` 固定为：`http://sunshineflow-api-server-prod.tmax.nie.netease.com/pixel-art-fs`
- `SUNSHINEFLOW_API_KEY` 默认使用公开蓝图 Key `4f4a0381-f9fe-422e-b8b8-b05ab5a7e151`（已内置于脚本中），用户也可在 `.env` 中覆盖。
- 轮询间隔默认 1 秒，超时时间默认 300 秒。