using System;
using System.Collections.Generic;

namespace Jianghu.Cultivation
{
    /// <summary>奇遇分类（A3-FINAL §4.4）。≥6 类，每类 ≥8 条。</summary>
    public enum StoryletCategory { Treasure, Battle, Mentor, Trade, Fate, Relation }

    /// <summary>奇遇稀有度。</summary>
    public enum StoryletRarity { Common = 0, Uncommon = 1, Rare = 2, Epic = 3 }

    /// <summary>奇遇选项——角色从中择一。纯数据，不可变。</summary>
    public sealed record StoryletOption(
        string Text,                                       // 选项文
        int? DaoHeartDelta,                                // 道心变化
        int? InnerDemonDelta,                              // 心魔变化
        int? ProgressDelta,                                // 修为进度变化
        IReadOnlyDictionary<string, int>? ResourceRewards, // 资源奖励
        int? RelationDelta                                 // 关系变化
    );

    /// <summary>
    /// 奇遇定义（A3-FINAL §4, story-013）。
    /// 数据驱动：加奇遇=加数据行，不改引擎。
    /// </summary>
    public sealed record StoryletDef(
        string Id,                           // 唯一标识
        string Title,                        // 标题
        StoryletCategory Category,           // 分类
        StoryletRarity Rarity,               // 稀有度
        int Salience,                        // 显著性初值 [0,100]
        int MinRealm,                        // 最低境界需求 (RealmIndex)
        IReadOnlyList<string> Tags,          // 情境标签（如 "vow"）
        IReadOnlyList<StoryletOption> Options // 2-4 个选项
    );

    /// <summary>
    /// 奇遇注册表（story-013）。内容池——持全量 StoryletDef。
    /// </summary>
    public sealed class StoryletRegistry
    {
        private readonly List<StoryletDef> _all;
        private readonly Dictionary<string, StoryletDef> _byId;

        public StoryletRegistry(IReadOnlyList<StoryletDef> defs)
        {
            _all = new List<StoryletDef>(defs);
            _byId = new Dictionary<string, StoryletDef>();
            foreach (var d in _all)
                _byId[d.Id] = d;
        }

        public IReadOnlyList<StoryletDef> All => _all;
        public StoryletDef ById(string id) => _byId[id];
        public int Count => _all.Count;

        /// <summary>按分类筛选。</summary>
        public List<StoryletDef> ByCategory(StoryletCategory cat)
        {
            var list = new List<StoryletDef>();
            foreach (var d in _all)
                if (d.Category == cat) list.Add(d);
            return list;
        }

        /// <summary>获取符合境界要求的可选奇遇列表。</summary>
        public List<StoryletDef> Eligible(int realmIndex)
        {
            var list = new List<StoryletDef>();
            foreach (var d in _all)
                if (realmIndex >= d.MinRealm)
                    list.Add(d);
            return list;
        }
    }
}
