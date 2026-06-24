using System.Collections.Generic;

namespace Jianghu.Cultivation
{
    /// <summary>
    /// 奇遇内容批1（content-001, 20 entries）。
    /// 纯数据行——加奇遇=加数据行，不改引擎。
    /// </summary>
    public static class StoryletContentBatch1
    {
        public static IReadOnlyList<StoryletDef> All = new StoryletDef[]
        {
            // === Treasure (5) ===
            new("cave_relic_02", "上古遗府", StoryletCategory.Treasure, StoryletRarity.Rare,
                40, 4, new[] { "relic" }, new[] {
                    new StoryletOption("入府探秘", DaoHeartDelta: 5, null, ProgressDelta: 20,
                        new Dictionary<string, int> { { "灵石", 200 } }, null),
                    new StoryletOption("在外守候", DaoHeartDelta: 2, null, null, null, null),
                }),
            new("cave_relic_03", "灵石矿脉", StoryletCategory.Treasure, StoryletRarity.Common,
                90, 0, new[] { "mine" }, new[] {
                    new StoryletOption("开采", null, null, null, new Dictionary<string, int> { { "灵石", 100 } }, null),
                    new StoryletOption("标记留给宗门", DaoHeartDelta: 3, null, null, null, RelationDelta: 10),
                }),
            new("cave_relic_04", "丹师遗蜕", StoryletCategory.Treasure, StoryletRarity.Uncommon,
                50, 2, new[] { "alchemist" }, new[] {
                    new StoryletOption("继承丹道", DaoHeartDelta: 4, null, ProgressDelta: 15, null, null),
                    new StoryletOption("搜寻丹药", null, null, null, new Dictionary<string, int> { { "灵石", 150 } }, null),
                }),
            new("cave_relic_06", "千年灵药", StoryletCategory.Treasure, StoryletRarity.Uncommon,
                45, 2, new[] { "herb" }, new[] {
                    new StoryletOption("采药炼丹", null, null, ProgressDelta: 15,
                        new Dictionary<string, int> { { "灵石", 80 } }, null),
                    new StoryletOption("留待成熟", DaoHeartDelta: 5, null, null, null, null),
                }),
            new("cave_relic_07", "古传送阵", StoryletCategory.Treasure, StoryletRarity.Epic,
                20, 6, new[] { "portal" }, new[] {
                    new StoryletOption("启动传送", null, InnerDemonDelta: 3, ProgressDelta: 30,
                        new Dictionary<string, int> { { "灵石", 500 } }, null),
                    new StoryletOption("研究阵纹", DaoHeartDelta: 8, null, ProgressDelta: 10, null, null),
                    new StoryletOption("毁掉阵法", null, InnerDemonDelta: -2, null, null, null),
                }),

            // === Battle (5) ===
            new("ambush_02", "妖兽突袭", StoryletCategory.Battle, StoryletRarity.Uncommon,
                60, 2, new[] { "beast" }, new[] {
                    new StoryletOption("斩杀妖兽", null, InnerDemonDelta: 2, ProgressDelta: 10, null, null),
                    new StoryletOption("驯服妖兽", DaoHeartDelta: 3, null, null,
                        new Dictionary<string, int> { { "灵石", 60 } }, null),
                }),
            new("ambush_03", "邪修伏击", StoryletCategory.Battle, StoryletRarity.Uncommon,
                55, 2, new[] { "evil" }, new[] {
                    new StoryletOption("迎战", null, InnerDemonDelta: 3, ProgressDelta: 8, null, null),
                    new StoryletOption("以道心感化", DaoHeartDelta: 6, null, null, null, RelationDelta: 5),
                }),
            new("ambush_04", "宗门截杀", StoryletCategory.Battle, StoryletRarity.Rare,
                35, 4, new[] { "sect" }, new[] {
                    new StoryletOption("血战到底", null, InnerDemonDelta: 5, ProgressDelta: 15, null, null),
                    new StoryletOption("亮出师门令牌", null, null, null, null, RelationDelta: 10),
                }),
            new("ambush_06", "蛊群来袭", StoryletCategory.Battle, StoryletRarity.Uncommon,
                40, 3, new[] { "gu" }, new[] {
                    new StoryletOption("以火攻蛊", null, InnerDemonDelta: 1, ProgressDelta: 8, null, null),
                    new StoryletOption("收服蛊母", DaoHeartDelta: -2, null, ProgressDelta: 12, null, null),
                }),
            new("ambush_07", "仇家寻仇", StoryletCategory.Battle, StoryletRarity.Common,
                70, 0, new[] { "vengeance" }, new[] {
                    new StoryletOption("应战", null, InnerDemonDelta: 2, null, null, RelationDelta: -15),
                    new StoryletOption("化解恩怨", DaoHeartDelta: 5, InnerDemonDelta: -3, null, null, RelationDelta: 10),
                }),

            // === Mentor (5) ===
            new("mentor_02", "前辈遗念", StoryletCategory.Mentor, StoryletRarity.Uncommon,
                45, 2, new[] { "legacy" }, new[] {
                    new StoryletOption("领悟遗念", DaoHeartDelta: 4, null, ProgressDelta: 12, null, null),
                    new StoryletOption("默哀致意", DaoHeartDelta: 2, null, null, null, null),
                }),
            new("mentor_03_extra", "天机感悟", StoryletCategory.Mentor, StoryletRarity.Rare,
                30, 4, new[] { "fate" }, new[] {
                    new StoryletOption("冥思苦想", DaoHeartDelta: 6, null, ProgressDelta: 20, null, null),
                    new StoryletOption("顺其自然", DaoHeartDelta: 3, null, null, null, null),
                }),
            new("mentor_04", "剑意传承", StoryletCategory.Mentor, StoryletRarity.Epic,
                15, 6, new[] { "sword" }, new[] {
                    new StoryletOption("参悟剑意", DaoHeartDelta: 10, null, ProgressDelta: 25, null, null),
                    new StoryletOption("拒绝传承", DaoHeartDelta: 8, null, null, null, null),
                }),
            new("mentor_05", "同门论道", StoryletCategory.Mentor, StoryletRarity.Common,
                75, 0, new[] { "fellow" }, new[] {
                    new StoryletOption("切磋论道", DaoHeartDelta: 2, null, ProgressDelta: 5, null, RelationDelta: 8),
                    new StoryletOption("虚心旁听", DaoHeartDelta: 1, null, ProgressDelta: 3, null, null),
                }),
            new("mentor_06", "古籍解读", StoryletCategory.Mentor, StoryletRarity.Uncommon,
                50, 1, new[] { "book" }, new[] {
                    new StoryletOption("潜心解读", DaoHeartDelta: 3, null, ProgressDelta: 8, null, null),
                    new StoryletOption("请教师长", null, null, ProgressDelta: 6, null, RelationDelta: 5),
                }),

            // === Trade (3) ===
            new("trade_02", "秘境交易会", StoryletCategory.Trade, StoryletRarity.Rare,
                30, 4, new[] { "market" }, new[] {
                    new StoryletOption("竞拍灵石", null, null, null,
                        new Dictionary<string, int> { { "灵石", -200 } }, null),
                    new StoryletOption("以物换物", null, null, null,
                        new Dictionary<string, int> { { "灵石", 150 } }, RelationDelta: 5),
                }),
            new("trade_03", "灵药交换", StoryletCategory.Trade, StoryletRarity.Common,
                60, 0, new[] { "herb" }, new[] {
                    new StoryletOption("交换灵药", null, null, null,
                        new Dictionary<string, int> { { "灵石", 40 } }, null),
                    new StoryletOption("赠予对方", DaoHeartDelta: 4, null, null, null, RelationDelta: 15),
                }),

            // === Fate (2) ===
            new("fate_03", "命格异变", StoryletCategory.Fate, StoryletRarity.Rare,
                25, 4, new[] { "fate" }, new[] {
                    new StoryletOption("接受命运", DaoHeartDelta: 5, InnerDemonDelta: -3, ProgressDelta: 15, null, null),
                    new StoryletOption("抗争命运", null, InnerDemonDelta: 8, ProgressDelta: 20, null, null),
                }),

            // === Trade (1) ===
            new("trade_04", "功法买卖", StoryletCategory.Trade, StoryletRarity.Uncommon,
                55, 1, new[] { "market" }, new[] {
                    new StoryletOption("买入功法", null, null, ProgressDelta: 10,
                        new Dictionary<string, int> { { "灵石", -150 } }, null),
                    new StoryletOption("抄录一份", null, null, ProgressDelta: 5, null, null),
                }),

            // === Fate (1) ===
            new("fate_04", "轮回忆起", StoryletCategory.Fate, StoryletRarity.Epic,
                10, 8, new[] { "reincarnation" }, new[] {
                    new StoryletOption("追寻前世", DaoHeartDelta: 12, InnerDemonDelta: -8, ProgressDelta: 30, null, null),
                    new StoryletOption("放下执念", DaoHeartDelta: 5, InnerDemonDelta: -2, null, null, null),
                    new StoryletOption("今生为重", DaoHeartDelta: 3, null, null, null, null),
                }),
        };
    }
}
