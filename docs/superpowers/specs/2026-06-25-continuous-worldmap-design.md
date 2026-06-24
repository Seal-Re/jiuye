# 连续大地图系统 — 设计规范 v2

> 日期: 2026-06-25 | 状态: Design | 替换: 2026-06-25-map-renderer-design.md（节点图版，废弃）
> 参考: 鬼谷八荒（grid-tilemap continuous world）

## 0. 架构批判与修正（已登记）

| # | 隐患 | 修正 |
|---|------|------|
| 1 | C#/Python 各自实现噪声 → 确定性撕裂 | **SSOT：C# 唯一生成地形。Python 通过共享数据读取，不自行计算** |
| 2 | float 大坐标精度溢出 | **GlobalPos = Chunk + offset。offset ∈ [0,64)** |
| 3 | 扁平数组 O(N²) 查找 | **空间哈希网格。查找 = 当前 Chunk + 8 邻居** |
| 4 | off-screen 回退 NodeId → 瞬移 | **所有角色保留 float 坐标。LOD 分级驱动（16ms/1000ms/5000ms），不丢坐标** |
| — | Voronoi O(N) 查询 | **Worley Grid：每 Chunk 隐式 1-2 特征点。查询仅扫 ~18 点 = O(1)** |

## 1. 核心数据模型

### 1.1 GlobalPos——全栈唯一坐标格式

```csharp
public struct GlobalPos {
    public int ChunkX, ChunkY;          // Chunk 坐标
    public float OffsetX, OffsetY;      // [0.0, 64.0) Chunk 内相对坐标
}
```

**转换**：`WorldPos(wx, wy) => ChunkX=(int)(wx/64), OffsetX=wx%64`（浮点取模）

### 1.2 TerrainConfig——全部参数集中

```csharp
public sealed record TerrainConfig(
    // 世界规模
    int WorldSizeChunks = 128,           // 128×128 = 16384 chunks
    float ChunkSize = 64.0f,             // 每 Chunk 世界单元
    
    // Voronoi 区域
    int RegionChunks = 8,                // 每区域 = 8×8 Chunk
    int FeaturesPerChunk = 1,            // 每 Chunk 隐式特征点数（1-2）
    
    // 生态群落权重（8 种，和为 100）
    int WtGrassland = 25, int WtDesert = 10, int WtForest = 20,
    int WtSnow = 10, int WtSwamp = 8, int WtMountain = 12,
    int WtCoast = 5, int WtWasteland = 10,
    
    // 噪声参数
    int ElevationSeed = 0, int MoistureSeed = 1, int DetailSeed = 2,
    float ElevationScale = 256.0f,       // 噪声频率（越小越密）
    float MoistureScale = 320.0f,
    
    // 站点密度
    int SitesPer1024 = 3,                // 每 1024×1024 单元 3 个站点
    int SiteWtNormal = 60, int SiteWtResource = 25,
    int SiteWtSecret = 12, int SiteWtSect = 3,
    
    // LOD 分级
    int Pool16msRadius = 3,              // 视野内 = 3 Chunk 半径
    int Pool1000msRadius = 8,            // 边缘 = 8 Chunk
    // Pool5000ms = 其余全部
);
```

### 1.3 WorldTerrain——静态基底（SSOT，C# 生成）

```csharp
public sealed class WorldTerrain {
    // 核心查询——底层封装 Worley Grid，O(1)
    BiomeType GetBiome(GlobalPos pos);
    float GetElevation(GlobalPos pos);
    float GetMoisture(GlobalPos pos);
    
    // 内部实现
    // - Perlin noise 采样（纯整数种子，确定性）
    // - Worley Grid：每 Chunk→hash→1-2 特征点
    // - Voronoi 查询：当前 Chunk + 8 邻居 → ~18 点 → 最近距离
    
    // 寻路拓扑缓存
    IReadOnlyDictionary<ChunkCoord, PathNode> NavigationMesh { get; }
}

public enum BiomeType { Grassland, Desert, Forest, Snow, Swamp, Mountain, Coast, Wasteland }
```

**生成流程：**
```
1. 遍历 (cx=0..WorldSizeChunks, cy=0..WorldSizeChunks)
     hash(cx,cy,seed) → 1-2 个 (featureX, featureY, biome) 隐式特征点
2. 不做显式 Voronoi 数组。查询时实时计算最近特征点
3. Perlin noise(cx*64+ox, cy*64+oy) → elevation, moisture
4. biome = Voronoi中心biome + noise微调（边缘混合）
```

**Clone()**: 浅拷——地形生成后冻结不可变。

### 1.4 WorldEntities——动态实体（空间哈希 + LOD）

```csharp
public sealed class WorldEntities {
    // 空间哈希
    Dictionary<ChunkCoord, EntityChunk> ActiveChunks;
    
    // LOD 分级驱动
    List<Character> Pool16ms;     // 逐帧物理
    List<Character> Pool1000ms;   // 每秒简易避障
    List<Character> Pool5000ms;   // 5 秒 Waypoint 跃进
    
    // 站点
    Dictionary<ChunkCoord, List<Site>> Sites;
    
    void MoveEntity(Character c, GlobalPos newPos);  // 自动迁移 Chunk
    IReadOnlyList<Character> Nearby(GlobalPos pos, float radius);  // O(1)
}
```

**角色位置**：`Character.Position: GlobalPos`——**永不回退 NodeId**。LOD 只影响 tick 频率，坐标精度永远保持。

## 2. 接口隔离（解耦层级）

```
ITerrainQuery（纯读取——Brain/Action 只能读，不能写地形）
  ├─ GetBiome(GlobalPos)
  ├─ GetElevation(GlobalPos)
  └─ GetNavigationNode(ChunkCoord)

ISpatialIndex（实体查询——Brain 找周围对象）
  ├─ Nearby(GlobalPos, float radius) → IReadOnlyList<Character>
  └─ SitesNear(GlobalPos, float radius) → IReadOnlyList<Site>

ITerrainRenderer（渲染器——Python 侧只依赖此接口）
  ├─ RenderChunk(ChunkCoord, output: bytes) → 64×64 tile 位图
  └─ GetPalette() → BiomeType → (r,g,b)

ITerrainGenerator（生成器——可插拔算法）
  ├─ Generate(config) → WorldTerrain
  └─ Name: string
```

**关键解耦**：
- WorldTerrain **不知道** WorldEntities 存在
- WorldEntities **不知道** TerrainConfig 的内部噪声参数
- Python 渲染器**只知道** ITerrainRenderer——不碰 C# 噪声代码
- 生成器可替换——`ITerrainGenerator` 新实现 = 新地形算法

## 3. C# ↔ Python 桥接

**纯粹方案（SSOT）：** C# CLI dump → JSON → Python 读取渲染。

```
C# side:
  dotnet run --project src/Jianghu.Cli -- --dump-terrain --seed 42 --out terrain.json
  → 输出: { chunks: [...], biome_grid: [...], sites: [...] }

Python side:
  terrain = TerrainData.from_json("terrain.json")
  renderer = PixelMapRenderer(config)
  renderer.render(terrain) → worldmap.png
```

**不做**：Python 侧自行计算噪声。不做跨语言同步。Python 只读取 C# 输出。

## 4. 模块结构

```
src/Jianghu.Core/Sim/
  ├── TerrainConfig.cs          ← 全部参数
  ├── GlobalPos.cs              ← Chunk+offset 坐标
  ├── ITerrainQuery.cs          ← 只读地形接口
  ├── ITerrainGenerator.cs      ← 可插拔生成器接口
  ├── WorleyTerrainGenerator.cs ← 默认生成器（Worley+Perlin）
  ├── WorldTerrain.cs           ← 静态地形（纯数据+查询，SSOT）
  ├── ISpatialIndex.cs          ← 空间查询接口
  ├── WorldEntities.cs          ← 空间哈希+LOD 动态实体
  └── TerrainRenderer.cs        ← ITerrainRenderer 实现（供 Python 桥接）

tools/pixel-pipeline/map_renderer/
  ├── terrain_data.py           ← TerrainData（从 JSON 读取，不自行计算）
  ├── render_config.py          ← RenderConfig（tile size, scale, output）
  ├── pixel_renderer.py         ← 读取 TerrainData → 逐 Chunk 渲染 64×64 tile
  ├── biome_palette.py          ← BiomeType → 颜色 ramp
  └── main.py                   ← CLI: python -m map_renderer.main --json terrain.json --out map.png
```

**Python 侧只有 5 个文件——纯渲染。零地形生成逻辑。**

## 5. 红线合规

- **B.2 确定性**：同 seed → 同地形 → 同渲染输出。C# 唯一生成。
- **B.8 双轨**：像素渲染器（Pillow）→ 游戏画面。古风 SVG 渲染器可后期追加。
- **整数确定性**：Perlin 噪声用整数查表实现（禁用 MathF）。Voronoi hash 纯整数。

## 6. 交付物

| 文件 | 描述 |
|------|------|
| C#: 8 个新文件 | TerrainConfig, GlobalPos, ITerrainQuery, ITerrainGenerator, WorleyTerrainGenerator, WorldTerrain, ISpatialIndex, WorldEntities |
| Python: 5 个文件 | 渲染管线（仅读取，不生成） |
| 测试: C# 生成确定性 + Python 渲染确定性 |
| 示例: 128×128 Chunk 地图渲染输出 |
