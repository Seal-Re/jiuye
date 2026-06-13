using System;
using Jianghu.Config;
using Jianghu.Stats;

namespace Jianghu.Cultivation
{
    /// <summary>
    /// per-path 声明式整数战力（spec §6）。三段式：terms→BaseSum→Modifiers→Curve。
    /// 全整数，long 防溢，每步 clamp，禁浮点（S0-b IL 扫描守护）。
    /// 道心解耦（R3/R-A-NF7）：<see cref="Resolve"/> 遇 daoHeart/innerDemon src 抛异常，
    /// daoHeart/innerDemon 不进 BaseSum/Modifier。
    /// </summary>
    public static class PowerEngine
    {
        /// <summary>
        /// 三段式整数求值（spec §6）：
        /// BaseSum = Σ (term.Weight + WeightStep(term.WeightStepKey, st)) × Resolve(term.Src, st, stats)；
        /// Modified = clamp(BaseSum 顺序过 Modifiers, 0, ..)（整数 x*Num/Den 每步 clamp）；
        /// final = (long)Modified × Curve.RealmMultipliers[RealmIndex] / 10，叠 PostMul，Clamp(0, PowerCap)。
        /// </summary>
        public static int Evaluate(CultivationState st, StatBlock stats, CultivationPathDef def, LimitsConfig limits)
        {
            var f = def.Power;

            // 段 1：terms → BaseSum
            long baseSum = 0;
            foreach (var term in f.Terms)
            {
                int weight = term.Weight + WeightStep(term.WeightStepKey, st);
                long src = Resolve(term.Src, st, stats, def);
                baseSum += (long)weight * src;
            }
            long acc = Clamp(baseSum, 0, limits.PowerCap);

            // 段 2：顺序过 Modifiers（整数定点 x*Num/Den，每步 clamp [0, PowerCap]）
            if (f.Modifiers != null)
            {
                foreach (var mod in f.Modifiers)
                {
                    if (!WhenHolds(mod.When, st)) continue;
                    acc = ApplyMod(acc, mod);
                    acc = Clamp(acc, 0, limits.PowerCap);
                }
            }
            long modified = acc;

            // 段 3：境界曲线放大（÷10 定点）+ PostMul（flag 条件 num/den）→ Clamp(0, PowerCap)
            long final = modified * def.Curve.RealmMultipliers[st.RealmIndex] / 10;
            if (f.PostMuls != null)
            {
                foreach (var pm in f.PostMuls)
                {
                    if (pm.WhenFlag != null && !FlagSet(pm.WhenFlag, st)) continue;
                    final = final * pm.Num / pm.Den;
                }
            }
            final = Clamp(final, 0, limits.PowerCap);
            return (int)final;
        }

        /// <summary>
        /// 解析 term.Src 为整数（公开 3 参版，无 def 上下文）。护栏：res:daoHeart / res:innerDemon
        /// 抛 <see cref="ArgumentException"/>（R3/R-A-NF7，道心严禁进战力）。无 def 时 sumArtPower 占 0。
        /// </summary>
        public static long Resolve(string src, CultivationState st, StatBlock stats)
            => Resolve(src, st, stats, null);

        // 内部 4 参版：带 def 才能对 sumArtPower 做 ΣChosenArt tier 折算（A.0 按 tier 求和占位）。
        internal static long Resolve(string src, CultivationState st, StatBlock stats, CultivationPathDef? def)
        {
            // R3 护栏：道心/心魔 src 在任何解引用前直接拒（不读 st/stats）。
            if (src == "res:daoHeart" || src == "res:innerDemon")
                throw new ArgumentException($"道心/心魔禁进战力公式（R3/R-A-NF7）: {src}", nameof(src));

            switch (src)
            {
                case "stat:Force": return stats.Get(StatKind.Force);
                case "stat:Internal": return stats.Get(StatKind.Internal);
                case "stat:Constitution": return stats.Get(StatKind.Constitution);
                case "stat:Insight": return stats.Get(StatKind.Insight);
                case "realm": return st.RealmIndex;
                case "sumArtPower": return SumArtPower(st, def);
            }

            if (src.StartsWith("res:", StringComparison.Ordinal))
            {
                string key = src.Substring(4);
                return st.Resources.TryGetValue(key, out int v) ? v : 0;
            }
            if (src.StartsWith("derived:", StringComparison.Ordinal))
            {
                string key = src.Substring(8);
                return DerivedRegistry.Resolve(key, st, stats); // A.0 空注册返回 0
            }

            throw new ArgumentException($"未知战力项 src: {src}", nameof(src));
        }

        /// <summary>WeightStepKey 升某 term 权重台阶：读 st.Flags（与 AddTermWeightStep 落 Flags 一致）；空键=0。</summary>
        private static int WeightStep(string? weightStepKey, CultivationState st)
        {
            if (weightStepKey == null) return 0;
            return st.Flags.TryGetValue(weightStepKey, out int step) ? step : 0;
        }

        // ΣChosenArt tier 折算（A.0 按 tier 求和占位）：ChosenArtIds 对应 def 内 ArtDef.Tier 求和。
        private static long SumArtPower(CultivationState st, CultivationPathDef? def)
        {
            if (def == null) return 0;
            long sum = 0;
            foreach (var artId in st.ChosenArtIds)
            {
                foreach (var cat in def.ArtCategories)
                {
                    foreach (var art in cat.Arts)
                    {
                        if (art.Id == artId) { sum += art.Tier; break; }
                    }
                }
            }
            return sum;
        }

        // ModKind 分派（整数定点 x*Num/Den）。新增 ModKind 在此追加并显式标 L1（R5）。
        private static long ApplyMod(long x, PowerMod mod)
        {
            switch (mod.Kind)
            {
                case "mul":   // 乘性整数定点 x*Num/Den
                    return x * mod.Num / mod.Den;
                case "addFlat": // 加性整数
                    return x + mod.Num;
                default:
                    throw new ArgumentException($"未知 ModKind: {mod.Kind}", nameof(mod));
            }
        }

        // Modifier/PostMul 的 When/WhenFlag 谓词：A.0 仅支持「flag:<key>」置位判定与空(恒真)。
        private static bool WhenHolds(string? when, CultivationState st)
        {
            if (when == null) return true;
            if (when.StartsWith("flag:", StringComparison.Ordinal))
                return FlagSet(when.Substring(5), st);
            return true;
        }

        private static bool FlagSet(string key, CultivationState st)
            => st.Flags.TryGetValue(key, out int v) && v != 0;

        private static long Clamp(long v, long min, long max)
        {
            if (v < min) return min;
            if (v > max) return max;
            return v;
        }
    }
}
