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

1. **48×48 native** — 导出时 4x NEAREST 缩放 (192×192)
2. **左上光源** — light from top-left, shadow on bottom-right
3. **5-tone ramp** — 暗部冷色调→亮部暖色调, hue-shift
4. **Selout 描边** — 实心像素边缘→最暗色 1px 描边
5. **禁止**: 抗锯齿 / 模糊 / 半透明渐变 / 非像素颗粒 / 分辨率不一致
6. **Layer 1/2**: 透明背景 (transparent)
7. **Layer 0/3**: 纯色背景 (solid)

## 生成流程

1. 逐条 prompt → SunshineFlow API (pixel-art-fs 蓝图)
2. 后处理: 对齐 48×48 + 调色板量化 (如 AI 产出尺寸不对)
3. 打包: `python tools/pixel-pipeline/pack_atlas.py`
