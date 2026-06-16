# 九野 · 像素美术生成规则（PIXEL_RULES）

> 像素是**刻意的载体，不是简陋的借口**。限制中见功夫：有限分辨率 + 有限调色板 → 靠 ramp/光影/轮廓/抖动 做出质感。
> 程序化的本分 = **确定性「变换 / 派生 / 拼装 / 后处理」**，不是凭空绘制原创内容。原创基础件交手绘/AI 小库，code 负责组合 + 批量 + 复现。
> 全程纯整数确定性（同输入同输出），与 sim 内核 ethos 一致。

> **美术管线文档地图**：本文(像素规则) · [`AIGEN_TOOL.md`](AIGEN_TOOL.md)(文生图工具 SunshineFlow 接入·永久知识) · [`PARTS_CONTRACT.md`](PARTS_CONTRACT.md)(角色部件契约) · [`AI_PROMPTS.md`](AI_PROMPTS.md)(逐部件 AI prompt) · `char_gen.py`(角色拼装器) · `icon_gen.py`(图标生成)

研究依据：autotile（redblobgames）/ 9-slice / 无缝 tile（np.roll+边界混合、4D 噪声环面）/ 鬼谷八荒=方格+PCG / 元气骑士=房间式模块 / 程序化强项=变换派生、弱项=原创角色立绘。

---

## 0. 程序化 vs 手绘/AI 边界（先钉死，避免幻想）

| 层 | 谁做 | 说明 |
|---|---|---|
| **地形 tile / 无缝纹理 / 噪声地表** | ✅ 纯 code(Pillow+numpy+noise) | 噪声→调色板量化→无缝化，确定性 |
| **autotile 展开 / 9-slice / spritesheet 打包 / 法线(Sobel)** | ✅ 纯 code | 几何变换，平凡可靠 |
| **PCG 世界布局(地形/biome/节点content)** | ✅ 纯 code(noise/WFC/BSP/CA) | 鬼谷式格点世界、元气式房间 |
| **物品/技能/UI 图标** | ✅ code 程序化(本规则 §3-6 抛光) | 几何母题+ramps+光影+框 |
| **角色 sprite** | ◐ **code 拼装 + 手绘小部件库** | 部件模板(体/发/衣/兵器)手绘或 AI 出一套，code 按角色数据拼装+palette swap(§8) |
| **精细原创立绘 / 流畅表情动画 / 手绘质感成套图** | ❌ 需 AI(扩散)/真美术 | 算法做不出语义+审美原创 |

**结论**：code 是「**已有基础件 → 批量程序化加工/拼装/派生**」的引擎。基础**几何**母题 code 直接画；基础**有机**件（角色部件/手绘 tile）需小库，code 拼。

---

## 1. 全局规格

- **基准网格**：项目统一 `16 / 32 / 48`。物品图标 native **48×48**（留 padding，主体 ~36）；地形 tile **32×32**（必须正方形+无缝）；角色 sprite **画布统一**（如 32×48 立姿，矩形，锚点底中）；UI 用 9-slice。
- **缩放**：仅整数倍 `×2/×3/×4/×5`，`NEAREST` 重采样，保像素锐利。禁非整数缩放/抗锯齿。
- **输出**：PNG（RGBA，含 alpha）。spritesheet 横排帧、纵排动作。命名见 §10。

---

## 2. 调色板（限制 + 协调）

- **全局主调色板**：限制在 **~32-48 色**（cohesion 关键）。禁每图随意取色。
- **per-material ramp**：每材质 1 条 `4-5 阶` ramp（darkest→dark→mid→light→lightest）。**hue-shift 规则**：暗部色相偏冷（蓝/紫）+ 降明度降饱和；亮部偏暖（黄）+ 升明度。禁直接 mix 黑/白（发灰发脏）。
- ramp 示例（钢）：`#2a2e3a → #555c6b → #8a93a3 → #c2cad6`，暗偏蓝亮偏白蓝。
- **元素色相**：火=橙红 ramp / 冰=青 ramp / 雷=黄 ramp / 木=绿 ramp / 鬼煞=紫 ramp / 佛金=金 ramp / 血=暗红 ramp / 邪=暗紫黑 ramp。元素变体 = **换 ramp 不换形**（palette swap）。

---

## 3. 光影（定向光源）

- **固定光源：左上**（全项目一致，否则成套素材光打架）。
- 实心形体：**左上边缘加 highlight**（ramp 最亮 1px），**右下加 shadow**（ramp 最暗），中间 mid。
- **AO（环境光遮蔽）**：形体内凹/叠层接缝处压一档暗。
- 球/管/刃要有体积感（最亮点 + 渐暗 + 反光 rim），非平涂。

---

## 4. 轮廓（outline）

- **选择性轮廓 selout**：silhouette 外轮廓 1px，用**该处材质 ramp 的最暗色**（带色相，非纯黑），重叠/前后景交界处可用纯暗。禁整图纯黑粗框（廉价感）。
- 内部分界用「**色块对比 + AO**」而非到处描黑线。

---

## 5. 抖动（dither）

- 渐变/纹理用 **棋盘/Bayer 抖动**（两相邻 ramp 阶交错），不用真渐变（破像素感）。
- 抖动**克制**：仅在大色块过渡、材质纹理（石/布/金属）用，小图标少用。

---

## 6. 物品/图标卡框 + 母题

- **卡框 48×48**：外 1px 暗轮廓 → 内 1-2px **斜角 bevel**（左上亮/右下暗，做凸起感）→ 角落 path-色饰角 → 底**品阶宝石/符文**（tier→颜色+数量+光效）。
- **母题语言**（按门类，几何 code 可画）：
  - 剑系=刃/锋(blade+guard+fuller+pommel 分件带光) / 体系=拳/甲/血气 / 法系=符箓/元素orb / 阵=阵纹grid / 器=法宝/砧 / 魂=识海/眼 / 雷=电 / 佛=金身/光轮 / 命=星盘/卦 / 驭兽=兽印/爪 / 儒=卷/文胆 / 魔=魔纹/角 / 妖=兽形/丹 / 血=血滴/煞 / 毒蛊=蛊/毒 / 符=符纸 / 傀儡=机关/线 / 音=音符/波 / 因果=阴阳/环。
- 母题要**多件带光**（剑=4-5 部件分别 ramp 光影），非单线条（破"简陋"）。

---

## 7. 地形 tile + 无缝 + 地图架构

- **tile 32×32 正方形**，**必须无缝可平铺**：
  - 生成：Perlin/Simplex 噪声 → 调色板量化(`round(e·levels)`) → 该地貌 ramp 着色。
  - 无缝化：① **4D 噪声映射环面(torus)** 天然 tileable；或 ② **np.roll 偏移半幅** 把四角拼中心 + 修中缝（高斯/羽化混合带）。
- **autotile**：基础边/角 tile → 算法展开 **16-tile(4-bit) / 47-tile blob(8-bit corner rule) / dual-grid(16图得平滑内角)**。据邻居 bitmask 查表选 tile → **消除可见网格**。
- **地图架构裁决**：
  - **底层 = tile-based 方格**（数据紧凑、碰撞/寻路格点化、PCG 友好）。
  - **表现 = 无缝**（tileable tile + autotile 让网格不可见；运行时 chunk 分块 + 流式懒加载，引擎侧 Unity/Godot 实现，非素材产物）。
  - **世界生成 = PCG**（鬼谷式：噪声地形 + biome 查表 + 节点 content[灵脉/秘境/门派/古迹] 随机植入；接 GeoCanon 锚点固定大区/地标 + 随机微 §地图C 设计）。
  - **「无缝感」靠 tile 做到看不出，不靠抛弃 tile**（鬼谷本身=方格+PCG）。

---

## 8. 角色模块化系统（元气骑士式，scale 到数百 NPC）

- **不画精细立绘**（不 scale + 算法做不出）。用**模块拼装 + palette swap**，从角色数据确定性派生。
- **部件库**（手绘/AI 出一套基础模板，小而精）：体型 / 头发 / 面部 / 衣袍(按 path 门类) / 兵器(按 path) / 配饰 / 站姿。每部件多变体。
- **确定性拼装**：`角色(persona/path/realm/faction/seed)` → 选部件变体 + 套 palette(element/faction ramp) + 叠 path 标识(剑修配剑/丹修配炉) + realm 高则加光环/特效 → 合成 sprite。同角色同输出（可复现）。
- **动画帧**：idle(4 帧/4-6fps) / walk(4-6 帧/8fps) / 简单 attack(3-6 帧)。**时序>帧数**。横排帧。
- 精度想升 → 扩部件库（手绘更多件），code 拼装逻辑不变。

---

## 9. 变体规则（一形多变，data-driven）

- **tier 品阶** → 卡框 bevel 复杂度 + 宝石数/色 + 光效(高阶加 glow/粒子)。
- **element 元素** → 换 ramp 色相（火/冰/雷/木…），形不变。
- **realm 境界** → 角色加光环/底座特效（炼气朴素→飞升华丽），物品加灵光。
- **faction 势力** → 配色 + 徽记叠加。
- 加变体 = 加数据行（L0），不改生成器（开闭）。

---

## 10. 目录 / 命名 / 管线

```
pixel/
  palette.py        # 全局调色板 + ramp + hue-shift
  shade.py          # 光影/轮廓/抖动 原语(highlight/shadow/AO/selout/dither)
  icon_gen.py       # 物品/技能/UI 图标(卡框+母题, §6)
  tile_gen.py       # 地形 tile(噪声+量化+无缝) + autotile 展开(§7)
  map_gen.py        # PCG 世界 + GeoCanon 拼图(§7)
  char_gen.py       # 角色模块拼装 + palette swap + 动画帧(§8)
  parts/            # 手绘/AI 基础部件库(角色部件、基础 tile)供拼装
  viewer.py         # 读 sim 状态 JSON → tilemap 叠角色/势力/事件(viewer 用)
  out/              # 生成产物 PNG
```
- 命名：`icon_<pathId>_<category>_<tier>.png` / `tile_<biome>_<variant>.png` / `char_<archetypeHash>.png` / `region_<id>.png`。
- **viewer 接 sim**：sim 侧加轻量 **JSON 状态导出**（World→大区/角色[位/path/realm]/势力领地/事件），Python viewer 读 → 渲染运行中江湖（解耦，不耦合 Core）。
- **确定性**：所有生成从 (数据 + 固定 seed) 派生，可复现；纹理种子用数据哈希。

---

## 11. 质量红线（拒绝"简陋"）

1. 有 ramp（≥3 阶/材质），无平涂大色块。
2. 有定向光影（左上光源一致），有体积。
3. 选择性轮廓，无纯黑粗框。
4. 调色板受控（全局协调），无杂色。
5. 母题多件带细节，非单线条。
6. tile 真无缝（平铺无接缝/网格感），autotile 消网格。
7. 角色模块拼装从数据派生，成套风格统一。
8. 程序化只做「变换/派生/拼装」，原创基础件诚实交手绘/AI。

---

## 12. 工具链

- **Pillow + numpy**：像素绘制/合成/缩放/mask。
- **noise / vec_noise**：Perlin/Simplex 地形（4D 环面无缝）。
- **autotile**：bitmask 查表展开（redblobgames 算法）。
- **法线**：Sobel 梯度 → 法线编码（numpy）。
- **9-slice**：切片 paste。
- **Playwright(headless)**：HTML Canvas/SVG 全景预览/合成大图（守红线不开前台窗）。
- **(可选) 扩散模型 MCP**：仅出角色基础部件/概念参考，非主管线。
