using System.Collections.Generic;

namespace Jianghu.Cultivation.Paths
{
    /// <summary>
    /// 血修·血煞道 <c>xue_xiu_xuesha</c>（physical 偏门·透支爆发路）。数据照《余 9 路深度设计》血修节
    /// 「血修 · 血煞道」+《内容补遗》⑥血煞道心层 bloodheart + counterKeys/资源表。
    /// 以精血为本钱、燃血暴涨噬血续命：武力为骨、血气燃料化为爆发主项（唯一靠 flag 阶跃 burnStep 的爆发开关）、
    /// 根骨刻意低权只作承血门槛（极凸短窗曲线，燃血理想态天花板冠绝、血气见底则坍回裸武夫线）。
    ///
    /// 红线落实：terms 无 ×0、无 daoHeart（R3/R6）——血煞 xuesha **不进 terms**（深度设计原案 res:xuesha×0
    /// 违 R6 禁 ×0，故删；xuesha 只做天谴自险/克制网放大档，仅以 Note 留痕，对齐 backlog2-M2）；
    /// SituationalTags=属性/形态 tag 非对手 PathId（R2）；RealmCurve 四列等长（M4）；含 1 个 Role=daoheart
    /// 类目 bloodheart（M1，A.0 仅装载不结算 → tier=0 使 sumArtPower 贡献 0、effects 留空不触
    /// daoHeart/innerDemon 资源算子，那是 A.2 道心层的事）。canon pathId（R4）。纯整数，禁浮点。
    /// </summary>
    public static class XueXiuXueshaPath
    {
        public static readonly CultivationPathDef Def = Build();

        // —— 被动算子（装配期落 state，核心 EffectOpKind）。功法的「武力威+N/穿透+N」等改四维/结算项 A.0
        //    为 flavor 不落算子（生成期 Σ=80 不被功法污染，深度设计⑧「功法只加 power 不改 Σ」），仅以 Note 留痕；
        //    能落 state 的「血气上限+N」走 AddResourceCap、被动开关走 GrantPassive、燃血爆发档跃迁走 AddTermWeightStep
        //    （burnStep，本路签名）、承血阈厚积侧走 AddResource(thrBurnBonus)。——
        private static EffectOp CapQixie(int amt, string note)
            => new EffectOp(EffectOpKind.AddResourceCap, "qixie", amt, note);

        private static EffectOp BurnStep(int step, string note)
            => new EffectOp(EffectOpKind.AddTermWeightStep, "burnStep", step, note);

        private static EffectOp ThrBurn(int amt, string note)
            => new EffectOp(EffectOpKind.AddResource, "thrBurnBonus", amt, note);

        private static EffectOp Passive(string flag, string note)
            => new EffectOp(EffectOpKind.GrantPassive, flag, 1, note);

        private static CultivationPathDef Build()
        {
            // —— per-path 专属资源（不进四维 C6）——
            // qixie 血气本钱：进攻燃料/可燃血换爆发；base cap=80（深度设计「cap=80+10×realm」的 realm0 基线，
            //   realm/噬法增益由功法 AddResourceCap 表达，A.0 单值起底）。
            // xuesha 血煞杀业：纯负债天敌轴（不进 terms，只挂天谴/克制网），cap=999、cooldown 极慢衰减。
            // thrBurnBonus 承血阈加成：抬 thrBurn=8+realm×2+thrBurnBonus，减轻燃血自燃；血脉返祖/噬法/血器叠加。
            var resources = new[]
            {
                new ResourceDef("qixie", 0, 80, 0),
                new ResourceDef("xuesha", 0, 999, 0),
                new ResourceDef("thrBurnBonus", 0, 40, 0),
            };

            // —— 战力公式（深度设计⑩ terms：武力×4 + 血气×3[burnStep 阶跃] + 内力×1 + 根骨×1 + realm×5 + 所选功法power×2）。
            //    武力权重全路最高（燃血穿透落点）、根骨刻意低权（承血门槛非耐久堆叠）；血气项挂 WeightStepKey="burnStep"
            //    （燃血回合临时抬权，本路唯一 flag 阶跃爆发开关）。**血煞 xuesha 不入 terms**（删 res:xuesha×0：违 R6 禁
            //    ×0，且血煞绝不进战力是本路反「越邪越强」换皮的关键 → 只做负债轴，Note 留痕）；无 daoHeart、无 ×0（R3/R6）。——
            var power = new PowerFormulaDef(
                new[]
                {
                    new PowerTerm("stat:Force", 4, null),            // 武力为骨：血战拳兵穿透输出，本路最高权重
                    new PowerTerm("res:qixie", 3, "burnStep"),       // 血气燃料：基础权重3，燃血回合临时 +burnStep（开关式爆发）
                    new PowerTerm("stat:Internal", 1, null),         // 内力：仅供运血/血气吐纳转化，弱权（血修非内力流）
                    new PowerTerm("stat:Constitution", 1, null),     // 根骨：刻意低权，仅作承血阈/噬血回血上限门槛源（不堆耐久）
                    new PowerTerm("realm", 5, null),                 // 境界：血道破障总闸；乘性放大由 realmMultipliers 承载
                    new PowerTerm("sumArtPower", 2, null),           // 所选血功/噬法/血祭各功法 tier→power 之和
                    // 血煞 res:xuesha 显式不入 terms：纯负债天谴轴（跨 {300,600,900} 触天谴、被佛/雷 anti_evil 放大档读此），
                    // 绝不进 EffectivePower（删原案 ×0 项以守 R6；对齐 backlog2-M2「血煞不进 terms 只注释」）。
                },
                System.Array.Empty<PowerMod>(),
                null);

            // —— 战力曲线（深度设计 realmMul=[10,14,21,32,50,78,122,192,300]，realm 0..8，极凸短窗·末倍率 30.0）。
            //    此数组是「燃血满载理想态」包络（实战另受 血气=0 坍缩 / 血煞>900 走火打折两道，退化态 ≤ 同 realm 0.6×）。
            //    四列等长（M4）：倍率 / UnifiedTierOf（UT0-12：三流→二流→一流→绝顶→宗师→大宗师→…→血神大成）/
            //    境界名（炼气→筑基→金丹→元婴→化神→炼虚→合体→大乘→血神）/ 升入阈值（血道里程累进，realm0=0 起）。——
            var curve = new RealmCurveDef(
                new[] { 10, 14, 21, 32, 50, 78, 122, 192, 300 },
                new[] { 0, 2, 4, 6, 8, 9, 10, 11, 12 },
                new[] { "炼气", "筑基", "金丹", "元婴", "化神", "炼虚", "合体", "大乘", "血神" },
                // 血道里程 ≥100×当前realm 升阶（见血+margin / 血池淬体+20 / 血祭祭炼+30）→ 升入第 i 境累进阈值
                // = Σ 100×(0..i-1)（深度设计途径③，与剑修斩道里程同构）。
                new[] { 0, 100, 300, 600, 1000, 1500, 2100, 2800, 3600 },
                // —— A.1 境界稿 §2：起步 SubLevelCount 全 1；CanAscend=true；MaxMajor=大境界数-1。——
                new[] { 1, 1, 1, 1, 1, 1, 1, 1, 1 }, true, 8);

            // —— 功法类目（血功/噬法/血祭 各 5 具名 + bloodheart 血心 4 具名）。
            //    具名/效果照深度设计「功法类目」节 + ⑥血煞道心层表；命名与血河魔宫血道一脉同源。——
            var arts = new[]
            {
                // 血功（主修·燃血搏杀，把血气燃料转穿透杀伤，定 sumArtPower 主体与燃血爆发上限）。武力威/穿透改结算项为 flavor（Note）。
                //   血神顶法以 AddTermWeightStep(burnStep) 抬燃血爆发档（本路签名爆发开关）。
                new ArtCategoryDef("血功", "attack", 1, 1, new[]
                {
                    new ArtDef("xue_xg_chili", "血煞拳·赤厉式", 1, "血功",
                        new[] { Passive("burn_strike", "武力威+6;燃血时每燃4点血气本击附穿透+3(基础燃血输出)") }),
                    new ArtDef("xue_xg_huaxue", "化血神功", 3, "血功",
                        new[] { Passive("devour_steady", "武力威+18;噬血:击杀/重伤后回血气额外+6、burnStep触发更稳(代价xuesha+2/次)") }),
                    new ArtDef("xue_xg_fenxue", "焚血八杀刀", 4, "血功",
                        new[]
                        {
                            ThrBurn(2, "承血阈thrBurn等效+2(更易燃多)"),
                            Passive("pierce_constitution_third", "武力威+26;燃血回合无视目标根骨减免1/3(血焰灼穿血肉)"),
                        }),
                    new ArtDef("xue_xg_daojuan", "血河倒卷诀", 4, "血功",
                        new[] { Passive("burn_cleave", "武力威+24;范围燃血:对至多3邻近目标各武力×1燃血穿透,噬中多目标按命中数叠回血气") }),
                    new ArtDef("xue_xg_shiba", "血神十八噬", 5, "血功",
                        new[]
                        {
                            // 血神道顶法：burnStep 上限再 +2（燃血爆发档跃迁，本路独有 flag 阶跃）。
                            BurnStep(2, "血神道顶法:burnStep上限+2(爆发档跃迁,燃血量越大档越高)"),
                            Passive("blood_god_overdraft", "武力威+36;燃血量≥30单击直伤×2(血神临世极限透支),但溢出燃血自燃翻倍"),
                        }),
                }),
                // 噬法（噬血夺元·续航与回本，定 qixie 上限与「以战养战」续航 → 落 AddResourceCap，对抗「断血来源则衰」）。
                new ArtCategoryDef("噬法", "internal", 1, 1, new[]
                {
                    new ArtDef("xue_sf_tuna", "噬血吐纳法", 1, "噬法",
                        new[] { CapQixie(30, "血气上限+30;噬血回本系数+1(每场战胜回血气底盘抬升)") }),
                    new ArtDef("xue_sf_chiyang", "血池温养经", 3, "噬法",
                        new[] { CapQixie(60, "血气上限+60;脱战缓回血气速率翻倍(缓'不能龟缩'之苦,仍远慢于法修回蓝)") }),
                    new ArtDef("xue_sf_shisui", "噬髓夺元大法", 4, "噬法",
                        new[]
                        {
                            CapQixie(90, "血气上限+90;噬血回本×3/2(代价xuesha+3/次)"),
                            Passive("drain_internal", "噬血时额外掠夺目标本源:命中后对目标Internal-4(夺元削敌,经ApplyStat chokepoint结算)"),
                        }),
                    new ArtDef("xue_sf_xuegui", "血傀续命术", 4, "噬法",
                        new[]
                        {
                            CapQixie(80, "血气上限+80"),
                            Passive("blood_puppet", "可炼血傀儡/血奴1具(roster,power=12+境界档),代主受一次燃血自燃、献祭立降xuesha-20(弃子压煞)"),
                        }),
                    new ArtDef("xue_sf_bumie", "血神不灭体", 5, "噬法",
                        new[]
                        {
                            CapQixie(120, "血气上限+120"),
                            Passive("blood_god_revive", "realm≥6每场首次'燃血焚身致死'自动燃尽全部血气续命一次;血气=0时不触发(克制点)"),
                        }),
                }),
                // 血祭（炼宝·布阵·血祭流，把精血/血煞祭炼成法宝血阵的厚积/枢纽侧，给血修一条非纯搏杀成长线）。
                //   常驻血器/血阵减伤走 AddFlatDR；血脉返祖抬承血阈走 AddResource(thrBurnBonus)；血煞结丹抬燃血档走 burnStep。
                new ArtCategoryDef("血祭", "combat", 1, 1, new[]
                {
                    new ArtDef("xue_xj_lianqi", "血祭炼器诀", 2, "血祭",
                        new[]
                        {
                            new EffectOp(EffectOpKind.AddFlatDR, "bloodArtifact", 4, "炼一件本命血器(roster,'饮血'纹随燃血累积涨power);常驻护体DR+4"),
                            Passive("blood_artifact", "本命血器随燃血累积涨power(祭炼耗qixie与xuesha)"),
                        }),
                    new ArtDef("xue_xj_jiedan", "血煞结丹法", 3, "血祭",
                        new[]
                        {
                            CapQixie(50, "金丹境结'血煞丹':血气上限+50"),
                            // 结丹后燃血爆发档常驻抬升 +1（代价:天谴阈 {300,600,900} 各下调 50,更早招谴 → Note 留痕,A.0 不结算阈移）。
                            BurnStep(1, "结丹后burnStep基线+1(常驻抬燃血档;代价:血煞天谴阈各下调50,更早招谴)"),
                        }),
                    new ArtDef("xue_xj_guiyuan", "万血归元血阵", 4, "血祭",
                        new[] { new EffectOp(EffectOpKind.AddFlatDR, "bloodArray", 6, "布血阵化战场为血煞地:阵内己方燃血自燃伤减半、噬血回本+50%(阵法需血祭起手,一次性燃血+xuesha+5)") }),
                    new ArtDef("xue_xj_jitan", "血神祭坛·炼魂", 5, "血祭",
                        new[] { Passive("blood_god_avatar", "以活祭炼'血神化身'(roster,power=25+境界档,随主燃血成长);最强血祭产物但血煞增速翻倍(+4/次),失控则反噬主身(高危高回报顶点)") }),
                    new ArtDef("xue_xj_fanzu", "噬血血脉返祖印", 3, "血祭",
                        new[]
                        {
                            ThrBurn(2, "永久承血阈thrBurn+2(燃血更耐烧,对冲自燃)"),
                            Passive("atavism_blood", "觉醒上古血脉一档,blood-related增益对本人加成+10%"),
                        }),
                }),
                // bloodheart 血心道心类目（M1，补遗⑥「血心·止杀之念」）。A.0 仅装载不结算 → tier=0（sumArtPower 贡献 0）、
                // effects 留空（不触 daoHeart/innerDemon/comprehension 资源算子,那是 A.2 道心层的事）。具名 + power=0。
                new ArtCategoryDef("血心", "daoheart", 1, 1, new[]
                {
                    new ArtDef("dh_xue_zhixie", "止血养性诀", 0, "血心", System.Array.Empty<EffectOp>()),
                    new ArtDef("dh_xue_kongsha", "控煞凝心录", 0, "血心", System.Array.Empty<EffectOp>()),
                    new ArtDef("dh_xue_busha", "不杀化煞心经", 0, "血心", System.Array.Empty<EffectOp>()),
                    new ArtDef("dh_xue_xuefo", "血佛·放下屠刀心", 0, "血心", System.Array.Empty<EffectOp>()),
                }),
            };

            // —— 战技（深度设计「战技」节，OnUse 算子 + Cost 资源表；弹药=qixie 血气，门槛 brake 见选取规则⑤）。
            //    伤害/穿透等具体结算 Phase 3 接，A.0 以 AddPenInteger 近似整数占位破防量（量级对齐本路公式 dmg=Force×3+burn×4 等）
            //    + Cost 表达资源门槛；刹车类「锁血止煞·镇心」走 AddResource(xuesha,-15) 压煞（核心算子近似，不自创新算子）。——
            var skills = new[]
            {
                // 燃血狂屠：本路招牌爆发——本回合燃尽指定额度血气,伤害=武力×3+燃血量×4;燃血量≥30且血神觉醒则无视根骨。
                //   施后该额度血气清零、按溢出触发燃血自燃。门槛燃血≥20。
                new CombatSkillDef("sk_xue_kuangtu", "燃血狂屠", 5,
                    new[] { new EffectOp(EffectOpKind.AddPenInteger, null, 60, "武力×3+燃血量×4单点终极透支,燃血量≥30且血神觉醒无视根骨;施后该额度血气清零按溢出触自燃,xuesha+3") },
                    new Dictionary<string, int> { { "qixie", 20 } }),
                // 噬血反哺：近敌抽血一击,造武力伤害并按命中回血气+8、本场承血阈临时+2;击杀额外回满一档(续航回本核心)。
                new CombatSkillDef("sk_xue_fanbu", "噬血反哺", 3,
                    new[]
                    {
                        new EffectOp(EffectOpKind.AddPenInteger, null, 16, "近敌抽血一击,基于武力的伤害,本场承血阈临时+2,xuesha+1"),
                        new EffectOp(EffectOpKind.AddResource, "qixie", 8, "按命中回血气+8(击杀额外回满一档)"),
                    },
                    new Dictionary<string, int>()),
                // 血河漫卷：召本命血器/血河之力群攻全场,对每个邻近敌手武力×1+燃血量×1燃血穿透,敌越多总伤越高,命中失血/肉身系+5。
                new CombatSkillDef("sk_xue_manjuan", "血河漫卷", 4,
                    new[] { new EffectOp(EffectOpKind.AddPenInteger, null, 30, "对每邻近敌手武力×1+燃血量×1燃血穿透,敌越多总伤越高,命中失血/肉身系目标额外+5") },
                    new Dictionary<string, int> { { "qixie", 15 } }),
                // 血祭·焚身爆：孤注同归——一次性燃尽当前全部血气自爆式打击,范围伤=当前全部血气×2;放完血气归零、根骨永久-3、本场武力减半。
                new CombatSkillDef("sk_xue_fenshen", "血祭·焚身爆", 4,
                    new[] { new EffectOp(EffectOpKind.AddPenInteger, null, 80, "燃尽全部血气自爆,范围伤=当前全部血气×2(透支流终极赌命);放完血气归零、根骨永久-3、本场武力减半,xuesha+5") },
                    new Dictionary<string, int>()),
                // 锁血止煞·镇心：防御性刹车(强制对冲项)——本回合不可燃血,立即xuesha-15、daoHeart+3,可在天谴/走火触发前打断(对应选取规则brake强制)。
                //   xuesha-15 走 AddResource 压煞(核心算子近似);daoHeart+3 是道心轴(A.2)不在 A.0 落算子,仅 Note(放算子会崩,且 daoHeart 不在 Resources)。
                new CombatSkillDef("sk_xue_zhisha", "锁血止煞·镇心", 2,
                    new[] { new EffectOp(EffectOpKind.AddResource, "xuesha", -15, "本回合不可燃血,立即xuesha-15(止杀凝煞,可在血煞天谴/走火触发前打断);daoHeart+3 属A.2道心轴不在A.0落算子(Note留痕)") },
                    new Dictionary<string, int>()),
                // 噬尽夺元：对单一高威胁目标暗噬夺元,对目标Internal-6+Force-2并结死仇-25;专破他路高战力者本源/失血脆弱。
                new CombatSkillDef("sk_xue_duoyuan", "噬尽夺元", 4,
                    new[] { new EffectOp(EffectOpKind.AddPenInteger, null, 8, "暗噬夺元:对目标Internal-6+Force-2(经ApplyStat chokepoint)并造死仇负边-25,专破高战力者本源/失血脆弱,xuesha+3") },
                    new Dictionary<string, int> { { "qixie", 8 } }),
                // 血脉暴走：濒死血脉返祖——血气见底或濒死时强燃本源续命爆发,本回合所有燃血档视为满档、直伤+武力,但战后血气上限本场-30。
                new CombatSkillDef("sk_xue_baozou", "血脉暴走", 3,
                    new[] { new EffectOp(EffectOpKind.AddPenInteger, null, 24, "血气≤cap/4或濒死时强燃本源续命,本回合燃血档视为满档、直伤+武力(血神道搏命高方差);战后血气上限本场-30,xuesha+8") },
                    new Dictionary<string, int>()),
            };

            return new CultivationPathDef(
                "xue_xiu_xuesha", "血修·血煞道",
                "physical",
                // 属性/形态 tag（melee 近战搏命 / brute 燃血蛮力 / evil 邪面血煞），非对手 PathId（R2）。
                new[] { "melee", "brute", "evil" },
                resources,
                power,
                curve,
                arts,
                skills,
                new EntryGateDef("tag:xue_root"),
                new SelectionRuleDef(2, 3), // 战技抽 2~3 且强制含≥1 brake（深度设计选取规则⑤,brake=锁血止煞·镇心/献祭血傀）
                null);
        }
    }
}
