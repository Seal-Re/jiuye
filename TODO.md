# TODO — jiuye 内核剩余工作

> 2026-07-14 会话收尾 | 1271 绿 | 内核编程收敛

## 高优先级（game design 阻塞）

- [ ] **路径级 SEC/SBC 数据铺设**：21 路招式闪避/格挡系数差异化
  - 设计模板: `design/balance/path-sec-sbc-design-template.md`
  - 铺设后重跑 `InvCrossDuelTests.C1Gate_FunnelOn_*` 验证 violations↓
  - 目标: [40,60]% 硬闸门 violations==0（TR-BAL-001 终 gate）

## 中优先级（工程改进）

- [ ] **cv-009 story-done**：`CombatExchangeResult` record 已落地（`e84ed47`），story 文件待闭合
- [ ] **cv-005 story-done**：harness+报告已就位，待 game design 数据后闭合
- [ ] **adr-0010 同步**：`MaxPermille`→`AutoHitPermille` annotate 已做，确认 adr-0010 正文无其他过期引用
- [ ] **off worktree sha256 正式核验**：B.3 红线要求，当前仅有 md5 一致性验证

## 低优先级（nice-to-have）

- [ ] **cultivation-a3 story-014/015**：不变量硬化 + 审计员终验（已实现代码的正式化验证）
- [ ] **combat-fullstruct story-008**：丹修非战斗机制（丹药改四维 + 经济晋升）
- [ ] **repo-tidy**：_research/raw3 清理

## 已完成（本会话）

- [x] cv-006 SEC 闪避合流 (adr-0010 Layer ①)
- [x] cv-007 派生抗性 R + 半衰减伤 (adr-0010 Layer ③)
- [x] cv-008 SBC 格挡调制 + 三层集成 (adr-0010 Layer ②)
- [x] cv-004 溢出检测+绝对秒杀+优先级链+防守帧契约
- [x] cv-005 校准 harness + 5参数扫面 + 报告 + 设计模板
- [x] cv-009 CombatExchangeResult record 重构
- [x] cultivation-a3 审计→Done
- [x] combat-fullstruct 审计→Done
- [x] integration 审计→Done
- [x] smoke check PASS
- [x] CLAUDE.md Section G 关键实现铁律
- [x] adr-0010 实现注
