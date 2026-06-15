namespace Jianghu.Cultivation
{
    /// <summary>
    /// 逆演栈 逆演重开/时光回溯（命 / 因果·命数系 唯一档签名，impl plan 余9路 3.x）：撤销刚结算的一次交锋
    /// （伤害/夺运/胜负回滚），但 Karma/天谴债**不回滚**。效果是结算回滚，非直接伤害
    /// → <see cref="SpecialResult.DamageDelta"/>=0。
    ///
    /// <para><b>核心机制真批4-gated（A.8 显式）</b>：真「栈回滚（撤销上次结算）」需要批4 的战斗结算结果栈
    /// （combat result stack，尚未建）——这是 3 个 handler 里唯一其核心机制确属批4 才能做的。本批 handler
    /// 只落确定性的**代价**部分，结算回滚整体 defer 批4 result-stack（不静默移走）：</para>
    /// <list type="bullet">
    /// <item>命 MingFateCausality：<c>netFortune -10</c>（消运势）+ <c>lifespanDebt +3</c>（逆演代价寿元债）；</item>
    /// <item>因果 YinguoFaze：无 netFortune，改消 <c>spaceTimeAuth -1</c>（时空权柄）+ <c>lifespanDebt +3</c>。</item>
    /// </list>
    /// <para>每键以 <see cref="CombatContext.HasResource"/> 守——缺该键的路（如纯命路无 spaceTimeAuth、
    /// 纯因果路无 netFortune）对应分支空转，apply 哪个存在哪个，不往无该资源的路硬写（避免 chokepoint 撞 KeyNotFound）。</para>
    ///
    /// <para>纪律（spec §7 M3）：纯整数；无 RNG；不读 daoHeart/innerDemon；代价**全经 chokepoint**
    /// <see cref="CombatContext.ApplyResource"/>（钳 [Min,Cap]，netFortune 钳底 -50），不直写 dict。</para>
    ///
    /// 资源键：命 <c>netFortune</c>[-50,40] / 因果 <c>spaceTimeAuth</c>[0,9]（择存在者消）；共用 <c>lifespanDebt</c>[0,100]（逆演寿元债）。
    /// 注：栈回滚（撤销上次结算）真机制→批4 result-stack（本 handler 唯一核心 batch4-gated）；本批只收确定性代价。
    /// </summary>
    internal sealed class ReverseStackModule : ISpecialModule
    {
        public string HandlerId => "reverseStack";

        // 逆演代价常量（确定性，无 RNG）：命势 -10、时空权柄 -1、寿元债 +3。
        const int FortuneCost = 10;
        const int SpaceTimeAuthCost = 1;
        const int LifespanDebtCost = 3;

        public SpecialResult Apply(CombatContext ctx, EffectOp op)
        {
            // 逆演确定性代价（栈回滚本身 = 批4 result-stack → defer，A.8 显式）：
            // 命路消 netFortune（运势），因果路无 netFortune 改消 spaceTimeAuth（时空权柄）。
            // 各以 HasResource 守，apply 哪个存在哪个，不往无该资源的路硬写撞 KeyNotFound。
            if (ctx.HasResource(Side.Attacker, "netFortune"))
                ctx.ApplyResource(Side.Attacker, "netFortune", -FortuneCost);
            else if (ctx.HasResource(Side.Attacker, "spaceTimeAuth"))
                ctx.ApplyResource(Side.Attacker, "spaceTimeAuth", -SpaceTimeAuthCost);

            // 逆演代价寿元债（命 / 因果两路共用 lifespanDebt），HasResource 守，缺则空转。
            if (ctx.HasResource(Side.Attacker, "lifespanDebt"))
                ctx.ApplyResource(Side.Attacker, "lifespanDebt", LifespanDebtCost);

            // 效果=结算回滚（非直伤），回滚本身 defer 批4 → 本批 delta 0。
            return new SpecialResult(0);
        }
    }
}
