namespace Jianghu.Cultivation
{
    /// <summary>
    /// 夺心 植蛊夺心（毒蛊·蛊系 唯一档签名，impl plan 余9路 3.x）：植蛊夺心——target 本回合反噬其阵营/受己调遣
    /// （mind-control / 反向夺兽）。确定性（无掷骰）：攻方若有空 <c>hostSlots</c> 则概念上耗 1 host 位，
    /// 经 chokepoint 抬高防方 <c>guRevolt</c>（蛊群反噬度）。效果是控制/反噬，非直接伤害
    /// → <see cref="SpecialResult.DamageDelta"/>=0。
    ///
    /// <para>本批落确定性的 guRevolt chokepoint 部分：<c>ctx.ApplyResource(Defender, "guRevolt", +op.Amount)</c>
    /// （植蛊使目标蛊群反噬度上升），以 <see cref="CombatContext.HasResource"/> 守——防方无 guRevolt（纯阳/佛门/死物傀儡
    /// 等非蛊目标）则空转，匹配该招既有语义「对纯阳/佛门/死物傀儡命中失败」。</para>
    ///
    /// <para>真「阵营反噬/受己调遣」（target 的行动重定向）= 批4 turn-state（target action redirect）→ defer。
    /// 本批只做 chokepoint 的 guRevolt 抬升占位（A.8 诚实标 defer，不静默移走）。</para>
    ///
    /// <para>纪律（spec §7 M3）：纯整数；无 RNG（确定性触发，无掷骰）；不读 daoHeart/innerDemon；副作用**全经 chokepoint**
    /// <see cref="CombatContext.ApplyResource"/>（钳 [Min,Cap]），不直写 dict。</para>
    ///
    /// 资源键：<c>guRevolt</c>（毒蛊 DuGuXiu Resources，[0,100]，写防方）；<c>hostSlots</c>（[0,1]，概念上耗 host 位，
    /// 真消耗属批4）。注：阵营反噬/受调遣→批4 turn-state；本批落 guRevolt chokepoint 抬升。
    /// </summary>
    internal sealed class DuoxinModule : ISpecialModule
    {
        public string HandlerId => "duoxin";

        public SpecialResult Apply(CombatContext ctx, EffectOp op)
        {
            // 植蛊夺心确定性 chokepoint 部分：抬防方 guRevolt（蛊群反噬度上升）。
            // HasResource 守——防方无 guRevolt（纯阳/佛门/死物傀儡等非蛊目标）则空转：
            // 「对纯阳/佛门/死物傀儡命中失败」（匹配该招既有语义），避免往无该资源的路硬写撞 KeyNotFound。
            // 真 host 位消耗 + target 阵营反噬/受己调遣（行动重定向）= 批4 turn-state → defer（A.8 不静默）。
            if (ctx.HasResource(Side.Defender, "guRevolt"))
                ctx.ApplyResource(Side.Defender, "guRevolt", op.Amount);
            // 效果=控制/反噬（非直伤）→ delta 0。
            return new SpecialResult(0);
        }
    }
}
