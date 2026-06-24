# Story 025: A.2审计员终验

> **Epic**: cultivation-a2
> **Status**: Not Started
> **Last Updated**: 2026-06-24
> **Layer**: Core
> **Type**: Logic
> **Estimate**: 小 (0.5d)
> **Depends**: story-023, story-024
> **ADR**: adr-0001, adr-0002, adr-0003
> **GDD**: A123

## Context

A.2 全量实现完成后的最终审计——由独立审计 agent 逐条审查所有验收标准、不变量、红线合规。审计不包括代码修改——仅产出 PASS/CONCERNS/FAIL 报告。

## Acceptance Criteria

- [ ] 25.1 全部 25 个 story 的 AC 逐条核对——每个 [x] 都有对应测试证据
- [ ] 25.2 红线 B.5（道心解耦）实证——IL 扫描 + PowerFormula audit
- [ ] 25.3 红线 B.2（整数确定性）实证——确定性回归测试通过
- [ ] 25.4 红线 B.3（off 逐字节）实证——off 回归测试通过
- [ ] 25.5 测试证据完整性——每个 story 有对应测试文件 + 通过计数
- [ ] 25.6 代码覆盖审计——A.2 新增代码路径 ≥90% 被测试覆盖（核心路径 100%）
- [ ] 25.7 设计偏差审计——实现 vs 设计规范的一致性检查
- [ ] 25.8 Chronicle 事件完整性——A.2 相关事件全部入编年史
- [ ] 25.9 Production token 对账——epic/story/sprint-status 一致性
- [ ] 25.10 审计报告输出到 `production/epics/cultivation-a2/audit-report.md`

## Implementation Notes

**审计 checkout 清单**：
1. 全量测试绿 + 计数
2. 每个 story-*.md 的 AC checklist 全部 [x]
3. 每个 story 对应的测试文件存在
4. off 逐字节 sha256 验证
5. IL 浮点扫描
6. BannedApiAnalyzers 编译零误
7. 设计文档 vs 实现的差异列表

**审计 agent**：由独立 Opus agent 执行（delegate），主控不干预。

## Out of Scope

- 代码修改——审计仅产报告
- 性能分析（此阶段无性能要求）

## Test Evidence Requirement

**Type**: Logic — audit report (production/epics/cultivation-a2/audit-report.md). All prior story tests must be green.
