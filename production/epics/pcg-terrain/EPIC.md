# Epic: PCG 地形生成 (pcg-terrain)

**Layer**: View (Godot) → Core 快照
**Status**: Planned
**ADR**: adr-0006 (Accepted 2026-07-23 — 备选 B: View→Core 快照)
**Depends**: mv-004 (Core 地形字段), mv-002 (四层渲染引擎)
**Created**: 2026-07-23

## Summary

ADR-0006 裁决落地：Godot View 用 FastNoiseLite 生成 2D 地形 → 量化为整数网格 → 快照写入 Core NodeGeo/RegionDef → 运行时 Core/View 各取所需。

## 四步管线

```
Step 1 (View)  FastNoiseLite(seed) → 2D 浮点连续地形网格
Step 2 (View)  按 NodeId 坐标采样 → TerrainKind/Element/Peril/HazardKind 整数值
Step 3 (Bridge) WorldBridge.SetTerrainSnapshot() → Core NodeGeo/RegionDef 一次性写入
Step 4 (Runtime) Core 规则演化 + View 四层渲染 —— 同源数据，互不污染
```

## Stories

### Must Have

- **pcg-001** FastNoiseLite 2D 地形生成器 (1.5d)
  - Godot GDScript: `TerrainGenerator.gd`
  - 多 Octave 分形噪声（3-4 octaves, 0.5 persistence, 2.0 lacunarity）
  - 噪声输出: 高度图(heightmap) + 湿度图(moisture) + 温度图(temperature) + 危险度(peril)
  - 双线性采样: 世界坐标 → 噪声值 (连续过渡，无块状边界)
  - 确定性: 固定 seed + FastNoiseLite 同 seed = 同输出

- **pcg-002** 噪声→TerrainKind 量化映射 (0.5d)
  - Whittaker 生物群系模型（简化）:
    - 高温+低湿 → 荒漠(T03)
    - 高温+高湿 → 林莽(T05)
    - 低温+低湿 → 雪原(T09)
    - 低温+高湿 → 山峦·密林(T06)
    - 中温+中湿+低海拔 → 平原(T01)
    - 极高湿+低海拔 → 水泽(T07)
    - 极高海拔 → 山岳·火(T04)
    - 极高危险度 → 鬼域(T10)/火山(T08)
  - 输出: `Dictionary[Vector2i, {terrain_id, element, peril, hazard}]`

- **pcg-003** WorldBridge 快照写入接口 (1d)
  - C# `WorldBridge.SetTerrainSnapshot(Dictionary<int, TerrainSnapshot>)`
  - `TerrainSnapshot` record: `TerrainKind, ElementKind, Peril, HazardKind, QiLayer`
  - Core 侧: `World.ApplyTerrainSnapshot()` → 填充 `NodeGeo`/`RegionDef` 字段
  - 生成期一次性写入——存档存快照，不复算
  - 1281绿不退 + off 逐字节安全（默认值 fallback）

### Should Have

- **pcg-004** 多 seed 地形多样性验证 (0.5d)
  - 10 seeds × 地形可视化对比（确保不同 seed 产生明显不同的地形布局）
  - "坏种子" 检测（全平原/全荒漠 → 自动调整噪声参数）

## Out of Scope
- 运行时动态地形修改（地震/火山喷发改变地形）
- 地形影响天气系统
- 洞穴/地下层
