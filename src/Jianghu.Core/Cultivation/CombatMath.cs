using System;

namespace Jianghu.Cultivation
{
    /// <summary>
    /// combat-variance cv-001（adr-0008 决策②）：Margin→关键事件触发概率的**整数查表**映射。
    /// 禁浮点 Sigmoid（B.2 IL 扫描守）——形状仿 Sigmoid 的整数 permille 桶表，落地为查表。
    ///
    /// 语义：<see cref="GetSuccessPermille"/> 返回攻方"命中/有效伤害"的千分比概率 p∈[1,999]，
    /// 由攻防 PE 的**相对差**（peMargin 相对 defenderPe 的百分比）分桶决定。同 PE→500(50/50 悬念)；
    /// 碾压→趋 999（保 1‰ 理论反杀，弱胜强叙事地基）；被碾压→趋 1。
    ///
    /// 纯整数、纯函数、无 RNG、无副作用——单测直验，确定性（B.2）。
    /// 桶边界/概率值仿 adr-0008 附录A（示例形状）；最终 [40,60]% 硬闸门标定在 cv-005（seed-sweep）。
    /// </summary>
    public static class CombatMath
    {
        /// <summary>同 PE（margin=0）基准命中率：50%。</summary>
        public const int BasePermille = 500;

        /// <summary>概率下钳（极端劣势仍保 1‰ 理论反杀，adr-0008 α "弱胜强地基"）。</summary>
        public const int MinPermille = 1;

        /// <summary>概率上钳（极端优势仍留 1‰ 失手，非绝对——绝对秒杀走 cv-004 溢出/auto-win）。</summary>
        public const int MaxPermille = 999;

        /// <summary>
        /// 攻方 PE − 防方 PE = <paramref name="peMargin"/>；<paramref name="defenderPe"/> = 防方战力（作相对基准）。
        /// 返回攻方命中千分比 p∈[<see cref="MinPermille"/>,<see cref="MaxPermille"/>]。
        ///
        /// 相对差（scale-invariant，跨 UT 通用）：relPct = peMargin×100 / max(1,defenderPe)。
        /// 分桶（仿 adr-0008 附录A）：每 1% 相对差 → ±5‰，即 permille = 500 + relPct×5，再钳 [1,999]。
        /// relPct≥100（碾压，攻方≥防方2倍）→ 上钳 999；relPct≤−100 → 下钳 1。
        /// </summary>
        public static int GetSuccessPermille(int peMargin, int defenderPe)
        {
            // 防 0 除 + 负 PE 兜底（PE 恒 ≥0，但 defenderPe=0 时用 1 作基准避免除零）。
            int basis = defenderPe > 0 ? defenderPe : 1;

            // 相对差百分比（整数）：先钳 peMargin 防 int 溢出（|peMargin| 上限取 basis×100 足够覆盖 [−100%,+100%]）。
            long clampedMargin = Math.Clamp((long)peMargin, -(long)basis * 100, (long)basis * 100);
            int relPct = (int)(clampedMargin * 100 / basis);

            // 每 1% 相对差 → 5‰ 偏移（仿附录A：±20%→±100‰、±60%→±300‰、±100%→±500‰... 此处线性桶）。
            long permille = BasePermille + (long)relPct * 5;

            return (int)Math.Clamp(permille, MinPermille, MaxPermille);
        }
    }
}
