using System;
using System.Collections.Generic;
using Jianghu.Random;

namespace Jianghu.Util
{
    /// <summary>
    /// 整数轮盘抽取原语（drama-006，GDD §4）。**无状态** —— 对一张「权重表」按比例抽一个索引。
    /// 前缀和 + 单次 <see cref="IRandom.NextInt(int)"/> 抽取（串行消费主流，不自建子流）。
    /// long 累加防溢出；total 越 int.MaxValue 即抛（杜绝静默环绕，强制调用方用 MaxArcWeightSum 守门）。
    /// 零权重索引永不被选。纯整数确定性（同表+同 IRandom 状态→同索引）。
    ///
    /// 与 <see cref="VariedSelector{TKey}"/> 分工：VariedSelector = 有状态「少用优先」均匀轮替（破单调）；
    /// WeightedPicker = 无状态「权重比例」轮盘。戏剧点火（drama-007/009）按需取用。
    /// 裁决序（Weight desc, Id asc, …）由调用方预排序后传入，本原语不重排。
    /// </summary>
    public static class WeightedPicker
    {
        /// <summary>
        /// 按权重比例抽一个索引，返回 [0, weights.Count)。
        /// 空/null/含负权重/全零/total 越 int.MaxValue → 抛 <see cref="ArgumentException"/>。
        /// </summary>
        public static int PickIndex(IReadOnlyList<int> weights, IRandom rng)
        {
            if (weights == null || weights.Count == 0)
                throw new ArgumentException("WeightedPicker.PickIndex: 权重表为空", nameof(weights));
            if (rng == null)
                throw new ArgumentException("WeightedPicker.PickIndex: rng 为空", nameof(rng));

            // 前缀和总量（long 防溢出）；负权重非法；越 int.MaxValue 即抛（picker 须在 int 抽取域内）。
            long total = 0;
            for (int i = 0; i < weights.Count; i++)
            {
                int w = weights[i];
                if (w < 0)
                    throw new ArgumentException($"WeightedPicker.PickIndex: 负权重 weights[{i}]={w}", nameof(weights));
                total += w;
                if (total > int.MaxValue)
                    throw new ArgumentException("WeightedPicker.PickIndex: 权重和越 int.MaxValue（调用方须以 MaxArcWeightSum 守门）", nameof(weights));
            }
            if (total <= 0)
                throw new ArgumentException("WeightedPicker.PickIndex: 权重和 <= 0（调用方须以 w>=1 兜底）", nameof(weights));

            // 单次抽取 [0,total)，命中首个 draw < 前缀和 的索引。
            int draw = rng.NextInt((int)total);
            long acc = 0;
            for (int i = 0; i < weights.Count; i++)
            {
                acc += weights[i];
                if (draw < acc) return i;
            }
            return weights.Count - 1; // 理论不可达（draw<total 必命中）；防御兜底。
        }
    }
}
