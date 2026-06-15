# Story 002: batch3 — 唯一档 Special handler（高价值签名）

> **Epic**: combat-r2
> **Status**: Not Started
> **Layer**: Core
> **Type**: Logic
> **Estimate**: 中
> **Depends**: story-001（普通/稀有档框架在位）

## Context

**深度源**: docs/superpowers/specs/2026-06-14-v1.2-B5-模块化效果系统-design.md §7 + impl plan 批3。
**Governing ADR**: adr-0002-module-factory-effect-system。

## Acceptance Criteria

经 `SpecialModuleRegistry` 注册式插件实现唯一档签名（每 handler TDD：副作用断言 + 纯整数 + 不读 daoHeart + 不掷随机 + 资源 [Min,Cap] 钳）：

- [ ] 3.1 落宝 luobao（器）：对方 itemTier 清零 + 借用
- [ ] 3.2 炸阵 explodeArray（阵）：阵 power×2 一次性
- [ ] 3.3 金身态 goldenBodyMax（佛）：3 回合 DR×2 Passive
- [ ] 3.4 律场总门 fieldActive（音）：未起调律场置 0
- [ ] 3.5 夺舍 duoshe（鬼/魂/魔）：濒死资源清算续命
- [ ] 余签名（因果栈/夺心…）框架就位 + 显式 deferred FULLSTRUCT（A.8）

## Out of Scope

DuelEngine 时序结算 → story-003。
