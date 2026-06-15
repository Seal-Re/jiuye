namespace Jianghu.Cultivation
{
    /// <summary>
    /// 落宝 落宝金光（器修·economic 唯一档签名，impl plan 3.1）：判定过门槛后**夺/打落对手 1 件法宝**——
    /// 防方本命法宝品阶 <c>itemTier</c> 本战清零、攻方借用 1 回合（借得对手被夺之宝）。效果是资源置换（夺器+借用），
    /// 非直接伤害 → <see cref="SpecialResult.DamageDelta"/>=0。
    ///
    /// <para>纪律（spec §7 M3）：纯整数；无 RNG；不读 daoHeart/innerDemon；资源移动**全经 chokepoint**
    /// <see cref="CombatContext.ApplyResource"/>（钳 [Min,Cap]），不直写 dict。先 <see cref="CombatContext.ReadResource"/>
    /// 读防方现值，再 -read 抽干防方、+read 灌入攻方（借用），读写对称 → 净额守恒（攻方 itemTier 上限 9 钳由 chokepoint 兜底）。</para>
    ///
    /// 资源键：<c>itemTier</c>（器修 qixiu_artificer Resources，[0,9]）。
    /// 真「夺器无视'御'纹/借用回合时序/对手 power 本战清零」属 Phase 3 战斗结算，本批落确定性资源置换（夺器→借用）的可做部分。
    /// </summary>
    internal sealed class LuobaoModule : ISpecialModule
    {
        public string HandlerId => "luobao";

        public SpecialResult Apply(CombatContext ctx, EffectOp op)
        {
            // 无宝可夺（防方无 itemTier 资源 / 攻方非器修无处借）则空转——只对**该路存在**的资源经 chokepoint 落地，
            // 不往无 itemTier 的路硬写（避免 ApplyResource 撞 KeyNotFound）。
            if (!ctx.HasResource(Side.Defender, "itemTier") || !ctx.HasResource(Side.Attacker, "itemTier"))
                return new SpecialResult(0);

            // 读防方本命法宝品阶现值（无该 key → 0，无宝可夺则空转）。
            int defenderItemTier = ctx.ReadResource(Side.Defender, "itemTier");
            // 夺器：防方 itemTier 抽干至 0（本战清零）；攻方借用：+原值（经 chokepoint 钳上限）。读写对称。
            ctx.ApplyResource(Side.Defender, "itemTier", -defenderItemTier);
            ctx.ApplyResource(Side.Attacker, "itemTier", defenderItemTier);
            // 效果=资源置换（夺器+借用），非直伤 → delta 0。
            return new SpecialResult(0);
        }
    }
}
