# Story 006: SEC 闪避系数合流 — Layer ① Evasion

> **Epic**: combat-variance
> **Status**: Not Started
> **Layer**: Core
> **Type**: Logic
> **TR**: TR-BAL-001（防御漏斗第①层——闪避系数合流 cv-001 单次核心判定，为 cv-005 提供防御端可校准层）
> **Estimate**: 中 (1.5d)
> **Governing ADR**: **adr-0010**（primary，决策① SEC 合流 + 物理语义裁定）+ adr-0008（cv-001 概率主轴，本 story 不改其核心逻辑，仅在其入口前加 SEC 调制）+ adr-0001（B.2 整数确定性）+ adr-0003（B.3 off 逐字节）
> **Manifest Version**: 2026-07-03b（`docs/architecture/control-manifest.md`）
> **Last Updated**: 2026-07-14

## Context

**GDD**: `design/gdd/combat-system.md`（实现期同步修订）；深度源/权威 `docs/architecture/adr-0010-defense-funnel-mechanism.md`（决策① 概率管线合并）

**ADR Decision Summary**: SEC（招式闪避系数）**不触发新随机判定**，而是作为输入参数合流进 cv-001 的 `CombatMath` 查表——在 cv-001 已有的那**一次**伯努利判定内解决命中/闪避。SEC=1000 中性（permille 不变）；SEC=0 必中（显式分支，非数学 `max(1,SEC)` 小聪明）；SEC>1000 易被闪避（命中衰减）。

**物理语义（adr-0010 用户裁定）**：闪避 = "空间维度的概率博弈"（打没打中）→ 归入 cv-001 命中那一次掷骰。格挡 = "动能维度的确定性对撞"（防住多少）→ 确定性，归 cv-008。

**Engine**: .NET 8 / netstandard2.1 | **Risk**: **HIGH**（触 `Jianghu.Cultivation`：改 CombatMath 概率计算路径 / 触 cv-001 已验收判定管线 / B.2 禁浮点。B.7 旗舰档实现 + 主控独立核验 A.3）
**Engine Notes**: 无 post-cutoff API。`CombatSkillDef` record 末尾追加可选字段（同 cv-003 DamageType 范式）。不新增 PRNG 流——复用 cv-001 已有 `duelRng`。

**Control Manifest Rules (Core 层)**:
- Required: SEC 全整数运算（`p * 1000 / SEC`）；SEC==0 显式分支（不依赖 `max(1,SEC)` 数学技巧）；calibrationMode 旁路。
- Forbidden: 浮点（B.2）；新增 `duelRng.Next` 调用（单次判定纯洁性）；`daoHeart/innerDemon` 进 SEC 计算（B.5）。
- Guardrail: cv-001 回归全绿不退（同种子逐字节复现）；off worktree sha256 IDENTICAL（B.3）；IL 浮点零。

---

## 背景（承 adr-0010 决策① + cv-001 概率主轴已验收）

adr-0010 决策① 明确规定：SEC **合流**进 cv-001 的命中伯努利，**不新增平行掷骰**。合流方式：

```
// cv-001 现有：p = CombatMath.GetSuccessPermille(margin, defPe)
// 本 story 在调用前插入 SEC 调制：
if (SEC == 0) return MaxPermille;      // 必中标签，显式分支
int p_afterEvasion = (p * 1000) / SEC; // SEC=1000 中性；SEC>1000 衰减
```

**关键工程事实（主控勘探核实）**：

1. **SEC 挂 `CombatSkillDef` 可选字段**（同 cv-003 DamageType 范式）：`CombatSkillDef`（`CultivationSchema.cs:24`）record 末尾追加 `int Sec = 1000`。默认 1000 = 中性（permille 不变）。21 路全用定位构造 → 追加带默认参数 → 全部原样编译、惰性零行为改变。裸攻（skill==null）→ 默认 SEC=1000。
2. **SEC==0 显式分支**：adr-0010 明确抛弃 `max(1, SEC)` 数学小聪明。SEC==0 → 直接返回 MaxPermille（1000），不进入 `p*1000/SEC` 除零路径。语义清晰：必中标签。
3. **合流落点**：`CombatMath` 新增 `ApplyEvasionCoefficient(int p, int sec)` 静态纯函数（整数，B.2）。`ResolveExchange` 在 cv-001 伯努利判定前调用：`p = CombatMath.ApplyEvasionCoefficient(p, skill?.Sec ?? 1000)`。
4. **不碰 cv-001 核心查表**：`GetSuccessPermille(margin, defPe)` 内部逻辑一字不改。SEC 是其**前置调制器**，不是替代。
5. **calibrationMode 旁路**（承 cv-001/002/003）：标定期 SEC 调制旁路（`if(!calibrationMode)`），保 cv-005 seed-sweep 裸 PE 纯净。
6. **确定性 + off**：全整数确定性运算，无新增 RNG（不碰 duelRng）；off 走 legacy SparAction 不入 DuelEngine 天然守 B.3。
7. **duel-local**：SEC 是静态招式数据，无跨回合累积。不新增 duel-local 状态。

---

## Acceptance Criteria

- [ ] **6.1 SEC 字段 + CombatSkillDef 扩展**：`CombatSkillDef` 追加 `int Sec = 1000`（可选，向后兼容）。21 路现有构造零改动编译通过，默认 1000（中性）。
- [ ] **6.2 ApplyEvasionCoefficient 纯函数**：`CombatMath.ApplyEvasionCoefficient(int p, int sec)` — SEC==0 → 1000（必中）；SEC>0 → `p * 1000 / sec`（整数向下取整，B.2）。单测直验：SEC=1000 不变、SEC=0 必中、SEC=2000 半衰、SEC=500 命中抬升。
- [ ] **6.3 ResolveExchange 接线**：cv-001 伯努利判定前调用 `ApplyEvasionCoefficient`，传入攻方招式 SEC。裸攻（skill==null）默认 SEC=1000。测试：同 margin 不同 SEC → 命中率差异。
- [ ] **6.4 SEC 惰性零行为改变**：21 路全默认 SEC=1000 → 与 cv-001 同种子逐字节复现（全量 1147 绿不退）。此条是 B.3 在 on 模式的核心守门。
- [ ] **6.5 calibrationMode 旁路**：标定模式 SEC 调制不生效（等同 SEC=1000 中性），保裸 PE 平价。
- [ ] **6.6 B.2 + B.3 + 不退**：IL 浮点零；同种子逐字节（cv-001 回归）；off worktree sha256 IDENTICAL；cv-001/002/003 + balance-007/008 全绿不退。

---

## Implementation Notes

*承 adr-0010 决策① + cv-003 DamageType 范式：*

- **SEC 落点**：`CombatSkillDef`（`CultivationSchema.cs`）追加 `int Sec = 1000`（定位构造末尾可选参，承 cv-003 `DamageType` 范式）。21 路 Path 文件零改动。
- **ApplyEvasionCoefficient 落点**：`CombatMath`（`src/Jianghu.Core/Cultivation/CombatMath.cs`）新增 `public static int ApplyEvasionCoefficient(int p, int sec)`。SEC==0 显式返回 `MaxPermille`（1000）。SEC>0 → `p * 1000 / sec`（整数运算，自然向下取整，B.2）。
- **ResolveExchange 接线点**：在 cv-001 判定段（`DuelEngine.cs` 现 `GetSuccessPermille` 调用处）**之前**插入 SEC 调制。伪代码：
  ```csharp
  int p = CombatMath.GetSuccessPermille(margin, defPe);
  if (!calibrationMode) p = CombatMath.ApplyEvasionCoefficient(p, skill?.Sec ?? 1000);
  int roll = duelRng.NextInt(1000);
  bool hit = roll < p; // cv-001 现有判定，不变
  ```
- **不碰**：cv-001 `GetSuccessPermille` 内部实现；`duelRng` 消费逻辑；DoT/压制管线；off 路径。
- **测试落点**：新建 `tests/Jianghu.Core.Tests/Cultivation/EvasionSecTests.cs`。测试维度：纯函数（SEC==0/500/1000/2000）+ 接线（同 margin 不同 SEC 命中率差异）+ 惰性（默认 SEC=1000 同种子复现）+ calibration（标定旁路）+ B.3（off worktree sha256）。
- **不碰**：SBC 格挡系数（cv-008）；抗性 R 派生（cv-007）；21 路 SEC 数据铺设（deferred，红线 A.8）。

---

## Out of Scope（下切片 / 显式 deferred）

- **SBC 格挡系数 + Chip 调制** → cv-008（adr-0010 决策②，依赖 cv-003 Chip 模型）
- **抗性 R 派生 + 半衰减伤** → cv-007（adr-0010 决策③，DerivedProviders 独立系统）
- **三层串行接线 ①→②→③** → cv-008（依赖 cv-006 + cv-007 就位）
- **21 路 SEC 数据铺设（deferred，红线 A.8）**：机制骨架先行，所有招式默认 SEC=1000（中性）。各招闪避难易度 = 数值实现期铺（依赖招式设计 + cv-005 平衡标定）。
- **Godot View 闪避 QTE 帧窗**（adr-0004 Model→View 边界，属 godot-host / cv-004）

---

## QA Test Cases

*Logic 自动化测试 spec。SEC 调制 cv-001 概率，需 duelRng 但确定性可复现（固定 seed）。*

- **AC-1（6.1 字段）**：CombatSkillDef.Sec 默认 1000；21 路构造编译通过（build 绿即证）。
- **AC-2（6.2 纯函数）**：SEC=1000 → p 不变；SEC=0 → 1000；SEC=2000 → p/2；SEC=500 → p×2（clamped to 1000）。边界：p=0 任意 SEC 仍为 0；p=1000 SEC>1000 衰减。
- **AC-3（6.3 接线）**：同 margin=50，SEC=500 攻方命中率 > SEC=2000 攻方。具体：构造双攻方不同 SEC 同 margin 对同防方 → 命中次数差统计显著。
- **AC-4（6.4 惰性）**：全默认 SEC=1000 → 与 cv-001 基线同种子逐字节复现（`ActiveClashVarianceTests` 全绿）。
- **AC-5（6.5 calibration）**：标定模式 SEC 调制旁路，结果等同 SEC=1000。
- **AC-6（6.6 B.2/B.3）**：IL 浮点零；off OffByteIdentical + worktree sha256；cv-001/002/003 + balance-007/008 全绿。
- 测试文件：`tests/Jianghu.Core.Tests/Cultivation/EvasionSecTests.cs` + `Determinism/OffByteIdenticalTests.cs`（回归守）。

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/Jianghu.Core.Tests/Cultivation/EvasionSecTests.cs` — 须存在且过 + cv-001 同种子回归守 + off 逐字节回归守（`Determinism/` + worktree sha256）+ IL 浮点零 + cv-002/003/balance-007/008 不退
**Status**: [ ] 待实现（/dev-story）

---

## Dependencies

- Depends on: **adr-0010 Accepted**（✅ `d35fef6`）；**cv-001 Done**（`dbd070c`，SEC 合流进其概率判定）；**adr-0008 Accepted**（✅）
- Unlocks: cv-008（SBC 格挡调制——与 SEC 同属 CombatSkillDef 招式系数，字段范式复用）+ cv-005（防御端可校准层就位后的 seed-sweep）
- Orthogonal to: cv-007（抗性 R 派生——不同限界上下文，无代码依赖）
- Deferred dependency: 21 路 SEC 数据铺设（数值实现期）
