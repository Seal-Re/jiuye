# Story 002: SituationalEdges 克制矩阵完整

> **Epic**: combat-fullstruct
> **Status**: Complete
> **Layer**: Core
> **Type**: Logic
> **Estimate**: 中 (1.5d)
> **Depends**: combat-r2 done
> **ADR**: adr-0002-module-factory-effect-system
> **GDD**: combat-system.md §克制矩阵

## Context

22 边克制矩阵完整实现：灭阴×3、克邪×2、元素相生克、经济压制、控场反制、环境优势。

**深度源**: combat-fullstruct EPIC §scope / SituationalEdges.cs。

## Acceptance Criteria

- [x] 2.1 灭阴三边（阳罡→阴/佛光→鬼/雷罡→魔）
- [x] 2.2 克邪二边（浩然→邪/浩然→魔）
- [x] 2.3 元素相生克（五行生克链）
- [x] 2.4 经济压制（经济差→战力修正）
- [x] 2.5 控场反制（控场方被反制衰减）
- [x] 2.6 环境优势（地形/天时匹配）
- [x] 2.7 22 边全入 SituationalEdges 数据表
- [x] 2.8 全量绿 + IL 浮点零 + off 逐字节

## Implementation Notes

- `SituationalEdges.cs` + `SituationalEdgesDataTests.cs`
- 所有边为整数修正（不破 B.2 确定性）

## Test Evidence

**Required**: `tests/Jianghu.Core.Tests/Cultivation/SituationalEdgesDataTests.cs`
**Commit**: `1093e6d`

## Out of Scope

- 动态情境生成（后续 FULLSTRUCT）
