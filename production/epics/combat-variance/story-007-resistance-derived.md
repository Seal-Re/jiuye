# Story 007: 派生抗性 R + 半衰减伤 — Layer ③ Resistance

> **Epic**: combat-variance
> **Status**: Not Started
> **Layer**: Core
> **Type**: Logic
> **TR**: TR-BAL-001（防御漏斗第③层——抗性半衰减伤，为防御端提供"面板→减伤"的可追溯派生链）
> **Estimate**: 中大 (2.0d)
> **Governing ADR**: **adr-0010**（primary，决策③ 抵抗层 + 派生抗性 R + 半衰公式 + B.5 守）+ adr-0001（B.2 整数确定性，半衰全整数）+ adr-0003（B.3 off 逐字节）+ adr-0004（Model/View 边界，R 不进 View）
> **Manifest Version**: 2026-07-03b（`docs/architecture/control-manifest.md`）
> **Last Updated**: 2026-07-14

## Context

**GDD**: `design/gdd/combat-system.md`（实现期同步修订）；深度源/权威 `docs/architecture/adr-0010-defense-funnel-mechanism.md`（决策③ 抵抗层 + 派生抗性 R）

**ADR Decision Summary**: 第三层抵抗采用**半衰模型**（纯整数 B.2）：`DamageMultiplier = K × 1000 / (K + R)`，R=0 → ×1000（无减伤），R=K → ×500（半衰），R→∞ → →0（趋 1 保底 `max(1, ...)`）。抗性 R 必须是**派生属性**（Derived Stat），绝不为 `StatBlock` 挂无根字段。R 由现有维度映射：体质→物理抗性、识/悟性→属性/法术抗性、功法标签（HasBodyArt→抬物理抗）+ 法宝 OnDefend 效果。内力留作后续主动护盾蓝条，不进常驻 R。

**关键约束（B.5）**：派生抗性 R **禁引用 daoHeart/innerDemon**，**禁进 EffectivePower**（抗性只作防御结算，不算战力）。

**Engine**: .NET 8 / netstandard2.1 | **Risk**: **HIGH**（触 `Jianghu.Cultivation`：新增 DerivedProvider / 改 ResolveExchange 判定管线 / B.2 半衰整数化 / B.5 道心解耦。B.7 旗舰档实现 + 主控独立核验 A.3）
**Engine Notes**: 无 post-cutoff API。`DerivedProviders` 已有先例（既有派生属性），本 story 追加 `ResistanceOf`。`LimitsConfig` 新增 3+ 旋钮（A.10）。

**Control Manifest Rules (Core 层)**:
- Required: 半衰公式全整数 `K*1000/(K+R)`；R 派生化（从体质/识/功法标签/法宝映射）；`max(1, ...)` 保底；旋钮外置 LimitsConfig。
- Forbidden: 浮点（B.2，禁 `Math.Round`/浮点除法）；`StatBlock` 加无根字段（R 不进四维）；`daoHeart/innerDemon` 进 R 或 EffectivePower（B.5）。
- Guardrail: IL 浮点零；off worktree sha256 IDENTICAL（B.3）；cv-001/002/003 + balance-007/008 全绿不退。

---

## 背景（承 adr-0010 决策③ + 用户 2026-07-08 裁定映射方向）

adr-0010 决策③ 确立：抵抗层是防御漏斗的最后一环（①闪避→②格挡→**③抵抗**），在 OnDefend 模块结算后、SuppressionMatrix 前对 RawDamage 做半衰减伤。R 的数据链**完全可追溯**（从 StatBlock 四维 + 功法标签 + 法宝效果派生，无凭空出现的魔法数字）。

### 半衰公式（纯整数 B.2）

```
DamageMultiplier = K × 1000 / (K + R)     // K=半衰常数(如500)，R=派生抗性
FinalDamage      = max(1, RawDamage × DamageMultiplier / 1000)  // 向下取整，≥1保底
```

- R=0 → multiplier=1000（无减伤，全伤）
- R=K → multiplier=500（半衰，50% 减伤）
- R→∞ → multiplier→0（趋 1 保底，永不归零）

### 抗性 R 派生映射（用户 2026-07-08 裁定）

| 抗性维度 | 映射源 | 系数旋钮（LimitsConfig） |
|---|---|---|
| 物理抗性 | 体质(Constitution) | `PhysResistPerConstitution`（整数，如 50 = 每点体质+50 R） |
| 属性/法术抗性 | 识/悟性(Insight) | `ElemResistPerInsight`（整数） |
| 物理抗性加成 | `HasBodyArt` 功法标签 | `BodyArtPhysResistBonus`（固定加值） |
| 属性抗性加成 | 对应门类功法标签 | `PathElemResistBonus`（固定加值） |
| 防御法宝加成 | ArtifactDef.Effects OnDefend | 经既有 ArtifactDef 管线（不改法宝系统） |

**内力不进常驻 R**（用户裁定：留作后续主动护盾蓝条）。

### 关键工程事实

1. **DerivedProviders 落点**：`DerivedProviders`（`src/Jianghu.Core/Cultivation/DerivedProviders.cs`）新增静态方法 `ResistanceOf(CultivationState cultivation, StatBlock stats, CodePathSource path, DamageType damageType)` → 按 DamageType 分物理/属性抗，返回 `int R`。纯整数派生，**不进 EffectivePower**（B.5：与 daoHeart 一样，抗性只作防御结算）。
2. **DamageType → 抗性维度映射**：Normal → 物理抗性（体质派生）；Blunt → 物理抗性（体质派生）；Elemental → 属性抗性（识派生）。此映射是**确定性规则**，不依赖 RNG。
3. **半衰计算落点**：`CombatMath`（`CombatMath.cs`）新增 `ApplyResistance(int rawDamage, int R, int K)` 静态纯函数。公式 `max(1, rawDamage * K * 1000 / (K + R) / 1000)` 或等价整数化（注意中间溢出，使用 `long` 中间类型或分步运算）。
4. **ResolveExchange 接线点**：在既有 OnDefend 模块结算后、SuppressionMatrix 前（adr-0010 决策④管线 step 4）。伪代码：
   ```csharp
   // step 3: 既有 OnDefend 结算（不变）
   // step 4 [新]: 抵抗层
   if (!calibrationMode) {
       int R = DerivedProviders.ResistanceOf(defCult, defStats, defPath, skill?.Damage ?? DamageType.Normal);
       dmg = CombatMath.ApplyResistance(dmg, R, limits.ResistanceHalfLifeK);
   }
   // step 5: SuppressionMatrix / cv-002 削韧（不变）
   ```
5. **半衰常数 K 外置**：`LimitsConfig` 新增 `ResistanceHalfLifeK`（默认 500，int + init + Validate `>0`）。K 越大 → 减伤曲线越平缓（需要更多 R 才能显著减伤）。负值抛。
6. **calibrationMode 旁路**：标定期抵抗层不生效（`if(!calibrationMode)`），保 cv-005 seed-sweep 裸 PE 纯净。承 cv-001/002/003/006 一致。
7. **确定性 + off**：全整数确定性运算，无 RNG（不碰 duelRng）。R 派生源（StatBlock 四维 + 功法标签 + 法宝）在 dueling 期间不变（无跨回合累积），duel-local 纯净。off 走 legacy SparAction 不入 DuelEngine 天然守 B.3。
8. **B.5 红线守门**：`ResistanceOf` 参数列表**不含** `daoHeart`/`innerDemon`。测试显式验证：调 `EffectivePower` 前后 R 值不变（证 R 不进战力）。

---

## Acceptance Criteria

- [ ] **7.1 DerivedProviders.ResistanceOf**：按 DamageType 分物理/属性抗。Normal/Blunt → 体质派生；Elemental → 识派生。功法标签（HasBodyArt）+ 法宝 OnDefend 加成叠加。纯整数返回。单测：同角色不同 DamageType 返回不同 R。
- [ ] **7.2 CombatMath.ApplyResistance**：半衰公式 `max(1, rawDamage × K × 1000 / (K + R) / 1000)` 纯整数（B.2）。单测：R=0 → 伤害不变；R=K → 伤害≈半衰；R 极大 → 伤害=1（保底）；rawDamage=1 任意 R 仍为 1。
- [ ] **7.3 LimitsConfig 旋钮**：`ResistanceHalfLifeK`(默认 500) + `PhysResistPerConstitution`(默认 50) + `ElemResistPerInsight`(默认 50) + `BodyArtPhysResistBonus`(默认 100) + `PathElemResistBonus`(默认 100)。全部 int + init + Validate 断言（K>0，系数≥0）。默认安全/负值抛。
- [ ] **7.4 ResolveExchange 接线**：OnDefend 结算后、SuppressionMatrix 前插入抵抗层。测试：防方高体质 → 物理攻击减伤 > 低体质；防方高识 → Elemental 攻击减伤 > 低识。
- [ ] **7.5 B.5 道心解耦**：`ResistanceOf` 参数不含 daoHeart/innerDemon。EffectivePower 不含 R。测试显式验证。
- [ ] **7.6 calibrationMode 旁路**：标定模式抵抗层不生效（R 视为 0），保裸 PE 平价。
- [ ] **7.7 B.2 + B.3 + 不退**：IL 浮点零；半衰中间值不溢出（long 中间类型）；同种子逐字节；off worktree sha256 IDENTICAL；cv-001/002/003 + balance-007/008 全绿不退。

---

## Implementation Notes

*承 adr-0010 决策③ + DerivedProviders 既有范式：*

- **ResistanceOf 落点**：`DerivedProviders`（`src/Jianghu.Core/Cultivation/DerivedProviders.cs`）新增 `public static int ResistanceOf(CultivationState cultivation, StatBlock stats, CodePathSource path, DamageType damageType)`。内部：`int baseR = damageType == DamageType.Elemental ? stats.Insight * limits.ElemResistPerInsight : stats.Constitution * limits.PhysResistPerConstitution`；`if (path.Tags.HasBodyArt) baseR += limits.BodyArtPhysResistBonus`；法宝加成遍历 `cultivation.Artifacts` OnDefend 效果。参数不含 daoHeart/innerDemon（B.5）。
- **ApplyResistance 落点**：`CombatMath`（`CombatMath.cs`）新增 `public static int ApplyResistance(int rawDamage, int R, int K)`。使用 `long` 中间类型防溢出：`long numerator = (long)rawDamage * K * 1000; long denominator = K + R; int dmg = (int)(numerator / denominator / 1000); return Math.Max(1, dmg);`。`rawDamage` 已是 ×Scale 空间的值（与现有 DuelEngine 一致）。
- **ResolveExchange 接线点**：OnDefend 模块结算（含 Block/Dodge 门控 cv-003）→ 软情境 → [新] 抵抗层 → SuppressionMatrix → return。伪代码见背景 §4。
- **旋钮**：`LimitsConfig`（`src/Jianghu.Core/Config/LimitsConfig.cs`）追加 5 个 int 属性 + Validate 断言 + 默认值（承 cv-002/003 范式）。
- **测试落点**：新建 `tests/Jianghu.Core.Tests/Cultivation/ResistanceTests.cs`。测试维度：纯函数（半衰公式各 R 值 + 溢出边界）+ DerivedProviders（不同 DamageType 返回不同 R + 功法标签加成 + B.5 不含 daoHeart）+ 接线（高体质 vs 低体质伤害差 + Elemental vs Blunt 抗性差）+ calibration + B.3。
- **不碰**：法宝系统（只读既有 ArtifactDef.Effects）；EffectivePower 计算；cv-001 概率/cv-002 削韧/cv-003 门控内部逻辑；SEC/SBC 招式系数（cv-006/008）。

---

## Out of Scope（下切片 / 显式 deferred）

- **SEC 闪避系数** → cv-006（CombatMath 命中调制，与本 story 正交）
- **SBC 格挡系数 + 三层串行** → cv-008
- **主动护盾（内力消耗型防御）**：内力留作后续主动技能资源，不进常驻 R（adr-0010 用户裁定）
- **ElementType 正交字段**：若未来需火/冰/毒元素相克，另立字段，不混 DamageType（adr-0010 决策②）
- **21 路功法标签数据铺设（deferred，红线 A.8）**：机制骨架先行，HasBodyArt/PathElemResist 标签 = 数值实现期铺

---

## QA Test Cases

*Logic 自动化测试 spec。纯确定性，无 RNG。*

- **AC-1（7.1 DerivedProviders）**：同角色 Normal→R_phys > 0；Elemental→R_elem > 0（通常不同）；无功法标签角色 R 仅来自四维；HasBodyArt 角色 Physical R 更高。
- **AC-2（7.2 半衰纯函数）**：R=0 → dmg 不变；R=K(500) → dmg≈raw/2；R=2000 → dmg≈raw×0.2；R=100000 → dmg=1（保底）；rawDamage=1,R=0 → 1。边界：rawDamage=int.MaxValue/1000 不溢出。
- **AC-3（7.3 旋钮）**：默认 K=500 安全；K=0 或负值抛（Validate）；系数默认合理（PhysResistPerConstitution=50 不极端）。
- **AC-4（7.4 接线）**：防方体质 100→R≈5000，物理攻击减伤 ≈90%。对比防方体质 10→R≈500，减伤 ≈50%。Elemental 攻击走识而非体质。
- **AC-5（7.5 B.5）**：`ResistanceOf` 参数无 daoHeart；EffectivePower 不含 R（修改 daoHeart → EffectivePower 不变，R 不变）。
- **AC-6（7.6 calibration）**：标定模式 R 视为 0，伤害无减损。
- **AC-7（7.7 B.2/B.3）**：IL 浮点零；off OffByteIdentical + worktree sha256；cv-001/002/003 + balance-007/008 全绿。
- 测试文件：`tests/Jianghu.Core.Tests/Cultivation/ResistanceTests.cs` + `Determinism/OffByteIdenticalTests.cs`（回归守）。

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/Jianghu.Core.Tests/Cultivation/ResistanceTests.cs` — 须存在且过 + off 逐字节回归守（`Determinism/` + worktree sha256）+ IL 浮点零 + cv-001/002/003/balance-007/008 不退
**Status**: [ ] 待实现（/dev-story）

---

## Dependencies

- Depends on: **adr-0010 Accepted**（✅ `d35fef6`）；**adr-0008 Accepted**（✅）；**DerivedProviders 既有先例**（✅）
- Unlocks: cv-008（三层串行接线——抵抗层作为漏斗最后一环就位）；cv-005（防御端可校准层）
- Orthogonal to: cv-006（SEC 闪避——不同限界上下文，无代码依赖）；cv-003（DamageType 枚举已就位，本 story 只读不写）
- Deferred dependency: 法宝 OnDefend 效果对接 R（既有 ArtifactDef 管线，本 story 预留接口不铺数据）
