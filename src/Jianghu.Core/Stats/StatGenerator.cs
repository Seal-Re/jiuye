using System;
using Jianghu.Config;
using Jianghu.Random;

namespace Jianghu.Stats
{
    /// <summary>偏中庸、定和+封顶、无拒绝、纯整数采样（§5.2；§4.1 禁浮点确定性路径，保证跨运行时复现）。</summary>
    public static class StatGenerator
    {
        public static StatBlock Generate(IRandom rng, LimitsConfig c)
        {
            int n = c.StatCount;
            // 1) 整数集中度权重：每维累加 Concentration 个 NextUInt（趋中由均值集中产生）
            ulong[] w = new ulong[n];
            ulong total = 0;
            for (int i = 0; i < n; i++)
            {
                ulong acc = 0;
                for (int j = 0; j < c.Concentration; j++) acc += rng.NextUInt();
                w[i] = acc;
                total += acc;
            }
            if (total == 0) { for (int i = 0; i < n; i++) w[i] = 1; total = (ulong)n; } // 退化保护（全 0 draws）

            // 2) 整数最大余数法：budget 按权重精确分配到精确和
            int budget = c.StatSum - n * c.StatMin;
            int[] floorv = new int[n];
            ulong[] rem = new ulong[n];
            int assigned = 0;
            for (int i = 0; i < n; i++)
            {
                ulong num = (ulong)budget * w[i];
                floorv[i] = (int)(num / total);
                rem[i] = num % total;
                assigned += floorv[i];
            }
            int remainder = budget - assigned;            // ∈ [0, n)
            int[] order = new int[n];
            for (int i = 0; i < n; i++) order[i] = i;
            Array.Sort(order, (a, b) =>
            {
                int cmp = rem[b].CompareTo(rem[a]);         // 余数大者优先
                return cmp != 0 ? cmp : a.CompareTo(b);     // 平手按索引（确定性）
            });
            int[] v = new int[n];
            for (int i = 0; i < n; i++) v[i] = c.StatMin + floorv[i];
            for (int i = 0; i < remainder; i++) v[order[i]]++;

            // 3) 封顶修正：越 cap 的溢出回流到未满维（保持总和，纯整数）
            ClampToCapKeepingSum(v, c);
            return new StatBlock(v);
        }

        private static void ClampToCapKeepingSum(int[] v, LimitsConfig c)
        {
            int overflow = 0;
            for (int i = 0; i < v.Length; i++)
                if (v[i] > c.StatCap) { overflow += v[i] - c.StatCap; v[i] = c.StatCap; }
            while (overflow > 0)
            {
                bool placed = false;
                for (int i = 0; i < v.Length && overflow > 0; i++)
                    if (v[i] < c.StatCap) { v[i]++; overflow--; placed = true; }
                if (!placed) throw new System.InvalidOperationException("回流容量不足：LimitsConfig 可行域越界，无法保持定和");
            }
        }
    }
}
