using System.Collections.Generic;

namespace Jianghu.Cultivation
{
    /// <summary>
    /// 功法/战技效果的有限算子集（spec §7，盘点 21 路收敛的核心集）。
    /// 被动算子装配期落 state；OnUse 算子战斗期产整数 delta（完整结算 Phase 3）。
    /// 新语义算子 = 在此 enum 追加并显式标 <c>// L1</c>（spec §0 R5，不谎称零改核心）。
    /// A.0-B1 仅用这 10 个核心 EffectOpKind。
    /// </summary>
    public enum EffectOpKind
    {
        // —— 装配期（被动）落 state ——
        AddTermWeightStep, // 抬某 PowerTerm 的权重台阶（state.Flags 记 step）
        AddResourceCap,    // 抬某资源上限
        AddResource,       // 加某资源（经 chokepoint 钳制）
        SetFlag,           // 置 0/1 标志
        GrantPassive,      // 授予被动标志（落 Flags）
        // —— 战斗期（OnUse）产整数 delta（完整结算 Phase 3）——
        AddFlatDR,         // 加固定减伤
        AddPenInteger,     // 加破防整数
        ScalarMul,         // 标量乘子（整数 Num/Den）
        AddSituationalAdj, // 加情境修正（整数 %）
        // —— 资源消耗 ——
        Cost,              // 消耗资源（不足则拒）
        // —— 稀有档战斗机制模块（L1 新语义，B5 模块化效果系统）——
        PenFromResource,   // L1 资源转伤: delta += res(Key)*Amount/Amount2
        AoePerTarget,      // L1 群攻×敌数(R2单挑退化×1)
        CounterMul,        // L1 tag克制倍乘: defender带tag(Key)→ dmg*Amount/Amount2(联合上界×3/2)
        DrainResource,     // L1 夺取: defender res(Key)-=Amount, attacker +=Amount(经chokepoint)
        Backlash,          // L1 反噬: 条件(Key)满足→attacker自伤Amount
        Dot,               // L1 持续伤: defender挂 Amount/tick × Amount2回合
        Control,           // L1 控场: defender 下Amount回合 selectMove失效
        ReflectDamage,     // L1 反伤(OnDefend): attacker受 incoming*Amount/Amount2(读扣血前/不递归)
        Evade,             // L1 闪避(OnDefend): 减伤=clamp((身法-命中+Amount)*k,0,maxReduce)连续
        // —— 唯一档逃逸口(L1, B5 §7 M3 纪律): Key=handlerId → SpecialModuleRegistry 派发 ——
        Special,           // L1 唯一档: ModuleResolver 派发 SpecialModuleRegistry[Key].Apply(ctx,op)→delta(纯整数/chokepoint)
        // —— 跨路机制(L1) ——
        ModifyStat,        // L1 改四维: Key=statKind(Force/Internal/Constitution/Insight), Amount=delta(可为负)。经CombatContext accumulator→DuelEngine→SparAction落地
        ModifyEffectivePower, // L1 改EP%: Key=resource, Amount=num, Amount2=den. 防方EP×=(1+Amount×res(Key)/den/100). 命修/因果削EP
        RelationAdjust,     // L1 造关系边: Key=delta(正=正边/负=负边), Amount=delta. 经CombatContext accumulator→SparAction→IWorldMutator
        PostMul,            // L1 乘法修正: dmg *= Amount/10（在 FlatPen/FlatDR 之后乘算，整数×10比例）。Key=压制Kind(LawSuppress/Transform/Literati/HeavenSuppress)。钳[0,20]。
        // 注: SumOfSet 撤(§15.1), 真Σ用 PenFromResource on 标量聚合资源
    }

    /// <summary>战斗期算子的触发时机：OnUse 主动使用 / OnDefend 受击防御 / Passive 被动常驻。</summary>
    public enum EffectTrigger { OnUse, OnDefend, Passive }

    /// <summary>模块算子稀有度：Common 常见 / Rare 稀有 / Unique 独门。</summary>
    public enum EffectRarity  { Common, Rare, Unique }

    /// <summary>
    /// 单个整数算子（spec §7）。<see cref="Key"/> = 资源/标志/权重台阶键；
    /// <see cref="Amount"/> = 整数量；<see cref="Note"/> = flavor，不参与结算。
    /// <see cref="Amount2"/> = 次参/分母（den），默认 0；
    /// <see cref="Trigger"/> = 战斗期何时 fire，默认 OnUse；
    /// <see cref="Rarity"/> = 稀有度，默认 Common。
    /// </summary>
    public sealed record EffectOp(
        EffectOpKind Kind, string? Key, int Amount, string? Note,
        int Amount2 = 0,
        EffectTrigger Trigger = EffectTrigger.OnUse,
        EffectRarity Rarity = EffectRarity.Common);

    /// <summary>
    /// 有限算子集解释器（spec §7），确定性纯整数。
    /// 装配期 <see cref="ApplyPassive"/> 落 state；<see cref="TryPayCost"/> 全或无扣资源；
    /// 战斗期 OnUse 算子 <see cref="ComputeOnUseDelta"/> 产整数 delta（完整战斗结算 Phase 3 接）。
    /// </summary>
    public static class EffectInterpreter
    {
        /// <summary>
        /// 装配期施加被动算子，落到 state：
        /// AddResourceCap 抬 cap / AddResource 经 chokepoint 钳 / SetFlag 置值 /
        /// GrantPassive 置 1 / AddTermWeightStep 累加权重台阶（落 Flags）。
        /// 战斗期算子（AddFlatDR/AddPenInteger/ScalarMul/AddSituationalAdj）不在装配期生效。
        /// </summary>
        public static void ApplyPassive(EffectOp op, CultivationState st)
        {
            switch (op.Kind)
            {
                case EffectOpKind.AddResourceCap:
                    st.RaiseCap(op.Key!, op.Amount);
                    break;
                case EffectOpKind.AddResource:
                    st.ApplyResource(op.Key!, op.Amount);
                    break;
                case EffectOpKind.SetFlag:
                    st.Flags[op.Key!] = op.Amount;
                    break;
                case EffectOpKind.GrantPassive:
                    st.Flags[op.Key!] = 1;
                    break;
                case EffectOpKind.AddTermWeightStep:
                    st.Flags.TryGetValue(op.Key!, out int cur);
                    st.Flags[op.Key!] = cur + op.Amount;
                    break;
                // 战斗期算子非装配期施加：装配阶段 no-op。
                case EffectOpKind.AddFlatDR:
                case EffectOpKind.AddPenInteger:
                case EffectOpKind.ScalarMul:
                case EffectOpKind.AddSituationalAdj:
                case EffectOpKind.Cost:
                case EffectOpKind.PostMul:     // L1 乘法修正 — 战斗期算子，装配阶段 no-op
                    break;
            }
        }

        /// <summary>
        /// 全或无扣资源：先验所有 key 余量足够，再统一扣；任一不足返 false 且不扣任何资源。
        /// </summary>
        public static bool TryPayCost(IReadOnlyDictionary<string, int> cost, CultivationState st)
        {
            foreach (var kv in cost)
            {
                if (!st.Resources.TryGetValue(kv.Key, out int have) || have < kv.Value)
                    return false;
            }
            foreach (var kv in cost)
            {
                st.ApplyResource(kv.Key, -kv.Value);
            }
            return true;
        }

        /// <summary>
        /// 战斗期 OnUse 算子骨架：产整数 delta（AddPenInteger/AddFlatDR=固定量；
        /// ScalarMul=整数乘子百分量；AddSituationalAdj=情境 %）。
        /// 完整战斗结算（经 attacker/defender/ctx 与 IWorldMutator）Phase 3 接。
        /// </summary>
        public static int ComputeOnUseDelta(EffectOp op)
        {
            switch (op.Kind)
            {
                case EffectOpKind.AddPenInteger:
                case EffectOpKind.AddFlatDR:
                case EffectOpKind.ScalarMul:
                case EffectOpKind.AddSituationalAdj:
                    return op.Amount;
                default:
                    return 0;
            }
        }
    }
}
