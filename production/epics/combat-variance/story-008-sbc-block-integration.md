# Story 008: SBC 格挡系数调制 + 三层漏斗串行接线 — Layer ② + Integration

> **Epic**: combat-variance
> **Status**: Not Started
> **Layer**: Core
> **Type**: Logic
> **TR**: TR-BAL-001（防御漏斗第②层——SBC 格挡系数调制 Chip 穿透 + 三层漏斗 ①→②→③ 串行接线，完成 adr-0010 防御端架构落盘）
> **Estimate**: 中大 (2.5d)
> **Governing ADR**: **adr-0010**（primary，决策② SBC 调制 Chip + 决策④ 判定顺序 + 全面复用 cv-003）+ adr-0008（cv-001 概率主轴）+ adr-0001（B.2）+ adr-0003（B.3 off 逐字节）+ adr-0002（B.9 模块工厂）
> **Manifest Version**: 2026-07-03b（`docs/architecture/control-manifest.md`）
> **Last Updated**: 2026-07-14

## Context

**GDD**: `design/gdd/combat-system.md`（实现期同步修订）；深度源/权威 `docs/architecture/adr-0010-defense-funnel-mechanism.md`（决策② SBC 调制 Chip + 决策④ 判定顺序 + 全面复用 cv-003 语义与 Chip 模型）

**ADR Decision Summary**: SBC（招式格挡系数）**不是掷骰阈值**，而是**调制 cv-003 Chip 穿透比例的乘子**（格挡 = 确定性对撞，不掷骰）。基准 SBC=1000 → 用 LimitsConfig 基准 ChipPermille；SBC 越低 → 有效 ChipPermille 越高（重锤破防语义）。同时完成三层漏斗在 `ResolveExchange` 的串行接线：①SEC 闪避（cv-006）→ ②门控+格挡（cv-003+SBC调制）→ ③抵抗（cv-007）。

**为什么 cv-008 是集成 story**：adr-0010 决策④ 定义了最终判定顺序。cv-006（①层）和 cv-007（③层）各自独立实现后，cv-008 负责：SBC 实现 + 确保三个独立系统在同一 `ResolveExchange` 管线中**串行协作**、**不互相污染**、**calibrationMode 统一旁路**、**既有测试全绿不退**。

**Engine**: .NET 8 / netstandard2.1 | **Risk**: **HIGH**（触 `Jianghu.Cultivation`：改 ResolveExchange 核心管线 / 调 cv-003 Chip 模型 / 串联 cv-006+007 / B.2/B.3/B.9 三地基。三层漏斗任一层的 bug 都会在 cv-008 集成测试中暴露。B.7 旗舰档实现 + 主控独立核验 A.3）
**Engine Notes**: 无 post-cutoff API。`CombatSkillDef` 追加 `Sbc` 字段（同 cv-006 的 `Sec` 范式）。

**Control Manifest Rules (Core 层)**:
- Required: SBC 全整数运算（`effChipPermille = ChipPermille × 1000 / max(1, SBC)`）；SBC 调制复用 cv-003 `ChipDamageFloor` 保底模型；三层漏斗判定顺序严格按 adr-0010 决策④；calibrationMode 统一旁路三层。
- Forbidden: 浮点（B.2）；新增 `duelRng.Next`（SBC 确定性，不掷骰）；裸 `new EffectOp`（B.9——SBC 是招式数据字段非 EffectOp）；三层漏斗任一层在 off 路径激活（B.3）。
- Guardrail: cv-001/002/003 全绿不退；off worktree sha256 IDENTICAL；IL 浮点零；三层漏斗集成后 1147 绿不退。

---

## 背景（承 adr-0010 决策② + 决策④ + cv-003 Chip 模型已验收）

### SBC 语义（adr-0010 决策② + 用户 2026-07-08 裁定）

SBC = "这一招有多容易被格挡削弱"：
- **SBC=1000（中性）**：Chip 穿透比例 = LimitsConfig 基准 ChipPermille（默认 300‰）
- **SBC<1000（重锤/钝器）**：有效 ChipPermille 抬升（防不住的动能更多），如 SBC=500 → effChip=600‰
- **SBC>1000（易格挡招式）**：有效 ChipPermille 降低（剑气/柔劲容易被招架吸收）
- **SBC=0**：归第②层"不可格挡穿透"处理，比照 cv-003 Blunt 门控

**关键：SBC 不掷骰**。格挡本身是否成功由 cv-003 的 Block 类模块（FlatDR/ReflectDamage）**确定性**决定——这是"动能维度确定性对撞"（adr-0010 用户裁定）。SBC 只调制格挡**成功后**的 Chip 穿透比例。

### 合流公式（整数 B.2）

```csharp
// 有效 Chip 穿透千分比（SBC 调制）
int effChipPermille = SBC == 0
    ? 1000  // 不可格挡穿透，全伤（比照 cv-003 Blunt 门控）
    : limits.ChipDamagePermille * 1000 / Math.Max(1, SBC);

// 复用 cv-003 保底防线模型（一字不改）：
// chipFloor = max(dmg, base×effChipPermille/1000 + MarginAdj)
```

### 三层漏斗最终判定顺序（adr-0010 决策④）

`ResolveExchange` 防御端串行顺序：
```
1. cv-001 命中判定（含 SEC 合流 cv-006）→ 未命中则伤害归零，短路（不进②③）
2. cv-003 门控 + 格挡（含 SBC 调制 cv-008）→ Blunt 关 Block / Elemental 关 Dodge + Chip 穿透
3. 既有 OnDefend 模块结算（FlatDR/Evade/Reflect/PostMul/SoulSplit，cv-003 门控后的存活者）
4. 第三层抵抗 cv-007：RawDamage × DamageMultiplier(R) → FinalDamage
5. 既有后置：SuppressionMatrix / 软情境 / cv-002 削韧派生（不变）
```

### 关键工程事实

1. **SBC 挂 `CombatSkillDef` 可选字段**（同 cv-006 `Sec` 范式）：`CombatSkillDef` 追加 `int Sbc = 1000`。默认 1000 = 中性（Chip 穿透 = 基准 ChipPermille）。21 路全默认 → 惰性零行为改变。裸攻（skill==null）→ 默认 SBC=1000。
2. **SBC 调制 Chip 落点**：cv-003 `ChipDamageFloor` 计算前，用 SBC 调制有效 ChipPermille。不新增函数——扩 cv-003 已有 Chip 计算段，传入 SBC 参数。**复用 cv-003 保底防线模型**（`max(dmg, base×ChipPermille/1000 + MarginAdj)`），不引入新公式。
3. **三层串行接线**：`ResolveExchange` 内严格按决策④顺序组织代码。步骤 1（SEC 命中）短路 → 步骤 2（门控+SBC Chip）→ 步骤 3（既有 OnDefend）→ 步骤 4（抵抗半衰 cv-007）→ 步骤 5（后置）。每步独立 `if(!calibrationMode)` 守卫，但**不嵌套**（三层旁路各自独立判定）。
4. **calibrationMode 统一旁路**：标定期三层漏斗全旁路——SEC 中性、SBC 中性（Chip 用基准 ChipPermille）、R 视为 0。各层 `if(!calibrationMode)` 独立，互不依赖。
5. **off 路径天然守 B.3**：legacy SparAction 不走 DuelEngine，三层漏斗全不激活。cv-008 不新增 off 路径代码。
6. **集成测试策略**：cv-008 的核心价值之一是**集成测试**——验证三层漏斗在 `ResolveExchange` 内串行协作不互相污染。测试需覆盖：①②③全开、仅①②、仅①③、仅②③、全关（calibration）、SEC=0 短路不进②③、各层 calibrationMode 独立旁路。
7. **反伤/吸血等后置不受破坏**（adr-0010 交付标准）：第③层抵抗在反伤收集**前**（抵抗只减最终扣血，不碰反伤计算源——反伤收集的是"进攻方原始输出"而非"防御方实际承受"）。此语义在 cv-003 既有实现中已确立，cv-008 不改。

---

## Acceptance Criteria

- [ ] **8.1 SBC 字段 + CombatSkillDef 扩展**：`CombatSkillDef` 追加 `int Sbc = 1000`（可选，向后兼容）。21 路现有构造零改动编译通过，默认 1000（中性）。
- [ ] **8.2 SBC 调制有效 ChipPermille**：`effChipPermille = SBC==0 ? 1000 : ChipPermille*1000/max(1,SBC)`（纯整数，B.2）。单测：SBC=1000→effChip=300（默认）；SBC=500→effChip=600；SBC=2000→effChip=150；SBC=0→effChip=1000（不可格挡）。
- [ ] **8.3 SBC 调制 Chip Damage**：Elemental 被 Block 成功 → `chipFloor = max(dmg, base×effChipPermille/1000 + MarginAdj)`（复用 cv-003 保底模型）。测试：同防方同攻击，SBC=500（重锤）Chip 穿透 > SBC=2000（易格挡）。
- [ ] **8.4 三层漏斗串行接线**：`ResolveExchange` 内严格按决策④顺序 ①→②→③→④→⑤。测试：SEC=0 必中 → 短路不进②③（伤害直接全额，无 Chip 无抵抗）；正常流程 ①命中→②Chip→③抵抗→最终伤害 < 原始伤害。
- [ ] **8.5 三层不互相污染**：①闪避未命中 → 伤害=0，不进②③（无 Chip 无减伤计算浪费）；②格挡失败（Block 类未触发）→ 无 Chip，但仍进③抵抗；③抵抗 R=0（低体质角色）→ 不减伤，但不影响①②结果。集成测试覆盖组合矩阵。
- [ ] **8.6 calibrationMode 三层统一旁路**：标定模式三层全旁路（SEC=1000、SBC 用基准 ChipPermille、R=0），等同 cv-001 裸概率 + cv-003 基准 Chip。测试：标定 vs 非标定同参数差异仅来自 cv-001 概率。
- [ ] **8.7 B.2 + B.3 + 不退**：IL 浮点零；同种子逐字节；off worktree sha256 IDENTICAL；cv-001/002/003 + cv-006 + cv-007 + balance-007/008 全绿不退。
- [ ] **8.8 反伤/吸血不受破坏**：反伤收集的"进攻方原始输出"不受抵抗层减伤影响（抵抗只减最终扣血）。测试：攻方反伤模块 → 防方高 R → 攻方反伤量 = 基于原始输出（非减伤后最终扣血）。

---

## Implementation Notes

*承 adr-0010 决策② + 决策④ + cv-003 Chip 模型 + cv-006 Sec 范式：*

- **SBC 落点**：`CombatSkillDef`（`CultivationSchema.cs`）追加 `int Sbc = 1000`（定位构造末尾可选参，承 cv-006 `Sec` 范式）。
- **SBC 调制 Chip 落点**：`DuelEngine.ResolveExchange` 内 cv-003 Chip 计算段。现有 Chip 判定位于 OnDefend 减伤后、return 前。在此段**之前**计算 `effChipPermille`（SBC 调制），然后传入既有 Chip 公式。伪代码：
  ```csharp
  // step 2: 门控 (cv-003, 不变)
  // ...
  // step 2b [新]: SBC 调制有效 ChipPermille
  int effChipPermille = limits.ChipDamagePermille;
  if (!calibrationMode && defenderBlockFired && damageType == DamageType.Elemental) {
      effChipPermille = skill?.Sbc == 0
          ? 1000  // 不可格挡穿透
          : limits.ChipDamagePermille * 1000 / Math.Max(1, skill?.Sbc ?? 1000);
  }
  // step 2c: cv-003 ChipDamageFloor (复用既有公式，只换 effChipPermille)
  int chipFloor = Math.Max(dmg, baseDmg * effChipPermille / 1000 + marginAdj);
  ```
- **三层接线落点**：`ResolveExchange` 管线重构为显式 5 步顺序（见背景 §三层漏斗最终判定顺序）。每步加注释标注 story 来源（cv-001/006/003/008/007）。不新增嵌套——各层平级串行。
- **SEC==0 短路**：步骤 1 判定未命中 → `return 0`（不执行步骤 2-5）。此为既有 cv-001 行为（miss → 伤害归零），cv-008 不改此语义。
- **反伤保护**：步骤 3 的 OnDefend 结算中，反伤模块（ReflectDamage）收集的是**步骤 1-2 后的 dmg**（抵抗前）还是**步骤 4 后的 FinalDamage**（抵抗后）——adr-0010 要求反伤基于"进攻方原始输出"。核实 cv-003 既有实现中的反伤收集点，确保步骤 4 抵抗层在反伤收集**之后**或反伤源不受其影响。
- **测试落点**：
  - 新建 `tests/Jianghu.Core.Tests/Cultivation/BlockSbcTests.cs`（SBC 纯函数 + Chip 调制 + SBC=0 不可格挡）
  - 新建 `tests/Jianghu.Core.Tests/Cultivation/DefenseFunnelIntegrationTests.cs`（三层集成：组合矩阵 × calibration × SEC=0 短路 × 反伤保护）
  - 回归：`TagGatingChipTests.cs`（cv-003 全绿不退）、`EvasionSecTests.cs`（cv-006）、`ResistanceTests.cs`（cv-007）
- **不碰**：cv-001 概率核心/cv-002 削韧/cv-003 门控枚举与分类函数（只读不写）；SEC 合流（cv-006 领域）；抵抗半衰公式（cv-007 领域）；off 路径；calibrationMode 定义。

---

## Out of Scope（下切片 / 显式 deferred）

- **SEC 闪避合流实现** → cv-006（cv-008 依赖其 SEC 字段 + 步骤 1 接线，不重复实现）
- **抗性 R 派生实现** → cv-007（cv-008 依赖其 DerivedProviders + 步骤 4 接线，不重复实现）
- **连续格挡防守帧递减（adr-0008 决策⑩.3）** → cv-004（需 duel-local 格挡计数，本 story 不改格挡次数逻辑）
- **Godot View 格挡 QTE 帧窗** → godot-host / cv-004
- **21 路 SEC/SBC 数据铺设（deferred，红线 A.8）**：所有招式默认 SEC=1000/SBC=1000（中性），机制骨架先行

---

## QA Test Cases

*Logic 自动化测试 spec。SBC 调制确定性（无 RNG），集成测试需 duelRng（确定性可复现）。*

- **AC-1（8.1 字段）**：CombatSkillDef.Sbc 默认 1000；21 路构造编译通过。
- **AC-2（8.2 SBC 调制）**：SBC=1000→effChip=300；SBC=500→600；SBC=2000→150；SBC=0→1000。边界：SBC=1（极端重锤）→ effChip 极大但不溢出。
- **AC-3（8.3 Chip 穿透差）**：同防方（高 FlatDR）同 Elemental 攻击，SBC=500 实际伤害 > SBC=2000（重锤穿透更多）。
- **AC-4（8.4 三层串行）**：SEC=0 攻击 → 伤害 = 原始伤害（不进 Chip/抵抗）；正常 SEC=1000 → ①命中→②Chip（若 Elemental+Block）→③抵抗 → 最终伤害 ≤ 原始伤害。
- **AC-5（8.5 不互相污染）**：①miss → dmg=0（②③不执行，无 Chip 计算浪费）；②无 Block（防方无 FlatDR）→ 无 Chip，但③抵抗仍生效；③R=0→无减伤，但①②结果不变。三层各自独立守卫。
- **AC-6（8.6 calibration 统一旁路）**：标定模式 SEC/SBC/R 全中性，与裸 cv-001+cv-003 基准同种子复现。
- **AC-7（8.7 B.2/B.3/不退）**：IL 浮点零；off worktree sha256 IDENTICAL；cv-001..007 + balance-007/008 全绿。
- **AC-8（8.8 反伤保护）**：攻方 ReflectDamage 模块 → 防方 R=5000 → 攻方反伤量 = 基于原始输出（不受抵抗减伤影响）。
- 测试文件：`tests/Jianghu.Core.Tests/Cultivation/BlockSbcTests.cs` + `DefenseFunnelIntegrationTests.cs` + 回归守 `Determinism/OffByteIdenticalTests.cs`

---

## Test Evidence

**Story Type**: Logic + Integration
**Required evidence**: `BlockSbcTests.cs` + `DefenseFunnelIntegrationTests.cs` — 须存在且过 + cv-001..007 回归守 + off 逐字节 + IL 浮点零
**Status**: [ ] 待实现（/dev-story）

---

## Dependencies

- Depends on: **adr-0010 Accepted**（✅ `d35fef6`）；**cv-003 Done**（`9ad6be0`，Chip 保底模型 + DamageType 枚举）；**cv-006 Done**（SEC 闪避合流，步骤 1 + Sec 字段范式）；**cv-007 Done**（抗性 R 派生，步骤 4 + DerivedProviders）
- Unlocks: cv-005（三层防御漏斗就位后的 seed-sweep [40,60]% 重标定——防御端终于有完整的可校准判定层）
- Blocks: 防御漏斗在 ResolveExchange 的最终闭环（cv-008 是实现链终点；之后 cv-005 可开始）
