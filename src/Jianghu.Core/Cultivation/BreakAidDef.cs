using System;
using System.Collections.Generic;

namespace Jianghu.Cultivation
{
    /// <summary>
    /// BreakAid 四法枚举（A2-FINAL §3.5, story-007）。
    /// </summary>
    public enum BreakAidMethod { Seclusion = 0, Epiphany = 1, Resource = 2, Guardian = 3 }

    /// <summary>
    /// 单破障方法定义。数据驱动：加方法=加数据行。
    /// </summary>
    public sealed record BreakAidDef(
        BreakAidMethod Method,
        int BreakProgressBonus,   // 突破进度增量
        int InnerDemonRisk,       // 心魔风险（正=增心魔）
        int DaoHeartReq,          // 道心最低要求
        IReadOnlyDictionary<string, int>? ResourceCost // 资源消耗（可选）
    );

    /// <summary>
    /// BreakAid 注册表（story-007）。四法预定义。
    /// </summary>
    public static class BreakAidRegistry
    {
        public static readonly BreakAidDef Seclusion = new(
            BreakAidMethod.Seclusion,
            BreakProgressBonus: 30, // max: min(K*4,30)
            InnerDemonRisk: 3,
            DaoHeartReq: 0,
            ResourceCost: null);

        public static readonly BreakAidDef Epiphany = new(
            BreakAidMethod.Epiphany,
            BreakProgressBonus: 25,
            InnerDemonRisk: 0,
            DaoHeartReq: 20, // need Insight>=20
            ResourceCost: null);

        public static readonly BreakAidDef Resource = new(
            BreakAidMethod.Resource,
            BreakProgressBonus: 15,
            InnerDemonRisk: -2, // reduces innerDemon
            DaoHeartReq: 0,
            ResourceCost: new Dictionary<string, int> { { "灵石", 50 } });

        public static readonly BreakAidDef Guardian = new(
            BreakAidMethod.Guardian,
            BreakProgressBonus: 20,
            InnerDemonRisk: -5,
            DaoHeartReq: 30,
            ResourceCost: null);

        public static BreakAidDef Get(BreakAidMethod method) => method switch
        {
            BreakAidMethod.Seclusion => Seclusion,
            BreakAidMethod.Epiphany => Epiphany,
            BreakAidMethod.Resource => Resource,
            BreakAidMethod.Guardian => Guardian,
            _ => throw new ArgumentOutOfRangeException(nameof(method))
        };

        /// <summary>获取闭关 streak K 的实际收益（钳 [0,30]）。</summary>
        public static int SeclusionBonus(int streak) => Math.Min(streak * 4, 30);
    }
}
