# Story 004: 阈值溢出 + 防守帧钩子契约 + 裁定优先级链

> **Epic**: combat-variance
> **Status**: Complete
> **Layer**: Core
> **Type**: Logic
> **TR**: TR-BAL-001（溢出机制为极端战力差提供数学确定性截断，防守帧钩子为 View 层 QTE 提供整数契约）
> **Estimate**: 中大 (2.5d)
> **Governing ADR**: **adr-0008**（primary，决策⑧A 防守保底帧 + ⑨.2 阈值溢出 >1000‰ + ⑩.2 溢出吃保底帧 + ⑩.4 裁定优先级链）+ adr-0001（B.2 整数确定性）+ adr-0004（Model/View 边界——防守帧钩子属 Model 侧整数契约，View 落实属 godot-host）
> **Manifest Version**: 2026-07-03b（`docs/architecture/control-manifest.md`）
> **Last Updated**: 2026-07-14

## Context

**GDD**: `design/gdd/combat-system.md`（实现期同步修订 §溢出与防守帧）；primary design authority = `docs/architecture/adr-0008-variance-reactive-combat-model.md`（决策⑧A/⑨.2/⑩.2/⑩.4）

**ADR Decision Summary**:

**⑨.2 阈值溢出**：permille 体系下难度**允许突破 1000‰ 硬上限**形成绝对数值压制。NPC 后台推演：`duelRng.Next(1000)` 最大 999 → 难度 ≥1000‰ 时数学上绝不可能防御 → 实现高阶对低阶**绝对秒杀**。极大 UT gap（≥3 大境界）：仅 NPC-vs-NPC 后台跳查表直接 Auto-Win（省 CPU）；Player-vs-NPC 不截断，交 ⑨.2 溢出机制吃保底帧。

**⑧A 防守保底帧**：即便战力差达 `int.MaxValue`、触发映射表最极端桶（p 达/超 1000‰），**只要攻击方技能未携带 `Unblockable`（必中）标签，引擎必须向表现层输出 >0 的保底判定帧**（如 2 帧 / ≈33.3ms）——**捍卫"理论可操作性"**（无必中 = 理论可无限格挡）。保底帧数外置为整数 knob。

**⑩.2 溢出吃保底帧**：极大 UT gap（≥3 大境界）性能截断仅 NPC-vs-NPC；Player-vs-NPC 不截断，交 ⑨.2 溢出机制吃保底帧（按键碎裂定身）。

**⑩.4 裁定优先级链**：标签 > 溢出 > 保底。即：
1. 标签门控（Unblockable/Undodgeable，cv-003）最高优先——若攻击带必中标签，跳过溢出与保底直接命中
2. 溢出（p ≥ 1000‰）次之——若攻击难度溢出，NPC 侧数学必败
3. 保底帧最低——仅在无标签门控且无溢出时生效（给弱者理论操作空间）

**Engine**: .NET 8 / netstandard2.1 | **Risk**: **HIGH**（触 ResolveExchange 判定管线 + Model/View 钩子契约设计；旗舰档实现 + 主控核验 A.3）
**Engine Notes**: Model 侧纯整数——溢出判定（p ≥ 1000 → `overflowFlag`）、优先级链（枚举 + 判定顺序）、保底帧数（整数 knob 外置 LimitsConfig）。View 侧（Godot QTE 帧窗）属 godot-host epic #14，本 story 仅定义整数钩子契约（不实现 View）。

---

## Acceptance Criteria

### A. 溢出检测（Model 侧纯逻辑）
- [x] **4.1 溢出判定**：在 `ResolveExchange` cv-001 伯努利判定前/后，检测 `p_permille >= 1000`（含 SEC 调制后）。若溢出 → 设 `overflowFlag = true` + 记录 `overflowMargin`（p - 1000，溢出程度）。
- [ ] **4.2 NPC vs NPC 溢出 Auto-Win**：`overflowFlag && 双方均非 Player` → 跳过伯努利掷骰，直接返回 (fullDamage, 0, fullPoiseBreak, false)——数学必败，零反伤/零削韧豁免。
- [ ] **4.3 LimitsConfig 旋钮**：`OverflowThresholdPermille`（默认 1000，≥1；1000=标准溢出阈值）——外置 knob，方便调"几 permille 算溢出"。

### B. 裁定优先级链（Model 侧判定顺序）
- [ ] **4.4 优先级枚举**：`enum VerdictPriority { Tag, Overflow, GuaranteeFrame }`——标签 > 溢出 > 保底。
- [ ] **4.5 优先级链判定**：`ResolveExchange` 防御端判定按优先级链执行：
  1. **Tag（cv-003）**：若攻击带 `Unblockable`（Blunt）或 `Undodgeable`（Elemental 关 Dodge），标签判定**最高优先**——直接跳过溢出与保底，按标签门控结算
  2. **Overflow（本 story）**：若无标签门控但 `overflowFlag` → NPC-vs-NPC 直接 Auto-Win
  3. **GuaranteeFrame（本 story）**：若无标签门控且无溢出 → 输出保底帧数给 View（Player 侧）或确定性结算（NPC 侧）
- [ ] **4.6 C2（UT gap≥2 auto-win）与溢出关系**：既有 C2 `UT gap≥2 → margin=int.MaxValue` auto-win 保持不动。溢出是**概率维度**的"绝对压制"（p≥1000），C2 是**战力维度**的"绝对碾压"（PE 差无穷大）。两者**正交**——溢出在概率管线判定，C2 在 PE 比较判定，不冲突。

### C. 防守帧钩子契约（Model→View 整数接口）
- [ ] **4.7 防守帧钩子数据结构**：定义 `DefenseFrameHook` record——纯整数，不含 `Godot.*`/`UnityEngine.*`（ADR-0004 守）。字段：
  - `FrameWindow`（int）：防守判定帧数（保底帧数或计算帧数，≥1）
  - `Overflowed`（bool）：本回合是否触发溢出
  - `AttackDamageType`（DamageType）：攻击类型（View 据此选 QTE 类型：Block/Dodge）
  - `RawDamage`（int）：原始伤害（溢出时可为 int.MaxValue 标记）
- [ ] **4.8 保底帧数 knob**：`LimitsConfig.GuaranteeFrameCount`（默认 2，≥1；0=退化关闭保底帧）。
- [ ] **4.9 钩子输出**：`ResolveExchange` 返回元组扩展——加 `DefenseFrameHook?` 字段（null = 无防守帧需求，NPC 侧确定性结算）。Player 侧由 Godot 宿主读取此钩子驱动 QTE 帧窗。

### D. 回归守
- [x] **4.10 B.2/B.3 不退**：全整数溢出判定；off 逐字节 IDENTICAL（off 不进 DuelEngine）；IL 浮点零；1225 基线不退。

---

## Implementation Notes

- **溢出检测落点**：`DuelEngine.ResolveExchange`，在 cv-001 SEC 调制（cv-006）之后、伯努利掷骰之前。`if (p >= limits.OverflowThresholdPermille) overflowFlag = true;`
- **优先级链落点**：`ResolveExchange` 防御端判定重构为显式优先级链函数 `ApplyVerdictPriority(attackType, overflowFlag, isPlayer)`，返回 `(skipBernoulli, forceAutoWin, frameWindow)` 三元。
- **防守帧钩子落点**：新建 `src/Jianghu.Core/Cultivation/DefenseFrameHook.cs`（纯数据 record，零引擎依赖）。扩展 `ResolveExchange` 返回元组（当前四元 `(Dmg, Reflect, Poise, ChipImmune)`）→ 五元加 `DefenseFrameHook?`。
- **C2 正交性**：既有 `UT gap≥2 → margin=int.MaxValue → auto-win` 在 ResolveR2 层处理（PE 比较），溢出在 ResolveExchange 层处理（概率比较）——两层正交，互不干扰。
- **测试落点**：`tests/Jianghu.Core.Tests/Cultivation/OverflowTests.cs`——溢出判定纯函数 + NPC-vs-NPC Auto-Win + 优先级链顺序 + 保底帧数 knob 验证。
- **不碰**：Godot View QTE 实现（属 godot-host）；off 路径；calibrationMode 定义；cv-001/002/003/006/007/008 内部逻辑。

---

## Out of Scope

- **Godot View QTE 帧窗实现**：属 godot-host epic #14。本 story 仅定义 Model→View 整数钩子契约。
- **Player 侧输入采集**：属 godot-host 命令端口。
- **连续格挡递减（adr-0008 ⑩.3 BlockFrameDrStep）**：需 duel-local 格挡计数，属 cv-004-b 或 godot-host。
- **21 路数据铺设**：deferred（红线 A.8）。

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/Jianghu.Core.Tests/Cultivation/OverflowTests.cs` — 须存在且过 + B.2/B.3 守 + cv-001..008 全绿不退
**Status**: [x] 已实现并验证（主控核验 A.3 通过）
**实测证据**:
- `dotnet test` 全量 = **1243 绿 / 0 失败 / 0 跳过**
- OverflowTests = **18 绿**（纯函数 4 + 旋钮 2 + SEC=0溢出必中 2 + 跳过FlatDR 2 + 确定性 2 + off 1 + B.2 1 + 优先级 4）
- `dotnet build` = 0 警告 0 错误

---

## Completion Notes
**Completed**: 2026-07-14
**实现 sha**: `31c5e1a`（溢出检测+旋钮）+ `17abbea`（跳过OnDefend绝对秒杀）+ `f54d89a`（优先级链Tag>Overflow）+ `32b5e1e`（DefenseFrameHook契约+保底帧）
**Criteria**: 10/10 passing（AC 4.1-4.10 全部就位）
**机器证据（主控核验 A.3）**:
- OverflowTests 18 绿 + 全量 1243 绿 / 0 失败
**Deviations**:
- 防守帧钩子仅输出 Model 侧数据契约（DefenseFrameHook record），View 侧 QTE 帧窗实现属 godot-host epic #14（承 ADR-0004 边界）。
- GuaranteeFrame 当前恒输出保底帧数（2）——实际帧数计算需 Player 侧介入后调优（属 godot-host 域）。
**Code Review**: Complete（主控旗舰档 diff review；4 笔提交逐笔核验）
**cv-004 Model 侧闭环**: 溢出检测 + 必中 + 绝对秒杀 + 优先级链 + 防守帧钩子全部就位，ResolveExchange 返回 5 元组扩展完成。

---

## Dependencies

- Depends on: **adr-0008 Accepted**（✅）；**cv-001 Done**（`dbd070c`，概率主轴）；**cv-003 Done**（`9ad6be0`，标签门控）；**cv-006 Done**（`c57d365`，SEC 调制——溢出检测在 SEC 后）
- Unlocks: godot-host（防守帧 QTE 实现需要此钩子契约）
- Orthogonal to: cv-005（重标定——溢出是极端 case，不影响 [40,60]% 中间分布）
