using System;
using System.Collections.Generic;

namespace Jianghu.Cultivation
{
    /// <summary>
    /// 软情境战斗（spec §8，替代废止的 CounterMatrix R2）。**绝对零 PathId**：边只认
    /// 战斗轴 + 上下文谓词（tag + 环境），剑与体之间没有边。胜负 = 境界×per-path 战力主导，
    /// 情境只修正不翻盘（adj clamp ±P0/4）。纯整数，禁浮点。
    /// </summary>

    /// <summary>
    /// 软情境修正边。<see cref="Axis"/> ∈ element|range|terrain|time|form|alignment（战斗轴，非 PathId）；
    /// <see cref="WhenPred"/> = 谓词 DSL（读 attacker/defender 的 tag/axis + 环境，全 AND）；
    /// <see cref="CoefPct"/> = 整数 % adj（正=增益攻方，负=减益）。
    /// </summary>
    public sealed record SituationalEdge(string Axis, string WhenPred, int CoefPct);

    /// <summary>
    /// 战斗情境上下文（spec §8）。仅 tag/axis/环境，**无 PathId**。
    /// <see cref="AttackerTags"/>/<see cref="DefenderTags"/> = 双方属性/形态 tag；
    /// <see cref="Axis"/> = 攻方 AttackDimension（作 attacker.axis）；
    /// <see cref="Env"/> = 环境字典（distance/is_night/terrain/qiDensity… 键→值，整数或字符串）。
    /// </summary>
    public sealed record SitContext(
        IReadOnlyList<string> AttackerTags,
        IReadOnlyList<string> DefenderTags,
        string Axis,
        IReadOnlyDictionary<string, string> Env);

    /// <summary>
    /// 软情境结算器（spec §8）：<see cref="AdjPct"/> = clamp(Σ 命中边.CoefPct, -p0/4, +p0/4)。
    /// p0 由调用方传（LimitsConfig.SituationalP0Base 或战力基准）。
    /// 解析/求值全程**不读 PathId 字段**（R2）；纯整数确定性。
    /// </summary>
    public sealed class SituationalResolver
    {
        private readonly IReadOnlyList<SituationalEdge> _edges;

        public SituationalResolver(IReadOnlyList<SituationalEdge> edges)
        {
            _edges = edges;
        }

        /// <summary>
        /// 累加全部命中边的 CoefPct，再钳到 [-p0/4, +p0/4]（情境只修正不翻盘）。
        /// 命中 = 该边 WhenPred 在 ctx 下为真。
        /// </summary>
        public int AdjPct(SitContext ctx, int p0)
        {
            int sum = 0;
            foreach (var edge in _edges)
            {
                if (Evaluate(edge.WhenPred, ctx))
                    sum += edge.CoefPct;
            }
            int bound = p0 / 4;
            return Clamp(sum, -bound, bound);
        }

        // —— WhenPred 小 DSL：'&' 连接多个原子（全 AND），全真才命中 ——
        // 原子形如 attacker.tag:X / defender.tag:X / attacker.axis:X / env:K op V。
        private static bool Evaluate(string pred, SitContext ctx)
        {
            foreach (var raw in pred.Split('&'))
            {
                var atom = raw.Trim();
                if (atom.Length == 0) continue;
                if (!EvaluateAtom(atom, ctx))
                    return false;
            }
            return true;
        }

        private static bool EvaluateAtom(string atom, SitContext ctx)
        {
            if (atom.StartsWith("attacker.tag:", StringComparison.Ordinal))
                return Contains(ctx.AttackerTags, atom.Substring("attacker.tag:".Length));
            if (atom.StartsWith("defender.tag:", StringComparison.Ordinal))
                return Contains(ctx.DefenderTags, atom.Substring("defender.tag:".Length));
            if (atom.StartsWith("attacker.axis:", StringComparison.Ordinal))
                return ctx.Axis == atom.Substring("attacker.axis:".Length);
            if (atom.StartsWith("env:", StringComparison.Ordinal))
                return EvaluateEnv(atom.Substring("env:".Length), ctx.Env);

            throw new ArgumentException($"未知情境谓词原子: {atom}", nameof(atom));
        }

        // env 原子：K op V。op ∈ = | != | >= | <= | > | <。比较 >= <= > < 用整数，= != 用字符串。
        private static bool EvaluateEnv(string expr, IReadOnlyDictionary<string, string> env)
        {
            // 先匹配双字符算子（>=/<=/!=），再匹配单字符（>/</=），顺序不可换。
            foreach (var op in TwoCharOps)
            {
                int i = expr.IndexOf(op, StringComparison.Ordinal);
                if (i >= 0)
                    return CompareEnv(expr.Substring(0, i), op, expr.Substring(i + op.Length), env);
            }
            foreach (var op in OneCharOps)
            {
                int i = expr.IndexOf(op, StringComparison.Ordinal);
                if (i >= 0)
                    return CompareEnv(expr.Substring(0, i), op, expr.Substring(i + op.Length), env);
            }
            throw new ArgumentException($"未知 env 谓词（缺比较算子）: {expr}", nameof(expr));
        }

        private static readonly string[] TwoCharOps = { ">=", "<=", "!=" };
        private static readonly string[] OneCharOps = { "=", ">", "<" };

        private static bool CompareEnv(string key, string op, string val, IReadOnlyDictionary<string, string> env)
        {
            key = key.Trim();
            val = val.Trim();
            if (!env.TryGetValue(key, out var actual))
                return false; // env 缺键 → 不命中

            switch (op)
            {
                case "=":  return actual == val;
                case "!=": return actual != val;
                case ">=": return ToInt(actual) >= ToInt(val);
                case "<=": return ToInt(actual) <= ToInt(val);
                case ">":  return ToInt(actual) > ToInt(val);
                case "<":  return ToInt(actual) < ToInt(val);
                default:
                    throw new ArgumentException($"未知 env 比较算子: {op}", nameof(op));
            }
        }

        private static int ToInt(string s)
        {
            if (!int.TryParse(s, out int v))
                throw new ArgumentException($"env 数值比较需整数: '{s}'", nameof(s));
            return v;
        }

        private static bool Contains(IReadOnlyList<string> tags, string tag)
        {
            foreach (var t in tags)
                if (t == tag) return true;
            return false;
        }

        private static int Clamp(int v, int min, int max)
        {
            if (v < min) return min;
            if (v > max) return max;
            return v;
        }
    }
}
