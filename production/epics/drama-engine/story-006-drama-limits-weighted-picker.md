# Story drama-006: LimitsConfig 戏剧上限 + WeightedPicker 整数轮盘

> **Epic**: drama-engine
> **Status**: Done（2026-06-26 /loop TDD：926 绿 [+30]，0 警告，IL 浮点零覆盖 Util+Drama）
> **Layer**: Core（`Jianghu.Config` + `Jianghu.Util`）
> **Type**: Logic
> **Estimate**: 小 (0.3d)
> **Depends**: drama-003（VariedSelector 共享原语，done）、drama-004/005（值类型/账本，done）
> **ADR**: adr-0001-integer-determinism（整数轮盘全 long 累加防溢出）、adr-0003-cultivation-off-byte-identical（纯加字段，off 不消费）
> **GDD**: `design/gdd/drama-system.md` §4（加权抽取裁决序）+ §7（Tuning Knobs）+ §9（drama-006 = spec Step 3-4）

## Context

承 GDD §9：drama-006 落 **两件预备件**，都为后续点火序列器（drama-007 FindIgnitions / drama-009 Pump）铺路，**本 story 不接 World、不进 Advance/Clone/Project**：

1. **LimitsConfig 戏剧上限**（GDD §7）：把全部戏剧调参集中进既有 `LimitsConfig`（init-only record），`Validate()` 追加越界断言。**纯加字段** —— off 轨迹（cultivation/drama 全关）绝不消费这些字段，故 B.3 逐字节守恒平凡成立（无 World 接线）。
2. **WeightedPicker 整数轮盘**（GDD §4）：无状态前缀和轮盘原语，置于 `Jianghu.Util`（与 drama-003 `VariedSelector` 同命名空间，可复用）。`long` 累加防溢出；单次 `IRandom.NextInt` 抽取（串行消费主流）；裁决序由调用方预排序（drama-007 `Sort by Weight desc,Id asc,CharacterId asc`），picker 只对「有序权重表」做比例抽取。

> **红线约束**：B.2 整数确定性（禁浮点，`long` total 防环绕，AC 验 IL 零浮点覆盖 `Jianghu.Util`+`Jianghu.Drama`）；B.3 off 逐字节（纯加，无 World 接线 → 既有 896 绿不退）；RNG 单次抽取不破流隔离（picker 只读传入 `IRandom`，不自建流）。

## Acceptance Criteria

- [x] **D6.1 LimitsConfig 戏剧字段**：新增 init-only 字段（默认值见 Implementation Notes），含 `GrudgeCap=100` / `GrudgeIgniteThreshold` / `MaxConcurrentArcs=3` / `MaxArcsPerCharacter=1` / `IgnitionCheckInterval` / `ArcPairCooldown` / 四 stage delay（First/BuildUp/Hunting/Showdown）/ `GrowthNeeded` / `EscapeRatioPct` / `DramaBudget` / `MaxArcWeightSum` / `InheritDecayPct` / `MaxGeneration=3` / `RelationMirrorCap` / `ShowdownTimeout`。
- [x] **D6.2 Validate 追加断言**：每个新字段越界即抛 `InvalidOperationException`（具体边界见 Notes）。**关键**：`MaxConcurrentArcs >= 0`（**0 合法** —— GDD AC-1 的 no-op 开关），`DramaBudget >= 1`（§7），`EscapeRatioPct ∈ [1,100]`（§7），`InheritDecayPct ∈ [0,100]`（保继承单调不增 §5），`GrudgeIgniteThreshold ∈ [1, GrudgeCap]`（须可达）。
- [x] **D6.3 Default 仍可行**：`LimitsConfig.Default.Validate()` 不抛（既有 `LimitsConfigTests.Default_is_feasible` 保持绿）；既有三断言（StatCap/StatMin/人口/寿命）行为不变。
- [x] **D6.4 WeightedPicker 比例抽取**：`PickIndex(IReadOnlyList<int> weights, IRandom rng)` 返回 `[0,weights.Count)`，按权重比例。前缀和 + `rng.NextInt((int)total)` 单次抽取，命中首个 `draw < 前缀和` 的索引。
- [x] **D6.5 语义正确（非仅 ≠0）**：零权重索引**永不**被选（用固定返回值 fake IRandom 逐边界验：weights `[3,0,1]` → draw 0/1/2→idx0、draw 3→idx2，idx1 永不出现）；单候选恒返 idx0；权重比例随抽样收敛（用确定 Pcg32 种子统计 idx 频次落在预期区间）。
- [x] **D6.6 防御性边界**：空表抛 `ArgumentException`；负权重抛；`total<=0`（全零）抛（调用方须 `w≥1` 兜底）；`total > int.MaxValue` 抛（杜绝静默环绕 → 强制调用方用 `MaxArcWeightSum` 守门）。
- [x] **D6.7 确定性**：同 weights + 同 `IRandom` 状态 → 同索引；picker 无状态（不持计数，区别于 `VariedSelector`）。
- [x] **D6.8 IL 浮点零 + 既有 896 绿不退**：新增 IL 扫描覆盖 `Jianghu.Util` + `Jianghu.Drama`（补 GDD §0.2 诚实缺口：此前 IL 扫描仅覆盖 `Jianghu.Cultivation`）；clean rebuild 0 警告。

## Implementation Notes

### LimitsConfig 默认值（GDD §7 对齐；未显式给值的取语义合理值）

| 字段 | 默认 | Validate 断言 | 依据 |
|---|---|---|---|
| `GrudgeCap` | 100 | `>= 1` | §3.1 Intensity[0,100] |
| `GrudgeIgniteThreshold` | 60 | `∈ [1, GrudgeCap]` | 强恩怨才点火（Maiming+ 区） |
| `MaxConcurrentArcs` | 3 | `>= 0`（**0 合法=no-op**） | §7 |
| `MaxArcsPerCharacter` | 1 | `>= 1` | §7/§5 防多弧抢 Goal |
| `IgnitionCheckInterval` | 20 | `>= 1` | §3.4 点火节流 |
| `ArcPairCooldown` | 200 | `>= 0` | §7 对子冷却 |
| `FirstStageDelay` | 10 | `>= 1` | §3.2 Victimized→BuildUp |
| `BuildUpDelay` | 100 | `>= 1` | §3.2「长」（疯修涨战力） |
| `HuntingDelay` | 30 | `>= 1` | §3.2「中」 |
| `ShowdownDelay` | 10 | `>= 1` | §3.2「短」 |
| `GrowthNeeded` | 50 | `>= 0` | §4 Hunting 门控增量 |
| `EscapeRatioPct` | 50 | `∈ [1,100]` | §7 显式范围 |
| `DramaBudget` | 4 | `>= 1` | §7 显式「≥1」（每 Pump 推进上限） |
| `MaxArcWeightSum` | 1_000_000 | `>= 1` | §5 溢出上界守门（int 范围内，picker int 抽取安全） |
| `InheritDecayPct` | 60 | `∈ [0,100]` | §3.3 整数衰减；≤100 保单调不增 |
| `MaxGeneration` | 3 | `>= 1` | §7 跨代封顶 |
| `RelationMirrorCap` | 30 | `∈ [0,100]` | §3.1 镜像负 Relations 钳幅 |
| `ShowdownTimeout` | 100 | `>= 1` | §3.2 死锁兜底 |

- 全 init-only `int`（`MaxArcWeightSum` 亦 int，自然 ≤ int.MaxValue → picker `(int)total` 安全）。
- Validate 在既有三断言后追加上述断言；顺序不限（独立断言）。
- **不改 GrudgeLedger**：`Form/Adjust` 仍收 `cap` 参数（drama-005 设计），World 接线（drama-010）时才传 `limits.GrudgeCap`。本 story 仅备字段。

### WeightedPicker（`src/Jianghu.Core/Util/WeightedPicker.cs`，无状态 static）

```
PickIndex(weights, rng):
  空/null → throw ArgumentException
  long total=0; for w in weights: { w<0→throw; total+=w; if total>int.MaxValue→throw }
  total<=0 → throw（须 w≥1 兜底）
  int draw = rng.NextInt((int)total)        // [0,total) 单次抽取
  long acc=0; for i: { acc+=weights[i]; if draw<acc return i }
  return Count-1                            // 理论不可达兜底
```

- 与 `VariedSelector` 分工：VariedSelector = 有状态「少用优先」轮替；WeightedPicker = 无状态「权重比例」轮盘。二者皆 `Jianghu.Util`，drama-007/009 按需取用。
- 裁决序责任在调用方（GDD §4：`Sort by Weight desc, Id asc, CharacterId asc` 后传入），picker 不重排（保「禁 Dictionary 枚举序参与裁决」由上游保证）。

## Test Evidence

**Required (BLOCKING — Logic)**:
- `tests/Jianghu.Core.Tests/Drama/WeightedPickerTests.cs` —— D6.4~D6.7：固定值 fake IRandom 逐边界（零权重永不选 / 前缀和边界）、单候选、比例收敛（Pcg32 统计）、确定性、空/负/全零/溢出异常。
- `tests/Jianghu.Core.Tests/LimitsConfigTests.cs`（扩展）—— D6.1~D6.3：Default 可行、各新字段越界抛、`MaxConcurrentArcs=0` 合法不抛、边界值（GrudgeIgniteThreshold>GrudgeCap 抛 / EscapeRatioPct=0 抛 / InheritDecayPct=101 抛 / DramaBudget=0 抛）。
- `tests/Jianghu.Core.Tests/Drama/DramaFloatScanTests.cs` —— D6.8：IL 扫描 `Jianghu.Util`+`Jianghu.Drama` 零浮点（复用 `ILFloatScanner`，元探针证扫描器有效）。

**机器证据（2026-06-26 /loop）**：全量 **926 绿** / 0 失败 / 0 skip（基线 896 → +30）；clean rebuild **0 警告 0 错误**；红线 focus 子集 57 绿（off 逐字节 + 确定性 + 浮点扫描）。提交 sha 见 epic/index + commit log。

## Out of Scope

- storylet schema / RevengeArc 5 态机 / FindIgnitions（drama-007）。
- DramaScheduler / Pump / WorldFactory dramaRng（drama-009）。
- World 字段 / Advance / Clone / Project 接线（drama-010）。
- 点火权重公式（BaseWeight×IntensityMul×ArchMul）本身（drama-007 计算后喂 picker）；本 story 只备 picker 与上限。
