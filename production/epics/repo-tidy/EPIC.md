# Epic: 仓库整理

**Layer**: chore
**Status**: Complete（2026-07-20 — 三项全部就绪：raw3 gitignored/icon_gen 已修复/pixel 已提交）
**GDD**: —
**Governing ADRs**: None yet（P8 增量补）
**Engine Risk**: LOW（.NET 8 纯整数）
**Created**: 2026-06-15（迁自 TASKS.md）

## Summary
仓库整理。

## Scope
- 清 _research/raw3（已 gitignore）
- pixel icon_gen.py disk bug 修复 + 验证
- pixel 提交决断
- .gitignore 补全（已 P1 部分完成）

## Dependencies
**Unblocked by**: —
**Blocks**: 无

## Definition of Done
- [x] _research/raw3 清理 — gitignored（`3e3a5b1`），本地 477KB 残留（非阻塞，权限受限待手动清理）
- [x] pixel icon_gen.py disk() KeyError 修复（KNOWN-ISSUE）— 已修复并提交（`3e3a5b1`，ramp() 第 27-33 行处理 raw RGB tuple）
- [x] pixel 提交决断 — 已提交（`3e3a5b1`: "pixel pipeline committed as tooling"）

## Notes
KNOWN-ISSUE: pixel icon_gen.py v2 disk() 收原始 RGB 元组致 KeyError（修复已写未落，fix 第 1 次）。
