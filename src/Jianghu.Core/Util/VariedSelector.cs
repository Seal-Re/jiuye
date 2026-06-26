using System.Collections.Generic;
using Jianghu.Random;

namespace Jianghu.Util
{
    /// <summary>
    /// "少用优先"均匀轮替选择器（drama-003，spec §5.5 Step 0 共享原语）。
    /// 状态 = 每元素 long 使用计数；Pick 在**最小计数子集**内用注入 IRandom 均匀抽、抽后 +1。
    /// 保证长程均匀轮替（破单调），纯整数确定性（同种子+同候选→同序列）。
    /// 供戏剧 storylet 去重 / 任何需均匀轮替的场景复用。Clone 深拷计数表（R-NF2 续跑安全）。
    /// </summary>
    public sealed class VariedSelector<TKey>
    {
        private readonly Dictionary<TKey, long> _usage;

        public VariedSelector()
        {
            _usage = new Dictionary<TKey, long>();
        }

        private VariedSelector(Dictionary<TKey, long> usage)
        {
            _usage = new Dictionary<TKey, long>(usage);
        }

        /// <summary>记一次使用（计数 +1）。</summary>
        public void Note(TKey key)
        {
            _usage.TryGetValue(key, out long c);
            _usage[key] = c + 1;
        }

        /// <summary>查询使用计数（未见过 = 0）。</summary>
        public long UsageOf(TKey key)
            => _usage.TryGetValue(key, out long c) ? c : 0;

        /// <summary>
        /// 在候选中"少用优先"抽一个：取最小计数子集（按 candidates 传入序，确定性），
        /// 用注入 rng 在该子集内均匀抽，抽后 Note。空候选抛 ArgumentException。
        /// </summary>
        public TKey Pick(IReadOnlyList<TKey> candidates, IRandom rng)
        {
            if (candidates == null || candidates.Count == 0)
                throw new System.ArgumentException("VariedSelector.Pick: 候选为空", nameof(candidates));

            // 求最小计数（纯整数）。
            long min = long.MaxValue;
            for (int i = 0; i < candidates.Count; i++)
            {
                long u = UsageOf(candidates[i]);
                if (u < min) min = u;
            }

            // 收集 == min 的候选，**保持传入序**（确定性，不依赖 Dictionary 枚举）。
            var minSet = new List<TKey>();
            for (int i = 0; i < candidates.Count; i++)
                if (UsageOf(candidates[i]) == min) minSet.Add(candidates[i]);

            // 最小计数子集内均匀抽。
            TKey picked = minSet[rng.NextInt(minSet.Count)];
            Note(picked);
            return picked;
        }

        /// <summary>深拷计数表（独立实例，续跑安全 R-NF2）。</summary>
        public VariedSelector<TKey> Clone() => new VariedSelector<TKey>(_usage);
    }
}
