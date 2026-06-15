namespace Jianghu.Cultivation
{
    /// <summary>
    /// 律场总门 起调·点律（音修·音 唯一档签名，impl plan 3.4）：gate-by-flag——律场未起调（起调进度未蓄）则律场
    /// spirit 效果整体归 0（律场未起调→效果归 0）；已起调则放行 op.Amount（律场 spirit 破发量计入）。
    ///
    /// <para>实现（impl plan 明确）：读攻方 <c>windupProgress</c>（起调进度），≤0 → <see cref="SpecialResult.DamageDelta"/>=0
    /// （律场总门未点亮）；&gt;0 → 透传 op.Amount。这正是 PowerEngine「未起调则律场项整体置 0」GateByFlag 的伤害侧 chokepoint 兑现。</para>
    ///
    /// <para>纪律（spec §7 M3）：纯整数；无 RNG；不读 daoHeart/innerDemon；本 handler 只读 gate 不改资源（点亮 fieldActive 的
    /// flag/起调时序属战技装配+Phase 3）→ 无 chokepoint 写入。</para>
    ///
    /// 资源键（只读 gate）：<c>windupProgress</c>（音修 yin_xiu_yuedao Resources，[0,100]）。
    /// </summary>
    internal sealed class FieldActiveModule : ISpecialModule
    {
        public string HandlerId => "fieldActive";

        public SpecialResult Apply(CombatContext ctx, EffectOp op)
        {
            // 律场总门：起调进度未蓄（≤0）→ 律场 spirit 效果整体归 0；已起调（>0）→ 放行 op.Amount。
            if (ctx.ReadResource(Side.Attacker, "windupProgress") <= 0)
                return new SpecialResult(0);
            return new SpecialResult(op.Amount);
        }
    }
}
