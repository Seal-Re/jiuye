using System.Collections.Generic;
using Jianghu.Random;
using Jianghu.Util;

namespace Jianghu.Drama
{
    /// <summary>
    /// storylet 选择器（drama-007，spec §3 候选 + §4 裁决序）。
    /// 过滤(Arc==弧种 &amp;&amp; Stage==弧阶段 &amp;&amp; AllPass 谓词) → 确定性排序(BaseWeight desc, Id asc)
    /// → 权重 max(1,BaseWeight) 兜底 → WeightedPicker 整数轮盘抽取。
    /// **空候选→null 且不消费 rng**（no-op 守恒，承 B.3）。**禁 Dictionary/HashSet 枚举序参与裁决**。
    /// </summary>
    public static class StoryletSelector
    {
        /// <summary>
        /// 在 pool 中为当前弧+上下文选一个合格 storylet；无合格候选→null（不抽取 rng）。
        /// </summary>
        public static StoryletSpec? Select(
            IReadOnlyList<StoryletSpec> pool, ArcInstance arc, DramaContext ctx, IRandom rng)
        {
            // ① 过滤：弧种 + 阶段 + 谓词全过。
            var cands = new List<StoryletSpec>();
            for (int i = 0; i < pool.Count; i++)
            {
                var s = pool[i];
                if (s.Arc != arc.Kind || s.Stage != arc.Stage) continue;
                if (!ctx.AllPass(s.Preconditions)) continue;
                cands.Add(s);
            }
            if (cands.Count == 0) return null; // no-op：不建权重、不调 rng。

            // ② 确定性排序：BaseWeight desc, Id asc（不依赖 pool 原序中的任何枚举序）。
            cands.Sort((a, b) =>
            {
                int c = b.BaseWeight.CompareTo(a.BaseWeight); // 权重降序
                if (c != 0) return c;
                return a.Id.CompareTo(b.Id);                  // Id 升序
            });

            // ③ 权重 max(1,·) 兜底（防全零 → WeightedPicker total>0）。
            var weights = new List<int>(cands.Count);
            for (int i = 0; i < cands.Count; i++)
            {
                int w = cands[i].BaseWeight;
                weights.Add(w < 1 ? 1 : w);
            }

            // ④ 整数轮盘抽取。
            int idx = WeightedPicker.PickIndex(weights, rng);
            return cands[idx];
        }
    }
}
