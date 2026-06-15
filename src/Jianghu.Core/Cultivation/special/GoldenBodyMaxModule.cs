namespace Jianghu.Cultivation
{
    /// <summary>
    /// 金身态 不动明王怒目（佛修·佛 唯一档签名，impl plan 3.3）：开金身大成态 3 回合——DR×2、所有受击全额转愿、
    /// 对阴邪伤害再×1.5。这是一个**逐回合 DR 状态标记**，按设计该态的「每回合 DR×2 消费」属批4 回合循环
    /// （per-turn DR state is batch4's turn loop）。
    ///
    /// <para>佛修 buddhist_golden_body 现有资源为 <c>vow</c>(愿力)/<c>merit</c>(功德)/<c>goldenLayers</c>(金身层 [0,9])，
    /// **无 goldenBodyTurns 这类「回合标记」资源**。按红线 A.8（不静默 defer、不发明资源），本批 handler 不强行往
    /// goldenLayers 写入冒充回合标记（语义不符：goldenLayers 是炼体叠层，非态计时），而是**占位返 0 + 诚实注**，
    /// 把 DR×2 标记/3 回合计时整体让给批4 回合循环消费。</para>
    ///
    /// <para>纪律（spec §7 M3）：纯整数；无 RNG；不读 daoHeart/innerDemon；本批不写任何资源（无干净的回合标记资源可落，
    /// 写 goldenLayers 会污染炼体层语义）→ 无 chokepoint 副作用。</para>
    ///
    /// 资源键：无（本批占位）。注：金身态 Passive 标记 → 批4 回合循环消费；本批 handler 占位返 0+注（无 goldenBodyTurns 资源，
    /// 不污染 goldenLayers 炼体层语义）。
    /// </summary>
    internal sealed class GoldenBodyMaxModule : ISpecialModule
    {
        public string HandlerId => "goldenBodyMax";

        public SpecialResult Apply(CombatContext ctx, EffectOp op)
        {
            // 金身大成态=逐回合 DR×2 标记（3 回合），属批4 回合循环 state。佛修无 goldenBodyTurns 干净资源可落，
            // 写 goldenLayers 会污染「炼体叠层」语义 → 本批占位返 0，DR×2/3 回合计时整体 defer 批4（红线 A.8 不静默）。
            return new SpecialResult(0);
        }
    }
}
