using System;

namespace Jianghu.Cultivation
{
    /// <summary>
    /// 稀有档战斗模块 OnUse/OnDefend 分支结算（spec §15，B5 批1/批4）。
    /// OnUse switch on <see cref="EffectOpKind"/> → 伤害修正；OnDefend switch → 减免/闪避/反伤。
    /// 资源移动经 <see cref="CombatContext"/> chokepoint（不直写 dict）。纯整数。
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
                    // 夺取：防方 res(Key) -= Amount、攻方 += Amount（经 chokepoint 钳）。
                    // 安全护栏：仅双方均有该 key 时执行（防 KeyNotFound，跨路 drain 非共享资源时静默跳过）
                    if (ctx.HasResource(Side.Defender, m.Key!) && ctx.HasResource(Side.Attacker, m.Key!))
                    {
                        ctx.ApplyResource(Side.Defender, m.Key!, -m.Amount);
                        ctx.ApplyResource(Side.Attacker, m.Key!, m.Amount);
                    }
                    return dmg;

                case EffectOpKind.Backlash:
                    // 自伤走批4 selfDmg 通道，本 task 不处理 → dmg 不变
                    return dmg;

                case EffectOpKind.ModifyStat:
                    // 跨路改四维: Key=statKind("Force"/"Internal"/"Constitution"/"Insight"), Amount=delta.
                    // 经 CombatContext accumulator 累积, 战后 SparAction 落地。
                    ctx.AccumulateStatDelta(Side.Defender, m.Key!, m.Amount);
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

        /// <summary>
        /// OnDefend 防御算子施加于来袭伤害，返回减免后整数 dmg（批4）。
        /// FlatDR=固定减伤；Evade=连续闪避减免（§15.2）；
        /// ReflectDamage=反伤（§15.5，读扣血前不递归，通过 out reflectDmg 回传攻方额外受击量，调用方合并）。
        /// </summary>
        /// <param name="incomingDmg">来袭伤害（扣血前值，供 ReflectDamage 读原始值）</param>
        /// <param name="m">防御模块算子</param>
        /// <param name="ctx">战斗上下文</param>
        /// <param name="defenderSide">防方视角</param>
        /// <param name="reflectDmg">反伤量（仅 ReflectDamage 非零；调用方累加到攻方伤害）</param>
        /// <returns>减免后伤害（≥0）</returns>
        public static int ApplyOnDefend(int incomingDmg, EffectOp m, CombatContext ctx, Side defenderSide, out int reflectDmg)
        {
            reflectDmg = 0;
            switch (m.Kind)
            {
                case EffectOpKind.AddFlatDR:
                    return Math.Max(0, incomingDmg - m.Amount);

                case EffectOpKind.Evade:
                {
                    int evadeReduce = incomingDmg * m.Amount / 100;
                    return Math.Max(0, incomingDmg - evadeReduce);
                }

                case EffectOpKind.ReflectDamage:
                {
                    int den = m.Amount2 >= 1 ? m.Amount2 : 1;
                    reflectDmg = incomingDmg * m.Amount / den;
                    return incomingDmg; // 反伤不减来袭伤害
                }

                default:
                    return incomingDmg;
            }
        }
    }
}
