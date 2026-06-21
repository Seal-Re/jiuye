# Game Concept: 九野 · 江湖涌现模拟

> **Status**: Designed
> **Layer**: Foundation
> **Source**: WorldBible canonical (`docs/legacy-specs/specs/2026-06-13-WorldBible-九野-canonical.md`) + 设计规格 (`...武侠人设生成-江湖涌现模拟内核-design.md`)
> **Created**: 2026-06-21（逆向自 canonical + design spec）

---

## 1. Overview

**九野**是一个武侠江湖的**涌现模拟内核**——程序化生成武侠角色，让角色在开放式连续时间中按各自节奏自主行动，产出可读的"江湖编年史"。Core 为纯 C# 逻辑库（netstandard2.1），CLI 驱动；后期 Unity 直接引用 Core 作为渲染/玩家介入宿主。

核心支柱：**确定性整数 PRNG（Pcg32）** → 同种子逐字节复现；**事件驱动开放式调度器** → 无固定回合数；**21 条修炼路径**全入册（剑仙/体修/法修/阵修/丹修 等）；**RuleBrain 效用机**常驻（LLM 脑 v1.1 待建）；**角色生老死 + 新人涌现**维持江湖代谢。

世界观基底：《九野》——末劫将临的修真世界，灵气渐衰、道枢欲裂。九块大陆（中央天极 + 八荒），宗门林立，正邪争锋。

---

## 2. Player Fantasy

**当前（v1 涌现模拟）**：玩家是"江湖观察者/编剧"——看 NPC 按各自功法路数自动厮杀、修炼突破、结仇复仇。涌现出剑仙一击破百、毒修阴蛊缠身、体修硬抗反震的武侠桥段。战斗爽感来自**门派各异的质感**和涌现的**戏剧性**。

**后期（Unity 玩家介入）**：玩家操控角色，出招是回合策略（选招/调度资源），关键时刻的闪/格/打断是即时按键——回合谋略 + 即时反应的复合快感。

---

## 3. Detailed Rules

### 3.1 世界架构
- **九野**：中央天极 + 八荒（东/南/西/北/东南/东北/西南/西北），每块大陆有若干区域（Region），区域含若干地点（Site/NodeId）
- **灵气体系**：全局 `AmbientQi ∈ [0,1000]`（决定可达境界软上限）+ 局部 `Region.QiDensity ∈ [0,100]`（修炼效率修正）
- **道枢倒计时**：`RiftClock.Integrity ∈ [0,1000]` 单调侵蚀，跌破 500/250/0 触发三段世界事件
- **大势张力**：`EraTension ∈ [0,10000]` 非单调累积/清算

### 3.2 角色系统
- 四维属性：Force（武力）/ Internal（内功）/ Constitution（根骨）/ Insight（悟性）
- 生成期 Σ=80，单维 Cap=30，Min=5；运行期单维 Apply 钳 `[0,Cap]`，不守和
- 角色含 Persona（姓名/性别/性格）、Relations（有向 `[-100,100]`）、MemoryStore（记忆条目）
- 生老死循环：`Lifecycle.Tick` 按年龄→寿元判定死亡；`MaybeSpawn` 按人口阈值创造新人

### 3.3 动作系统
- 事件驱动调度器（Scheduler，min-heap by NextActAt）
- 动作枚举：SparAction（切磋/战斗）、TrainAction（修炼）、TravelAction（游历）
- ActionSystem 校验 → 执行 → 生成 DomainEvent → 编年史追加
- 所有动作确定性（RuleBrain 效用机，种子驱动）

### 3.4 战斗系统（结算层）
- 速度序列 CTB：speed 决定行动频率
- 操作类型：进攻/防御/控制/资源/签名
- 效果经 Modules 工厂（普通/稀有）+ SpecialModuleRegistry（唯一）积木化组合
- 纯整数确定性结算（B.2）；防御含运气/护体真气/招架/结界/闪避概率档
- UT gap ≥ 2 auto-win

### 3.5 修炼系统
- 21 路径，每路含：Resources（修炼资源）、RealmCurve（境界曲线）、PowerEngine 公式
- 突破机制：CultivationPoints 累积 → RealmIndex 进阶 → UT（UnifiedTier 0-12）
- 功法门控：防御能力由所修功法决定（HasMovementArt/HasBodyArt）

---

## 4. Formulas

| 公式 | 定义 | 值域 |
|---|---|---|
| `StatSum = Σ stats` | 生成期四维和 | 固定 80 |
| `Power (off) = Force×2 + Internal + Constitution` | 无修炼战斗力 | int |
| `pe = PowerEngine.Evaluate(cs)` | 修炼战斗力 | 同 UT ∈ [0.7,1.3]×剑修 |
| `Speed = Insight + Internal/2 + speedBonuses` | 行动速度 | int |
| `HP = pe` | 生命值 | int |
| `Age >= Lifespan + lifespanBonus` | 寿元死亡判定 | bool |
| `UT = RealmCurve.Lookup(realmIndex)` | 统一境界等阶 | 0-12 |

---

## 5. Edge Cases

- **修炼 off 模式**：`cultivation=false` 时必须与 v1.0 逐字节一致（独立 PRNG 流保证）
- **WIP 限制**：同一时刻 doing ≤ 2 story
- **整数溢出**：所有战斗数值取 `int`，`Modules` 工厂内置 `Amount2≥1` 钳
- **无限循环**：战斗有回合上限 N；Scheduler 有空队列 fallback
- **飞升离场**：UT12 圆满 → 转 Ascended 移出 _alive，防人口塞满
- **辅助路战力**：丹 UT≤7 / 阵 UT≤7 / 器 UT≤10 / 符 UT=12

---

## 6. Dependencies

| 依赖 | 方向 |
|---|---|
| 修炼系统（cultivation） | Core ← cultivation（可选层，off 时可剥离） |
| 战斗系统（combat） | 依赖 Modules 工厂 + PowerEngine + DuelEngine |
| 戏剧引擎（drama） | 依赖 Relations + ModuleResolver（RelationAdjust） |
| 地图系统（map） | 依赖 World + WorldNode + NodeId |
| LLM 脑 | 依赖 IBrain 端口（Core 不含传输代码） |
| Unity 宿主（后期） | 依赖 Jianghu.Core 库（零改写） |

---

## 7. Tuning Knobs

| 参数 | 默认 | 安全范围 | 影响 |
|---|---|---|---|
| `AmbientQi` | 420 | [0,1000] | 全局可达境界上限 |
| `StatSum` | 80 | 固定 | 生成期角色强度 |
| `StatCap` | 30 | [20,40] | 单维上限 |
| `PopulationLow/High` | 待定 | [10,500] | 人口代谢速率 |
| `UT gap auto-win` | 2 | [1,4] | 碾压门槛 |
| `AgeBase` | 600 | [400,1200] | 寿元基线 |
| `LifespanBonus[UT]` | 见 A123 spec | ±50% | 修炼延寿幅度 |

---

## 8. Acceptance Criteria

- [x] v1.0 确定性竖切：同种子逐字节一致，38+ 测试
- [x] A.0 修炼引擎：21 路全入册，204+ 测试
- [x] combat-r2 模块化战斗：Modules 工厂 + DuelEngine，410 测试绿
- [x] Sprint 2 全量机制结构化 + 恩怨基础：5/5 stories done
- [ ] 跨路平衡 INV-CROSS：同 UT 胜率 ∈ [40%,60%]
- [ ] LLM 脑 v1.1：涌现戏剧性
- [ ] Unity 宿主：玩家介入 + 即时反应窗口
