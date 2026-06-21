using System.Collections.Generic;

namespace Jianghu.Cultivation
{
    /// <summary>
    /// 负向压制矩阵（fullstruct-005 / B5 模块化效果系统）。特定 tag 对目标 tag 的战力衰减机制：
    /// 当攻方 tag 命中防方 tag 时，伤害乘以压制比例（整数 ×10）。如 阴→阳 ratio=8（×0.8 压制），
    /// 魔→佛 ratio=7（×0.7 压制）。压制值钳 [0, MaxRatio] 防过压/反转。
    ///
    /// <para>Public API：<see cref="GetSuppressionRatio"/> 供 DuelEngine 查询，
    /// balance-001/002 矩阵 dump 亦依赖此公共接口。</para>
    ///
    /// <para>全静态无状态：纯数据驱动，新增压制对只需在 <see cref="Default"/> 加 Rule 行。</para>
    /// </summary>
    public static class SuppressionMatrix
    {
        /// <summary>最大压制比例（×10）：20 = ×2.0（优势上限）。</summary>
        public const int MaxRatio = 20;

        /// <summary>最小压制比例（×10）：0 = ×0.0（完全压制/免疫，禁负值反转）。</summary>
        public const int MinRatio = 0;

        /// <summary>无压制时的中性比例（×10）：10 = ×1.0 = 无影响。</summary>
        public const int NeutralRatio = 10;

        /// <summary>单条压制规则：攻方带 AttackerTag 对防方带 DefenderTag 时生效，Ratio 为 ×10 整数比例。</summary>
        public sealed record Rule(string AttackerTag, string DefenderTag, int Ratio, string Note);

        /// <summary>
        /// 完整压制矩阵（≥2 种压制对，AC 5.2）。零 PathId，纯 tag 驱动。
        /// 新增压制对：在此数组追加 Rule 行即可，无需改代码。
        /// </summary>
        public static readonly IReadOnlyList<Rule> Default = new[]
        {
            // ═══ 阴→阳 压制（阴气克阳气，Bible §6.4 counterWheel）═══
            new Rule("yin", "yang", 8, "阴→阳 压制"),
            // ═══ 魔→佛 压制（魔性克佛性，Bible §6.4）═══
            new Rule("mo", "fo", 7, "魔→佛 压制"),
        };

        /// <summary>
        /// 查压制矩阵：攻方 tag 对防方 tag 的压制比例（×10）。
        /// 返回钳位后的比例：<see cref="MinRatio"/> ≤ result ≤ <see cref="MaxRatio"/>。
        /// 无匹配 → 返回 <see cref="NeutralRatio"/>（10 = ×1.0 = 无影响）。
        /// 匹配策略：首个命中即返回（多规则不叠加）。
        /// </summary>
        /// <param name="attackerTags">攻方 SituationalTags</param>
        /// <param name="defenderTags">防方 SituationalTags</param>
        /// <returns>压制比例（×10），钳 [MinRatio, MaxRatio]</returns>
        public static int GetSuppressionRatio(
            IEnumerable<string> attackerTags,
            IEnumerable<string> defenderTags)
        {
            foreach (var rule in Default)
            {
                foreach (var aTag in attackerTags)
                {
                    if (aTag != rule.AttackerTag) continue;
                    foreach (var dTag in defenderTags)
                    {
                        if (dTag == rule.DefenderTag)
                        {
                            return Clamp(rule.Ratio);
                        }
                    }
                }
            }
            return NeutralRatio;
        }

        /// <summary>钳比例到 [MinRatio, MaxRatio]，禁反转/过乘。</summary>
        private static int Clamp(int ratio)
        {
            if (ratio < MinRatio) return MinRatio;
            if (ratio > MaxRatio) return MaxRatio;
            return ratio;
        }
    }
}
