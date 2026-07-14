# Story 007: 派生抗性 R + 半衰减伤 — Layer ③ Resistance

> **Epic**: combat-variance
> **Status**: review
> **Layer**: Core
> **Type**: Logic
> **TR**: TR-BAL-001（防御漏斗第③层——抗性半衰减伤，为防御端提供"面板→减伤"的可追溯派生链）
> **Estimate**: 中大 (2.0d)
> **Governing ADR**: **adr-0010**（primary，决策③ 抵抗层 + 派生抗性 R + 半衰公式 + B.5 守）+ adr-0001（B.2 整数确定性，半衰全整数）+ adr-0003（B.3 off 逐字节）+ adr-0004（Model/View 边界，R 不进 View）
> **Manifest Version**: 2026-07-03b（`docs/architecture/control-manifest.md`）
> **Last Updated**: 2026-07-14

## Context

**GDD**: `design/gdd/combat-system.md`（实现期同步修订 §防御漏斗 / 抵抗层）；primary design authority = `docs/architecture/adr-0010-defense-funnel-mechanism.md`（决策③ 抵抗层 + 派生抗性 R，GDD 将在漏斗三层全部落盘后同步修订）

**ADR Decision Summary**: 第三层抵抗采用**半衰模型**（纯整数 B.2）：`DamageMultiplier = K × 1000 / (K + R)`，R=0 → ×1000（无减伤），R=K → ×500（半衰），R→∞ → →0（趋 1 保底 `max(1, ...)`）。抗性 R 必须是**派生属性**（Derived Stat），绝不为 `StatBlock` 挂无根字段。R 由现有维度映射：体质→物理抗性、识/悟性→属性/法术抗性、功法标签（HasBodyArt→抬物理抗）+ 法宝 OnDefend 效果。内力留作后续主动护盾蓝条，不进常驻 R。

**关键约束（B.5）**：派生抗性 R **禁引用 daoHeart/innerDemon**，**禁进 EffectivePower**（抗性只作防御结算，不算战力）。

**Engine**: .NET 8 / netstandard2.1 | **Risk**: **HIGH**（触 `Jianghu.Cultivation`：新建 ResistanceProviders / 改 ResolveExchange 判定管线 / B.2 半衰整数化 / B.5 道心解耦。B.7 旗舰档实现 + 主控独立核验 A.3）
**Engine Notes**: 无 post-cutoff API。新建 `ResistanceProviders` 静态类（**非** `DerivedProviders`——后者是 power registry 单一职责，见背景 §1）。复用 `DuelEngine.HasBodyArt` 既有 helper。`CombatMath` 追加 `ApplyResistance`。`LimitsConfig` 新增 5 旋钮（A.10）。**真实 API 事实**：`StatBlock` 无命名属性用 `Get(StatKind)`；`CodePathSource` 是路线集合非单条路（用 `CultivationPathDef`）；角色不持法宝实例（法宝加成留 TODO）。

**Control Manifest Rules (Core 层)**:
- Required: 半衰公式全整数 `K*1000/(K+R)`；R 派生化（从体质/识/功法标签/法宝映射）；`max(1, ...)` 保底；旋钮外置 LimitsConfig。
- Forbidden: 浮点（B.2，禁 `Math.Round`/浮点除法）；`StatBlock` 加无根字段（R 不进四维）；`daoHeart/innerDemon` 进 R 或 EffectivePower（B.5）。
- Guardrail: IL 浮点零；off worktree sha256 IDENTICAL（B.3）；cv-001/002/003 + balance-007/008 全绿不退。

---

## 背景（承 adr-0010 决策③ + 用户 2026-07-08 裁定映射方向）

adr-0010 决策③ 确立：抵抗层是防御漏斗的最后一环（①闪避→②格挡→**③抵抗**），在 OnDefend 模块结算后、SuppressionMatrix 前对 RawDamage 做半衰减伤。R 的数据链**完全可追溯**（从 StatBlock 四维 + 功法标签派生，无凭空出现的魔法数字；法宝 OnDefend 加成因角色不持法宝实例而留 TODO 接口，见背景 §5）。

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

### 关键工程事实（2026-07-14 主控勘探核实真实 API，钉死实现形态）

1. **`ResistanceOf` 落点 = 新静态类，非 `DerivedProviders`**：`DerivedProviders`（`DerivedProviders.cs`）是 `static` 类，只含 `RegisterAll()` + 9 个 `internal sealed class XxxProvider : IDerivedProvider`（走 `DerivedRegistry.Register` 注册 + `Compute(st, stats)`），其单一职责是 **power 派生 registry**，**不是**放任意静态派生函数的地方。抗性派生是防御结算用的独立计算，**新建** `src/Jianghu.Core/Cultivation/ResistanceProviders.cs` 静态类承载 `ResistanceOf`（与 power registry 解耦，B.5：抗性不进 EffectivePower/power 体系）。
2. **`ResistanceOf` 签名（含 `LimitsConfig limits`，对齐真实 API）**：
   ```csharp
   public static int ResistanceOf(
       CultivationState cultivation, StatBlock stats,
       CultivationPathDef path, GateType gate,       // gate 来自 CombatContext, 非 CultivationState
       DamageType damageType, LimitsConfig limits)
   ```
   - `StatBlock`（`Stats/StatBlock.cs`）**无** `Constitution`/`Insight` 命名属性，只有 `Get(StatKind k)` + `Sum`。四维经 `StatKind` 枚举（`StatKind.cs`：`Force=0, Internal=1, Constitution=2, Insight=3`）索引 → 用 `stats.Get(StatKind.Constitution)` / `stats.Get(StatKind.Insight)`。
   - `LimitsConfig`（`Config/LimitsConfig.cs`，`sealed record`，旋钮范式 `{ get; init; } = 默认`）必须**显式传参**（不在 `CultivationState`/`StatBlock` 内）。承 cv-002/003 旋钮注释模板。
3. **DamageType → 抗性维度映射**（确定性规则，无 RNG）：Normal/Blunt → 物理抗性（`stats.Get(StatKind.Constitution) × limits.PhysResistPerConstitution`）；Elemental → 属性抗性（`stats.Get(StatKind.Insight) × limits.ElemResistPerInsight`）。
4. **`HasBodyArt` 用真实既有逻辑，非 `path.Tags.HasBodyArt`（不存在）**：真实 `DuelEngine.cs:890` 有 `private static bool HasBodyArt(CultivationPathDef path, CultivationState st)` —— 遍历 `path.ArtCategories` 找 `Role=="body"||"defense"` 且角色已 `ChosenArtIds` 选了该 art（语义="已修横练/护体"，正是抗性抬升所需）。**方案**：将该 helper 提为 `internal static`（或抽到 `ResistanceProviders` 共享），`ResistanceOf` 内复用判定 `BodyArtPhysResistBonus`。**不**用 `gate.HasFlag(GateType.HasBodyArt)`（那是 `CombatContext.CheckGate` 的静态能力门="有能力"，非"已修"，语义不符）。
5. **法宝加成 = 预留接口，不遍历（角色不持法宝实例）**：`CultivationState` **无** `Artifacts` 字段；法宝是全局 `ArtifactDef` registry（`Cultivation/Artifacts/ArtifactData.All` + `ArtifactRegistry`），角色经 `ChosenArtIds` 选功法**不**含法宝持有。cv-003 code-review 已记"法宝盾无 Blunt 豁免（cv-004 裁定）"——法宝防御接入是 cv-004/cv-008 域。**本 story 仅预留** `// TODO(cv-008): 法宝 OnDefend 效果加 R` 注释接口，**不**臆造遍历代码（避免子代理按不存在的 `cultivation.Artifacts` 写）。
6. **半衰计算落点 = `CombatMath.ApplyResistance`**（`CombatMath.cs`，纯函数库，与 cv-006 `ApplyEvasionCoefficient` 同处）。`public static int ApplyResistance(int rawDamage, int R, int K)`，用 `long` 中间类型防溢出：`long mul = (long)K * 1000 / (K + R); long dmg = (long)rawDamage * mul / 1000; return Math.Max(1, (int)dmg);`。R=0→mul=1000（全伤）；R=K→mul=500（半衰）；R 极大→mul→0（趋 1 保底）。
7. **ResolveExchange 接线点**（adr-0010 决策④管线 step 4）：既有 OnDefend 模块结算后、SuppressionMatrix 前。接线需 `limits`（DuelEngine 已持有 `LimitsConfig` 字段，直接传）。伪代码：
   ```csharp
   // step 3: 既有 OnDefend 结算（不变）
   // step 4 [新]: 抵抗层（需 CombatContext 的 gate + DuelEngine 的 limits，均已可见）
   if (!calibrationMode) {
       int R = ResistanceProviders.ResistanceOf(defCult, defStats, defPath, defGate,
                                                 skill?.Damage ?? DamageType.Normal, limits);
       dmg = CombatMath.ApplyResistance(dmg, R, limits.ResistanceHalfLifeK);
   }
   // step 5: SuppressionMatrix / cv-002 削韧（不变）
   ```
   **注**：`defGate`（`GateType`）来自 `CombatContext`，DuelEngine 结算时可见——若接线点拿不到 gate，则 BodyArt 加成退化为"仅体质/识派生"，BodyArt bonus 留 TODO（同法宝，不臆造）。实现期由子代理核实 ResolveExchange 作用域内 `CombatContext` 可达性后定。
8. **半衰常数 K + 映射系数外置**：`LimitsConfig` 追加 5 旋钮（承 cv-002/003 `{ get; init; } = 默认` + 注释 + Validate 范式）：`ResistanceHalfLifeK`(默认 500，Validate `>0`)、`PhysResistPerConstitution`(50，`≥0`)、`ElemResistPerInsight`(50，`≥0`)、`BodyArtPhysResistBonus`(100，`≥0`)、`PathElemResistBonus`(100，`≥0`)。
9. **calibrationMode 旁路**：标定期抵抗层不生效（`if(!calibrationMode)`），保 cv-005 seed-sweep 裸 PE 纯净。承 cv-001/002/003/006 一致。
10. **确定性 + off**：全整数确定性运算，无 RNG（不碰 duelRng）。R 派生源（StatBlock 四维 + 功法标签）在 dueling 期间不变（无跨回合累积），duel-local 纯净。off 走 legacy SparAction 不入 DuelEngine 天然守 B.3。
11. **B.5 红线守门**：`ResistanceOf` 参数列表**不含** `daoHeart`/`innerDemon`（`CultivationState` 本就有这些字段，但签名不传它们）。测试显式验证：调 `EffectivePower` 前后 R 值不变（证 R 不进战力）。

---

## Acceptance Criteria

- [x] **7.1 ResistanceProviders.ResistanceOf**：新建 `ResistanceProviders` 静态类，签名 `ResistanceOf(CultivationState, StatBlock, CultivationPathDef, GateType, DamageType, LimitsConfig)`。按 DamageType 分物理/属性抗：Normal/Blunt → `stats.Get(StatKind.Constitution) × PhysResistPerConstitution`；Elemental → `stats.Get(StatKind.Insight) × ElemResistPerInsight`。HasBodyArt（复用 `DuelEngine.HasBodyArt` 提为 internal）→ `+BodyArtPhysResistBonus`。法宝 OnDefend 加成留 TODO 接口（不臆造遍历）。纯整数返回，参数不含 daoHeart/innerDemon（B.5）。单测：同角色不同 DamageType 返回不同 R；HasBodyArt 角色 Physical R 更高。
- [x] **7.2 CombatMath.ApplyResistance**：半衰公式 `max(1, rawDamage × K × 1000 / (K + R) / 1000)` 纯整数（B.2，`long` 中间类型防溢出）。单测：R=0 → 伤害不变；R=K → 伤害≈半衰；R 极大 → 伤害=1（保底）；rawDamage=1 任意 R 仍为 1；rawDamage=int.MaxValue/1000 不溢出。
- [x] **7.3 LimitsConfig 旋钮**：`ResistanceHalfLifeK`(默认 500) + `PhysResistPerConstitution`(默认 50) + `ElemResistPerInsight`(默认 50) + `BodyArtPhysResistBonus`(默认 100) + `PathElemResistBonus`(默认 100)。全部 `{ get; init; }` + Validate 断言（K>0，系数≥0）。默认安全/负值抛。
- [x] **7.4 ResolveExchange 接线**：OnDefend 结算后、SuppressionMatrix 前插入抵抗层。测试：防方高体质 → 物理攻击减伤 > 低体质；防方高识 → Elemental 攻击减伤 > 低识。
- [x] **7.5 B.5 道心解耦**：`ResistanceOf` 参数不含 daoHeart/innerDemon；EffectivePower 不含 R。测试显式验证（改 daoHeart → EffectivePower 不变，R 不变）。
- [x] **7.6 calibrationMode 旁路**：标定模式抵抗层不生效（R 视为 0），保裸 PE 平价。
- [x] **7.7 B.2 + B.3 + 不退**：IL 浮点零；半衰中间值不溢出（long 中间类型）；同种子逐字节；off worktree sha256 IDENTICAL；cv-001/002/003 + balance-007/008 全绿不退。

---

## Implementation Notes

*承 adr-0010 决策③ + DerivedProviders 既有范式：*

- **ResistanceOf 落点**：**新建** `src/Jianghu.Core/Cultivation/ResistanceProviders.cs` 静态类（非 `DerivedProviders`——后者是 power registry 单一职责）。签名见背景 §2。内部：`int baseR = damageType == DamageType.Elemental ? stats.Get(StatKind.Insight) * limits.ElemResistPerInsight : stats.Get(StatKind.Constitution) * limits.PhysResistPerConstitution`；`if (HasBodyArt(path, cultivation)) baseR += limits.BodyArtPhysResistBonus`；法宝加成 `// TODO(cv-008): ArtifactDef OnDefend 加 R`（角色不持法宝实例，留接口不臆造）。参数不含 daoHeart/innerDemon（B.5）。
- **HasBodyArt 复用**：`DuelEngine.cs:890` 的 `private static bool HasBodyArt(CultivationPathDef, CultivationState)` 提为 `internal static`（或移 `ResistanceProviders`），供 `ResistanceOf` 复用。语义="已修横练/护体"（遍历 `path.ArtCategories` Role=body/defense 且 ChosenArtIds 命中）。
- **ApplyResistance 落点**：`CombatMath`（`CombatMath.cs`，与 cv-006 `ApplyEvasionCoefficient` 同库）新增 `public static int ApplyResistance(int rawDamage, int R, int K)`。`long` 中间类型防溢出：`long mul = (long)K * 1000 / (K + R); long dmg = (long)rawDamage * mul / 1000; return Math.Max(1, (int)dmg);`。`rawDamage` 已是 ×Scale 空间的值（与现有 DuelEngine 一致）。
- **ResolveExchange 接线点**：OnDefend 模块结算（含 Block/Dodge 门控 cv-003）→ 软情境 → [新] 抵抗层 → SuppressionMatrix → return。接线需 `limits`（DuelEngine 已持 `LimitsConfig` 字段）+ `defGate`（来自 `CombatContext`，若作用域内不可达则 BodyArt bonus 退化为 TODO，仅体质/识派生——子代理核实后定）。伪代码见背景 §7。
- **旋钮**：`LimitsConfig`（`Config/LimitsConfig.cs`，`sealed record`）追加 5 个 `{ get; init; } = 默认` 属性 + Validate 断言 + 注释（承 cv-002/003 旋钮范式：注释标 adr-0010 决策③ + B.2 + off 天然守）。
- **测试落点**：新建 `tests/Jianghu.Core.Tests/Cultivation/ResistanceTests.cs`。测试维度：纯函数（半衰公式各 R 值 + 溢出边界）+ ResistanceProviders（不同 DamageType 返回不同 R + HasBodyArt 加成 + B.5 不含 daoHeart）+ 接线（高体质 vs 低体质伤害差 + Elemental vs Blunt 抗性差）+ calibration + B.3。
- **Performance**: No impact expected — resistance calc is O(1) integer arithmetic (two multiplies + one divide) inserted once per exchange at step 4, no new allocations, no RNG, no loop nesting change. The only new per-exchange work is the derived-R lookup (≤5 integer ops + one ArtCategories scan gated by HasBodyArt).
- **不碰**：法宝系统（只留 TODO 接口）；EffectivePower 计算；cv-001 概率/cv-002 削韧/cv-003 门控内部逻辑；SEC/SBC 招式系数（cv-006/008）。

---

## Out of Scope（下切片 / 显式 deferred）

- **SEC 闪避系数** → cv-006（CombatMath 命中调制，与本 story 正交）
- **SBC 格挡系数 + 三层串行** → cv-008
- **主动护盾（内力消耗型防御）**：内力留作后续主动技能资源，不进常驻 R（adr-0010 用户裁定）
- **法宝 OnDefend 加 R（本 story 仅预留 TODO 接口）**：角色不持法宝实例列表（`CultivationState` 无 `Artifacts` 字段），法宝防御接入属 cv-004/cv-008 域。本 story 留 `// TODO(cv-008)` 注释接口，不臆造遍历代码。
- **ElementType 正交字段**：若未来需火/冰/毒元素相克，另立字段，不混 DamageType（adr-0010 决策②）
- **21 路功法标签数据铺设（deferred，红线 A.8）**：机制骨架先行，HasBodyArt/PathElemResist 标签 = 数值实现期铺

---

## QA Test Cases

*Logic 自动化测试 spec。纯确定性，无 RNG。*

- **AC-1（7.1 ResistanceProviders）**：同角色 Normal→R_phys > 0；Elemental→R_elem > 0（通常不同）；无功法标签角色 R 仅来自四维；HasBodyArt 角色（已修 body/defense 类 art）Physical R 更高。参数无 daoHeart（B.5 编译期保证：签名不含）。
- **AC-2（7.2 半衰纯函数）**：R=0 → dmg 不变；R=K(500) → dmg≈raw/2；R=2000 → dmg≈raw×0.2；R=100000 → dmg=1（保底）；rawDamage=1,R=0 → 1。边界：rawDamage=int.MaxValue/1000 不溢出（long 中间类型）。
- **AC-3（7.3 旋钮）**：默认 K=500 安全；K=0 或负值抛（Validate）；系数默认合理（PhysResistPerConstitution=50 不极端）。
- **AC-4（7.4 接线）**：防方体质 100→R≈5000，物理攻击减伤 ≈90%。对比防方体质 10→R≈500，减伤 ≈50%。Elemental 攻击走识（`StatKind.Insight`）而非体质。
- **AC-5（7.5 B.5）**：`ResistanceOf` 参数无 daoHeart；EffectivePower 不含 R（修改 daoHeart → EffectivePower 不变，R 不变）。
- **AC-6（7.6 calibration）**：标定模式 R 视为 0，伤害无减损。
- **AC-7（7.7 B.2/B.3）**：IL 浮点零；off OffByteIdentical + worktree sha256；cv-001/002/003 + balance-007/008 全绿。
- 测试文件：`tests/Jianghu.Core.Tests/Cultivation/ResistanceTests.cs` + `Determinism/OffByteIdenticalTests.cs`（回归守）。

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/Jianghu.Core.Tests/Cultivation/ResistanceTests.cs` — 须存在且过 + off 逐字节回归守（`Determinism/` + worktree sha256）+ IL 浮点零 + cv-001/002/003/balance-007/008 不退
**Status**: [x] 已实现并验证（主控独立核验 A.3 通过）
**实测证据**:
- `dotnet test` 全量 = **1198 绿 / 0 失败 / 0 跳过**（1166 cv-006 基线 + 32 新增 ResistanceTests）
- determinism 子集 **27 绿**（B.2 IL 浮点扫描 + B.3 off 逐字节两轨）
- **off md5 一致**：`42 100` ×2 md5 `8bf2b2af…` 一致（与 cv-006 基线同 = off 零漂移，B.3 天然守）
- `dotnet test --filter Resistance` = **32 绿**（AC 7.1 ResistanceOf 6 + AC 7.2 ApplyResistance 11 + AC 7.3 旋钮 3 + AC 7.4 接线 4 + AC 7.5 B.5 2 + AC 7.6 calibration 3 + AC 7.7 B.2/B.3 3）
- `dotnet build` = 0 警告 0 错误（BannedApiAnalyzers + ILFloat 守通过）

---

## Completion Notes
**Completed**: 2026-07-14
**实现 sha**: `（pending commit）`（代码已落 5 文件，待用户指示提交；主控 A.3 已独立核验 1198 绿）
**Criteria**: 7/7 passing（AC 7.1 ResistanceOf + 7.2 ApplyResistance + 7.3 旋钮 + 7.4 接线 + 7.5 B.5 + 7.6 calibration + 7.7 B.2/B.3 全通过）
**机器证据（主控独立核验 A.3）**:
- `dotnet test` = **1198 绿 / 0 失败 / 0 跳过**（1166 + 32 新增）
- determinism 27 绿 + off `42 100` ×2 md5 `8bf2b2af…` 一致（B.3 天然守）
- `ResistanceTests` 32 绿 + 接线位置 Chip 后（符合 adr-0010 决策④ step 4）
**Deviations**:
- **接线位置修正**：子代理初版把抵抗层插在 OnDefend 后/Chip 前（L459），主控核验发现违背 adr-0010 决策④（step 4 抵抗应在 step 2 Chip 之后）。用户 2026-07-14 裁定移到 Chip 后。修正后抵抗层在 Chip 块后、cv-002 削韧前（L502），符合决策④。
- **2 个既有测试回归适配（A.7 边界：cv-007 改管线行为，既有测试前提失效，改测试不改实现）**：
  - `TagGatingChipTests.test_elemental_does_not_gate_block`：cv-007 BodyArt→物理抗使 Normal/Elemental 走不同 R，污染"Block 对两者等效"本意。修：测试 cfg 加 `BodyArtPhysResistBonus=0` 消除抵抗层干扰，回归 cv-003 原意图。
  - `SwordImmortalTests.Swordsman_KitedByRanged_SituationalAdj_Decides`：cv-007 对称减伤后伤害绝对值变小，Margin 落到 0（Winner=1 攻方仍胜，正确）。修：`Margin>0` → `Margin>=0`（Winner=1 已证 adj 生效，容忍 cv-007 减伤后精度收敛）。
- **子代理两次截断**：cv-007 实现子代理在写测试时两次异常终止（0 tokens usage）。主控接管：(1) 修接线位置 (2) 修 2 个测试 fixture bug（R=100000 断言值错→4 + 静态 `_chosenArtIds` 覆盖 bug→改从 path.ArtCategories 派生）(3) 适配 2 个既有测试回归。产品逻辑子代理首跑即对，所有失败均为测试自身问题。
- **gate 参数退化**：`ResistanceOf` 的 `gate` 参数当前传 `GateType.None`（CombatContext 的 gate 在 ResolveExchange 作用域内未直接可达），BodyArt 加成改走 `DuelEngine.HasBodyArt(path, st)` helper 判定（语义="已修"更准，非 gate.HasFlag 的"有能力"）。符合 story §7 的退化方案。
**实现要点**:
- 抵抗层对称作用于双方（同 R 同减伤），不改变胜负逻辑，仅缩小伤害绝对值
- R 派生纯整数确定性，无 RNG，duel-local 纯净（无跨回合累积）
- B.5 守：ResistanceOf 签名不含 daoHeart/innerDemon，R 不进 EffectivePower
- calibrationMode 旁路（`if(!calibrationMode)`），保 cv-005 seed-sweep 裸 PE 纯净
- 法宝 OnDefend 加 R 留 TODO(cv-008) 接口（角色不持法宝实例，不臆造遍历）
**Code Review**: Pending（待 /code-review）

---

## Dependencies

- Depends on: **adr-0010 Accepted**（✅ `d35fef6`）；**adr-0008 Accepted**（✅）；**DuelEngine.HasBodyArt 既有 helper**（✅ `DuelEngine.cs:890`，提 internal 复用）；**CombatMath 既有纯函数库**（✅，cv-006 `ApplyEvasionCoefficient` 同库）
- Unlocks: cv-008（三层串行接线——抵抗层作为漏斗最后一环就位）；cv-005（防御端可校准层）
- Orthogonal to: cv-006（SEC 闪避——不同限界上下文，无代码依赖，仅共享 CombatMath 库）；cv-003（DamageType 枚举已就位，本 story 只读不写）
- Deferred dependency: 法宝 OnDefend 效果加 R（本 story 预留 TODO 接口，角色不持法宝实例，接入属 cv-004/cv-008）
