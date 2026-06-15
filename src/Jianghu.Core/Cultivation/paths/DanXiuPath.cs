using System.Collections.Generic;

namespace Jianghu.Cultivation.Paths
{
    /// <summary>
    /// 丹修·炼丹师 <c>dan_xiu</c>（economic 资源枢纽代表路）。数据照《每路深度设计》丹修节 +
    /// 《内容补遗》第八部「8. 丹修 dan_xiu — 道心：丹火定鼎心」+ 命名池丹修条目。
    /// 弱战力强干预·厚积晚发·双战力分裂：directPower（受击时挨打用，权重压到最低、直面被秒、需护道人）
    /// 与 alchemyLeverage（真正定义本路强弱=开炉造化+改写他人 stat 的丹力，靠异火阶/丹方库阶跃）。
    /// 不打人改人：三类丹（突破/疗伤→恩义依附正边；毒/夺元→死仇负边；买丹积分→economic 晋升），半解耦品阶。
    ///
    /// 红线落实：terms 无 ×0、无 daoHeart（R3/R6）——深度设计「武力×0/根骨×0」属 ×0 项，按约定**整体不发该 term**
    /// （丹修练拳脚/堆肉身毫无收益是与剑修/体修的硬分野，故二项弃权不入项，非写 ×0）；realm 乘性放大由 RealmCurve
    /// 倍率承载（power 内放一个 realm 正权项=directPower/alchemyLeverage 各 ×2 的线性项，不放 realm×0）。
    /// SituationalTags=属性/形态 tag 非对手 PathId（R2）；RealmCurve 四列等长（M4）；含 1 个 Role=daoheart 类目
    /// pillheart（M1，A.0 仅装载不结算 → tier=0 使 sumArtPower 贡献 0、effects 留空不触 daoHeart 资源算子）。
    /// canon pathId（R4）。纯整数，禁浮点。
    /// </summary>
    public static class DanXiuPath
    {
        public static readonly CultivationPathDef Def = Build();

        // —— 被动算子（装配期落 state，核心 EffectOpKind）。功法的「火候power+N/药理power+N/悟性+N」等改派生项
        //    A.0 为 flavor 不落算子（生成期 Σ=80 不被功法污染，深度设计「功法只加 power 不改 Σ」），仅以 Note 留痕；
        //    能落 state 的「异火阶/丹方数+N」走 AddResource、被动开关/资源上限走 GrantPassive/AddResourceCap。
        //    注：核心 10 个 EffectOpKind 无「造关系边(Relations.Adjust)」「改他人四维(Stats.Apply)」算子——
        //    那是丹修跨路资源交互（深度设计§跨路边界缺口、需独立 InteractionResolver+权限闸）L1 留给后续；
        //    A.0 不引入新算子，全部「改人/造边」效果仅以 Note 注释表达，战技用 AddPenInteger 近似量级占位。——
        private static EffectOp Passive(string flag, string note)
            => new EffectOp(EffectOpKind.GrantPassive, flag, 1, note);

        private static EffectOp Flame(int amt, string note)
            => new EffectOp(EffectOpKind.AddResource, "flameTier", amt, note);

        private static EffectOp Recipe(int amt, string note)
            => new EffectOp(EffectOpKind.AddResource, "recipeCount", amt, note);

        private static CultivationPathDef Build()
        {
            // —— per-path 专属资源（不进四维 C6）——
            // flameTier 异火阶：异火榜阶位（0=斗气火焰,1=兽火,2..8=异火榜名次倒序），本路战力真正脊柱、决定可炼丹阶上限，
            //   控火功法靠 AddResource 阶跃抬升；base cap=8（异火榜首=帝炎 FlameTier=8，深度设计 realmMul 与异火阶皆 0..8）。
            // recipeCount 丹方数：已掌握具名丹方条数（凡人正典「无方则失败率剧增」），丹方功法 +1，0..9（九品丹方）。
            // pillStock 成品丹库存：开炉炼丹的造化产出（economic 维度），战技吞服/赠丹/卖丹消耗它；0..50。
            var resources = new[]
            {
                new ResourceDef("flameTier", 0, 8, 0),
                new ResourceDef("recipeCount", 0, 9, 0),
                new ResourceDef("pillStock", 0, 50, 0),
            };

            // —— 战力公式（深度设计 alchemyLeverage terms：悟性×3 + 内力×1 + realm×2 + 火候power×2 + 药理power×1 +
            //    异火阶FlameTier×8 + 丹方数RecipeCount×2；directPower 的 内力×1+realm×2 与此重叠故单项即可）。
            //    武力×0/根骨×0 按约定不发 term（R6 禁 ×0；丹修练手劲/堆肉身毫无意义=与剑修/体修硬分野）。
            //    异火阶×8=alchemyLeverage 最高单权重（本路脊柱、台阶式跃迁）；悟性×3=灵魂感知（炼药三要素）卡丹阶上限。
            //    sumArtPower 聚合火候/药理/控火/丹方各功法 tier 之和（引擎单口径,取火候主权重 2;药理×1 之差 A.0 以 Note 留痕）。
            //    无 daoHeart、无 ×0（R3/R6）。——
            var power = new PowerFormulaDef(
                new[]
                {
                    new PowerTerm("stat:Insight", 3, null),       // 悟性：alchemyLeverage 第一权重=灵魂感知力(炼药三要素之一),卡丹阶上限
                    new PowerTerm("stat:Internal", 1, null),      // 内力：directPower 护体与运火 + alchemyLeverage 控火耗内息各占 1(非杀招)
                    new PowerTerm("realm", 2, null),              // 境界：directPower/alchemyLeverage 各 ×2;倍率低(资源全砸丹炉,境界涨得慢)
                    new PowerTerm("res:flameTier", 8, null),      // 异火阶：本路最高单权重=战力真正脊柱,每 +1 可炼丹阶上限 +1、杠杆跳 +8(台阶式)
                    new PowerTerm("res:recipeCount", 2, null),    // 丹方数：库越广=可造的关系边种类越多;凡人无方则失败率剧增
                    new PowerTerm("sumArtPower", 2, null),        // 所选火候/药理/控火/丹方各功法 tier 之和(火候权重 2 主口径)
                },
                System.Array.Empty<PowerMod>(),
                null);

            // —— 战力曲线（深度设计 directPower realmMul=[1,1,2,2,3,3,4,5]，realm 0..7，全路最低、近乎平坦——同 realm 被剑/体碾压，
            //    必须有护道人）。本路真正成长是 alchemyLeverage 的异火阶跃迁（FlameTier+1→杠杆跳+8,台阶式而非平滑,由 res:flameTier
            //    项承载,不入此倍率曲线）→『直接战力一条平线 + 杠杆值一串台阶』双曲线,厚积晚发。
            //    四列等长（M4）：倍率 / UnifiedTierOf（UT0-12 映射,枢纽型低 realm 段刻意≤高爆发路同阶）/
            //    境界名（散修学徒→灵境初→灵境→天境初→天境→帝境初→帝境→丹帝;灵魂境界卡丹阶:灵境炼八品/天境九品/帝境帝品）/
            //    升入阈值（不靠打怪经验,靠炼成更高丹阶的丹×N 炉 + 灵魂感知达门槛,realm0=0 起累进）。——
            var curve = new RealmCurveDef(
                new[] { 1, 1, 2, 2, 3, 3, 4, 5 },
                new[] { 0, 2, 4, 6, 8, 10, 11, 12 },
                new[] { "散修学徒", "灵境初", "灵境", "天境初", "天境", "帝境初", "帝境", "丹帝" },
                // 炼成更高丹阶的丹×N 炉 + 灵魂感知达灵/天/帝门槛累进（≥100×当前realm 升阶,枢纽路厚积晚发故阈值平稳）。
                new[] { 0, 100, 300, 600, 1000, 1500, 2100, 2800 },
                // —— A.1 境界稿 §2：起步 SubLevelCount 全 1；CanAscend=true；MaxMajor=大境界数-1。——
                new[] { 1, 1, 1, 1, 1, 1, 1, 1 }, true, 7);

            // —— 功法类目（丹方/火候/药理/控火 各 4~5 具名 + pillheart 道心 4 具名）。
            //    具名/效果照深度设计「功法类目」节;命名与命名池丹修条目同源。改他人 stat/造关系边/成丹率等
            //    核心算子集无对应项 → 仅 Note 留痕,可落 state 的「异火阶/丹方数 +N」走 AddResource。——
            var arts = new[]
            {
                // 丹方（配方库——决定能造哪些丹/造什么关系边;条数计入 recipeCount(权重2),无方则失败率剧增）。每习一方 recipeCount+1。
                new ArtCategoryDef("丹方", "recipe", 1, 2, new[]
                {
                    new ArtDef("da_df_juqi", "聚气丹方", 1, "丹方",
                        new[] { Recipe(1, "可炼【聚气丹】:对目标内力+2(Stats.Apply,L1跨路);丹方数+1;炼成阶上限=丹阶2") }),
                    new ArtDef("da_df_huiyuan", "回元疗伤丹方", 2, "丹方",
                        new[] { Recipe(1, "可炼【回元丹】:受伤目标根骨+3并造恩义正边Relations.Adjust(+8,L1);丹方数+1") }),
                    new ArtDef("da_df_pozhang", "破障筑基丹方", 3, "丹方",
                        new[] { Recipe(1, "可炼【筑基丹/升仙丸】:使目标 realm 突破判定+1档(产突破),造强依附正边+15(L1);阶上限=丹阶5") }),
                    new ArtDef("da_df_jiuzhuan", "九转金身丹方", 4, "丹方",
                        new[] { Recipe(1, "可炼【九转金丹】:目标根骨+5+内力+5并造死忠依附边+20(L1);需千年药材+异火阶≥4,阶上限=丹阶7") }),
                    new ArtDef("da_df_duoyuan", "夺元化魂丹方", 5, "丹方",
                        new[] { Recipe(1, "[毒丹暗杀分支]可炼【夺元丹】:目标内力-6+悟性-4并造死仇负边-25(L1);隐匿入药难溯源,阶上限=丹阶6") }),
                }),
                // 火候（控火稳定度——抬开炉成丹率与丹纹品质期望;power 之和计入 alchemyLeverage(权重2),把随机往有上限高期望推）。
                new ArtCategoryDef("火候", "internal", 1, 1, new[]
                {
                    new ArtDef("da_hh_wenhuo", "文火诀", 1, "火候",
                        new[] { Passive("steady_low", "火候功法power+2:开炉成丹率基线+10(整数百分点),降炸炉概率") }),
                    new ArtDef("da_hh_wuhuo", "武火真意", 2, "火候",
                        new[] { Passive("yield_up", "火候功法power+4:成丹数期望+1,丹纹(品质)圈数期望+0(只提量不提质)") }),
                    new ArtDef("da_hh_wenwu", "文武相济火候", 3, "火候",
                        new[] { Passive("balanced_fire", "火候功法power+6:成丹率+15且丹纹期望+1(三纹青灵丹式:每+1纹品质逼近上一品)") }),
                    new ArtDef("da_hh_dingding", "心火不乱定鼎诀", 4, "火候",
                        new[] { Passive("calm_furnace", "火候功法power+9:炸炉概率减半,丹阶上限+1(异火阶允许内多吃一阶),悟性低于门槛仍能稳炉") }),
                }),
                // 药理（药材调和——扩药材兼容年份/化解相冲/催熟;power 之和计入 alchemyLeverage(权重1),决定资源端可得性与上限）。
                new ArtCategoryDef("药理", "support", 1, 1, new[]
                {
                    new ArtDef("da_yl_baicao", "百草辨识录", 1, "药理",
                        new[] { Passive("herb_id", "药理功法power+2:可用药材年份上限+百年档,识破假药/劣药") }),
                    new ArtDef("da_yl_tiaohe", "药力调和术", 2, "药理",
                        new[] { Passive("reconcile", "药理功法power+4:化解双药相冲,单炉可投药材种数+1,丹纯度提升(丹纹期望+0但成丹率+8)") }),
                    new ArtDef("da_yl_cuishu", "灵草催熟法", 3, "药理",
                        new[] { Passive("ripen", "药理功法power+6:自种灵草可催熟跨1档年份(百年→千年,韩立掌天瓶式),使千年丹方可达") }),
                    new ArtDef("da_yl_wangu", "万古药藏通解", 4, "药理",
                        new[] { Passive("myriad_herb", "药理功法power+9:可用万年药材,单炉投药种数+2,毒/疗双理皆通(夺元丹与回元丹同炉可切)") }),
                }),
                // 控火（异火驾驭——抬升 flameTier 异火阶(权重8,本路最强单项);战力曲线的『台阶』来源,每驯一阶造化跳变）。
                // 落 state:AddResource(flameTier,+N) 阶跃(SetTo 语义核心集无→用增量 AddResource 经 chokepoint 钳到 cap)。
                new ArtCategoryDef("控火", "flame", 1, 1, new[]
                {
                    new ArtDef("da_kh_jichu", "驭火基础心法", 1, "控火",
                        new[] { Flame(1, "可驾驭FlameTier≤1(斗气火焰/兽火):异火阶→1,丹阶上限基线=3") }),
                    new ArtDef("da_kh_naru", "纳火入体诀", 2, "控火",
                        new[] { Flame(3, "可吞纳异火榜末位异火,FlameTier可达4(自基线累进):异火阶+,丹阶上限+,alchemyLeverage 阶跃+8/阶") }),
                    new ArtDef("da_kh_tongyuan", "异火同源融炼", 3, "控火",
                        new[] { Flame(2, "可同时驾驭2种异火并融合提阶,FlameTier可达6:丹阶上限+1,开炉火力溢出可加炼1枚高纹丹") }),
                    new ArtDef("da_kh_diyan", "万火归炉帝焰诀", 4, "控火",
                        new[]
                        {
                            Flame(2, "可凝『帝炎』级火种,FlameTier可达8(异火榜首):达帝境者炼帝品丹,丹阶上限=8,异火项达峰值"),
                            Passive("emperor_flame", "本路终极资源杠杆:alchemyLeverage 异火阶项达峰值(8×8=64)"),
                        }),
                }),
                // pillheart 道心类目（M1,补遗第八部「丹火定鼎心」pillheart 丹心·守炉之念）。A.0 仅装载不结算 → tier=0
                // （sumArtPower 贡献 0）、effects 留空（不触 daoHeart/innerDemon/comprehension 资源算子,那是 A.2 道心层的事）。
                // 具名 + power=0（守炉静心诀/丹纹悟道录/临炉不急心经/文武定鼎道心,照补遗 pillheart 表）。
                new ArtCategoryDef("丹心", "daoheart", 1, 1, new[]
                {
                    new ArtDef("dh_da_shoulu", "守炉静心诀", 0, "丹心", System.Array.Empty<EffectOp>()),
                    new ArtDef("dh_da_danwen", "丹纹悟道录", 0, "丹心", System.Array.Empty<EffectOp>()),
                    new ArtDef("dh_da_buji", "临炉不急心经", 0, "丹心", System.Array.Empty<EffectOp>()),
                    new ArtDef("dh_da_dinging", "文武定鼎道心", 0, "丹心", System.Array.Empty<EffectOp>()),
                }),
            };

            // —— 战技（深度设计「战技」节,OnUse 算子 + Cost 资源表;弹药=pillStock 成品丹库存）。
            //    丹修无主动杀招:自卫/改人/造边为主。**改人造网(夺元改stat/施丹造关系边/聚丹换realm)非战斗机制、
            //    核心算子集缺(ApplyStatDelta/AdjustRelationEdge 未建) → 显式 deferred EPIC-COMBAT-FULLSTRUCT
            //    (红线 A.8 不静默;§10 覆盖账丹标'占位战斗+改人造网 deferred')**。本批仅炸炉(异火引爆)可结构化为
            //    PenFromResource(flameTier);夺元/毒烟/施丹/聚丹保 Note 占位待 FULLSTRUCT L1 接。——
            var skills = new[]
            {
                // 炸炉自爆（孤注）：主动引爆丹炉,对当前节点全体(含自身)施 directPower 比例范围伤害;被逼绝境的同归招(正典炸炉武器化)。
                // 牺牲全部在炉药材+自身重伤,消耗成品丹3(在炉储备)。
                // B5批2: → PenFromResource(flameTier,4) 异火阶越高炉越猛(占位战斗;plan原写directPower但其非本路资源,锚真资源flameTier)。
                new CombatSkillDef("sk_da_zhalu", "炸炉自爆", 3,
                    new[] { Modules.PenFromResource("flameTier", 4, note: "引爆丹炉对当前节点全体(含自身)施异火阶比例范围伤害,牺牲全部在炉药材+自身重伤(同归绝境招)") },
                    new Dictionary<string, int> { { "pillStock", 3 } }),
                // 夺元一击（毒丹暗杀分支·斩首）：对单一高威胁目标暗下夺元丹,内力-6+悟性-4并造死仇负边-25(L1);
                // 不溯源者最强阴招,专破他路高战力者的资源依赖。消耗1枚夺元丹(pillStock) + 一次接触机会窗口。
                new CombatSkillDef("sk_da_duoyuan", "夺元一击", 4,
                    new[] { new EffectOp(EffectOpKind.AddPenInteger, null, 28, "对单一高威胁目标暗下夺元丹:内力-6+悟性-4并造死仇负边-25(L1跨路改stat/造边),专破他路高战力者资源依赖") },
                    new Dictionary<string, int> { { "pillStock", 1 } }),
                // 护身丹爆（自保）：战斗中吞服储备丹,directPower 临时+realm×2 维持3结算步;纯自卫不增杀伤(丹修无主动杀招写照)。
                // 消耗1枚成品丹(pillStock)。
                new CombatSkillDef("sk_da_hushen", "护身丹爆", 1,
                    new[] { new EffectOp(EffectOpKind.AddFlatDR, null, 12, "吞服储备丹,directPower临时+realm×2维持3结算步,纯自卫不增杀伤(丹修无主动杀招)") },
                    new Dictionary<string, int> { { "pillStock", 1 } }),
                // 回元急救（资源克制·正边）：对濒死友方即时灌服回元丹,根骨+3拉回战力并造强恩义正边+12(L1);把疗伤变人情债。
                // 消耗1枚回元丹(pillStock)。
                new CombatSkillDef("sk_da_huiyuan", "回元急救", 2,
                    new[] { new EffectOp(EffectOpKind.AddFlatDR, null, 10, "对濒死友方即时灌服回元丹:根骨+3拉回战力并造强恩义正边+12(L1造边),把疗伤变人情债(资源克制造网)") },
                    new Dictionary<string, int> { { "pillStock", 1 } }),
                // 毒烟丹·撒豆（毒丹暗杀分支）：投掷毒烟丹,对当前节点全体非己目标内力-2并降其受击战力1步;造群体微负边(一念发情丹乱军式)。
                // 消耗1枚毒丹(pillStock),暴露概率低。
                new CombatSkillDef("sk_da_duyan", "毒烟丹·撒豆", 2,
                    new[] { new EffectOp(EffectOpKind.AddPenInteger, null, 8, "投掷毒烟丹对当前节点全体非己:内力-2并降受击战力1步,造群体微负边(L1群体debuff/造边,一念发情丹乱军式)") },
                    new Dictionary<string, int> { { "pillStock", 1 } }),
                // 施丹结契（资源枢纽·造网）：非战斗主动技,向目标赠突破/疗伤丹,造依附正边+15(L1)并登记『丹债』;把战力弱转成关系网中心。
                // 消耗1枚高阶丹(pillStock),无战斗消耗。
                new CombatSkillDef("sk_da_jieqi", "施丹结契", 3,
                    new[] { new EffectOp(EffectOpKind.AddSituationalAdj, null, 0, "非战斗主动:向目标赠突破/疗伤丹造依附正边+15(L1造边)并登记丹债,把战力弱转成关系网中心(黄枫谷索丹=依附链)") },
                    new Dictionary<string, int> { { "pillStock", 1 } }),
                // 聚丹换酬（economic 维度）：卖丹换灵石/积分推进 realm 与资源(白小纯卖丹十万积分升阶);造化产出直接转修为,本路独有非武力晋升通道。
                // 出让成品丹库存(pillStock)。
                new CombatSkillDef("sk_da_huanchou", "聚丹换酬", 1,
                    new[] { new EffectOp(EffectOpKind.AddSituationalAdj, null, 0, "卖丹换灵石/积分推进realm与资源(L1 economic 晋升通道),造化产出直接转修为(白小纯卖丹十万积分升阶)") },
                    new Dictionary<string, int> { { "pillStock", 2 } }),
            };

            return new CultivationPathDef(
                "dan_xiu", "丹修·炼丹师",
                "economic",
                // 属性/形态 tag（economic 经济后勤 / support 资源干预辅助 / fire 控火炼丹本色），非对手 PathId（R2）。
                new[] { "economic", "support", "fire" },
                resources,
                power,
                curve,
                arts,
                skills,
                new EntryGateDef("tag:dan_root"),
                new SelectionRuleDef(3, 3), // 战技抽 3(深度设计选取规则:≥1 资源克制/造边技,至多 1 毒丹暗杀分支技控阴谋密度)
                null);
        }
    }
}
