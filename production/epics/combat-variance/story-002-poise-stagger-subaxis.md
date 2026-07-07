# Story 002: 削韧副轴 — 韧性/硬直/抗性递减（Poise & Stagger）

> **Epic**: combat-variance
> **Status**: Complete
> **Layer**: Core
> **Type**: Logic
> **TR**: TR-BAL-001（同 UT 胜率 [40,60]%——削韧副轴是"读招节奏 + 打断"维度，与 cv-001 概率主轴正交叠加；硬闸门断言仍在 cv-005）
> **Estimate**: 中大 (2.5d)
> **Manifest Version**: 2026-07-03b（`docs/architecture/control-manifest.md`）
> **Last Updated**: 2026-07-07

## Context

**GDD**: `design/gdd/combat-system.md`（实现期同步修订）；深度源/权威 `docs/architecture/adr-0008-variance-reactive-combat-model.md`（决策⑦步7 韧性副轴 + 决策⑧ 硬性规则 + 决策⑩.3 连续格挡递减 + 决策⑩.4 裁定优先级）
**Requirement**: `TR-BAL-001`（削韧副轴提供"霸体/打断"节奏控制维度，接续 balance-007/008 的 CD/DR 纪律，使战斗从"纯血量比拼"升级为"读招 + 节奏"）

**ADR Governing Implementation**: **adr-0008**（primary，决策⑦步7/⑧/⑩.3/⑩.4）· adr-0001（整数确定性 B.2）· adr-0002（Modules 工厂 B.9）· adr-0003（off 逐字节 B.3）
**ADR Decision Summary**: 伤害结算后同步计算独立【削韧值】累加至防守方韧性条；韧性 ≤0 触发硬直（Stagger）→ 强制该方本回合无法行动 + 触发抗性递减（DR）修正后续。削韧状态 **duel-local**（不入 `World.Clone`，比照 balance-007 `ControlLimiterState`）。硬直复用现有 Control 管线（注入 turns=1 stagger 条目）。

**Engine**: .NET 8 / netstandard2.1 | **Risk**: **HIGH**（触 `Jianghu.Cultivation`：禁浮点 IL 扫描 / off 逐字节 / 平衡数值 / 复用 balance-007/008 CD-DR 时序；一处细微 bug 破确定性或平衡且难查。B.7 旗舰档实现 + 主控独立核验 A.3）
**Engine Notes**: 无 post-cutoff API。若走路线 B 显式算子：`EffectOpKind` 追加 `PoiseDamage`（标 `// L1`，append 语义）。`Math.Clamp(int,...)` 整数重载合法。

**Control Manifest Rules (Core 层)**:
- Required: 战斗效果经 Modules 工厂（路线 B 削韧算子 = 1 工厂方法 + `ModuleResolver` 1 分支）；新增状态离散整数；确定性（同种子逐字节）；削韧状态 duel-local。
- Forbidden: 浮点（B.2，削韧/韧性全整数）；裸 `new EffectOp` 七参（B.9）；`daoHeart/innerDemon` 进战力或削韧（B.5）；**Poise/韧性状态挂 `CombatContext`/`CultivationState` 资源**（会进 `World.Clone` → 破 duel-local + 威胁 B.3）。
- Guardrail: 改 DuelEngine 须过 off 逐字节（`Determinism/OffByteIdenticalTests` + worktree sha256）+ IL 浮点零（`ILFloatScanner`）+ balance-007/008 CD-DR 逐字节不退 + cv-001 方差不退 + C2/C3 不退。

---

## 背景（承 adr-0008 决策⑦步7 + 用户 2026-07-07 裁定 + 主控勘探核验）

adr-0008 推演管线第 7 步「韧性副轴」是**纯 Model 侧、确定性可算**的节奏控制维度：伤害结算后同步计算独立削韧值，韧性归零触发硬直清空行动 + DR。这与 cv-001 的概率主轴（管线步骤 2-4）正交——概率决定"这一击是否有效"，削韧决定"连续挨打后能否被打断"。

现状 `DuelEngine` 已有 balance-007/008 的成熟基础设施可直接接续：
- `ControlLimiterState`（`DuelEngine.cs:450`）= duel-local 状态范本（不入 Clone，off 天然安全）。
- `EffectiveControlTurns`/`StoredControlTurns`（`DuelEngine.cs:466/480`）= duration-based DR 纯函数 + turns=1 打断补偿。
- Control 挂载/`IsControlled`/`TickDots` 全套（`DuelEngine.cs:305/579/484`）= 硬直可复用的"无法行动"机制。

本 story 落 adr-0008 削韧副轴最小闭环。

### ⚠️ 关键工程事实（2026-07-07 主控旗舰档勘探已核代码，钉死实现形态）

1. **头号约束 — Poise 必须 duel-local，严禁挂 CombatContext/CultivationState**：证据链 `DuelEngine.cs:87`（CombatContext 直接持 live `attacker.Cultivation` 无 Clone）→ `CombatContext.cs:73`（`ApplyResource` 转发）→ `CultivationState.cs:104/118`（直改持久 `Resources` dict + Clone 深拷）→ `Character.cs:45` → `World.cs:442`（World.Clone 逐角色 Clone）。**一旦 Poise 挂 CombatContext 资源，一场对拍削掉的韧性会永久写进角色状态并克隆传播，且破 off 逐字节。** 故新增独立 `PoiseState`（比照 `ControlLimiterState`），生活于 `ResolveR2` 作用域，非持久字段。

2. **削韧来源 = A+B 混合（用户 2026-07-07 裁定）**：
   - **路线 A（基础削韧，作用所有交锋）**：削韧值从本次交锋**有效伤害派生** = `dmg × PoiseDamageRatioPermille / 1000`（整数，向下取整，承 /1000 定点粒度）。在 `ResolveR2` 扣血邻域（`DuelEngine.cs:167-170` 后）计算并累加。**不加新算子、不动 path 数据**（结算层派生，非 path 里的裸算子，无需过工厂，B.9 不约束派生）。
   - **路线 B（强控标签附加削韧，机制骨架）**：按 B.9 加 `EffectOpKind.PoiseDamage`（`// L1`）+ `Modules.PoiseBreak(...)` 工厂 + `ResolveExchange` 1 挂载分支（比照 Control `:305`）。**cv-002 只建机制通路**（含合成测试路验证算子生效）；**21 路全量削韧数据铺设 = Out-of-Scope，显式 deferred**（红线 A.8）留数值实现期。

3. **硬直 = 复用 Control 管线（用户 2026-07-07 裁定）**：韧性 ≤0 的结算后检查触发 → 向 `pendingControls` 注入一个 `turns=1` 的 stagger `ControlEntry`（key 如 `"stagger"`）→ 天然复用 `IsControlled`（下回合 dmg=0）+ `TickDots`（回合末递减）+ balance-008 `StoredControlTurns`（turns=1 补偿 → 实拒止 1 回合 = 打断语义）+ balance-007 CD/DR（stagger 冷却/递减防连锁硬直锁）。**霸体语义**：韧性 >0 时不注入 stagger，伤害照受但不打断。

4. **DR（抗性递减）语义**：同一防守方连续被硬直 → 硬直后有免疫/递减窗口（防 stagger-lock），比照 `ControlCooldown`/`ControlDRStep`。用 `PoiseState` 内 `(Side)→count/until` 字典按键读写（从不遍历 → 无序不破 B.2）。硬直触发后韧性重置为 `PoiseMax`（新一轮霸体）。

5. **裁定优先级（决策⑩.4，本 story 相关部分）**：cv-002 只涉及"削韧 → 硬直"链；标签强制干预（⑨.1）、溢出截断（⑨.2）、防守帧保底（⑧A/⑩.3）属 cv-003/cv-004。cv-002 的硬直判定发生在**伤害结算后**（管线步 7），不与概率主轴（步 2）冲突：概率决定 dmg 是否生效，削韧从"生效的 dmg"派生。dmg=0（cv-001 未命中/被控）→ 削韧=0（未命中不削韧，语义自洽）。

6. **确定性**：削韧/硬直全整数派生 + duel-local，无 RNG（cv-001 的 duelRng 只管概率主轴，削韧是确定性派生轴）。同种子逐字节；off 不调 DuelEngine 天然守 B.3。

---

## Acceptance Criteria

- [ ] **2.1 PoiseState duel-local**：新增 `private sealed class PoiseState`（比照 `ControlLimiterState`），字段按 (Side) 键读写；在 `ResolveR2` 局部 new、传入 `ResolveExchange`/`TickPoise`，**不入任何持久字段/Clone**。测试证：一场削韧对拍后，双方 `Character.Cultivation.Resources` 无韧性残留；`World.Clone` 后韧性不传播。
- [ ] **2.2 基础削韧派生（路线 A）**：有效伤害 → 削韧值 = `dmg × ratio / 1000`（整数）累加防守方韧性条。dmg=0（未命中/被控）→ 削韧=0。纯函数 `DerivePoiseDamage(dmg, ratioPermille)` 单测直验（单调/钳制/零伤零削）。
- [ ] **2.3 PoiseDamage 算子骨架（路线 B）**：`EffectOpKind.PoiseDamage` + `Modules.PoiseBreak(...)` 工厂 + `ResolveExchange` 挂载分支。合成测试路带 PoiseBreak 算子 → 验额外削韧生效（基础派生之外叠加）。**禁裸 `new EffectOp`**（B.9）。
- [ ] **2.4 硬直触发（复用 Control 管线）**：韧性 ≤0 → 注入 turns=1 stagger ControlEntry → 该方下回合 dmg=0（`IsControlled` 生效）→ 韧性重置 PoiseMax。霸体：韧性 >0 时伤害照受不打断。测试证：高削韧连击 → 触发硬直回合 → margin 因对手停手而跃升。
- [ ] **2.5 抗性递减（DR，防 stagger-lock）**：连续硬直经 `StaggerDRStep`/`StaggerCooldown` 递减（比照 balance-007）→ 免疫窗口内不重复硬直。纯函数 `[Theory]` 阶梯验。
- [ ] **2.6 LimitsConfig 旋钮**：`PoiseMax`/`PoiseDamageRatioPermille`/`StaggerDurationTurns`/`StaggerDRStep`/`StaggerCooldown`（int + init + 内联默认，A.10 外置）。`Validate()` 加 `<0` 断言。旋钮默认安全 / 负值抛 / 零值合法（退化无削韧）三测。
- [ ] **2.7 B.2 + 确定性**：IL 浮点零；同种子两跑逐字节（削韧派生轴无 RNG）。
- [ ] **2.8 B.3 off 逐字节 + balance-007/008 不退**：off 不调 DuelEngine；worktree sha256 base vs work off 配置逐字节 IDENTICAL；balance-007/008 CD-DR 测试 + cv-001 方差测试全绿不退。
- [ ] **2.9 全量绿 + C2/C3 不退**：全量测试绿（贴计数）；UT gap≥2 auto-win 短路保留；cultivation 确定性轨不退。
- [ ] **2.10 calibrationMode 旁路削韧**（code-review 发现补）：标定模式旁路 `TickPoise`（stagger 锁回合=行动经济扰动，与裸 PE 平价正交，同 balance-006 Control/CounterMul/压制旁路一致）。保 cv-005 seed-sweep 标定纯净。测试证：标定模式下 PoiseMax 无影响。

---

## Implementation Notes

*承 adr-0008 决策⑦步7 + 主控勘探地图（2026-07-07）：*

- **PoiseState 落点**：`DuelEngine.cs:450` 邻域新增 `private sealed class PoiseState`。字段建议：`Dictionary<Side,int> PoiseRemaining`（初值 PoiseMax）、`Dictionary<Side,int> StaggerHitCount`（DR 阶梯）、`Dictionary<Side,int> StaggerCooldownUntilRound`（防连锁）。`ResolveR2:100` 邻域 `new PoiseState()` 与 `controlLimiter` 并列。
- **削韧派生 + 硬直落点**：`ResolveR2` 扣血后（`:167-170`）→ `TickDots`（`:204`）→ **新增 `TickPoise(poise, pendingControls, dmgToA, dmgToB, round, limits)` 步骤**（`TickTurnState` `:207` 邻域）。TickPoise：① 双方 dmg 派生削韧累加 ② 韧性≤0 → DR 检查（冷却/递减）→ 未免疫则注入 turns=1 stagger ControlEntry + 重置 PoiseMax + 记 hit/cooldown。
- **PoiseDamage 算子（路线 B）**：`EffectOp.cs:44` 邻域 enum 追加 `PoiseDamage`（`// L1`）；`Modules.cs:70`（Dot 邻域）加 `PoiseBreak` 工厂方法；`ResolveExchange` 算子遍历（`:299` Dot 分支邻域）加 `if (op.Kind == EffectOpKind.PoiseDamage)` 分支挂到 poise（经参数传入 PoiseState 引用或 out 累加量）。
- **纯函数**：`DerivePoiseDamage(int dmg, int ratioPermille)` + `StaggerImmune(int priorStaggers, int drStep)` 做 `public static`（比照 `EffectiveControlTurns`），单测直验。
- **测试落点**：新建 `tests/Jianghu.Core.Tests/Cultivation/PoiseStaggerTests.cs`。照抄 cv-001/balance-007 fixtures（`MakeChar`/`MakePath`/`ListPathSource`，`RealmIndex=1` 避 auto-win）。`RunPoiseDuel(poiseMax, ratio, drStep)` harness。确定性逐标量比对（Result 含 Dictionary 不能整体 Equal）。
- **不碰**：`CombatContext.cs`/`CultivationState.cs`（Poise 不挂其资源——会进 Clone）；`SparAction.cs`（削韧全在 ResolveR2 内闭环）；cv-001 概率主轴逻辑（正交）；off 路径。

---

## Out of Scope（下切片 / 显式 deferred）

- **21 路全量削韧数据铺设（deferred，红线 A.8）**：cv-002 只建 PoiseDamage 算子机制骨架 + 合成测试路；21 路各自的削韧数值 = 数值实现期铺，依赖 cv-005 重标定确定平衡曲线。**不静默移走,此处显式登记依赖。**
- 标签门控（Unblockable_Weapon 关招架 / Undodgeable_Space 关闪避）+ 元素格挡穿透 Chip Damage（cv-003，决策⑨.1/⑩.1）。
- 连续格挡防守帧递减（⑩.3 BlockFrameDrStep）+ 难度溢出 >1000‰（⑨.2）+ Godot 防守帧钩子整数契约 + 裁定优先级链完整落实（cv-004，决策⑧A/⑨.2/⑩.2/⑩.4）。
- Godot 侧硬直动画 / QTE 弹反渲染（godot-host epic）。
- 韧性每回合恢复（PoiseRegenPerTurn）—— 若最小闭环需要"喘息重置"再评估纳入；本切片先做"硬直触发即重置 PoiseMax"的离散语义。

---

## QA Test Cases

*Logic 自动化测试 spec。削韧派生轴无 RNG（确定），无需 mock。*

- **AC-1（2.1 duel-local）**：Given 一场削韧对拍；When 结算完 + `World.Clone`；Then 双方 `Cultivation.Resources` 无韧性键残留 + Clone 后韧性不传播（证不入持久态）。
- **AC-2（2.2 基础派生）**：Given dmg=各档（0/小/大）+ ratio；When `DerivePoiseDamage`；Then 削韧单调 + 零伤零削 + 整数向下取整。Edge：dmg=0（cv-001 未命中）→ 削韧=0。
- **AC-3（2.3 算子骨架）**：Given 合成路带 PoiseBreak 算子；When 结算；Then 削韧 = 基础派生 + 算子附加（叠加生效）。Edge：无算子路 → 仅基础派生。
- **AC-4（2.4 硬直）**：Given 高削韧连击使韧性≤0；When TickPoise；Then 注入 turns=1 stagger → 下回合被打断方 dmg=0 → 韧性重置。霸体：韧性>0 不注入。隔离验：有/无削韧机制 margin 对比。
- **AC-5（2.5 DR）**：Given 连续硬直；When StaggerCooldown/DRStep；Then 免疫窗口内不重复硬直（防 stagger-lock）。`[Theory]` 阶梯。
- **AC-6（2.6 旋钮）**：默认安全 / 负值抛 Validate / 零值合法（ratio=0 → 无削韧退化）。
- **AC-7（2.7/2.8 B.2/B.3）**：IL 浮点零；同种子两跑逐字节；off `OffByteIdentical` + worktree sha256 不退；balance-007/008 + cv-001 测试全绿。
- **AC-8（2.9 C2）**：UT gap≥2 auto-win 短路保留。
- 测试文件：`tests/Jianghu.Core.Tests/Cultivation/PoiseStaggerTests.cs`（削韧派生 + 硬直 + DR 纯函数与对拍）+ `Determinism/OffByteIdenticalTests.cs`（回归守）。

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `tests/Jianghu.Core.Tests/Cultivation/PoiseStaggerTests.cs` — 须存在且过 + off 逐字节回归守（`Determinism/` + worktree sha256）+ IL 浮点零（`ILFloatScanner`）+ balance-007/008/cv-001 不退
**Status**: [ ] 待实现（/dev-story）

---

## Completion Notes
**Completed**: 2026-07-07
**实现 sha**: `1bcd48f`（feat(cultivation): cv-002 削韧副轴 Poise/Stagger）
**Criteria**: 10/10 passing（含 code-review 补的 2.10 calibrationMode 旁路；0 UNTESTED，25 测试全 COVERED）
**机器证据（主控独立核验 A.3）**:
- `dotnet test` = **1127 绿 / 0 失败 / 0 跳过**（1102+25）
- determinism 子集 **27 绿**（B.2 IL 浮点扫描 + B.3 off 逐字节两轨）
- **B.3 worktree sha256 实证**：cv-001 基线（be873ee）vs cv-002 工作树，4 组 off 配置逐字节 **IDENTICAL ×4**（改 DuelEngine 后复验，off 走 legacy 不入 DuelEngine，削韧旋钮零影响）
- on 同种子 md5 一致；对抗式子代理逐回合模拟 + stash 前后实测（旗舰档 B.7）
**Deviations**: None（blocking）。3 项 ADVISORY（code-review INFO，deferred-by-design，在 Out-of-Scope 内）：
1. `PoiseDamageRatioPermille` 无上界断言（病态配置 ratio≫1000+极大伤害理论溢出；默认 1000+PowerCap 不可达）→ cv-005 重标定期补 ≤10000 上界
2. `StaggerResetPoise` 极大 poiseMax 溢出（病态配置，受 MaxRounds+冷却门控逻辑仍自洽无死循环）
3. 21 路削韧数据未铺（PoiseBreak 机制骨架就位，数据 deferred 至数值实现期，依赖 cv-005 平衡曲线）
**已修复（code-review 发现，非遗留）**: calibrationMode 未旁路 TickPoise（stagger=行动经济扰动污染 balance-006 标定纯净）→ 加 `if(!calibrationMode)` 门控 + 补 `test_calibration_mode_bypasses_poise`（AC 2.10）+ 验证生产轨 25% 无漂移。
**balance-004 张力（用户 2026-07-07 裁定）**: 削韧使弱者被硬直→强者无伤→切磋碾压率 24%→25%（245/980=25.0% 整数舍入碰线）。判定=合理设计演化（adr-0008「高阶威压定身」叙事），非底层失控；`SparStompRateTests` 阈值 <25→<27 放宽 + 认知更新注释。削韧强度留 cv-005 seed-sweep + Godot 实机 QTE 锚定（避免过早雕琢）。
**Test Evidence**: Logic — `tests/Jianghu.Core.Tests/Cultivation/PoiseStaggerTests.cs`（25/25 绿）
**Code Review**: Complete（本会话 /code-review = APPROVED；旗舰档 + 对抗式子代理 1126 测试独立跑 + stash 实测，B.7；lean 模式虽跳过 LP gate，实际超额执行）。

---

## Dependencies

- Depends on: **adr-0008 Accepted**（✅）；**cv-001 Done**（`dbd070c`，概率主轴就位 → 削韧从"生效伤害"派生，二者正交）；balance-007/008（CD/DR 基础设施 + Control 管线复用，`9592d41`/`d385cd8`）
- Unlocks: cv-003（标签门控——招架崩坏 = 大量削韧，复用本 story 削韧机制）· cv-004（防守帧递减 ⑩.3 挂 PoiseState 计数）· cv-005（削韧维度进 seed-sweep 胜率模型）
- Deferred dependency: 21 路削韧数据铺设（数值实现期，依赖 cv-005 平衡曲线）
- Blocks: 无（cv-003/cv-004 可在 cv-002 机制就位后并行展开）
