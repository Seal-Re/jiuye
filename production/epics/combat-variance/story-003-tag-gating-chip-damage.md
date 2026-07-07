# Story 003: 标签门控 + 元素格挡穿透（Tag Gating + Chip Damage）

> **Epic**: combat-variance
> **Status**: In Progress
> **Layer**: Core
> **Type**: Logic
> **TR**: TR-BAL-001（标签门控是"读招策略"维度——看攻击类型选防御姿态，与 cv-001 概率主轴/cv-002 削韧节奏正交叠加；[40,60]% 硬闸门断言仍在 cv-005）
> **Estimate**: 中 (2.0d)
> **Manifest Version**: 2026-07-03b（`docs/architecture/control-manifest.md`）
> **Last Updated**: 2026-07-07

## Context

**GDD**: `design/gdd/combat-system.md`（实现期同步修订）；深度源/权威 `docs/architecture/adr-0008-variance-reactive-combat-model.md`（决策⑨.1 标签门控 + ⑩.1 Chip Damage + ⑩.4 裁定优先级）
**Requirement**: `TR-BAL-001`（标签门控使战斗从"纯反应/数值"升级为"读招策略（石头剪刀布）+ 反应"——看敌方招式 DamageType 选正确防御姿态）

**ADR Governing Implementation**: **adr-0008**（primary，决策⑨.1/⑩.1/⑩.4）· adr-0001（整数确定性 B.2）· adr-0002（Modules 工厂 B.9）· adr-0003（off 逐字节 B.3）· adr-0004（Model/View 边界——帧窗钩子留 View）
**ADR Decision Summary**: 攻击带 `DamageType` 标签（Normal/Blunt/Elemental）；后台 NPC 推演时**确定性门控**防御模块：Blunt（Unblockable_Weapon）→ 禁 Block 类防御（招架崩坏：全额伤害）；Elemental（Undodgeable_Space）→ 禁 Dodge 类防御。Elemental 被 Block 成功时**免控制/硬直但承受 Chip 穿透伤害**（纯整数公式）。QTE 帧窗/玩家选姿态属 View（cv-004）。

**Engine**: .NET 8 / netstandard2.1 | **Risk**: **HIGH**（触 `Jianghu.Cultivation`：禁浮点 IL 扫描 / off 逐字节 / 改 ResolveExchange OnDefend 结算 / 与 cv-002 削韧耦合；一处细微 bug 破确定性或平衡且难查。B.7 旗舰档实现 + 主控独立核验 A.3）
**Engine Notes**: 无 post-cutoff API。新增 `enum DamageType`（纯数据）。`CombatSkillDef` record 末尾追加可选字段 `DamageType Damage = DamageType.Normal`（定位构造向后兼容，21 路零改动默认 Normal）。

**Control Manifest Rules (Core 层)**:
- Required: DamageType 离散整数枚举；门控 = ResolveExchange OnDefend 遍历内确定性 `continue`；Chip 纯整数向下取整；新旋钮外置 LimitsConfig（A.10）。
- Forbidden: 浮点（B.2，Chip 全整数）；裸 `new EffectOp` 七参（B.9——本 story 不新增 EffectOp 算子，DamageType 挂 CombatSkillDef 非算子）；`daoHeart/innerDemon` 进战力/门控（B.5）；**帧数/QTE/玩家输入进 Core**（adr-0004——那是 View/cv-004）。
- Guardrail: 改 DuelEngine 须过 off 逐字节（`Determinism/OffByteIdenticalTests` + worktree sha256）+ IL 浮点零（`ILFloatScanner`）+ cv-001 方差/cv-002 削韧/balance-007/008 不退 + C2/C3 不退。

---

## 背景（承 adr-0008 决策⑨.1/⑩.1 + 用户 2026-07-07 裁定 + 主控旗舰档勘探）

adr-0008 决策⑨.1 让"单纯弹反不再万能"：内核据攻击 `DamageType` 动态开关防御钩子，使博弈升级为"读招（石头剪刀布）+ 反应"。**关键 Model/View 分层洞察（主控勘探核实）**：
- **Model 侧（cv-003 本切片，纯确定性可交付，与 Godot 宿主解耦）**：DamageType 标签数据 + 后台 NPC vs NPC 的**确定性门控**（NPC 无 QTE，"关闭钩子"退化为"该类防御模块不结算"）+ Chip 穿透（纯整数）。
- **View 侧（明确留 cv-004/godot-host）**：Block/Dodge 的玩家 QTE **帧数窗口**（决策⑦ 非对称结算的"Player vs NPC = 帧数"路，adr-0004 Model→View）；玩家"选错防御姿态"的交互反馈。

本 story 落 cv-003 Model 侧最小闭环。

### ⚠️ 关键工程事实（2026-07-07 主控旗舰档勘探已核代码，钉死实现形态）

1. **DamageType 挂 `CombatSkillDef` 可选字段（非 SituationalTags/非 EffectOp）**：`SituationalTags`（路径级 flavor，`CultivationSchema.cs:127`）是"修士是什么流派"，装不下"这一招是钝击/元素"。DamageType 是**招式属性** → `CombatSkillDef`（`CultivationSchema.cs:24`）record 末尾追加 `DamageType Damage = DamageType.Normal`。21 路全用 5 参定位构造 → 追加带默认第 6 参 → **全部原样编译、默认 Normal（Block/Dodge 全开）**，21 路数据零改动（承 cv-002"机制骨架先行、数据 deferred"范式）。裸攻（skill==null）→ 默认 Normal。
2. **enum 三值（AOE 并入 Elemental，用户 2026-07-07 裁定）**：`enum DamageType { Normal, Blunt, Elemental }`。Blunt=Unblockable_Weapon（必不可招架）；Elemental=Undodgeable_Space（必不可闪避，含元素+范围）。
3. **Block/Dodge 分类从 EffectOpKind 派生（零数据改动）**，新建 `IsBlockClass`/`IsDodgeClass` 纯函数：
   - **Dodge 类** = `{ Evade, SoulSplit }`（用户 2026-07-07 裁定 SoulSplit 归 Dodge——分魂挡刀用身法秘术，门控同 Evade）。
   - **Block 类** = `{ AddFlatDR, ReflectDamage }`（+ 法宝盾 OnDefend）。
4. **门控 = ResolveExchange OnDefend 遍历内确定性拦截**（`DuelEngine.cs:406-420` 循环 + 法宝段 `393-404`）：Blunt 攻击 → 跳过（`continue`）Block 类 op；Elemental 攻击 → 跳过 Dodge 类 op。**不加新 GateType**（GateType 是"防方有无功法"的静态能力门，与"按攻击标签动态禁用"是不同轴，混用污染语义）。
5. **NPC 侧语义 = 确定性禁用防御模块**（用户勘探确认）：纯 NPC 对拍无 QTE/帧数/概率新维度。Blunt→Block 类失效（全额伤害）；Elemental→Dodge 类失效。"招架崩坏"（Blunt 强行 Block）在 NPC 侧退化为"Block 禁用 + 追加大额削韧"（复用 cv-002 PoiseBreakBonus 通道）。
6. **Chip Damage（决策⑩.1，纯 Model 核心）**：Elemental 攻击**被 Block 类成功减伤**时 → 免控制/硬直（保霸体）但承受穿透 = `max(dmg, 基础伤害×ChipPermille/1000 + Margin修正)`。落点 = ResolveExchange OnDefend 减伤后、return 前（`DuelEngine.cs:442-454` 邻域）。Margin=`attackerPe-defenderPe`（cv-001 已在作用域）。ChipPermille（默认 300）外置 LimitsConfig。
7. **头号接线点 — Chip 与 cv-002 削韧的免疫耦合**：⑩.1"免疫控制与硬直" = Chip 穿透伤害**不派生削韧**。但 cv-002 `TickPoise`（`DuelEngine.cs:220`）无条件用返回 dmg 派生削韧。**方案**：ResolveExchange 对 chip 交锋额外返回"免削韧"信号（扩返回元组或专用标记），`ResolveR2` 对该方向传 `poiseDamage=0` 旁路 TickPoise 基础派生。这是 cv-003↔cv-002 唯一实质耦合，实现须显式处理。
8. **calibrationMode 旁路（承 cv-001/002）**：标签门控禁用防御 + Chip 扰动"裸 PE 平价"，标定期须 `if(!calibrationMode)` 旁路，保 cv-005 seed-sweep 纯净。
9. **无 duel-local 状态**：DamageType 静态数据 + Chip 即时计算，无跨回合累积（不同 cv-002 PoiseState）。连续格挡防守帧递减（⑩.3）需 duel-local，属 cv-004。
10. **确定性 + off**：全整数确定性判定，无 RNG（不碰 duelRng）；off 走 legacy SparAction 不入 DuelEngine 天然守 B.3。

---

## Acceptance Criteria

- [ ] **3.1 DamageType enum + CombatSkillDef 字段**：`enum DamageType { Normal, Blunt, Elemental }`；`CombatSkillDef` 追加 `DamageType Damage = DamageType.Normal`（可选，向后兼容）。21 路现有构造零改动编译通过，默认 Normal。
- [ ] **3.2 Block/Dodge 分类纯函数**：`IsBlockClass(EffectOpKind)` = {AddFlatDR, ReflectDamage}；`IsDodgeClass(EffectOpKind)` = {Evade, SoulSplit}。纯函数单测直验（含非防御算子返 false）。
- [ ] **3.3 Blunt 门控**：Blunt 攻击 → ResolveExchange 跳过防方 Block 类 OnDefend 模块（含法宝盾）→ 全额伤害。测试：防方带 FlatDR，Blunt vs Normal 对比伤害差。
- [ ] **3.4 Elemental 门控**：Elemental 攻击 → 跳过防方 Dodge 类模块（Evade/SoulSplit）→ 闪避失效。测试：防方带 Evade，Elemental vs Normal 对比。
- [ ] **3.5 Chip Damage 穿透**：Elemental 被 Block 类成功减伤 → `dmg = max(dmg, 基础×ChipPermille/1000 + MarginAdj)`（纯整数向下取整）。测试：防方高 FlatDR 挡 Elemental → 仍受 chip 保底（非零）。
- [ ] **3.6 Chip 免削韧（cv-002 耦合）**：Chip 穿透交锋 → 不派生削韧（免硬直，保霸体，⑩.1）。测试：Elemental 被挡触发 chip → 该方向 poise 不减（对比 Normal 同伤害会削韧）。
- [ ] **3.7 招架崩坏（NPC 退化）**：Blunt 命中仅有 Block 类防御的防方 → Block 禁用 + 追加削韧（复用 PoiseBreakBonus）。测试：Blunt vs 纯 FlatDR 防方 → 削韧 bonus 生效。
- [ ] **3.8 LimitsConfig 旋钮**：`ChipDamagePermille`(默认 300)+ Chip Margin 修正系数（int + init + 默认，A.10）。Validate `<0`/范围断言。默认安全/负值抛/零值合法（退化无 chip）。
- [ ] **3.9 calibrationMode 旁路**：标定模式旁路标签门控 + Chip（保裸 PE 平价，同 cv-001/002）。测试：标定下 Blunt/Elemental 无门控效果。
- [ ] **3.10 B.2 + 确定性 + off + 不退**：IL 浮点零；同种子逐字节；off worktree sha256 IDENTICAL；cv-001/cv-002/balance-007/008 全绿不退；C2 auto-win 保留。

---

## Implementation Notes

*承 adr-0008 决策⑨.1/⑩.1 + 主控勘探地图（2026-07-07）：*

- **DamageType 落点**：新建 `src/Jianghu.Core/Cultivation/DamageType.cs`（或并入 EffectOp.cs）。`CombatSkillDef`（`CultivationSchema.cs:24`）追加 `DamageType Damage = DamageType.Normal`。
- **分类纯函数落点**：`DuelEngine` 加 `public static bool IsBlockClass(EffectOpKind)` / `IsDodgeClass(EffectOpKind)`（单测直验）。
- **门控落点**：`ResolveExchange` OnDefend 遍历（`DuelEngine.cs:406-420`）+ 法宝 OnDefend 段（`393-404`）：读 `skill?.Damage`，`Blunt && IsBlockClass(op.Kind)` → continue；`Elemental && IsDodgeClass(op.Kind)` → continue。记 `bool blockFired`（Block 类实际减伤）供 chip 判定。
- **Chip 落点**：OnDefend 减伤 + 软情境后、return 前（`DuelEngine.cs:442-454`）：`if (Elemental && blockFired) dmg = Max(dmg, basePart×ChipPermille/1000 + marginAdj)`。在 ×Scale 空间算，统一 `/Scale` 于 return。
- **Chip 免削韧信号**：`ResolveExchange` 返回元组扩为四元（加 `bool ChipImmuneToPoise` 或复用语义），`ResolveR2` 对 chip 方向 TickPoise 基础派生传 0。
- **旋钮**：`LimitsConfig` 加 `ChipDamagePermille`(300) + `ChipMarginDivisor`(margin 修正系数) + Validate 断言（承 cv-002 范式）。
- **测试落点**：新建 `tests/Jianghu.Core.Tests/Cultivation/TagGatingChipTests.cs`。照 `DuelGateTests.cs` 富 fixture（MinPath + ArtCategoryDef role=movement/body 使 CheckGate 通过 + 挂 Evade/FlatDR OnDefend 模块）；MakeChar RealmIndex=1 避 auto-win。
- **不碰**：QTE 帧窗/玩家输入（cv-004/godot-host）；连续格挡防守帧递减 ⑩.3（cv-004）；SuppressionMatrix（路径 tag 压制，正交）；cv-001 概率主轴/cv-002 PoiseState 内部逻辑（只在 TickPoise 入参处协调）；off 路径。

---

## Out of Scope（下切片 / 显式 deferred）

- **Block/Dodge 玩家 QTE 帧数窗口钩子**（决策⑦ 非对称结算 Player-vs-NPC 帧数路 + ⑧A 保底帧 + ⑨.2 溢出吃保底帧 + ⑩.4 裁定优先级完整链）→ **cv-004**（Model 侧整数钩子契约）+ godot-host（View 落实）。
- **连续格挡防守帧递减（⑩.3 BlockFrameDrStep）** → cv-004（需 duel-local 计数）。
- **21 路 DamageType 数据铺设（deferred，红线 A.8）**：cv-003 建 DamageType 机制骨架 + 合成测试路；21 路各招式的 Blunt/Elemental 归类 = 数值实现期铺（依赖世界观招式设定 + cv-005 平衡）。**不静默移走，显式登记。**
- 玩家"选错防御姿态 → 招架崩坏"的交互反馈（View 语义）。
- 难度溢出 >1000‰ 绝对压制（⑨.2）→ cv-004。

---

## QA Test Cases

*Logic 自动化测试 spec。纯确定性门控 + Chip，无 RNG，无需 mock。*

- **AC-1（3.1 enum+字段）**：DamageType 三值存在；CombatSkillDef 默认 Normal；21 路构造编译通过（build 绿即证）。
- **AC-2（3.2 分类）**：IsBlockClass/IsDodgeClass 各值正确；非防御算子（Dot/Control/PenFromResource）两者皆 false。
- **AC-3（3.3 Blunt）**：防方 FlatDR，Blunt 攻击伤害 > Normal 攻击（Block 被禁）。
- **AC-4（3.4 Elemental）**：防方 Evade，Elemental 攻击伤害 > Normal（Dodge 被禁）。
- **AC-5（3.5 Chip）**：防方高 FlatDR 挡 Elemental → 受击 ≥ chip 保底（非零穿透）；Normal 同配置可被挡到更低。Edge：Margin 修正整数向下取整。
- **AC-6（3.6 免削韧）**：Elemental 触发 chip → 该方向韧性不减（对比 Normal 同伤害削韧）。
- **AC-7（3.7 招架崩坏）**：Blunt vs 纯 Block 防方 → 削韧 bonus 追加。
- **AC-8（3.8 旋钮）**：默认安全/负值抛/零值合法（ChipPermille=0 退化无 chip）。
- **AC-9（3.9 calibration）**：标定模式 Blunt/Elemental 无门控效果（等同 Normal）。
- **AC-10（3.10 B.2/B.3）**：IL 浮点零；同种子逐字节；off OffByteIdentical + worktree sha256；cv-001/002 + balance-007/008 全绿。
- 测试文件：`tests/Jianghu.Core.Tests/Cultivation/TagGatingChipTests.cs` + `Determinism/OffByteIdenticalTests.cs`（回归守）。

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/Jianghu.Core.Tests/Cultivation/TagGatingChipTests.cs` — 须存在且过 + off 逐字节回归守（`Determinism/` + worktree sha256）+ IL 浮点零 + cv-001/002/balance-007/008 不退
**Status**: [ ] 待实现（/dev-story）

---

## Dependencies

- Depends on: **adr-0008 Accepted**（✅）；**cv-001 Done**（`dbd070c`）；**cv-002 Done**（`1bcd48f`，Chip 免削韧需协调 TickPoise）
- Unlocks: cv-004（防守帧钩子契约——标签门控的 View 侧帧窗 + 裁定优先级链 + 连续格挡递减）· cv-005（标签门控进 seed-sweep 胜率模型）
- Deferred dependency: 21 路 DamageType 数据铺设（数值实现期）
- Blocks: cv-004 的裁定优先级链（标签门控是优先级链第 1 层，决策⑩.4）
