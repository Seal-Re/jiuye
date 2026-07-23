# 九野 · AI 素材 Prompt 目录

## 目录结构

```
prompts/
├── README.md              ← 本文件
├── paths/
│   └── 21_paths.yaml      ← 21路修炼图标 prompt (含视觉宪法)
└── tiles/
    └── 83_tiles.json      ← 83种瓦片 prompt (待按像素规范重写)
```

## 生成目标

| 类别 | 数量 | 尺寸 | 用途 |
|---|---|---|---|
| 修炼图标 | 21 | 48×48 → 192×192 | 角色创建面板/角色信息卡 |
| 地形瓦片 | 25 | 48×48 | Layer 0 基础地表 |
| 装饰物 | 24 | 48×48 | Layer 1 植被/破损 |
| 稀有特征 | 10 | 48×48 | Layer 2 宝箱/遗迹 |
| 地标建筑 | 21 | 48×48 | Layer 3 皇城/祖庭/魔窟 |
| 边境 | 3 | 48×48 | 道路/关隘/门控 |

## 视觉宪法 (继承 path_icons.png 程序化版)

所有素材必须遵守：
1. **48×48 native** → 导出时 4x NEAREST 缩放
2. **左上光源** (light top-left, shadow bottom-right)
3. **5-tone ramp** 色阶 (暗冷→亮暖, hue-shift)
4. **Selout** 选择性轮廓描边
5. **斜角卡框** (仅图标: bevel frame + tier 宝石)
6. **禁止**: 抗锯齿/模糊/半透明渐变/非像素颗粒

## 生成流程

1. 本地预览: `python tools/pixel-pipeline/gen_tiles.py` (Pillow 占位)
2. AI 生成: 逐条 prompt → SunshineFlow API
3. 后处理: 对齐像素格 + 调色板量化 (如有需要)
4. 打包: `python tools/pixel-pipeline/pack_atlas.py`
