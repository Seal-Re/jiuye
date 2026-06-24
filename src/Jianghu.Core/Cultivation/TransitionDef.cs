using System;
using System.Collections.Generic;

namespace Jianghu.Cultivation
{
    /// <summary>A.3 跃迁形态（A123 §A.3.1）。</summary>
    public enum TransitionKind { Transmute, Awaken, DualCultivation }

    /// <summary>跃迁门控——复合条件（AND）。</summary>
    public sealed record TransitionGate(
        int RealmMin,                                      // 最低境界 (RealmIndex)
        IReadOnlyDictionary<string, int>? ResourceReqs,    // 资源需求
        string? KarmicPredicate                            // 因果谓词（可选）
    );

    /// <summary>carryover 规则——转职时保留/丢弃的内容。</summary>
    public sealed record CarryoverRule(
        IReadOnlyList<string> KeepResources,               // 保留的资源 key 列表
        IReadOnlyList<string> KeepArts,                    // 保留的功法 ID 列表
        int RealmMappingOffset                             // 境界映射偏移（0=对应境界，-1=降一级，+1=升一级）
    );

    /// <summary>
    /// 跃迁定义——转职/觉醒/双修的统一数据模型（A123 §A.3.1）。
    /// 数据驱动：加跃迁=加数据行，不改引擎。
    /// </summary>
    public sealed record TransitionDef(
        string Id,                           // 唯一标识
        TransitionKind Kind,                 // 跃迁形态
        string FromPathId,                   // 源路线（觉醒时为 null→同路线内）
        string? ToPathId,                    // 目标路线（觉醒时为 null→不换路线）
        TransitionGate Gate,                 // 门控条件
        CarryoverRule? Carryover,            // carryover 规则（觉醒时为 null）
        int Cost                             // 跃迁代价（灵石或类似）
    );

    /// <summary>
    /// 跃迁注册表（story-001）。数据驱动。
    /// </summary>
    public sealed class TransitionRegistry
    {
        private readonly List<TransitionDef> _all;
        private readonly Dictionary<string, TransitionDef> _byId;

        public TransitionRegistry(IReadOnlyList<TransitionDef> defs)
        {
            _all = new List<TransitionDef>(defs);
            _byId = new Dictionary<string, TransitionDef>();
            foreach (var d in _all) _byId[d.Id] = d;
        }

        public IReadOnlyList<TransitionDef> All => _all;
        public TransitionDef ById(string id) => _byId[id];
        public int Count => _all.Count;

        /// <summary>按源路线和跃迁形态筛选。</summary>
        public List<TransitionDef> Eligible(string fromPathId, TransitionKind kind)
        {
            var list = new List<TransitionDef>();
            foreach (var d in _all)
                if (d.FromPathId == fromPathId && d.Kind == kind)
                    list.Add(d);
            return list;
        }
    }
}
