using System.Collections.Generic;

namespace Jianghu.Cultivation.Paths
{
    /// <summary>
    /// 鬼修·养魂噬煞道 <c>gui_xiu_yang_hun</c>（spirit 鬼系代表路）。数据照《每路深度设计》鬼修节 +
    /// 《内容补遗》第六部「9. 鬼修 gui_xiu_yang_hun — 道心：守魂不噬心」+ 命名池鬼修条目。
    /// 高爆发低容错·昼弱夜强·养蛊噬主：魂力源于内力/悟性（绕物攻防，弃肉身）、煞值带电量爆发（昼夜倍率）、
    /// 养鬼噬主双账本（鬼兵之力进战力但积噬主度反噬）。被佛门/雷法/纯阳/昼日所克。
    ///
    /// 红线落实：terms 无 ×0、无 daoHeart（R3/R6）——深度设计「武力×0/realm×0」属 ×0 项，按约定**整体不发该 term**
    /// （realm 爆发改由 RealmCurve 倍率承载，武力本路弃权故不入项）；根骨×−1 负权重=本路签名机制（弃炼肉身），
    /// PowerEngine 支持负权但 baseSum 经 Clamp(0,cap) 兜底不会负 final。SituationalTags=属性/形态 tag 非对手 PathId（R2）；
    /// RealmCurve 四列等长（M4）；含 1 个 Role=daoheart 类目 ghostheart（M1，A.0 仅装载不结算 → tier=0）。
    /// canon pathId（R4）。纯整数，禁浮点。
    /// </summary>
    public static class GhostYangHunPath
    {
        public static readonly CultivationPathDef Def = Build();

        // —— 被动算子（装配期落 state，核心 EffectOpKind）。功法的「魂力+N」等改派生项 A.0 为 flavor 不落算子
        //    （生成期 Σ=80 不被功法污染），仅以 Note 留痕；能落 state 的「煞值上限+N」走 AddResourceCap、
        //    「永久魂力/煞值+N」走 AddResource、被动开关走 GrantPassive。——
        private static EffectOp CapSha(int amt, string note)
            => new EffectOp(EffectOpKind.AddResourceCap, "shaCharge", amt, note);

        private static EffectOp Passive(string flag, string note)
            => new EffectOp(EffectOpKind.GrantPassive, flag, 1, note);

        private static CultivationPathDef Build()
        {
            // —— per-path 专属资源（不进四维 C6）——
            // shaCharge 煞值：本路专属夜聚资源/战技弹药（入夜聚煞、阴地+、噬魂+），base cap=40（深度设计'结丹后上限+20'起底）。
            // devourMeter 噬主度：养鬼噬主天敌账本（养鬼越多越涨，跨阈反噬），0..100。
            // ghostSoldierPower 鬼兵之力：所养鬼兵 power 之和（养魂功法叠加），0..200。
            var resources = new[]
            {
                new ResourceDef("shaCharge", 0, 40, 0),
                new ResourceDef("devourMeter", 0, 100, 0),
                new ResourceDef("ghostSoldierPower", 0, 200, 0),
            };

            // —— 战力公式（深度设计 terms：内力×2 + 悟性×2 + 根骨×−1 + 煞值×3 + 鬼兵之力×1 + 所选功法power×1；
            //    武力×0/realm×0 按约定不发 term（R6 禁 ×0；realm 爆发改由 RealmCurve 承载）。根骨负权=弃肉身签名机制。
            //    煞值×3 是'带电量'项(昼×1/夜×3 由 Phase 3 战斗结算昼夜倍率施加,A.0 不落 Modifier,Note 留痕)。
            //    无 daoHeart、无 ×0（R3/R6）。——
            var power = new PowerFormulaDef(
                new[]
                {
                    new PowerTerm("stat:Internal", 2, null),            // 内力：魂力主源(阴煞内息养元神),占魂力一半
                    new PowerTerm("stat:Insight", 2, null),             // 悟性：魂力另一主源(勾魂摄魄/御鬼全凭神识)
                    new PowerTerm("stat:Constitution", -1, null),       // 根骨负权=签名机制:鬼修弃炼肉身,根骨高反成阳气累赘
                    new PowerTerm("res:shaCharge", 3, null),            // 煞值：专属夜聚资源,'带电量'项(昼夜倍率 Phase 3 接)
                    new PowerTerm("res:ghostSoldierPower", 1, null),    // 鬼兵之力：所养鬼兵 power 之和(越多越强也越易反噬)
                    new PowerTerm("sumArtPower", 1, null),              // 所选鬼术/养魂/煞气各功法 tier 之和
                },
                System.Array.Empty<PowerMod>(),
                null);

            // —— 战力曲线（深度设计 realmMul=[1,2,4,7,12,20,33,55]，realm 0..7，比剑修更陡高爆发；低容错来自昼夜坍缩+噬主反噬）。
            //    四列等长（M4）：倍率 / UnifiedTierOf（UT0-12 映射）/ 境界名（炼气游魂→筑基凝魄→金丹鬼丹→元婴鬼婴→
            //    化神夺舍→鬼将→鬼王→渡劫冲幽冥）/ 升入阈值（煞值蓄满+魂魄淬纯累进，realm0=0 起；噬魂捷径故阈值略陡）。——
            var curve = new RealmCurveDef(
                new[] { 38, 54, 79, 121, 190, 468, 754, 1224 }, // INV-CROSS校准: 对齐剑修target(BaseSum=60)
                new[] { 0, 2, 4, 6, 8, 10, 11, 12 },
                new[] { "游魂", "凝魄", "鬼丹", "鬼婴", "夺舍", "鬼将", "鬼王", "冲幽冥" },
                // 煞值蓄满+魂魄淬纯累进（≥100×当前realm 升阶，噬魂捷径故略陡，呼应高爆发曲线）。
                new[] { 0, 100, 300, 600, 1000, 1500, 2100, 2800 },
                // —— A.1 境界稿 §2：起步 SubLevelCount 全 1；CanAscend=true；MaxMajor=大境界数-1。——
                new[] { 1, 1, 1, 1, 1, 1, 1, 1 }, true, 7);

            // —— 功法类目（鬼术/养魂/煞气 各 5 具名 + ghostheart 道心 4 具名）。
            //    具名/效果照深度设计「功法类目」节；命名与命名池鬼修条目同源。——
            var arts = new[]
            {
                // 鬼术(主修·勾摄噬魂的神识攻防功法,本路核心法门)。魂力+N 改派生项 A.0 为 flavor(Note);噬魂涨煞值落 AddResource。
                new ArtCategoryDef("鬼术", "attack", 1, 1, new[]
                {
                    new ArtDef("gu_gs_gouhun", "勾魂鬼音诀", 1, "鬼术",
                        new[] { Passive("soul_disrupt", "魂力+8;命中后目标下一动作战力-10(神识紊乱debuff)") }),
                    new ArtDef("gu_gs_youming", "幽冥血遁", 2, "鬼术",
                        new[]
                        {
                            new EffectOp(EffectOpKind.AddResource, "shaCharge", -5, "消耗煞值5瞬遁,脱战/接战择一,遁后下一战技煞值-3"),
                            Passive("blood_blink", "魂力+14;低境保命兼起手术"),
                        }),
                    new ArtDef("gu_gs_yinluocha", "阴罗刹身决", 3, "鬼术",
                        new[] { Passive("astral_halfbody", "魂力+22;入夜化半实体阴罗刹,physical伤害对其减半(绕物防);白昼失效(昼弱)") }),
                    new ArtDef("gu_gs_qigui", "七鬼噬魂术", 4, "鬼术",
                        new[]
                        {
                            new EffectOp(EffectOpKind.AddResource, "shaCharge", 8, "噬魂吞魄一次:击杀/重伤后掠夺其魂,煞值+8、永久魂力+2"),
                            new EffectOp(EffectOpKind.AddResource, "devourMeter", 5, "代价:噬主度+5、阴德污点+1(最损也最快的境界捷径)"),
                            Passive("soul_devour", "魂力+30;本路最损也最快的境界捷径"),
                        }),
                    new ArtDef("gu_gs_xuanhun", "玄魂炼妖大法", 5, "鬼术",
                        new[] { Passive("yinshen_possess", "魂力+40;解锁出阴神/夺舍前置,阴神附身他体不受夺舍境界限制(战力按宿主realm重算)") }),
                }),
                // 养魂(养鬼·驭鬼兵的召唤统御功法,带噬主天敌账本)。鬼兵之力叠加落 AddResource(ghostSoldierPower)+涨噬主度 devourMeter。
                new ArtCategoryDef("养魂", "movement", 1, 1, new[]
                {
                    new ArtDef("gu_yh_zhenhun", "镇魂锁鬼诀", 1, "养魂",
                        new[]
                        {
                            new EffectOp(EffectOpKind.AddResource, "devourMeter", -3, "主动压制:消耗煞值4,全体在册鬼兵噬主度-3、本回合不触反噬(养鬼流必备刹车)"),
                            Passive("brake_zhenhun", "魂力+6;悟性越高压得越稳('刹车'brake family)"),
                        }),
                    new ArtDef("gu_yh_lianshi", "炼尸阴傀经", 2, "养魂",
                        new[]
                        {
                            new EffectOp(EffectOpKind.AddResource, "ghostSoldierPower", 10, "炼制尸傀1只(power=10+境界档),偏耐久"),
                            Passive("sacrifice_brake", "尸傀代主受一次反噬可献祭抵消噬主度-4(弃子压主)"),
                        }),
                    new ArtDef("gu_yh_yinbing", "阴兵借道符", 3, "养魂",
                        new[]
                        {
                            new EffectOp(EffectOpKind.AddResource, "ghostSoldierPower", 18, "古战场召阴兵一队(等效power18,持续3动作),不积常驻噬主度"),
                            new EffectOp(EffectOpKind.AddResource, "shaCharge", -6, "每次召唤煞值-6(适合速战不愿背噬主债者)"),
                        }),
                    new ArtDef("gu_yh_wangui", "万鬼幡录", 4, "养魂",
                        new[]
                        {
                            new EffectOp(EffectOpKind.AddResource, "ghostSoldierPower", 30, "统御鬼兵上限+3(基础1→4只),每只power×1进战力"),
                            new EffectOp(EffectOpKind.AddResource, "devourMeter", 8, "每只每境噬主度自然+2(养得越多越强也越易反噬)"),
                        }),
                    new ArtDef("gu_yh_guiying", "鬼婴夺胎法", 5, "养魂",
                        new[]
                        {
                            new EffectOp(EffectOpKind.AddResource, "ghostSoldierPower", 25, "孕养鬼婴本命鬼兵(power=25+境界档,随主成长),最强"),
                            new EffectOp(EffectOpKind.AddResource, "devourMeter", 12, "噬主度增速翻倍(+4/境),失控则直接夺舍主身(高危高回报顶点)"),
                        }),
                }),
                // 煞气(炼煞·阴煞结丹与煞气护体的资源/防御功法,昼夜倍率所系)。煞值上限增益落 AddResourceCap。
                new ArtCategoryDef("煞气", "internal", 1, 1, new[]
                {
                    new ArtDef("gu_sq_jusha", "聚煞养元功", 1, "煞气",
                        new[] { Passive("sha_gain_up", "夜修聚煞效率+50%(入夜煞值额外+3/旬);阴地驻留+煞值再+2(本路最基础充能内功)") }),
                    new ArtDef("gu_sq_xuanyin", "玄阴煞气罩", 2, "煞气",
                        new[]
                        {
                            new EffectOp(EffectOpKind.AddResource, "shaCharge", -3, "消耗煞值3张煞气护体,抵下次伤害量=当前煞值×2"),
                            Passive("yin_shield", "魂力+12;被佛门/雷法/纯阳攻击时该护罩减半(天克落整数)"),
                        }),
                    new ArtDef("gu_sq_jiedan", "阴煞结丹诀", 3, "煞气",
                        new[]
                        {
                            CapSha(20, "金丹境结鬼丹,煞值上限+20"),
                            Passive("daynight_x4", "结丹后入夜战力昼夜系数×3升至×4(夜战暴发更猛,白昼依旧×1更显反差)"),
                        }),
                    new ArtDef("gu_sq_jiuyou", "九幽煞河阵", 4, "煞气",
                        new[]
                        {
                            new EffectOp(EffectOpKind.AddResource, "shaCharge", -10, "布阵将战场化阴地起手煞值10"),
                            Passive("sha_river_array", "阵内己方煞值每动作+4、敌方若属纯阳/佛门则攻击-15(以阵补煞)"),
                        }),
                    new ArtDef("gu_sq_duoshe", "夺舍重煞身", 5, "煞气",
                        new[] { Passive("duoshe_reset", "魂力+35;弃旧身夺新体,成功重置噬主度0并洗去阴德污点;夺舍中招雷/纯阳则失魂风险(化神标志大法)") }),
                }),
                // ghostheart 道心类目（M1，补遗第六部「守魂不噬心」）。A.0 仅装载不结算 → tier=0（sumArtPower 贡献 0）、
                // effects 留空（不触 daoHeart/innerDemon/comprehension 资源算子,那是 A.2 道心层的事）。具名 + power=0。
                new ArtCategoryDef("鬼心", "daoheart", 1, 1, new[]
                {
                    new ArtDef("dh_gu_shouhun", "守魂镇心诀", 0, "鬼心", System.Array.Empty<EffectOp>()),
                    new ArtDef("dh_gu_jisha", "祭弱护本录", 0, "鬼心", System.Array.Empty<EffectOp>()),
                    new ArtDef("dh_gu_busha", "噬而不堕心经", 0, "鬼心", System.Array.Empty<EffectOp>()),
                    new ArtDef("dh_gu_cunshen", "存神不化道心", 0, "鬼心", System.Array.Empty<EffectOp>()),
                }),
            };

            // —— 战技（深度设计「战技」节，OnUse 算子 + Cost 资源表；弹药=shaCharge 煞值）。
            //    伤害/穿透等具体结算 Phase 3 接，A.0 以 AddPenInteger 占位破防量（量级对齐该路公式）+ Cost 表达资源门槛。
            //    强制 ≥1 刹车/保命类（镇魂/献祭/夺舍）对冲养鬼噬主反噬——本路签名选取规则（深度设计选取规则）。——
            var skills = new[]
            {
                // 夺舍·借尸还魂：魂力濒死强夺邻近肉身续命,成功保命且噬主度清零;失败(遇雷/纯阳/佛光)魂飞魄散。煞值≥15(+全部魂力压上)。
                // B5扫尾 defer(红线A.8): 夺舍战略级(强夺肉身续命/realm重算)→batch4/A.2 战略层,非战术伤害(amount 0),保 AddPenInteger(0) 占位 + devourMeter 清零。
                new CombatSkillDef("sk_gu_duoshe", "夺舍·借尸还魂", 5,
                    new[]
                    {
                        Modules.Special("duoshe", 1, 0, "夺舍续命:濒死强夺肉身(遇雷/纯阳/佛光→失败)"),
                        new EffectOp(EffectOpKind.AddResource, "devourMeter", -100, "夺舍成功噬主度清零"),
                    },
                    new Dictionary<string, int> { { "shaCharge", 15 } }),
                // 幽冥煞爆：倾泻煞值自爆式打击,伤害=当前全部煞值×2(入夜再×昼夜系数),放完归零。清空全部煞值。
                // B5批2: → PenFromResource(shaCharge,2) 煞值自爆(带电量越满越痛,见底哑火;昼夜系数 Phase3 接)。
                new CombatSkillDef("sk_gu_shabao", "幽冥煞爆", 3,
                    new[] { Modules.PenFromResource("shaCharge", 2, note: "倾泻煞值自爆,伤害=当前全部煞值×2(入夜再×昼夜系数),放完归零(空电量极高方差爆发)") },
                    new Dictionary<string, int> { { "shaCharge", 20 } }),
                // 万鬼夜行：倾巢令全部在册鬼兵齐出协攻,伤害=Σ鬼兵power×2;战后全体鬼兵噬主度+2。煞值≥8(+噬主度风险+2)。
                // B5扫尾: 占位 AddPenInteger(36) → Modules.PenFromResource(ghostSoldierPower,×2)（与毒蛊'万蛊噬身'同构:Σ鬼兵 power×2
                //   的真 per-鬼 Σ 是逐兵派生→FULLSTRUCT,本批用聚合资源 ghostSoldierPower 近似差分(越多越痛、空册哑火);Amount2=1 工厂保证 §15.6）。
                //   倾巢背债 devourMeter+2 保留。
                new CombatSkillDef("sk_gu_yexing", "万鬼夜行", 4,
                    new[]
                    {
                        Modules.PenFromResource("ghostSoldierPower", 2, note:"倾巢全部在册鬼兵齐出协攻,伤害=Σ鬼兵power×2(真Σ鬼兵 derived→FULLSTRUCT,本批用 ghostSoldierPower 聚合资源近似)"),
                        new EffectOp(EffectOpKind.AddResource, "devourMeter", 2, "战后全体鬼兵噬主度+2(倾巢必背债)"),
                    },
                    new Dictionary<string, int> { { "shaCharge", 8 } }),
                // 勾魂索命：锁定单体施勾魂鬼音强化版,目标战力-20并定身一动作;对纯阳/佛门体质命中率减半(天克)。煞值≥5,消耗5。
                // B5批2: → Control(soulLock,1) 定身一动作(控场积木,回合间结算批4接;战力-20 debuff 批4 接,本轮不改dmg)。
                new CombatSkillDef("sk_gu_suoming", "勾魂索命", 3,
                    new[] { Modules.Control("soulLock", 1, "锁定单体定身一动作;对纯阳/佛门命中率减半(天克)") },
                    new Dictionary<string, int> { { "shaCharge", 5 } }),
                // 阴风血雾：范围阴雾,区域内敌每动作-煞值/失血,普通敌悟性judge失败则战力-8;白昼范围与时长减半(昼弱)。煞值≥4,消耗4。
                new CombatSkillDef("sk_gu_xuewu", "阴风血雾", 2,
                    new[] { Modules.FlatPen(8, "范围阴雾,敌悟性judge失败则战力-8;白昼范围与时长减半(昼弱;judge/debuff Phase3)") },
                    new Dictionary<string, int> { { "shaCharge", 4 } }),
                // 噬魂一缕：近敌抽魂造基于魂力的伤害并回煞值+3;击杀时噬主度不增(轻量噬魂无反噬负担)。煞值≥2,消耗2。
                new CombatSkillDef("sk_gu_yilv", "噬魂一缕", 1,
                    new[]
                    {
                        Modules.FlatPen(12, "近敌抽魂造基于魂力的伤害(轻量噬魂,击杀时噬主度不增;魂力scaling Phase3 基线)"),
                        new EffectOp(EffectOpKind.AddResource, "shaCharge", 3, "回煞值+3"),
                    },
                    new Dictionary<string, int> { { "shaCharge", 2 } }),
                // 献祭弱鬼·镇魂：防御性刹车,献祭1只最弱在册鬼兵,立即噬主度-6并回煞值+4,可在反噬触发前打断。门槛=1只鬼兵(占 ghostSoldierPower)。
                new CombatSkillDef("sk_gu_jisha", "献祭弱鬼·镇魂", 2,
                    new[]
                    {
                        new EffectOp(EffectOpKind.AddResource, "devourMeter", -6, "献祭1只最弱在册鬼兵立即噬主度-6(养鬼流保命核心刹车操作)"),
                        new EffectOp(EffectOpKind.AddResource, "shaCharge", 4, "回煞值+4,可在反噬触发前打断"),
                    },
                    new Dictionary<string, int> { { "ghostSoldierPower", 10 } }),
                // 鬼影遁·闪：鬼影遁形闪避（OnDefend）。需养魂→门控。shaCharge≥4,消耗4。
                // B5扩21: Evade — 鬼修鬼影遁闪避,Amount=35→35%减免(高闪避脆皮路线)。
                new CombatSkillDef("sk_gu_guiying_dun", "鬼影遁·闪", 2,
                    new[] { Modules.Evade(35, "鬼影遁形闪避:35%来袭减免(需养魂→门控)") },
                    new Dictionary<string, int> { { "shaCharge", 4 } }),
                // 勾魂索命[control]：控场—目标下2回合无法行动。shaCharge≥8,消耗8。
                // B5扩21: Control — 鬼修控场代表招。
                new CombatSkillDef("sk_gu_gouhun", "勾魂索命", 3,
                    new[] { Modules.Control("gouhun", 2, "控场:目标下2回合无法行动(勾魂锁命)") },
                    new Dictionary<string, int> { { "shaCharge", 8 } }),
            };

            return new CultivationPathDef(
                "gui_xiu_yang_hun", "鬼修·养魂噬煞道",
                "spirit",
                // 属性/形态 tag（spirit_attack 魂力绕物攻 / ghost 鬼系 / evil 阴邪偏门），非对手 PathId（R2）。
                new[] { "spirit_attack", "ghost", "evil" },
                resources,
                power,
                curve,
                arts,
                skills,
                new EntryGateDef("tag:ghost_root"),
                new SelectionRuleDef(3, 3), // 战技抽 3,其中 ≥1 刹车/保命类对冲噬主反噬（深度设计强制风险对冲选取规则）
                null);
        }
    }
}
