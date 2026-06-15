namespace Jianghu.Cultivation
{
    /// <summary>
    /// 炸阵 引爆·焚阵（阵修·阵 唯一档签名，impl plan 3.2）：主动**炸毁己方一张在场阵**，弃阵换一次性爆发
    /// （对阵内全体造成该阵 power×2 一次性真伤）→ <see cref="SpecialResult.DamageDelta"/> = 阵力代理值 × 2。
    ///
    /// <para>真阵 power 是 derived（按在场阵图 tier×地利聚合，尚未建 → EPIC-COMBAT-FULLSTRUCT），本批用阵修**已有资源**
    /// 作阵力代理：优先 <c>stones</c>（灵石储量/各阵供能燃料），其次 <c>setupProgress</c>（布阵进度）；二者皆 0 时退化为 op.Amount
    /// （工厂传入的占位破发量）。**不消费不存在的资源**——只读代理值，不写（炸阵的「弃阵/清供能 stones」属 Phase 3 战斗结算）。</para>
    ///
    /// <para>纪律（spec §7 M3）：纯整数（×2 整数乘）；无 RNG；不读 daoHeart/innerDemon；本 handler 不改资源（无副作用，
    /// 只算一次性 delta），故无需 chokepoint 写入。</para>
    ///
    /// 资源键（只读代理）：<c>stones</c>（阵修 array_formation，[0,100]）/ <c>setupProgress</c>（[0,220]）。
    /// 注：真阵power derived→FULLSTRUCT，本批用 stones(其次 setupProgress) 代理。
    /// </summary>
    internal sealed class ExplodeArrayModule : ISpecialModule
    {
        public string HandlerId => "explodeArray";

        public SpecialResult Apply(CombatContext ctx, EffectOp op)
        {
            // 阵力代理：优先 stones（灵石供能燃料），其次 setupProgress（布阵进度），皆 0 退化为 op.Amount 占位破发量。
            int proxy = ctx.ReadResource(Side.Attacker, "stones");
            if (proxy <= 0) proxy = ctx.ReadResource(Side.Attacker, "setupProgress");
            if (proxy <= 0) proxy = op.Amount;
            // 弃阵换爆发：一次性 power×2（整数乘）。
            return new SpecialResult(proxy * 2);
        }
    }
}
