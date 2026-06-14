using System.Collections.Generic;

namespace Jianghu.Cultivation.Paths
{
    /// <summary>
    /// 命修·因果时空 <c>ming_fate_causality</c>（气运轴）。数据照《每路深度设计》命修节
    /// 「### 命修·因果时空（气运轴）」+《内容补遗》第七部「7. 命修 ming_fate_causality — 道心：知命不惧心」+
    /// 命名池命修条目。气运正交泛克 + 先手信息战 + 双向反噬：不在四维正面拼，开辟独立资源【净气运】
    /// 为战力核心；厚积晚发、高方差低正面容错（凹起步→凸尾，前 7 阶 ≤ 剑修同阶物理孱弱）。
    ///
    /// 红线落实：terms 无 ×0、无 daoHeart/innerDemon（R3/R6 — 反噬经 netFortune 可为负承载，
    /// 非靠 daoHeart 项）；SituationalTags=属性/形态 tag 非对手 PathId（R2）；RealmCurve 四列等长（M4，
    /// realm 0..9 共 10 档）；含 1 个 Role=daoheart 类目 fateheart（M1，A.0 仅装载不结算 → tier=0
    /// 使 sumArtPower 贡献 0、effects 留空不触 daoHeart/innerDemon 资源算子）。canon pathId（R4）。纯整数，禁浮点。
    /// </summary>
    public static class MingFateCausalityPath
    {
        public static readonly CultivationPathDef Def = Build();

        // —— 被动算子（装配期落 state，核心 EffectOpKind）。功法的「悟性+N/推衍并发+N」等改四维/衍生项 A.0
        //    为 flavor 不落算子（生成期 Σ=80 不被功法污染），仅以 Note 留痕；能落 state 的「先手 Tempo 上限+N」
        //    走 AddResourceCap、被动开关/布局子走 GrantPassive/SetFlag、即时气运/先手走 AddResource。——
        private static EffectOp CapTempo(int amt, string note)
            => new EffectOp(EffectOpKind.AddResourceCap, "tempo", amt, note);

        private static EffectOp Passive(string flag, string note)
            => new EffectOp(EffectOpKind.GrantPassive, flag, 1, note);

        private static CultivationPathDef Build()
        {
            // —— per-path 专属资源（不进四维 C6）——
            // netFortune 净气运（=Fortune−Karma 的整数投影）：本路战力脊柱，A.0 单值承载，可为负 → 本场计负=反噬判负
            //   （故 Min 给负数，由 chokepoint 钳）。base cap=40（深度设计「起手 Fortune=悟性×2」高悟性≈40 量级）。
            // tempo 先手信息差档：卜算/推衍累积，开战削敌；上限随 realm 由功法 AddResourceCap 抬，A.0 单值起底 cap=4。
            // karma 天谴 / lifespanDebt 折寿：夺运/卜术的成本货币与反噬落点（深度设计成本不是内力而是天谴/折寿）。
            var resources = new[]
            {
                new ResourceDef("netFortune", -50, 40, 0),
                new ResourceDef("tempo", 0, 4, 0),
                new ResourceDef("karma", 0, 100, 0),
                new ResourceDef("lifespanDebt", 0, 100, 0),
            };

            // —— 战力公式（深度设计 terms：净气运×8 + tempo(先手信息差)×4 + 悟性×3(本路代替内力的主属性,衡 ForesightTerm)
            //    + realm×6 + 所选卜术/夺运/推衍功法 power×1 + 武力×1(仅作零头下限)）。净气运权重全路最高（正交泛克脊柱）；
            //    武力刻意权重 1（与剑/体修主靠武力形成鲜明对比）；无 daoHeart/innerDemon、无 ×0（R3/R6）。
            //    注：深度设计 InfoTempoTerm=敌战力×Tempo/16 是「减对方」的战斗期乘性削敌，A.0 单角色 Def 层
            //    以「tempo 正权项」近似先手信息压制带来的本人战力当量（完整削敌 Phase 3 接战斗结算）。——
            var power = new PowerFormulaDef(
                new[]
                {
                    new PowerTerm("res:netFortune", 8, null),   // 净气运：正交泛克脊柱，全路最高权重；为负则本场计负(反噬判负)
                    new PowerTerm("res:tempo", 4, null),        // 先手信息差：开战预知走位的战力当量(近似 InfoTempoTerm)
                    new PowerTerm("stat:Insight", 3, null),     // 悟性：本路代替内力的主属性(ForesightTerm=悟性×推衍并发)
                    new PowerTerm("realm", 6, null),            // 境界：净气运的乘性闸门(厚积晚发:低阶倍率小、高阶超线性)
                    new PowerTerm("sumArtPower", 1, null),      // 所选卜术/推衍/夺运 power 之和(信息压制广度,权重低于净气运)
                    new PowerTerm("stat:Force", 1, null),       // 武力：仅作零头下限,防完全无肉身(与剑/体修硬分野)
                },
                System.Array.Empty<PowerMod>(),
                null);

            // —— 战力曲线（深度设计 realmMul=[8,10,13,18,26,40,64,105,175,300]，realm 0..9 严格递增，凹起步→凸尾）。
            //    四列等长（M4，10 档）：倍率 / UnifiedTierOf（UT0-12 映射：观星窥命→渐入信息枢纽→泛克全场，
            //    厚积晚发故低阶 UT 压低、尾阶冲顶）/ 境界名（命修命格跃迁序：观星→知命→演卦→推衍→夺运→
            //    改命→衍天→大衍→窥天机→因果主）/ 升入阈值（命格跃迁里程累进，realm0=0 起，比剑修更陡=改命献祭代价）。——
            var curve = new RealmCurveDef(
                new[] { 8, 10, 13, 18, 26, 40, 64, 105, 175, 300 },
                new[] { 0, 1, 3, 5, 7, 8, 9, 10, 11, 12 },
                new[] { "观星", "知命", "演卦", "推衍", "夺运", "改命", "衍天", "大衍", "窥天机", "因果主" },
                // 命格跃迁里程（深度设计「改命献祭」途径）：升入第 i 境累进阈值 = Σ 120×(0..i-1)（比剑修 100× 更陡=对赌代价）。
                new[] { 0, 120, 360, 720, 1200, 1800, 2520, 3360, 4320, 5400 });

            // —— 功法类目（卜术/推衍/夺运/因果 各 5 具名 + fateheart 命心 4 具名）。
            //    具名/效果照深度设计「功法类目」节；命名与命名池命修条目同源。——
            var arts = new[]
            {
                // 卜术（卜算·占算类——产气运/先手，本路一切战力源头；多为低 Karma 常驻技，落 tempo 上限/即时资源）。
                new ArtCategoryDef("卜术", "divination", 1, 1, new[]
                {
                    new ArtDef("mi_bu_guanxing", "观星问卜", 1, "卜术",
                        new[] { new EffectOp(EffectOpKind.AddResource, "tempo", 1, "结算前推演:tempo+1(上限随realm),成本Karma+1,不折寿") }),
                    new ArtDef("mi_bu_quji", "趋吉避凶诀", 2, "卜术",
                        new[] { Passive("foresee_evade", "受袭时消netFortune-4换'敌先手tempo削减对己失效'(预知闪避),Karma+1") }),
                    new ArtDef("mi_bu_zhoutian", "周天卦象演算", 3, "卜术",
                        new[]
                        {
                            CapTempo(2, "演算引擎:tempo上限+2(中期先手扩容)"),
                            new EffectOp(EffectOpKind.AddResource, "netFortune", 3, "且netFortune+3,成本Karma+2/LifespanDebt+1"),
                        }),
                    new ArtDef("mi_bu_ziwei", "紫微斗数·命盘开演", 4, "卜术",
                        new[] { Passive("read_fate", "锁定一目标'读命':截其下一动作(必先手)且对其夺运反弹reflect减半,成本Karma+3/LifespanDebt+2") }),
                    new ArtDef("mi_bu_taiyi", "太乙神数·一念演天", 5, "卜术",
                        new[]
                        {
                            CapTempo(3, "全场推衍:tempo上限+3(直升当前realm上限的容量)"),
                            new EffectOp(EffectOpKind.AddResource, "netFortune", 6, "且netFortune+6,但LifespanDebt+4/Karma+4(借寿换满先手),决战开局技"),
                        }),
                }),
                // 推衍（推演类——把信息差转成压制；power 之和直接进战力衡量=信息压制广度，决定 ForesightTerm 与 tempo→削敌转化）。
                new ArtCategoryDef("推衍", "deduction", 1, 1, new[]
                {
                    new ArtDef("mi_ty_qingming", "青冥推演术", 1, "推衍",
                        new[] { Passive("foresight_base", "power=6:提供ForesightTerm基底,首回合tempo削敌系数+1档") }),
                    new ArtDef("mi_ty_lianzhu", "因果连珠算", 2, "推衍",
                        new[] { Passive("tempo_ratio_up", "power=10:tempo削敌由×tempo/16升为×tempo/12(信息差转化更狠)") }),
                    new ArtDef("mi_ty_wanxiang", "万象归一推衍图", 3, "推衍",
                        new[] { Passive("concurrency_up", "power=16:同时推衍2目标(夺运/读命并发+1),多线信息枢纽成型") }),
                    new ArtDef("mi_ty_huisu", "时空回溯·逆演前尘", 4, "推衍",
                        new[] { Passive("reverse_once", "power=24:每场限1次失手时消netFortune-10逆演重来(撤本次结算不撤Karma),LifespanDebt+3") }),
                    new ArtDef("mi_ty_dayan", "推天衍命·大衍之纲", 5, "推衍",
                        new[]
                        {
                            CapTempo(2, "tempo上限+2"),
                            // 净气运为正时超出部分1/4额外计入EP=后期超线性起飞 → A.0 以 netFortune 项权重+1阶近似(AddTermWeightStep)。
                            new EffectOp(EffectOpKind.AddTermWeightStep, "netFortuneStep", 1, "power=34:净气运为正时超出部分1/4额外入EP(近似netFortune项+1阶),后期超线性主源"),
                        }),
                }),
                // 夺运（夺运截命类——正交泛克兑现层,直接从目标扣气运/有效战力转入自身,无视对方修何路;Backlash反噬高发区）。
                new ArtCategoryDef("夺运", "fatesteal", 1, 1, new[]
                {
                    new ArtDef("mi_du_jieyun", "借运术", 1, "夺运",
                        new[] { Passive("steal_fortune", "夺目标Fortune-3转自身netFortune+2(损耗1);目标气运高于己则reflect入自身LifespanDebt,Karma+2") }),
                    new ArtDef("mi_du_jieming", "截命符", 2, "夺运",
                        new[] { Passive("cut_destiny", "命中削目标本回合EP的(tempo/16)倍并计入己;tempo越高夺得越多,Karma+3/LifespanDebt+1") }),
                    new ArtDef("mi_du_yihuo", "移祸江东·因果转嫁", 3, "夺运",
                        new[] { Passive("karma_transfer", "把自身已积Karma的一半(整除)转嫁一名目标(其NetFortune同额降),成本netFortune-6,清债式泛克") }),
                    new ArtDef("mi_du_gaiming", "夺运改命大法", 4, "夺运",
                        new[] { Passive("seize_fate", "强夺目标Fortune-8→自身+5;对气运高于己者reflect=max(0,目标-己)全额入自身Karma+LifespanDebt(强夺命大者自险)") }),
                    new ArtDef("mi_du_jueming", "断人生死·绝命截运", 5, "夺运",
                        new[] { Passive("sever_life", "终结:消netFortune-15/Karma+6/LifespanDebt+6,清零目标本场tempo并夺其NetFortune的1/3;仅己净气运为正且高于目标可发,否则发动即自噬") }),
                }),
                // 因果（布局/护运类——把高方差气运博弈对冲成可控:预埋因果子/抵御反噬/压制敌先手,降本路低容错雪崩风险）。
                new ArtCategoryDef("因果", "causality", 1, 1, new[]
                {
                    new ArtDef("mi_yg_luozi", "因果落子", 1, "因果",
                        new[] { Passive("karma_stone", "预埋一枚因果子(整数计数),之后任一夺运命中时额外netFortune+2、Karma-1(应验回吐天谴);布局越早越稳") }),
                    new ArtDef("mi_yg_yunsuo", "气运锁", 2, "因果",
                        new[] { Passive("fortune_lock", "本场封锁自身Fortune不被夺(免疫敌借运/截命的气运侧),但tempo增长-1档(防守换先手)") }),
                    new ArtDef("mi_yg_tianqian", "天谴转移阵", 3, "因果",
                        new[] { Passive("karma_defer", "把本回合将累积Karma暂存入阵(延迟3回合结算)给透支夺运开窗,阵破则Karma双倍回灌") }),
                    new ArtDef("mi_yg_chengfu", "逆天改命·承负护体", 4, "因果",
                        new[] { Passive("backlash_shield", "受Backlash反弹时以netFortune-6抵消最多reflect的一半(整除),防强夺命大者被一击打穿") }),
                    new ArtDef("mi_yg_dunqu", "大衍五十·遁去其一", 5, "因果",
                        new[] { Passive("last_ditch_void", "保命底牌:净气运被打穿(≤0)濒死时自动触发一次,消全部剩余Fortune重置Karma与本场tempo归零续命,每场限1次") }),
                }),
                // fateheart 命心类目（M1，补遗第七部「知命不惧心」）。A.0 仅装载不结算 → tier=0（sumArtPower 贡献 0）、
                // effects 留空（不触 daoHeart/innerDemon/comprehension 资源算子,那是 A.2 道心层的事）。具名 + power=0。
                new ArtCategoryDef("命心", "daoheart", 1, 1, new[]
                {
                    new ArtDef("dh_mi_zhiming", "知命安神诀", 0, "命心", System.Array.Empty<EffectOp>()),
                    new ArtDef("dh_mi_qingye", "清业定心录", 0, "命心", System.Array.Empty<EffectOp>()),
                    new ArtDef("dh_mi_buju", "知命不惧心经", 0, "命心", System.Array.Empty<EffectOp>()),
                    new ArtDef("dh_mi_woming", "我命由我承负心", 0, "命心", System.Array.Empty<EffectOp>()),
                }),
            };

            // —— 战技（深度设计「战技」节，OnUse 算子 + Cost 资源表；先手=tempo,气运=netFortune,成本=karma/lifespanDebt）。
            //    夺运/削敌等具体结算 Phase 3 接，A.0 以 AddPenInteger 近似整数破防量占位 + Cost 表达资源门槛。
            //    本路成本货币是 karma/lifespanDebt 累积（"发动即透支"），故 Cost 多含这两项 + netFortune 支出。——
            var skills = new[]
            {
                // 一念断因果：开战即结算,按tempo档削对方本回合EP并截其下一动作,夺其Fortune-5入己。决战起手最强先手技。
                new CombatSkillDef("sk_mi_yinian", "一念断因果", 5,
                    new[] { new EffectOp(EffectOpKind.AddPenInteger, null, 40, "按tempo档削对方EP×tempo/16(整除)并截其下一动作,同时夺Fortune-5入己") },
                    new Dictionary<string, int> { { "karma", 5 }, { "lifespanDebt", 3 }, { "netFortune", 4 } }),
                // 夺运截命·一击：对单体夺Fortune-8→己+5;目标气运>己则reflect全额入己LifespanDebt+Karma(强夺命大者自险),命中则其tempo-2。
                new CombatSkillDef("sk_mi_duoyun", "夺运截命·一击", 4,
                    new[] { new EffectOp(EffectOpKind.AddPenInteger, null, 24, "夺单体Fortune-8→己+5;气运>己则reflect=目标-己全额入己,命中其tempo-2") },
                    new Dictionary<string, int> { { "karma", 4 }, { "lifespanDebt", 2 } }),
                // 逆演重开：本场限一次,撤销刚结算的一次交锋(伤害/夺运/胜负回滚),但Karma不回滚。以信息优势重来一手纠错。
                new CombatSkillDef("sk_mi_niyan", "逆演重开", 4,
                    new[] { new EffectOp(EffectOpKind.AddPenInteger, null, 0, "撤销刚结算的一次交锋结果(伤害/夺运/胜负回滚),Karma不回滚(信息优势纠错)") },
                    new Dictionary<string, int> { { "netFortune", 10 }, { "lifespanDebt", 3 } }),
                // 移祸天下：群体转嫁,把自身Karma一半(整除)平摊场上所有敌方(各NetFortune同步降),顺带各夺Fortune-2。多打一时摊薄天谴。
                new CombatSkillDef("sk_mi_yihuo", "移祸天下", 3,
                    new[] { new EffectOp(EffectOpKind.AddPenInteger, null, 12, "把自身Karma一半(整除)平摊全场敌方(各NetFortune同步降),各夺Fortune-2") },
                    new Dictionary<string, int> { { "netFortune", 6 } }),
                // 借寿演天：瞬间满算,tempo直升当前realm上限并netFortune+4。以折寿换此刻全知。
                new CombatSkillDef("sk_mi_jieshou", "借寿演天", 3,
                    new[]
                    {
                        new EffectOp(EffectOpKind.AddResource, "tempo", 4, "tempo直升当前realm上限(满先手)"),
                        new EffectOp(EffectOpKind.AddResource, "netFortune", 4, "并netFortune+4(折寿换此刻全知)"),
                    },
                    new Dictionary<string, int> { { "karma", 3 } }),
                // 趋吉闪命：预知闪避,消Fortune-4使敌本回合先手tempo削减/截命对己全部失效(算到对方走位)。被集火时的活路。
                new CombatSkillDef("sk_mi_quji", "趋吉闪命", 2,
                    new[] { new EffectOp(EffectOpKind.AddFlatDR, null, 8, "敌本回合先手tempo削减/截命对己全部失效(预知闪避,算到对方走位)") },
                    new Dictionary<string, int> { { "netFortune", 4 }, { "karma", 1 } }),
                // 断生死·绝命：终结技,仅己净气运>0且>目标时可发——清零目标本场tempo并夺其NetFortune的1/3,几乎必杀已被压制者;条件不满足则发动反噬全额入己。
                new CombatSkillDef("sk_mi_jueming", "断生死·绝命", 5,
                    new[] { new EffectOp(EffectOpKind.AddPenInteger, null, 50, "清零目标本场tempo并夺其NetFortune的1/3(几乎必杀已被压制者);仅己净气运>0且>目标可发,否则反噬全额入己") },
                    new Dictionary<string, int> { { "netFortune", 15 }, { "karma", 6 }, { "lifespanDebt", 6 } }),
            };

            return new CultivationPathDef(
                "ming_fate_causality", "命修·因果时空",
                "fate",
                // 属性/形态 tag（spirit_attack 神识/气运侧攻击 / ranged 远程算计非贴脸 / righteous 正道泛克非邪术），
                // 非对手 PathId（R2）。气运正交泛克=信息/神识侧，故 spirit_attack；卜算夺运不近身=ranged。
                new[] { "spirit_attack", "ranged", "righteous" },
                resources,
                power,
                curve,
                arts,
                skills,
                new EntryGateDef("tag:ming_root"),
                new SelectionRuleDef(2, 2), // 战技抽 2（深度设计选取规则:再从战技池选 2 个特殊战技）
                null);
        }
    }
}
