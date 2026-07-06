# ADR-0007: ECS 倾向 vs OOP 聚合根（内核实体架构方向）

- **Status**: **Proposed**（张力登记，未裁决 — 红线 A.1 大方向交用户）
- **Date**: 2026-07-04（自 godot-architecture-manifest.md 补§1.3 落盘引出，architecture.md §10.2 R3 升格）
- **Last Verified**: 2026-07-04
- **Deciders**: huangjiaqi13（待裁决）+ Claude（architecture，张力起草）
- **Affects**: `Jianghu.Model.Character`（聚合根）/ `World.Clone` 深拷语义 / 确定性子流绑定 / 全 Core 遍历热路径（**当前架构不变**）

---

## Summary

Godot Manifest 补§1.3 要求"为支撑**海量同屏/全局实体推演**，内核架构严格限制深层 OOP 继承，**强制向 ECS（实体组件系统）靠拢**，降低 CPU 遍历开销"。当前 `Jianghu.Model.Character` 是 **OOP 聚合根**（含 Persona/Relations/MemoryStore/CultivationState 等，`World.Clone` 深拷 + 确定性子流绑定）。ECS 化是**大架构决定**（同级于 retro"方差战斗模型需专门立项"）。本 ADR **登记张力 + 备选**，**不裁决**——裁决须先立**性能基准证明必要性**（当前无实测瓶颈）。

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Godot 4.x (.NET)（Core 引擎无关；ECS 与否属 Core 内部架构） |
| **Domain** | Core（实体架构 / 性能） |
| **Knowledge Risk** | LOW — ECS/DOD 模式与 C# 实现均在训练数据内 |
| **References Consulted** | [godot-architecture-manifest.md](godot-architecture-manifest.md) 补§1.3 / [architecture.md](architecture.md) §3（Model 命名空间）/ `World.Clone` R1 注释 |
| **Post-Cutoff APIs Used** | None（本 ADR 不写代码） |
| **Verification Required** | 裁决前置：实体规模性能基准（当前 LimitsConfig 上限下 `Advance` 遍历耗时 profile）；裁决落地时：ECS 化后确定性子流/Clone 逐字节回归 |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | [adr-0001](adr-0001-integer-determinism.md)（整数确定性 — ECS 化后每步轨迹仍须逐字节一致）；[adr-0003](adr-0003-cultivation-off-byte-identical.md)（off 逐字节 — 重构不得破 v1.0 record 字段顺序 / 侧表纪律） |
| **Enables** | 海量实体宏观世界（manifest §2.1 "视野外 NPC 网格数据点"）；性能天花板提升 |
| **Blocks** | 无硬阻塞——当前 OOP 可运转；本 ADR 是**方向性评估**，非阻塞项 |
| **Ordering Note** | **需求驱动，非规范驱动**——先有性能瓶颈实证再裁。规模需求来自 manifest 宏观层（未落地），故当前**低优先级**。 |

---

## Context

### 问题
manifest 补§1.3（性能天花板）："为支撑**海量同屏/全局实体推演**，内核架构严格限制深层 OOP 继承，强制向 **ECS** 靠拢，降低 CPU 遍历开销。"

这与当前实体架构方向相反：
- `Character` 是**面向对象聚合根**（architecture.md §3 `Jianghu.Model`）——一个对象聚合 Persona/StatBlock/Relations/MemoryStore/CultivationState 等子对象。
- `World.Advance` 遍历 `_alive` 角色列表逐个推进（OOP 对象方法调用）。
- `World.Clone` 深拷整个对象图 + 所有 PRNG 子流深拷续跑（确定性保证，adr-0001 §4.1 / F-REQUIRED-2）。

ECS 化 = 把实体拆成**数据组件数组**（SoA，Structure-of-Arrays），系统批量遍历组件而非对象——缓存友好、遍历快，但**颠覆现有对象模型 + Clone/确定性绑定**。

### 现状（当前无瓶颈实证）
- **模拟规模由 `LimitsConfig` 约束**（角色数/节点数上限，F-PERF-2）——当前**非海量**（headless 模拟，1087 测试 10s 跑完，active.md）。
- **无性能 profile 数据证明 OOP 遍历是瓶颈**——manifest 的"海量同屏"是**宏观世界 View 层的未来诉求**（微缩沙盒全局实体），当前 headless Core 未触及该规模。
- manifest 措辞是"**倾向/靠拢**"（soft），非"必须全 ECS"（hard）——留了渐进空间。

### 约束
- **确定性不可破**（adr-0001）：ECS 化后每步轨迹、`Clone` 续跑仍须逐字节一致——SoA 遍历顺序、组件默认值都需确定。
- **off 逐字节不可破**（adr-0003）：v1.0 core record 字段顺序、侧表纪律——大重构最易踩雷处（F-REQUIRED-5）。
- **YAGNI / 内核前置（A.10）**：无实证瓶颈的大重构 = 过早优化风险；产品未到海量规模。
- 裁决须**数据驱动**（先 profile），故 defer（A.1）。

---

## Decision

> **DEFERRED（未裁决）。** 前置门槛 = **性能基准证明 OOP 遍历确为瓶颈**。无实证前不启动大重构（YAGNI + A.10 内核前置）。下列备选按侵入度递增。

### 候选备选

#### 备选 A：维持 OOP（现状 + 局部优化，倾向默认）— 直到实证瓶颈
- **描述**：保持 `Character` 聚合根；继承本就浅（manifest 担心的"深层 OOP 继承"在本项目其实不严重——`Character` 是组合非深继承）。规模问题先靠 `LimitsConfig` + 视野外降维（manifest §2.1 "网格数据点"，本就不实例化）解决。
- **Pros**：零重构风险；确定性/Clone/off 逐字节全不动；符合 YAGNI（无实证瓶颈）。
- **Cons**：若真到海量同屏（万级实体），OOP 遍历/GC 压力可能成瓶颈（未证）。
- **判据**：先跑 profile；若 `LimitsConfig` 上限下 `Advance` 耗时可接受，**维持不动**。

#### 备选 B：局部 data-oriented 重构热路径（渐进，中间道路）
- **描述**：保留 `Character` 对象模型，仅把**遍历热路径的数据**抽成并行数组（如战力/速度/位置的 SoA 缓存），系统批量算后写回对象。对象仍是真相源，SoA 是派生加速层。
- **Pros**：局部收益（缓存友好遍历）不颠覆全架构；确定性风险可控（SoA 派生自对象，顺序确定）；可增量、可回退。
- **Cons**：双表（对象 + SoA 缓存）需同步；收益不如全 ECS 极致。
- **判据**：profile 指出具体热路径（如 PowerEngine 批量求值）才做，且只做那一处。

#### 备选 C：全 ECS 重构
- **描述**：`Character` 拆为组件（`StatComponent`/`RelationComponent`/`CultivationComponent`…），`World` 变 ECS 世界，系统批量遍历组件数组。
- **Pros**：极致缓存性能；天然支撑海量实体；契合 manifest 字面。
- **Cons**：**颠覆性重构**——确定性子流绑定、`Clone` 深拷语义、off 逐字节 record、侧表纪律全需重新设计验证；1087 测试大面积重写；高风险（一处确定性 bug 难查，红线 B.7 高风险档）。
- **判据**：仅当备选 A/B 均不足、且海量规模成硬需求时才考虑；须专门立项 + 独立 sprint。

### 关键前置：性能基准（任何裁决的输入）
在选任一备选前，须有数据：
1. 当前 `LimitsConfig` 上限下 `World.Advance` 单步耗时 profile；
2. 目标规模（manifest "海量同屏"到底多少实体？= 产品定义）；
3. 目标规模下 OOP 遍历的外推耗时 vs 帧预算（宿主层帧预算，P-PERF）。

---

## Consequences

### Positive（登记本身的价值）
- 显式化"ECS 是需求驱动非规范驱动"——避免因 manifest 一句"向 ECS 靠拢"就盲目大重构（YAGNI 守）。
- 备选 A/B/C 侵入度递增，给出渐进路径而非"全或无"。

### Negative
- 未裁决 = 若未来真需海量规模，重构启动较晚（但当前无此需求，可接受）。

### Neutral
- 当前 OOP 架构完全可运转（1087 绿），本 ADR 不改任何现状。

---

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|-----------|
| 无实证就全 ECS 重构（过早优化） | 低 | 高 | 本 ADR 立"先 profile"前置门；A.10 内核前置 |
| ECS 化破确定性/off 逐字节 | 中（若做C） | 高 | adr-0001/0003 回归套件；高风险档旗舰实现（B.7） |
| 海量规模需求突现但未准备 | 低 | 中 | 备选 B 可快速局部加速争取时间 |

## Performance Implications

| Metric | Before | Expected After | Budget |
|--------|--------|---------------|--------|
| `Advance` 单步遍历 | 未 profile | 待基准 | 宿主帧预算（P-PERF，海量时才紧） |
| 内存（实体） | OOP 对象图 | ECS SoA 更紧凑（若做C） | LimitsConfig 约束 |

> **本 ADR 的核心产出恰是"去测这张表"**——无 Before 数据则无裁决依据。

## Validation Criteria（裁决落地后）

- [ ] 性能基准 profile 完成（当前规模 `Advance` 耗时 + 目标规模外推）
- [ ] 用户定"海量同屏"目标实体量级（产品定义）
- [ ] 据数据选备选写入本 ADR（Status → Accepted）；若维持 OOP 则标 A 为决议
- [ ] （若重构）adr-0001 确定性 + adr-0003 off 逐字节回归全绿

## GDD Requirements Addressed

| GDD Document | System | Requirement | How This ADR Satisfies It |
|-------------|--------|-------------|--------------------------|
| [godot-architecture-manifest.md](godot-architecture-manifest.md) | 实体平权与规模 补§1.3 | "限制深层 OOP，强制向 ECS 靠拢，支撑海量实体" | 登记与现 OOP 聚合根的张力；立"需求/数据驱动"前置门，列渐进备选 |

## Related

- [adr-0001](adr-0001-integer-determinism.md)（整数确定性）— ECS 化后每步轨迹/Clone 仍须逐字节一致
- [adr-0003](adr-0003-cultivation-off-byte-identical.md)（off 逐字节）— 重构不得破 v1.0 record 字段顺序/侧表纪律
- [tr-registry.yaml](tr-registry.yaml) `TR-VIEW-R3`（本张力的溯源锚）
- [architecture.md](architecture.md) §10.2 R3（张力首次登记处）/ §3（Model 命名空间现状）
- retro action item「评估方差战斗模型需专门立项」（active.md）— 同级大架构决定的处理范式
