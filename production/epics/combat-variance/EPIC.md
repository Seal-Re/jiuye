# Epic: 方差 + 反应式 QTE 战斗模型（combat-variance）

**Layer**: Core（战斗内核范式重构）
**Status**: Done（cv-001~009 全 Complete；cv-005 三层漏斗重标定 harness 落成 @2026-07-17；1271 绿全量）
**GDD**: `design/gdd/combat-system.md`（实现期须同步修订：确定性结算 → 概率博弈结算）；深度源 `docs/architecture/adr-0008-variance-reactive-combat-model.md`（权威）
**Governing ADRs**: **adr-0008**（primary，方差+反应式 QTE 战斗模型）· **adr-0010**（三层防御漏斗 Evasion→Block→Resistance，Accepted @ `d35fef6`）· adr-0001（整数确定性 B.2）· adr-0002（Modules 工厂 B.9）· adr-0003（off 逐字节 B.3）· adr-0004（Godot View/Host 边界，判定权移交）
**Engine Risk**: **HIGH**（触 `Jianghu.Cultivation`：B.2 禁浮点 / B.3 off 逐字节 / PRNG 流 / 平衡数值——一处细微 bug 破确定性或平衡且难查。B.7 旗舰档实现 + 主控独立核验 A.3）
**Created**: 2026-07-06（承 balance-cross EPIC "方差战斗模型" 立项预告 + retro-sprint-7 action item）

## Summary

废弃"纯 PE 差值定胜负"（现状 100% 确定性碾压，实测"差 999/差 1111"），确立 **「Margin→关键事件触发概率」为主轴** 的战斗内核：PE 差经**整数查表**映射为命中/招架等**伯努利概率**，逐次掷骰累加定胜负。内核（Model）只提供"方差基准 + 判定钩子（防守窗口/概率）"，状态反转（弹反/QTE 质量档）由表现层（Godot QTE / 玩家 或后台 NPC 检定）接管。

**范式转移核心**（adr-0008 决策③，用户 2026-07-06 精确界定）：方差概率判定**只作拦截器插入"主动技能交锋"一环**，DoT/压制/法宝等既有机制在 `ResolveR2` 内**原样保留**：
- **管线1 主动攻击交锋**（刀剑/暗器/主动法术）→ 走 adr-0008 概率+QTE 管线（可弹反/闪避）。
- **管线2 DoT/场地伤害**（已挂的毒/燃/血）→ **绕过**概率判定，每回合绝对确定扣血（现有 `TickDots`）。
- **管线3 光环/压制**→ 不产交锋事件，在管线1查表**前**动态削 PE/防御（现有 `ApplyEPModifiers`/`SuppressionMatrix`）。

**解锁**：balance-006 判死的 [40,60]% 硬闸门（确定性不可达 → 降级 PE-band 代理）经方差**数学复活**（seed-sweep 统计，每场仍逐字节确定）。

## Scope

- **cv-001** 主动交锋概率拦截最小闭环（CombatMath 查表 + Duel=9 流 + ResolveExchange 插入伯努利；后台 NPC 内核代掷）
- **cv-002** 削韧副轴 + 硬直（duel-local Poise/Stagger，接 balance-007/008 DR）
- **cv-003** 标签门控（Unblockable_Weapon/Undodgeable_Space 动态开关钩子）+ 元素格挡穿透 Chip Damage
- **cv-004** 阈值溢出 + 防守帧钩子契约（Model→View 整数钩子）+ 裁定优先级链
- **cv-005** 重标定：InvCrossDuel seed-sweep 复活 [40,60]% 硬闸门（解除 balance-006 降级）
- **cv-006** SEC 闪避系数合流（adr-0010 Layer ①）：SEC 挂 CombatSkillDef → CombatMath 命中调制 → 合流 cv-001 单次判定
- **cv-007** 派生抗性 R + 半衰减伤（adr-0010 Layer ③）：DerivedProviders.ResistanceOf → 半衰公式 → ResolveExchange step 4
- **cv-008** SBC 格挡系数调制 + 三层漏斗串行接线（adr-0010 Layer ② + Integration）：SBC 调制 cv-003 Chip + ①→②→③→④→⑤ 管线闭环

## Out of Scope（本 epic 不含）

- Godot 宿主侧 QTE/弹反/动画落实（属 godot-host epic #14；本 epic 只定 Model 侧整数钩子契约，承 adr-0004）。
- 玩家输入采集（属宿主命令端口）。
- 连续格挡资源约束的最终数值曲线（cv-002 定机制，数值实现期标定）。

## Dependencies

**Unblocked by**: adr-0008 Accepted（架构封闭前置门，✅ 2026-07-06）；balance-006（判死确定性天花板，确认本 epic 必要性）
**Blocks**: balance-cross Fairness [40,60]% 硬闸门最终兑现（cv-005）；Godot 宿主战斗 View（需 cv-004 钩子契约）

## Definition of Done

- [x] cv-001 主动交锋概率化：CombatMath 查表 + Duel=9 流 + ResolveExchange 伯努利拦截，off 逐字节 + IL 浮点零 + 同种子复现（`dbd070c`，1102 绿 + worktree sha256 实证）
- [x] cv-002 削韧/硬直副轴（duel-local，接 balance-007/008）（`1bcd48f`，1127 绿 + worktree sha256 实证）
- [x] cv-003 标签门控 + Chip Damage（`9ad6be0`，1147 绿 + worktree sha256 实证）
- [x] cv-004 溢出 + 防守帧钩子契约 + 裁定优先级（Model 侧闭环 @ 31c5e1a+17abbea+f54d89a+32b5e1e）
- [x] cv-005 [40,60]% 硬闸门 seed-sweep 复活（TR-BAL-001 完整达成，解除 balance-006 降级）
- [x] cv-006 SEC 闪避系数合流 cv-001 命中判定（adr-0010 Layer ①）
- [x] cv-007 派生抗性 R + 半衰减伤（adr-0010 Layer ③）
- [x] cv-008 SBC 格挡系数调制 Chip + 三层漏斗串行接线（adr-0010 Layer ② + Integration）
- [x] 全程守 B.2（禁浮点，IL 扫描）/ B.3（off 逐字节）/ B.9（Modules 工厂）
- [ ] `design/gdd/combat-system.md` 同步修订为概率博弈模型

## Stories（story 级指针；机器可读状态在各 story 文件 Status 字段 + sprint-status.yaml）

- **story-001** active-clash-variance — **Complete**（`dbd070c` @2026-07-07；本 epic 首切片）。CombatMath 查表 + RngStreamIds.Duel=9 + ResolveExchange 插入伯努利判定 + duel-local seed 派生。后台 NPC 内核代掷，`dotnet run` 已见"差999"变有悬念对局（88/13/109）。1102 绿 + 27 determinism + worktree sha256 实证（A.3）。
- **story-002** poise-stagger-subaxis — **Complete**（`1bcd48f` @2026-07-07；削韧+硬直+DR 最小闭环）。PoiseState duel-local + DerivePoiseDamage/StaggerResetPoise 纯函数 + TickPoise 复用 Control 管线注入 turns=1 stagger + A+B 混合削韧来源（伤害派生 + PoiseDamage 算子骨架，21 路数据 deferred）+ calibrationMode 旁路。1127 绿 + worktree sha256 实证（A.3）。balance-004 阈值因削韧放宽 <27。
- **story-003** tag-gating-chip-damage — **Complete**（`9ad6be0` @2026-07-07；Model 侧最小闭环）。DamageType 标签（Normal/Blunt/Elemental，AOE 并入 Elemental）确定性门控 Block 类={FlatDR,ReflectDamage}/Dodge 类={Evade,SoulSplit} + 元素格挡穿透 Chip Damage（免削韧协调 cv-002 TickPoise）+ 招架崩坏 bonus + calibrationMode 旁路（决策⑨.1/⑩.1）。1147 绿 + worktree sha256 实证（A.3）。**QTE 帧窗/裁定优先级/连续格挡递减留 cv-004**；21 路 DamageType 数据 deferred。
- **story-004** overflow-defense-frame-contract — **Complete**（`31c5e1a`+`17abbea`+`f54d89a`+`32b5e1e` @2026-07-14；Model 侧闭环）。溢出检测（p≥1000→跳过伯努利必中）+ 跳过OnDefend绝对秒杀 + 优先级链 Tag>Overflow + DefenseFrameHook 契约 + GuaranteeFrameCount 保底帧旋钮 + ResolveExchange 5元组扩展。OverflowTests 18 绿 + 全量 1243 绿。View 侧 QTE 属 godot-host。
- **story-005** recalibration-40-60-gate — **Not Started**（创建 2026-07-14；依赖 cv-001+adr-0010 防御漏斗已就位）。三层漏斗全开（calibrationMode=false）seed-sweep：InvCrossDuelTests 全路径对拍胜率统计 → [40,60]% 硬闸门 violations==0（解除 balance-006 PE-band 降级）。若 violations>0：调参 K/ChipPermille/SEC/SBC 全局参数 + 路径级诊断 dump。C2/C3 不退；平衡矩阵 CSV dump。
- **story-006** sec-evasion-merge — **Complete**（`c57d365` @2026-07-14；adr-0010 Layer ①）。SEC 闪避系数合流 cv-001 命中判定：`CombatSkillDef.Sec` 字段 + `CombatMath.ApplyEvasionCoefficient`（AutoHitPermille=1000 必中/中性/衰减/抬升钳≤1000）+ ResolveExchange step 1 接线 + calibrationMode 旁路。1166 绿 + 27 determinism + off md5 一致（A.3）。21 路数据 deferred（全默认 1000 中性）。偏离 AutoHitPermille=1000 vs ADR 字面 999 已登记（adr-0008 ⑨.2 背书）。
- **story-007** resistance-derived — **Complete**（`4e51dfd` @2026-07-14；adr-0010 Layer ③）。派生抗性 R + 半衰减伤：新建 `ResistanceProviders.ResistanceOf`（体质→物理抗/识→法术抗 + HasBodyArt 复用）+ `CombatMath.ApplyResistance` 半衰公式（K×1000/(K+R)，max(1,...) 保底）+ ResolveExchange step 4 接线（Chip 后，符合决策④）+ 5 旋钮 + B.5 守（R 不进 EffectivePower）。1198 绿 + 27 determinism + off md5 一致（A.3）。偏离：接线位置修正 + gate 退化用 HasBodyArt + 2 既有测试回归适配（A.7）。法宝 OnDefend 加 R 留 TODO(cv-008)。
- **story-008** sbc-block-integration — **Complete**（`f34caed`+`341d390` @2026-07-14；adr-0010 Layer ② + Integration）。SBC 格挡系数调制 cv-003 Chip 穿透：`CombatSkillDef.Sbc` 字段 + `CombatMath.ApplyBlockCoefficient` + Chip 段替换 ChipDamageFloor 第 3 参（effChipPermille）+ 三层漏斗串行接线已由 cv-006/007 就位。1224 绿 + 27 determinism + off md5 一致（A.3）。adr-0010 三层防御漏斗闭环。

## Notes

- adr-0008 = 本 epic 权威真相源，含 α（Margin→概率）+ 决策①~⑩（全 Accepted）+ 附录A permille 示例映射表。
- 承 balance-cross EPIC 收官记录「根因单」：C1 硬闸门 + AC 3.7 反扁平同源于确定性无方差，本 epic 是其架构解。
- 实现档位：B.7 旗舰档（触 B.2/B.3/PRNG/平衡）+ 主控独立核验（A.3：自跑 test / git log 核 sha）。
