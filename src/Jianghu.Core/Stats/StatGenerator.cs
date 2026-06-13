using System;
using Jianghu.Config;
using Jianghu.Random;

namespace Jianghu.Stats
{
    /// <summary>偏中庸、定和+封顶、无拒绝采样（§5.2）。</summary>
    public static class StatGenerator
    {
        public static StatBlock Generate(IRandom rng, LimitsConfig c)
        {
            int n = c.StatCount;
            // 1) 集中度采样：每维取 Concentration 个 [0,1) 均值 → 趋向 0.5（中心极限→中庸）
            double[] w = new double[n];
            double total = 0;
            for (int i = 0; i < n; i++)
            {
                double acc = 0;
                for (int j = 0; j < c.Concentration; j++)
                    acc += rng.NextUInt() / 4294967296.0; // [0,1)
                w[i] = acc / c.Concentration;             // 趋中
                total += w[i];
            }
            // 2) 分配可分配预算 = Sum - n*Min 到各维，按权重；最大余数法整数化到精确和
            int budget = c.StatSum - n * c.StatMin;
            double[] raw = new double[n];
            int[] floor = new int[n];
            int assigned = 0;
            for (int i = 0; i < n; i++)
            {
                raw[i] = budget * (w[i] / total);
                floor[i] = (int)Math.Floor(raw[i]);
                assigned += floor[i];
            }
            int remainder = budget - assigned;
            var order = new int[n];
            for (int i = 0; i < n; i++) order[i] = i;
            Array.Sort(order, (a, b) =>
            {
                double fa = raw[a] - Math.Floor(raw[a]);
                double fb = raw[b] - Math.Floor(raw[b]);
                int cmp = fb.CompareTo(fa);
                return cmp != 0 ? cmp : a.CompareTo(b);
            });
            int[] v = new int[n];
            for (int i = 0; i < n; i++) v[i] = c.StatMin + floor[i];
            for (int i = 0; i < remainder; i++) v[order[i]]++;
            // 3) 封顶修正：越 cap 的溢出回流到未满维（保持总和）
            ClampToCapKeepingSum(v, c);
            return new StatBlock(v);
        }

        private static void ClampToCapKeepingSum(int[] v, LimitsConfig c)
        {
            int overflow = 0;
            for (int i = 0; i < v.Length; i++)
                if (v[i] > c.StatCap) { overflow += v[i] - c.StatCap; v[i] = c.StatCap; }
            int guard = 0;
            while (overflow > 0 && guard++ < 10000)
                for (int i = 0; i < v.Length && overflow > 0; i++)
                    if (v[i] < c.StatCap) { v[i]++; overflow--; }
        }
    }
}
