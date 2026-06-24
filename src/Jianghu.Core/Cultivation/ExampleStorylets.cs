using System.Collections.Generic;

namespace Jianghu.Cultivation
{
    /// <summary>
    /// 10 条范例奇遇（story-013 引擎验证模板）。
    /// 加奇遇=加数据行。完整 50+ 条见 story-026 内容积压。
    /// </summary>
    public static class ExampleStorylets
    {
        public static IReadOnlyList<StoryletDef> All = new StoryletDef[]
        {
            // 1. 山洞遗宝 (Treasure/Common)
            new("cave_relic_01", "山洞遗宝", StoryletCategory.Treasure, StoryletRarity.Common,
                Salience: 80, MinRealm: 0, Tags: new[] { "cave" },
                Options: new[]
                {
                    new StoryletOption("取宝", DaoHeartDelta: 3, null, null,
                        new Dictionary<string, int> { { "灵石", 50 } }, null),
                    new StoryletOption("离去", DaoHeartDelta: 1, null, null, null, null),
                }),

            // 2. 散修劫道 (Battle/Common)
            new("ambush_01", "散修劫道", StoryletCategory.Battle, StoryletRarity.Common,
                Salience: 70, MinRealm: 0, Tags: new[] { "ambush" },
                Options: new[]
                {
                    new StoryletOption("迎战", null, InnerDemonDelta: 2, ProgressDelta: 5, null, null),
                    new StoryletOption("避让", null, InnerDemonDelta: -1, null, null, null),
                }),

            // 3. 前辈指点 (Mentor/Uncommon)
            new("mentor_01", "前辈指点", StoryletCategory.Mentor, StoryletRarity.Uncommon,
                Salience: 60, MinRealm: 0, Tags: new[] { "mentor" },
                Options: new[]
                {
                    new StoryletOption("虚心求教", DaoHeartDelta: 5, null, ProgressDelta: 10, null, null),
                    new StoryletOption("婉拒", DaoHeartDelta: 2, null, null, null, null),
                }),

            // 4. 心魔试炼 (Battle/Uncommon)
            new("ambush_09", "心魔试炼", StoryletCategory.Battle, StoryletRarity.Uncommon,
                Salience: 50, MinRealm: 0, Tags: new[] { "innerDemon" },
                Options: new[]
                {
                    new StoryletOption("力抗心魔", DaoHeartDelta: 5, InnerDemonDelta: 5, null, null, null),
                    new StoryletOption("退避守心", null, InnerDemonDelta: -2, null, null, null),
                }),

            // 5. 灵药发现 (Treasure/Uncommon)
            new("cave_relic_05", "灵药发现", StoryletCategory.Treasure, StoryletRarity.Uncommon,
                Salience: 75, MinRealm: 0, Tags: new[] { "herb" },
                Options: new[]
                {
                    new StoryletOption("采集", null, null, ProgressDelta: 10,
                        new Dictionary<string, int> { { "灵石", 30 } }, null),
                    new StoryletOption("留待有缘", DaoHeartDelta: 4, null, null, null, null),
                }),

            // 6. 地下坊市 (Trade/Uncommon)
            new("trade_01", "地下坊市", StoryletCategory.Trade, StoryletRarity.Uncommon,
                Salience: 55, MinRealm: 0, Tags: new[] { "market" },
                Options: new[]
                {
                    new StoryletOption("交易灵石换丹药", null, null, null,
                        new Dictionary<string, int> { { "灵石", -40 } }, null),
                    new StoryletOption("以物易物", null, null, null,
                        new Dictionary<string, int> { { "灵石", 20 } }, null),
                    new StoryletOption("不参与", null, null, null, null, null),
                }),

            // 7. 同门相助 (Relation/Uncommon)
            new("relation_01", "同门相助", StoryletCategory.Relation, StoryletRarity.Uncommon,
                Salience: 65, MinRealm: 0, Tags: new[] { "fellow" },
                Options: new[]
                {
                    new StoryletOption("出手相助", DaoHeartDelta: 4, null, null, null, RelationDelta: 15),
                    new StoryletOption("袖手旁观", null, InnerDemonDelta: 1, null, null, RelationDelta: -5),
                }),

            // 8. 因果显现 (Fate/Epic)
            new("fate_01", "因果显现", StoryletCategory.Fate, StoryletRarity.Epic,
                Salience: 30, MinRealm: 4, Tags: new[] { "fate" },
                Options: new[]
                {
                    new StoryletOption("顺天应命", DaoHeartDelta: 10, InnerDemonDelta: -5, ProgressDelta: 20, null, null),
                    new StoryletOption("逆天改命", DaoHeartDelta: -5, InnerDemonDelta: 10, ProgressDelta: 25, null, null),
                    new StoryletOption("观望天命", null, null, ProgressDelta: 5, null, null),
                }),

            // 9. 古籍残卷 (Mentor/Common)
            new("mentor_03", "古籍残卷", StoryletCategory.Mentor, StoryletRarity.Common,
                Salience: 85, MinRealm: 0, Tags: new[] { "book" },
                Options: new[]
                {
                    new StoryletOption("潜心研读", DaoHeartDelta: 3, null, ProgressDelta: 5, null, null),
                    new StoryletOption("转售换灵石", null, null, null,
                        new Dictionary<string, int> { { "灵石", 80 } }, null),
                }),

            // 10. 天劫征兆 (Fate/Rare)
            new("fate_02", "天劫征兆", StoryletCategory.Fate, StoryletRarity.Rare,
                Salience: 25, MinRealm: 6, Tags: new[] { "tribulation" },
                Options: new[]
                {
                    new StoryletOption("迎劫而上", DaoHeartDelta: 8, InnerDemonDelta: 3, ProgressDelta: 15, null, null),
                    new StoryletOption("设法规避", null, InnerDemonDelta: -3, null, null, null),
                }),
        };
    }
}
