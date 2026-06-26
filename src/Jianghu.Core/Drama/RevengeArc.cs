using System;
using Jianghu.Config;

namespace Jianghu.Drama
{
    /// <summary>
    /// 复仇弧 5 态**纯转移函数**（drama-007b，spec §3.2）。
    /// Victimized → BuildUp → Hunting → Showdown → Resolved | Abandoned。
    ///
    /// **纯函数**：门控读 IDramaView/LimitsConfig，`with` 产新弧实例，**不消费 rng、不产事件、不 mutate**
    /// （事件 drama-008，Goal/Relations 耦合 drama-011，escape 掷骰/Showdown 超时叠加在 Pump drama-009）。
    /// 故状态转移逻辑本身完全确定可单测。死亡守卫优先于阶段门控。
    /// </summary>
    public static class RevengeArc
    {
        /// <summary>
        /// 推进一步。返回 (下一弧, 转移结果, 复仇者是否得手)。终态弧（已退场）推进 → 抛。
        /// </summary>
        public static ArcTransition TryAdvance(ArcInstance arc, IDramaView view, LimitsConfig limits)
        {
            // 终态守卫（D7b.7）：已退场弧不可推进。
            if (arc.Completed || arc.Stage == ArcStage.Resolved || arc.Stage == ArcStage.Abandoned)
                throw new InvalidOperationException($"RevengeArc.TryAdvance: 弧已退场（Stage={arc.Stage}, Completed={arc.Completed}），不可推进");

            // 死亡守卫（D7b.6，先于门控）：任一参与者亡 → 放弃。
            if (!view.IsAlive(arc.Target) || !view.IsAlive(arc.Avenger))
                return Abandon(arc);

            switch (arc.Stage)
            {
                case ArcStage.Victimized:
                    // 进 BuildUp：记当前战力为基线（涨够才寻仇）。
                    return new ArcTransition(
                        arc with { Stage = ArcStage.BuildUp, BuildUpBasePower = view.Power(arc.Avenger) },
                        ArcResolution.Advanced, false);

                case ArcStage.BuildUp:
                    // 门控：战力 ≥ 基线 + GrowthNeeded → 进 Hunting；否则停留蓄力。
                    if (view.Power(arc.Avenger) >= arc.BuildUpBasePower + limits.GrowthNeeded)
                        return new ArcTransition(arc with { Stage = ArcStage.Hunting }, ArcResolution.Advanced, false);
                    return new ArcTransition(arc, ArcResolution.Stalled, false);

                case ArcStage.Hunting:
                    // 门控：与仇人同节点 → 进 Showdown；否则继续追寻。
                    if (view.SameNode(arc.Avenger, arc.Target))
                        return new ArcTransition(arc with { Stage = ArcStage.Showdown }, ArcResolution.Advanced, false);
                    return new ArcTransition(arc, ArcResolution.Stalled, false);

                case ArcStage.Showdown:
                    // 结算：战力比较判胜负（非致死后果属 Effect，drama-008/011）。
                    bool prevailed = view.Power(arc.Avenger) >= view.Power(arc.Target);
                    return new ArcTransition(
                        arc with { Stage = ArcStage.Resolved, Completed = true },
                        ArcResolution.Completed, prevailed);

                default:
                    throw new InvalidOperationException($"RevengeArc.TryAdvance: 未知阶段 {arc.Stage}");
            }
        }

        private static ArcTransition Abandon(ArcInstance arc)
            => new ArcTransition(arc with { Stage = ArcStage.Abandoned, Completed = true }, ArcResolution.Abandoned, false);
    }
}
