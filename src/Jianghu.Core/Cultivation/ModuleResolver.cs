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

        /// <summary>PostMul 倍数钳 [0,20]：ratio=10 为 ×1.0，ratio<0 钳 0（禁反转），ratio>20 钳 20（×2.0 上限）。</summary>
        private static int ClampPostMul(int value, int ratio)
        {
            if (ratio < 0) ratio = 0;
            if (ratio > 20) ratio = 20;
            return value * ratio / 10;
        }

        /// <summary>OnUse 算子施加于本次伤害，返回修正后整数 dmg。
        /// 门控检查（story fullstruct-006）：若 EffectOp.Gate != None 且攻方不满足门控，静默跳过（dmg 不变）。</summary>
        public static int ApplyOnUse(int dmg, EffectOp m, CombatContext ctx)
        {
            // 功法门控检查：攻方不满足门控 → 跳过操作（dmg 不变，不抛异常）
            if (m.Gate != GateType.None && !ctx.CheckGate(Side.Attacker, m.Gate))
                return dmg;

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
                {
                    bool self = m.Key!.StartsWith("self:");
                    string statKind = self ? m.Key.Substring(5) : m.Key;
                    ctx.AccumulateStatDelta(self ? Side.Attacker : Side.Defender, statKind, m.Amount);
                    return dmg;
                }

                case EffectOpKind.ModifyEffectivePower:
                    ctx.AccumulateEPModifier(Side.Defender, m.Key!, m.Amount, Den(m));
                    return dmg;

                case EffectOpKind.RelationAdjust:
                    // Key=delta. 累积关系边修改, 战后 SparAction 落地.
                    ctx.AccumulateRelationDelta(Side.Defender, m.Amount);
                    return dmg;

                case EffectOpKind.PostMul:
                    // 乘法修正（在 FlatPen 之后乘算）：dmg *= ratio/10。钳 [0,20] 防反转/过乘。
                    return ClampPostMul(dmg, m.Amount);

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

            // 功法门控检查（story fullstruct-006）：防方不满足门控 → 跳过操作（dmg 不变，不抛异常）
            if (m.Gate != GateType.None && !ctx.CheckGate(defenderSide, m.Gate))
                return incomingDmg;

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

                case EffectOpKind.PostMul:
                    // 乘法修正（在 FlatDR 之后乘算，防方削减来袭伤害）：dmg *= ratio/10。钳 [0,20]。
                    reflectDmg = 0;
                    return ClampPostMul(incomingDmg, m.Amount);

                case EffectOpKind.SoulSplit:
                {
                    // 分魂挡刀（story fullstruct-007）：ratio% 的伤害转移到魂资源，本体只承受剩余部分。
                    // Key = 魂资源键（如 "soulHp"），Amount = 转移比例（0-100%）。
                    string soulKey = m.Key ?? "soulHp";
                    int ratio = m.Amount;
                    int soulAbsorb = incomingDmg * ratio / 100;
                    // 从防方魂资源扣除（经 chokepoint 钳 [Min,Cap]，缺该 key 则空转）
                    if (ctx.HasResource(defenderSide, soulKey))
                        ctx.ApplyResource(defenderSide, soulKey, -soulAbsorb);
                    reflectDmg = 0;
                    return Math.Max(0, incomingDmg - soulAbsorb);
                }

                case EffectOpKind.Special:
                {
                    // OnDefend 特殊模块派发（story fullstruct-007：未来 OnDefend 唯一档扩展口）。
                    // 经 SpecialModuleRegistry[Key] 派发 handler，副作用经 chokepoint 落。
                    var handler = SpecialModuleRegistry.Get(m.Key!);
                    var result = handler.Apply(ctx, m);
                    reflectDmg = 0;
                    if (result.DamageDelta != 0)
                        return Math.Max(0, incomingDmg + result.DamageDelta);
                    return incomingDmg;
                }

                default:
                    return incomingDmg;
            }
        }
    }
}
