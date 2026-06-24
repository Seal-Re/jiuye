using System;
using System.Collections.Generic;
using Jianghu.Random;
using Jianghu.Stats;

namespace Jianghu.Cultivation
{
    /// <summary>
    /// 数据驱动 TribulationDef（§A1 §3.6）。劫不是引擎里 switch(劫型) 读不同 stat，
    /// 而是 ResistTerms 列表声明——加第 4 种劫 = 加一条 TribulationDef 数据，零改 TribulationResolver。
    /// </summary>
    public sealed record TribulationDef(
        /// <summary>劫标识：tribulation / heavenly / heart_demon</summary>
        string Key,
        /// <summary>抗劫项列表：每项 Src×Weight 累加得 TribScore 基底</summary>
        IReadOnlyList<TribulationTerm> ResistTerms,
        /// <summary>门值表达式（占位：后续 story 用 GateExpr 做动态门）</summary>
        string GateExpr,
        /// <summary>失败分支列表</summary>
        IReadOnlyList<TribulationFailBranch> FailBranches);

    /// <summary>
    /// 抗劫项：Src 可为 EffectivePower/Foundation/Constitution/Insight/InnerDemon/任意资源key。
    /// InnerDemon 用负 Weight 表达"心魔越重越难过心魔劫"。
    /// </summary>
    public sealed record TribulationTerm(string Src, int Weight);

    /// <summary>
    /// 失败分支：TribScore 在不同 band 内触发的后果。
    /// BandLow/BandHigh 为相对于 TribGate 的偏移（负=低于门值）。
    /// </summary>
    public sealed record TribulationFailBranch(
        int BandLow, int BandHigh,
        CultivationPhase FailTarget,
        string EffectDesc);

    /// <summary>
    /// 三劫结算器（纯整数，确定性，数据驱动）。新劫型 = 加 TribulationDef 数据。
    /// </summary>
    public static class TribulationResolver
    {
        /// <summary>渡劫基线门值（按 UT 缩放：BASE × (UT+1) / 2）。</summary>
        public const int BaseTribGate = 40;

        /// <summary>天谴威胁惩罚基线（来自 AmbientQi 等环境压）。</summary>
        public const int BaseThreatPenalty = 0;

        /// <summary>Roll 带宽度（±band），保"强者大概率过+留爆冷"。</summary>
        public const int RollBand = 8;

        // —— 内置三劫定义 ——

        /// <summary>渡劫（修为劫）：EffectivePower + Foundation + 灵力池等资源。</summary>
        public static TribulationDef Tribulation => new TribulationDef(
            "tribulation",
            new TribulationTerm[]
            {
                new("EffectivePower", 3),
                new("Foundation", 2),
                new("manaPool", 1),
            },
            "BASE_TRIB_GATE * (UT+1) / 2",
            new TribulationFailBranch[]
            {
                new(-10, 0, CultivationPhase.Setback, "轻伤: tribDebt+=余伤, Foundation-20"),
                new(-25, -11, CultivationPhase.Setback, "跌境: major-1, Foundation-40, TribGate永久+ΔP"),
                new(int.MinValue, -26, CultivationPhase.Fallen, "陨落: 渡劫致命"),
            });

        /// <summary>天劫（雷劫/肉身劫）：Constitution + henglian 横练值。</summary>
        public static TribulationDef HeavenlyTribulation => new TribulationDef(
            "heavenly",
            new TribulationTerm[]
            {
                new("Constitution", 4),
                new("henglian", 3),
                new("qixue", 1),
            },
            "BASE_TRIB_GATE * (UT+1) / 2 * 1.2",
            new TribulationFailBranch[]
            {
                new(-10, 0, CultivationPhase.Setback, "雷噬轻伤: tribDebt+=余伤"),
                new(-25, -11, CultivationPhase.Fallen, "雷噬过载·陨落"),
            });

        /// <summary>心魔劫：Insight + (−InnerDemon) + 道心资源。</summary>
        public static TribulationDef HeartDemonTribulation => new TribulationDef(
            "heart_demon",
            new TribulationTerm[]
            {
                new("Insight", 3),
                new("InnerDemon", -2), // 心魔越重越难过
                new("daoHeart", 2),
            },
            "BASE_TRIB_GATE * (UT+1) / 2",
            new TribulationFailBranch[]
            {
                new(-10, 0, CultivationPhase.Deviation, "走火: innerDemon+20, 需PurgeRoll自救"),
                new(int.MinValue, -11, CultivationPhase.Fallen, "心魔噬主·陨落"),
            });

        /// <summary>三劫全量注册表。</summary>
        public static readonly IReadOnlyDictionary<string, TribulationDef> All = new Dictionary<string, TribulationDef>
        {
            ["tribulation"] = Tribulation,
            ["heavenly"] = HeavenlyTribulation,
            ["heart_demon"] = HeartDemonTribulation,
        };

        // ================================================================
        // 结算
        // ================================================================

        /// <summary>
        /// 计算 TribScore = Σ(src×weight) − ThreatPenalty + Roll(±band)。
        /// </summary>
        /// <param name="def">劫定义</param>
        /// <param name="st">修炼状态（读资源/stat）</param>
        /// <param name="stats">角色四维</param>
        /// <param name="threatPenalty">环境威胁惩罚（默认 0）</param>
        /// <param name="rng">确定性 PRNG（Roll 带）</param>
        /// <returns>TribScore</returns>
        public static int ComputeTribScore(
            TribulationDef def, CultivationState st, StatBlock stats,
            int threatPenalty = 0, IRandom? rng = null)
        {
            long sum = 0;
            foreach (var term in def.ResistTerms)
            {
                int src = ResolveTerm(term.Src, st, stats);
                sum += (long)term.Weight * src;
            }
            sum -= threatPenalty;
            int roll = rng?.NextInt(RollBand * 2 + 1) - RollBand ?? 0; // [−band, +band]
            sum += roll;
            return (int)Math.Max(int.MinValue, Math.Min(int.MaxValue, sum));
        }

        /// <summary>
        /// 计算 TribGate = BASE × (UT+1) / 2。
        /// </summary>
        public static int ComputeTribGate(TribulationDef def, int unifiedTier)
        {
            // GateExpr 后续 story 扩展；当前用基线公式
            return BaseTribGate * (unifiedTier + 1) / 2;
        }

        /// <summary>
        /// 根据 TribScore vs TribGate 确定失败分支。
        /// </summary>
        public static TribulationFailBranch? GetFailBranch(TribulationDef def, int tribScore, int tribGate)
        {
            int offset = tribScore - tribGate;
            foreach (var branch in def.FailBranches)
            {
                if (offset >= branch.BandLow && offset <= branch.BandHigh)
                    return branch;
            }
            return null; // 通过（score >= gate）
        }

        /// <summary>
        /// 解析 TribulationTerm.Src → 整数值。
        /// Src 可为: EffectivePower / Foundation / Constitution / Insight / InnerDemon / 任意资源key。
        /// </summary>
        private static int ResolveTerm(string src, CultivationState st, StatBlock stats)
        {
            switch (src)
            {
                case "EffectivePower":
                    // 占位：PowerEngine.Evaluate 需要 path+limits，此处分层调用（后续 story 接线）
                    // 当前用 stat sum 近似
                    return stats.Sum;
                case "Foundation":
                    return st.Flags.TryGetValue("foundation", out int f) ? f : 0;
                case "Constitution":
                    return stats.Get(StatKind.Constitution);
                case "Insight":
                    return stats.Get(StatKind.Insight);
                case "InnerDemon":
                    return st.Flags.TryGetValue("innerDemon", out int id) ? id : 0;
                default:
                    // 资源 key（如 manaPool / henglian / qixue / daoHeart）
                    return st.Resources.TryGetValue(src, out int r) ? r : 0;
            }
        }
    }
}
