# QA Plan: Sprint 4 — cultivation-a2 启动
**日期**: 2026-06-24
**生成者**: /qa-plan
**范围**: 7 个故事（5 个必须完成 + 2 个应该完成，跨 2 个史诗），跳过 2 个锦上添花
**引擎**: .NET 8 headless（纯整数确定性模拟）
**冲刺文件**: `production/sprints/sprint-4.md`

---

## 测试摘要

| 故事 | 类型 | 需要自动化测试 | 需要手动验证 |
|------|------|------------------------|------------------------------|
| a2-001 道心资源表 | Logic | 单元测试 — `tests/.../DaoHeartTableTests.cs` | 无 |
| a2-002 道心/心魔资源 | Logic | 单元测试 — `tests/.../DaoHeartResourceTests.cs` | 无 |
| a2-003 佛修破戒 | Logic | 单元测试 — `tests/.../BuddhistVowTests.cs` | 无 |
| a2-004 DailyMode 枚举 | Logic | 单元测试 — `tests/.../DailyModeTests.cs` | 无 |
| a2-005 贪心+迟滞 | Logic | 单元测试 — `tests/.../DailyModeGreedyTests.cs` | 无 |
| a2-006 INV-VARIETY | Logic | 单元测试 — `tests/.../VarietyTrackerTests.cs` | 无 |
| a2-009 闭关 DES | Integration | 集成测试 — `tests/.../SeclusionDESTests.cs` | 冒烟检查 |
| a2-019 Phase 集成 | Integration | 集成测试 — `tests/.../DailyModePhaseIntegrationTests.cs` | 冒烟检查 |
| drama-001 关系调整 | Logic | 单元测试 — `tests/.../RelationAdjustTests.cs` | 无 |

---

## 所需自动化测试

### a2-001 — 21 路道心资源表（Logic）
**测试文件路径**: `tests/Jianghu.Core.Tests/Cultivation/DaoHeartTableTests.cs`
**测试内容**：
- 全部 21 条路径 daoHeart_init > 0（每路独立测试）
- 每条路径 ≥3 个 daoHeart 增益来源，权重非零
- 每条路径 ≥3 个 innerDemon 来源，权重非零
- `DaoHeartRegistry` 加载全部 21 条路径，无缺失
- 辅助路径（dan_xiu、array_formation、qixiu_artificer）的乘子与战斗路径相当
- 佛修破戒规则：vow 标记存在于 SituationalTags

**边界情况**：
- 零路径注册表 → 空集合（不崩溃）
- 重复 PathId → 注册表抛错或覆盖
- 数据驱动：新增第 22 条路径 → 自动入池

**预估测试数量**: ~25 项单元测试（21 路 standalone + 注册表 + 边界）

---

### a2-002 — 道心/心魔伪资源系统（Logic）
**测试文件路径**: `tests/Jianghu.Core.Tests/Cultivation/DaoHeartResourceTests.cs`
**测试内容**：
- `GainDaoHeart(delta)` 正增量 → daoHeart 正确增加
- `GainDaoHeart(delta)` → 钳位 [0,100]（输入 150 → 钳至 100）
- `GainInnerDemon(delta)` → 钳位 [0,100]
- 负增量 → daoHeart/innerDemon 不低于 0
- 零增量 → 无变化
- Chronicle 事件：`DaoHeartChanged` / `InnerDemonChanged` 写入，包含 old/new/source
- 事件包含正确的 `pathId`、`characterId`
- Clone 深拷：克隆后修改原对象不影响克隆体
- off 模式：daoHeart=innerDemon=0 恒成立
- R3 解耦：`PowerEngine.Evaluate` 遍历所有 term，无 daoHeart/innerDemon（IL 扫描 + 术语审计）

**边界情况**：
- delta=0（无操作）
- delta=200 → 钳至 100（上限）
- delta=-200 → 钳至 0（下限）
- 并发操作：同 tick 内多次增益 → 累积正确

**预估测试数量**: ~12 项单元测试

---

### a2-003 — 佛修破戒修正（Logic）
**测试文件路径**: `tests/Jianghu.Core.Tests/Cultivation/BuddhistVowTests.cs`
**测试内容**：
- 破戒触发：daoHeart = max(current/2, 1)（折半非归零）
- 破戒时 innerDemon+（按 A3 §3.1 定义的增量）
- `innerDemon >= 95` → 触发 Fallen（仅 lethal 触发堕落）
- innerDemon < 95 → 不触发 Fallen
- 佛修 NPC 50 代模拟：堕落率 < 10%

**边界情况**：
- daoHeart=0 破戒 → 保持 0（min clamp 1）
- daoHeart=1 破戒 → 保持 1
- 佛修路径不加载 → 破戒逻辑不激活
- off 模式 → 无影响

**预估测试数量**: ~6 项单元测试

---

### a2-004 — 4 路 DailyMode 枚举 + 整数倍率表（Logic）
**测试文件路径**: `tests/Jianghu.Core.Tests/Cultivation/DailyModeTests.cs`
**测试内容**：
- 4 种枚举值互异（Fast=0, Steady=1, Comprehend=2, Roam=3）
- 整数倍率表精度（每模式每输出值验证）：

| 模式 | progress Δ | innerDemon Δ | daoHeart Δ | 副作用 |
|------|:---------:|:----------:|:--------:|------|
| Fast | +6/4×base | +2 | — | — |
| Steady | +3/4×base | -1 | — | Foundation+1 |
| Comprehend | ×1/2 | — | 见 Epiphany | EpiphanyRoll |
| Roam | ×1/4 | -2 | — | Move, encounter×3 |

- `Comprehend.EpiphanyRoll`：Insight=20 → 10% 触发；Insight=25 → 35% 触发
- 顿悟概率分布（1000 次试验，每 Insight 级别）——与阈值表匹配
- `DailyModeResult` 结构完整性（所有字段非默认值）
- off 模式 → dailyMode flag 不写入

**边界情况**：
- Insight < 18 → 顿悟概率 = 0%（阈值数学证明）
- Progress=0 → 所有模式 yield 0（零基）
- Progress=100 → Fast 模式不溢出

**预估测试数量**: ~14 项单元测试

---

### a2-005 — DailyMode 贪心算法 + 迟滞规则（Logic）
**测试文件路径**: `tests/Jianghu.Core.Tests/Cultivation/DailyModeGreedyTests.cs`
**测试内容**：
- 贪心评分公式——每项正确加权
- `innerDemon >= 65` → 进入 DANGER 状态 → innerDemonWeight ×3
- `innerDemon <= 50` → 退出 DANGER 状态 → 恢复正常权重
- 迟滞带宽：进入 65，退出 50（15 点带宽）——不频繁抖动
- Breakthrough 阶段 → 锁定 Fast（不可换模式）
- Deviation 阶段 → 强制 Roam 或 Steady
- Fallen 阶段 → 无 DailyMode（角色已废）
- 确定性：相同状态 + 相同种子 → 相同模式选择（5 次运行）
- off 模式 → 无操作

**边界情况**：
- innerDemon=64 → 正常状态（阈值以下 1 点）
- innerDemon=65 → DANGER 状态（恰在阈值）
- innerDemon=51 → 仍处于 DANGER（高于退出值 1 点）
- innerDemon=50 → 退出 DANGER（恰在退出值）
- 同时满足多个条件 → 优先级：Fallen > Deviation > Breakthrough > DANGER > Normal

**预估测试数量**: ~14 项单元测试

---

### a2-006 — 破单调 INV-VARIETY 真判据（Logic）
**测试文件路径**: `tests/Jianghu.Core.Tests/Cultivation/VarietyTrackerTests.cs`
**测试内容**：
- INV-VARIETY：K=10 tick 窗口内，DailyMode 种类 ≥ 2（满意度证明）
- INV-NO-DOMINANT：50 tick 窗口内，单一模式 ≤ 80%
- VarietyTracker 循环缓冲区正确记录
- 违反不变量 → Chronicle Warning 事件（不抛异常）
- 确定性：VarietyTracker 深拷正确
- off 模式 → 不追踪

**边界情况**：
- 仅 1 种模式连续 50 tick → INV-NO-DOMINANT 违反
- 空缓冲区 → 无违反
- 窗口滚动时正确逐出旧条目

**预估测试数量**: ~8 项单元测试

---

### a2-009 — 闭关 DES 单点唤醒（Integration）
**测试文件路径**: `tests/Jianghu.Core.Tests/Cultivation/SeclusionDESTests.cs`
**测试内容**：
- 进入闭关：NextActAt = Now + Duration，Flags["secluded"]=true
- 闭关期间：Scheduler.PopMin() 不弹出该角色
- 出关 Wake：`Seclusion.Exit()` 触发——计算收益、补 Age、生成 Chronicle
- Disturb 计数器在闭关期间递增（其他角色互动时）
- Disturb < 3 → 继续闭关
- 闭关期间 spar 动作 no-op
- 闭关锁定 Breakthrough Phase
- 确定性：相同 WorkUnits + 相同种子 → 相同 Duration
- off 模式 → 无激活

**边界情况**：
- WorkUnits=0 → 最短 Duration
- 不同 WorkUnits → 不同 Duration（单调递增）
- 10 个 NPC + 2 个闭关 → 运行 200 tick → 闭关角色仅在 Wake tick 出现

**预估测试数量**: ~12 项集成测试

---

### a2-019 — DailyMode→Phase FSM 集成（Integration）
**测试文件路径**: `tests/Jianghu.Core.Tests/Cultivation/DailyModePhaseIntegrationTests.cs`
**测试内容**：
- DailyMode.Apply 结果接入 World.Tick → AdvanceCultivation 流程
- Fast 模式 progress 累积 → 触发 RealmCurve 进度检查
- Comprehend 顿悟 → breakProgress+25 → 可能直接通过瓶颈
- Steady 模式 Foundation+1
- Roam 模式 → Move 触发 + encounter 曝光
- Breakthrough Phase → DailyMode 锁定 Fast
- Deviation Phase → 强制 Steady 或 Roam
- Fallen → DailyMode 停用
- 确定性：相同种子 + 相同 DailyMode 序列 → 相同 Phase 转移
- off 模式 → DailyMode 不执行，Phase 走 A.0 路径

**边界情况**：
- DailyMode 在 Phase 转移边界的行为
- Phase=Fallen 后 DailyMode 完全停用
- off 模式遗留路径——不崩溃，输出与 v1.0 一致

**预估测试数量**: ~14 项集成测试

---

### drama-001 — 关系调整（Logic）
**测试文件路径**: `tests/Jianghu.Core.Tests/Drama/RelationAdjustTests.cs`
**测试内容**：
- 关系增量应用（正/负）
- Chronicle 事件生成
- 确定性：相同输入 → 相同关系增量
- off 模式 → 无影响

**预估测试数量**: ~6 项单元测试

---

## 手动 QA 检查清单

### a2-009 — 闭关 DES（冒烟检查）
- [ ] 运行 `dotnet run --project src/Jianghu.Cli -- 42 100 --cultivation`——验证闭关事件出现在 Chronicle 中
- [ ] 检查闭关角色在出关前不产生 spar 事件
- [ ] 检查出关时一次性 Age 增量正确

### a2-019 — DailyMode→Phase 集成（冒烟检查）
- [ ] 运行 CLI 100 tick —— 验证 DailyMode 相关事件出现在 Chronicle 中
- [ ] 检查 Phase 转移日志（CultivationPhaseChanged）与 DailyMode 一致
- [ ] 检查 off 模式不产生 DailyMode 事件

---

## 冒烟测试范围

此冲刺在 QA 交接前的关键路径：

1. `dotnet build` — 零错误编译
2. `dotnet test` — 全量测试绿（≥562 + 新增 ~110）
3. off 逐字节 — `dotnet test --filter "FullyQualifiedName~Off"` 全部通过
4. IL 浮点 — `dotnet test --filter "FullyQualifiedName~FloatScan"` 零违规
5. CLI 确定性 — `dotnet run -- 42 100 --cultivation` 输出与已知参考一致
6. CLI off 确定性 — `dotnet run -- 42 100` 输出与 v1.0 逐字节一致

---

## 游戏测试需求

此冲刺无需游戏测试环节。所有故事均为纯逻辑/集成类型，无视觉/手感/UI 组件。

---

## 本冲刺的完成定义

故事完成需满足以下**全部**条件：

- [ ] 所有验收标准已验证——通过自动化测试结果或文档化的人工证明
- [ ] Logic 和 Integration 故事的测试文件存在于指定路径
- [ ] 冒烟检查通过（在 QA 交接前运行 `/smoke-check sprint`）
- [ ] 未引入回归
- [ ] 代码已审查（通过 `/code-review`）
- [ ] 故事文件已更新为 `Status: Complete`（通过 `/story-done`）
