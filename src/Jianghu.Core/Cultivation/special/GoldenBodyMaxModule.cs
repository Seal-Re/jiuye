namespace Jianghu.Cultivation
{
    /// <summary>
    /// 金身态 不动明王怒目（佛修·佛 唯一档签名，impl plan 3.3）：开金身大成态 3 回合——DR×2、所有受击全额转愿、
    /// 对阴邪伤害再×1.5。这是一个**逐回合 DR 状态标记**，按设计该态的「每回合 DR×2 消费」属批4 回合循环
    /// （per-turn DR state is batch4's turn loop）。
    ///
    /// <para>本批（批3收口）从「纯占位返0」升级为「资源 chokepoint 初值落地」：
    /// <c>goldenBodyTurns</c> 置 3（大成态剩余回合初值），同时金身层临时 +2（上限 9，表达大成态金身强化）。
    /// DR×2 逐回合消费、受击→愿力转化批4 turn-loop overall defer，本批以资源起底 + goldenLayers 强化兑现金身态
    /// 的可观测机械增益（goldenLayers×25 直接入战力公式 + flat DR=8×层），消除「占位返 0」的 ⚠️ 冲突标记。</para>
    ///
    /// <para>纪律（spec §7 M3）：纯整数；无 RNG；不读 daoHeart/innerDemon；副作用全经 chokepoint
    /// <see cref="CombatContext.ApplyResource"/>（钳 [Min,Cap]），不直写 dict。HasResource 守——攻方无 goldenBodyTurns
    /// （非佛修路径）则空转，不往无该资源的路硬写。</para>
    ///
    /// 资源键：<c>goldenBodyTurns</c>（佛 BuddhistGoldenBody Resources，[0,3]，写攻方）；
    /// <c>goldenLayers</c>（[0,9]，写攻方，+2 临时强化，钳上限 9）。
    /// 注：DR×2 标记/受击转愿×2/anti_evil×1.5 → 批4 回合循环消费；本批落 goldenBodyTurns 初值 + goldenLayers 强化。
    /// </summary>
    internal sealed class GoldenBodyMaxModule : ISpecialModule
    {
        public string HandlerId => "goldenBodyMax";

        public SpecialResult Apply(CombatContext ctx, EffectOp op)
        {
            // 金身大成态确定性 chokepoint 部分（批3收口）：
            // (1) 置 goldenBodyTurns=3（大成态剩余回合初值）；
            // (2) goldenLayers+2 临时强化（上限 9，表达大成态金身叠层增强→战力×25 项+flat DR=8×新增层）。
            // HasResource 守——攻方无 goldenBodyTurns（非佛修路径）则空转，不往无该资源的路硬写撞 KeyNotFound。
            if (ctx.HasResource(Side.Attacker, "goldenBodyTurns"))
            {
                ctx.ApplyResource(Side.Attacker, "goldenBodyTurns", 3);
                ctx.ApplyResource(Side.Attacker, "goldenLayers", 2); // 钳 [0,9]，AddResource Cap 守
            }
            // DR×2 逐回合消费 + 受击→愿力转化 + anti_evil×1.5 = 批4 turn-loop overall defer（A.8 不静默）。
            // 本批以 goldenBodyTurns 初值 + goldenLayers+2 兑现可观测机械增益（战力+flat DR），消除占位返0。
            return new SpecialResult(0);
        }
    }
}
