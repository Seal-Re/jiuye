using System.Collections.Generic;

namespace Jianghu.Cultivation.Paths
{
    /// <summary>
    /// 毒蛊修·毒道养蛊 <c>du_gu_xiu</c>（苗疆五毒/百蛊渊一脉）。数据照《余9路深度设计》毒蛊节 +
    /// 《内容补遗》道心层补遗「新增道心类目 guheart 蛊心」。
    /// physical 渗透+寄生役使：悟性为脑(辨毒配毒推蛊)、蛊群为爪(蛊母+子蛊可繁殖加权和)、百毒值为弹药、
    /// 境界开毒(凸加速但养蛊噬主封顶)。阴影消耗暗杀，被识破/解毒/焚蛊即崩——高方差低容错偏门之最。
    ///
    /// 红线落实：terms 无 ×0（武力/根骨/realm 三 ×0 项按约定显式删去，realm 爆发改由 RealmCurve 承载）、
    /// 无 daoHeart/innerDemon（R3/R6）；噬主度 guRevolt 是养蛊负债账本，**绝不入 power term**（与 daoHeart 同性，
    /// 仅由功法/战技 AddResource 落账，反噬走 Phase 3 Modifier）。SituationalTags=属性/形态 tag 非对手 PathId（R2）。
    /// RealmCurve 四列等长（M4）；含 1 个 Role=daoheart 类目 guheart（M1，A.0 仅装载不结算 → tier=0
    /// 使 sumArtPower 贡献 0、effects 留空不触 daoHeart 资源算子）。canon pathId（R4）。纯整数，禁浮点。
    /// </summary>
    public static class DuGuXiuPath
    {
        public static readonly CultivationPathDef Def = Build();

        // —— 被动算子（装配期落 state，核心 EffectOpKind）。功法的「悟性+N/配毒推蛊」等改四维项 A.0
        //    为 flavor 不落算子（生成期 Σ=80 不被功法污染，深度设计「功法只加 power 不改 Σ」），仅以 Note 留痕；
        //    能落 state 的「百毒值上限+N」走 AddResourceCap、被动开关走 GrantPassive/SetFlag。
        //    噬主度 guRevolt 增减由养蛊/刹车功法以 AddResource 落账（经 chokepoint 钳）。——
        private static EffectOp CapVenom(int amt, string note)
            => new EffectOp(EffectOpKind.AddResourceCap, "venomCharge", amt, note);

        private static EffectOp Passive(string flag, string note)
            => new EffectOp(EffectOpKind.GrantPassive, flag, 1, note);

        private static CultivationPathDef Build()
        {
            // —— per-path 专属资源（不进四维 C6）——
            // venomCharge 百毒值：淬毒/施毒/驱蛊弹药，瘴疠区+采毒缓充，战后不清零；base cap=20（深度设计
            //   capFormula="20+5*realm" 的 realm0 基线，realm 增益由瘟疫类心法 AddResourceCap 表达，A.0 单值起底）。
            // guSwarmPower 蛊群之力：本命蛊母 power + Σ子蛊 guPower×纽带/100（养蛊功法叠加,外部役使加权和,可繁殖/可夺取/
            //   可被清场）；战力主源之一。base cap=200（与鬼修 ghostSoldierPower 同构起底，深度设计 derived:guSwarmWeighted
            //   A.0 以 res 落账占位:养蛊功法 AddResource 叠加,被解毒/焚蛊按比例失效走 Phase 3）。
            // guRevolt 噬主度：养蛊负债账本(随子蛊总 power 涨,跨阈触反噬,镇蛊/献蛊降)，0..100。**不入 power term**。
            // hostSlots 宿主名额：寄生型子蛊占用上限(名额满则不可再寄生)，base cap=1（capFormula="1+realm/2+guMotherTier" realm0 基线）。
            // poisonPurity 百毒精纯度：突破货币之一(每境需达阈)，淬毒/试毒提升，0..100。
            var resources = new[]
            {
                new ResourceDef("venomCharge", 0, 20, 0),
                new ResourceDef("guSwarmPower", 0, 200, 0),
                new ResourceDef("guRevolt", 0, 100, 0),
                new ResourceDef("hostSlots", 0, 1, 0),
                new ResourceDef("poisonPurity", 0, 100, 0),
            };

            // —— 战力公式（深度设计 terms：悟性×4 + 内力×2 + 蛊群之力×3 + 百毒值×2 + 所选功法power×1；
            //    武力×0/根骨×0/realm×0 三项按约定显式删去（R6 禁 ×0；realm 爆发改由 RealmCurve 承载）。
            //    悟性权重全路最高(辨毒配毒/推蛊调度/寄生夺心全凭悟性,对位驭兽悟性×4)；武力近乎弃权(不正面搏杀)。
            //    蛊群之力=本命蛊母 power + Σ子蛊 guPower×纽带/100（derived，复用驭兽 rosterWeighted 模式，A.0 以 res 占位）。
            //    噬主度 guRevolt 不在 terms(养蛊负债与 daoHeart 同性,反噬走 Phase 3 Modifier)。无 daoHeart、无 ×0（R3/R6）。——
            var power = new PowerFormulaDef(
                new[]
                {
                    new PowerTerm("stat:Insight", 4, null),       // 悟性为脑：辨毒配毒/推蛊调度/寄生夺心,本路第一主源全路最高
                    new PowerTerm("stat:Internal", 2, null),      // 内力：灵力/蛊力供能,催施毒/饲灵回纽带/维持寄生链与瘟疫场
                    new PowerTerm("res:guSwarmPower", 3, null),   // 蛊群之力：本命蛊母+Σ子蛊加权,外部役使项最高权(可繁殖/可夺取/可被清场)
                    new PowerTerm("res:venomCharge", 2, null),    // 百毒值：渗透淬毒当量,放完战技即跌(带量非恒定,无昼夜倍率)
                    new PowerTerm("sumArtPower", 1, null),        // 所选蛊术/毒功/瘟疫各功法 tier 之和
                },
                System.Array.Empty<PowerMod>(),
                null);

            // —— 战力曲线（深度设计 realmMul=[1,2,4,8,14,24,38,56]，realm 0..7，阴影消耗·厚积反噬:低开慢热-中段
            //    以蛊养蛊超线性-后段噬主见顶增速收敛。末倍率/10=5.6 介于鬼修5.5与驭兽7.0之间）。
            //    四列等长（M4）：倍率 / UnifiedTierOf（UT0-12 映射:三流→…→渡劫；前期慢热故 UT 起步与鬼修同低）/
            //    境界名（炼气凝百毒→筑基炼蛊母→金丹百毒结丹→元婴育本命蛊王→化神万蛊归宗/夺心→合体→大乘→渡劫冲万毒不侵劫）/
            //    升入阈值（百毒精纯+蛊母品阶+蛊群总power累进,realm0=0 起；厚积型故阈值与鬼修同累进）。——
            var curve = new RealmCurveDef(
                new[] { 19, 27, 40, 61, 95, 234, 377, 612 }, // INV-CROSS校准: 对齐剑修target
                new[] { 0, 2, 4, 6, 8, 10, 11, 12 },
                new[] { "凝百毒", "炼蛊母", "百毒结丹", "本命蛊王", "万蛊归宗", "蛊仙", "万蛊之主", "冲万毒劫" },
                // 百毒精纯达阈+蛊母淬成品阶+蛊群总 power 达门槛累进（≥100×当前realm 升阶，没蛊喂不饱境界）。
                new[] { 0, 100, 300, 600, 1000, 1500, 2100, 2800 },
                // —— A.1 境界稿 §2：起步 SubLevelCount 全 1；CanAscend=true；MaxMajor=大境界数-1。——
                new[] { 1, 1, 1, 1, 1, 1, 1, 1 }, true, 7);

            // —— 功法类目（蛊术/毒功/瘟疫 各 5 具名 + guheart 蛊心道心 4 具名）。
            //    具名/效果照深度设计「功法类目」节 + 道心层补遗 guheart 表。——
            var arts = new[]
            {
                // 蛊术（养蛊·驭蛊兵的炼养统御功法,本路核心法门,带噬主天敌账本）。
                // 养蛊抬名额走 AddResourceCap(hostSlots)；以蛊养蛊涨噬主度落 AddResource(guRevolt)；悟性+N 改四维为 flavor(Note)。
                new ArtCategoryDef("蛊术", "attack", 1, 1, new[]
                {
                    new ArtDef("du_gs_miaojiang", "苗疆养蛊心经", 1, "蛊术",
                        new[]
                        {
                            new EffectOp(EffectOpKind.AddResourceCap, "hostSlots", 1, "宿主名额+1;同养子蛊上限+2(基础1只→3只)"),
                            new EffectOp(EffectOpKind.AddResource, "guSwarmPower", 12, "基础蛊群:养成在册子蛊 guPower×纽带/100 进战力(蛊群之力主源起底)"),
                            Passive("gu_breed_base", "每只在册子蛊每境噬主度自然+2(养得越多越易反噬)"),
                        }),
                    new ArtDef("du_gs_baigu", "百蛊噬母大法", 4, "蛊术",
                        new[]
                        {
                            new EffectOp(EffectOpKind.AddResource, "guSwarmPower", 30, "以蛊养蛊:两只低阶子蛊融为高阶(guTier+1、guPower×120/100),繁殖式壮大蛊群"),
                            new EffectOp(EffectOpKind.AddResource, "guRevolt", 5, "代价噬主度+5、需活祭1(阴德污点+1)"),
                            Passive("gu_cannibal", "本路指数成长核心,南疆百蛊渊一脉标志"),
                        }),
                    new ArtDef("du_gs_duoxin", "母蛊夺心术", 5, "蛊术",
                        new[] { Passive("parasite_seize", "解锁寄生夺心:对活体目标植寄生蛊占1宿主名额,渗透成功后其战力按比例转己(反向夺兽/夺人);化神境方可施,名额满则不可用") }),
                    new ArtDef("du_gs_huzhu", "炼蛊护主诀", 2, "蛊术",
                        new[]
                        {
                            new EffectOp(EffectOpKind.AddResource, "guSwarmPower", 10, "炼护主子蛊(guPower=10+境界档,偏耐久):可代主受一次蛊噬反噬"),
                            new EffectOp(EffectOpKind.AddResource, "guRevolt", -4, "献祭抵消噬主度-4(弃子压主)"),
                            Passive("gu_guard", "护主子蛊偏耐久,弃子压主"),
                        }),
                    new ArtDef("du_gs_zhengu", "镇蛊锁母诀", 1, "蛊术",
                        new[]
                        {
                            new EffectOp(EffectOpKind.AddResource, "guRevolt", -3, "主动压制:消耗百毒值4,全体在册子蛊噬主度-3、本回合不触发蛊噬主反噬"),
                            Passive("gu_brake", "养蛊流必备刹车,悟性越高压得越稳"),
                        }),
                }),
                // 毒功（淬毒·施毒的渗透暗杀与消耗功法,百毒值所系,以弱胜强阴人于无形）。
                // 蓄毒涨百毒值落 AddResource(venomCharge)；渗透/克制改判定为 flavor(Note)。
                new ArtCategoryDef("毒功", "venom", 1, 1, new[]
                {
                    new ArtDef("du_dg_cuidu", "淬毒经·见血封喉", 1, "毒功",
                        new[]
                        {
                            new EffectOp(EffectOpKind.AddResource, "venomCharge", 8, "百毒值+8(蓄毒)"),
                            Passive("venom_coat", "淬兵器/暗器,命中后目标中毒,下一动作战力-10且持续掉血(整数debuff);淬毒暗杀流入门"),
                        }),
                    new ArtDef("du_dg_huagu", "化骨绵掌·腐毒劲", 3, "毒功",
                        new[]
                        {
                            new EffectOp(EffectOpKind.AddResource, "venomCharge", 22, "百毒值+22(近身渗透毒劲)"),
                            Passive("venom_vs_brute", "对肉身越强中毒越烈的体修/血修目标伤害额外+8(克制项),对纯阳/佛门体质效果减半(天克落整数)"),
                        }),
                    new ArtDef("du_dg_qichong", "七虫七花膏", 4, "毒功",
                        new[]
                        {
                            new EffectOp(EffectOpKind.AddResource, "venomCharge", 40, "百毒值+40(配制复合奇毒)"),
                            Passive("venom_infiltration", "命中无视常规护体(渗透绕一层 physical 软防,仍受±P0/4 clamp);解毒判定连续失败则战力持续-15"),
                        }),
                    new ArtDef("du_dg_wuxing", "无形蚀骨散", 2, "毒功",
                        new[]
                        {
                            new EffectOp(EffectOpKind.AddResource, "venomCharge", 14, "百毒值+14(无色无味缓毒)"),
                            Passive("venom_latent", "潜伏数动作后爆发(慢性消耗特征),潜伏期不暴露施毒者(阴:被识破即废,潜伏不被察则强)"),
                        }),
                    new ArtDef("du_dg_wandu", "万毒朝宗·毒煞身", 5, "毒功",
                        new[]
                        {
                            new EffectOp(EffectOpKind.AddResource, "venomCharge", 35, "百毒值+35(弃旧身重塑毒人之躯)"),
                            Passive("venom_body", "自身化半毒体,物理近身攻击者反受腐蚀,免疫血/毒减益;被符/丹净化或雷火焚身则毒体崩解失魂(化神境标志大法)"),
                        }),
                }),
                // 瘟疫（散毒·控场的范围消耗与防御功法,决定百毒值上限与扩散）。
                // 结丹抬百毒值上限走 AddResourceCap(venomCharge)；布场/护体改判定为 flavor(Note)。
                new ArtCategoryDef("瘟疫", "plague", 1, 1, new[]
                {
                    new ArtDef("du_wy_juducu", "聚毒养炉功", 1, "瘟疫",
                        new[]
                        {
                            new EffectOp(EffectOpKind.AddResource, "venomCharge", 3, "瘴疠区炼毒效率+50%(驻留额外+百毒值);毒药田/蛊虫渊采集再+百毒值"),
                            Passive("plague_furnace", "本路最基础的充能内功"),
                        }),
                    new ArtDef("du_wy_jiedan", "百毒结丹诀", 3, "瘟疫",
                        new[]
                        {
                            CapVenom(20, "金丹境结蛊丹,百毒值上限+20(随境提升)"),
                            Passive("plague_dan", "结丹后施毒/淬毒的百毒值开销-2/技,养蛊效率提升"),
                        }),
                    new ArtDef("du_wy_xuanyin", "玄阴毒煞罩", 2, "瘟疫",
                        new[]
                        {
                            new EffectOp(EffectOpKind.AddResource, "venomCharge", 12, "百毒值+12"),
                            Passive("plague_ward", "消耗百毒值3布毒煞护体,抵下一次伤害量=当前百毒值×2(整数);被佛门/雷法/纯阳攻击时该护罩效果减半(天克落整数)"),
                        }),
                    new ArtDef("du_wy_wangu", "万蛊瘟疫阵", 4, "瘟疫",
                        new[]
                        {
                            new EffectOp(EffectOpKind.AddResource, "venomCharge", 4, "布阵化战场为瘴疠区:己方百毒值+4/动作"),
                            Passive("plague_field", "阵内敌方每动作受毒-战力,对群体阵营瘟疫式消耗削弱(需消耗百毒值10起手,以毒债敌的持续 debuff 场)"),
                        }),
                    new ArtDef("du_wy_fenshen", "蛊母分身·万蛊炉", 5, "瘟疫",
                        new[]
                        {
                            new EffectOp(EffectOpKind.AddResourceCap, "hostSlots", 3, "孕育蛊母分身,宿主名额上限+3;子蛊上限+3"),
                            Passive("gu_avatar", "本命蛊母被反蛊夺母时由分身续接不致全崩(高阶蛊师互克兜底)"),
                        }),
                }),
                // guheart 蛊心道心类目（M1，补遗道心层「蛊心·御毒守魂之念」）。A.0 仅装载不结算 → tier=0（sumArtPower 贡献 0）、
                // effects 留空（不触 daoHeart/innerDemon/comprehension 资源算子,那是 A.2 道心层的事）。具名 + power=0。
                new ArtCategoryDef("蛊心", "daoheart", 1, 1, new[]
                {
                    new ArtDef("dh_du_yangxin", "御毒养心诀", 0, "蛊心", System.Array.Empty<EffectOp>()),
                    new ArtDef("dh_du_qishe", "弃舍护本录", 0, "蛊心", System.Array.Empty<EffectOp>()),
                    new ArtDef("dh_du_busha", "养而不噬心经", 0, "蛊心", System.Array.Empty<EffectOp>()),
                    new ArtDef("dh_du_yidu", "以毒证道·守元道心", 0, "蛊心", System.Array.Empty<EffectOp>()),
                }),
            };

            // —— 战技（深度设计「战技」节，OnUse 算子 + Cost 资源表；弹药=百毒值 venomCharge）。
            //    伤害/穿透等具体结算 Phase 3 接，A.0 以 AddPenInteger 占位破防量 + Cost 表达资源门槛；
            //    刹车类(弃子献蛊·镇母)以 AddResource(guRevolt,负) 降噬主度 + 回百毒值，对冲养蛊反噬。——
            var skills = new[]
            {
                // 万蛊噬身：倾巢,全部在册子蛊齐出寄生攻协,本次伤害=Σ子蛊 guPower×2;战后全体噬主度+2(倾巢必背债)。百毒值8。
                // B5 批2 招牌招迁移：占位 AddPenInteger(40) → Modules.PenFromResource(guSwarmPower,×2)（Σ子蛊齐出,蛊群越强越痛、空册哑火真差分；
                //   真 per-child Σ 是逐蛊派生→FULLSTRUCT,本批用 guSwarmPower 聚合资源近似；Amount2=1 工厂保证 §15.6）。倾巢背债 guRevolt+2 保留。
                new CombatSkillDef("sk_du_wangu", "万蛊噬身", 4,
                    new[]
                    {
                        Modules.PenFromResource("guSwarmPower", 2, note:"令全部在册子蛊齐出寄生攻协,伤害=Σ子蛊 guPower×2(真Σ子蛊 derived→FULLSTRUCT,本批用 guSwarmPower 聚合资源)"),
                        new EffectOp(EffectOpKind.AddResource, "guRevolt", 2, "倾巢必背债:战后全体子蛊噬主度+2"),
                    },
                    new Dictionary<string, int> { { "venomCharge", 8 } }),
                // 百毒朝天·瘟疫爆：倾泻百毒值自爆式范围,伤害=当前全部百毒值×2,放完归零(空量极高方差);对死物/纯阳/佛门锐减。清空 venomCharge。
                // B5扫尾: 占位 AddPenInteger(40) → Modules.PenFromResource(venomCharge,×2)（百毒值越满越痛、见底哑火真差分；
                //   Amount2=1 工厂保证 §15.6；对死物/纯阳/佛门锐减走 Phase3 CounterMatrix）。
                new CombatSkillDef("sk_du_chaotian", "百毒朝天·瘟疫爆", 3,
                    new[] { Modules.PenFromResource("venomCharge", 2, note:"倾泻百毒值范围打击,伤害=当前全部百毒值×2,放完百毒值归零(空量极高方差);对死物傀儡/纯阳/佛门锐减 Phase3") },
                    new Dictionary<string, int> { { "venomCharge", 20 } }),
                // 夺心控蛊：对活体植蛊夺心,成功则其本回合反噬阵营/受己调遣(反向夺兽),占1宿主名额;对纯阳/佛门/死物命中失败。百毒值5。
                // B5 批2：植蛊夺心(mind control,战力按比例转己/调遣敌方)是唯一档签名机制(SpecialModuleRegistry 派发) → batch3 Special,
                //   显式 deferred（红线 A.8 不静默,待批3 wiring 后补 Special 构造）,保 AddPenInteger 占位破防量。
                new CombatSkillDef("sk_du_duoxin", "夺心控蛊", 3,
                    new[] { new EffectOp(EffectOpKind.AddPenInteger, null, 24, "植蛊夺心(mind control→batch3 Special defer):成功则目标本回合反噬其阵营/受己调遣;对纯阳/佛门体质或死物傀儡(无血肉)命中失败") },
                    new Dictionary<string, int> { { "venomCharge", 5 } }),
                // 淬毒一击：淬毒暗器/兵刃一击向单体,基于百毒值的渗透伤害并使中毒掉血;潜伏施放则额外破一层护体。百毒值3。
                new CombatSkillDef("sk_du_cuidu", "淬毒一击", 1,
                    new[] { Modules.FlatPen(12, "淬毒暗器/兵刃一击,基于百毒值的渗透伤害并使目标中毒掉血;潜伏施放(对方未察觉)则额外破一层护体") },
                    new Dictionary<string, int> { { "venomCharge", 3 } }),
                // 瘟疫毒雾：范围毒雾消耗,区域内敌每动作-战力且累积中毒,judge 失败则持续掉血;对纯阳/佛门减半,对死物无效。百毒值4。
                // B5 批2 招牌招迁移：占位 AddPenInteger(8) → Modules.Dot(plague,8/tick,3回合)（范围毒雾持续掉血,
                //   OnUse 挂载、回合间结算批4 接,ApplyOnUse 不改 dmg,本轮断言 Kind+Key 在册）。
                new CombatSkillDef("sk_du_duwu", "瘟疫毒雾", 2,
                    new[] { Modules.Dot("plague", 8, 3, "范围毒雾持续掉血3回合(区域内敌每动作-战力且累积中毒);对纯阳/佛门减半,对死物傀儡无效(批4结算)") },
                    new Dictionary<string, int> { { "venomCharge", 4 } }),
                // 弃子献蛊·镇母：防御性刹车,献祭1只最弱在册子蛊,立即噬主度-6并回百毒值+4,可在蛊噬主反噬触发前打断(保命核心)。无百毒门槛。
                new CombatSkillDef("sk_du_xianzi", "弃子献蛊·镇母", 2,
                    new[]
                    {
                        new EffectOp(EffectOpKind.AddResource, "guRevolt", -6, "献祭1只最弱在册子蛊,立即噬主度-6,可在蛊噬主反噬触发前打断"),
                        new EffectOp(EffectOpKind.AddResource, "venomCharge", 4, "回百毒值+4(养蛊流保命核心操作)"),
                    },
                    new Dictionary<string, int>()),
                // 万毒朝宗·脱壳：蛊母濒死弃旧身夺新宿续命兼重置,成功则保命且噬主度清零(阴德不洗);失败(遇符/丹/雷火/纯阳/佛光)则蛊母俱焚。百毒值15。
                new CombatSkillDef("sk_du_tuoke", "万毒朝宗·脱壳", 5,
                    new[]
                    {
                        new EffectOp(EffectOpKind.AddResource, "guRevolt", -100, "蛊母濒死弃旧身夺新宿续命兼重置:成功则保命且噬主度清零;失败(遇符/丹/雷火/纯阳/佛光在场)则蛊母俱焚永久退场风险"),
                        Modules.FlatPen(0, "终极手段:阴德污点不洗(脱壳续命非伤害置0)"),
                    },
                    new Dictionary<string, int> { { "venomCharge", 15 } }),
            };

            return new CultivationPathDef(
                "du_gu_xiu", "毒蛊修·毒道养蛊",
                "physical",
                // 属性/形态 tag（physical 渗透 / parasite 寄生役使 / evil 阴邪偏门），非对手 PathId（R2）。
                new[] { "physical", "parasite", "evil" },
                resources,
                power,
                curve,
                arts,
                skills,
                new EntryGateDef("tag:gu_root"),
                new SelectionRuleDef(3, 3), // 战技抽 3（深度设计选取规则:三类目各选+战技选3,其中≥1刹车/保命类）
                null);
        }
    }
}
