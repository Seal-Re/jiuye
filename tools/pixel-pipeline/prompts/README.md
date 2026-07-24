# 九野 · AI 素材 Prompt 目录 v4 (大图切片 + 调色板量化)

## 管线架构

```
Terrain Group Sheet (192×192 大图, 4×4网格)
  → AI 单次生成 (Self-Attention 保证纹理/色彩一致)
  → Python 48×48 切片 (Grid Crop)
  → 全局调色板量化 (32色 LUT, K-Means)
  → 输出 v1_tiles/
```

## 输出版本管理

```
out/
├── v1_sheets/     AI 生成的大图 (192×192 PNG, 含alpha+noalpha)
├── v1_tiles/      切片后的 48×48 瓦片 (调色板量化后)
└── v1_atlas/      打包后的 TileSet Atlas
```

> 废弃版本直接删除整个目录。当前仅保留 v1。

## 大图布局规范

每个 Terrain Group Sheet = 192×192 (4×4 grid, 每格48×48):

```
┌──────┬──────┬──────┬──────┐
│NW外角│ N边  │NE外角│ N边  │
├──────┼──────┼──────┼──────┤
│ W边  │CENTER│ E边  │CENTER│
├──────┼──────┼──────┼──────┤
│SW外角│ S边  │SE外角│ S边  │
├──────┼──────┼──────┼──────┤
│ W边  │CENTER│ E边  │CENTER│
└──────┴──────┴──────┴──────┘
```

中心2×2 = 4个center (纯地形)
边缘 = 8个edge (4方向×2)
角落 = 4个outer-corner

内角由边缘+center组合在引擎层处理, 不单独生成。
