using System.Collections.Generic;

namespace Jianghu.Cultivation.Paths
{
    /// <summary>
    /// 剑修·剑仙 <c>sword_immortal</c>（21 路范式路）。数据照《每路深度设计》剑修节 +
    /// 《内容补遗》第六部「1. 剑修 sword_immortal — 道心：无漏剑心」+ 命名池剑修条目。
    /// 高爆发低容错：武力为骨、剑意为锋、悟性化意、境界开锋（凸加速曲线，高阶天花板冠绝诸路）。
    ///
    /// 红线落实：terms 无 ×0、无 daoHeart（R3/R6）；SituationalTags=属性/形态 tag 非对手 PathId（R2）；
    /// RealmCurve 四列等长（M4）；含 1 个 Role=daoheart 类目 swordheart（M1，A.0 仅装载不结算 → tier=0
    /// 使 sumArtPower 贡献 0、effects 留空不触 daoHeart 资源算子）。canon pathId（R4）。纯整数，禁浮点。
    /// </summary>
    public static class SwordImmortalPath
    {
        public static readonly CultivationPathDef Def = Build();

        // —— 被动算子（装配期落 state，核心 EffectOpKind）。功法的「武力+N/悟性+N」等改四维项 A.0
        //    为 flavor 不落算子（生成期 Σ=80 不被功法污染，深度设计§选取规则「功法只加 power 不改 Σ」），
        //    仅以 Note 留痕；能落 state 的「剑意值上限+N」走 AddResourceCap、被动开关走 GrantPassive/SetFlag。——
        private static EffectOp CapSwordWill(int amt, string note)
            => new EffectOp(EffectOpKind.AddResourceCap, "swordWill", amt, note);

        private static EffectOp Passive(string flag, string note)
            => new EffectOp(EffectOpKind.GrantPassive, flag, 1, note);

        private static CultivationPathDef Build()
        {
            // —— per-path 专属资源（不进四维 C6）——
            // swordWill 剑意值：见血积累、驱动战技；base cap=20（深度设计「上限=20+5×realm」的 realm0 基线，
            //   realm 增益由心法 AddResourceCap 表达，A.0 单值起底）。jianCheng 本命剑剑成度 0..100。
            var resources = new[]
            {
                new ResourceDef("swordWill", 0, 20, 0),
                new ResourceDef("jianCheng", 0, 100, 0),
            };

            // —— 战力公式（深度设计 terms：武力×4 + 悟性×3 + 内力×2 + 根骨×1 + realm×6 + 所选功法power×2 + 剑意值×3）。
            //    武力权重全路最高（单点穿透）、根骨刻意最低（脆皮低容错）；无 daoHeart、无 ×0（R3/R6）。——
            var power = new PowerFormulaDef(
                new[]
                {
                    new PowerTerm("stat:Force", 4, null),       // 武力为骨：单点穿透/出剑速度，全路最高
                    new PowerTerm("stat:Insight", 3, null),     // 悟性化意：剑意爆发之根
                    new PowerTerm("stat:Internal", 2, null),    // 内力：御剑/剑气外放燃料
                    new PowerTerm("stat:Constitution", 1, null),// 根骨：刻意最低权重，脆皮兜底
                    new PowerTerm("realm", 6, null),            // 境界：斩道破境总闸门
                    new PowerTerm("sumArtPower", 2, null),      // 所选剑法/心法/身法/剑意各功法 tier 之和
                    new PowerTerm("res:swordWill", 3, null),    // 剑意值：满载爆发/清空哑火
                },
                System.Array.Empty<PowerMod>(),
                null);

            // —— 战力曲线（深度设计 realmMul=[10,14,20,30,46,70,108,170,270]，realm 0..8，凸加速）。
            //    四列等长（M4）：倍率 / UnifiedTierOf（UT0-12 映射：三流→二流→一流→绝顶→宗师→大宗师/手中无剑）/
            //    境界名（炼气→筑基→金丹→元婴→化神→炼虚→合体→大乘→剑仙）/ 升入阈值（斩道里程累进，realm0=0 起）。——
            var curve = new RealmCurveDef(
                new[] { 10, 14, 20, 30, 46, 70, 108, 170, 270 },
                new[] { 0, 2, 4, 6, 8, 9, 10, 11, 12 },
                new[] { "炼气", "筑基", "金丹", "元婴", "化神", "炼虚", "合体", "大乘", "剑仙" },
                // 斩道里程 ≥100×当前realm 升阶 → 升入第 i 境累进阈值 = Σ 100×(0..i-1)（深度设计途径③）。
                new[] { 0, 100, 300, 600, 1000, 1500, 2100, 2800, 3600 },
                // —— A.1 境界稿 §2：起步 SubLevelCount 全 1（flatIndex==大境界，小境界加密属 A1.2）；
                //    CanAscend=true（修士）；MaxMajor=大境界数-1。——
                new[] { 1, 1, 1, 1, 1, 1, 1, 1, 1 }, true, 8);

            // —— 功法类目（剑法/心法/身法/剑意 各 5 具名 + swordheart 道心 5 具名）。
            //    具名/效果照深度设计「功法类目」节；命名与命名池剑修条目同源。——
            var arts = new[]
            {
                // 剑法（主修招式·单点穿透，决定 sumArtPower 主体）。武力/悟性/穿透等改四维项为 flavor（Note）。
                new ArtCategoryDef("剑法", "attack", 1, 1, new[]
                {
                    new ArtDef("sw_jf_sanchi", "三尺霜", 1, "剑法",
                        new[] { Passive("sword_pierce", "武力+2,出剑命中margin判定+3") }),
                    new ArtDef("sw_jf_liuguang", "流光剑法", 2, "剑法",
                        new[] { Passive("sword_pierce", "武力+4,对单体穿透伤害+6(无视根骨减免2)") }),
                    new ArtDef("sw_jf_dugu", "独孤九剑·破剑式", 3, "剑法",
                        new[] { Passive("yu_qiang_ze_qiang", "武力+6,悟性≥18遇强则强(对方power每高10本招+5,上限+30)") }),
                    new ArtDef("sw_jf_liangyi", "两仪微尘剑", 4, "剑法",
                        new[] { Passive("sword_pierce", "武力+8,内力+3,剑气外放穿透至多3邻近目标各减半") }),
                    new ArtDef("sw_jf_wuliang", "太上忘情·无量剑诀", 5, "剑法",
                        new[] { Passive("renjian_double", "武力+11,悟性+4;剑意值≥30单击伤害×2(人剑两忘)") }),
                }),
                // 心法（剑心内功·养剑意与续航，定 swordWill 上限增益 → 落 AddResourceCap）。
                new ArtCategoryDef("心法", "internal", 1, 1, new[]
                {
                    new ArtDef("sw_xf_ningjian", "凝剑诀", 1, "心法",
                        new[] { CapSwordWill(3, "内力+2,剑意值上限+3") }),
                    new ArtDef("sw_xf_qingxin", "清心剑典", 2, "心法",
                        new[] { Passive("postwar_swordwill", "内力+3,悟性+2,每场战后剑意值额外回+2") }),
                    new ArtDef("sw_xf_taiqing", "太清剑意心经", 3, "心法",
                        new[]
                        {
                            CapSwordWill(6, "内力+5,剑意值上限+6"),
                            Passive("swordwill_keep10", "'剑意不散':胜后保留至多10点剑意值不清零"),
                        }),
                    new ArtDef("sw_xf_cihang", "慈航剑典·静念禅", 4, "心法",
                        new[] { Passive("hard_block_once", "内力+6,根骨+3;被击破时剑意值≥15可硬抗一次致死伤(每境界1次)") }),
                    new ArtDef("sw_xf_liangyi", "两仪剑心·太极归元", 5, "心法",
                        new[]
                        {
                            CapSwordWill(10, "内力+8,悟性+5,剑意值上限+10"),
                            // 开光后心法 power 使 swordWill 项再+1阶（AddTermWeightStep 抬 swordWill 项权重台阶）。
                            new EffectOp(EffectOpKind.AddTermWeightStep, "swordWillStep", 1, "开光后swordWill项+1阶"),
                        }),
                }),
                // 身法（御剑遁形·走位贴脸，定脆皮容错/先手）。闪避/先手改判定为 flavor（Note）。
                new ArtCategoryDef("身法", "movement", 1, 1, new[]
                {
                    new ArtDef("sw_bf_taxue", "踏雪无痕", 1, "身法",
                        new[] { Passive("evade", "闪避+3,先手出剑判定+2") }),
                    new ArtDef("sw_bf_lingbo", "凌波微步", 2, "身法",
                        new[] { Passive("evade", "闪避+5,绕侧背首击穿透+4") }),
                    new ArtDef("sw_bf_yujian", "御剑术·乘虚", 3, "身法",
                        new[] { Passive("fly_sword", "内力+3,御剑飞行越障直取后排,贴脸单点+6") }),
                    new ArtDef("sw_bf_sandiu", "剑遁·三十六天罡", 4, "身法",
                        new[] { Passive("evade", "闪避+7,剑意值≥10每场可剑光一闪强制规避一次必中") }),
                    new ArtDef("sw_bf_fatian", "法天象地·万里御剑", 5, "身法",
                        new[]
                        {
                            new EffectOp(EffectOpKind.AddResource, "swordWill", 5, "开战首回合剑意值+5(先声夺人)"),
                            Passive("escape_pursue", "内力+6,悟性+3,脱离战场或追杀必成"),
                        }),
                }),
                // 剑意（本路独有类目·斩道所开的「意」，直接放大 swordWill 与突破天花板）。
                new ArtCategoryDef("剑意", "swordwill", 1, 1, new[]
                {
                    new ArtDef("sw_jy_jianxie", "见血意·杀伐", 1, "剑意",
                        new[] { Passive("bleed_gain", "每次见血额外+1剑意值;斩道里程获取+10%") }),
                    new ArtDef("sw_jy_shouzhuo", "守拙意·后发", 2, "剑意",
                        new[] { Passive("houfa", "被攻击未死积怒,下一击穿透+5;剑意值为0时本意失效") }),
                    new ArtDef("sw_jy_jianzhong", "剑冢意·万剑朝宗", 3, "剑意",
                        new[] { Passive("swarm_scale", "悟性+4;场上每1名敌手本人剑意值上限+2、出剑伤害+2") }),
                    new ArtDef("sw_jy_zhandao", "斩道意·见微", 4, "剑意",
                        new[] { Passive("pierce_constitution", "武力+5,悟性+5;斩破绽:算对手power时其根骨减免对本人无效") }),
                    new ArtDef("sw_jy_renjian", "本命剑意·人剑合一", 5, "剑意",
                        new[]
                        {
                            // 本命剑开光(jianCheng=100)方可习;swordWill项权重再+1阶（AddTermWeightStep）。
                            new EffectOp(EffectOpKind.AddTermWeightStep, "swordWillStep", 1, "swordWill项权重视为+1阶"),
                            Passive("renjian_heyi", "realm倍率结算再×11/10,臻手中无剑(本命剑开光方可习)"),
                        }),
                }),
                // swordheart 道心类目（M1，补遗第六部「无漏剑心」）。A.0 仅装载不结算 → tier=0（sumArtPower 贡献 0）、
                // effects 留空（不触 daoHeart/innerDemon/comprehension 资源算子，那是 A.2 道心层的事）。具名 + power=0。
                new ArtCategoryDef("剑心", "daoheart", 1, 1, new[]
                {
                    new ArtDef("dh_sw_chijian", "持剑问心诀", 0, "剑心", System.Array.Empty<EffectOp>()),
                    new ArtDef("dh_sw_shouzhuo", "守拙剑心录", 0, "剑心", System.Array.Empty<EffectOp>()),
                    new ArtDef("dh_sw_wenjian", "问剑名宿心得", 0, "剑心", System.Array.Empty<EffectOp>()),
                    new ArtDef("dh_sw_wulou", "无漏剑心经", 0, "剑心", System.Array.Empty<EffectOp>()),
                    new ArtDef("dh_sw_renjian", "人剑两忘·太上忘情心", 0, "剑心", System.Array.Empty<EffectOp>()),
                }),
            };

            // —— 战技（深度设计「战技」节，OnUse 算子 + Cost 资源表；剑意值=swordWill）。
            //    伤害/穿透等具体结算 Phase 3 接，A.0 以 AddPenInteger 占位破防量 + Cost 表达资源门槛。——
            var skills = new[]
            {
                // 剑二十三：倾尽全部剑意值,单点终极穿透(武力×3+剑意值×4);施后剑意值清零。门槛剑意值≥20且本命剑开光。
                // B5批2: 招牌招结构化 → PenFromResource(swordWill,4) 剑意转伤(剑意越满越痛,见底则哑火),
                //   武力×3 基础部分由 power 公式/DuelEngine 基线承载(批4),本模块表达剑意爆发的可变穿透。
                new CombatSkillDef("sk_sw_jian23", "剑二十三", 5,
                    new[] { Modules.PenFromResource("swordWill", 4, note: "剑意值×4单点终极穿透(资源转伤),斩道开光无视根骨") },
                    new Dictionary<string, int> { { "swordWill", 20 } }),
                // 万剑归宗：召本命剑分化群剑攻全场(武力×1+悟性×1/敌)。剑意值≥15,消耗15。
                // B5批2: → AoePerTarget(30) 群攻(R2单挑退化×1=+30,群战按敌数放大,敌越多总伤越高)。
                new CombatSkillDef("sk_sw_wanjian", "万剑归宗", 4,
                    new[] { Modules.AoePerTarget(30, "对每个邻近敌手武力×1+悟性×1穿透,敌越多总伤越高(R2单挑退化×1)") },
                    new Dictionary<string, int> { { "swordWill", 15 } }),
                // 破·御剑诀：御剑直取最高power者,必中(武力×2+剑意值×2);窃势-5。剑意值≥10,消耗10。
                new CombatSkillDef("sk_sw_poyujian", "破·御剑诀", 3,
                    new[] { Modules.FlatPen(24, "无视阻挡与距离直取敌方最高power者,必中") },
                    new Dictionary<string, int> { { "swordWill", 10 } }),
                // 遇强破式：独孤九剑反制,读招,对方power每高10本人下一击+8(上限+48)。剑意值≥6,消耗6。
                new CombatSkillDef("sk_sw_yuqiang", "遇强破式", 3,
                    new[] { Modules.FlatPen(8, "对方power每高出本人10点,本人下一击伤害+8(上限+48)") },
                    new Dictionary<string, int> { { "swordWill", 6 } }),
                // 剑气如虹：直线剑气穿透至多2目标(武力×1+8,无视根骨2)。剑意值≥4,消耗4。
                new CombatSkillDef("sk_sw_jianqi", "剑气如虹", 2,
                    new[] { Modules.FlatPen(8, "直线至多2目标各武力×1+8穿透,无视根骨2点") },
                    new Dictionary<string, int> { { "swordWill", 4 } }),
                // 舍身一剑：放弃全部闪避,换下一击+武力值,但本回合受击+10。剑意值≥3,消耗3。
                // B5批2: → Backlash(selfExposed) 自伤换爆发通道(本回合受击+10由批4 selfDmg 接,ApplyOnUse 不改入伤)。
                new CombatSkillDef("sk_sw_sheshen", "舍身一剑", 2,
                    new[] { Modules.Backlash("selfExposed", 10, "下一击伤害+武力值(翻倍式爆发),本回合受击+10(低容错,自伤通道)") },
                    new Dictionary<string, int> { { "swordWill", 3 } }),
                // 祭剑·见血：主动割血祭剑,立即+8剑意值、剑成度+2,根骨-2(本场)。无剑意门槛。
                new CombatSkillDef("sk_sw_jijian", "祭剑·见血", 1,
                    new[]
                    {
                        new EffectOp(EffectOpKind.AddResource, "swordWill", 8, "立即+8剑意值(剑意见底的起爆引信)"),
                        new EffectOp(EffectOpKind.AddResource, "jianCheng", 2, "本命剑剑成度+2"),
                    },
                    new Dictionary<string, int>()),
                // 剑遁·闪：御剑遁形闪避（OnDefend）。需身法类功法→门控。剑意值≥5,消耗5。
                // B5补缺：Evade 模块 — 剑修脆皮低容错的保命闪避, Amount=30→30%来袭伤害减免(功法门控)。
                new CombatSkillDef("sk_sw_jiandun", "剑遁·闪", 3,
                    new[] { Modules.Evade(30, "御剑遁形闪避来袭:30%来袭伤害减免(需身法类功法→门控)") },
                    new Dictionary<string, int> { { "swordWill", 5 } }),
                // 破邪剑意：对阴邪(evil tag)伤害×3/2(联合上界)。剑意值≥4,消耗4。
                // B5扩21: CounterMul — 剑修正道破邪。
                new CombatSkillDef("sk_sw_poxie", "破邪剑意", 2,
                    new[] { Modules.CounterMul("evil", 3, 2, "对阴邪(evil tag)伤害×3/2(联合上界)") },
                    new Dictionary<string, int> { { "swordWill", 4 } }),
            };

            return new CultivationPathDef(
                "sword_immortal", "剑修·剑仙",
                "physical",
                // 属性/形态 tag（melee 近战 / sword 用剑 / righteous 正道），非对手 PathId（R2）。
                new[] { "melee", "sword", "righteous" },
                resources,
                power,
                curve,
                arts,
                skills,
                new EntryGateDef("tag:sword_root"),
                new SelectionRuleDef(2, 3), // 战技抽 2~3（深度设计选取规则⑤）
                null);
        }
    }
}
