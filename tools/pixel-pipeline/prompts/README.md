# 九野 · AI 素材 Prompt 目录 v3

## 四层新架构

```
Layer 3 — 顶棚遮罩    (Canopy, 7 tiles)
          树冠/屋顶/云雾 → Z-Sorting 渲染在角色头顶
Layer 2 — 模块化建筑   (Building, 20 tiles) 🆕
          屋顶/墙体/门窗/台阶/石狮/图腾柱 → 地编拼出任意城池
Layer 1 — 微缩装饰物   (Decoration, 20 tiles) 🆕
          孤立物件居中 5-15px, 纯绿幕 #00FF00 → 后处理 1-bit Alpha
Layer 0 — Autotile 地形 (36 tiles) 🆕
          草地/沙地 各13块无缝拼接套件 + 水域/岩壁/密林/水泽中心
```

## 素材统计

| 层 | 文件 | 数量 |
|---|---|---|
| Layer 0 | `layer0_autotile.yaml` | 36 |
| Layer 1 | `layer1_decoration.yaml` | 20 |
| Layer 2 | `layer2_building.yaml` | 20 |
| Layer 3 | `layer3_canopy.yaml` | 7 |
| 图标 | `paths/21_paths.yaml` | 21 |
| **总计** | | **104** |

## 关键变更 (v2→v3)

1. **废弃单图块城池** → 模块化拼积木 (Layer 2)
2. **废弃透明背景字眼** → 纯绿幕 #00FF00 (Layer 1/2/3)
3. **微缩物件** → 48x48画布, 物件仅5-15px居中 (Layer 1)
4. **Autotile 无缝拼接** → 13块套件 (Layer 0)
5. **Z-Sorting** → 顶棚层渲染在角色上方 (Layer 3)
