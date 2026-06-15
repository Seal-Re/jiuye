using System;

namespace Jianghu.Cultivation
{
    /// <summary>
    /// 稀有档战斗模块 OnUse 分支结算（spec §15，B5 批1）。switch on <see cref="EffectOpKind"/>，
    /// 各 OnUse Kind 真分支，纯整数（ratio 用整数除）。资源移动经 <see cref="CombatContext"/> chokepoint
    /// （不直写 dict）。OnDefend（Dot/Control/ReflectDamage/Evade）与自伤（Backlash selfDmg）走批3/批4，
    /// 本轮 default 返回 dmg 不变。
    /// </summary>
    public static class ModuleResolver
    {
        /// <summary>§15.6 防除零/双义：分母取 Amount2，<1 退化为 1。</summary>
        private static int Den(EffectOp m) => m.Amount2 >= 1 ? m.Amount2 : 1;

        /// <summary>§15.4 tag 克制联合上界：dmg×Amount/Den 与 dmg×3/2 取小。</summary>
        private static int CapCounter(int dmg, EffectOp m) => Math.Min(dmg * m.Amount / Den(m), dmg * 3 / 2);

        /// <summary>OnUse 算子施加于本次伤害，返回修正后整数 dmg。</summary>
        public static int ApplyOnUse(int dmg, EffectOp m, CombatContext ctx)
        {
            switch (m.Kind)
            {
                case EffectOpKind.AddPenInteger:
                    return dmg + m.Amount;

                case EffectOpKind.PenFromResource:
                    // 资源转伤：delta += res(Key)*Amount/Den
                    return dmg + ctx.ReadResource(Side.Attacker, m.Key!) * m.Amount / Den(m);

                case EffectOpKind.AoePerTarget:
                    // R2 单挑退化×1：等价 dmg + Amount
                    return dmg + m.Amount;

                case EffectOpKind.CounterMul:
                    // 防方带 tag(Key) → 联合上界倍乘；否则不变
                    return ctx.HasTag(Side.Defender, m.Key!) ? CapCounter(dmg, m) : dmg;

                case EffectOpKind.DrainResource:
                    // 夺取：防方 res(Key) -= Amount、攻方 += Amount（经 chokepoint 钳），dmg 不变
                    ctx.ApplyResource(Side.Defender, m.Key!, -m.Amount);
                    ctx.ApplyResource(Side.Attacker, m.Key!, m.Amount);
                    return dmg;

                case EffectOpKind.Backlash:
                    // 自伤走批4 selfDmg 通道，本 task 不处理 → dmg 不变
                    return dmg;

                case EffectOpKind.Special:
                    // 唯一档逃逸口(§7 M3)：派发 SpecialModuleRegistry[Key]，handler 经 chokepoint 落副作用、返伤害 delta。
                    // 缺失 handlerId 注册期/查询期抛(不静默)；handler 纪律纯整数/不读 daoHeart/不掷随机。
                    return dmg + SpecialModuleRegistry.Get(m.Key!).Apply(ctx, m).DamageDelta;

                default:
                    // Dot/Control/ReflectDamage/Evade/其余（批3/批4 处理）→ dmg 不变
                    return dmg;
            }
        }
    }
}
