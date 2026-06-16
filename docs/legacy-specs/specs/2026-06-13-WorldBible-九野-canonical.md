# World Bible · 《九野 · 末劫将临》（Canonical）

> 本文是江湖涌现模拟内核的**世界圣经唯一权威版**。它把六节草案合并为一份连贯设定，并逐条消化了批判意见（blocker/major/minor 全部在文内消解或显式取舍）。
>
> **本文是仓库 canonical 文档**：任何后续 writing-plans / implementer / auditor 以本文为准；六节草案降级为历史素材。凡草案与本文冲突，以本文为准；凡本文与源码冲突，以源码为准（关键字段已实读核定，见各节脚注）。
>
> **两条红线贯穿全文**：
> 1. **可程序化**：每条设定都还原为「确定性整数机制 + 随机但有上限（频率/强度/并发）」，无自由文本叙事承载逻辑。
> 2. **不锁死（开闭可扩展）**：加新势力/地区/路线/历史 = 追加数据；本文对「真·零改核心」与「需加枚举/分支的有限扩展」**诚实分级**（见 §9），不对后者谎称零改。premise 只给底色，不写死中心剧情线。
>
> **已锁系统对齐基线（实读源码核定，凡冲突以源码为准）**：
> - 四维 = `StatKind{Force,Internal,Constitution,Insight}`（武力/内力/根骨/悟性，`StatKind.cs:3`、`StatBlock.cs` 强制 4 维）。
> - `StatSum=80 / StatCap=30 / StatMin=5 / StatCount=4 / Concentration=6`（`LimitsConfig.cs:8-12`）。**Σ=80 仅生成期契约**；运行期单维 `Apply` clamp `[0,StatCap]`，不守和（`StatBlock.cs:19-25`）。
> - 三轴解耦 flatIndex/(MajorTier,SubLevel)/UnifiedTier 0–12（A2-FINAL §1）。
> - Pcg32 + Split 派生流（`Pcg32.cs:56-60`，`Split(id)` 跳号派生不消费 root 状态）；RngStreamIds 1–8（1=gen / 2=domain / 3=spawn / 4=brain / 5=cult / 6=drama / 7=map / 8=faction）。
> - 关系 = 有向钳制 `[-100,100]`（`Relations.cs:15`，复用同一 `Adjust` 语义）。
> - 战斗 off 口径 = `Power = Force×2 + Internal + Constitution`、`winner = pa>=pb`、`margin=|pa-pb|`（`SparAction.cs:14-27`）。
> - `DomainEvent(long Tick)` 单源，现有 6 record（`DomainEvent.cs`）。
> - **World 持久化现状（关键，决定确定性可行性）**：仅 `_domainRng`(Split2) 与 `SpawnRng`(Split3) 是 World 字段、进 `Clone` 深拷（`World.cs:27-28,161-165`）；`genRng`(Split1)/`brainRngBase`(Split4) 是 `CreateInitial` 局部变量，消费后丢弃，**不进 Clone**。本文据此约束所有运行期随机流（见 §8.3）。
> - 侧表纪律：新态挂侧表（CultivationState / World.Map / World.Factions / World.Era 等），不污染 v1.0 核心 record（StatBlock/Character/Persona/Sect/WorldNode 字段顺序一字不改）。

---

# 〇 · 关键冲突收敛裁决（先钉死单一真相源）

> 批判意见暴露了六节草案间多处「同一机制两套定义」的硬冲突。本章**先一次性钉死单一真相源**，后续各节只引用、不重定义。这是「单一真相源」纪律的强制落点。

## 〇.1 全局灵气标量 —— 唯一容器 `World.Era.AmbientQi`，量纲 `[0,1000]`

**冲突**：premise 草案 `WorldEra.AmbientQi ∈ [0,1000]=420`（派生 `QiToTierTable[/100]`）vs 历史草案 `EraClock.AmbientQi ∈ [0,100]`（派生 `g()`）—— 容器名、量纲、派生表三处全打架。

**裁决（唯一）**：
- **唯一容器**：`World.Era`（一个 record，名 `EraState`，吸收原 `WorldEra` 与 `EraClock` 两个名字，二者均废止）。
- **唯一量纲**：**全局**灵气基数 `AmbientQi ∈ [0,1000]`（取大量纲，刻意与**局部**区域 `Region.QiDensity ∈ [0,100]` 区分量级，杜绝索引混淆）。
- **唯一软上限派生**：`RealmCapHint => QiToTierTable[AmbientQi / 100]`（整数查表，`QiToTierTable` 是长度 11 的 `int[]`，索引 0..10 → UnifiedTier 软上限）。`g()` 一名废止。
- **全局 vs 局部显式标注**：`AmbientQi`（全局，`World.Era`，`[0,1000]`）≠ `Region.QiDensity`（局部，地图侧表，`[0,100]`）。二者是不同的量，不可互相代入。局部修炼效率读 `Region.QiDensity`；全局可达境界软上限读 `AmbientQi`。

> 消解批判：[major] AmbientQi 双定义 → 收敛为单一字段 `World.Era.AmbientQi[0,1000]` + `QiToTierTable[/100]`，premise 与历史两节均改为引用本裁决，删除各自重复 schema。

## 〇.2 全局张力时钟 —— 唯一标量 `World.Era.EraTension[0,10000]` + 唯一倒计时 `RiftClock`

**冲突**：宇宙观草案 `era_tension ∈ [0,100]` vs 历史草案 `EraTension ∈ [0,10000]` vs premise 草案另起 `RiftClock.Integrity ∈ [0,1000]+Stage[0,3]`——三个标量、三个量纲、两套阈值表，且语义重叠（都是「末劫将临未临」的钟）。

**裁决（二者是两个不同机制，显式分工，消除二义）**：

| 标量 | 唯一容器 | 量纲 | 语义 | 演化方向 | 阈值/事件 |
|---|---|---|---|---|---|
| **`EraTension`** | `World.Era` | `[0, ERA_TENSION_CAP=10000]` | **大势张力**：响应「当世」战争/顶阶拥挤/气运极化的累积压力 | 累积 + 清算后回落（**非单调**） | 跨 `NEXT_CALAMITY_GATE` → 纪元级全局清算（§7.5） |
| **`RiftClock.Integrity`** | `World.Rift` | `[0,1000]`，`Stage[0,3]` | **道枢倒计时**：上一纪元遗留的「镇世道枢」完整度，被侵蚀的单调倒计时进度条 | 单调侵蚀（`Integrity` 单调不增，`Stage` 单调不减） | 跌破 `{500,250,0}` → 三段渐强世界事件（§2.2） |

- **二者关系（显式钉死）**：`EraTension` 是「**软压力表**」（当世态的响应式投影，可涨可落），`RiftClock` 是「**硬倒计时**」（历史遗留的不可逆侵蚀闸）。两者**单向耦合**：`RiftClock` 每跨一阶 → 给 `EraTension` 一次阶跃加成（裂得越深，世道越紧）；`EraTension` 高 → 加速 `RiftClock` 侵蚀（越争越裂）。**正反馈但双方各有硬上下界**，不爆。
- `era_tension ∈ [0,100]`（宇宙观草案）的量纲废止，统一为 `[0,10000]`。
- **唯一阈值表**：`EraTension` 的清算阈值在 `EraConfig.CalamityGates`；`RiftClock` 的裂阈值 `{500,250,0}` 在 `EraConfig.RiftThresholds`。二者写进 `LimitsConfig` 嵌套的单一 `EraConfig` record，其余节只引用。

> 消解批判：[major] EraTension 三处互斥取值域 → 钉死为「一个软压力标量 `EraTension[0,10000]` + 一个硬倒计时 `RiftClock.Integrity[0,1000]`」两个**不同机制**，显式说明关系与归属，宇宙观/历史/premise 三节改为引用。

## 〇.3 气运存储位 —— 势力气运 = `Faction.Fortune` 字段；个体气运 = `CultivationState.Resources["fortune"]`

**冲突**：§F 与历史草案把势力气运作为 `Faction` 实例整数字段（随 `FactionLedger` 走、`Faction.Clone` 带走）；premise 草案另建独立 `FortuneLedger` 类用 `Dictionary` 外挂（个体气运也进该类 `_charFortune`）。两套真相源。

**裁决（采多数方案，删独立类）**：
- **势力气运** = `Faction.Fortune`（`int`，归 `FactionLedger._factions`，随 `Faction.Clone` 深拷）。
- **个体气运** = `CultivationState.Resources["fortune"]`（侧表，**不进 StatBlock 四维**）。
- **删除** premise 草案的独立 `FortuneLedger` 类。其守恒/衰减/CAP 逻辑归 `FactionLedger`（势力侧）与 `CultivationState.ApplyResource` chokepoint（个体侧）。

> 消解批判：[major] Fortune 三章两套存储 → 统一为 `Faction.Fortune` + `CultivationState.Resources["fortune"]`，删除独立 `FortuneLedger`。

## 〇.4 势力间关系量纲 —— 统一 `[-100,100]`

**冲突**：宇宙观草案母题二写「友好值矩阵 `[-1000,1000]`」vs §F/v1.2-C/源码 `Relations.cs:15` 全为 `[-100,100]`。

**裁决**：势力间关系 `_relations` 一律钳制 `[-100,100]`（与角色 `Relations` 同构，复用 `Adjust`）。宇宙观草案 `[-1000,1000]` 改为 `[-100,100]`。所有阈值（`RegimeWarinessThreshold`、`FeudThreshold≤-50`、`AllyThreshold`）按此量纲标定。

> 消解批判：[minor] `[-1000,1000]` 量级错 → 改为 `[-100,100]`。

## 〇.5 纪元命名 —— `EraIndex` 绑宇宙世代；`ReignName` 单独承载朝代年号

**冲突**：premise 草案把 `EraIndex 0/1/2` 命名为仙古/乱古/承熹、`EraName="承熹"`；历史草案命名为 上古·神魔纪/远古·百圣纪/今世·江湖纪。「承熹」是大胤朝年号（朝代纪年），「江湖纪」是宇宙世代（epoch），二者本可分层共存，却都钉到同一字段。

**裁决（分层钉死）**：
- `EraIndex`（`int`）绑**宇宙世代**：`0=神魔纪(上古) / 1=百圣纪(乱古/远古) / 2=江湖纪(今世，开局)`。`EraName` 取世代名（开局 `="江湖纪"`）。
- 新增 `ReignName`（`string`，纯 flavor）承载**今世朝代年号** `="承熹"`（属江湖纪内大胤朝纪年，非世代）。
- 两字段均纯 flavor，仅入 Chronicle，零数值路径。

> 消解批判：[minor] EraIndex 命名打架 → `EraIndex` 绑世代，新增 `ReignName` 承载「承熹」。

## 〇.6 区域级危险层 HazardLayer —— 统一为 Site 级侧表 `World.Map.SiteHazards`

**冲突**：历史草案往 `NodeGeo.HazardLayer{Type,Intensity}`（Site 级）写，称「地图已预留」；舆图草案 `HazardLayer(RegionId,Kind,Intensity)` 是区域级（`GeoCanon.Hazards[]`），且 `GeoRec` 字段表**不含** HazardLayer——引用了不存在的字段，且区域级/Site 级粒度混用。

**裁决（统一 Site 级 + 显式声明字段与可变性）**：
- HazardLayer 统一为 **Site 级**：独立侧表 `World.Map.SiteHazards : Dictionary<int /*NodeId*/, HazardLayer>`（键 NodeId）。`GeoRec`（拓扑只读体）**不含** HazardLayer（保持 `GeoRec` 纯只读可浅拷）。
- `HazardLayer(int Intensity, HazardKind Kind)` —— 不带 RegionId（归属由所在 NodeId 隐含）。
- 区域级灾劫（灭世大劫的余烬）落地时，由 `HistorySeeder` 展开为该区域**若干代表 Site** 的 `SiteHazards` 条目（区域→Site 展开在 gen 期一次性完成）。
- **可变性**：`SiteHazards` 生成期写；运行期可变（纪元清算可增减），故**进 `World.Clone` 深拷清单**（不在 `GeoRec` 浅拷体内）。

> 消解批判：[minor] HazardLayer 粒度/字段悬空 → 统一 Site 级 `World.Map.SiteHazards`（独立可变侧表，进 Clone 深拷），从 `GeoRec` 剥离。

## 〇.7 「零改核心」诚实分级（红线②自洽）

**冲突**：草案多处对「加身份层需加 `IdentityLayerOf` 分支」「加 `TieKind` 仅加 case」也宣称「零改核心」，但往派生函数/switch 加分支就是改核心代码逻辑，与「追加数据零改」口号矛盾。

**裁决（分两级，见 §9 完整表）**：
- **L0 · 纯数据扩展（真·零改核心）**：加势力/地区/历史锚点/情境边/资源 key/纪元锚点 = 追加数据行。
- **L1 · 有限枚举扩展（小改核心，诚实标注）**：加身份层 / `TieKind` / `AlignmentAxis` / `FactionType` 枚举值 + 配套分支或 profile 行 = 加枚举值 + 加表行（引擎读表，但枚举本身是代码改动）。
- **身份层进一步数据化（消除 L1）**：本文把 `IdentityLayerOf` 改为**数据表驱动的 `(谓词→层)` 求值**（见 §3.3），使「加新身份层 = 加表行」真正退回 L0，不再需要加派生分支。

> 消解批判：[major] 红线②自相矛盾 → 诚实分级 L0/L1，且把身份层判定彻底数据化以消除其 L1 例外。

---

# 一 · 宇宙观与主题（Cosmology & Themes）

## 1.1 世界本质：一界两层，灵气连续梯度，无硬墙分界

世界名「**九野**」，是**单一物理平面、双修行层叠加**的连续世界——不采离散位面（下界/中界/上界），而采**一界之内灵气浓度连续梯度**模型。凡俗武林与修真宗门**不在两个隔绝世界，而在同一张地图的不同灵气区**：

- **武侠层（薄灵区）**：中原腹地、朝廷京畿、世家坞堡、市井坊镇——灵气稀薄。众生绝大多数是**凡人**与**武夫**：以人身锻气血、以招式分高下，真气=内力（`Internal` 维 + 武侠路 per-path 资源），不渡天劫、不破虚空、老死即终。金庸式江湖的层。
- **仙侠层（厚灵区）**：名山大川、洞天福地、灵脉秘境、塞外异域——灵气浓郁。容得下**修士**：引气入体、筑基结丹、渡劫飞升。凡人修仙式宗门的层。
- **衔接带（梯度过渡）**：二层不靠位面壁垒隔开，而靠**局部区域灵气浓度 `Region.QiDensity ∈ [0,100]`**（地图侧表，地利三维之一）自然分层。武夫游历入厚灵区可能机缘引气转修士；修士入薄灵区修炼效率被环境压制。**凡俗武林↔修真宗门的衔接 = 同一全谱 Faction 模型下的势力交往 + 同一地图上的地理流动 + 个体跨层转化事件**，非两套并行世界。

**避坑依据**：洪荒流顶层无限延伸→数值通胀失控；飞升=移除实体，跨位面角色破坏单层平衡。本世界用**一界连续梯度 + UnifiedTier 0–12 硬封顶 + 飞升离场（转 `World.Ascended` 档）** 三重防爆：不开第二张需重新平衡的位面图，飞升者直接离场腾位，顶层封死在 UT12（YAGNI 占位，不可达）。

> **关键澄清（局部 vs 全局，承 〇.1）**：分层读**局部** `Region.QiDensity[0,100]`（区域尺度）；全局可达境界软上限读**全局** `World.Era.AmbientQi[0,1000]`（世界尺度）。二者不同量纲、不同容器，不可混淆。

## 1.2 灵气 / 真气 / 天道：三层能量本体，全整数标量，各挂其位

把三种能量**严格区分为不同存储位的整数标量**，各有上下限与再分配规则，杜绝玄学黑箱：

| 能量 | 世界观含义 | 存储位（侧表纪律） | 取值/机制 | 闭环/上限规则 |
|---|---|---|---|---|
| **灵气** | 天地本源，外部环境资源 | 局部 `Region.QiDensity`（地图侧表）/ 全局 `World.Era.AmbientQi`（〇.1） | 局部 `[0,100]` / 全局 `[0,1000]` 整数场 | **可再生有限资源**：灵脉产出→修炼/采集消耗→再生回补；过采枯竭 |
| **真气/内力** | 个体内化的力量 | `Internal` 维 + per-path 资源（`CultivationState.Resources`，**不进 StatBlock**） | 武侠层主用 `Internal`；仙侠层用 per-path 资源（剑意/血气/法力/煞值…） | 经 `ApplyResource` chokepoint 钳 `[Min,Cap]`；走火/雷噬衰减 |
| **天道** | 世界级规则与运势总和 | **拆三个独立整数，不做单一全局变量**（见下） | 见下 | 各有守恒/衰减项，可争夺/转移，非纯增益 |

**「天道」反玄学拆解**（直接回应「天道别做成模糊全局变量」）——三个可计算、可复现、有边界的整数子系统：

```
(a) 法则权限位 LawPermission —— 「合道」的程序化身
    高 UnifiedTier 修士战力来源从「自身资源池」切换为「借天地规则」
    机制：charUnifiedTier >= LAW_BIND_TIER(默认 ≈UT10) → 置 Flags["lawBound"]=1
         PowerEngine 读此 flag 施规则加成（大碾压闸已是雏形）
    避坑：必须有确定性触发阈值 + 可计算战力换算（无阈值合道=不可判定玄学，破确定性）

(b) 纪元张力 EraTension —— 「大势将临」的全局软压力表（详见 §7，归 World.Era）
    标量 [0,10000]，响应当世态累积、清算后回落；跨 CalamityGate 触发全局清算

(b') 道枢倒计时 RiftClock.Integrity —— 历史遗留的硬倒计时（详见 §2.2，归 World.Rift）
    Integrity[0,1000] 单调侵蚀；跌破 {500,250,0} 触发三段世界事件

(c) 气运 Fortune —— 个体/势力的「运数」，可争夺可转移的战略资源
    存储：个体 → CultivationState.Resources["fortune"]；势力 → Faction.Fortune（〇.3）
    守恒铁律：夺取=源减目标增（守恒）；增发（信徒/辖地）必配衰减项（香火随时间蒸发），防通胀
    硬上限 + 反制项：设 FortuneCap，特定敌人/秘境免疫气运压制（防正反馈失控）
```

> **承 〇.2**：(b) `EraTension` 与 (b') `RiftClock` 是**两个不同机制**——前者软压力（当世响应、可涨落），后者硬倒计时（历史侵蚀、不可逆）。二者单向耦合、各自有界。

### 可程序化锚点（§1）
- **分层**：`LayerOf(int qiLocal)` 纯函数读 `Region.QiDensity[0,100]` → 武侠/衔接/仙侠层，阈值 `Q_THIN/Q_RICH` 在 `LimitsConfig`，不存 enum 字段（杜绝双源）。
- **全局软上限**：`World.Era.AmbientQi[0,1000]` → `RealmCapHint = QiToTierTable[AmbientQi/100]`（长度 11 整数表）。
- **天道三拆**：`LawPermission`（`Flags["lawBound"]`）/ `EraTension`（World.Era，[0,10000]）/ `RiftClock.Integrity`（World.Rift，[0,1000]）/ 气运（`Faction.Fortune` + `CultivationState.Resources["fortune"]`）—— 全整数、全有界、各挂其位。
- **加第四种天道子系统** = 追加一个挂侧表的整数标量（L0），不动前述。

---

# 二 · 核心 Premise 与中心张力源（《九野 · 末劫将临》）

> 本章只为每一局**铺底色**：手工编撰 premise + 初始大势 + 张力源，给涌现一个有方向的起点。**不写死任何中心剧情线**——谁称霸、谁飞升、哪条恩怨链点燃、末劫由谁触发，全交 agent 在确定性内核上涌现。

## 2.1 世界 Premise（手写底色，一段话钉死）

**世界名：九野（Jiǔyě）。** 曾由上一纪元至强者以无上修为镇压天地法则、维持灵机充沛。三百余年前，**「玄昊大劫」**爆发——诸路宗师在一场争夺「**镇世道枢**」（天地灵机总闸）的大战中两败俱伤，道枢崩裂、灵机锐减，一个纪元终结。

今日开局，九野处在**「乱古之后、末法将至」**的下行世代 **江湖纪**（`EraIndex=2`），当朝年号 **「承熹」**（`ReignName`，大胤朝纪年）：表面承平，名义共主 **「大胤朝」** 据中原立国、设「**镇玄司**」节制江湖；实则灵机逐年枯竭，老牌道统青黄不接，资源争夺日烈，**镇世道枢的裂痕正在缓慢扩大**。世间流传一则谶语——「**道枢三裂，末劫重临**」。没人知道末劫何时来、由谁引动；但每个人都在为那一天积蓄：有人疯狂修炼求飞升脱劫，有人趁灵机未尽抢占灵脉道统，有人复仇旧怨、有人觊觎神器、有人只想在乱世活下去。

**底色：一个灵机下行、共主衰微、道枢将裂的世代，「将临未临」的末劫是悬在所有人头顶的全局时钟。** 具体由谁、如何敲响这口钟——留给江湖自己写。

> 设计依据：取「纪元链单调下行 + 大劫倒计时」与「资源-冲突振荡器」骨架，但**封死顶层、只设将临的全局阈值事件**，不预写反派、不预写结局。

## 2.2 张力源 B — 道枢裂痕（硬倒计时 `RiftClock`，将临未临）

> 承 〇.2：`RiftClock` 是「历史遗留的单调侵蚀倒计时」，与 `EraTension`（当世软压力）分工。

```
RiftClock（侧表 World.Rift）
  Integrity     : int [0,1000] = 720   // 道枢完整度，承熹开局已裂（非满 1000）；单调不增
  StressPerKTick: int          = 2     // 自然侵蚀（整数，受 §2.4 气运/战争加成，但加成有界）
  Thresholds    : int[]  = { 500, 250, 0 }   // 三道「裂」阈值（EraConfig.RiftThresholds）
  Stage         : int [0,3]   = 0      // 当前已触发裂数（单调不减；0=未裂 … 3=末劫将临态）
```

| Integrity 跌破 | Stage | 世界级后果（全走 DomainEvent，整数效果，**无预写胜负**） |
|---|---|---|
| 500（一裂） | 1 | 全局 `Era.QiDecayPerKTick +1`（加速枯竭）；随机一区灵脉异动（`Region.QiDensity ±整数`）；`EraTension` 阶跃加成；广播 `RiftCracked(stage=1)`，在世角色 Memory 记一条低 valence「天象示警」 |
| 250（二裂） | 2 | 新增「**道枢碎片**」秘境配额（§5，≤MaxSecrets）；势力 `RiftAmbition` 加成（觊觎道枢者野心↑→驱动夺枢战争）；`EraTension` 阶跃；`RiftCracked(stage=2)` |
| 0（三裂） | 3 | **末劫将临态**：开放「问枢」终局事件**槽**（资格判据涌现裁定，**不指定赢家**）；全局天劫 `ThreatPenalty` 阶跃；`RiftCracked(stage=3)` |

- **Stage 3 不是「世界毁灭脚本」，而是解锁一个开放终局槽。** 道枢被谁补全/夺取/引爆、世界走向新纪元还是崩塌，由当时 agent 状态生成。内核只保证「将临」这口钟一定会响，不保证怎么响。
- **侵蚀加成有界**：`StressPerKTick` 受全局战争烈度与夺枢行为加成，但加成累加项 clamp 上限 `RiftStressBonusCap`；`Integrity` 恒 `∈[0,1000]`，绝不爆。

## 2.3 张力源 A — 灵机下行（环境承载力，缓慢单调 + 可局部回升）

全局灵机是**有限可再生资源**，逐世代下行，但允许局部事件回升（避坑「不可纯单调到底→晚期死寂」）。

```
World.Era（EraState record，承 〇.1/〇.5；吸收原 WorldEra/EraClock 两名）
  EraName           : string  = "江湖纪"  // 世代名 flavor（〇.5），仅 Chronicle
  ReignName         : string  = "承熹"    // 今世朝代年号 flavor（〇.5），仅 Chronicle
  EraIndex          : int     = 2         // 宇宙世代序号（0=神魔纪 1=百圣纪 2=江湖纪开局）；上行不可达占位
  AmbientQi         : int [0,1000] = 420  // 全局灵机基数（〇.1），承熹开局显著低于鼎盛 1000
  QiDecayPerKTick   : int     = 3         // 每 1000 逻辑 tick 自然下行（整数，clamp≥QiFloor）
  QiFloor           : int     = 80        // 末法地板（绝灵不归零，留低境窗口）
  EraTension        : int [0,10000] = 0   // 大势软压力表（〇.2、§7）；累积+清算回落
```

- **耦合点（软上限，不硬锁角色）**：`AmbientQi` 经整数查表派生全局 UnifiedTier 软上限 `RealmCapHint`，灌入修炼劫的 `ThreatPenalty`（`TribScore = Σ(src×weight) − ThreatPenalty + Roll`）：灵机越枯，高 UnifiedTier 渡劫越难。**软情境修正，非硬门控**——压制（而非禁止）高境堆积，自然制造「趁灵机未尽冲境」的紧迫。`AmbientQi` 作为「唯一全局软上限旋钮」的特例地位，见 §8.4 显式正名。
- **局部回升**：秘境开启 / 道枢碎片现世可对所在 Region 的 `QiDensity` 施一次性整数增益（地图侧表），制造「新机会窗口」热点，不让后期死寂。
- **确定性**：`AmbientQi`/`EraTension` 推进只读 `World.Clock` 逻辑时间节流，**不消费决策 RNG**；off 时该侧表不构造，v1.0 轨迹逐字节不变。

## 2.4 张力源 C — 气运争夺（势力/角色局部标量，可掠夺、守恒+损耗）

> 承 〇.3：势力气运 = `Faction.Fortune`；个体气运 = `CultivationState.Resources["fortune"]`。**独立 `FortuneLedger` 类已删**。

```
气运（势力侧归 FactionLedger，个体侧归 CultivationState；全整数）
  Faction.Fortune : int [0, FortuneCap=1000]        // 势力气运/国运/底蕴
  CultivationState.Resources["fortune"]              // 角色气运（命修「机缘」轴可初值播种，独立演化）
  Sources(整数，每 K-tick 结算，硬上限优先于随机):
    + 控制 Site 数 × WealthWeight        // 据地利（IGeoQuery 查 Wealth/QiDensity/Strategic）
    + 在世高 UnifiedTier 弟子数 × TierWeight
    + 占有道枢碎片 × RelicWeight（与 §2.2 强耦合）
  Sinks(损耗，防通胀，香火随时间蒸发):
    − Fortune × DecayPct / 100           // 整数蒸发
  Transfer(守恒，夺取):
    战败/夺地/夺碎片 → 源 −Δ, 目标 +Δ   // 守恒转移，clamp 两端
```

- **驱动目标**：高气运 → ① 修炼破障「外力/资源」法的可凑 `BreakAid`；② 命修改命、丹修自给的资源乘子；③ **「问枢」资格判据**（Stage 3 终局槽：`Fortune + UnifiedTier` 综合排名，给争夺一个可量化顶点，但**谁登顶由涌现决定**）。
- **单一稀缺顶点 + N 竞争者**：道枢只有一个，气运/UnifiedTier 最高者获「问枢资格」槽位。槽位竞争天然驱动联盟/背叛/战争——胜者由模拟涌现，非预设。
- **反制项（防单极锁死）**：气运设硬上限 `FortuneCap`；高气运者侵蚀道枢更快（`RiftStressBonus` 加成）→ **强者通吃即加速末劫即引火烧身**，自我抑制的负反馈。

### 可程序化锚点（§2）
```csharp
// World.Era（承 〇.1/〇.2/〇.5）—— 全局灵气+软压力，纯整数，Clock 节流不消费 RNG
record EraState(string EraName, string ReignName, int EraIndex,
                int AmbientQi /*[0,1000]*/, int QiDecayPerKTick, int QiFloor,
                int EraTension /*[0,10000]*/) {
    int RealmCapHint => QiToTierTable[AmbientQi / 100];   // 长度 11 整数表 → UT 软上限
}
// World.Rift（承 〇.2）—— 单调倒计时，硬倒计时进度条
record RiftClock(int Integrity /*[0,1000]*/, int StressPerKTick,
                 IReadOnlyList<int> Thresholds /*{500,250,0}*/, int Stage /*[0,3]*/);
// 气运（承 〇.3）：势力 Faction.Fortune[0,FortuneCap]；个体 CultivationState.Resources["fortune"]
// EraConfig（嵌入 LimitsConfig）：CalamityGates / RiftThresholds / RiftStressBonusCap / FortuneCap / FortuneDecayPct
// DomainEvent 纯加：RiftCracked / QiSurged / FortuneTransferred / RegimeWarinessRaised / RelicClaimed / ApocalypseLooming
```

---

# 三 · 力量层级总览（Power Tiers）

> 本章是可程序化骨架，与 A2-FINAL 三轴解耦逐字段对齐、零冲突。三个「身份层」不是新枚举字段，而是 **数据表驱动的纯函数派生**（承 〇.7：彻底数据化以消除 L1 例外）。

## 3.1 三身份层 = 数据表驱动派生（不存字段，加新层=加表行）

> **承 〇.7 关键改进**：草案原 `IdentityLayerOf` 是 if-else 派生分支（加新身份层需加分支=L1 小改核心）。本文改为 **`(谓词→层)` 数据表 `IdentityRuleTable`** 驱动求值，**加新身份层 = 加表行（L0 真·零改核心）**。

```
// 身份层判定 = 按 Priority 升序匹配第一条谓词命中的数据行（PredEval 复用情境谓词求值器）
record IdentityRule(int Priority, string WhenPred, IdentityLayer Layer);

IdentityRuleTable（默认四行，数据驱动）:
  Priority 0 : WhenPred="cult_null"        → Mortal      // Cultivation==null（off 时全员凡人）
  Priority 1 : WhenPred="root_zero"        → Mortal      // rootQuality==0（永凡人，状态机锁 Phase0）
  Priority 2 : WhenPred="cannot_ascend"    → Martial     // Curve.CanAscend==false（武夫，老死封顶）
  Priority 3 : WhenPred="ascendable"       → Cultivator  // Curve.CanAscend==true（修士，可飞升）

IdentityLayerOf(ch) = 引擎读 IdentityRuleTable 按 Priority 求值首个命中  // 纯函数，读时算，不进 StatBlock
// 加「半妖/器灵」身份层 = 注册谓词 key + 追加 IdentityRule 行（L0），引擎不改
```

- **凡人（Mortal）**：`Cultivation==null` 或 `rootQuality==0`。市井众生、纯武人之下者、无灵根世家子。人口基数与戏剧/势力的土壤。cultivation-off 时全员凡人（38 测试逐字节不变 = INV-OFF-38）。
- **武夫（Martial）**：走 `CanAscend=false` 武侠路。封顶 **UnifiedTier 9 = 陆地神仙**（武道绝对顶点，非仙侠第五境化神）。武夫「境界少但每境当量高」——UnifiedTierOf 跳档大。
- **修士（Cultivator）**：走 `CanAscend=true` 仙侠路。可一路修到 **UnifiedTier 11 = 飞升 → Ascended 离场**。

> **武侠↔仙侠同台不崩**：武夫与修士经 **UnifiedTier 0–12 统一刻度同台可战**。判据「同 UnifiedTier 战力当量」（INV-CROSS：同 UT 两路 1v1 胜率∈[40%,60%]），不要求每路在每个 UT 都有锚。UT9 陆地神仙武夫与 UT9 仙侠修士同台势均力敌——「一界两层共存」在战力层的落点。

## 3.2 力量层级总览表（凡人→武夫→修士 × UnifiedTier × 仙侠/武侠双锚）

> UnifiedTier 是唯一跨路/跨层可比刻度；仙侠/武侠两列是同一 UT 在两层的「称谓投影」（per-path `RealmCurveDef.UnifiedTierOf` 数据驱动，引擎零特例）。
>
> **承 〇.7 / 消解身份层表层矛盾**：本表「典型身份 / 封顶事件」列**不是 UT 的函数**——身份权威判据仍是 §3.1 的 `IdentityRuleTable`（读 `CanAscend`）。UT9–11 行的单一标注仅为「典型情况」，已补注「若某仙侠路 major 恰映射至此 UT 则该 UT 也可能是修士」。

| UnifiedTier | 典型身份/封顶事件※ | 仙侠层称谓（厚灵） | 武侠层称谓（薄灵） | 本 UT 世界观/机制锚（整数门） |
|:---:|:---:|---|---|---|
| 0 | 凡人→初修 | 炼气 | 不入流/三流 | 引气完成；资源池小；走火轻。身份层在此可由凡人跨入 |
| 1 | 武夫/修士 | 筑基 | 二流 | Foundation 启用；首瓶颈；首小延寿(+50) |
| 2 | 武夫/修士 | 金丹 | 一流 | 资源上限跳升；渡劫首现 |
| 3 | 武夫/修士 | 元婴 | 后天巅峰 | 神识；心魔萌（innerDemon 漂移加速） |
| 4 | 武夫/修士 | 化神 | 先天 | **心魔劫强制**；神通解锁 |
| 5 | 武夫/修士 | 炼虚 | 宗师 | 法则初触；虚境惩罚减轻 |
| 6 | 武夫/修士 | 合体 | 大宗师 | 道体融合（高爆发段起点） |
| 7 | 武夫/修士 | 大乘圆满 | 绝顶 | 积功德/道行额外资源 |
| 8 | 武夫/修士 | （大乘巅） | 天人/陆地神仙下 | 天劫加剧 |
| 9 | **武夫典型封顶**※ | — | **陆地神仙** | 武道顶点；武侠路 `CanAscend=false` 在此封顶老死。**仙侠路若有 major 映射至此，亦可为修士** |
| 10 | 修士·飞升劫态※ | 渡劫（飞升劫裁决态，非台阶） | 手中无招 | 法则权限位 `LawPermission` 绑定段；渡劫=大乘大圆满引动飞升之劫，成功即飞升 |
| 11 | **修士典型飞升**※ | 飞升 → Ascended 离场 | — | 转 `World.Ascended` 档，离场腾位（防长生者塞满 AliveCount，INV-POP） |
| 12 | — | 域外/天人（传说·失传境） | — | **YAGNI 硬封顶占位（不可达，留 A.1+）** |

※「典型身份/封顶事件」列**非身份权威源**；身份权威判据 = §3.1 `IdentityRuleTable`（读 `CanAscend`）。同一 UT7 可能是绝顶武夫也可能是大乘修士。

**对齐校验（与 12 路 / 大小境界）**：
- **12 路并行**：UnifiedTier 是跨 12 路的公共刻度；每路 `RealmCurveDef.UnifiedTierOf[major]` 把该路 `Nₚ`（7~10 大境界）映射到 0–12（武侠路跳档大/压缩，仙侠路较密；魂修仅 7 major 必跳档如 `[0,2,4,6,8,10,12]`）。身份层由 `CanAscend` 决定，不由 UT 决定。
- **大小境界双层**：每行是大境界（MajorTier）粒度。小境界（per-major `SubLevelCount` 数据驱动，可炼气9/化神3/大乘1）**不进 PowerEngine 主战力**，只用于显示/突破节奏/寿元/劫。战力台阶 = 大境界 × `MajorMul[flatIndex]`，小境界是台阶内进度条。
- **独立战力公式/曲线**：每路 `final = (long)raw × MajorMul[flatIndex] / 10`（整数除），`raw` 由该路独有 `PowerFormulaDef` 求值。层级总览只统一「跨路可比刻度」，绝不统一各路战力公式。

## 3.3 身份层跃迁 = 既有状态机事件，非新机制

```
凡人(Phase 0 Mortal) ──rootQuality>0 ∧ Insight≥T_INDUCT_INS──▶ QiInduction ──引气成──▶ 入修行(UT0)
   分流：定路时 Curve.CanAscend==false → 武夫；==true → 修士（生成期 WorldFactory.Spawn 定路，Split 5）
武夫顶：major==MaxMajor(UT9) ∧ 圆满 → 老死即终（无飞升）
修士顶：major==MaxMajor ∧ 飞升劫 TribScore≥AscensionGate → Ascended 离场（UT11）
```

跃迁概率「随机但有上限」：灵根 `rootQuality` 生成期按整数概率抽取；引气成败走有界 `InductRoll`（非无界随机）。

### 可程序化锚点（§3）
```csharp
enum IdentityLayer { Mortal, Martial, Cultivator }
record IdentityRule(int Priority, string WhenPred, IdentityLayer Layer);  // 数据表（承 〇.7）
IdentityLayer IdentityLayerOf(Character c);  // 读 IdentityRuleTable 按 Priority 求值首个命中
// 力量层级 = UnifiedTier 0-12 唯一跨路刻度（per-path RealmCurveDef.UnifiedTierOf 数据驱动）
// 武夫典型封顶 UT9(CanAscend=false 老死)；修士 UT11 飞升离场(World.Ascended)；UT12 硬封顶
// 核心四维(锁): Force/Internal/Constitution/Insight, 生成期 Σ=80, 运行期单维 clamp[0,30]
// 心性/道心 → CultivationState.Resources["daoHeart"/"innerDemon"/"comprehension"]（侧表）
// 机缘/气运 → CultivationState.Resources["fortune"]（个体）/ Faction.Fortune（势力）
```

> **与已锁四维契约的衔接（防引入冲突字段）**：简报语义「心性/道心」「机缘/气运」**不是核心四维**，而是侧表运行态资源（`CultivationState.Resources`），初值可公式播种（如 `daoHeart=Insight×k`）但此后独立演化，**不参与 Σ=80**。核心 `StatBlock` 4 维一字不动。

---

# 四 · 全谱统一势力模型（Unified Faction Model）

> 锁定决定 (c) 落地章。一切江湖组织——名门正宗、庙堂朝廷、千年世家、魔道邪宗、市井帮派、行商钱庄、无依散修、化外异族——统一为**单一 `Faction` 模型 + `FactionType` 类型位**，共享同一套关系/恩怨/联盟/战争/附庸/夺地机制。是已落盘 `Jianghu.Faction`（v1.2-C/D）的**泛化超集**，零改既有接缝。

## 4.1 设计立场：一个模型，八类位，差异全在数据

- **`Faction` 运行态结构对所有类型同构**（同一 `FactionLedger` 主存、同一关系矩阵、同一 `FactionDirector.Pump`）。
- **类型差异全部外置为数据**：每个 `FactionType` 的「典型目标函数权重 / 资源结构偏置 / 气运来源 / 偏好地利 / 关系基线」写在 `FactionTypeProfile` 数据表，不写死在代码分支。
- 魔道与正宗共用 `AmbitionWeight`/`ResourceHunger` 字段，只是取值不同——落实「别让正魔做成道德二元静态对立，给魔道同样理性目标函数」。

## 4.2 FactionType 分类法（8+ 类型位）

> **承 〇.7（诚实分级）**：`FactionType` 是 `byte` 枚举——加新类型 = **加枚举值（L1 小改核心）+ 加一行 `FactionTypeProfile`（L0）**。引擎读 profile 表无 type 分支，但枚举本身是代码改动，**不谎称「零改核心」**，标 L1。

```csharp
public enum FactionType : byte {
    Sect=0, Court=1, Clan=2, Demonic=3, Gang=4, Merchant=5, Rogue=6, Exotic=7,
    // 8+ 预留：Imperial_Cult / Hermit_Order / Sea_Pirates… 加值即可（L1）
}
```

**`Rogue`（散修）是特例**：不是有掌门的实体组织，而是「散修登记处」的**聚合伪势力**（复用 v1.0 `Sect` 单例降级体，faction-on 时降级为散修登记处不删）。散修个体经 `Rogue` 这个 `FactionId` 挂账，但 `MasterId=null`、`Disciples` 松散名册、`Treasury` 公共集市池；**不参与夺地/灭门**，只做相遇/拜师/招募源池。保住「散修可被各势力招揽→涌现入门」而不破坏 4X 式势力博弈。

### 各类型 Profile（典型目标 / 资源 / 气运）

| Type | 定义 | 典型目标（整数目标函数偏置） | 资源结构（主导维） | 气运来源 |
|---|---|---|---|---|
| **宗门 Sect** | 武学道统传承组织，有山门门规辈分 | 高 `Cultivate`、中 `Prestige`、低 `Conquest` | `Might` + `QiTerritory`（灵脉气运地） | 道统延续 + 灵脉品阶 + 高手境界（UnifiedTier 加权） |
| **朝廷 Court** | 据法统治、掌兵权官僚的政权 | 高 `Order`（招安/镇压/制衡，**忌惮值反馈**）、高 `Territory`、中 `Conquest` | `Territory` + `Treasury`，`Might`=常备军 | 国运/龙气（辖地民心×城镇数）、法统正当性 |
| **世家 Clan** | 血脉传承武学/资源的门阀 | 高 `Lineage`、高 `Alliance`（联姻世交）、中 `Prestige` | `Treasury`（田产底蕴）+ 血脉气运 | 血脉传承值（代数×嫡系人才）、世交网络密度 |
| **魔道 Demonic** | 行邪功、夺精血气运的异端（理性掠夺者非脸谱恶） | 高 `Conquest`、高 `ResourceHunger`、低 `Order`（与朝廷/正宗敌对基线） | `Might` + 掠夺所得 `Treasury` | 邪道气运（吞噬他势力气运→守恒转移）、血煞积累 |
| **帮派 Gang** | 占码头收保护费控漕运绿林 | 高 `Territory`、中 `Treasury`、中 `Conquest`（火并） | `Territory`（坊市渡口）+ `Treasury` | 地盘民心（控制 Site 的 Wealth 之和）、人多势众 |
| **商会 Merchant** | 钱庄镖局药行拍卖，逐利通商 | 高 `Treasury`、高 `Alliance`、极低 `Conquest` | `Treasury` 绝对主导，`Might`=镖师 | 商路气运（贸易 Site 连通度×Wealth）、信誉值 |
| **散修 Rogue** | 无依修士/游侠松散登记池（伪势力） | 无组织目标（个体自驱）；仅招募源+集市 | 公共 `Treasury`（集市池），无 `Might`/`Territory` 聚合 | 不持势力气运（个体气运归个人侧表） |
| **异族 Exotic** | 化外妖族/海外/塞外/古族，法理相异 | 高 `Conquest`（入侵）或高 `Isolation`（守境），由 `AlignmentAxis` 细分 | `Might`（异种天赋）+ 地利，据塞外/西域/苗疆 PreferredTerrain | 族运（古族传承/图腾）、地利气运 |

> **正交轴 `AlignmentAxis`**（正 +1 / 中 0 / 邪 −1）与 `FactionType` **解耦**。例：`{Sect,+1}`=正宗、`{Sect,-1}`=邪派宗门、`{Court,-1}`=昏暴朝廷。**两轴叉乘**给出丰富立场而不爆类型枚举——「加立场=调数据」（L0/L1，见 §9）。

## 4.3 数据驱动定义 `FactionDef`（泛化 v1.2-C，纯加字段）

```csharp
public sealed record FactionDef(
    int DefId, string Name, FactionType Type, int AlignmentAxis /*正1/中0/邪-1*/,
    string[] PreferredPathKeys, string[] ForbiddenPathKeys,  // 软绑 12 路（string，注册表不知 faction）
    int[] Codes,                          // 门规/法度：声明式整数谓词
    int BaseReputation, int Ambition /*[0,100]*/, int ResourceHunger /*[0,100]，战争动机=稀缺非道德*/,
    int PreferredTerrainMask, RankBand RankBands, int TypeProfileRef /*→FactionTypeProfile*/);

public sealed record FactionTypeProfile(
    FactionType Type,
    int W_Conquest, int W_Prestige, int W_Treasury, int W_Alliance,
    int W_Lineage, int W_Order, int W_Cultivate, int W_Isolation,   // 目标函数权重（整数，Σ 不强制守和）
    FortuneSourceKind FortuneFrom, int FortuneRatePct,              // 气运来源映射 + 折算系数
    int TreasuryWeight, int MightWeight, int TerritoryWeight);       // 综合实力三维权
```

> **PathKey 存在性校验**：`Validate()` 对照已注册修炼路线集 fail-fast；A 未落地时该集空→显式标「软绑悬空，cultivation-off 降级」，不静默拼错。
> **开闭验收必含异构 case**：不只加同 Type 同 Align（同构），要能加新 `FactionType`（如 `Sea_Pirates`）或新 `AlignmentAxis` 关系类型且零改引擎（L1：加枚举+表行），才算证明可扩展。

## 4.4 运行态聚合 `Faction` + `FactionLedger`（泛化 `SectLedger`）

```csharp
public sealed class Faction {
    public int Id; public int DefId; public FactionType Type;  // Type 冗余缓存自 Def
    public CharacterId? MasterId;     // 掌门/家主/帮主/族长；朝廷=君主；散修=null
    public List<CharacterId> Elders, Members;  // 确定性：按 Id 升序维护
    public int? HomeRegion; public List<int> ControlledSites;
    public int Reputation;  // [0,PrestigeCap] 经 ApplyPrestige
    public int Treasury;    // [0,TreasuryCap] 经 ApplyTreasury
    public int Fortune;     // ★气运/国运 [0,FortuneCap] 经 ApplyFortune（可被夺取→守恒转移）（承 〇.3）
    public int MightCache;  // 门下战力（脏标记缓存，失效=入门/逐出/死亡/晋升）
    public SectPhase Phase; public bool Active;
}
public sealed class FactionLedger {
    List<Faction> _factions;                       // 主存，裁决前按 Id 升序
    Dictionary<int,int> _bySite, _byMember;
    Dictionary<(int,int),int> _relations;          // ★势力间有向好感 [-100,100]（承 〇.4，同构 Relations）
    Dictionary<(int,int),FactionTie> _ties;        // ★结构性关系（联盟/附庸/战争/世仇）
}
```

- **气运 `Fortune` 守恒纪律**（承 〇.3）：来源按 `FortuneRatePct` 缓慢累加但封顶 `FortuneCap`，设每 Pump 周期蒸发项 `Fortune -= Fortune * DecayPct / 100`（香火随时间蒸），防长跑通胀。**夺取气运=源减目标增（严格守恒）**：魔道吞噬/朝廷收编/灭门转移走 `FortuneTransferred` 事件，`Σ Fortune` 转移瞬间守恒。
- **层级整数 rank**：掌门 5/长老 4/真传 3/内门 2/外门 1/记名 0，存 `SectMembership.Rank`。INV-FACTION-1：每个非散修势力恰 1 个 `MasterId`（散修豁免）。
- **综合实力**：`Power(f) = Treasury*TW + MightCache*MW + Territory*TerW + Fortune*FW`（各权来自 profile，long 防溢出），整数全序，平手按 `Id` 升序。

## 4.5 势力间关系机制（恩怨/联盟/战争/附庸/夺地）

### 第一层：连续好感标量 `_relations`（承 〇.4，钳 `[-100,100]`）

| 外交动作 | `_relations` 整数增减 | 上限/纪律 |
|---|---|---|
| 联姻（世家/朝廷） | 双边 `+ MarriageBond`（如 +40） | 每对子 cooldown，防刷 |
| 赠礼/通商（商会） | 按规模分档 `+ giftValue/scale` | **侮辱阈值**：低于阈值反扣分 |
| 边境摩擦/夺地 | 守方对攻方 `- ConquestEnmity`（如 −60） | 见夺地 |
| 同盟共御外敌 | 盟内各对 `+ AllyTrust/tick`（缓增） | 威胁消失即衰减 |
| 阵营基线（生成期） | `AlignmentRelationTable[(轴A,轴B)]` 注入初值 | 外置数据表，加对立轴=加行（L0） |

> **`AlignmentRelationTable` + `TypeRelationBias` 双表**：关系基线 = `f(AlignmentAxis 对, FactionType 对)`。例 `(邪,正)→-50`、`(Court,Demonic)→-40`、`(Merchant,*)→+10`。两表外置，加新类型/新轴=加表行（L0/L1）——「做成中性 relation matrix 比布尔善恶鲁棒」的落地。

### 第二层：结构性关系纽带 `FactionTie`（状态化）

```csharp
public enum TieKind : byte { Neutral=0, Feud=1, Alliance=2, Vassal=3, War=4, Trade=5, Marriage=6 }
public sealed record FactionTie(TieKind Kind, int Strength /*[0,100]*/, long FormedTick, int Cooldown);
```
> **承 〇.7（诚实分级）**：加新 `TieKind` = **加枚举值（L1 小改核心）+ 转移阈值入 `LimitsConfig`**，引擎框架不动但枚举是代码改动，标 L1，不谎称零改。

- **恩怨（Feud）**：`_relations` 跌破 `FeudThreshold`（≤−50）→ 升 `Feud`，产 `SectFeudFormed`。**势力世仇可下沉为个体恩怨**：经 `GrudgeLedger` 把 `SectFeud` 投影成参战成员间的 `Grudge`（`GrudgeKind=SectFeud`，带 `Intensity`）→ 势力级冲突→复仇弧涌现。A/B 缺席时降级为仅镜像负 `_relations` 驱动 `notFoe`。
- **联盟（Alliance）**：好感 ≥ `AllyThreshold` 且（可选）存在共同高威胁势力 → 升 `Alliance`。**联盟非永久**：每 Pump 周期 `Strength` 受「共同敌人威胁度」供养；威胁消失→`Strength` 衰减→跌破即瓦解（产 `AllianceDissolved`）。
- **附庸（Vassal）**：强弱 `Power` 差 ≥ `VassalGap` 且非敌对 → 弱方成附庸（有向）。附庸每周期向宗主上缴 `Treasury` 分成 + 部分 `Fortune`（守恒转移），宗主提供保护。附庸可叛（关系恶化或宗主衰弱→`VassalRevolt`，降 `Neutral` 并生 `Feud`）。
- **战争（War）**：`Feud` 双方 + 攻方 `Ambition` 高 + `Power` 差 ≥ `WarGap` + 地理相邻 → 升 `War`，进夺地结算。**并发上限 `MaxConcurrentWars` + 冷却 `WarCooldown`**，防全员开战过载。

### 夺地（非致死最薄结算，继承 v1.2-C）

```
攻方 Ambition 高 + Power 差 ≥ ConquestGap + 区域相邻(IGeoQuery)
  → TerritoryLost 单事件：改 SiteOwnership + _bySite 索引
  → 守方 _relations 对攻方 -ConquestEnmity
  → A/B 在位：喂 SectFeud → GrudgeLedger（个体复仇种子）
  → 守方 Territory 归零 → 触发兴衰状态机 Declining/被吞并（全员转散修 Rogue，非致死）
```
- 夺地候选只扫相邻边界 Site（非全图），`FactionDirector.Pump` 按 `Clock` 节流，`MightCache` 脏标记缓存非重算 → 满足 `INV-PERF` 量化耗时上界。
- 致死式灭门留 C.1；C.0 覆灭 = 弟子离散归零/被吞并 → 全员转 `Rogue`。

## 4.6 兴衰状态机（`FactionDirector.Pump`，泛化 `SectDirector`）

```
SectPhase: Founding0 → Flourishing1 → Stable2 → Declining3 → {Defunct4 | Reborn5}
```
- 转移读 `Reputation / MightCache / Members.Count / Fortune / MasterId 在世`。掌门寿尽 → 继任 = 门内 `Rank desc → MightCache 个体值 desc → JoinTick asc → Id asc`（确定性取首）。
- **气运联动**：`Fortune` 跌破 `FortuneFloor` → 强制 `Declining`；`Fortune` 满 `FortuneCap` 且 `Power` 高 → 触发「鼎盛」（`FactionFlourished`）。
- **Pump 挂载次序**（逐字节敏感，固化）：`World.Advance` 末尾 `角色循环(v1.0) → _faction?.Pump → _drama?.Pump → era/rift Pump → MaybeSpawn(v1.0)`。按 `Clock` 节流。
- `factionRng = root.Split(8)` 私有流，子流 `factionRng.Split(factionId)` 顺序无关；faction-off 不构造不消费。**承 §8.3：faction(8) 必须作为 World 字段持久化进 Clone**（与 domain/spawn 同等待遇），否则夺地/气运转移随机端点 Clone 续跑不可复现。

## 4.7 初始势力 Landscape（底色张力，非中心剧情）

> `WorldFactory.CreateInitial` 在 seed 控制下经 `CodeFactionSource` 注入。只给「将临未临」高 tension 底色，**不预写谁赢谁灭**。命名/数值为示例锚点，实际魔数待平衡标定。

| 命名势力（手写种子） | Type | Align | 据地（Region） | 初始张力钩 | 气运/规模 |
|---|---|---|---|---|---|
| **大胤朝（镇玄司）** | Court | 中 0 | 中州·神京 | 对**血河魔宫** `War` 预置；对**剑墟道盟**忌惮（关系 −20，功高震主）；对**万器商会** `Trade` | 国运高、辖地最广 |
| **剑墟道盟** | Sect(剑修) | 正 +1 | 东海·剑墟 | 武林泰斗，据东海剑墟，气运随道枢一裂上升，与朝廷既合作又防备；与**百蛊渊**预置强恩怨 | Might 顶、灵脉气运盛 |
| **万器商会** | Merchant | 中 0 | 西陲·万器谷 | 灵石/法宝硬通货垄断，气运最高、Might 偏低→易成众矢之的 | Treasury 绝对第一 |
| **铁佛寺** | Sect(佛修) | 正 +1 | 北漠·铁佛寺 | 北漠御异族，与朝廷临时同盟（威胁消失即瓦解） | 底蕴厚、佛门气运 |
| **百蛊渊魔宗** | Demonic(鬼修) | 邪 −1 | 南疆·百蛊渊 | 对朝廷/正宗全面 `Feud`，`Ambition=90 ResourceHunger=85`（理性掠夺，南下夺灵脉）；与剑墟预置 1 对强恩怨（gen0 复仇弧种子） | 邪道气运、Might 强 |
| **苗疆古蛊一族** | Exotic | 邪 −1 | 苗疆·十万大山 | 闭关守境（`Isolation` 高）、对中原警惕；古族传承气运；偶有冲突 | 族运、地利气运 |
| **（散修登记处）** | Rogue | 中 0 | 全图 | 招募源池，无纽带；各势力从此招揽→涌现入门 | 无聚合（个体气运归个人） |
| …（程序生成世家/帮派/异族若干） | 数据驱动 | AlignmentRelationTable 裁定 | 程序随机 | 同轴基础友好加成/跨轴惩罚 | — |

**底色张力来源（全局时钟，非脚本反派）**：
1. **血河魔宫/百蛊渊南下** = 资源驱动的「将临之战」：灵脉总量有限→魔道 `ResourceHunger` 高→预置 `War` 纽带但**未触发夺地**，留足 agent 行动窗口。
2. **朝廷 vs 剑墟道盟的忌惮链**（「功高震主→猜忌」）：朝廷对江湖势力持 `RegimeWariness` 整数；某势力 `MightCache` 超阈→`RegimeWariness` 上升→触发打压/联合围剿（经 DomainEvent + `_relations` 恶化）。**强者不通吃**，过强反招围剿。
3. **世家血脉渐稀** = 老牌势力衰退伏笔（`Fortune` 缓降），为「大战重洗格局/新秀上位」留涌现钩子。

三条都是**整数维 + 阈值 + 纽带**，无一行写死结局。

### 可程序化锚点（§4）
```csharp
// 全谱 Faction：统一运行态 Faction + FactionLedger + 两张外置数据表（FactionTypeProfile × AlignmentRelationTable/TypeRelationBias）+ 整数关系机制
// 势力气运 Faction.Fortune[0,FortuneCap]（承 〇.3，随 Faction.Clone）；关系 [-100,100]（承 〇.4）
// factionRng=Split(8) 私有流，须进 World.Clone 深拷（§8.3）；off 不构造不消费
// DomainEvent 纯加：FortuneTransferred / AllianceDissolved / VassalSubjugated / VassalRevolt / WarDeclared / WarEnded
//   （v1.2-C 已预留 FactionJoined/Promoted/Succession/Defected/Fallen/SectFeudFormed/SectAlliance/TerritoryLost…）
// 润色文本只进渲染层，绝不回写 DomainEvent 流（同种子同输出不破）
```

---

# 五 · 江湖地理架构（固定骨架 + 随机微）

> 锁定决定 (b) 落地章。一切地理设定遵两红线：可程序化（确定性整数机制 + 有上限随机）；不锁死（加州/地标/秘境/资源种类 = 追加数据行，零改核心，L0）。地图归 `Jianghu.Geo`；`Site.Id` 复用 `NodeId` 单主键空间；地图随机流 `mapRng=root.Split(7)`；地理资源经 string `resourceKey`/`pathAffinityKey` 软绑 12 路；移动可达性按 UnifiedTier 硬阈值门控；地形/昼夜/灵气只作软情境战斗小%修正。

## 5.1 设计公理：固定骨架 + 随机微

世界切两层，确定性来源不同：
- **固定层（`Fixed`）**：命名大区（州/域）、大门派山门与朝廷皇城等**地标锚点**、**区域邻接骨架**。手工编撰为**数据表常量 `GeoCanon`**，启动静态加载，**不消费 mapRng**，逐种子完全一致，是世界底色与跨纪元不变锚。
- **随机层（`Procedural`）**：秘境、资源点、坊市、散修聚落、流动 NPC。由 `mapRng=root.Split(7)` 在固定骨架之上程序生成，**随机但有硬上限**（先 clamp 到 caps 再用，硬上限优先于随机）。同种子逐字节复现。

> **可程序化锚句**：固定层 = 静态确定性查表（零 RNG）；随机层 = `(固定骨架 × 上限约束 × seeded mapRng)`。两层在 `NodeId` 单主键空间拼成一张连续整数图。

## 5.2 固定层：三级空间模型 + 地标锚点 + 邻接骨架

```
World(江湖根)
 └─ Region[]   命名大区 6~12：中州/东海/北漠/西陲/南疆/苗疆/江南…
     │          固定；门派/朝廷据此占地利
     ├─ 地利四维整数 [0,100]: QiDensity 灵气 / Wealth 财货 / Strategic 形胜 / Peril 凶险
     ├─ ElementAffinity: 大区主元素（金/木/水/火/土/无），喂软情境战斗 ±小%
     ├─ TerrainClass: 主地形（平原/山岳/水泽/荒漠/林莽/海域），喂软情境 + 移动
     └─ Site[]   地点 3~8/区：城镇/客栈/关隘/渡口/门派山门/皇城/秘境/资源点
                 Site.Id = NodeId；固定 Site 手工锚（低段），随机 Site 程序追加（高段）
```

- **`Site.Id` 复用 `NodeId` 同主键空间**（连续 `0..N-1`，按大区前缀和分配），保 v1.0 `Nodes/NodeCount/AtNode/Move`、`TravelAction` 的 `0<=To<NodeCount` 边界检查全兼容。`RegionOf(nodeId)` = 对前缀和数组整数二分，纯整数。
- **`WorldNode` record 一字不改**；地理/资源/归属/地形全走**侧表**（键 NodeId）。
- **固定 Site 在前、随机 Site 在后**，`NodeId` 全局单调递增——固定地标的 NodeId 在同一 `GeoCanon` 下恒定（可叙事引用），随机段随 mapRng 变。

**地标锚点 `LandmarkDef`（手工固定常量，启动静态加载，零 RNG）**：
```csharp
record LandmarkDef(int Id, int RegionId, LandmarkKind Kind /*皇城/祖庭/魔窟/王庭/雄关/巨港/学宫/古迹*/,
                   string Name, int FactionDefId /*可空：绑 Faction*/, GeoRec Geo /*地利 override*/);
```
- **地标与势力领地耦合**：`LandmarkDef.FactionDefId` 在门派 Seeder 阶段被读取——`Jianghu.Faction` 据此把该势力 `HomeRegion` 与初始 `ControlledSites` 锚到地标 Site。**地图不知门派存在**（`IGeoQuery` 只读单向）。

**区域邻接骨架 `RegionAdjacency`（手工固定图 + 带门控边）**：
> **关键修订（相对旧设计）**：废除程序化 MST，改手工固定骨架（锁定决定 b 明确「命名大区 + 邻接骨架固定」）。区域级连接是世界政治地理骨干（谁与谁接壤决定夺地/联盟/战争），必须可信可叙事。随机性只下沉到区内 Site 微拓扑。

```csharp
record RegionEdge(int RegionA, int RegionB, int BaseCost /*整数旅行权*/,
                  PassKind Pass /*Open/关隘需通牒/限时开启/境界门控*/, int UnifiedTierGate /*跨边最低 UT*/);
```
- **跨区连接 = 稀疏带门控图，非全连通**（关隘需通牒、海域需高境界横渡→制造势力割据与资源垄断）。
- **境界门控用 UnifiedTier 硬阈值**：`canCrossRegion(char,edge) = char.UnifiedTier >= edge.UnifiedTierGate`，纯整数比较。
- **INV-GEO-CONNECTED**：`GeoCanon` 加载期 BFS 断言区域骨架在「忽略门控」下全连通（无孤岛），fail-fast。门控只限制谁能走，不切断图是否连通。
- 区内 Site 微拓扑：区间骨架全固定；区内 = 固定地标锚 + 随机微边（`degree≤MaxDegree`），随机段接入后区内 BFS 连通断言。

## 5.3 随机层：程序生成（随机但有上限）

全部 roll 走 `mapRng=root.Split(7)`（私有流）。硬上限优先于随机（先 clamp 再用），频率/强度/并发三类上限齐备。

**生成总流程（纯整数 seeded）**：
```
1 固定层加载：Regions/Landmarks/RegionAdjacency 拷自 canon（零 RNG）；固定 Site 占 NodeId 低段；RegionOf 前缀和建好
2 区内随机 Site 数 = mapRng.NextInclusive(SiteMin,SiteMax) clamp ≤ MaxSitesPerRegion；类型从 SiteComposition 加权抽（稀有优先不重复）
3 资源点：每 Site 按 ResourceTable 加权抽 0..K 个；Grade=NextInclusive(GradeMin,GradeMax)；全图同 resourceKey ≤ ResourceCap[key]
4 秘境：对 QiDensity≥SecretQiThreshold 的 Site，NextInt(100)<SecretChance 建 Secret；全图 ≤ MaxSecrets（先到先得，余丢弃）
5 坊市：对 Wealth≥MarketWealthThreshold 城镇 Site，MarketChance 标 Hub；全图 ≤ MaxMarkets
6 散修聚落 + 流动 NPC：生成期布点 + 运行期受 PopulationLow/High 约束（散修出生偏置用 spawnRng=Split3，非 mapRng）
7 区内随机微拓扑接边 + BFS 区内连通断言
8 邻接表每项升序 Sort（裁决前必排序）
```

**秘境 `SecretSite`（周期开启 + 位置 seeded，内容固定表）**：
```csharp
record SecretArchetype(int Id, string Name, int CyclePeriod /*整数 tick 周期*/, int EntryGateUT,
                       int Capacity /*并发上限*/, LootTable Loot /*固定：(resourceKey,grade)+storylet 钩子*/);
// 状态机：Hidden → Open(剩 OpenWindow tick) → Closed → (下个 CyclePeriod) Hidden
```
- **位置 seeded**：绑生成期选定 Site（`SecretSite.HostNodeId`），开启时显形（`World.Map.RevealedSecrets : HashSet<int>`）。位置 gen 期一次性定，**不每帧随机**。
- **三类上限齐**：频率（`CyclePeriod`）、强度（`EntryGateUT`）、并发（`Capacity` + 全图 `MaxSecrets`）。进入门控用 UnifiedTier。
- **耦合修炼**：`LootTable` 产 `(resourceKey,grade)`，经 `CultivationState.ApplyResource` chokepoint 喂对应路 per-path 资源；A 未落地降级为仅 Chronicle flavor + 门派 Treasury，不碰四维。

**资源点 `ResourceNode`（稀疏散布 + 可再生有限）**：
```csharp
record ResourceNode(int HostNodeId, string ResourceKey /*"qi_vein"/"ore"/"herb_field"/"beast_lair"/"music_stone"…*/,
                    int PathAffinityKey /*可空，偏好某路*/, int Grade /*[1..GradeMax]*/,
                    int Reserve, int RegenPerTick, int ReserveCap, int? OwnerFactionId);
```
- **采集闭环（`HarvestAction`）**：`Reserve>0` 可采，产 `ResourceHarvested`，`Reserve -= 采集量`（clamp≥0）；每 tick `Reserve = min(Reserve+RegenPerTick, ReserveCap)`。**抽干即枯竭**，逼迫迁徙/争夺（灵气来源闭环）。
- **耦合修炼**：`ResourceKey/PathAffinityKey` 是 string，资源点不认识具体路，修炼注册表据 key 决定喂哪路（加新路/新资源种类 = 加 key 映射，L0 零改地图）。
- **耦合势力领地**：`OwnerFactionId` 经 `SiteOwnership` 侧表持有；产出 = 势力 Treasury 收益整数公式，是夺地标的（对接 `TerritoryLost`）。

**坊市/散修/流动 NPC（邂逅密度锚）**：
- **坊市 = Hub 标记**：高 `Wealth` 城镇标 `SiteKind.Market`，`MaxMarkets` 上限。决策（`RuleBrain.Travel`）map-on 时按「邻居资源/Hub 权重」加权排序取首（仅加权排序，非寻路），角色自然向城镇/山门/坊市聚集→相遇密度生效。**受控 `RuleBrain.Travel` 改动**（map-off `Reachable=[Node+1]` 逐字节等价旧）。
- **散修聚落**：生成期低门派覆盖区布据点（`SiteKind.RoninCamp`）；运行期散修涌现由 v1.0 `Lifecycle.MaybeSpawn` + `PopulationLow/High` 控制（涌现个体不入门派）。散修出生偏置走 `spawnRng`（非 mapRng——分流不串）。

**区域危险层（承 〇.6，Site 级侧表）**：
```csharp
record HazardLayer(int Intensity, HazardKind Kind /*瘴疠/妖兽潮/鬼雾/风暴*/);  // 不带 RegionId，归属由 NodeId 隐含
// 挂 World.Map.SiteHazards : Dictionary<int/*NodeId*/, HazardLayer>（独立可变侧表，进 Clone 深拷）
```
- 危险层影响：移动 `BaseCost` 上调、滞留概率事件（storylet 钩子）、软情境战斗环境修正（`Intensity` 整数小%）。区域级灾劫（灭世余烬）由 gen 期展开为该区域若干代表 Site 的 `SiteHazards` 条目。

## 5.4 与软情境战斗的耦合（地形/昼夜/灵气作小%修正，不复活硬克制）

地图为软情境提供三类整数源，全部经 `PowerEngine` Modifier 流水线作**小%修正**（`x*pct/100` 每步 clamp，总修正受 ±P0/4 即 25% 上限钳制）：
- **`Region.ElementAffinity`（元素相生克）**：战斗发生地主元素 vs 双方攻击元素，查元素相生克表（数据外置，加新元素=加表行 L0）得小%adj。**「地利」不是「path 克 path」**——同一对 path 在不同元素大区结果可不同，但克制源是*地形元素*非*路线身份*。
- **`Site.TerrainClass`（地形）**：山岳/水泽/林莽给特定攻击维或特定路小%修正（如阵修己方布阵地形加成，经已注册 `IDerivedProvider`）。
- **昼夜**：全局 `Clock` 派生昼夜相，喂特定路昼夜倍率（鬼修夜强、纯阳昼盛），×整数定点修正。
- **距离**：邻接图 `BaseCost`/同 Site 与否提供，喂软情境距离项。
- **关键纪律**：地图喂的全是「情境」（在哪打、何时打），**不是「身份克制」**。移除地图层任何「path A 天然克 path B」硬编码。**软克制环不复活。**

### 可程序化锚点（§5）
```csharp
// 固定层 GeoCanon（手工常量，启动静态加载，零 RNG）：
record GeoCanon(RegionDef[] Regions, LandmarkDef[] Landmarks, RegionEdge[] RegionAdjacency,
                SiteEdge[] FixedSiteEdges, HazardSeed[] HazardSeeds /*区域级灾劫种子→gen 期展开为 SiteHazards*/);
record RegionDef(int RegionId, string Name, int QiDensity, int Wealth, int Strategic, int Peril,
                 ElementKind ElementAffinity, TerrainKind TerrainClass, SiteComposition Composition, ResourceTable Resources);
// 随机层 GeoConfig（全为上限/阈值，纯整数）：SiteMin/Max, MaxSitesPerRegion, MaxDegree, MaxSecrets, SecretQiThreshold,
//   SecretChance, MaxMarkets, MarketWealthThreshold, Dictionary<string,int> ResourceCap
// 侧表（键 NodeId；WorldNode record 不改）：
//   WorldMap（gen 后只读拓扑，可浅拷）：RegionPrefixSum / NodeGeo(GeoRec) / Adjacency / LandmarkNodeIds
//   GeoRec（纯只读）：QiDensity/Wealth/Strategic/Peril/Element/Terrain（★承 〇.6：不含 HazardLayer）
//   运行期可变侧表（进 World.Clone 深拷清单，承 〇.6 与批判修正）：
//     Dictionary<int,ResourceNode> Resources（含可变 Reserve，值拷或逐拷）
//     Dictionary<int,HazardLayer> SiteHazards
//     HashSet<int> RevealedSecrets ; Dictionary<int,int> SecretCycleState
// 门控用 UnifiedTier 硬阈值；距离用 dx*dx+dy*dy 平方比较（无方根）；mapRng=Split(7) 须进 World.Clone（§8.3）
```

> **承批判修正（WorldMap 不可变 vs 可变 Reserve 自相矛盾）**：所有运行期可变态（`ResourceNode.Reserve`、`RevealedSecrets`、`SecretCycleState`、`SiteHazards`）**从「Clone=>this 浅拷」的 `WorldMap` 拓扑体彻底剥离**，移入明确进 `World.Clone` 深拷的独立可变侧表；`WorldMap` 只保留 gen 后真正只读的拓扑/地利（可浅拷）。消除「WorldMap 内含可变 Reserve 又浅拷安全」的矛盾。

---

# 六 · 战斗哲学（软情境战斗 · Soft-Situational Combat）

> 世界铁律一句话：**胜负不是路线相生克的查表，而是「境界压制 × 本路战力」定大局、「天时地利人和」拨小数、「神兵道心」开变数、「天意」留爆冷。** 江湖人言「一力降十会，一巧破千斤」——前半句是境界与战力的碾压（大局已定），后半句是情境与道心的回旋（小处可翻）。

## 6.1 设计立场：为什么废除「硬 path-vs-path 克制环」

旧草案给过路线对路线有向克制环（「剑>体>法>剑」石头剪刀布）。**本世界观正式废除**，三条理由各落到机制：
1. **锁死路线、毁可扩展（破红线②）**：硬环加第 13 路要回环里重接所有边（O(N²) 改核心）。软情境表零改核心可扩（L0）。
2. **路线耦合、毁解耦**：12 路是「数据可并行、独立战力公式/曲线/途径」的解耦实体。让「剑」天然克「体」把两条正交路线焊死。
3. **碾压无变数、戏剧死**：硬环让「查表我赢」——无回旋、无以弱胜强、NPC 退化成可预测锯齿。

**取而代之**：胜负由**情境**而非**路线身份**调制。「火克木」不是「法修这条路克某木路」，而是**当攻方所持属性标签=火、守方=木**时给一个小整数修正——**上下文谓词**（context predicate），与「谁修哪条路」解耦。一个体修若持火灵根同样吃「火克木」情境利；一个法修在烈日下夜战增益清零。**情境，不是出身，决定那几个百分点。**

## 6.2 胜负主方程（整数、确定、五段式）

```
              ┌──────── 大局（境界 × 本路战力，主导项）────────┐
EffPower(X) = CrushGate( UnifiedTierΔ , PerPathEffectivePower(X) )    // §6.3 境界压制 + per-path 战力
              + SituationalAdj(X, 对手, 场景)                          // §6.4 软情境修正轴（小%，总额硬封顶 ±P0/4）
              + Skill/DaoAdj(X)                                        // §6.5 技（战技 OnUse）与道心
              + BoundedRoll(rng5)                                      // §6.6 有界随机（留爆冷，不无界）

winner = EffPower(A) >= EffPower(B) ? A : B                           // 平局沿用 v1.0「>=」语义
margin = |EffPower(A) − EffPower(B)|                                  // 喂 DuelResolved / 关系增减 / 生死阈
```

**口径锚定（与 v1.0 不破裂）**：cultivation-**off** 时本式坍缩回 v1.0 `Power = Force×2 + Internal + Constitution`、`winner = pa>=pb`、`margin=|pa-pb|`（`SparAction.cs:14-27` 实读核定）——38 测试逐字节不变（INV-OFF-38）。on 时才接入五段。**主导权排序**（量级递减）：境界压制 ≫ per-path 战力 > 情境修正 ≈ 技/道心 > 随机。这条**量级铁律**是「以弱胜强有空间、但不廉价」的根。

## 6.3 主导项：境界压制 × per-path 整数战力

**per-path 战力（复用已锁 PowerEngine 三段管线）**：
```
base  = Σ term.weight × resolve(term.src, ctx)   // src ∈ stat:* | realm | sumArtPower | res:<key> | derived:<key>；权重可负
raw   = clamp(base + 有序整数 modifiers, 0, ..)    // 每步整数 ×num/den + clamp
final = (long)raw × MajorMul[flatIndex] / 10      // 整数除，clamp POWER_CAP=2e9
```
12 路项不同、权重不同、曲线形状不同（剑修凸加速高爆发低容错；体修稳健耐久；法修均衡续航…），但同一解释器算出可比较整数。`src:realm` 解析为 **MajorTier**（非 flatIndex），小境界不进主战力。

**境界压制（CrushGate，单向渐进只削守方）**：
```
gap = atk.UnifiedTier − def.UnifiedTier
if gap >= CRUSH_TIER_GAP(2):
    defEffBase = defEffBase × crushNum / crushDen   // gap2→×3/4 ; gap3→×1/2 ; gap>=4→×1/4
// gap < 2 → no-op，结果 == 纯 per-path 逐字节回滚（INV-POWER-3）
```
- 碾压**只削守方基数、不加攻方**，置于情境修正之前，与情境/技/随机**各自独立 clamp 不互乘**（防归零）。
- 碾压是**大概率不是确定**：`gap<=2` 守方仍可靠情境/战技/道心/有界 roll 翻盘；`gap>=4` 才接近真碾压。
- **武侠↔仙侠同台**：陆地神仙/手中无招锚 UT9~11（大乘/渡劫量级），非「陆地神仙=化神」错锚。某路无某 UT 锚不影响同台判据。

## 6.4 软情境修正轴（SituationalModifier，本章核心）

情境修正**不查路线身份，只查上下文谓词**：把元素相生克/距离/地形/昼夜/准备表达为**带 guard 谓词的整数边表**，每条给小整数%，总额**硬封顶 ±P0/4（25%）**。

```csharp
public sealed record SituationalEdge(
    string Axis /*element|distance|terrain|daynight|prep*/,
    string WhenPred /*上下文谓词 key，禁硬编码对手 PathId*/,
    int AdjPctNum /*可负*/, int AdjPctDen /*=100*/, int Priority);
public sealed class SituationalTable {
    IReadOnlyList<SituationalEdge> Edges;
    int Resolve(IBattleContext atk, IBattleContext def) {  // 纯整数、确定性、无 RNG
        int sum = 0;
        foreach (var e in Edges.OrderBy(e => e.Priority))
            if (PredEval(e.WhenPred, atk, def)) sum += atk.P0 * e.AdjPctNum / 100;
        return sum;  // 调用方再 Clamp(sum, −P0/4, +P0/4)
    }
}
int sitAdj = Clamp(table.Resolve(atk, def), -atk.P0/4, +atk.P0/4);  // 25% 硬闸，防情境链碾压
```

**与旧 CounterMatrix 本质差异**：旧主键含 **PathId**（路线身份）；新 `SituationalEdge` 主键是 **Axis + WhenPred**（情境谓词），**不含任何 PathId**。「剑」与「体」之间没有边——只有「贴脸距离档」「破绽地形」这样的情境边，谁满足谁吃。

**五条情境轴**：

| 轴 | 谓词读取 | 整数修正（示例，标定期收敛） | 世界观语义 |
|---|---|---|---|
| **元素相生克** | 攻/守 `elementTag∈{火,冰,雷,木,土,纯阳,阴煞…}` | 相生克对 +6/−6；纯阳 vs 阴邪 +8；土泄雷 −6 | 火克木、雷破阴——**属性对属性**。法修元素盘降级为此轴一条数据，**不再法修专属克制环** |
| **距离** | `distBand∈{贴脸,近,中,远}` | 贴脸利近战 +5/远程被贴 −5；放风筝远程 +5/近战被风筝 −5 | 「体修>法修(近身)」从硬环改为**距离谓词**——同一对手，距离一变方向就翻 |
| **地形** | `terrainTag∈{平地,险隘,水泽,林莽,己方阵域…}` | 阵域内布阵方 +(2+terrain) 折入 P0/4；险隘伏击 +4；水泽利阴/不利火 ±4 | 「一夫当关」「主场作战」——情境给的，非身份给的 |
| **昼夜** | `isNight 0/1` | 阴系夜战 +（折入 P0/4）/昼战 −；纯阳/雷昼增 | 「昼伏夜出」「烈日灼阴」——时辰是天时 |
| **准备** | `arrayed/ambush/charged/openedFlag 0/1` | 已布阵/蓄势/伏击 +；裸身仓促 −（偷家把 arrayed/bond/blood 置 0） | 「先发制人」「未及结阵」——厚积型路（阵/器/丹/驭兽）开窗与被偷家在此轴 |

谓词 `PredEval` 只读 `IBattleContext`（`DecisionContext` + `CultivationState` 派生），全整数，零新 RNG。距离/地形/昼夜来自地图骨架与世界钟；元素/准备来自 `CultivationState.Flags`（侧表）。绕防（魂修绕物防、命修气运正交、器修落宝）也表达为此表 `WhenPred`——「绕防≠无视战力」，只在 adj 内把对手某防御项按 0 计，**仍受 ±25% 上限**。

**反碾压硬闸（最关键）**：
```
INV-SIT-CLAMP：∀ 战斗，Σ(所有情境轴修正) ∈ [−P0/4, +P0/4]
即便一方同时吃「火克木+夜战+已布阵+贴脸+伏击」，情境总修正封顶 25%。
```

## 6.5 技与道心（变数项，正交于战力/情境）

- **技（战技 OnUse）**：不进基础 EffectivePower，只在 `OnUse` 经 `DamageResolver` 结算整数 delta（剑二十三 `Force×3+swordWill×4` 全或无、燃血、夺运 `NetFortune/3`、万宝 `Σ itemTier×8`…），扣资源经 `ApplyResource` chokepoint。门控 `tier<=realmCap & 资源够 & flag 满足`。「巧」=会不会用、敢不敢倾尽。
- **道心（daoHeart）**：与 `innerDemon`、`comprehension` 构成心性轴，**只进 TribScore/Phase，不进 EffectivePower**（INV-DECOUPLE：corr(daoHeart,Insight)<0.7）。战斗中作用间接但致命：心魔劫 `ResistTerms` 含 `(daoHeart,+w)(innerDemon,−w)`；战力更高但 `innerDemon≥80` 的强者可在战中触发走火打折（`DeviationDebuff` flag 经 EffectOp 施战力惩罚，**不扣四维**）。给「以弱胜强」开第二扇门：**引动强者心魔**。

## 6.6 以弱胜强：合理空间（量化五源 + 随机纪律）

「以弱胜强」必须有真实空间但不廉价。锚定为**五个可叠加整数来源**，全部受上限约束——只在 `gap<=2` 能真翻盘，`gap>=4` 五源齐全也难撼：

| # | 来源 | 机制锚点 | 量级 | 何时奏效 |
|---|---|---|---|---|
| 1 | 境界压制非确定 | CrushGate gap2=×3/4（守方仅削 25%） | 守方保 75% 基数 | 只差 1~2 境时，下面四源足以补回 25% |
| 2 | 情境全占 | SituationalAdj 封顶 ±P0/4（25%） | 最多 +25%自身/对手 −25% | 天时地利人和俱全 |
| 3 | 战技倾尽 | SkillAdj：剑二十三/燃血/夺运一次性爆发 | 路线相关大额（全或无） | 攒够资源、敢梭哈的关键一击 |
| 4 | 引动心魔 | 对手 `innerDemon≥80` → 走火打折 | 对手战力打折 | 强者道心不稳、被逼急或环境恶劣 |
| 5 | 天意爆冷 | BoundedRoll(rng5)：`Roll=NextInclusive(−band,+band)` | ±TRIB_BAND(20) 量级，**有界** | 强者大概率赢但留爆冷尾巴 |

**随机纪律**：战斗随机一律**有界** `BoundedRoll = rng.NextInclusive(−band,+band)`，**无 float 概率**（整数比较 `win ⇔ EffA+Roll >= EffB`）。战斗 roll 取自 **`cultRng=root.Split(5)`** 的战斗子流，子流 id 含 `(atkId,defId,Clock)` 全元组，同种子逐字节复现、Clone 续跑一致（**承 §8.3：cult(5) 须进 World.Clone**）。off 不构造不消费。

### 可程序化锚点（§6）
```
胜负全程整数五段式，无浮点（IL 浮点扫描覆盖战斗路径）
CRUSH_TIER_GAP=2 ; crush 渐进 gap2=3/4, gap3=1/2, gap>=4=1/4（只削守方基数）
SITUATIONAL_CAP = P0/4（情境总修正硬闸 ±25%）；情境 = SituationalEdge 谓词整数边表（加边=加数据 L0）
BoundedRoll band = TRIB_BAND(20) 量级（战斗 roll 有界，取 Split(5) 子流含全元组）
gate：INV-CROSS（同 UT 两路 [40,60]%）/ INV-SIT-CLAMP（情境≤25%）/ INV-WEAK-WIN（gap2 有窗、gap4 无廉价翻）/ INV-POWER-3（gap<GAP 碾压逐字节回滚）/ INV-DET（同种子逐字节+Clone）/ INV-OFF-38（off 坍缩 v1.0）
```

---

# 七 · 纪元与历史背景（History & Eras）

> 锁定决定 (d) 的历史载体。历史 = 「手写 premise 文本（仅 Chronicle flavor，零数值）」 + 「一张 `HistoryConfig` 数据表」 + 「一个生成期 `HistorySeeder`，把数据表确定性播撒成角色 backstory 初始条件」。产出**随机但有上限**的开局态，之后靠三引擎涌现，**历史不再 tick、不锁死后续**。

## 7.1 设计立场：历史必须是「初始条件」而非「时间线」

把历史做成线性剧情会同时违反两红线（不可程序化 + 锁死涌现）。故采**「冻结的过去 + 活的现在」**双相结构：

| 相 | 时间 | 载体 | 是否 tick |
|---|---|---|---|
| **过去（上古/远古）** | 模拟开始之前 | 手写 premise + 已沉淀为整数遗产的锚点 | **否**（只读常量，懒加载） |
| **现在（今世）** | `Clock=0` 起 | 12 路 / Faction / 戏剧三引擎涌现 | 是 |
| **薄纪元时钟** | 贯穿 | `World.Era.EraTension`（软压力）+ `World.Rift`（硬倒计时） | 是（极慢、阈值驱动） |

**关键裁决：过去不是被回放的，是被沉淀的。** 上古大劫不在模拟里「发生」一遍；它**已经发生**，运行期唯一可见形式 = 它留在初始条件里的整数残值（散落古迹节点 / 断绝传承的失落功法 / 跨代恩怨 `Grudge.Generation>0` 种子 / 某区域被打成的灵气贫瘠 buff）——「跨纪元锚点懒加载，只持久化史诗摘要，其余归档丢弃」。

## 7.2 世界编年骨架（三世 + 薄纪元时钟，承 〇.5）

世界纪年分**三世**（`EraIndex` 绑世代，承 〇.5）：
- **上古·神魔纪（`EraIndex=0`，已逝）**：天地初开，神魔为尊，灵气鼎盛、法则完整，UnifiedTier 触顶（曾存 UT11+）。终结于**灭世大劫**（神魔互戕+天倾），灵气从鼎盛跌向稀薄，多数顶阶传承断绝。→ 贡献**古战场锚点 + 失落传承锚点 + 灵气衰减基线**。
- **远古·百圣纪（`EraIndex=1`，已逝）**：人族于神魔真空崛起，百家争鸣、宗门林立、朝代更迭，是当下多数门派/世家/朝廷「祖上荣光」与「世仇起源」来源。终结于**正邪大战/改朝换代级动乱**，重洗格局与气运。→ 贡献**势力起源恩怨 + 气运初始分配 + 师承谱系根**。
- **今世·江湖纪（`EraIndex=2`，运行中，`ReignName="承熹"`）**：`Clock=0` 开局态。灵气处「劫后缓慢恢复」中段（既不鼎盛也非全末法，留成长窗口），格局是远古动乱沉淀的「将临未临」高张力初态。→ **不预写，交三引擎涌现。**

> **避坑**：灵气基线绝不单调到底。三世 `AmbientQi` 曲线 = 鼎盛(上古)→骤跌(灭世劫)→谷底(远古初)→缓升(远古中后)→中段微升(今世)，今世处恢复期上升段，保证开局到长跑都有成长空间。曲线为 per-纪元整数锚点 + 段内线性整数插值，禁浮点。

**薄纪元时钟**：运行期历史唯一持续演化的部分——承 〇.2 拆为两个机制：`World.Era.EraTension[0,10000]`（当世软压力，累积+回落）+ `World.Rift.Integrity[0,1000]`（历史硬倒计时，单调侵蚀）。

## 7.3 历史注入总管道 `HistorySeeder`（生成期一次性，写侧表）

**所有「历史厚度」在 `Clock=0` 之前由 `HistorySeeder` 用 `genRng=Split(1)` 的命名子流一次性播撒完毕**，写既有侧表，然后「过去」冻结。

**接入点（纯加，承批判修正：诚实标注是源码兼容非签名不变）**：
```
WorldFactory.CreateInitial(seed, limits, initialCount, bool history=false, bool geo=false, bool faction=false):
    ... v1.0/地图/Faction 生成 ...
    if history:
        HistorySeeder.Seed(world, genRng.Split(GEN_HISTORY/*=101，Split(1)命名子流*/), historyConfig)
    return world
```
- **承批判修正（off 逐字节不变的真因，非「签名兼容」）**：实读 `WorldFactory.cs:14` 现签名 `CreateInitial(ulong seed, LimitsConfig limits, int initialCount)`，无 feature flag。加带默认值的新形参在 C# 源码层**不是「签名不变」**（重载决议、二进制兼容受影响），只是**「调用点源码兼容」**。`off 逐字节不变`的**真正成立条件**不是「签名兼容」，而是可机器验证的：**off 分支一行不构造任何侧表对象、一条不调用任何新 `Split(n)`、不改变既有 `root.Split(1..4)` 的派生顺序与消费次数**。前者经实读核对成立（`CreateInitial` 现只 `Split(1..4)`），后者依赖实现自律。以现有 `DeterminismTests`（`Same_seed_same_chronicle` / `Snapshot_continue_equals_uninterrupted`）作 off 回归实证锚。
- **不开新顶层 Split 流**：`HistorySeeder` 用 `genRng.Split(GEN_HISTORY)` **子流**（`Pcg32.Split` 跳号派生不消费 root 状态，`Pcg32.cs:58` 实读核定）。子流号在 `RngStreamIds` 登记为 `Split(1)` 命名子流（`GEN_HISTORY=101`），与 1–8 顶层号正交。
- **依赖顺序**：`HistorySeeder` 必须在**地图 + Faction 生成之后**运行（往已存在 Site/Faction 挂锚点）。若 `geo/faction=false` 而 `history=true`，则依赖地图/势力的锚点**优雅降级**为「仅 Chronicle premise flavor」，不静默崩。

**写入目标（全部既有侧表，零新核心字段）**：

| 历史产物 | 写入的既有侧表 | 对应锁定系统 |
|---|---|---|
| 古战场/古迹/秘境分布 | `World.Map` 的 `SiteKind.Secret` + `ResourceNode` + `RelicSite` 侧表（键 NodeId） | 地图系统(b)，历史只**标注**已有 Site |
| 失落传承/断绝功法 | `RelicSite.LostLineageKey`（string 软绑 12 路）+ `World.Map.RevealedSecrets` 初始空集 | 12 路修炼 |
| 势力起源恩怨/世仇 | `FactionLedger._relations[(fA,fB)]`（有向 [-100,100]，承 〇.4） | Faction 系统(c) |
| 气运初始分配 | `Faction.Fortune`（承 〇.3）+ `CultivationState.Resources["fortune"]`（命修轴） | 气运守恒 |
| 师承谱系/隐藏血脉 | `DramaProfile`（`DramaState.Profiles`，含 `Master?/Bloodline?`） | 戏剧系统(d) |
| 跨代未了恩怨 | `GrudgeLedger` 的 `Grudge{Generation>0, InheritedFrom, Cause=Ancestral}` 种子 | 戏剧系统 |
| 区域灾劫余烬 | `World.Map.SiteHazards`（承 〇.6，区域→代表 Site 展开） | 地图危险层 |
| 个体 backstory 标签 | `Persona.Origin`（**复用既有 string 字段**，仅改值不改 schema，经 `with`）+ Memory 高 valence | v1.0 Persona 不动 schema |

**铁律**：`HistorySeeder` **绝不新增 `Character`/`Persona`/`Sect` record 字段顺序**，只写既有侧表与既有可写字段（Persona 经 `with` 产新实例）。

## 7.4 大事件锚点 `HistoryAnchor`（数据表 + 加权抽样）

历史「具体」靠一张**固定容量锚点表**承载。**加一个历史大事件 = 追加一条 `HistoryAnchor` 数据，零改播种器（L0）**——不锁死红线的命门。

```csharp
record HistoryAnchor(
    int Id, HistoryEra Era /*Primordial|Ancient；今世不放锚点*/,
    AnchorKind Kind /*Calamity|LostLineage|Battlefield|Feud|Bloodline|Fortune*/,
    int BaseWeight, int Intensity /*[0,100]，决定残值量级*/,
    IReadOnlyList<AnchorPredicate> Preconditions /*全 AND 整数门控*/,
    IReadOnlyList<AnchorEffect> Effects /*声明意图→HistorySeeder 翻译成侧表写入*/,
    string PremiseTemplate /*仅 Chronicle flavor，绝不进数值路径*/);
AnchorPredicate(AnchorVar Var, CmpOp Op, int Threshold);   // 纯整数比较
AnchorEffect(AnchorEffectKind Kind, int Amount, int Tag);  // 声明式，播种器落地
```
> **schema 复用**：此结构与戏剧引擎 `IStorylet`（Id/Preconditions/Effects/Weight/Template）**同构**——历史锚点本质是「在 `Clock<0` 触发一次的 storylet」。共用 `WeightedPicker` 整数轮盘、`AnchorPredicate` 求值器、Chronicle 模板渲染层，减少新引擎面。

**锚点容量与「随机有上限」三重门控（`HistoryConfig`，init-only，`Validate()` 守门）**：
```
MaxAnchorsTotal / MaxAnchorsPerKind[] / MaxAnchorsPerRegion / MaxFeudGeneration(=GrudgeLedger.MaxGeneration=3)
RelicPerRegionCap / FortuneTotalBudget / AnchorIntensityCap
```
**抽样算法（确定性、整数、随机有上限）**：
```
1 候选 = 所有 Preconditions 全满足的 HistoryAnchor（对已生成地图/Faction 快照求值）
2 候选按 (BaseWeight desc, Id asc) 稳定排序   // 禁 Dictionary/HashSet 枚举序参与裁决
3 while picked.Count < MaxAnchorsTotal 且候选非空:
     a = WeightedPick(候选, historySubRng)     // 前缀和 + 整数轮盘
     if 违反 MaxAnchorsPerKind/MaxAnchorsPerRegion/RelicPerRegionCap: 跳过（不重抽偏置）
     else: 落地 a（翻译 Effects→侧表写入），从候选移除（OncePerWorld 语义）
4 所有数量先 clamp 到 caps 再用——硬上限优先于随机
```
- 气运走**总预算守恒**（`FortuneTotalBudget`，分配即扣减，不凭空增发）；恩怨世代 clamp `MaxFeudGeneration` 防无限链；锚点强度 clamp `AnchorIntensityCap` 防初始数值爆表。

**六类锚点 → 侧表落地细则**：
- **(A) Calamity 灭世大劫** → 写 `World.Era.AmbientQi` 初始（劫后恢复段整数起点）；在 `Intensity` 决定的若干区域写**灾劫区 buff**（`World.Map.SiteHazards{Kind,Intensity}`，承 〇.6 区域→代表 Site 展开）。解释「为何当世灵气只到恢复中段」「为何某区域至今凶险」。
- **(B) LostLineage 失落传承** → 选 `SiteKind.Secret` Site 挂 `RelicSite{LostLineageKey:string, Grade, EntryGate}`。`LostLineageKey` 软绑 12 路某 path 某功法类目（string，加新路新功法=加 key L0）。运行期：今世角色游历到该 Site 且过 `EntryGate` → `EnterSecret`/`HarvestAction` → 拾得→经 `ApplyResource` 喂修炼（A 在位），产 `LostLineageRecovered`。**这是「过去」与「现在」的接口**：古迹冻结，谁去拾/何时拾/引发什么由涌现决定。
- **(C) Battlefield 古战场** → 选 Site 挂 `RelicSite{Kind=Battlefield, Grade}` + `World.Map.SiteHazards`，同时挂指向远古某方的 `Grudge` 溯源（`Cause=Ancestral, OriginTick=负数, InheritedFrom=锚点Id`）。古战场是「高风险高回报」资源点 + 跨代恩怨地理坐标。
- **(D) Feud 势力起源恩怨** → 写 `FactionLedger._relations[(fA,fB)]` 为 `Intensity` 映射负值（承 〇.4 钳 [-100,100]）。可选在代表角色间预置个体 `Grudge`（喂戏剧首刀必有复仇线）。**恩怨写关系矩阵 + 可选个体 Grudge，不写死善恶布尔**——Faction 仍是中性 `AlignmentAxis` + 关系值。解释「为何 X 门与 Y 门世代为敌」，**不预写谁赢**。
- **(E) Bloodline 隐藏血脉** → 选若干初始角色，在 `DramaProfile` 写 `Bloodline=锚点Id`，附带生成期成长曲线偏置（`CultivationState` 初始 `rootQuality` 加成）。血脉多实例，可被「血脉觉醒」storylet 触发。先天加成 clamp，绝不破 UnifiedTier 同台当量（INV-CROSS）。
- **(F) Fortune 气运初始分配** → 从 `FortuneTotalBudget` 按 `Intensity` 加权分配 `Fortune` 给 Faction 与个别角色（命修气运轴）。**分配即扣预算，总量守恒。** 气运可被掠夺/转移/因大战归零，绝非纯增益。

## 7.5 气运系统：可争夺、可转移、守恒（历史给初值，涌现重分配）

> 承 〇.3：势力气运 = `Faction.Fortune`；个体气运 = `CultivationState.Resources["fortune"]`。

```
全局守恒账：Σ(所有 Faction.Fortune) + Σ(角色 fortune) + 未分配池 == FortuneTotalBudget + 涌现增发 − 涌现损耗
```
- **掠夺/转移**：Faction 战争胜方夺败方 `Fortune`（源减目标增守恒），产 `FortuneTransferred`。命修「夺运」技有完整 Backlash（夺气运高于己者反弹自身 Karma/折寿），直接复用。
- **大战归零**：纪元级全局清算（§7.6）可批量重置/转移气运（大战重洗格局），刷新老牌垄断。
- **增发与损耗配平**：若引入「信徒/辖地产气运」（凭空增发），**必配衰减项** `Fortune -= Fortune*decayPct/100`。
- 气运设 `FortuneCap` 硬上限 + 反制项（命修 Backlash 天然反制：强夺命大者自险）；**气运不进 PowerEngine 主战力**（境界×per-path 主导，气运经命修 path 的 adj 在 ±P0/4 闸内影响战斗，不破软情境哲学）。

## 7.6 薄纪元时钟演化（`EraTension` 推进的确定性配方）

> 承批判修正：草案 `EraTension += f(战争数,顶阶,气运极化度)` 未规定聚合遍历确定性顺序与性能上界。本文补全。

`World.Advance` 末尾按 `EraTickInterval` 逻辑 tick 节流（**不每 Advance 全扫**），演化：
```
每 EraTickInterval tick（确定性整数纯函数，遍历按 Id 升序）：
  warCount   = FactionLedger._ties 中 Kind==War 的计数（按 (fa,fb) Id 升序遍历）
  apexCount  = Alive 中 UnifiedTier >= APEX_UT 的计数（按 CharacterId 升序遍历）
  polarize   = 整数化气运极化度 = maxFortune − medianFortune（Faction.Fortune 按 Id 升序收集后整数排序取中位与最大；并列按 Id 定序；无除法，纯减法）
  EraTension += (warCount*W_WAR + apexCount*W_APEX + polarize*W_POL/100)   // clamp [0,10000]
  if EraTension >= NextCalamityGate 且 Clock − LastCalamityTick >= MinCalamityGap:
      触发「纪元级全局清算事件」：EraTension 部分回落，EraIndex++，AmbientQi 按曲线进下一段
      气运重分配 + 超龄超阈 actor 淘汰刷新（保留古迹/遗物锚点做跨纪元钩子，不清空全部状态）
      LastCalamityTick = Clock
```
- **承批判修正（确定性配方 + 性能上界）**：各聚合项定义明确（战争数=War tie 计数、顶阶=`UnifiedTier≥APEX_UT` 的 Alive 计数、极化度=`maxFortune−medianFortune` 整数化并定死并列/截断规则）；**遍历一律按 Id 升序**；节流周期 `EraTickInterval`。**性能不变量 INV-ERA-PERF**：每次推进 `O(factions + alive)`，每 `EraTickInterval` tick 一次（非每 Advance）。
- **承批判修正（清算复用 RNG 流）**：纪元清算若需随机（淘汰名单、气运重分配端点），**复用 `domainRng=Split(2)`**（v1.0/v1.1 保留给「世界级 domain 事件」的流，纪元清算正是其语义归属，零编号冲突）。`domainRng` 实读核定是 World 字段且进 Clone 深拷（`World.cs:27,161`），故清算随机端点 **Clone 续跑可复现**（满足 §8.3）。
- **避坑**：`ERA_TENSION_CAP=10000` 硬上限 + 清算后回落，杜绝正反馈失控；`MinCalamityGap` 防清算过频；清算**不强制清空全局状态**，只气运重分配 + 超龄超阈 actor 刷新。

### 可程序化锚点（§7）
```csharp
// 历史 = 手写 premise（零数值 Chronicle）+ HistoryConfig 数据表 + 生成期 HistorySeeder（写既有侧表）
// 三世 AmbientQi 曲线 = per-纪元整数锚点 + 段内线性整数插值（鼎盛→骤跌→谷底→缓升→今世微升）
// HistoryAnchor 六类（Calamity/LostLineage/Battlefield/Feud/Bloodline/Fortune），加事件=加数据行（L0）
// 抽样：候选按 (BaseWeight desc, Id asc) 稳定排序 + 前缀和整数轮盘 + 三重 clamp（数量/强度/分布）
// genRng.Split(GEN_HISTORY=101) 命名子流（生成期一次性）；纪元清算复用 domainRng=Split(2)（已进 Clone）
// EraTension 推进：O(factions+alive)，每 EraTickInterval tick 一次，遍历按 Id 升序，极化度=max−median 整数化
// DomainEvent 纯加：LostLineageRecovered / FortuneTransferred / EraCalamity / BloodlineAwakened
```

---

# 八 · 与已锁系统的接口（System Interfaces & Determinism）

> 本章是 World Bible 对实现层的硬契约，集中钉死跨系统接缝、确定性前提与 RNG 流持久化。**这是确定性红线的最硬承诺。**

## 8.1 侧表挂载总表（一切新态零进 v1.0 核心 record）

| 新态 | 容器 | Clone 待遇 |
|---|---|---|
| 全局灵气/软压力 | `World.Era`（EraState，承 〇.1/〇.2/〇.5） | 拷一组 int + 两 string |
| 道枢倒计时 | `World.Rift`（RiftClock） | 拷一组 int |
| 地图拓扑（只读） | `World.Map.WorldMap`（GeoRec/Adjacency/前缀和） | 浅拷安全（gen 后只读） |
| 地图可变态 | `World.Map` 独立侧表：`Resources`(含 Reserve)/`SiteHazards`/`RevealedSecrets`/`SecretCycleState`（承 〇.6 + 批判修正） | **深拷清单** |
| 势力（含 Fortune） | `World.Factions`（FactionLedger） | 深拷清单（Faction.Clone 带 Fortune） |
| 个体修炼/道心/气运 | `CultivationState.Resources`（侧表） | 随 Character.Clone 深拷 |
| 戏剧（恩怨/师承） | `DramaState`（GrudgeLedger/Profiles） | 深拷清单 |
| 历史古迹 | `World.Map.RelicSite`（键 NodeId） | 深拷 |

**核心 record 一字不改**：`StatBlock`（4 维）/`Character`/`Persona`/`Sect`/`WorldNode` 字段顺序不变（实读 `StatBlock.cs:11`、`SparAction.cs`、`WorldFactory.cs` 核定）。`Persona.Origin` 仅改值不改 schema（经 `with`）。

## 8.2 RngStreamIds 单一真相源（append-only 冻结）

```
1=gen 2=domain 3=spawn 4=brain 5=cult 6=drama 7=map 8=faction   （顶层，1-8 满且冻结）
命名子流：GEN_HISTORY=101（属 Split(1) 生成期），灵气/地图态→map(7)，气运/势力→faction(8)，
         纪元张力/纪元清算→domain(2)，修炼/道心/战斗→cult(5)
off 分支一律不构造不消费对应流 → INV-OFF-38 逐字节不变
```
- **不开新顶层 Split 号**：premise/era/rift 推进按 `World.Clock` 逻辑时间节流，纯整数演进，**不消费决策 RNG**；其触发的随机事件复用 map(7)/faction(8)/domain(2) 既有流。
- 历史用 `genRng.Split(101)` 命名子流（`Pcg32.Split` 跳号派生不消费 root，`Pcg32.cs:58` 核定）。

## 8.3 RNG 流持久化前提（承 major 批判：Clone 续跑可复现的硬要求）

> **批判核心**：实读 `World.cs` 发现**只有 `_domainRng`(Split2) 与 `SpawnRng`(Split3) 是 World 字段并进 `Clone` 深拷**（`World.cs:27-28,161-165`）；`genRng`(Split1)/`brainRngBase`(Split4) 是 `CreateInitial` 局部变量，消费后丢弃，**不进 Clone**。若 premise/rift/fortune Pump 消费 map(7)/faction(8) 流，而这两条流像 genRng 一样是局部派生流，则 **Clone 续跑后流状态丢失，秘境位置/气运转移端点不可复现**，直接破 INV-DET / INV-RIFT-BOUNDED 的「Clone 续跑一致」。

**硬契约（必须满足，否则确定性不成立）**：
```
INV-RNG-PERSIST：凡被 premise / rift / fortune / era / map / faction / cult Pump 在【运行期】消费的 Split 流，
                 必须作为 World 字段持久化、并进 World.Clone 的 GetState/SetState 深拷（与 _domainRng/SpawnRng 同等待遇）。
                 断言：凡 premise 路径运行期消费的子流，Clone 前后 GetState 逐字节一致。
```
- **落地要求**：map-on / faction-on / cult-on / premise-on 时，`mapRng`(Split7)、`factionRng`(Split8)、`cultRng`(Split5) 必须像 `_domainRng` 一样**升格为 World 字段并进 Clone**（不可像 genRng 那样消费后丢弃）。
- **纪元清算 / 道枢碎片秘境 / 气运转移随机端点**：明确归 `domainRng`(Split2)——它**已经**是 World 字段且进 Clone（`World.cs:27,161` 核定），故这些随机端点 Clone 续跑天然可复现，是**最安全的归属**。
- **澄清**：「不消费决策 RNG 的纯 Clock 推进」只覆盖 `EraTension`/`AmbientQi`/`Integrity` 标量自增（这些确实无 RNG）；它们**触发的随机事件**（秘境 roll、淘汰名单）必须落到已持久化的流（优先 domain(2)）。
- **若架构是「每 Advance 由 root 重新 Split 派生」**：则 root 本身须进 Clone，且派生须在 Clone 续跑点重建到同一状态——本文不采此路（root 当前不进 Clone），统一要求「运行期消费的流升格为 World 字段」。

## 8.4 全局软上限旋钮 `AmbientQi` 的特例正名（承 major 批判：解耦纪律的显式例外）

> **批判核心**：`AmbientQi` 是全局可变标量，被「修炼侧渡劫公式」跨系统读取以抬高 `ThreatPenalty`，软压制全谱高 UnifiedTier。这是一条隐式「全局写→读耦合通道」，与「系统间只经只读接缝不互 mutate」纪律抵触，且逼近「天道别做成模糊全局变量」的坑（只是改了名加了 clamp）。须当成**显式架构决策**而非顺带提及。

**显式正名（边界硬约束）**：
- `AmbientQi` 是**唯一全局软上限旋钮**，特例地位明确登记。它**只能经一个 well-defined 只读接缝**喂入修炼侧：`IEnvQuery.GetRealmCapHint() → QiToTierTable[AmbientQi/100]`，且**只进 `TribScore` 的单一加项 `ThreatPenalty`**，不得旁路、不得进任何其他公式。
- **per-path 战力零侵入不变量 INV-QI-NOLEAK**：`AmbientQi` / `RealmCapHint` **只进 TribScore/ThreatPenalty，绝不进 `PerPathEffectivePower`/`EffectivePower`**（战斗主战力与 `AmbientQi` 解耦）。可证伪：固定双方 per-path 输入，仅变 `AmbientQi`，`EffectivePower` 逐字节不变、只有渡劫 `ThreatPenalty` 变。
- **为何不退化为「模糊全局变量」**：① 它是**确定性整数查表**（`QiToTierTable[AmbientQi/100]`），非浮点黑箱；② 可证伪「同 `AmbientQi` 同 `ThreatPenalty`」（纯函数，无隐藏状态）；③ 它**只调渡劫难度斜率（软上限），不锁个体、不进战力、不解释一切**——与被警告的「万能气运/模糊天道」划清界限。
- **它是 12 路独立曲线纪律的一个显式例外口子**：12 路渡劫公式各自 `ResistTerms` 不同，但都**共享同一个 `ThreatPenalty` 全局加项**。此例外被正名为「唯一全局难度旋钮」，并由 INV-QI-NOLEAK 限制其侵入面到「仅 TribScore 单加项」。

## 8.5 与各已锁系统对齐声明（无冲突核对）

- **12 路修炼**：premise/历史不偏袒任一路；灵机下行经 `ThreatPenalty` 平等作用所有路渡劫（per-path `TribulationDef` 自取 `ResistTerms`）。锚点（剑墟/铁佛寺/百蛊渊）只是初始分布偏置，非路线绑定。
- **三轴解耦**：`RealmCapHint` 是 UnifiedTier 软上限提示，不碰 flatIndex 编码、不碰 (MajorTier,SubLevel) 投影；问枢资格读 UnifiedTier 跨路可比刻度——正是三轴解耦用途。
- **软情境战斗(a)**：灵机/气运修正都是「境界×战力主导 + 软%修正」里的软项（喂 `ThreatPenalty`/`BreakAid`），绝不引入硬 path-vs-path 克制环；夺枢战争用 `FactionDirector` 非致死 `TerritoryLost` + CRUSH_TIER_GAP 渐进碾压。
- **固定骨架地图(b)**：手写命名 Region/地标锚点固定，秘境/资源/坊市/散修程序随机（mapRng=Split7）；历史不生成地图，只在已生成 Site 打锚点。
- **全谱 Faction(c)**：六大势力 + 程序生成全走统一 `FactionDef` + `AlignmentRelationTable`，势力间恩怨/联盟/战争/气运统一建模；正邪是中性 `AlignmentAxis` 标签 + 关系值。
- **编撰背景+涌现(d)**：premise + 初始大势手写底色；Stage 3 终局槽 + 耦合点只给方向不给轨道，剧情涌现。
- **辅助实体抽象整数化**：商会/丹修等的气运/灵石/Treasury 均为势力侧整数维（`Faction`/`CultivationState`），不建完整物品/经济子系统——对齐「御兽=兽群强度值、丹=丹药储备乘子」。
- **确定性/侧表/数据纪律**：全文侧表挂载、纯整数、Clock 节流、生成期四维 Σ=80 不受影响（premise 不碰 StatBlock）、DomainEvent 单源纯加 case。

### 可程序化锚点（§8）
```
INV-RNG-PERSIST：运行期消费的 Split 流（map7/faction8/cult5/domain2）必须进 World.Clone 深拷；Clone 前后 GetState 逐字节一致
INV-QI-NOLEAK：AmbientQi/RealmCapHint 只经 IEnvQuery 只读接缝进 TribScore.ThreatPenalty 单加项，零进 EffectivePower
off 逐字节不变真因：off 分支不构造任何侧表、不调用任何新 Split(n)、不改 root.Split(1..4) 派生顺序与消费次数（非「签名兼容」）
实证锚：DeterminismTests.Same_seed_same_chronicle / Snapshot_continue_equals_uninterrupted
核心 record 不改：StatBlock(4维)/Character/Persona/Sect/WorldNode 字段顺序不变（实读核定）
```

---

# 九 · 可扩展性与红线自检（Extensibility & Red-Line Audit）

## 9.1 扩展能力诚实分级（承 〇.7：不对「小改核心」谎称「零改」）

> **关键诚实**：红线②判据是机器可验的「零改核心」。本文把扩展分两级，**L1 明确承认是「小改核心」，不挂「零改」标签**。

| 扩展操作 | 级别 | 改动范围 | 核心代码改动 |
|---|---|---|---|
| 加新势力（如「少林寺」Sect） | **L0** | `CodeFactionSource` 追加 1 行 `FactionDef` | **零** |
| 加新地区/州 | **L0** | `GeoCanon` 追加 `RegionDef`+`RegionEdge` 行 | **零**（BFS 断言自动校验） |
| 加新地标 | **L0** | 追加 `LandmarkDef` 行 | **零** |
| 加新资源种类 | **L0** | 追加 `resourceKey` + map 映射数据 | **零**（且开闭验收必含异构：加新 key 并验它被某路消费） |
| 加新秘境型 | **L0** | 追加 `SecretArchetype` 行 | **零** |
| 加新情境轴/边 | **L0** | `SituationalTable.Edges` 追加 `SituationalEdge` + 注册谓词 key | **零** |
| 加新历史大事件 | **L0** | 追加 `HistoryAnchor` 行 | **零** |
| 加新身份层（半妖/器灵） | **L0**（已数据化，承 〇.7） | 注册谓词 key + 追加 `IdentityRule` 行 | **零**（§3.1 改为表驱动后，不再需加派生分支） |
| 加新阵营立场 | **L0/L1** | 调 `AlignmentAxis` 值（L0）；若加新 `AlignmentAxis` 枚举值则 L1 | L0 零 / L1 加枚举值 |
| 加新天道子系统 | **L0** | 追加挂侧表整数标量 | **零** |
| 加新 `FactionType`（如海盗） | **L1（小改核心，诚实标注）** | 加枚举值 + 加 1 行 `FactionTypeProfile` | **加枚举值**（引擎读表无 type 分支，但枚举是代码改动） |
| 加新结构关系 `TieKind`（如质子盟约） | **L1（小改核心）** | 加 `TieKind` 枚举值 + 转移阈值入 `LimitsConfig` | **加枚举值 + 加阈值**（框架不动但枚举是代码改动） |
| 加新元素标签 | **L0** | 追加谓词常量 + 元素相生克表行 | **零** |
| 加新历史/新大势 | **L0** | `WorldFactory` 初始注入数据调整 | **零核心** |

> **断言**：纯数据扩展（L0：势力/地区/历史锚点/情境边/资源 key/秘境型/身份层/天道子系统）确为**零改核心**；有限枚举扩展（L1：`FactionType`/`TieKind`/新 `AlignmentAxis`）是**小改核心**，本文不谎称零改。身份层经 §3.1 数据化后从 L1 退回 L0。

## 9.2 不变量总表（交 auditor gate）

| 不变量 | 判据 |
|---|---|
| **INV-OFF-38** | premise/geo/faction/cult/history-off（默认）时各侧表不构造、各 Pump no-op、不消费 RNG → v1.0 + v1.2 既有 38 测试逐字节不变 |
| **INV-WORLD-PURITY** | 新增态（灵气/天道/气运/道心/法则位/身份层/地图/势力/历史）零进 v1.0 核心 record；StatBlock/Character/Persona/Sect/WorldNode 字段顺序不变 |
| **INV-STAT-CONTRACT** | 核心四维仅 Force/Internal/Constitution/Insight；生成期 Σ=80（INV-STAT-GEN）+ 运行期单维 clamp `[0,StatCap]`（INV-STAT-CAP）；不引入第五核心维 |
| **INV-TIER-CEILING** | UnifiedTier 硬封顶 12（不可达占位）；武夫封顶 UT9 老死、修士 UT11 飞升离场，无顶层无限延伸 |
| **INV-TAO-BOUNDED** | 天道子系统（法则位/EraTension/Rift/气运）全整数有界；气运夺取守恒、增发必配衰减、有硬上限与反制项 |
| **INV-QI-BOUNDED** | `World.Era.AmbientQi ∈ [QiFloor,1000]`（承 〇.1），自然下行 clamp≥QiFloor、局部回升 clamp≤1000，不爆不归零 |
| **INV-TENSION-BOUNDED** | `EraTension ∈ [0,10000]`（承 〇.2，累积+回落，清算后部分回落） |
| **INV-RIFT-BOUNDED** | `Integrity ∈ [0,1000]`、`Stage ∈ [0,3]`（承 〇.2，单调侵蚀/单调不减）；三阈值各触发恰一次 `RiftCracked`（去重） |
| **INV-FORTUNE-CONSERVE** | 气运 Transfer 守恒（源减=目标增）；Source/Sink 后恒 `∈[0,FortuneCap]`；长跑无通胀（Sink 兜底）；势力气运归 `Faction.Fortune`、个体归 `CultivationState.Resources["fortune"]`（承 〇.3） |
| **INV-RELATION-RANGE** | 势力/角色关系恒钳 `[-100,100]`（承 〇.4） |
| **INV-NO-SCRIPT** | Stage 3 只开放终局槽，内核不写入任何预定胜负；问枢结果由当时 agent 状态生成（可证伪：mock 不同初态产不同终局） |
| **INV-RNG-PERSIST** | 运行期消费的 Split 流（map7/faction8/cult5/domain2）必须进 World.Clone 深拷；Clone 前后 GetState 逐字节一致（承 §8.3） |
| **INV-QI-NOLEAK** | `AmbientQi`/`RealmCapHint` 只经 `IEnvQuery` 只读接缝进 `TribScore.ThreatPenalty` 单加项，零进 `EffectivePower`（承 §8.4，可证伪：仅变 AmbientQi，EffectivePower 逐字节不变） |
| **INV-CROSS** | 同 UnifiedTier 任意两路 1v1（情境中性）胜率 ∈ [40%,60%] |
| **INV-SIT-CLAMP** | ∀ 战斗，Σ(所有情境轴修正) ∈ [−P0/4, +P0/4]（情境总额 ≤25%） |
| **INV-WEAK-WIN** | gap=2 弱方集齐（情境满+战技+对手心魔）胜率>0（窗口存在）；gap≥4 集齐五源胜率仍<阈（碾压不可廉价翻） |
| **INV-POWER-3** | gap<CRUSH_TIER_GAP 时碾压 no-op，结果逐字节回滚到纯 per-path |
| **INV-GEO-CONNECTED** | 区域骨架（忽略门控）BFS 全连通无孤岛；区内随机段接入后区内 BFS 连通，fail-fast |
| **INV-GEO-NODEID** | `Site.Id` 在 `NodeId` 连续 `0..N-1`；固定段在前、随机段在后；`RegionOf` 二分正确 |
| **INV-GEO-CAP** | 秘境≤MaxSecrets、坊市≤MaxMarkets、每 resourceKey≤ResourceCap、degree≤MaxDegree（硬上限优先于随机，先 clamp 再用） |
| **INV-GEO-REPLAY** | map-on 同种子→同图（逐字节）；`World.Clone` 续跑后地图可变态（Resources.Reserve/SiteHazards/RevealedSecrets/SecretCycleState）逐字节复现（承 〇.6 + 批判修正：可变态从只读 WorldMap 剥离进深拷清单） |
| **INV-GEO-GATE** | 跨区/进秘境门控用 UnifiedTier(0-12) 硬阈值整数比较 |
| **INV-GEO-DECOUPLE** | 地图不引用 `Jianghu.Faction`（编译期断言）；资源/路绑定只经 string key；地图态不进 `Sect` 单例、不改核心 record |
| **INV-GEO-SOFTONLY** | 地图喂战斗的全是情境修正（元素/地形/昼夜/距离），经 ±P0/4 总修正上限；无任何 path-vs-path 硬克制编码 |
| **INV-ERA-PERF** | `EraTension` 推进每次 `O(factions+alive)`，每 `EraTickInterval` tick 一次（非每 Advance）；聚合遍历按 Id 升序、极化度=max−median 整数化定序（承 §7.6 批判修正） |
| **INV-FLOAT** | Jianghu 内 premise/geo/cult/faction/history 相关代码零浮点（IL 扫描覆盖各新建命名空间，不假装复用既有引用程序集扫描） |
| **INV-EXTEND-OPEN** | L0 扩展（加层/势力/路线/历史/情境维/资源 key/秘境型）= 追加数据零改核心；L1 扩展（FactionType/TieKind/AlignmentAxis）= 加枚举值（诚实标注小改核心，承 〇.7/§9.1） |

## 9.3 两条红线最终落点自检

**红线①·可程序化（确定性整数 + 有上限随机）**：
- 全局标量（AmbientQi/EraTension/Rift/Fortune）全整数有界（〇.1/〇.2/〇.3）；天道三拆全整数（§1.2）。
- 战斗五段全整数无浮点（§6.2）；情境=谓词整数边表（§6.4）；随机=有界 roll（§6.6）。
- 历史=数据表 + 确定性整数播种 + 三重 clamp（§7.4）；纪元推进有确定性配方 + 性能上界（§7.6/INV-ERA-PERF）。
- **确定性前提补全（承批判）**：运行期消费的 RNG 流强制进 Clone（INV-RNG-PERSIST，§8.3）；WorldMap 可变态从只读体剥离（INV-GEO-REPLAY，〇.6）。

**红线②·不锁死（开闭可扩展）**：
- L0 纯数据扩展确为零改核心（§9.1）；身份层数据化消除 L1 例外（§3.1/〇.7）。
- L1 有限枚举扩展诚实标注「小改核心」，不谎称零改（§9.1/〇.7）。
- 废硬克制环：加路线在战斗层是 O(0)（路线不出现在任何克制边，§6.1）。
- premise 给底色不写死中心剧情线（INV-NO-SCRIPT，§2/§7）；`EraIndex` 留上行不可达占位给未来纪元。

---

# 十 · 一句话世界 premise（供 agent 涌现的底色）

> **九野**：一界两层，灵机退潮。薄灵的中原庙堂忌惮厚灵的修真宗门，正魔为残存灵脉而争，世家以气运续门楣，武夫争老死前最后的机缘，修士忧末法断了飞升路。天道未崩、大劫未至、第一之位空悬——**这是一个「将临未临」的江湖，谁主沉浮，由身在其中者自己写下。**

premise 给四组大势的初始名号与数值底色（灵机下行时钟 / 道枢裂痕倒计时 / 气运争夺 / 师承恩怨）；具体恩怨、谁崛起谁陨落、哪座宗门兴衰、哪个武夫越级斩杀——全部由 agent 在确定性整数机制下涌现，World Bible 不预写一条中心剧情线。**手写底色，涌现剧情；可程序化，不锁死。**

---

> **相关源文件锚点（绝对路径，供 writing-plans / implementer 追溯）**：
> - 设计规格（v1.0 主）：`D:\AgentWorkStation\Any\武侠人设生成\docs\superpowers\specs\2026-06-13-武侠人设生成-江湖涌现模拟内核-design.md`
> - 修炼三轴/全流程 FINAL：`D:\AgentWorkStation\Any\武侠人设生成\docs\superpowers\specs\2026-06-13-v1.2-A2-修炼大小境界与全流程-FINAL-design.md`
> - 破单调奇遇闭关道心 FINAL：`D:\AgentWorkStation\Any\武侠人设生成\docs\superpowers\specs\2026-06-13-v1.2-A3-破单调奇遇闭关道心-FINAL-design.md`
> - 地图+门派（Geo/Faction）：`D:\AgentWorkStation\Any\武侠人设生成\docs\superpowers\specs\2026-06-13-v1.2-C-江湖地图与门派系统-design.md`
> - 戏剧引擎（GrudgeLedger/复仇弧）：`D:\AgentWorkStation\Any\武侠人设生成\docs\superpowers\specs\2026-06-13-v1.2-B-戏剧引擎-design.md`
> - 修炼路线注册表：`D:\AgentWorkStation\Any\武侠人设生成\docs\superpowers\specs\2026-06-13-v1.2-A-修炼路线注册表-design.md`
> - PRNG 实现（Split 派生，实读核定）：`D:\AgentWorkStation\Any\武侠人设生成\src\Jianghu.Core\Random\Pcg32.cs`
> - World（Clone/RNG 持久化现状，实读核定 §8.3）：`D:\AgentWorkStation\Any\武侠人设生成\src\Jianghu.Core\Sim\World.cs`
> - WorldFactory（CreateInitial 签名/Split 流，实读核定 §7.3）：`D:\AgentWorkStation\Any\武侠人设生成\src\Jianghu.Core\Sim\WorldFactory.cs`
> - 关系钳制 [-100,100]（实读核定 〇.4）：`D:\AgentWorkStation\Any\武侠人设生成\src\Jianghu.Core\Model\Relations.cs`
> - 四维契约（实读核定）：`D:\AgentWorkStation\Any\武侠人设生成\src\Jianghu.Core\Stats\StatKind.cs`、`...\Stats\StatBlock.cs`、`...\Config\LimitsConfig.cs`
> - 战斗 off 口径（实读核定 §6.2）：`D:\AgentWorkStation\Any\武侠人设生成\src\Jianghu.Core\Actions\SparAction.cs`
> - 核心 record（本文不改之）：`D:\AgentWorkStation\Any\武侠人设生成\src\Jianghu.Core\Model\Persona.cs`、`...\Model\Sect.cs`、`...\Model\WorldNode.cs`
>
> **RngStreamIds 落地提示**：本文 premise/era/rift Pump 不占新顶层 Split 号（用 domain(2)/map(7)/faction(8)/cult(5) 既有流 + GEN_HISTORY=101 命名子流）；developer 须在 `RngStreamIds` 常量类核对 5–8 真实占用、并按 INV-RNG-PERSIST 将运行期消费的 map(7)/faction(8)/cult(5) 升格为 World 字段进 Clone 后，再固化含 premise 的黄金轨迹。
