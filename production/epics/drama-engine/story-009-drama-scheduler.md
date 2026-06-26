# Story drama-009: DramaScheduler 最小堆 + IDramaMutator 事件汇 seam

> **Epic**: drama-engine
> **Status**: Done（2026-06-26 /loop TDD：997 绿 [+7]，0 警告，IL 浮点零）
> **Layer**: Core（`Jianghu.Drama`）
> **Type**: Logic
> **Estimate**: 小 (0.3d)
> **Depends**: drama-004（ArcId，done）、drama-008（DomainEvent，done）；仿 `Jianghu.Sim.Scheduler`
> **ADR**: adr-0001-integer-determinism（最小堆 (At,ArcId) 确定性序）、adr-0003-cultivation-off-byte-identical
> **GDD**: `design/gdd/drama-system.md` §3.4A(推进相调度) + §9（drama-009 = spec Step 7，**Step 7 拆分前半**）

## Context（drama-009 拆分说明）

spec Step 7 = 「DramaScheduler + DramaDirector.Pump + WorldFactory dramaRng」。承 007/007b 成功拆分模式，再拆两 story：

- **drama-009（本 story）= Pump 的两块预备件**：
  - **`DramaScheduler`**：确定性最小堆 `(NextWakeAt, ArcId)`——**仿 `Jianghu.Sim.Scheduler`**（既有成熟件，按 (At, Id) 排序 + Snapshot/LoadFrom 续跑）。弧到期推进的时间轮。
  - **`IDramaMutator`**：戏剧层**写 seam**——`Emit(DomainEvent)` 事件汇（drama-010 由 World 实现为 Project+Chronicle.Append；本 story 测试用 recording fake）。这是「DomainEvent 单源」红线的落点：戏剧效果只经 Emit，不旁路 mutate。
- **drama-009b（下一 story）= DramaDirector.Pump**：推进相 A（弹到期弧→TryAdvance→Emit→reschedule）+ 节流点火相 B/C（FindIgnitions→WeightedPicker→CreateArc→Emit ArcIgnited）。建立在本预备件 + drama-007/007b 之上。

> **范围决策（A.8 显式）**：`WorldFactory dramaRng = root.Split(6)` **延后到 drama-010**——此刻构造 dramaRng 而无 director 消费它即是「off 路径徒调 Split」隐患（虽 Split(6) 不碰 1..4，但无消费者时构造无意义且增 off 风险面）。随 World 接线一并落，届时同步证「空候选时 dramaRng 未被消费 → off 逐字节」。**不静默，记此**。

> **红线约束**：B.2 整数确定性（最小堆 (At,ArcId) 全整数比较，禁浮点）；B.3 off 逐字节（纯加，无 World 接线，DramaScheduler/IDramaMutator 仅 drama-010 接入后才活）；DomainEvent 单源（IDramaMutator.Emit 是戏剧唯一写口）；R-NF2 续跑（Snapshot/LoadFrom 供 drama-010 Clone）。

## Acceptance Criteria

- [x] **D9.1 DramaScheduleItem**：`readonly record struct DramaScheduleItem(ArcId Arc, long At)`。
- [x] **D9.2 最小堆推进序**：`Push(ArcId, at)` / `PopMin()` 按 `(At asc, Arc.Id asc)` 出堆（确定性，同 Scheduler.Cmp 模式）；空堆 PopMin 抛 `InvalidOperationException`。
- [x] **D9.3 PeekMin + HasDue**：`PeekMin()` 不弹出返堆顶；`HasDue(clock)` = 非空且 `PeekMin().At <= clock`（到期判定）；空堆 HasDue→false。
- [x] **D9.4 Snapshot/LoadFrom 续跑**：`Snapshot()` 返当前堆项只读列表；`LoadFrom(items)` 清空后重建（R-NF2，供 drama-010 Clone 深拷调度器）；往返后出堆序一致。
- [x] **D9.5 IDramaMutator seam**：`interface IDramaMutator { void Emit(DomainEvent e); }`——戏剧层唯一写口（drama-010 World 实现为 Project+Chronicle）。本 story 仅定义 + recording fake 测试验。
- [x] **D9.6 确定性**：同 Push 序列两 scheduler → 同 PopMin 序列；乱序 Push 同集合 → 同出堆序（堆序不依赖插入序，只依赖 (At,ArcId)）。
- [x] **D9.7 IL 浮点零 + 既有 990 绿不退 + clean rebuild 0 警告**：`Jianghu.Drama` 扫描仍零；off 逐字节（无 World 接线）。

**机器证据（2026-06-26 /loop）**：全量 **997 绿** / 0 失败 / 0 skip（990 → +7）；clean rebuild 0 警告；红线 focus 16 绿（DramaScheduler + off 逐字节 + 浮点扫描）。

## Implementation Notes

- **DramaScheduler**：几乎照搬 `Jianghu.Sim.Scheduler`（同 List 二叉堆 + 上浮/下沉），仅元素换 `DramaScheduleItem(Arc, At)`，`Cmp` 按 `(At, Arc.Id.Value)`。复用成熟实现降风险。加 `HasDue(clock)`（Scheduler 无此法，drama Pump 需要）。
- **IDramaMutator**：最小接口，仅 `Emit(DomainEvent)`。**本 story 不含 Goal 覆写 / 镜像 Relations** —— 那些写操作延 drama-011（届时扩 IDramaMutator 或并入 World 实现）。这里只立事件汇骨架，保 drama-009b Pump 可注入 mock 验事件序。
- 全 `Jianghu.Drama` 命名空间，纯整数。无 World 接线（DramaScheduler 实例在 drama-010 由 World 持有 + Clone）。

## Test Evidence

**Required (BLOCKING — Logic)**:
- `tests/Jianghu.Core.Tests/Drama/DramaSchedulerTests.cs` —— D9.2~D9.4/D9.6：乱序 Push → 有序 PopMin（(At,ArcId)）；同 At 按 ArcId asc；PeekMin 不弹；HasDue 边界（At==clock 到期、At>clock 未到期、空堆 false）；空堆 PopMin 抛；Snapshot/LoadFrom 往返出堆序一致；确定性两跑同序。
- IDramaMutator recording fake 在本测试文件内验 Emit 收集（drama-009b 复用）。

## Out of Scope

- `DramaDirector.Pump` 推进相/点火相编排（drama-009b）。
- `WorldFactory dramaRng = Split(6)`（drama-010，显式延后）。
- World 字段 / Advance / Clone 全 drama 态（drama-010）。
- IDramaMutator 的 Goal 覆写 / 镜像 Relations 扩展（drama-011）。
