namespace Jianghu.Cultivation
{
    /// <summary>
    /// 断链 commandChain 断（傀儡·傀儡系 唯一档签名，impl plan 余9路 3.x）：傀儡师本体被斩首/断线 →
    /// <c>residualOrder</c>（残命惯性）决定军团是否还能继续行动。这是**防御/状态签名**，非直接伤害
    /// → <see cref="SpecialResult.DamageDelta"/>=0。
    ///
    /// <para>本批落确定性 residualOrder 初值置入：触发即 <c>ctx.ApplyResource(Attacker, "residualOrder", op.Amount)</c>
    /// （断链时置残命惯性初值），以 <see cref="CombatContext.HasResource"/> 守——攻方无 residualOrder（非傀儡师）则空转，
    /// 不往无该资源的路硬写（避免 chokepoint 撞 KeyNotFound）。</para>
    ///
    /// <para>真「每 tick 衰减 + 军团僵死判定」= 批4 turn-loop（per-tick decay + fleet-freeze）→ defer。
    /// 本批只经 chokepoint 置 residualOrder 确定性初值，每 tick 衰减由批4 消费（A.8 诚实标 defer，不静默移走）。</para>
    ///
    /// <para>纪律（spec §7 M3）：纯整数；无 RNG；不读 daoHeart/innerDemon；副作用**全经 chokepoint**
    /// <see cref="CombatContext.ApplyResource"/>（钳 [Min,Cap]），不直写 dict。</para>
    ///
    /// 资源键：<c>residualOrder</c>（傀儡 KuileiShi Resources，[0,100]，写攻方）。
    /// 注：每 tick 衰减 + 军团僵死判定→批4 turn-loop；本批落 residualOrder 初值 chokepoint。
    /// </summary>
    internal sealed class BrokenChainModule : ISpecialModule
    {
        public string HandlerId => "brokenChain";

        public SpecialResult Apply(CombatContext ctx, EffectOp op)
        {
            // 断链=防御/状态签名（非直伤）：经 chokepoint 置攻方 residualOrder（残命惯性）确定性初值。
            // HasResource 守——攻方无 residualOrder（非傀儡师）则空转，不往无该资源的路硬写撞 KeyNotFound。
            // 每 tick 衰减 + 军团僵死判定 = 批4 turn-loop → defer（A.8 不静默）；本批只置初值。
            if (ctx.HasResource(Side.Attacker, "residualOrder"))
                ctx.ApplyResource(Side.Attacker, "residualOrder", op.Amount);
            // 防御/状态签名（非直伤）→ delta 0。
            return new SpecialResult(0);
        }
    }
}
