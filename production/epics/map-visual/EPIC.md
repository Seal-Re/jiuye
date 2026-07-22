# Epic: 2D 瓦片地图可视化 (map-visual)

**Layer**: Presentation (Godot View)
**Status**: Planned
**GDD**: docs/legacy-specs/specs/2026-06-13-v1.2-C-江湖地图与门派系统-design.md
**Canon**: docs/legacy-specs/specs/2026-06-14-九野-地图概要与宗门排布-canonical.md
**Created**: 2026-07-22

## Summary

四层叠加渲染的 2D 像素瓦片地图。纯哈希确定性生成（同 seed + 同坐标 = 同结果）。
底层多概率变体铺满 → 上层小概率装饰叠加 → 再上层稀有特征点缀 → 最上层地标建筑锚定。

## 架构：四层叠加模型 + 确定性哈希

### 节点拓扑（Godot 4.3+ TileMapLayer）

```
Main (Node)
├── MapLayer0_Terrain   (TileMapLayer)  — 基础地形 100% 覆盖
├── MapLayer1_Decor     (TileMapLayer)  — 装饰物 <15%
├── MapLayer2_Rare      (TileMapLayer)  — 稀有特征 <3%
└── MapLayer3_Landmark  (TileMapLayer)  — 地标建筑 固定锚点
```

> 不使用单一 TileMap 的 `layer` 参数（Godot 4.3 已弃用多图层模式）。
> 写入时分别调用各层 `set_cell(coords, source_id, atlas_coords)`。

### 确定性哈希函数（Wang Hash 变体 —— 跨平台/跨版本稳定）

```gdscript
# 纯位运算位移哈希。不依赖 Godot 内置 hash()（大版本间可能变动）。
# 使用 & 0x7FFFFFFFFFFFFFFF 取正 —— 避免 abs(INT_MIN) 溢出崩溃。
func hash_cell(x: int, y: int, map_seed: int, layer: int) -> int:
    var h: int = (x * 73856093) ^ (y * 19349663) ^ (map_seed * 83492791) ^ (layer * 631)
    h ^= h >> 16
    h *= 0x85ebca6b
    h ^= h >> 13
    h *= 0xc2b2ae35
    h ^= h >> 16
    return h & 0x7FFFFFFFFFFFFFFF   # 屏蔽符号位，绝不安 abs()
```

### 渲染管线计算顺序（关键：时序依赖）

```
1. 计算 Layer 0 基础哈希变体 → 选出 T01-T07 具体变体
2. 执行条件拦截 → 检查 Peril/QiDensity/Element
   若满足 T08-T11 触发条件 → 覆盖 Layer 0 为基础特殊地形
3. 基于覆盖后的 最终 Layer 0 ID → 判定 Layer 1 装饰物
4. 基于覆盖后的 最终 Layer 0 ID → 判定 Layer 2 稀有特征
5. NodeId 精确匹配 → 写入 Layer 3 地标建筑
```

> 时序铁律：Layer 0 条件拦截必须在 Layer 1/2 判定之前。否则鬼域(T10)上会长出翠绿草地的野花(D01)。

---

## Layer 0 — 基础地形（100% 覆盖，每地形 3 变体，概率 60/25/15）

### 7 大区基础瓦片

| # | 地形 | 变体 A (60%) | 变体 B (25%) | 变体 C (15%) | 对应 Region |
|---|---|---|---|---|---|
| T01 | **平原 Plain** | 草地·翠绿 (grass_green) — 青草+野花点缀 | 草地·茂密 (grass_dense) — 高草丛+蝴蝶 | 草地·枯黄 (grass_dry) — 黄草+土块裸露 | 0 中州 |
| T02 | **海域 Sea** | 海面·平静 (sea_calm) — 深蓝+微浪 | 海面·波浪 (sea_wave) — 白浪花+泡沫 | 海面·暗涌 (sea_dark) — 暗蓝+漩涡暗纹 | 1 东海 |
| T03 | **荒漠 Desert** | 沙地·金黄 (sand_gold) — 沙丘纹理+热浪 | 沙地·碎石 (sand_gravel) — 碎石+枯枝 | 沙地·龟裂 (sand_crack) — 龟裂纹+盐碱白斑 | 2 北漠 |
| T04 | **山岳·火 Mountain-Fire** | 岩壁·红褐 (rock_red) — 红褐岩+矿脉细线 | 岩壁·富矿 (rock_ore) — 金属光泽矿脉+晶簇 | 岩壁·熔隙 (rock_lava) — 细小熔岩裂隙+火星 | 3 西陲 |
| T05 | **林莽 Jungle** | 密林·暗绿 (jungle_dark) — 暗绿密叶+斑驳光影 | 密林·藤蔓 (jungle_vine) — 粗藤缠绕+巨叶 | 密林·瘴紫 (jungle_miasma) — 紫色瘴气薄雾+荧光菌 | 4 南疆 |
| T06 | **山岳·密林 Mountain-Forest** | 山峦·翠绿 (mtn_green) — 翠绿层叠山+松树 | 山峦·古树 (mtn_ancient) — 参天古树+盘根 | 山峦·雾霭 (mtn_mist) — 云雾半掩+瀑布银线 | 5 苗疆 |
| T07 | **水泽 Marsh** | 水面·青绿 (marsh_teal) — 青绿水面+浮萍 | 水面·莲花 (marsh_lotus) — 莲花+蜻蜓 | 水面·芦苇 (marsh_reed) — 芦苇丛+水鸟 | 6 江南 |

### 特殊地形变体（极端条件触发，替换 Layer 0 基础变体）

| # | 变体 | 触发条件 | 替换区域 | 描述 |
|---|---|---|---|---|
| T08 | **火山/熔岩 Volcano** | Peril≥80 ∧ Element=火 | 西陲·地火熔渊 | 岩浆河+黑曜石柱+漫天火星+赤红天空映照 |
| T09 | **雪原/冰峰 Snowfield** | Peril≥70 ∧ (北漠 ∨ 高海拔) | 北漠·高海拔带 | 皑皑白雪+冰晶树挂+暴风雪粒子+冻湖 |
| T10 | **鬼域/冥土 Netherworld** | QiDensity≤25 ∧ Peril≥70 | 北漠古战场/江南太湖鬼潮 | 灰黑焦土+幽绿鬼火+枯骨+扭曲枯树+暗紫天光 |
| T11 | **灵脉福地 Spirit Land** | QiDensity≥70 ∧ Wealth≥70 | 东海剑墟/苗疆灵脉 | 金色灵气光点+灵芝仙草+祥云+柔和白光 |

---

## Layer 1 — 装饰物（<15% 覆盖率，透明背景，只在匹配的 Layer 0 变体上叠加）

### 植被装饰

| # | 装饰 | 放置条件（Layer 0 变体） | 覆盖率 |
|---|---|---|---|
| D01 | 野花丛 (flowers) | T01-A 草地·翠绿 | 12% |
| D02 | 蘑菇圈 (mushrooms) | T01-B 草地·茂密 | 8% |
| D03 | 灌木/浆果 (bush_berry) | T01-C 草地·枯黄 | 10% |
| D04 | 贝壳/海星 (shells) | T02-A 海面·平静 | 8% |
| D05 | 海鸥 (seagull) | T02-B 海面·波浪 | 5% |
| D06 | 浮木/漂流物 (driftwood) | T02-C 海面·暗涌 | 6% |
| D07 | 仙人掌 (cactus) | T03-A 沙地·金黄 | 8% |
| D08 | 枯骨/兽骸 (bones) | T03-B 沙地·碎石 | 5% |
| D09 | 盐碱结晶 (salt_crystal) | T03-C 沙地·龟裂 | 7% |
| D10 | 矿脉闪光 (ore_glint) | T04-B 岩壁·富矿 | 10% |
| D11 | 锻炉烟 (forge_smoke) | T04-A 岩壁·红褐 | 5% |
| D12 | 毒蘑菇/荧光菌 (toxic_fungus) | T05-A 密林·暗绿 | 10% |
| D13 | 垂藤 (hanging_vine) | T05-B 密林·藤蔓 | 8% |
| D14 | 瘴气粒子 (miasma_particle) | T05-C 密林·瘴紫 | 6% |
| D15 | 松果/松针 (pinecone) | T06-A 山峦·翠绿 | 10% |
| D16 | 灵芝仙草 (lingzhi) | T06-B 山峦·古树 | 5% |
| D17 | 云雾薄纱 (cloud_wisp) | T06-C 山峦·雾霭 | 8% |
| D18 | 浮萍/睡莲 (water_lily) | T07-A 水面·青绿 | 10% |
| D19 | 蜻蜓 (dragonfly) | T07-B 水面·莲花 | 8% |
| D20 | 水鸟/鹭鸶 (egret) | T07-C 水面·芦苇 | 7% |

### 破损/废墟装饰

| # | 装饰 | 放置条件 | 覆盖率 |
|---|---|---|---|
| D21 | 路面裂缝 (crack) | T01-C 草地·枯黄 ∨ T03-C 沙地·龟裂 | 8% |
| D22 | 碎石堆 (rubble) | T04-C 岩壁·熔隙 ∨ T08 火山 | 6% |
| D23 | 残垣断壁 (ruin_wall) | T10 鬼域 ∨ 古迹地标周边 | 4% |
| D24 | 古剑残片 (sword_fragment) | T05 林莽 ∨ 独孤剑冢周边 | 3% |

---

## Layer 2 — 稀有特征（<3% 覆盖率，极低概率，发现时叙事事件触发）

| # | 特征 | 放置条件 | 覆盖率 | 叙事钩子 |
|---|---|---|---|---|
| R01 | 宝箱/遗物 (treasure) | T01-B 草地·茂密 | 1% | 发现前人遗宝 → 获得功法残卷 |
| R02 | 沉船残骸 (shipwreck) | T02-C 海面·暗涌 | 2% | 探索沉船 → 获得海外奇物 |
| R03 | 上古石碑 (ancient_stele) | T03-C 沙地·龟裂 | 1.5% | 破译碑文 → 获得失落功法线索 |
| R04 | 地火喷口 (geyser) | T04-C 岩壁·熔隙 ∨ T08 火山 | 2% | 地火爆燃 → 获得异火/受伤 |
| R05 | 妖兽巢穴 (beast_lair) | T05-C 密林·瘴紫 | 2% | 遭遇妖兽 → 战斗/驯服 |
| R06 | 隐士草庐 (hermit_hut) | T06-C 山峦·雾霭 | 1% | 拜见隐士 → 获得指点/收徒 |
| R07 | 废弃渡口 (old_dock) | T07-C 水面·芦苇 | 1.5% | 发现渡口 → 解锁隐藏水路 |
| R08 | 玄铁陨坑 (meteor_crater) | T03-B 沙地·碎石 | 0.8% | 陨铁可铸神兵 |
| R09 | 灵泉涌出 (spirit_spring) | T11 灵脉福地 | 2% | 饮用灵泉 → 灵气+CP |
| R10 | 劫烬余痕 (calamity_scar) | T10 鬼域 | 1.5% | 触碰劫烬 → 触发 gen3 跨代恩怨回忆 |

---

## Layer 3 — 地标建筑（21 固定坐标锚点，非概率）

| # | Landmark Kind | 地标名 | Region | 描述 |
|---|---|---|---|---|
| L01 | 皇城 | 神京·紫宸城 | 0 中州 | 金顶宫殿群+朱红城墙+龙旗+禁军巡逻 |
| L02 | 学宫 | 稷下论道宫 | 0 中州 | 青瓦书院+竹林+学子辩论+藏书阁 |
| L03 | 雄关 | 镇玄关 | 0 中州 | 青石关隘+烽火台+守军+关门 |
| L04 | 祖庭 | 剑墟·万剑祖庭 | 1 东海 | 剑形山峰+剑气光柱+悬空石阶+剑碑林 |
| L05 | 巨港 | 沧澜巨港 | 1 东海 | 木栈码头+巨舰+起重机+货栈 |
| L06 | 古迹 | 独孤剑冢 | 1 东海 | 残剑插地+剑意裂痕+古藤+萤火 |
| L07 | 祖庭 | 铁佛寺·伏魔禅院 | 2 北漠 | 石砌寺庙+佛光金轮+经幡+降魔杵 |
| L08 | 雄关 | 玉门孤关 | 2 北漠 | 风化城墙+塞外风沙+烽燧+驼队 |
| L09 | 古迹 | 玄昊古战场 | 2 北漠 | 巨大陨坑+断裂兵器+劫烬黑烟+白骨 |
| L10 | 魔窟 | 血煞渊 | 2 北漠 | 暗红深渊裂隙+血雾+锁链+魔气 |
| L11 | 王庭 | 万器谷·百炼总坛 | 3 西陲 | 巨型锻炉+铁砧阵+蒸气锤+兵器架 |
| L12 | 巨港 | 流金商埠 | 3 西陲 | 繁华市集+金库+商旗+车队 |
| L13 | 险地 | 地火熔渊 | 3 西陲 | 熔岩湖+锻造台+火元素+黑烟 |
| L14 | 坞堡 | 镔铁坞 | 3 西陲 | 铁铸堡垒+烟囱+兵器库+铁水渠 |
| L15 | 魔窟 | 百蛊渊·噬魂魔宫 | 4 南疆 | 暗紫洞窟+蛊虫图腾+毒池+祭坛 |
| L16 | 险地 | 万毒尸沼 | 4 南疆 | 绿色毒沼+气泡+僵尸+毒藤 |
| L17 | 秘境口 | 幽冥鬼窟·入口 | 4 南疆 | 发光洞穴+符文封印+鬼火+石像 |
| L18 | 王庭 | 十万大山·古蛊王庭 | 5 苗疆 | 竹木王宫+图腾柱阵+蛊鼎+祭司 |
| L19 | 古迹 | 图腾神冢 | 5 苗疆 | 巨石图腾+祭祀火盆+古纹+骨饰 |
| L20 | 巨港 | 姑苏·烟雨坊市 | 6 江南 | 白墙黑瓦+石拱桥+画舫+灯笼 |
| L21 | 险地 | 太湖鬼潮渚 | 6 江南 | 暗潮水面+鬼船残骸+水妖+迷雾 |

---

## 边境/路径瓦片（Layer 0 邻接边专用）

| # | 类型 | 描述 |
|---|---|---|
| B01 | 道路/商路 (road) | 土路+车辙+马蹄印，Open 边 |
| B02 | 关隘门控边 (pass_gate) | 关门+栅栏+检查站旗帜，通牒边 |
| B03 | 境界门控边 (realm_gate) | 半透明灵力气墙+符文+光效，境界边 |

---

## 素材汇总

| 层 | 数量 | 明细 |
|---|---|---|
| Layer 0 基础地形 | **21** | 7地形×3变体 |
| Layer 0 特殊变体 | **4** | 火山/雪原/鬼域/灵脉 |
| Layer 1 植被装饰 | **20** | D01-D20 |
| Layer 1 破损装饰 | **4** | D21-D24 |
| Layer 2 稀有特征 | **10** | R01-R10 |
| Layer 3 地标建筑 | **21** | L01-L21 |
| 边境/路径 | **3** | B01-B03 |
| **总计** | **83** | |

## Stories

### Must Have

- **mv-001** 瓦片素材生成管线 (2d)
  - 83 种瓦片 AI prompt 模板（每瓦片含：地形描述+像素规格 48×48+色彩约束+光影方向左上）
  - Pillow 程序化生成 fallback（icon_gen.py 同类模式）
  - Godot TileSet Atlas 打包脚本（自动切片+Source ID 映射表）

- **mv-002** 四层叠加渲染引擎 (2d)
  - 确定性哈希函数 `hash_cell(x,y,seed,layer)` → 概率判定+变体选择
  - Layer 0：全面铺满——按 hash%100 落 60/25/15 概率带的变体
  - Layer 1：装饰叠加——hash 后与阈值比较（<覆盖率才放置），仅匹配特定 Layer 0 变体
  - Layer 2：稀有特征——同 Layer 1 但阈值更低(<3%)
  - Layer 3：地标建筑——NodeId 坐标精确匹配，非概率
  - TileMapLayer 批量 `set_cell` 写入
  - 区块生成 `generate_chunk(chunk_pos, chunk_size)` 支持无缝懒加载

- **mv-003** 边境连线 + Region 边界 (1d)
  - Region 边界线渲染（RegionId 变化处半透明色带）
  - 邻接边道路/关隘/境界门控线（B01-B03，RegionEdge 数据驱动）
  - 地缘张力可视化（同区接壤=实线/跨区远程=虚线）

### Should Have

- **mv-004** Core 地形字段补齐 (1d)
  - `RegionDef` 加 `TerrainKind`/`Element`/`Peril`/`HazardKind` 字段（当前代码仅有 Wealth/QiDensity/Strategic）
  - `NodeGeo` 加 `BiomeVariant`/`QiLayer` 字段
  - 纯 L0 数据行——不改算法、不破 B.3 off 逐字节

- **mv-005** 视觉层级 LOD + 动态秘境 (1d)
  - 缩放 LOD：大区级(Region) → 地标级(Landmark) → Site级 三级切换
  - 秘境未显形：半透明覆盖+问号标记
  - QiDensity 可视化：35-55衔接带偏暗/56-100厚灵区灵气光点密度渐增

### Movement System

- **mv-006** TileSet 物理碰撞层 + CharacterBody2D 移动基础 (2d)
  - TileSet 加 Physics Layer：海域(T02)/山岳(T04-T06)/地标建筑(L01-L21)绘制碰撞多边形
  - 角色节点类型从 `Node2D` 重构为 `CharacterBody2D`
  - WASD 八方向平滑移动（`Input.get_vector` + `move_and_slide()`）
  - 碰撞自动处理：海边自然阻挡+沿障碍物平滑滑动——不依赖"检查下一格是否可走"
  - 速度常量：玩家 150px/s，AI 100px/s

- **mv-007** AStarGrid2D 寻路 + 路点平滑跟随 (2d)
  - `AStarGrid2D` 基于 TileMap 网格构建（`tilemap.local_to_map` / `map_to_local`）
  - 玩家鼠标点击→世界坐标→A* 路径→路点队列→`_physics_process` 逐路点跟随
  - AI 决策目标（Travel to Site/Sect/Hub）→同上 A* 路点平滑跟随
  - 路点容差半径 5px，到点即切下一个路点
  - 不阻塞模拟 Tick：移动中仍可 Advance（位置是 View 层插值，不影响 Core NodeId）

- **mv-008** Steering Behaviors — AI 移动自然化 (1d)
  - **Wander Jitter**：AI 跟随路点时，velocity 叠加 `FastNoiseLite` 2D 噪音向量——轨迹呈平滑波浪线/轻微蛇形，非机器直线
  - **Separation**：同屏多 AI 检测距离——过近时 velocity 加反向推力，自然散开，形成有机"人群"感
  - 纯 View 层——不改 Core 决策、不碰 B.2/B.3

### Core 地形字段补齐

- **mv-004** Core 地形字段补齐 (1d)
  - `RegionDef` 加 `TerrainKind`/`Element`/`Peril`/`HazardKind` 字段
  - `NodeGeo` 加 `BiomeVariant`/`QiLayer` 字段
  - 纯 L0 数据行——不改算法、不破 B.3 off 逐字节

## 素材 prompt 模板示例

```
像素画，48×48，俯视视角，游戏瓦片，左上光源。
主题：[变体描述，如"草地·茂密：高草丛+蝴蝶"]。
色彩：4-5色调色板，暗部冷色偏蓝，亮部暖色偏黄。
风格：16-bit RPG 像素，清晰边缘，无抗锯齿，不超出画布。
透明背景（Layer 1/2 素材专用）。
```
