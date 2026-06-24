using System.Collections.Generic;

namespace Jianghu.Cultivation
{
    /// <summary>
    /// 儒修·文气浩然道 <c>ru_xiu_haoran</c>（physical·道德维度门·正气克邪+群增益一脉）。数据照
    /// 《余 9 路深度设计》儒修节「儒修（文气浩然道 · 正气克邪+群增益一脉）」。归属九野：稷下论道宫
    /// (中州学宫)道统母庭、奉承熹科举功名、浩然克末劫阴邪。横向破邪护神 + 群体增益枢纽 + 规则压制控场：
    /// 不靠 Force 靠 Insight×浩然，以读书明理积「浩然正气」、诗成杀敌、言出法随、以理/礼/律束敌。
    /// 厚积晚发凸尾曲线，进士「文宫」开群增益、半圣→文圣文位乘性放大破邪/护神/诛心。
    ///
    /// 红线落实（backlog2-B1 浩然×道心解耦）：terms 无 ×0、无 daoHeart（R3/R6）——浩然 haoran 资源上限
    /// 用 realm/养气心法 cap 不用 daoHeart；攻心自溃走 flag:daoHeartBroken 的 PostMul（×1/2）而非全式硬编 ×1/2；
    /// daoHeartGate/usableHaoran=min(haoran,5×daoHeart) 二次钳属 A.2 取值层，A.0 不落（Note 留痕）。
    /// SituationalTags=属性/形态 tag 非对手 PathId（R2）；RealmCurve 四列等长（M4）；含 1 个 Role=daoheart
    /// 类目 ruheart=文心（M1，A.0 仅装载不结算 → tier=0 使 sumArtPower 贡献 0、effects 留空不触
    /// daoHeart/innerDemon 资源算子）。canon pathId（R4）。纯整数，禁浮点。
    /// </summary>
    public static class RuXiuHaoranPath
    {
        public static readonly CultivationPathDef Def = Build();

        // —— 被动算子（装配期落 state，核心 EffectOpKind）。功法的「悟性+N/攻-X%」等改四维/战斗判定项 A.0
        //    为 flavor 不落算子（生成期 Σ=80 不被功法污染），仅以 Note 留痕；能落 state 的「浩然上限+N」
        //    走 AddResourceCap（**仅 realm/养气心法承载，绝不引 daoHeart**，B1 解耦）、被动开关走 GrantPassive。——
        private static EffectOp CapHaoran(int amt, string note)
            => new EffectOp(EffectOpKind.AddResourceCap, "haoran", amt, note);

        private static EffectOp Passive(string flag, string note)
            => new EffectOp(EffectOpKind.GrantPassive, flag, 1, note);

        private static CultivationPathDef Build()
        {
            // —— per-path 专属资源（不进四维 C6）——
            // haoran 浩然正气 HaoranQi：战诗/律条主开销资源。深度设计 capFormula="30+12*realm"，A.0 ResourceDef
            //   仅静态 Cap → 取 base cap=120（≈realm0 的 30 起底 + 容多重养气心法 AddResourceCap 增益空间），
            //   realm/功法增益由 AddResourceCap 表达。**cap 用 realm/功法 不用 daoHeart**（B1 解耦核心）；
            //   可用值 usableHaoran=min(haoran,5×daoHeart) 的 daoHeartGate 二次钳属 A.2 取值层，A.0 不落。
            // wenDan 文胆（文位承载）0..100：战力倍率根基，可被「夺文胆」攻击暂时锐减（战后回升 Phase 3）。
            var resources = new[]
            {
                new ResourceDef("haoran", 0, 120, 0),
                new ResourceDef("wenDan", 0, 100, 0),
            };

            // —— 战力公式（深度设计 terms：悟性×4 + 浩然×3 + realm(文位)×5 + Σ功法power×2 + wenGong×2 + 内力×1 + 武力×1）。
            //    Insight 主属性(代内力,明理之体)、浩然为锋(受道心钳制,A.0 raw 占位)、文位为乘、wenGong 群战放大、
            //    Force 仅零头下限（权重 1，「秀才不靠拳脚」，与剑/体修主靠 Force 鲜明对立）；无 daoHeart、无 ×0（R3/R6）。
            //    res:haoran 项 raw 占位（usableHaoran=min(haoran,5×daoHeart) 二次钳属 A.2，A.0 不引 daoHeart）；
            //    derived:wenGong 群战「人和」派生项（在场可教化同阵营数×文位档，A.0 空注册 DerivedRegistry 返回 0，
            //    单挑即 0，群战 Phase 3 接 IDerivedProvider）。——
            var power = new PowerFormulaDef(
                new[]
                {
                    new PowerTerm("stat:Insight", 4, null),      // 悟性：明理之体，本路主属性(代内力)，决定战诗威能与规则压制深度
                    new PowerTerm("res:haoran", 3, null),        // 浩然正气：横向破邪倍率之源(raw 占位，daoHeartGate 二次钳 A.2)
                    new PowerTerm("realm", 5, null),             // 文位：童生→半圣功名跃迁，最重乘数来源
                    new PowerTerm("sumArtPower", 2, null),       // 所选战诗/律条/养气心法/教化各功法 tier 之和(文章广度)
                    new PowerTerm("derived:wenGong", 2, null),   // 文宫规模派生量：群战「人和」(A.0 空注册→0，单挑近 0)
                    new PowerTerm("stat:Internal", 1, null),     // 内力：养气调息之底，微调项
                    new PowerTerm("stat:Force", 1, null),        // 武力：仅作零头下限，防完全无肉身(刻意几乎不进战力)
                },
                System.Array.Empty<PowerMod>(),
                // —— PostMul（flag 条件后置乘子，PowerEngine 支持；A.0 flag 未置位 → 恒被跳过、纯装载留痕）——
                //    ① 群战「人和」上抬：文宫开演后在场友军达阈 → ×6/5（深度设计 postMul wenGongOpen，
                //       allyCount>=wenGongThr 的人数条件属 Phase 3，A.0 仅以 flag:wenGongOpen 近似）。
                //    ② 攻心自溃·道德负反馈：德崩(innerDemon≥collapseThr 或 daoHeart<文位门槛)置 flag → 全式 ×1/2
                //       （**走 flag 不在基础公式硬编 ×1/2**，B1 浩然项强制对称负反馈；flag 由 A.2 心性层置位，A.0 恒 0）。
                new[]
                {
                    new PostMul("wenGongOpen", 6, 5),    // 文宫群战人和上抬(A.0 未开 → 跳过)
                    new PostMul("daoHeartBroken", 1, 2), // 攻心自溃腰斩(A.0 未崩 → 跳过；走 flag 非硬编)
                });

            // —— 战力曲线（深度设计 realmMul=[9,13,19,30,48,78,122,188,285]，realm 0..8，厚积晚发凸尾·三段不连续跃升）。
            //    四列等长（M4）：倍率 / UnifiedTierOf（UT0-12 文位投影）/ 文位境界名（童生→秀才→举人→进士→翰林→
            //    大儒→亚圣→半圣→文圣）/ 升入阈值（读书明理+养气穷理厚积，realm0=0 起；前段平缓物理孱弱、后段文位乘性起飞）。
            //    倍率为 ×10 定点整数；进士(index3)「文宫」群增益位首跳、翰林(index4)文人风骨心魔劫再跳、
            //    半圣→文圣(index7→8)「借圣人之言」最大跳；末倍率/10=28.5 高天花板。——
            var curve = new RealmCurveDef(
                new[] { 11, 16, 29, 47, 74, 121, 189, 291, 442 }, // INV-CROSS v2: buff +25~28% UT4+; UT8=1.61x sword (was 1.28x)
                new[] { 0, 2, 4, 6, 8, 9, 10, 11, 12 },
                new[] { "童生", "秀才", "举人", "进士", "翰林", "大儒", "亚圣", "半圣", "文圣" },
                // 文位厚积晚发（读书明理累进，前段平缓、宗师后陡，≈110×当前 realm 量级，realm0=0 起）。
                new[] { 0, 110, 330, 660, 1100, 1650, 2310, 3080, 3960 },
                // —— A.1 境界稿 §2：起步 SubLevelCount 全 1；CanAscend=true；MaxMajor=大境界数-1。——
                new[] { 1, 1, 1, 1, 1, 1, 1, 1, 1 }, true, 8);

            // —— 功法类目（战诗/律条/养气心法/教化 各 5 具名 + ruheart 文心道心 4 具名）。
            //    具名/效果照深度设计「功法类目」节。改四维/战斗结算项(伤害/攻-X%/规则压制)A.0 为 flavor 不落算子
            //    （走 GrantPassive flag + Note 留痕，完整结算 Phase 3 接）；能落 state 的浩然上限增益走 AddResourceCap。——
            var arts = new[]
            {
                // 战诗（言出法随·诗成杀敌）·主输出与破邪护神，决定 sumArtPower 主项。诗成即生效=宣言式 EffectOp。
                //   伤害/破邪×2/诛心加伤等具体结算 A.0 不落（GrantPassive 标志 + Note），决定 sumArtPower 的是 tier。
                new ArtCategoryDef("战诗", "attack", 1, 1, new[]
                {
                    new ArtDef("ru_zs_zhengqige", "正气歌·首章", 1, "战诗",
                        new[] { Passive("yan_chu_fa_sui", "解锁言出法随,吟成即生效;命中阴邪/幻术享正气克邪×2,对innerDemon≥60附诛心攻-10%") }),
                    new ArtDef("ru_zs_manjianghong", "满江红·怒发冲冠", 2, "战诗",
                        new[] { Passive("rouse_ally", "外放更省(开销-2/技);连吟同标第2句起+3破邪附伤;对己方同阵营鼓舞攻+8%") }),
                    new ArtDef("ru_zs_zhuxinfu", "春秋大义·诛心赋", 3, "战诗",
                        new[] { Passive("zhu_xin", "专破心识:对soul/illusion/demon按innerDemon×2加伤,并使其夺舍/施幻本回合失效(spirit 反制)") }),
                    new ArtDef("ru_zs_mingmingde", "大学之道·明明德", 4, "战诗",
                        new[] { Passive("guard_shen_group", "护神大成:本方全体免疫一次夺舍/乱心/诛心并清1层精神减益;对阴邪 base×2 真伤,范围随文位扩") }),
                    new ArtDef("ru_zs_haoranchangge", "天地正气·浩然长歌", 5, "战诗",
                        new[]
                        {
                            // 文圣级倾泻全槽浩然换范围破邪净化(ScalarMul 浩然倾泻属战斗 OnUse，A.0 不落)；对纯阳正道同道 power 减半
                            // 且不享克邪(破邪不利己同道)A.0 为 flag+Note。习得抬浩然上限承载倾泻(AddResourceCap)。
                            CapHaoran(20, "文圣级:浩然上限+20承载全槽倾泻"),
                            Passive("haoran_aoe_purify", "可一次倾泻全槽浩然换 AOE 破邪净化+护神;对纯阳正道同道 power 减半且不享克邪系数"),
                        }),
                }),
                // 律条（规则压制·以理礼律束敌）·控场与压制，本路独有「非伤害控场」轴。引经据典=数据化整数束缚。
                //   攻-X%/移动禁/规则谓词等结算 A.0 不落（GrantPassive 标志 + Note，SetFlag 敌侧束缚属 OnUse/Phase 3）。
                new ArtCategoryDef("律条", "control", 1, 1, new[]
                {
                    new ArtDef("ru_lt_lileshushen", "礼乐束身经", 1, "律条",
                        new[] { Passive("mark_shili", "对单体上失礼标记:其下一次主动攻击攻-15%;对阴邪改禁其借煞/借阴回复一回合") }),
                    new ArtDef("ru_lt_wangfarulu", "王法如炉律", 2, "律条",
                        new[] { Passive("law_bind", "以律拘身:范围内敌移动优先级-2、脱离缠斗成功率降;对劫掠/破戒标签者额外攻-10%") }),
                    new ArtDef("ru_lt_yizilifa", "微言大义·一字立法", 3, "律条",
                        new[] { Passive("rule_predicate", "本场设一条规则压制谓词(如此地不得用幻):违者每步受固定6点正气反制;锁自身浩然不被断才气衰减一段") }),
                    new ArtDef("ru_lt_haorantianxian", "浩然天宪·镇压不轨", 4, "律条",
                        new[] { Passive("suppress_immoral", "对邪修群体以理镇压:其innerDemon每多10被本方额外+8伤、增益/护体阴煞-1层;规则压制可同挂2条") }),
                    new ArtDef("ru_lt_yiyanduanzui", "春秋笔削·一言断罪", 5, "律条",
                        new[] { Passive("verdict_sever", "终极压制:对单体断罪清其本回合1增益/禁制、截其下一动作(必先手)、按innerDemon×3计伤;仅对失德/阴邪全效,正道德全者几无效") }),
                }),
                // 养气心法（明理养气·护神根基）·把读书明理/养德守心转成净增益与护神底盘。抬浩然上限(AddResourceCap)、
                //   抬道心容错/护神等结算 A.0 走 GrantPassive flag + Note（daoHeart 自然回升/心魔阈抬属 A.2 心性层，A.0 不落算子）。
                new ArtCategoryDef("养气心法", "internal", 1, 1, new[]
                {
                    new ArtDef("ru_xf_jiyiyangqi", "集义养气诀", 1, "养气心法",
                        new[]
                        {
                            CapHaoran(10, "浩然上限+10;蓄浩然速率+1/步"),
                            Passive("daoheart_regen", "daoHeart 自然回升+1/休整(抬道德地板,A.2 结算)"),
                        }),
                    new ArtDef("ru_xf_zhiyanyanghaoran", "知言养浩然", 2, "养气心法",
                        new[] { Passive("sustain_half", "脱离文气环境时浩然衰减减半(缓秀才离场软肋);被乱心/诛心时心神自伤固定-3") }),
                    new ArtDef("ru_xf_haoranhushen", "浩然护神功", 3, "养气心法",
                        new[]
                        {
                            CapHaoran(15, "浩然上限+15"),
                            Passive("mind_guard", "daoHeart≥阈时免疫精神攻击的乱心/夺神一击(神魂无防变有防,克魂修/幻术);体表常驻正气近身反制固定6点正气伤"),
                        }),
                    new ArtDef("ru_xf_budongxin", "文人风骨·不动心", 4, "养气心法",
                        new[] { Passive("buddongxin", "心魔劫抗+大(对应 UT4 心魔劫强制);innerDemon 涨速减半;攻心自溃触发阈由80→90(更晚崩,容错抬)") }),
                    new ArtDef("ru_xf_weitiandilixin", "为天地立心·圣人之言", 5, "养气心法",
                        new[]
                        {
                            // 文圣级路线专属溢出:浩然上限突破常规 cap +50（仅本心法持有者，AddResourceCap 直落）。
                            // daoHeart 上限+10、战诗对正道同道不再倒扣属 A.2 心性层，A.0 为 flag+Note。
                            CapHaoran(50, "文圣级路线专属溢出:浩然上限+50(仅本心法持有者)"),
                            Passive("sage_public_heart", "daoHeart 上限+10(A.2);战诗外放对正道同道不再倒扣(借圣人公心,破邪不利己解除一半)"),
                        }),
                }),
                // 教化（礼乐增益·群战枢纽）·本路独有戏剧轴:以人为本、化敌为友、收束恩怨,决定 derived:wenGong 群增益效率。
                //   鼓舞攻+%/守御DR+/感召倒戈/回向清减益等结算 A.0 走 GrantPassive flag + Note（AdjustRelationEdge 友军向、
                //   daoHeart+/HaoranQi+ 属 A.2/Phase 3 运行期，A.0 不落资源算子以免污染生成态）。
                new ArtCategoryDef("教化", "support", 1, 1, new[]
                {
                    new ArtDef("ru_jh_xian'gejiaohua", "弦歌教化行", 1, "教化",
                        new[] { Passive("group_dr3", "每教化1名中立/低煞或退1场私斗 daoHeart+2、HaoranQi+5(A.2);在场同阵营守御 DR+3(群体)") }),
                    new ArtDef("ru_jh_lilueguwu", "礼乐鼓舞令", 2, "教化",
                        new[] { Passive("rouse_decree", "开鼓舞(友军向):在场同阵营攻+8%、士气+1档;wenGong 派生项基数+1(群战放大)") }),
                    new ArtDef("ru_jh_chunfenghuayu", "春风化雨·感召", 3, "教化",
                        new[] { Passive("ganzhao", "对濒败非死敌感召:本场敌意-50%、有几率倒戈为中立(走关系边);化敌减员不结杀业,daoHeart+5(A.2)") }),
                    new ArtDef("ru_jh_haoranhuixiang", "浩然回向·正气长存", 4, "教化",
                        new[] { Passive("huixiang", "可把自身浩然按2:1转为德行善果永久抬 daoHeart 上限(A.2 加速文位突破);对全体同阵营回向一次精神减益清除") }),
                    new ArtDef("ru_jh_wanshishibiao", "万世师表·一言兴邦", 5, "教化",
                        new[] { Passive("shibiao", "群体大教化:一次清退/感召全场低煞中立与可动摇之敌,给本方全体师表增益(攻+15%、护神一回合);耗尽浩然不计杀业") }),
                }),
                // ruheart 文心道心类目（M1，对应道心「浩然问心」）。A.0 仅装载不结算 → tier=0（sumArtPower 贡献 0）、
                // effects 留空（**不触 daoHeart/innerDemon/comprehension 资源算子**,那是 A.2 道心层的事——
                // 且 daoHeart/innerDemon 不在 Resources 字典,写 AddResource 会崩,亦违 R3）。具名 + power=0。
                new ArtCategoryDef("文心", "daoheart", 1, 1, new[]
                {
                    new ArtDef("dh_ru_haoranwenxin", "浩然问心诀", 0, "文心", System.Array.Empty<EffectOp>()),
                    new ArtDef("dh_ru_zhiyanyangqi", "知言养气心录", 0, "文心", System.Array.Empty<EffectOp>()),
                    new ArtDef("dh_ru_budongxinjing", "文人不动心经", 0, "文心", System.Array.Empty<EffectOp>()),
                    new ArtDef("dh_ru_weitiandilixin", "为天地立心·圣贤心法", 0, "文心", System.Array.Empty<EffectOp>()),
                }),
            };

            // —— 战技（深度设计「战技」节，OnUse 算子 + Cost 资源表；主开销=haoran 浩然正气）。
            //    伤害/破邪×2/诛心 innerDemon 加伤等具体结算 Phase 3 接，A.0 以 AddPenInteger 占位破防量（量级对齐本路
            //    公式：Insight×N+usableHaoran×N）+ Cost 表达资源门槛。控场/群增益技以 AddSituationalAdj/AddFlatDR 近似。——
            var skills = new[]
            {
                // 诗成杀敌·绝句：吟绝命战诗集火单体,伤害=Insight×3+usableHaoran×2;对 demon/ghost/illusion 享克邪×2 并打断养魂/施幻/炼尸。浩然≥20,消耗20。
                // B5 批2 招牌招迁移：占位 AddPenInteger(40) → FlatPen(40) 基线 + Modules.CounterMul(evil,×2) 克邪两档
                //   （正气克邪：防方带 evil tag→×2,联合上界 ×3/2，§15.4；ru 用 "evil" 统覆 demon/ghost/blood/gu 阴邪）。
                new CombatSkillDef("sk_ru_jueju", "诗成杀敌·绝句", 3,
                    new[]
                    {
                        Modules.FlatPen(40, "吟绝命战诗集火单体 Insight×3+usableHaoran×2 基线破防量"),
                        Modules.CounterMul("evil", 2, note:"对 demon/ghost/illusion 等阴邪(evil tag)享正气克邪×2 并打断养魂/施幻/炼尸"),
                    },
                    new Dictionary<string, int> { { "haoran", 20 } }),
                // 浩然破幻·正气护神：以浩然斩幻身/魂体,对 illusion/soul 真伤无视绕物防,破魂力绕物防/天魔乱心,本方全体免疫一次乱心。浩然≥25+内力5。
                new CombatSkillDef("sk_ru_pohuan", "浩然破幻·正气护神", 4,
                    new[] { Modules.FlatPen(35, "对 illusion/soul 真伤无视绕物防,破魂力绕物防/天魔乱心;本方全体免疫一次乱心(真伤定值;无视绕物防/免乱心 Phase3)") },
                    new Dictionary<string, int> { { "haoran", 25 } }),
                // 微言诛心·断罪：对心术不正者宣读其罪,按目标 innerDemon×3 真伤+引动其心魔走火;对德全/正道几无伤。浩然≥18+文胆透支3。
                // B5扫尾 defer(红线A.8): 伤害按目标 innerDemon×3 缩放,innerDemon 属 A.2 道心轴(B1 解耦:战斗期不读 innerDemon)→A.2 道心层 defer,保 AddPenInteger 占位。
                new CombatSkillDef("sk_ru_zhuxin", "微言诛心·断罪", 4,
                    new[] { Modules.PenFromResource("innerDemon", 3, note: "按目标innerDemon×3真伤;对德全/正道者几乎无伤(诛不义不诛义)") },
                    new Dictionary<string, int> { { "haoran", 18 }, { "wenDan", 3 } }),
                // 文宫鼓舞·阵词：开群战增益词,在场同阵营攻+12%、护神 DR+5、士气+1档共3回合;wenGong 当回合翻倍——群战开团核心。浩然≥15。
                new CombatSkillDef("sk_ru_zhenci", "文宫鼓舞·阵词", 2,
                    new[] { Modules.SituationalAdj(12, "在场同阵营攻+12%、护神 DR+5、士气+1档共3回合;wenGong 派生项当回合翻倍(群战开团)") },
                    new Dictionary<string, int> { { "haoran", 15 } }),
                // 王法镇压·律狱：以律条布律狱困范围失德/阴邪,被困者每步受固定正气灼伤,且无法借煞/遁走/施幻。浩然≥16+1道律条敕牒。
                // B5 批2 招牌招迁移：占位 AddPenInteger(14) → Modules.Control(lawPrison,1)（控场困范围,selectMove 失效；
                //   ApplyOnUse 不改 dmg,控场结算批4 接,本轮断言 Kind+Key 在册）。
                new CombatSkillDef("sk_ru_lvyu", "王法镇压·律狱", 3,
                    new[] { Modules.Control("lawPrison", 1, "布律狱困范围失德/阴邪:被困者每步受固定正气灼伤,且无法借煞/遁走/施幻(控场非伤害,批4结算)") },
                    new Dictionary<string, int> { { "haoran", 16 } }),
                // 舍身取义·浩然爆发：浩然槽连同部分文胆一次性引爆,超高范围破邪净化(全槽×2 折算);自身 WenDan 永久-3、规则压制位清零——文人风骨赌命技。
                // B5扫尾: 占位 AddPenInteger(50) → Modules.PenFromResource(haoran,×2)（全槽浩然×2 折算,浩然越满越痛、见底哑火真差分；
                //   wenDan 永久-3 自损另以 AddResource 表达；Amount2=1 工厂保证 §15.6）。
                new CombatSkillDef("sk_ru_sheshen", "舍身取义·浩然爆发", 4,
                    new[]
                    {
                        Modules.PenFromResource("haoran", 2, note:"全槽浩然×2 折算超高范围破邪净化(赌命终极,浩然转伤)"),
                        new EffectOp(EffectOpKind.AddResource, "wenDan", -3, "自身文胆永久-3(文位倒退风险),本场规则压制位清零"),
                    },
                    new Dictionary<string, int> { { "haoran", 40 } }),
                // 浩然正气·天地一统：为天地立心降临式,对全场阴邪/幻术/失德 AOE 净化+诛心重创(叠克邪×2 与 innerDemon 加伤),本方全体护神+师表;对纯阳正道同道几无伤。浩然≥40+realm≥6。
                // B5扫尾: 占位 AddPenInteger(60) → FlatPen(60) 基线 + Modules.CounterMul(evil,×2)（对阴邪(evil tag)叠正气克邪×2,
                //   §15.4 联合上界）。innerDemon 加伤属 A.2 道心轴不在 A.0 落,AOE全场/护神 Phase3。
                new CombatSkillDef("sk_ru_tiandiyitong", "浩然正气·天地一统", 5,
                    new[]
                    {
                        Modules.FlatPen(60, "对全场阴邪/幻术/失德 AOE 净化+诛心重创基线破防量;本方全体护神+师表;对纯阳正道同道几无伤 Phase3"),
                        Modules.CounterMul("evil", 2, note:"对阴邪(evil tag)叠正气克邪×2(联合上界);innerDemon 加伤属 A.2 道心轴 defer"),
                    },
                    new Dictionary<string, int> { { "haoran", 40 } }),
            };

            return new CultivationPathDef(
                "ru_xiu_haoran", "儒修·文气浩然道",
                "physical",
                // 属性/形态 tag（spirit_attack 护神/诛心走精神侧 / righteous 正道 / control 规则压制控场），
                // 非对手 PathId（R2）。破邪靠克制矩阵 element(纯阳vs阴邪) 与 tag 谓词(demon/ghost/illusion 享克邪×2、
                // undead_construct 诛心结构性失效),不在此硬编对手身份。
                new[] { "spirit_attack", "righteous", "control" },
                resources,
                power,
                curve,
                arts,
                skills,
                // 21 路唯一 entry tag 约定：每路 entry tag = 唯一 <pathkey>_root（ru_root）。
                // 派生池 RootTagPool() 随之含 ru_root → 儒修可被定路。
                new EntryGateDef("tag:ru_root"),
                new SelectionRuleDef(2, 3), // 战技抽 2~3（群战开团+破邪+护神组合）
                null);
        }
    }
}
