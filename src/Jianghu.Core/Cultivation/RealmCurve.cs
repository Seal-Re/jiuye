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
        /// 四列（RealmMultipliers / UnifiedTierOf / RealmNames / RealmThresholds）必须等长，
        /// 否则数据非法（拦截 backlog2-M4：因果路原案 8 个 major vs 7 个 UT 映射）。
        /// </summary>
        public static void Validate(RealmCurveDef curve)
        {
            int m = curve.RealmMultipliers.Count, u = curve.UnifiedTierOf.Count,
                n = curve.RealmNames.Count, t = curve.RealmThresholds.Count;
            if (m != u || m != n || m != t)
                throw new InvalidOperationException(
                    $"RealmCurveDef 四列长度不等: Multipliers={m} UnifiedTierOf={u} Names={n} Thresholds={t}（R6/M4）");
        }
    }
}
