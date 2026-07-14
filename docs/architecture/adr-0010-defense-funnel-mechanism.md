# ADR-0010: 三层防御漏斗机制（Evasion → Block → Resistance）

- **Status**: **Accepted**（2026-07-08 用户批准 —— 4 项决策细节裁定回写完毕：SEC=0 显式分支 / SBC 调 Chip 穿透 / 抗性 R 派生映射 / 格挡确定性。规范先行闭合，可进实现拆解）
- **Date**: 2026-07-08（用户交付"三层防御漏斗 + 招式系数"指令 → 主控指出与 cv-001/002/003 三处冲突 → 用户裁定方案3"先修 ADR 再实现" + 三条调和原则）
- **Deciders**: huangjiaqi13（机制设计 + 冲突调和裁定）+ Claude（架构落地 + cv-001/002/003 兼容化 + B.2 整数化）
- **Affects**: `Jianghu.Cultivation`（`DuelEngine.ResolveExchange` 判定管线细化；`CombatMath` 概率合流；`DerivedProviders` 派生抗性）/ `Jianghu.Config`（`LimitsConfig` 新增 SEC/SBC 默认 + 半衰常数 K + Chip 千分比复用）/ `Jianghu.Core.Tests`（`DefenseFunnelTests` + cv-001/002/003 回归守）

> **本 ADR 只立防御漏斗的数学模型与判定顺序，调和它与 cv-001/002/003 的关系，不写实现代码、不拆 story。** 引入新判定层是结构性变更，必须先锚定"如何合流既有概率/门控/Chip 而不破坏 1147 全绿"，封闭架构后再进实现拆解（承核心执行原则"规范先行" / 红线 A.10）。

---

## Summary

在 cv-001（Margin→概率主轴）已就位的基础上，将防御端**细化为三层串行漏斗**：

```
交锋伤害 → ①闪避(Evasion) → ②格挡(Block/Chip) → ③抵抗(Resistance) → 实际扣血
```

**核心调和原则（用户 2026-07-08 裁定，本 ADR 灵魂）**：三层漏斗**不引入平行的第二套随机判定**——**SEC（闪避系数）合流进 cv-001 命中那一次掷骰**（空间维度概率博弈），**SBC（格挡系数）调制第②层确定性格挡的 Chip 穿透比例**（动能维度确定性对撞，不掷骰），维持"单次交锋、单次核心掷骰"的纯洁性；全面沿用 cv-003 已落盘的 `DamageType` 枚举与 Chip 穿透模型；抗性 R 为**派生属性**（体质→物理抗/识→法术抗/功法标签/法宝映射，内力留作后续主动护盾不进常驻抗），绝不为 `StatBlock` 挂无根字段。

**动机**：cv-005 勘探（2026-07-08 实跑数据）证明"仅注入 cv-001 对称 miss-gate 不能复活 balance-006 判死的 [40,60]% 硬闸门"——因为真实 21 路同 UT 胜负由**模块非对称性**（DoT 绕过 miss-gate、压制矩阵、反伤、削韧）主宰，而非 PE 竞速。三层防御漏斗给防御端引入**结构化的、可被招式系数调制的**判定层，是把"胜负决定权从模块非对称拉回可校准的判定管线"的架构手段——为 cv-005 复活硬闸门**奠定基础**（注：奠定基础 ≠ 一步复活；完整复活仍需模块中性化 + 复利抑制 + 伤害带方差，见 §7 遗留）。

**三条地基一字不改**：整数确定性（ADR-0001 / B.2，半衰公式整数化）、off 逐字节（ADR-0003 / B.3，off 不调 DuelEngine 天然守）、模块工厂（ADR-0002 / B.9，防御系数不裸造 EffectOp）。

---

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | 当前 .NET 8 (CLI headless) / `Jianghu.Core` = netstandard2.1；后期 Godot 4.x .NET 消费判定钩子（ADR-0004） |
| **Domain** | Core 结算（Model）：三层漏斗全整数确定性判定 + 派生抗性。View（Godot）：闪避/格挡的玩家 QTE 帧窗（承 cv-004 契约，本 ADR 只定 NPC 侧确定性判定） |
| **Knowledge Risk** | LOW —— 纯整数算法 + 复用既有 duelRng（cv-001）+ 派生属性（DerivedProviders 已有先例），无 post-cutoff API |
| **Post-Cutoff APIs Used** | None（本 ADR 不写代码） |
| **Verification Required** | 实现期：① SEC/SBC 合流后 cv-001 同种子逐字节复现（B.2）② 半衰公式 IL 浮点零（ILFloatScanner）③ off 逐字节不退（B.3 worktree sha256）④ cv-001/002/003 + balance-007/008 全绿不退（1147 基线）⑤ 派生抗性不进 EffectivePower（B.5，仅防御结算用） |

---

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0008（方差主轴 α + duelRng 流；本 ADR 的 SEC/SBC 合流进 cv-001 概率判定）；ADR-0001（整数确定性，半衰公式禁浮点）；ADR-0002（Modules 工厂，防御系数经工厂不裸造）；ADR-0003（off 逐字节，防御漏斗 duel-local 不入 Clone）；ADR-0004（Model/View 边界，闪避/格挡玩家 QTE 帧窗属 View） |
| **Enables** | cv-005 [40,60]% 硬闸门复活的**基础**（防御端可校准判定层）；招式差异化（SEC/SBC 让"必中招/易闪招"成数据） |
| **Blocks** | 防御漏斗实现 story（本 ADR Accepted 后拆）；cv-005 seed-sweep（需漏斗就位后才有可校准的胜率分布） |
| **Relationship to cv-001/002/003** | **叠加细化，非取代**（用户 2026-07-08 裁定）：cv-001 概率主轴 = 漏斗第①层的判定引擎；cv-002 削韧 = 漏斗后置副轴（不变）；cv-003 门控 + Chip = 漏斗第②层的既有实现（保留，本 ADR 复用其 DamageType 与 Chip 模型）。 |

---

## Context

### 触发：用户"三层防御漏斗"指令与既有架构的三处冲突

用户 2026-07-08 交付一份《三层防御漏斗 + 招式系数》实现指令。主控核验后指出它与刚验收入 master 的 cv-001/002/003（`b249ebb`）有**三处硬冲突**，若强行实现会"推土机式破坏"已收敛的架构：

1. **双重掷骰**：指令的闪避层 `duelRng.Next(1000) < FinalEvasion` 与 cv-001 的命中伯努利 `roll >= p → miss` 是**两套平行随机判定**。
2. **DamageType 语义分裂**：指令新增 `DamageType{物理,毒,火,冰}`（抵抗语义）与 cv-003 已落盘的 `DamageType{Normal,Blunt,Elemental}`（门控语义）冲突。
3. **无根抗性字段**：指令的第三层抵抗公式需"抗性分值 R"，但 `Character.StatBlock` 只有力/内/体/识四维，无抗性维度。
4. **Chip 模型冲突**：指令的 `dmg×300‰` 乘法穿透模型 与 cv-003 已实现的 `max(dmg, base×300‰+margin)` 保底穿透模型冲突。

用户裁定**方案3（先修 ADR 再实现）** + 三条调和原则（见 Decision）。

### cv-005 勘探背景（2026-07-08 实跑数据，本 ADR 的必要性证据）

旗舰档勘探实跑数据证明：仅注入 cv-001 方差 → C1 gate 违规 34→37（**更差**）。根因：真实 21 路同 UT 胜负由**模块非对称**主宰（DoT 经 TickDots 绕过 miss-gate；lei_xiu vs gui 近等 PE 差 1% 却打成剩 259 HP=22%PE 碾压）。cv-001 的**对称 miss-gate** 只把某次直接招式伤害归零，两侧对称，碰不到模块结构优势。**三层防御漏斗是把胜负决定权从"模块非对称"拉回"可被招式系数/抗性调制的判定管线"的架构手段。**

### 约束（不可破）

- **B.2 整数确定性**：半衰公式 `K×1000/(K+R)` 全整数（禁浮点，禁 `Math.Round`）；同种子逐字节。
- **B.3 off 逐字节**：off 走 legacy SparAction 不入 DuelEngine，漏斗判定 duel-local。
- **B.5 道心解耦**：派生抗性 R 禁引用 daoHeart/innerDemon（仅防御结算用，不进 EffectivePower）。
- **B.9 模块工厂**：SEC/SBC 若作招式字段属数据（同 cv-003 DamageType，非 EffectOp 算子）；若作效果算子须经 Modules 工厂。
- **单次交锋单次核心判定**（用户裁定灵魂）：绝不平行两套 `duelRng.Next`。

---

## Decision

**确立三层防御漏斗（Evasion → Block → Resistance）为 `ResolveExchange` 防御端的判定组织，招式系数与派生抗性合流进 cv-001 单次核心判定，全面复用 cv-003 语义与 Chip 模型。**

### ① 概率管线合并（解决双重掷骰 —— 用户原则1）

**绝不平行两套随机判定。** SEC（招式闪避系数）**不触发新 `duelRng.Next(1000)`**，而作为**输入参数合流进 cv-001 的 `CombatMath` 查表**（SBC 归第②层格挡，见 ②）：

- **物理语义（用户 2026-07-08 裁定）**：闪避 = "空间维度的概率博弈"（打没打中）→ 归入 cv-001 命中那一次掷骰；格挡 = "动能维度的确定性对撞"（防住多少）→ 确定性，不掷骰。底层不狂抛骰子，上层动作反馈（Godot QTE）才不乱。
- **合流方式**：SEC 视为对 cv-001 `p_permille` 的**整数修正**。基准 `1000`（=中性，permille 不变）；SEC=0 → 攻击必中（必中标签）；SEC>1000 → 易被闪避（命中衰减）。
  ```csharp
  // 合流（整数，B.2）：cv-001 基础命中 p = CombatMath.GetSuccessPermille(margin, defPe)
  // SEC 修正 —— 显式分支拦截 SEC=0（用户裁定：抛弃 max(1,SEC) 数学小聪明，避免除零 Code Smell）：
  if (SEC == 0) return MaxPermille;      // 必中标签，直接赋最高命中概率（无法闪避）
  int p_afterEvasion = (p * 1000) / SEC; // SEC=1000 中性；SEC>1000 命中正常衰减，语义清晰且无除零风险
  ```
  > **实现注（cv-006 @ c57d365, 2026-07-14）**：上记伪代码 `return MaxPermille` 实现时改为返回新常量 `AutoHitPermille = 1000`（非 `MaxPermille = 999`）。原因：cv-001 伯努利判定为 `roll < p`（`roll = NextInt(1000)` ∈ [0,999]），若 SEC==0 返回 999（MaxPermille），则 `roll < 999` 留有 0.1% 失败概率（`roll==999` 时 `999<999` 为假→miss），与 AC 6.2「SEC==0 → 1000」及"必中/无法闪避"语义矛盾。`AutoHitPermille = 1000` 使 `roll < 1000` 恒真 → 真·必中。被 adr-0008 ⑨.2「permille≥1000 = 不可闪避」背书。`MaxPermille = 999` 未改（属 cv-001 byte-identical 基线，AC 6.4 / G.2 守）。用户方案 A 批准。详见 cv-006 Completion Notes Deviations 段。
- **维持"单次交锋单次核心判定"**：命中/闪避在 cv-001 已有的那**一次**伯努利判定内解决（SEC 调制 permille），不新增掷骰。格挡（第②层）**确定性**，不掷骰。
- **落点**：`CombatMath` 增合流函数（如 `ApplyEvasionCoefficient(p, sec)`，内含 SEC==0 显式分支）；`ResolveExchange` 的 cv-001 gate 处传入攻方招式 SEC。

### ② 统一语义枚举 + Chip 模型（保留 cv-003 成果 —— 用户原则2）

- **废弃指令新枚举**：全面沿用 cv-003 已落盘的 `DamageType{Normal, Blunt, Elemental}`（门控语义）。**不引入平行的物理/毒/火/冰分类**（避免状态机认知分裂）。元素/属性区分若未来需要，另立正交字段（如 `ElementType`），不混入 DamageType。
- **统一 Chip 穿透公式**：保留 cv-003 已实现的 `ChipDamageFloor` = `max(dmg, base×ChipPermille/1000 + MarginAdj)`**保底防线模型**。**废弃指令的 `dmg×300‰` 纯乘法模型**（那会让格挡后伤害恒为原 30%，与 cv-003 的"保底穿透"语义不同且撞已验收测试）。新架构**向已验收的 1147 全绿低头**。
- **SBC（招式格挡系数）新语义 = Chip 穿透比例的调制乘子（用户 2026-07-08 裁定）**：格挡确定性对撞，SBC 不再是掷骰阈值，而是**调制穿透比例**——SBC 越低（如重锤/钝器），穿透防线的 Chip Damage 比例越高（防不住的动能越多）。基准 SBC=1000 → 用 LimitsConfig 基准 ChipPermille；SBC 越低 → 有效 ChipPermille 越高。合流示例（整数）：`effChipPermille = ChipDamagePermille × 1000 / max(1, SBC)`（SBC=1000 中性；SBC<1000 抬穿透；SBC=0 归第②层"不可格挡穿透"处理，比照 cv-003 门控）。这让 SBC 拥有深度策略价值（重锤破防 vs 轻兵器可被格挡），且复用 cv-003 保底模型。
- **第②层格挡 = cv-003 门控 + Chip 的既有实现（确定性）**：Blunt→关 Block 类（招架崩坏）；Elemental→关 Dodge 类 + Block 成功则 Chip 穿透（免削韧）。本 ADR 不改 cv-003 第②层核心逻辑，只把它**纳入漏斗第②层的位置**并让 SBC 调制其 Chip 穿透比例。

### ③ 抵抗层 + 派生抗性 R（用户原则3）

- **第三层抵抗（半衰模型，纯整数 B.2）**：
  ```
  DamageMultiplier = K × 1000 / (K + R)        // K=半衰常数(LimitsConfig，如 500)；R=派生抗性分值
  FinalDamage      = max(1, RawDamage × DamageMultiplier / 1000)   // 向下取整；R 越大伤害趋近但不低于 1
  ```
  R=0 → multiplier=1000（无减伤）；R=K → multiplier=500（半衰）；R→∞ → multiplier→0（趋 1 保底）。
- **抗性 R 必须是派生属性（Derived Stat），绝不为 StatBlock 挂无根字段**（用户原则3铁律）。R 由现有维度映射，数据链可追溯（用户 2026-07-08 裁定映射方向）：
  - **体质（Constitution）→ 物理抗性**：抵御钝击、锐器切割（横练护体直觉）。
  - **识/悟性（Insight）→ 属性/法术抗性**：抵御冰火毒等元素侵蚀，符合"神识御法"修仙设定。
  - **内力（Internal）不参与常驻被动抗性**（用户裁定：保持维度清晰）——内力留作**后续主动开启护盾的蓝条资源**（主动消耗型防御，非常驻 R），不进本 ADR 的抵抗层。
  - **功法标签**：`HasBodyArt`（横练/护体）→ 抬物理抗；对应门类功法 → 抬属性抗。
  - **法宝**：防御法宝 OnDefend 效果 → 加 R（经既有 ArtifactDef.Effects）。
  - **映射系数外置 LimitsConfig**（如 `PhysResistPerConstitution` / `ElemResistPerInsight`，整数标定，A.10）。
  - **落点**：`DerivedProviders`（已有派生属性先例）增 `ResistanceOf(cultivation, stats, path, damageType)` → 按 DamageType 分物理/属性抗，纯整数派生，**不进 EffectivePower**（B.5：与 daoHeart 一样，抗性只作防御结算，不算进战力）。

### ④ 判定顺序与既有机制的位置（裁定优先级，承 cv-003 ⑩.4）

`ResolveExchange` 防御端串行顺序（细化 adr-0008 决策③管线）：
```
1. cv-001 命中判定（含 SEC 合流）→ 未命中则伤害归零，短路（不进②③）
2. cv-003 门控 + 格挡（含 SBC 调制）→ Blunt 关 Block / Elemental 关 Dodge + Chip 穿透
3. 既有 OnDefend 模块结算（FlatDR/Evade/Reflect/PostMul/SoulSplit，cv-003 门控后的存活者）
4. 【新】第三层抵抗：RawDamage × DamageMultiplier(R) → FinalDamage
5. 既有后置：SuppressionMatrix / 软情境 / cv-002 削韧派生（不变）
```
- **反伤/吸血等后置结算不受破坏**（用户指令交付标准要求；第③层抵抗在软情境前、反伤收集不变）。
- **calibrationMode 旁路**：三层漏斗的 SEC/SBC 调制 + 抵抗层在标定模式旁路（承 cv-001/002/003 一致，保 cv-005 seed-sweep 裸 PE 纯净）。

---

## Consequences

### 正面
- **消双重掷骰**：SEC/SBC 合流 cv-001 单次判定，随机判定唯一，确定性可复现（B.2）。
- **保 cv-003 成果**：DamageType/Chip 复用，1147 全绿不退。
- **抗性可追溯**：R 派生化，无无根字段，数据链清晰（B.5 守）。
- **招式差异化**：SEC/SBC 让"必中暗器""易闪身法"成招式数据（21 路 deferred 铺）。
- **为 cv-005 奠基**：防御端可校准判定层，胜负从模块非对称部分拉回管线。

### 负面/风险
- **改 ResolveExchange 判定管线**：触已验收 cv-001/002/003 时序，须严格回归（1147 全绿 + worktree sha256 + IL 浮点零）。旗舰档实现 + 主控独立核验（B.7/A.3）。
- **SEC/SBC 合流数学需标定**：合流系数对胜率的影响需 seed-sweep 验证（属 cv-005）。
- **抵抗层半衰整数舍入**：`K×1000/(K+R)` 在 R 极大时趋 0，`max(1,...)` 保底；须测极值（R=0/R=K/R 极大）。
- **奠基≠复活**：本 ADR 不承诺一步复活 [40,60]%——仍需模块中性化 + 复利抑制 + 伤害带方差（见 §遗留）。

---

## 遗留 / 后续（不在本 ADR 范围）

- **cv-005 完整复活 [40,60]%**：本 ADR 提供防御端可校准层，但勘探证明还需 ① calibrationMode 与方差解耦 ② 抑制回合复利（MaxRounds 或伤害带方差）③ 平局 tiebreak 偏置修正。这些是 cv-004/cv-005 的后续工作。
- **伤害带方差（adr-0008 决策③步4）**：当前未实现，属 cv-004。
- **SEC/SBC/R 的 21 路数据铺设**：机制骨架先行，数据 deferred 至数值实现期（承 cv-002/cv-003 范式，红线 A.8 显式登记）。
- **ElementType 正交字段**：若未来需元素相克，另立字段，不混 DamageType。

---

## Alternatives Considered

- **A. 强行执行原指令（平行两套掷骰 + 新枚举 + 无根 R + 乘法 Chip）**：拒 —— 推土机式破坏 cv-001/002/003，双重掷骰破"单次判定"纯洁性，撞 1147 全绿。
- **B. adr-0008-v2 大改**：拒 —— adr-0008 已 Accepted，防御漏斗是叠加机制层，独立 adr-0010 更清晰（annotate-don't-rewrite）。
- **C. 本 ADR（三层漏斗合流既有）**：采纳 —— 用户三原则调和，向下兼容，规范先行。
