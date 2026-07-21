namespace Jianghu.Cultivation
{
    /// <summary>
    /// cv-009：ResolveExchange 结算结果——命名 record 替换 5 元组。
    ///
    /// 值语义（Value Equality）→ Assert.Equal 直比；
    /// 不可变性 → 防下游副作用篡改；
    /// 非破坏性扩展 → 未来加字段仅改构造器和内部传参，调用方解构签名不变。
    /// </summary>
    public sealed record CombatExchangeResult(
        int DmgToDefender,
        int ReflectToAttacker,
        int PoiseBreakBonus,
        bool ChipImmuneToPoise,
        DefenseFrameHook? FrameHook
    );
}
