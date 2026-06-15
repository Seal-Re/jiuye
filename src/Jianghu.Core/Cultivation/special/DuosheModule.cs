namespace Jianghu.Cultivation
{
    /// <summary>
    /// 夺舍 夺舍续命（鬼/魂/魔·阴邪系 唯一档签名，impl plan 3.5）：濒死强夺新躯**续命**——确定性条件（**无掷骰**），
    /// 触发即做资源清算续命。效果是续命资源清算，非直接伤害 → <see cref="SpecialResult.DamageDelta"/>=0。
    ///
    /// <para>真「濒死判定」依赖 HP/濒死态（属批4 回合 state，尚未建）。本批保守实现：只对**该路存在**的「续命相关」资源做
    /// 确定性清算续命，经 chokepoint 落地——
    /// 魂修 soul_divine_sense：<c>seaIntegrity</c>（识海完整度 [0,100]）→ 回满（夺舍成功识海回满 100，+100 由 chokepoint 钳顶）；
    /// 鬼修 gui_xiu_yang_hun：<c>devourMeter</c>（噬主度 [0,100]）→ 清零（夺舍成功噬主度清零，-100 由 chokepoint 钳底）。
    /// 两键各自 ReadResource 探测「是否存在于本路」（无该 key→0，delta 后仍 0，空转无害），故同一 handler 跨魂/鬼路皆安全。
    /// **只用该路存在的资源**，不发明资源。</para>
    ///
    /// <para>纪律（spec §7 M3）：纯整数；无 RNG（确定性触发，无掷骰）；不读 daoHeart/innerDemon；资源清算**全经 chokepoint**
    /// <see cref="CombatContext.ApplyResource"/>（钳 [Min,Cap]），不直写 dict。</para>
    ///
    /// 资源键：<c>seaIntegrity</c>（魂修，回满续命）/ <c>devourMeter</c>（鬼修，清零续命）。
    /// 注：夺舍续命真濒死判定→批4 HP；本批 handler 落确定性资源清算续命占位。
    /// </summary>
    internal sealed class DuosheModule : ISpecialModule
    {
        public string HandlerId => "duoshe";

        public SpecialResult Apply(CombatContext ctx, EffectOp op)
        {
            // 续命资源清算（确定性，无掷骰）：只对**该路存在**的资源经 chokepoint 落地，钳 [Min,Cap]，
            // 不往无该资源的路硬写（HasResource 区分「缺 key」与「值 0」，避免 ApplyResource 撞 KeyNotFound）。
            // 魂修 seaIntegrity 回满（夺舍成功识海回满）：+(100-现值)，由 chokepoint 钳顶。
            if (ctx.HasResource(Side.Attacker, "seaIntegrity"))
            {
                int seaIntegrity = ctx.ReadResource(Side.Attacker, "seaIntegrity");
                ctx.ApplyResource(Side.Attacker, "seaIntegrity", 100 - seaIntegrity);
            }
            // 鬼修 devourMeter 清零（夺舍成功噬主度清零）：-现值，由 chokepoint 钳底。
            if (ctx.HasResource(Side.Attacker, "devourMeter"))
            {
                int devourMeter = ctx.ReadResource(Side.Attacker, "devourMeter");
                ctx.ApplyResource(Side.Attacker, "devourMeter", -devourMeter);
            }
            // 效果=续命资源清算，非直伤 → delta 0。
            return new SpecialResult(0);
        }
    }
}
