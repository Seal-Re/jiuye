# Epic: 戏剧引擎 B

**Layer**: Feature
**Status**: In Design → Stories（2026-06-26 /loop：GDD 已补，待拆 story 落地）
**GDD**: `design/gdd/drama-system.md`（2026-06-26 形式化）；深度源 `docs/legacy-specs/specs/2026-06-13-v1.2-B-戏剧引擎-design.md`
**Governing ADRs**: adr-0001-integer-determinism, adr-0003-cultivation-off-byte-identical
**Engine Risk**: LOW（.NET 8 纯整数）；最高危=确定性回归（drama 介入 Advance + Clone 深拷）
**Created**: 2026-06-15（迁自 TASKS.md）；**Updated**: 2026-06-26（GDD 补 + 状态推进）

## Summary
戏剧引擎 B——恩怨账本 + 复仇弧状态机（5 态）+ 跨代继承 + storylet 序列器。独立事件流序列器叠加 v1.0 核，零改 RuleBrain，空库 no-op 保 off 逐字节。

## Scope
- 恩怨账本 GrudgeLedger（独立有向表，与 Relations 并存）
- 复仇弧 5 态机（Victimized→BuildUp→Hunting→Showdown→Resolved/Abandoned）
- 跨代恩怨继承（师承/血缘，父债子偿）
- storylet 声明式事件库 + DramaDirector.Pump 序列器

## 已落地（薄工具层）
- drama-001 `RelationService`（Relations.Adjust + Chronicle 封装）
- drama-002 `DramaStoryletEngine`（关系 storylet + 仇敌检测 affinity≤-50）
- 二者为薄工具层；GDD §9 的核心引擎（账本/弧/继承）待 drama-003~013 落地。

## Dependencies
**Unblocked by**: Relations/MemoryStore/Scheduler/RuleBrain/SparAction/Lifecycle（v1.0 已存在）
**Blocks**: faction C.1（A/B 在位喂 GrudgeLedger，dramaprofile 联动）

## Definition of Done
- [x] GDD（`design/gdd/drama-system.md`，2026-06-26，红线先行 + Core/Unity 分层 + 8 节 + story 映射）
- [ ] drama-003~013 全实现（VariedSelector→账本→弧→继承→端到端 INV-CHAIN）
- [ ] AC-1~10 全过（空库 no-op / 确定性 / IL 浮点零 / 容量 / 性能 / 状态机 / 跨代链 / 无死锁 / RuleBrain 零改 / 全量绿）

## Notes
GDD 开头先立红线（B.2/B.3/RNG隔离 Drama=6/RuleBrain零改/Clone命门/非致死），再机制。Core 整数层全实现；玩家亲历复仇弧的即时演出属 Unity 宿主层后期。
