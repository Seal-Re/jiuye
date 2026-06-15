# Epic: 可视化

**Layer**: Presentation
**Status**: Spike only
**GDD**: design/gdd/xxx.md（P8 补）或 — ；深度源 pixel/PIXEL_RULES.md
**Governing ADRs**: None yet（P8 增量补）
**Engine Risk**: LOW（.NET 8 纯整数）
**Created**: 2026-06-15（迁自 TASKS.md）

## Summary
可视化——像素（游戏世界 tile/角色/物品）+ 古风 UI（界面）。

## Scope
- 像素 tile / 角色模块 / 地图 viewer
- 古风 UI 轨（SVG/HTML-CSS 水墨，非像素）

## Dependencies
**Unblocked by**: —
**Blocks**: 无

## Definition of Done
- [ ] 像素 tile / 角色 / 物品渲染管线
- [ ] 古风 UI 轨（SVG/HTML-CSS 水墨）渲染
- [ ] 可视化管线 + 渲染

## Notes
仅 spike + 规则 doc；红线 B.8 分轨（游戏世界 = 像素 Pillow，UI = 古风非像素）。
