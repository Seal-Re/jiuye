# 像素地图渲染器 — 设计规范

> 日期: 2026-06-25 | 状态: Design | 关联: Map系统(C), 像素管线, 红线B.8

## 1. 概述

插拔式地图渲染引擎——将 WorldMap 数据（区域、站点、邻接、地形）渲染为像素地图。四接口架构：IMapRenderer（渲染器）、IMapLayer（图层）、IAssetProvider（资产）、IMapDataSource（数据源）。全部可替换、可独立测试。

**红线 B.8 双轨**：像素渲染器（Pillow，32×32 tile）用于游戏画面；古风 SVG 渲染器（HTML-CSS，水墨/卷轴）用于 UI 界面。两渲染器共用数据源和资产接口。

## 2. 接口定义

### 2.1 IMapDataSource（数据源——纯数据，零渲染依赖）

```python
class MapData:
    """地图纯数据——从 WorldMap、seed 或 JSON 构造。不可变。"""
    regions: List[RegionData]      # 区域列表
    sites: List[SiteData]          # 站点列表（NodeId = index）
    adjacency: List[List[int]]     # 邻接表（已排序）
    node_count: int
    region_count: int

class RegionData:
    name: str
    center_x: int; center_y: int
    wealth: int; qi: int; strategic: int

class SiteData:
    kind: str           # "normal" | "resource" | "secret" | "sect"
    region_id: int
    resource_amount: int
    danger_tier: int
```

**构造方式**（MapDataBridge）：
- `MapDataBridge.from_worldmap(wm: WorldMap)` — 从 C# 侧直接构造（内存）
- `MapDataBridge.from_seed(seed: int, config: MapConfig)` — Python 独立运行（重复 C# 生成算法）
- `MapDataBridge.from_json(path: str)` — 从 CLI dump 文件导入

### 2.2 IAssetProvider（资产提供者——可替换）

```python
class IAssetProvider(ABC):
    """资产提供者接口。加资产风格 = 加实现。"""
    def get_site_icon(self, kind: str, size: int) -> Image
    def get_region_tile(self, biome: str, size: int) -> Image
    def get_palette(self) -> dict
```

**内置实现**：

| 实现 | 风格 | 用途 |
|------|------|------|
| `ProgrammaticIconProvider` | 几何母题 + ramp（纯 Pillow） | 默认——零外部依赖 |
| `AIGenIconProvider` | SunshineFlow 文生图（后期） | 替换——AI 生成精细图标 |

**ProgrammaticIconProvider 图标定义**（48×48，每类一个几何母题）：
- 普通站点 = 小屋（三角形顶 + 矩形底，棕 ramp）
- 资源站点 = 矿石（菱形 + 高光，金 ramp）
- 秘境 = 问号（弧形 + 紫 ramp）
- 宗门 = 三重塔（三层矩形 + 金 ramp）
- 区域瓦片 = 噪声纹理 + 区域色（32×32，无缝）

### 2.3 IMapLayer（图层——可插拔）

```python
class IMapLayer(ABC):
    """图层接口。priority 决定渲染顺序（0=最底）。"""
    priority: int
    def render(self, canvas: Image, data: MapData, config: MapRenderConfig, assets: IAssetProvider) -> None
```

**内置图层**（按 priority 排序）：

| priority | Layer | 职责 |
|:--------:|-------|------|
| 0 | `TerrainLayer` | 区域底色 + 噪声纹理 |
| 1 | `RoadLayer` | 站点间连接线（灰色 Bresenham 线） |
| 2 | `SiteLayer` | 站点图标（48×48）+ 资源量标注 |
| 3 | `LabelLayer` | 地名标注 + 区域名（像素字体） |
| 4 | `FogLayer` (后期) | 战争迷雾（未探索区域黑色覆盖） |
| 5 | `TerritoryLayer` (后期) | 门派领地色框 |

### 2.4 IMapRenderer（渲染器——可替换）

```python
class IMapRenderer(ABC):
    """地图渲染器接口。加渲染风格 = 加实现。"""
    def render(self, data: MapData, config: MapRenderConfig, assets: IAssetProvider) -> Image
```

**内置实现**：

| 实现 | 输出 | 用途 |
|------|------|------|
| `PixelMapRenderer` | PNG (RGBA), Pillow | 游戏画面（32×32 tile） |
| `SvgMapRenderer` (后期) | SVG/HTML-CSS | UI 界面（古风水墨/卷轴，匹配红线 B.8） |

## 3. 数据流

```
WorldMap (C#) ──或── seed + MapConfig (Python)
        │
        ▼
  MapDataBridge
        │
        ▼
  MapData (纯数据，不可变)
        │
        ├──────────────────────────────┐
        ▼                              ▼
  PixelMapRenderer              SvgMapRenderer (后期)
        │                              │
        ▼                              ▼
  ┌─────────────┐              ┌─────────────┐
  │ Layer Stack │              │ Layer Stack │
  │ 0 Terrain   │              │ 0 Terrain   │
  │ 1 Road      │              │ 1 Road      │
  │ 2 Site      │              │ 2 Site      │
  │ 3 Label     │              │ 3 Label     │
  └──────┬──────┘              └──────┬──────┘
         │                            │
         ▼                            ▼
  IAssetProvider              IAssetProvider
  (ProgrammaticIcon)         (ProgrammaticIcon)
         │                            │
         ▼                            ▼
    output/map.png              output/map.svg
```

## 4. 配置

```python
@dataclass
class MapRenderConfig:
    # 画布
    tile_size: int = 32          # 每个站点占 tile_size × tile_size
    padding: int = 16            # 画布边缘留白
    scale: int = 2               # 输出缩放（NEAREST，整数倍）
    
    # 图层
    layers: List[str] = field(default_factory=lambda: ["terrain", "road", "site", "label"])
    
    # 视觉
    palette_name: str = "default"   # 调色板：default / warm / cold / ink (水墨)
    show_labels: bool = True
    show_resources: bool = True
    road_color: tuple = (100, 100, 100, 200)
    
    # 高级
    fog_mask: Set[int] | None = None     # 迷雾节点集
    highlight_sites: Set[int] | None = None  # 高亮站点
```

## 5. 模块结构（9 文件）

```
tools/pixel-pipeline/
  ├── map_renderer/
  │     ├── __init__.py              # 公开 API
  │     ├── interfaces.py            # IMapRenderer, IMapLayer, IAssetProvider
  │     ├── map_data.py              # MapData, MapDataBridge, RegionData, SiteData
  │     ├── render_config.py         # MapRenderConfig
  │     ├── pixel_renderer.py        # PixelMapRenderer
  │     ├── icon_provider.py         # ProgrammaticIconProvider (默认)
  │     ├── layers.py                # TerrainLayer, RoadLayer, SiteLayer, LabelLayer
  │     ├── terrain_palette.py       # 区域色板 + 噪声纹理生成
  │     └── pixel_font.py            # 像素字体渲染 (4×6 或 5×7)
```

## 6. 接口隔离 + 可测试性

```
每接口 ≤ 3 个公共方法          ← 接口最小化
每层 ≤ 2 个依赖（data, assets）← 依赖显式注入
每文件 ≤ 150 行                ← 单文件可读
每层独立测试                    ← stub data + stub assets → 验证 layer.render
渲染器端到端测试                ← real data + real assets → 验证输出尺寸+颜色
```

**测试清单**：
- `test_map_data.py` — MapData 构造（from_seed 确定性，from_json 可解析）
- `test_icon_provider.py` — ProgrammaticIconProvider 四类图标输出尺寸正确
- `test_layers.py` — 每层独立渲染（stub canvas + stub data → 像素正确位置）
- `test_pixel_renderer.py` — 端到端：MapData + config + assets → PNG 输出
- `test_terrain_palette.py` — 色板生成确定性 + 无缝 tile 验证

## 7. 抗灾

| 灾难 | 响应 |
|------|------|
| IAssetProvider.get_icon 抛异常 | 回退到纯色方块（fallback_icon = 16×16 单色矩形） |
| MapData 数据损坏 | from_seed 重新生成；from_json 抛清晰错误消息 |
| 图层渲染失败 | 跳过该层，其余层继续（日志记录） |
| 画布超出内存 | RenderConfig 限制 max_canvas = 8192×8192 |

## 8. 红线合规

- **B.2 确定性**：同 seed + 同 config → 输出逐像素一致（Python Pillow 整数像素操作）
- **B.8 双轨**：PixelMapRenderer（游戏画面，Pillow）+ SvgMapRenderer（UI，古风，预留接口）
- **无外部网络依赖**：默认 ProgrammaticIconProvider 纯 Pillow 运行。AIGenIconProvider 可选
- **Python 3.10+**，依赖仅 Pillow + numpy

## 9. 交付物

| 文件 | 描述 |
|------|------|
| `map_renderer/` 包 (9 文件) | 渲染引擎 |
| `output/map_seed42_scale2.png` | 示例输出 |
| 测试 (5 文件) | 覆盖所有接口 |
| `map_renderer/README.md` | 使用文档 |
