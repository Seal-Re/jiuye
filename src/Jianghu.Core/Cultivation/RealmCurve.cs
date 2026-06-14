using System;

namespace Jianghu.Cultivation
{
    /// <summary>
    /// 境界曲线运算（spec §9）：突破判定 + 四列等长校验（R6 / backlog2-M4）。纯整数，禁浮点。
    /// </summary>
    public static class RealmCurve
    {
        /// <summary>
        /// 修为达「下一境阈值」则升一境（封顶不越界）；否则维持当前境界。
        /// A.0 确定性突破：达阈即升，不掷随机。
        /// </summary>
        public static int NextIndexIfReady(int currentIndex, int cultivationPoints, RealmCurveDef curve)
        {
            int next = currentIndex + 1;
            if (next < curve.RealmThresholds.Count && cultivationPoints >= curve.RealmThresholds[next])
                return next;
            return currentIndex;
        }

        /// <summary>
        /// 数据合法性校验（纯整数，禁浮点）：
        /// ① 四列（RealmMultipliers / UnifiedTierOf / RealmNames / RealmThresholds）等长
        ///    （沿用 backlog2-M4：因果路原案 8 个 major vs 7 个 UT 映射拦截）；
        /// ② A.1 多列校验（境界稿 §2/§11.2）：Σ SubLevelCount == flatIndex 数（前缀和闭合）+
        ///    SubLevelCount.Count == MaxMajor+1 + 每 SubLevelCount[i] ≥ 1 + UnifiedTierOf 非降 +
        ///    (CanAscend==false ⇒ UnifiedTierOf.Last() ≤ 9，武夫陆地神仙 UT9 封顶)。
        /// 新列不进运行期（NextIndexIfReady 不读），仅本校验/投影/查询消费。
        /// </summary>
        public static void Validate(RealmCurveDef curve)
        {
            int m = curve.RealmMultipliers.Count, u = curve.UnifiedTierOf.Count,
                n = curve.RealmNames.Count, t = curve.RealmThresholds.Count;
            if (m != u || m != n || m != t)
                throw new InvalidOperationException(
                    $"RealmCurveDef 四列长度不等: Multipliers={m} UnifiedTierOf={u} Names={n} Thresholds={t}（R6/M4）");

            // —— A.1：SubLevelCount 前缀和闭合（Σ == flatIndex 数）——
            int sub = curve.SubLevelCount.Count, sum = 0;
            for (int i = 0; i < sub; i++)
            {
                if (curve.SubLevelCount[i] < 1)
                    throw new InvalidOperationException(
                        $"SubLevelCount[{i}]={curve.SubLevelCount[i]} < 1（每大境界至少 1 小境界，境界稿 §2）");
                sum += curve.SubLevelCount[i];
            }
            if (sum != m)
                throw new InvalidOperationException(
                    $"Σ SubLevelCount={sum} ≠ flatIndex 数={m}（前缀和未闭合，境界稿 §2）");

            // —— A.1：大境界数与 MaxMajor 对齐 ——
            if (sub != curve.MaxMajor + 1)
                throw new InvalidOperationException(
                    $"SubLevelCount.Count={sub} ≠ MaxMajor+1={curve.MaxMajor + 1}（大境界数与 MaxMajor 不符，境界稿 §2）");

            // —— A.1：UnifiedTierOf 随境界非降 ——
            for (int i = 1; i < u; i++)
            {
                if (curve.UnifiedTierOf[i] < curve.UnifiedTierOf[i - 1])
                    throw new InvalidOperationException(
                        $"UnifiedTierOf 在 flatIndex {i} 降序（{curve.UnifiedTierOf[i - 1]}→{curve.UnifiedTierOf[i]}，UT 须非降，境界稿 §2）");
            }

            // —— A.1：武夫（CanAscend=false）顶段 = 陆地神仙 UT9 封顶 ——
            if (!curve.CanAscend && u > 0 && curve.UnifiedTierOf[u - 1] > 9)
                throw new InvalidOperationException(
                    $"CanAscend=false 但 UnifiedTierOf 顶={curve.UnifiedTierOf[u - 1]} > 9（武夫陆地神仙 UT9 封顶，境界稿 §11.2）");
        }
    }
}
