namespace Jianghu.Cultivation
{
    /// <summary>
    /// 唯一档 handler 结算结果（spec §7）：伤害增量。
    /// 资源副作用已经 <see cref="CombatContext.ApplyResource"/> 落地，不在此回传。
    /// </summary>
    public readonly struct SpecialResult
    {
        public readonly int DamageDelta;
        public SpecialResult(int damageDelta) { DamageDelta = damageDelta; }
    }

    /// <summary>
    /// 唯一档逃逸口 handler（spec §7）：有限算子集外的受控扩展点。纪律——纯整数；
    /// 副作用经 <see cref="CombatContext.ApplyResource"/>（不直写 dict）；不读 daoHeart/innerDemon；
    /// 不消费随机。<see cref="HandlerId"/> 为注册键，<see cref="SpecialModuleRegistry"/> 凭此确定派发。
    /// </summary>
    public interface ISpecialModule
    {
        string HandlerId { get; }
        SpecialResult Apply(CombatContext ctx, EffectOp op);
    }

    /// <summary>
    /// 唯一档 handler 注册表（spec §7 chokepoint）：id → 单例 handler。
    /// 同 id 重复注册抛；缺失 id 查询抛（不静默回 null）；同 id 始终回同单例（派发确定）。
    /// </summary>
    public static class SpecialModuleRegistry
    {
        static readonly System.Collections.Generic.Dictionary<string, ISpecialModule> _handlers
            = new System.Collections.Generic.Dictionary<string, ISpecialModule>();

        static SpecialModuleRegistry()
        {
            // 批1 占位 handler 验框架。
            Register(new NoopSpecialModule());
            // 批3 5 唯一档真签名（impl plan 3.1–3.5）：落宝(器)/炸阵(阵)/金身态(佛)/律场总门(音)/夺舍(鬼·魂·魔)。
            // 各 handler 纪律：纯整数 / 无 RNG / 不读 daoHeart/innerDemon / 副作用经 ApplyResource chokepoint。
            Register(new LuobaoModule());
            Register(new ExplodeArrayModule());
            Register(new GoldenBodyMaxModule());
            Register(new FieldActiveModule());
            Register(new DuosheModule());
        }

        static void Register(ISpecialModule m)
        {
            if (_handlers.ContainsKey(m.HandlerId))
                throw new System.InvalidOperationException($"Special handler 重复注册: {m.HandlerId}");
            _handlers[m.HandlerId] = m;
        }

        public static ISpecialModule Get(string handlerId)
        {
            if (!_handlers.TryGetValue(handlerId, out var m))
                throw new System.InvalidOperationException($"Special handler 未注册: {handlerId}");
            return m; // 同 id → 同单例（派发确定）
        }
    }

    /// <summary>占位 handler（框架验证用）：无副作用，0 增量。</summary>
    internal sealed class NoopSpecialModule : ISpecialModule
    {
        public string HandlerId => "noop";
        public SpecialResult Apply(CombatContext ctx, EffectOp op) => new SpecialResult(0);
    }
}
