using System.Collections.Generic;

namespace Jianghu.Cultivation.Paths
{
    /// <summary>
    /// 妖修·化形道 <c>yao_xiu_huaxing</c>（血脉肉身长寿 / 妖兽精怪由兽身求人形 · physical）。
    /// 数据照《余 9 路深度设计》妖修节（妖丹精纯 × 化形度 × 兽躯/人形双态 三套互锁真账本）+ 命名池驭兽/古族条目同源。
    /// 肉身碾压同阶（根骨/武力并列最重权）、寿元绵长慢而稳、外部风险为主（化形劫/正道斩妖/天道压制）+ 返祖暴走内险。
    ///
    /// 红线落实：terms 无 ×0、无 daoHeart（R3/R6）；态系数走 PostMul（人形态 ×9/10，flag formHuman 门，整数 num/den）；
    /// SituationalTags=属性/形态 tag 非对手 PathId（R2）；RealmCurve 四列等长（M4）；含 1 个 Role=daoheart 类目 yaoheart
    /// （M1，A.0 仅装载不结算 → tier=0 使 sumArtPower 贡献 0、effects 留空不触 daoHeart 资源算子）。canon pathId（R4）。纯整数，禁浮点。
    /// </summary>
    public static class YaoXiuHuaxingPath
    {
        public static readonly CultivationPathDef Def = Build();

        // —— 被动算子（装配期落 state，核心 EffectOpKind）。功法的「根骨+N/武力+N」等改四维项 A.0
        //    为 flavor 不落算子（生成期 Σ=80 不被功法污染，深度设计§选取规则「功法只加 sumArtPower 不改 Σ」），
        //    仅以 Note 留痕；能落 state 的「妖丹上限+N」走 AddResourceCap、被动开关走 GrantPassive/SetFlag、
        //    「化形度上限解锁至 N」因资源 cap 落点也走 AddResourceCap（huaXingDu）。——
        private static EffectOp CapYaoDan(int amt, string note)
            => new EffectOp(EffectOpKind.AddResourceCap, "yaoDan", amt, note);

        private static EffectOp CapHuaXing(int amt, string note)
            => new EffectOp(EffectOpKind.AddResourceCap, "huaXingDu", amt, note);

        private static EffectOp Passive(string flag, string note)
            => new EffectOp(EffectOpKind.GrantPassive, flag, 1, note);

        private static CultivationPathDef Build()
        {
            // —— per-path 专属资源（不进四维 C6）——
            // yaoDan 妖丹：本路唯一内修资源，吞灵气/食妖丹/采天材凝纯；base cap=30（深度设计「cap=30+10×realm」的 realm0 基线，
            //   realm 增益由境界引擎/吞噬功法 AddResourceCap 表达，A.0 单值起底），非战后清零、空则失养。
            // huaXingDu 化形度 0..100：签名跃迁门槛轴；初值/上限基线 cap=30（深度设计「启灵开智诀解锁至30」为入门起底，
            //   高阶上限由化形心法 AddResourceCap 抬到 60/90/100），派生 Flag formHuman。
            // atavismDeg 返祖度 0..100：激爆发/解暴走，derived:atavismFold 折算入战力。
            var resources = new[]
            {
                new ResourceDef("yaoDan", 0, 30, 0),
                new ResourceDef("huaXingDu", 0, 30, 0),
                new ResourceDef("atavismDeg", 0, 100, 0),
            };

            // —— 战力公式（深度设计 terms：根骨×3 + 武力×3 + 内力×1 + realm×5 + 所选功法power×1 + 妖丹×3 + 返祖度折算(atavismDeg/10)）。
            //    根骨/武力并列最高权（兽躯肉身碾压脆皮）、内力刻意小权（妖修非内力流）；无 daoHeart、无 ×0（R3/R6）。
            //    derived:atavismFold 走 IDerivedProvider（A.0 空注册解析为 0，不抛、不算 ×0：Weight=1≠0）。——
            var power = new PowerFormulaDef(
                new[]
                {
                    new PowerTerm("stat:Constitution", 3, null),     // 根骨为骨：兽躯肉身堆叠主轴，全路最重权之一
                    new PowerTerm("stat:Force", 3, null),            // 武力为爪牙：撕咬扑击/血脉术外放输出，与根骨并列
                    new PowerTerm("stat:Internal", 1, null),         // 内力：弱权，仅供妖丹运转/术法外放转化效率
                    new PowerTerm("realm", 5, null),                 // 境界：每境界+5 厚势线性底，再经 RealmCurve 二次放大
                    new PowerTerm("sumArtPower", 1, null),           // 所选 肉身锻体/血脉术/化形心法/吞噬纳灵 各功法 tier 之和
                    new PowerTerm("res:yaoDan", 3, null),            // 妖丹：续航弹药+战力双主项；空则归零（失养，本路低容错）
                    // 返祖度折算（atavismDeg/10，满约+10）：返祖暴走时此项走 weightStep 临时翻倍（暴走 flag→权重档+1，
                    //   复用 AddTermWeightStep 落 Flags["atavismBurstStep"] 抬权重，不改公式结构）。A.0 空 DerivedRegistry → 解析为 0。
                    new PowerTerm("derived:atavismFold", 1, "atavismBurstStep"),
                },
                System.Array.Empty<PowerMod>(),
                // 态系数（签名机制，PostMul flag 门，整数 num/den）：人形态（formHuman 置位）EffectivePower ×9/10
                //   （战力略降换规避斩妖天克）；兽躯态（formHuman 未置）= 默认不乘（×10/10 即 1）。
                //   天道压制门（LawSuppress：高 UT 段对非人血脉注 −整数压制）是 L1 ModKind（需负向 clamp，PostMul 表达不了），A.0 不引入，见 Note。
                new[]
                {
                    new PostMul("formHuman", 9, 10),
                });

            // —— 战力曲线（深度设计 realmMul=[10,13,17,22,29,38,50,66,88]，realm 0..8，慢而稳、尾段凸加速、封顶低于剑/佛）。
            //    四列等长（M4）：倍率 / UnifiedTierOf（UT0-12 映射）/ 境界名（妖兽→精怪→妖丹结丹→开智化形→化形大成→妖将→妖王→大妖→妖圣）/
            //    升入阈值（吞灵历劫累进，realm0=0 起，相邻差递增反映『慢而稳+化形劫高阶坎』）。——
            var curve = new RealmCurveDef(
                new[] { 15, 21, 29, 44, 67, 101, 155, 242, 383 }, // balance-003: §5 归一化校准至 sword 锚 target(UT)（TR-BAL-001）
                new[] { 0, 2, 4, 6, 8, 9, 10, 11, 12 },
                new[] { "妖兽", "精怪", "妖丹", "开智化形", "化形大成", "妖将", "妖王", "大妖", "妖圣" },
                // 升入第 i 境累进阈值 = Σ 120×(0..i-1)（妖修慢修底厚、realm3 化形劫为第一大坎，阈值步长 120 略高于剑修 100）。
                new[] { 0, 120, 360, 720, 1200, 1800, 2520, 3360, 4320 },
                // —— A.1 境界稿 §2：起步 SubLevelCount 全 1；CanAscend=true；MaxMajor=大境界数-1。——
                new[] { 1, 1, 1, 1, 1, 1, 1, 1, 1 }, true, 8);

            // —— 功法类目（肉身锻体/血脉术/化形心法/吞噬纳灵 各 5 具名 + yaoheart 道心 4 具名）。
            //    具名/效果照深度设计「功法类目」节；命名与命名池驭兽/古族条目同源。——
            var arts = new[]
            {
                // 肉身锻体（兽躯·撕咬硬功，常驻肉身底盘与爪牙输出，决定 sumArtPower 主体之一与兽躯碾压）。
                //   根骨/武力/穿透等改四维项为 flavor（Note）；能落 state 的走具体算子。
                new ArtCategoryDef("肉身锻体", "body", 1, 1, new[]
                {
                    new ArtDef("yx_rs_xuanjia", "玄甲淬鳞功", 1, "肉身锻体",
                        new[] { Passive("scale_armor", "根骨+4;兽躯态受钝击类武力威额外−3(鳞甲抗砸)") }),
                    new ArtDef("yx_rs_silao", "撕天獠牙诀", 2, "肉身锻体",
                        new[] { Passive("fang_pierce", "武力+10;扑击命中后附穿透−4(獠牙破甲,削对方当次减伤)") }),
                    new ArtDef("yx_rs_shanjun", "山君伏虎劲", 3, "肉身锻体",
                        new[] { Passive("crush_fragile", "根骨+8,武力+6;兽躯态对脆皮低根骨目标(对方根骨<本人一半)伤害额外+8(力压剑/丹脆肉)") }),
                    new ArtDef("yx_rs_jiaojin", "蛟筋换血经", 4, "肉身锻体",
                        new[] { Passive("kalpa_body_grow", "根骨+12;每历1次化形劫永久根骨+1(上限+8);妖丹运转回血+2/跳") }),
                    new ArtDef("yx_rs_zumo", "祖魔不灭兽身", 5, "肉身锻体",
                        new[] { Passive("burst_no_exhaust", "根骨+16,武力+10;返祖度≥60时返祖暴走结束后兽躯不虚脱(免暴走后续航惩罚),臻大妖不坏之躯") }),
                }),
                // 血脉术（本命神通·撕咬术法外放，element 克制注入与爆发；本路独有类目，本命神通进阶载体）。
                //   element 克制真判定走独立 CounterMatrix（本体不硬编码对手），A.0 仅以 flag/Note 留痕。
                new ArtCategoryDef("血脉术", "bloodart", 1, 1, new[]
                {
                    new ArtDef("yx_xm_tuxi", "本命吐息·初型", 1, "血脉术",
                        new[] { Passive("innate_breath", "武力+6;按血脉属性(雷/火/冰/风)对对应被克属性目标伤害+5(喂克制矩阵element环,本体不硬编码对手)") }),
                    new ArtDef("yx_xm_hande", "撼地兽吼", 2, "血脉术",
                        new[] { Passive("beast_roar", "武力+8;范围震慑,命中目标下一动作战力−10;对兽类/驭兽群额外打断兽阵协同") }),
                    new ArtDef("yx_xm_baofa", "血脉爆发·祖息", 3, "血脉术",
                        new[]
                        {
                            new EffectOp(EffectOpKind.Cost, "yaoDan", 8, "消耗妖丹8(本击按本命属性附element大克)"),
                            Passive("ancestral_burst", "武力+14;对对应属性目标穿透其一半减免(克制矩阵注+系数)"),
                        }),
                    new ArtDef("yx_xm_xuanwu", "玄武镇煞甲", 3, "血脉术",
                        new[] { Passive("xuanwu_ward", "根骨+10;玄水/玄冰血脉专属,结煞气护体抵下次伤害=妖丹×2,且对纯阳/佛门攻击减免不失效(兼防天克之一)") }),
                    new ArtDef("yx_xm_zulong", "祖龙真形吐息", 5, "血脉术",
                        new[]
                        {
                            new EffectOp(EffectOpKind.Cost, "yaoDan", 15, "顶阶本命神通耗妖丹15(直线至多3目标各武力×1+妖丹×2的element真伤)"),
                            Passive("zulong_breath", "武力+22;返祖度≥80时无视目标根骨,兽躯态终极穿透"),
                        }),
                }),
                // 化形心法（开智参道·由兽求人,养化形度与人形态/稳理智,定本路跃迁门槛与隐蔽轴）。
                //   「化形度上限解锁至 N」落 AddResourceCap(huaXingDu)；态系数升档/隐世钩为 flag/Note。
                new ArtCategoryDef("化形心法", "transform", 1, 1, new[]
                {
                    new ArtDef("yx_hx_qiling", "启灵开智诀", 1, "化形心法",
                        new[]
                        {
                            // base cap=30 已是入门起底，本功确认/巩固化形度上限至 30（A.0 单值起底,不再加抬，避超表）。
                            Passive("kaizhi", "开智参道每跳化形度+1(化形入门,化形度上限解锁至30)"),
                        }),
                    new ArtDef("yx_hx_renmian", "人面参道经", 2, "化形心法",
                        new[]
                        {
                            CapHuaXing(30, "化形度上限至60"),
                            Passive("huaxing_kalpa_roll", "渡化形劫掷点+10;化形度≥30可短时拟人形(规避部分非人歧视)"),
                        }),
                    new ArtDef("yx_hx_taiyi", "太乙化形真解", 3, "化形心法",
                        new[]
                        {
                            CapHuaXing(60, "化形度上限至90"),
                            Passive("form_loss_low", "化形度≥阈时人形态战力态系数由×9/10升至×95/100(化形越精损耗越小);可学1门人族正统功法(隐世融合钩)"),
                        }),
                    new ArtDef("yx_hx_fanben", "返本归元心经", 4, "化形心法",
                        new[] { Passive("burst_keep_form", "返祖暴走时化形度不归0改为临时−40(保留部分隐蔽);暴走理智判定+15(防失控伤己/滥杀结仇)") }),
                    new ArtDef("yx_hx_dadao", "大道化形·人妖两忘", 5, "化形心法",
                        new[]
                        {
                            CapHuaXing(70, "化形度上限满100"),
                            Passive("form_immune_purge", "化形圆满后人形态免疫克制矩阵全部斩妖/非人类counterKey(彻底隐于人族);天道压制门对人形态再减半,臻化形登顶"),
                        }),
                }),
                // 吞噬纳灵（资源引擎·吞灵气食妖丹采天材,养妖丹与返祖度,定续航与捷径）。
                //   「妖丹上限+N」落 AddResourceCap(yaoDan)；噬妖夺丹/吞噬同类的正道仇视=AdjustRelationEdge(L1),A.0 以 Note 留痕。
                new ArtCategoryDef("吞噬纳灵", "devour", 1, 1, new[]
                {
                    new ArtDef("yx_ts_naling", "纳灵吐纳法", 1, "吞噬纳灵",
                        new[]
                        {
                            CapYaoDan(10, "妖丹上限+10"),
                            Passive("qi_vein_charge", "厚灵区/灵脉节点驻留每跳妖丹+3(基础充能,对位★qi_vein)"),
                        }),
                    new ArtDef("yx_ts_shiyao", "噬妖夺丹诀", 3, "吞噬纳灵",
                        new[]
                        {
                            CapYaoDan(20, "妖丹上限+20"),
                            Passive("devour_dan", "食/夺他妖妖丹一次妖丹+8、返祖度+3,但结正道/同类仇视(近魔道掠夺,养丹捷径也是最损者,仇视走AdjustRelationEdge L1)"),
                        }),
                    new ArtDef("yx_ts_zuxue", "返祖血食经", 3, "吞噬纳灵",
                        new[] { Passive("atavism_food", "食祖血/返祖灵物时返祖度额外+5;返祖度≥40解锁短时返祖暴走(兽躯战力暴涨档)") }),
                    new ArtDef("yx_ts_tuntian", "吞天纳灵大法", 4, "吞噬纳灵",
                        new[]
                        {
                            CapYaoDan(40, "妖丹上限+40"),
                            Passive("kalpa_refill_dan", "化形劫/天劫后回满妖丹(历劫即补);以内力换妖丹:每点内力额外折2点妖丹上限"),
                        }),
                    new ArtDef("yx_ts_wanyao", "万妖归元食髓功", 5, "吞噬纳灵",
                        new[]
                        {
                            CapYaoDan(60, "妖丹上限+60"),
                            Passive("devour_no_backlash", "吞噬同类妖丹时不增理智反噬、返祖度+8(吞噬流终局),但正道仇视加倍(高收益高仇视)"),
                        }),
                }),
                // yaoheart 道心类目（M1，深度设计「妖心·由兽证道之念」）。A.0 仅装载不结算 → tier=0（sumArtPower 贡献 0）、
                // effects 留空（不触 daoHeart/innerDemon/comprehension 资源算子，那是 A.2 道心层的事）。具名 + power=0。
                new ArtCategoryDef("妖心", "daoheart", 1, 1, new[]
                {
                    new ArtDef("dh_yao_qiling", "启灵明性诀", 0, "妖心", System.Array.Empty<EffectOp>()),
                    new ArtDef("dh_yao_zhishou", "制兽守心录", 0, "妖心", System.Array.Empty<EffectOp>()),
                    new ArtDef("dh_yao_bumi", "化而不迷心经", 0, "妖心", System.Array.Empty<EffectOp>()),
                    new ArtDef("dh_yao_renyao", "人妖两忘大道心", 0, "妖心", System.Array.Empty<EffectOp>()),
                }),
            };

            // —— 战技（深度设计「战技」节，OnUse 算子 + Cost 资源表；妖丹=yaoDan）。
            //    伤害/穿透等具体结算 Phase 3 接，A.0 以 AddPenInteger 近似整数破防量占位 + Cost 表达资源门槛；
            //    护体/防御类以 AddFlatDR 占位；回资源类以 AddResource 落 state。——
            var skills = new[]
            {
                // 血脉爆发·撼天扑：兽躯终极扑击,倾注当前妖丹单点穿透(武力×2+妖丹×3);本命属性对对应被克属性无视根骨。施后妖丹清空。门槛妖丹≥10。
                // B5 批2 招牌招迁移：占位 AddPenInteger(50) → Modules.PenFromResource(yaoDan,×3)（妖丹越满扑击越狠,见底哑火真差分；Amount2=1 工厂保证 §15.6）。
                new CombatSkillDef("sk_yx_hantian", "血脉爆发·撼天扑", 3,
                    new[] { Modules.PenFromResource("yaoDan", 3, note:"武力×2+妖丹×3单点穿透,妖丹转穿透;本命属性对对应被克属性目标无视根骨;施后妖丹清空(全或无血脉爆发)") },
                    new Dictionary<string, int> { { "yaoDan", 10 } }),
                // 返祖暴走：激发祖兽血脉,3 tick 内 根骨/武力各×130/100、本命神通element克制系数+;但化形度归0、暴走结束理智不过则误伤场上并结仇。返祖度≥40,妖丹6。
                new CombatSkillDef("sk_yx_atavism", "返祖暴走", 4,
                    new[]
                    {
                        // 暴走置 atavismBurstStep（抬 derived:atavismFold 项权重档,临时翻倍折算)与 atavismBurst flag(战后清),近似返祖增幅。
                        new EffectOp(EffectOpKind.AddTermWeightStep, "atavismBurstStep", 1, "返祖暴走:返祖度折算项权重档+1(临时翻倍),根骨/武力各×130/100,本命神通element克制系数+"),
                        new EffectOp(EffectOpKind.SetFlag, "atavismBurst", 1, "返祖暴走态(临时,战后清);化形度归0(或经返本归元心经改−40)、暴走结束理智判定不过则误伤友方并结正道仇"),
                    },
                    new Dictionary<string, int> { { "yaoDan", 6 } }),
                // 兽躯硬抗·铜筋：1回合内 根骨态减免×2,把整段攻势硬扛成擦伤(妖修版铁山靠);专破被放风筝前的接战窗口。妖丹15。(保命/扛接战类)
                new CombatSkillDef("sk_yx_tongjin", "兽躯硬抗·铜筋", 2,
                    new[] { Modules.FlatDR(12, "1回合内根骨态减免×2,把整段攻势硬扛成擦伤(类体修铁山靠妖修版),专破被放风筝前的接战窗口") },
                    new Dictionary<string, int> { { "yaoDan", 15 } }),
                // 化形遁影：化形度≥阈入人形态瞬遁/拟人混入,脱战或规避斩妖/非人锁定一次;遁后本场斩妖类counterKey对己按0计1回合。妖丹8。(规避斩妖/保命类)
                new CombatSkillDef("sk_yx_dunying", "化形遁影", 2,
                    new[] { Modules.SituationalAdj(0, "化形度≥阈入人形态瞬遁/拟人混入,脱战或规避斩妖/非人类锁定一次;遁后本场斩妖类counterKey对己按0计1回合(规避正道斩妖保命术)") },
                    new Dictionary<string, int> { { "yaoDan", 8 } }),
                // 噬妖回元：扑杀/重伤目标后掠夺精血妖气,回妖丹+4、本场根骨+2;击杀妖类额外妖丹+4(轻量噬妖无返祖反噬)。妖丹2。
                new CombatSkillDef("sk_yx_huiyuan", "噬妖回元", 1,
                    new[] { new EffectOp(EffectOpKind.AddResource, "yaoDan", 4, "扑杀/重伤目标后掠夺精血妖气回妖丹+4(击杀妖类目标额外+4),本场根骨+2;轻量噬妖无返祖反噬负担") },
                    new Dictionary<string, int> { { "yaoDan", 2 } }),
                // 妖王威压·镇群：以血脉位阶强制威慑低阶兽/驭兽群,战力−15并打断兽阵协同;对纯人族只算半数(妖王立威,集体分支起手)。妖丹6。
                new CombatSkillDef("sk_yx_zhenqun", "妖王威压·镇群", 3,
                    new[] { Modules.FlatPen(15, "对低阶兽/驭兽群施血脉压制使其战力−15并打断兽阵协同;对纯人族目标只算半数(妖王立威,克驭兽师/低阶妖;血脉压制debuff Phase3)") },
                    new Dictionary<string, int> { { "yaoDan", 6 } }),
                // 玄煞兽鳞罩：起妖煞护体抵下次伤害=妖丹×2;被纯阳/雷法/佛光攻击时该护罩仅减半失效(兼防天克的刹车)。妖丹5。(防天克/保命类)
                new CombatSkillDef("sk_yx_linzhao", "玄煞兽鳞罩", 2,
                    new[] { Modules.FlatDR(10, "起妖煞护体抵下次伤害=妖丹×2;被纯阳/雷法/佛光攻击时该护罩仅减半失效而非全失(兼防本路天克的刹车之一)") },
                    new Dictionary<string, int> { { "yaoDan", 5 } }),
                // 妖兽铠·反震：妖兽鳞甲反震（OnDefend）。yaoDan≥8,消耗8。
                // B5扩21: ReflectDamage — 妖修妖兽鳞甲反震,Amount=1/Amount2=4→1/4来袭伤害反震攻方。
                new CombatSkillDef("sk_yx_yaoshou_kai", "妖兽铠·反震", 3,
                    new[] { Modules.Reflect(1, 4, "妖兽鳞甲反震:1/4来袭伤害反震攻方") },
                    new Dictionary<string, int> { { "yaoDan", 8 } }),
                // 妖毒噬体[dot]：妖毒持续伤,2/tick×3回合。yaoDan≥3,消耗3。
                // B5扩21: Dot — 妖修妖毒挂载持续伤。
                new CombatSkillDef("sk_yx_yaodu", "妖毒噬体", 2,
                    new[] { Modules.Dot("yaoDu", 2, 3, "妖毒噬体:2/tick×3回合持续伤") },
                    new Dictionary<string, int> { { "yaoDan", 3 } }),
            };

            return new CultivationPathDef(
                "yao_xiu_huaxing", "妖修·化形道",
                "physical",
                // 属性/形态 tag（melee 近战 / brute 兽躯肉身碾压 / parasite 妖躯可中毒寄生态 / evil 偏邪面非人），非对手 PathId（R2）。
                new[] { "melee", "brute", "parasite", "evil" },
                resources,
                power,
                curve,
                arts,
                skills,
                new EntryGateDef("tag:yao_root"),
                new SelectionRuleDef(2, 3), // 战技抽 2~3（深度设计选取规则：至少1个保命/扛接战/防天克类）
                null);
        }
    }
}
