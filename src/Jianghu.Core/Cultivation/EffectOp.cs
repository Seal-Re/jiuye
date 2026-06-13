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
}
