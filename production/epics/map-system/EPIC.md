# Epic: 地图系统 C

**Layer**: Feature
**Status**: Wired
**GDD**: design/gdd/xxx.md（P8 补）或 — ；深度源 docs/legacy-specs/specs/2026-06-13-v1.2-C-江湖地图与门派系统-design.md
**Governing ADRs**: None yet（P8 增量补）
**Engine Risk**: LOW（.NET 8 纯整数）
**Created**: 2026-06-15（迁自 TASKS.md）

## Summary
地图系统 C——江湖地图。

## Scope
- 地图生成
- 无缝懒加载

## Dependencies
**Unblocked by**: —
**Blocks**: faction（门派依赖地图）

## Definition of Done
- [ ] 地图生成实现 + 测试
- [ ] 无缝懒加载实现 + 测试

## Notes
已接线（圆桌订正 2026-06-26）：WorldMap/Kruskal/WorldMapFactory 接 WorldFactory + World.Advance/Clone，`--map` 激活（story-008 `a05cd8d`）。与 index.md #8=Wired 一致。无缝懒加载（DoD 第 2 项）待后续 map story。
