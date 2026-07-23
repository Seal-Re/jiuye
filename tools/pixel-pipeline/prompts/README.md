# 九野 · AI 素材 Prompt 目录

## 目录结构

```
prompts/
├── README.md              ← 本文件
├── paths/
│   └── 21_paths.yaml      ← 21路修炼图标 (v2, 已按像素宪法重写)
└── tiles/
    ├── 83_tiles.yaml       ← 83种瓦片 (v2, 已按像素宪法重写) 🆕
    └── 83_tiles.json       ← 旧版 (deprecated, 勿用于生成)
```

## 生成目标

| 类别 | 数量 | 尺寸 | 用途 |
|---|---|---|---|
| 修炼图标 | 21 | 48×48 | 角色创建面板/角色信息卡 |
| 地形瓦片 | 25 | 48×48 | Layer 0 基础地表 |
| 装饰物 | 24 | 48×48 | Layer 1 植被/破损 (透明背景) |
| 稀有特征 | 10 | 48×48 | Layer 2 宝箱/遗迹 (透明背景) |
| 地标建筑 | 21 | 48×48 | Layer 3 皇城/祖庭/魔窟 |
| 边境 | 3 | 48×48 | 道路/关隘/门控 |
| **总计** | **104** | | |

## 视觉宪法

所有素材必须遵守 (继承 path_icons.png 程序化版的视觉参数):

1. **48×48 native** — 导出时 4x NEAREST 缩放 (192×192)。每个像素 = 1 游戏单位，全图统一像素粒度
2. **左上光源** — light from top-left, shadow on bottom-right
3. **5-tone ramp** — 暗部冷色调→亮部暖色调, hue-shift。后处理 color_quantize=5 强制收敛
4. **Selout 描边** — 实心像素边缘→最暗色 1px 描边
5. **禁止**: 抗锯齿 / 模糊 / 半透明渐变 / 非像素颗粒 / 分辨率不一致 / 像素大小不一
6. **Layer 1/2**: 透明背景 (transparent)
7. **Layer 0/3**: 纯色背景 (solid)

### Alpha 通道 1-bit 二值化

- Alpha 仅作 `0` (完全透明) 或 `255` (完全不透明) 二值判别
- 严禁 1~254 之间的半透明边缘像素 (NO anti-aliasing)
- 烟雾/瘴气/云雾等半透明视觉效果→**抖动棋盘格像素** (dithered checkerboard pattern) 表现
- 后处理 `alpha_threshold=128` 强制阈值化

### 负向提示词 (global_negative_prompt)

调用 API 时与每条 prompt 自动拼接:
```
anti-aliasing, soft alpha gradient, semi-transparent blending, blur,
3D rendering, sub-pixel rendering, smooth gradient, vector art,
photorealistic, high resolution texture, mixed pixel sizes
```

### 管线配置

详见 `pipeline_config.yaml`——生成脚本读取后自动注入 API 调用。

## 生成流程

1. 脚本读取 `pipeline_config.yaml` → 拼接 `global_negative_prompt`
2. 逐条 prompt → SunshineFlow API (pixel-art-fs 蓝图)
3. 后处理: `alpha_threshold=128` + `color_quantize=5`
4. 打包: `python tools/pixel-pipeline/pack_atlas.py`
