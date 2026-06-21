# Story 003: 辅助路 UT 锚锁终定

> **Epic**: combat-fullstruct
> **Status**: Complete
> **Layer**: Core
> **Type**: Logic
> **Estimate**: 小 (0.5d)
> **Depends**: fullstruct-001 (derived 真求和)
> **ADR**: adr-0003-cultivation-off-byte-identical
> **GDD**: combat-system.md §辅助路力量带

## Context

辅助路（丹/阵/器/符）UT 力量带锚锁，确保非战斗路径在 duel 中有合理战力范围。锚定值：Dan≤7、Array≤7、Qixiu≤10、Fu=12（战斗路参考）。

**深度源**: 丹修/阵修/器修/符修 path 文件 + Modules.cs。

## Acceptance Criteria

- [x] 3.1 Dan UT ≤ 7（丹道不以战力胜）
- [x] 3.2 Array UT ≤ 7（阵法为辅助）
- [x] 3.3 Qixiu UT ≤ 10（器修制造优势，战力上限受限）
- [x] 3.4 Fu UT = 12（符修战斗路，等同战斗路径力量带）
- [x] 3.5 锚锁不破其他路径 UT 带
- [x] 3.6 全量绿 + IL 浮点零 + off 逐字节

## Implementation Notes

- 在 `DanXiuPath.cs` 等文件中锚定 UT 值
- 不涉及新算子，仅调校现有 Modules 参数

## Test Evidence

**Required**: `tests/Jianghu.Core.Tests/Cultivation/paths/DanXiuTests.cs` 等
**Commit**: `5f4aeb7`

## Out of Scope

- 经济晋升系统（combat-r2 story-005）
