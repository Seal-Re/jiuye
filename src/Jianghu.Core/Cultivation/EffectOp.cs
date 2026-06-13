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
    }

    /// <summary>
    /// 单个整数算子（spec §7）。<see cref="Key"/> = 资源/标志/权重台阶键；
    /// <see cref="Amount"/> = 整数量；<see cref="Note"/> = flavor，不参与结算。
    /// </summary>
    public sealed record EffectOp(EffectOpKind Kind, string? Key, int Amount, string? Note);

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
