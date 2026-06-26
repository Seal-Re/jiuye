# Story drama-010: World 接线（⚠️ 最高危）— 字段 + Advance Pump + Clone 全 drama 态 + dramaRng=Split(6)

> **Epic**: drama-engine
> **Status**: Done（2026-06-26 /loop TDD：1019 绿 [+11]，0 警告，⚠️ Clone 续跑 + off 逐字节实证）
> **Layer**: Core（`Jianghu.Sim.World` + `Jianghu.Sim.WorldFactory` + `Jianghu.Cli`）
> **Type**: Logic + 集成
> **Estimate**: 中-大 (0.7d)
> **Depends**: drama-005~009b（全 done——GrudgeLedger/DramaDirector/IDramaView/IDramaMutator/事件）
> **ADR**: adr-0003-cultivation-off-byte-identical（⚠️ 触 World ctor/Advance/Clone）、adr-0001-integer-determinism
> **GDD**: `design/gdd/drama-system.md` §0.2(Clone 命门/RNG 隔离) + §6(集成) + §9（drama-010 = spec Step 8，**全 epic 最高危步**）

## Context（⚠️ 全 epic 最高危——触 World 主循环 + Clone 命门）

前 9 个 drama story 全是「积木 + 独立可单测」，**零接 World**。drama-010 把 DramaDirector 接进 `World`——首次让戏剧真正在 `Advance` 主循环跑、进 `Clone` 续跑。这是确定性回归最高危步（spec risks §最高危）。

**接线完全复刻 story-008 Map/Faction 成熟模式**（已验证安全的纯加接线）：nullable 字段 + setter + WorldFactory 仅 on 构造 + Advance 末尾 null-guarded 调用 + Clone 深拷。

### 接线六处（全 null-guarded，off 不可达）

1. **World 字段**：`GrudgeLedger? Grudges`（public get）+ `DramaDirector? _drama` + `IRandom? _dramaRng`（全 off=null）。`SetDrama(ledger, director, dramaRng)` setter（仿 SetMap/SetFaction）。
2. **WorldFactory**：`dramaOn` 参数 → 仅 on 时 `root.Split(RngStreamIds.Drama=6)` + 构造 GrudgeLedger + DramaDirector → `w.SetDrama(...)`。**off 绝不调 Split(6)** → Split(1..4) 编号不变（B.3）。
3. **Advance 末尾**：Faction block 后插 `_drama?.Pump(Clock, this, this, _dramaRng!)`（World 自身实现 IDramaView+IDramaMutator）。固定在 Faction 后 → 事件顺序确定。
4. **World : IDramaView**：`Power`（Force×2+Int+Con，复用 BuildFactionMight 同公式）/ `Affinity`（Relations.Affinity）/ `IsAlive`（_alive 含）/ `SameNode`（两角色 Node 相等）。
5. **World : IDramaMutator**：`Emit(e)` → `Chronicle.Append(e, NameOf)` + drama memory Project（drama-008 延后项落此：RevengeConsummated 写参与者负 valence Memory）。
6. **Clone**：深拷 `Grudges`（Grudges.Clone）+ `_drama`（`_drama.Clone(clonedLedger)` 复用同一克隆账本）+ `_dramaRng`（CloneRngOrNull）。

> **范围**：StateSnapshot 加 drama 段（仿 Faction，on-only，off 省略保逐字节）。CLI `--drama` 开关。

> **红线约束（先证后改）**：B.3 off 逐字节（**核心**——dramaOn=false 必须与既有 1008 绿逐字节一致 + dramaOn=true 但空库时亦逐字节，因空库 Pump no-op 不消费 dramaRng）；B.2 整数确定性（dramaRng=Split(6) 独立流 + Clone 续跑不发散）；RNG 隔离（Split(6) 不碰 Split(1..4)，决策相不碰 dramaRng）；DomainEvent 单源（Emit 唯一写口）；R-NF2 Clone 全 drama 态深拷。

## Acceptance Criteria

- [x] **D10.1 字段 + setter**：World 加 `Grudges`(get)/`_drama`/`_dramaRng` 可空字段 + `SetDrama`；off 全 null。
- [x] **D10.2 WorldFactory dramaOn**：`CreateInitial(..., bool dramaOn=false)`；on→`root.Split(6)`+GrudgeLedger+DramaDirector+SetDrama；off→不构造不调 Split(6)。
- [x] **D10.3 ⚠️ off 逐字节（核心）**：`dramaOn=false`（默认）与既有轨迹**逐字节一致**——既有 `OffByteIdenticalTests`/`DeterminismTests` 全绿不退；CLI 默认输出不变（30 行 == 30 行）。
- [x] **D10.4 ⚠️ drama-on 空库逐字节**：`dramaOn=true` 但无预置恩怨 → Pump 每 tick no-op → Chronicle 与 off **逐字节一致**（多种子 Theory 验）。
- [x] **D10.5 World IDramaView 正确**：`Power`/`Affinity`/`IsAlive`/`SameNode` 正确；死亡/不存在角色 IsAlive→false。
- [x] **D10.6 drama-on 确定性**：预置强恩怨 fixture，同种子两跑 Chronicle 逐字节；产生 drama 行（接线真活）。
- [x] **D10.7 ⚠️ Clone 续跑确定性（命门）**：drama-on 跑 120 步 → Clone → 原与克隆各再跑 120 步 → 两者 Chronicle 逐字节一致（Grudges/DramaDirector/dramaRng 全进 Clone 无漏拷）。
- [x] **D10.8 Emit 走 Chronicle + memory**：RevengeConsummated 经 Emit → Chronicle 出对应行（长跑实证决战/中止结局）+ 参与者 Memory 负 valence。
- [x] **D10.9 StateSnapshot drama 段**：on 序列化恩怨账本（Id/Holder/Target/Kind/Intensity/Gen）；off 省略段（逐字节）。
- [x] **D10.10 全量绿 + clean rebuild 0 警告 + IL 浮点零 + CLI --drama 冒烟**。

**机器证据（2026-06-26 /loop）**：全量 **1019 绿** / 0 失败 / 0 skip（1008 → +11）；clean rebuild 0 警告；
⚠️ 命门验证：determinism + off byte-identical + StateSnapshot + float scan + DramaWiring 共 **42 绿**；
CLI `--drama` 冒烟通过（off 30 行 == drama-on 空库 30 行，byte 一致）。
> 接线复刻 story-008 Map/Faction 成熟模式（nullable 字段 + setter + 仅 on 构造 + Advance null-guarded + Clone 深拷）。
> Clone 命门要点：账本只克隆一份，DramaDirector 复用同一克隆实例（避免 director 账本 ≠ World.Grudges 漂移）。

## Implementation Notes

- **接线点**：Advance 中 `_drama?.Pump(Clock, this, this, _dramaRng!)` 放 Faction block **之后**、`return processed` 前（事件顺序：行动→寿命→spawn→faction→**drama**）。`_dramaRng!` 仅 `_drama!=null` 时解引用（同构造，on 必非 null）。
- **World : IDramaView, IDramaMutator**：class 声明加接口；方法已在别处有等价逻辑（Power 同 BuildFactionMight 内联公式 → 抽 `DramaPower(Character)` 私有复用）。`SameNode(a,b)`：查 _alive 两角色 Node.Value 相等（任一不在世→false）。
- **Emit**：`void IDramaMutator.Emit(DomainEvent e) { Chronicle.Append(e, NameOf); ProjectDrama(e); }`。ProjectDrama：`RevengeConsummated rc` → 给 avenger/target Remember 负 valence（仿 Project 的 spar memory）。**仅 drama 事件**，不碰既有 Project。
- **Clone 顺序**：先 `Grudges?.Clone()` 得 clonedLedger → `_drama?.Clone(clonedLedger)`（director 复用同一克隆账本，**不各拷一份**，否则 director 操作的账本 ≠ World.Grudges → 漂移）→ `CloneRngOrNull(_dramaRng)`。三者经新 SetDrama 或私有 ctor 装回。
- **WorldFactory**：`var dramaRng = dramaOn ? root.Split(RngStreamIds.Drama) : null;` + `if (dramaOn) { var led = new GrudgeLedger(); var dir = new DramaDirector(led, limits); w.SetDrama(led, dir, dramaRng); }`。预置冤孽 fixture 延 drama-013（本 story 空库即可证接线 + 测试 helper 手动 Form 验确定性）。
- **dramaRng 主流 vs 私有子流**：本 story Pump 用 `_dramaRng` 作点火主流（WeightedPicker 串行消费）。弧内 TryAdvance 纯转移不消费 rng（drama-007b 已定），故无需 per-arc 子流——简化且不破确定性。

## Test Evidence

**Required (BLOCKING — Logic + 确定性命门)**:
- `tests/Jianghu.Core.Tests/Drama/DramaWiringTests.cs` —— D10.4~D10.8：drama-on 空库 == off 逐字节；World IDramaView 各方法正确；预置强恩怨同种子两跑逐字节 + 出 drama 行；⚠️ Clone 续跑逐字节（advance N→clone→各跑 M→比对）；Emit→Chronicle+memory。
- `tests/Jianghu.Core.Tests/Determinism/DramaWiringByteIdenticalTests.cs` —— D10.3/D10.4：dramaOn=false==默认；dramaOn=true 空库==off。
- ⚠️ 既有 `OffByteIdenticalTests`/`DeterminismTests`/`StateSnapshotTests`/`OffRegressionWith21PathsTests` 全绿（回归命门）。

## Out of Scope

- Goal 覆写 / 镜像 Relations 受控耦合（drama-011，让 RuleBrain 自发趋同节点/疯修）。
- 跨代继承监听 CharacterDied（drama-012）。
- 预置冤孽 fixture + 种子 storylet + Showdown 超时 + INV-CHAIN 端到端（drama-013）。
