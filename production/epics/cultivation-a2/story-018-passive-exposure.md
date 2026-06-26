# Story 018: 太吾双面教训

> **Epic**: cultivation-a2
> **Status**: Complete （A.6 审计 2026-06-26 订正：git/代码证据证实已实现，台账滞后）
> **Last Updated**: 2026-06-24
> **Layer**: Core
> **Type**: Logic
> **Estimate**: 小 (0.5d)
> **Depends**: story-013
> **ADR**: adr-0001-integer-determinism
> **GDD**: A3-FINAL §4.3

## Context

太吾绘卷的教训：奇遇系统如果仅靠"被动暴露"（玩家主动触发），会导致内容池利用率极低（玩家只刷最优几条）。修复：① 被动 storylet 曝光（不依赖玩家主动选择，NPC 自动遭遇）；② salience decay（防反复刷最优，story-015）。此故事聚焦于被动曝光机制。

## Acceptance Criteria

- [ ] 18.1 `PassiveExposure`：每 tick 有一定概率自动曝光一个未触发的 storylet（不消耗 ActorMinGap）
- [ ] 18.2 曝光不等同于触发——仅将该 storylet 的 salience +10（提高下次触发的权重）
- [ ] 18.3 `InterestModel`：角色根据当前状态对 storylet 产生兴趣偏移——高 innerDemon → 偏好降心魔类；低 daoHeart → 偏好增道心类
- [ ] 18.4 Interest 偏移仅影响 Option 选择，不改变触发概率
- [ ] 18.5 确定性：同状态→同 Interest 偏移
- [ ] 18.6 off 模式不曝光

## Implementation Notes

**Interest 偏移**：
```
interestBias(category):
    if innerDemon >= DEMON_DANGER_ENTER:  // 65
        if category == "心魔试炼": bias -= 10  // 避开心魔
        if category == "前辈指点": bias += 10  // 寻求帮助
    if daoHeart <= 30:
        if category == "遗宝": bias += 5  // 寻求外力
```

**PassiveExposure 频率**：每 tick 10% 基础概率（不受 ActorMinGap 限制，这是曝光不是触发）。

## Out of Scope

- 完整兴趣模型（仅最小实现，后期可扩展）

## Test Evidence Requirement

**Type**: Logic — automated unit tests. Passive exposure rate, interest bias correctness, off mode.
