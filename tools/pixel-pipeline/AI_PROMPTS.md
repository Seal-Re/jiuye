# 像素角色部件 · AI 绘图 prompt 清单

> 拿这些 prompt 去任意 AI 绘图工具（Midjourney / SD / 即梦 / 可灵 / DALL·E）出图，回来按 `PARTS_CONTRACT.md` 修整丢进 `pixel/parts/<层>/`。
> **通用前缀**（每个 prompt 都加）：`pixel art, 32x48 sprite, transparent background, hard edges no anti-aliasing, limited 5-tone palette, top-left light source, wuxia xianxia ancient chinese style, full body standing front view, anchored bottom-center, retro game sprite sheet style`
> **通用负向**（SD 类填 negative）：`anti-aliasing, blur, gradient, anime portrait, chibi, 3d render, realistic photo, watermark, text, multiple characters`

---

## 1_body 体型底（2-3 款，肤色，会被袍服盖大半）

| 文件 | prompt(接通用前缀) |
|---|---|
| body_lean_v1 | `a slender lean adult male martial artist body base, neutral pose, bare/underclothes, skin tone shading, no equipment` |
| body_brawny_v1 | `a muscular broad-shouldered martial artist body base, sturdy stance, bare torso hint, skin tone shading` |
| body_female_v1 | `a slender female cultivator body base, graceful stance, plain underrobe` |

## 3_robe 袍服（**用中性灰阶 ramp 出**，loader 换色 → 一款多色；按 path 门类出款式）

> 出图加：`drawn in 5-step GRAYSCALE ramp only (dark to light grey), so it can be recolored` —— 这是 palette-swap 的关键。

| 文件 | path | prompt |
|---|---|---|
| robe_sword_v1 | 剑修 | `flowing scholar swordsman long robe with sash, elegant clean lines, sleeves` |
| robe_body_v1 | 体修 | `sleeveless rugged tunic, exposed muscular arms, cloth wraps, martial` |
| robe_alchemist_v1 | 丹修 | `alchemist robe with hanging pouches and apron, practical, herb satchel` |
| robe_buddhist_v1 | 佛修 | `buddhist monk kasaya robe, simple draped cloth, one shoulder` |
| robe_demon_v1 | 魔修 | `dark sinister long robe, ragged hem, ominous, high collar` |
| robe_artificer_v1 | 器修 | `artificer robe with tool belt and talisman straps, refined` |
| robe_ghost_v1 | 鬼修 | `tattered ghostly robe, wispy frayed edges, eerie` |
| robe_confucian_v1 | 儒修 | `confucian scholar gown, wide sleeves, dignified, sash` |

## 4_hair 头发（2-4 款）

| 文件 | prompt |
|---|---|
| hair_topknot_v1 | `ancient chinese male hair in a topknot bun with hairpin, side strands` |
| hair_long_v1 | `long flowing black hair down the back, ancient style` |
| hair_short_v1 | `short tied-back warrior hair` |
| hair_female_v1 | `female cultivator hair with ornamental hairpin, partial updo` |

## 5_face 面部（小，眼/眉/须；可少款）

| 文件 | prompt |
|---|---|
| face_calm_v1 | `calm composed face, simple pixel eyes and brows, no expression` |
| face_fierce_v1 | `fierce determined face, sharp brows` |
| face_elder_v1 | `elder face with beard and wise expression` |

## 7_weapon 手持武器（**固定成品色**，按 path 兵器）

| 文件 | path | prompt |
|---|---|---|
| weapon_sword_v1 | 剑修 | `a slender straight chinese jian sword held vertically, steel blade with fuller, ornate guard and pommel` |
| weapon_saber_v1 | 体/魔 | `a curved chinese dao saber, broad blade` |
| weapon_staff_v1 | 佛/法 | `a long monk staff or wooden cultivator staff` |
| weapon_fan_v1 | 儒/音 | `a folding scholar fan, ornate` |
| weapon_whip_v1 | 驭兽 | `a soft chain whip weapon` |

## 6_back 背挂（剑鞘/符囊，画在身后）

| 文件 | prompt |
|---|---|
| back_sheath_v1 | `a sword sheath/scabbard worn diagonally on the back, ornate` |
| back_talisman_v1 | `a talisman pouch and paper charms hanging on the back` |

## 8_accessory 配饰（按 path）

| 文件 | path | prompt |
|---|---|---|
| acc_gourd_v1 | 丹修 | `a small alchemist medicine gourd hanging at the waist, glowing` |
| acc_artifact_v1 | 器修 | `a small floating glowing magic artifact orb beside the head` |
| acc_beads_v1 | 佛修 | `buddhist prayer beads worn` |
| acc_gu_v1 | 毒蛊 | `a small poison gu insect jar at waist` |

## 0_aura / 9_aura_front 光环（realm 高叠加；也可纯 code 程序生成，AI 可选）

| 文件 | prompt |
|---|---|
| aura_qi_v1 | `swirling spiritual qi energy aura ring, glowing particles, transparent background, no character` |
| aura_gold_v1 | `golden buddhist halo glow ring, radiant` |

---

## 出图工作流建议

1. **优先出可复用的"通用件"**：body(2-3) + hair(3-4) + face(3) —— 这些跨所有 path 复用，性价比最高。
2. **再出 path 特色件**：robe(8 门类) + weapon(5) + accessory(4) —— 给每路身份感。
3. **光环可不出**：code 已能程序生成（char_gen 的 aura()），AI 出更精则替换。
4. 每类先出 v1 验证管线，满意再扩 v2/v3 变体（同款多变体 = 同 path 不同长相）。
5. 出图后按 `PARTS_CONTRACT.md` §5 修整对齐 32×48 + 抠透明底，丢进 `pixel/parts/`。
