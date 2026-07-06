# 修炼系统（Cultivation System）

> **Status**: Implemented（A.0/A.1/A.2 Done；A.3 Designed）
> **Layer**: Core
> **Source**: `docs/legacy-specs/specs/2026-06-14-v1.2-A123-收敛对齐设计.md` + A.0 build-spec + A2/A3-FINAL
> **Created**: 2026-06-21（逆向自 legacy specs + 源码）

---

## 1. Overview

修炼系统是江湖涌现模拟的**核心纵深**——21 条修炼路径，每条含独立的资源体系、境界曲线（RealmCurve）、力量公式（PowerEngine）、战斗模块（Modules），以及突破/渡劫/失败/寿元/道心等深度机制。

当前实现状态：A.0 walking-skeleton + A.1 境界层 + A.2（道心/破单调/奇遇/闭关）+ combat-r2 模块化战斗均 Done；A.3（转职/觉醒/双修）= Designed。当前全量基线 1062 绿（A.0 时点曾为 410）。

---

## 2. Player Fantasy

修炼者踏上 21 条路中的一条，经历大小境界突破、渡劫抗天、道心磨砺、奇遇机缘。每条路独特：剑仙一击破百（物攻）、丹修一粒丹改命（炼丹改四维）、毒修蛊虫噬敌（dot）、命修因果逆天（因果债）。修炼不是线性升级——瓶颈、走火入魔、寿元耗尽、被仇家打断都是真实风险。

---

## 3. Detailed Rules

### 3.1 21 条修炼路径

| # | 路名 | 英文标识 | 类型 | UT 带 | 核心机制 |
|---|---|---|---|---|---|
| 1 | 剑仙 | sword_immortal | 战斗路 | 0.7-1.3×基准 | 剑意/剑气/剑域，物攻天花板 |
| 2 | 体修 | body_henglian | 战斗路 | 战斗路带 | 炼体/铁山靠/反震，Con 坦 |
| 3 | 法修 | fa_xiu | 战斗路 | 战斗路带 | 法术/五行，元素克制 |
| 4 | 鬼修 | ghost_yang_hun | 战斗路 | 战斗路带 | 鬼兵召唤/阴气/夺舍 |
| 5 | 魔修 | mo_xiu_xinmo | 战斗路 | 战斗路带 | 心魔/魔功/堕魔风险 |
| 6 | 佛修 | buddhist_golden_body | 战斗路 | 战斗路带 | 金身/佛光/灭阴 |
| 7 | 雷修 | lei_xiu | 战斗路 | 战斗路带 | 雷法/天劫亲和 |
| 8 | 儒修 | ru_xiu_haoran | 战斗路 | 战斗路带 | 浩然正气/克邪/教化 |
| 9 | 妖修 | yao_xiu_huaxing | 战斗路 | 战斗路带 | 化形/兽性/妖丹 |
| 10 | 血修 | xue_xiu_xuesha | 战斗路 | 战斗路带 | 血煞/杀业/天谴 |
| 11 | 毒蛊修 | du_gu_xiu | 战斗路 | 战斗路带 | dot/蛊虫/毒噬主 |
| 12 | 符修 | fu_xiu_fulu | **战斗路** | Fu=12 | 符箓/储备/爆发 |
| 13 | 音修 | yin_xiu_yuedao | 战斗路 | 战斗路带 | 乐道/心境/心魔 |
| 14 | 因果修 | yinguo_faze | 战斗路 | 战斗路带 | 因果债/天谴/逆天 |
| 15 | 御兽 | yu_shou | 战斗路 | 战斗路带 | 御兽/兽潮 |
| 16 | 丹修 | dan_xiu | 辅助路 | Dan≤7 | 炼丹/改四维/交易 |
| 17 | 阵修 | array_formation | 辅助路 | Array≤7 | 阵法/结界/地利 |
| 18 | 器修 | qixiu_artificer | 辅助路 | Qixiu≤10 | 制器/法宝/装备 |
| 19 | 魂修 | soul_divine_sense | 战斗路 | 战斗路带 | 神识/探查/心念 |
| 20 | 命修 | ming_fate_causality | 战斗路 | 战斗路带 | 运势/改命/占卜 |
| 21 | 傀儡师 | kuilei_shi | 战斗路 | 战斗路带 | 傀儡/机心/bandwidth |

### 3.2 境界系统（A.1，已建）

- **UT 锚集** = {0,2,4,6,8,9,10,11,12}（9 大境界）
- **三轴解耦**：flatIndex / (MajorTier, SubLevel) / UnifiedTier 0-12
- **SubLevelCount**：每个 MajorTier 内的子层数，真字段在 RealmCurveDef
- **CanAscend**：可否飞升（武夫=false，修士=true）
- **UT gap ≥ 2 auto-win**：防止重复低效战斗

### 3.3 境界突破与累积（A.0，已建）

- `CultivationGainPerAction = 1` 累 `CultivationPoints`
- `AdvanceCultivation` 推进 RealmIndex
- `PowerEngine.Evaluate(cs)` 算 pe（EffectivePower）
- **道心解耦**（红线 B.5）：daoHeart/innerDemon 只进 TribScore/Phase，严禁进 EffectivePower

### 3.4 A.1 余项（Done — cultivation-a1-rest）

- **10 态流程状态机**：CultivationPhase（idle→breakthrough→tribulation→…）
- **三劫**：天劫/心魔劫/道劫，数据驱动 TribulationDef
- **五失败模式**：走火入魔/卡瓶颈/寿元耗尽/境界跌落/渡劫陨落
- **寿元**：`Age >= Lifespan + lifespanBonus` → 飞升离场
- **飞升**：UT12 圆满 → 转 Ascended 移出 _alive

### 3.5 A.2（Done — cultivation-a2）

- **道心**（daoHeart）：每路独立资源，影响 TribScore/Phase
- **心魔**（innerDemon）：反噬/污染机制
- **破单调**：日课微决策（修炼/历练/闭关/守一）
- **奇遇**：修炼侧最小 storylet 子集
- **闭关**：单点 wake + 折寿 + 收益

### 3.6 A.3（设计未建）

- **转职**：换 PathId（如剑修→剑仙、武夫→修真）
- **觉醒**：同路血脉/体质解锁高阶功法
- **双修**：兼修第二路（slotCap/bandwidth 限）
- **INV-CROSS 标定**：跨路平衡，同 UT 胜率 ∈ [40%,60%]

---

## 4. Formulas

| 公式 | 定义 |
|---|---|
| `pe = PowerEngine.Evaluate(cs)` | 有效战斗力（含 path/境界/resources/derived） |
| `RealmIndex → UT` | `RealmCurve.Lookup(realmIndex)` → UT 0-12 |
| `SubLevel` | `flatIndex - MajorTier前缀和` |
| `daoHeart` | 不进 pe，只进 `TribScore = f(daoHeart, innerDemon, realm)` |
| `lifespanBonus[UT]` | 0=0, 2=+100, 4=+250, 6=+450, 8=+700, 9=+900, 10=+1100, 11=+1200, 12=离场 |
| `DerivedProvider.Compute(cs)` | fleetWeighted/rosterWeighted/ghostSoldierWeighted/guSwarmWeighted |

---

## 5. Edge Cases

- **off 模式**：cultivation=false → 所有修炼逻辑旁路，输出与 v1.0 逐字节一致
- **辅助路战力**：丹/阵/器不含战斗模块时，pe 仍须有定义（锚锁 UT 保证）
- **死路**：无功法修炼的角色无法突破（RuleBrain 不会选 TrainAction）
- **飞升**：UT12 圆满 + CanAscend + 劫过 → 离场，否则陆地神仙封顶老死
- **道心/心魔上溢**：钳制 [0,100]；心魔 ≥ 80 走火入魔，≥ 95 致死
- **突破失败**：TribGate 永久 +ΔP（不会无限重试同难度）

---

## 6. Dependencies

| 依赖 | 方向 |
|---|---|
| 角色系统（Model） | Character 持 CultivationState 侧表 |
| PRNG 系统 | 修炼流 = RngStreamIds.Cultivation (Split 5) |
| 战斗系统 | PathDef 定义可用的 Modules/SpecialModules |
| 动作系统 | TrainAction ON 分支接 AdvanceCultivation |
| 戏剧引擎 | 恩怨/复仇可通过 ModuleResolver.RelationAdjust 触发 |
| 地图系统 | Region.QiDensity 影响修炼效率 |

---

## 7. Tuning Knobs

| 参数 | 默认 | 范围 | 影响 |
|---|---|---|---|
| `CultivationGainPerAction` | 1 | [1,5] | 突破速度 |
| `RealmCurve.Thresholds[realmIndex]` | 路径差异 | 每路可调 | 突破难度 |
| `SubLevelCount` | 路径差异 | [1,9] | 境界细粒度 |
| `LifespanBase` | 600 | [400,1200] | 角色寿命基线 |
| `lifespanBonus[UT]` | 见 §4 | ±50% | 修炼延寿 |
| `daoHeart_init` | 路径差异 | [×2,×4] | 道心初始值 |
| `UT auto-win gap` | 2 | [1,4] | 碾压门槛 |

---

## 8. Acceptance Criteria

- [x] 21 路全入册（PathRegistry.RegisterAll）
- [x] PathAssigner 按概率分配路径
- [x] PowerEngine.Evaluate 返回非零 pe（所有路径）
- [x] RealmCurve 表完整（UT 0-12，含 SubLevelCount/CanAscend）
- [x] Modules 工厂覆盖所有 EffectOp（普通/稀有/唯一）
- [x] DuelEngine.ResolveR2 结算正确（HP=pe/选招/软情境/同时扣血）
- [x] dot/control 完整时序（被控方 skill→null, dmg=0）
- [x] 辅助路 UT 锚锁（Dan≤7/Array≤7/Qixiu≤10/Fu=12）
- [x] off 逐字节一致（38+ 回归测试）
- [x] IL 浮点扫描零（BannedApiAnalyzers 守）
- [x] A.0 里程碑全量测试绿（410 passed，2026-06 时点快照；当前基线 1051 绿）
- [x] A.1 余项：10 态流程 + 三劫 + 寿元/飞升（cultivation-a1-rest Done）
- [x] A.2：道心/心魔/破单调/奇遇/闭关（cultivation-a2 Done，26 story 全实现）
- [ ] A.3：转职/觉醒/双修（cultivation-a3 = Designed，未建）
- [ ] INV-CROSS 标定（同 UT 胜率 [40,60]%）：⚠️ 未兑现——balance-cross 仅 advisory gate[35,65]%（47/48 已知违规），balance-003 硬闸门 Deferred（详见圆桌纪要 §1）
