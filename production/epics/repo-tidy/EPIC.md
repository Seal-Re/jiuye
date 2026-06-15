# Epic: 仓库整理

**Layer**: chore
**Status**: Todo
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
- [ ] _research/raw3 清理
- [ ] pixel icon_gen.py disk() KeyError 修复（KNOWN-ISSUE）
- [ ] pixel 提交决断

## Notes
KNOWN-ISSUE: pixel icon_gen.py v2 disk() 收原始 RGB 元组致 KeyError（修复已写未落，fix 第 1 次）。
