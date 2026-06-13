using System.Collections.Generic;

namespace Jianghu.Random
{
    public static class RandomExtensions
    {
        /// <summary>Fisher–Yates 原地洗牌，确定性（用注入 IRandom）。</summary>
        public static void Shuffle<T>(this IRandom rng, IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.NextInt(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
