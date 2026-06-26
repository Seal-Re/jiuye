using System;
using System.Collections.Generic;
using Jianghu.Model;

namespace Jianghu.Drama
{
    /// <summary>点火候选——一条强恩怨 + 其整数权重（drama-007b）。</summary>
    public sealed record IgnitionCandidate(Grudge Grudge, int Weight);

    /// <summary>
    /// 点火候选扫描器（drama-007b，spec §3.4C/§4）。
    /// **只扫 ledger.AboveIntensity(threshold)** 强恩怨（O(强恩怨) 非 O(全员)，INV-PERF）；
    /// 过滤「持有者已有活跃弧 / 持有者或仇人已亡 / 对子冷却中」；
    /// 权重 max(1,intensity) 兜底；返回按 (Weight desc, Grudge.Id asc) 确定性排序。
    /// 上层（drama-009 Pump）再以 WeightedPicker 抽取。
    /// </summary>
    public static class IgnitionScanner
    {
        /// <summary>
        /// 收集合格点火候选。hasActiveArc / onPairCooldown 由 Pump/Director（drama-009）注入
        /// （活跃弧集 + 冷却表在其持有），本扫描器只吃谓词保解耦可测。
        /// HashSet/Dictionary 仅可用于成员测试（谓词内），其枚举序不得参与裁决。
        /// </summary>
        public static IReadOnlyList<IgnitionCandidate> FindIgnitions(
            GrudgeLedger ledger,
            int threshold,
            IDramaView view,
            Func<CharacterId, bool> hasActiveArc,
            Func<CharacterId, CharacterId, bool> onPairCooldown)
        {
            var cands = new List<IgnitionCandidate>();

            // 只遍历强恩怨（账本侧已稳定排序，O(强恩怨数)）。
            var strong = ledger.AboveIntensity(threshold);
            for (int i = 0; i < strong.Count; i++)
            {
                var g = strong[i];
                if (hasActiveArc(g.Holder)) continue;                 // 一人一时一条复仇主线
                if (!view.IsAlive(g.Holder) || !view.IsAlive(g.Target)) continue; // 参与者须在世
                if (onPairCooldown(g.Holder, g.Target)) continue;     // 对子冷却内不二次点火
                int weight = g.Intensity < 1 ? 1 : g.Intensity;       // w>=1 兜底（B.2）
                cands.Add(new IgnitionCandidate(g, weight));
            }

            // 确定性排序：Weight desc, Grudge.Id asc（不依赖账本枚举序）。
            cands.Sort((a, b) =>
            {
                int c = b.Weight.CompareTo(a.Weight);
                if (c != 0) return c;
                return a.Grudge.Id.Value.CompareTo(b.Grudge.Id.Value);
            });
            return cands;
        }
    }
}
