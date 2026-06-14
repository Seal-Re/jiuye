using System.Collections.Generic;

namespace Jianghu.Cultivation
{
    /// <summary>
    /// 魔修·夺养噬心道 <c>mo_xiu_xinmo</c>（西陲血河魔宫嫡传·魔道总纲）。数据照《余9路深度设计》魔修节 +
    /// 《内容补遗》第六部「魔修 mo_xiu_xinmo — 道心：噬心御魔（devilheart）」。
    /// physical 速成爆发：以杀伐/夺取/破戒「夺养」把他人修为直接转己爆发；魔功为弹药、心魔武器化、
    /// 凸·高爆发·方差最大（满电峰值逼近剑修，退化态地板极低）。被佛门渡魔/雷法纯阳横破（evil tag）。
    ///
    /// 红线落实：terms 无 ×0、无 daoHeart/innerDemon（R3/R6）；SituationalTags=属性/形态 tag（physical/melee/
    /// evil…，非对手 PathId，R2）；RealmCurve 四列等长（M4）；含 1 个 Role=daoheart 类目 devilheart（M1，
    /// A.0 仅装载不结算 → tier=0 使 sumArtPower 贡献 0、Effects 留空不触 moHeart/innerDemon 资源算子那是 A.2 的事）。
    /// canon pathId（R4）。纯整数，禁浮点。仅用 10 个核心 EffectOpKind。
    /// </summary>
    public static class MoXiuXinmoPath
    {
        public static readonly CultivationPathDef Def = Build();

        // —— 被动算子（装配期落 state，核心 EffectOpKind）。功法的「武力+N/悟性+N」等改四维项 A.0
        //    为 flavor 不落算子（生成期 Σ=80 不被功法污染），仅以 Note 留痕；能落 state 的「魔功上限+N」
        //    走 AddResourceCap、被动开关走 GrantPassive/SetFlag。MoGong/burnGate/darkKarma 才是本路在 Resources
        //    里的 key，故只对这些 AddResource/AddResourceCap；moHeart/innerDemon/comprehension 是三轴共用键、
        //    不在 Resources 字典（CultivationState 独立 init 字段，A.0 不读写 R3），故那些语义只走 GrantPassive+Note。——
        private static EffectOp CapMoGong(int amt, string note)
            => new EffectOp(EffectOpKind.AddResourceCap, "MoGong", amt, note);

        private static EffectOp Passive(string flag, string note)
            => new EffectOp(EffectOpKind.GrantPassive, flag, 1, note);

        private static CultivationPathDef Build()
        {
            // —— per-path 专属资源（不进四维 C6）——
            // MoGong 魔功值（魔元）：夺养式战力燃料，杀夺暴涨、每旬漏电；base cap=30（深度设计「cap=30+8×realm」
            //   的 realm0 基线，realm 增益由心法 AddResourceCap 表达，A.0 单值起底）；战力主项 + 招式弹药。
            // burnGate 燃心阀档[0,3]：主动赌 innerDemon 换爆发的本路签名旋钮（HeartBurn 武器化 innerDemon 的档位，
            //   ModKind 放大属 L1，A.0 仅装载档位资源不结算放大，见 Notes）。darkKarma 暗德污点[0,99]：杀夺累积，
            //   招佛雷天克/命修天谴强度阈（counterAdj 走独立 CounterMatrix，A.0 仅承载累积量）。
            // 注：innerDemon/moHeart/comprehension（三轴共用键）不在此声明 → 不进 Resources 字典、不被 AddResource 触碰（R3）。
            var resources = new[]
            {
                new ResourceDef("MoGong", 0, 30, 0),
                new ResourceDef("burnGate", 0, 3, 0),
                new ResourceDef("darkKarma", 0, 99, 0),
            };

            // —— 战力公式（深度设计 terms：武力×3 + 内力×2 + 悟性×2 + 根骨×1 + realm×5 + 魔功×4 + 功法power×1 + 魔兵×1）。
            //    res:MoGong 权重最高（夺养燃料直进战力主项）；根骨权重刻意低（扛反噬但远低于体修）。
            //    无 daoHeart/innerDemon、无 ×0（R3/R6）。HeartBurnMul/BerserkPenalty/fallenDevil postMul 是新 ModKind（L1），
            //    A.0 不引入：Modifiers 留空、PostMuls=null，魔功放大与暴走折损 A 后续接（见 Notes）；realm 乘性放大由 Curve 承载。——
            var power = new PowerFormulaDef(
                new[]
                {
                    new PowerTerm("stat:Force", 3, null),        // 武力主项：暴烈搏杀、掌刀魔功靠武力外放
                    new PowerTerm("stat:Internal", 2, null),     // 内力：催动魔功、御魔念之基
                    new PowerTerm("stat:Insight", 2, null),      // 悟性：御心魔也易走极端（双刃，决燃心容错/暴走概率）
                    new PowerTerm("stat:Constitution", 1, null), // 根骨：扛夺养反噬/燃血，权重低于体修远低
                    new PowerTerm("realm", 5, null),             // 境界线性底，主爆发倍率在 Curve 承载
                    new PowerTerm("res:MoGong", 4, null),        // 专属资源·权重最高：夺养燃料直进战力（HeartBurn 放大 L1 后接）
                    new PowerTerm("sumArtPower", 1, null),       // 所选四类功法 tier→power 之和
                    new PowerTerm("derived:demonWeapon", 1, null),// 炼魔器物所炼魔兵/魔婴/魔宝 power 之和（A.0 空注册返 0）
                },
                System.Array.Empty<PowerMod>(),
                null);

            // —— 战力曲线（深度设计 realmMul=[10,16,26,44,72,116,180,280]，realm 0..7，凸·极陡前段·方差最大）。
            //    四列等长（M4）：倍率 / UnifiedTierOf（UT0,2,4,6,8,9,10,11，化神段心魔劫强制处起跳）/
            //    境界名（炼气→筑基→金丹结魔丹→化神出魔念→炼虚→合体→渡劫冲魔劫→飞升魔尊证道）/ 升入阈值（夺养里程累进，realm0=0 起）。——
            var curve = new RealmCurveDef(
                new[] { 10, 16, 26, 44, 72, 116, 180, 280 },
                new[] { 0, 2, 4, 6, 8, 9, 10, 11 },
                new[] { "炼气", "筑基", "金丹", "化神", "炼虚", "合体", "渡劫", "飞升" },
                // 夺养里程 ≥100×当前realm 升阶 → 升入第 i 境累进阈值 = Σ 100×(0..i-1)（对齐剑修途径③同式）。
                new[] { 0, 100, 300, 600, 1000, 1500, 2100, 2800 });

            // —— 功法类目（噬命夺元/暴烈速成/炼魔器物 各 5 具名 + devilheart 魔心 5 具名）。
            //    具名/效果照深度设计「功法类目」节；命名与血河魔宫嫡传同源。——
            var arts = new[]
            {
                // 魔功心法·噬命夺元（主修夺养引擎，决 MoGong 掠夺转化率与 cap，定 sumArtPower 主体）。
                //   能落 state 的「魔功上限+N」走 CapMoGong；掠夺率/破戒涨魔功/高魔功暴走等改判走 GrantPassive+Note。
                new ArtCategoryDef("噬命夺元", "internal", 1, 1, new[]
                {
                    new ArtDef("mo_huaxue", "化血噬元神功", 2, "噬命夺元",
                        new[]
                        {
                            CapMoGong(10, "魔功上限+10(对齐 cap+10×realm 段,A.0 单值起底)"),
                            Passive("devour_boost", "击杀/重伤夺修为时 MoGong 掠夺量+50%(整除)"),
                        }),
                    new ArtDef("mo_qiqing", "七情噬心魔典", 3, "噬命夺元",
                        new[] { Passive("oathbreak_gain", "解锁'破戒涨魔功':屠戮无辜/背盟时 MoGong+8 之外 innerDemon+4(夺养越狠魔功心魔同涨)") }),
                    new ArtDef("mo_tianmo", "天魔解体大法", 4, "噬命夺元",
                        new[] { Passive("mogong_overload", "MoGong≥cap×7/10 时战力魔功分量额外×12/10;但每动作 innerDemon+2(高魔功必伴高心魔)") }),
                    new ArtDef("mo_zunjing", "魔尊噬天证道经", 5, "噬命夺元",
                        new[]
                        {
                            CapMoGong(30, "魔功上限+30(本路终极杠杆)"),
                            Passive("moheart_overload_guard", "moHeart≥60 燃心阀过载不暴走容错+1档(以魔证道);fallenDevil 态则改燃心阀解禁3但 moHeart 锁≤20(双变体分水岭)"),
                        }),
                    new ArtDef("mo_huashen", "化身天魔分魂法", 5, "噬命夺元",
                        new[] { Passive("demon_avatar_seed", "解锁'天魔附体/分身'前置,魔念附他体按宿主 realm 重算战力;代价 innerDemon+6、噬主风险转嫁") }),
                }),
                // 杀伐魔技·暴烈速成（魔功外放杀招，速成压制同阶正统的爆发主项）。power+N 改伤为 flavor(Note)。
                new ArtCategoryDef("暴烈速成", "attack", 1, 1, new[]
                {
                    new ArtDef("mo_xuehe", "血河噬煞掌", 1, "暴烈速成",
                        new[] { Passive("bloodlust_leech", "power+8;命中后吸目标受创量1/4转 MoGong(噬血续航·与血修联动接口)") }),
                    new ArtDef("mo_modao", "灭世魔刀决", 3, "暴烈速成",
                        new[] { Passive("slaughter_combo", "power+24;连击同一目标第2击起+5破防附伤;消耗 MoGong 越多本招 power 越高(燃料即伤害)") }),
                    new ArtDef("mo_tianmojin", "天魔大手印", 4, "暴烈速成",
                        new[] { Passive("aoe_suppress", "power+40;AOE 镇压式爆发,可一次性倾泻 MoGong 半槽换范围伤;对纯阳/正道命中则自身 innerDemon+5(伤正逆涨心魔)") }),
                    new ArtDef("mo_huantian", "焚天欢喜魔功", 3, "暴烈速成",
                        new[] { Passive("joy_devour", "power+20;双修夺元分支:对单体可夺其 Internal 转己 MoGong,需'七情灵物'gate 且 moHeart-4(以情入魔损道心)") }),
                    new ArtDef("mo_jienan", "浩劫魔焰·灭世", 5, "暴烈速成",
                        new[] { Passive("doom_blast", "power+60;大乘级,burnGate 拉满时可清空全槽魔功换毁灭一击;放后 MoGong 归零、innerDemon+10(空电量+心魔反扑)") }),
                }),
                // 炼魔器物·以活祭炼（炼魔兵/魔婴/魔宝造常驻战力，进 derived:demonWeapon）。养魔兵=养心魔(innerDemon↑)走 Note。
                new ArtCategoryDef("炼魔器物", "artifice", 1, 1, new[]
                {
                    new ArtDef("mo_xuebing", "血祭魔兵箓", 2, "炼魔器物",
                        new[] { Passive("forge_demon_soldier", "炼'魔兵'1件(power=12+境界档,常驻进 RosterIds→demonWeapon);每件每境 innerDemon+2(养魔兵如养心魔)") }),
                    new ArtDef("mo_moying", "噬魂魔婴养炼", 4, "炼魔器物",
                        new[] { Passive("forge_demon_infant", "孕'魔婴'本命魔兵(power=25+档,随主成长);最强但 innerDemon 增速翻倍,失控反噬夺舍主身(高危顶点)") }),
                    new ArtDef("mo_modao_qi", "炼血魔刀·噬主器", 3, "炼魔器物",
                        new[] { Passive("forge_devour_blade", "炼'噬主魔刀'本命魔宝:战力+18 但持有期 innerDemon 自然+1/旬(噬主增益式邪宝,魔剑母题)") }),
                    new ArtDef("mo_huoji", "万魂幡·活祭大阵", 4, "炼魔器物",
                        new[] { Passive("blood_array_field", "以活祭布阵化战场为血煞地:阵内己方 MoGong/动作+4、敌方若属纯阳/佛门 攻击-15;起手耗 MoGong10+暗德污点") }),
                    new ArtDef("mo_fenshen", "天魔分身·化身术", 5, "炼魔器物",
                        new[] { Passive("demon_clone", "以魔念凝分身(等效 power20,持续数动作);分身受创不伤本体但散后 innerDemon+4") }),
                }),
                // devilheart 魔心类目（M1，补遗第六部「噬心御魔」）。A.0 仅装载不结算 → tier=0（sumArtPower 贡献 0）、
                // Effects 留空（不触 moHeart/innerDemon/comprehension/tribResist 资源算子,那是 A.2 道心层的事）。具名 + power=0。
                // 双向开口(控魔洗白 dh_mo_zhengdao / 纵魔速成 dh_mo_modao_xin)语义留 Note,A.2 接。
                new ArtCategoryDef("魔心", "daoheart", 1, 1, new[]
                {
                    new ArtDef("dh_mo_kongmo", "御魔守一心诀", 0, "魔心", System.Array.Empty<EffectOp>()),
                    new ArtDef("dh_mo_renxin", "忍心炼魔录", 0, "魔心", System.Array.Empty<EffectOp>()),
                    new ArtDef("dh_mo_taishang", "太上忘情魔心经", 0, "魔心", System.Array.Empty<EffectOp>()),
                    new ArtDef("dh_mo_zhengdao", "以魔证道·借魔入道经", 0, "魔心", System.Array.Empty<EffectOp>()),
                    new ArtDef("dh_mo_modao_xin", "堕道魔心·我即是魔", 0, "魔心", System.Array.Empty<EffectOp>()),
                }),
            };

            // —— 战技（深度设计「战技池」节，OnUse 算子 + Cost 资源表；魔功值=MoGong）。
            //    伤害/夺养/暴走改判等具体结算 Phase 3 接，A.0 以 AddPenInteger 近似整数占位破防量 + Cost 表达资源门槛。
            //    强制风险对冲选取规则(必含 brake mo_sk_zhenmo)由 Selection/Tuning 留 Note,A 调度层接。——
            var skills = new[]
            {
                // 噬元夺脉[burst]：近敌夺修为,伤害基于 MoGong,击杀回 MoGong+8、夺目标1层增益;轻量夺养无暴走负担。消耗 MoGong3。
                new CombatSkillDef("mo_sk_duomai", "噬元夺脉", 1,
                    new[] { new EffectOp(EffectOpKind.AddPenInteger, null, 12, "近敌夺修为,伤害基于 MoGong;击杀回 MoGong+8、夺目标1层增益") },
                    new Dictionary<string, int> { { "MoGong", 3 } }),
                // 灭世魔刀·斩[burst]：蓄 MoGong 越满本招 power 越高的定向重斩,对资源型修士(丹/器/符/驭兽)额外+10。消耗 MoGong6。
                new CombatSkillDef("mo_sk_modao", "灭世魔刀·斩", 3,
                    new[] { new EffectOp(EffectOpKind.AddPenInteger, null, 36, "蓄 MoGong 越满 power 越高的定向重斩,对资源型修士额外+10(夺养克资源依赖者)") },
                    new Dictionary<string, int> { { "MoGong", 6 } }),
                // 血河倾泻[burst]：清空全部 MoGong 自爆式范围打击,伤害=MoGong全槽×2(燃心阀开则再乘放大),放后归零。消耗全槽 MoGong。
                new CombatSkillDef("mo_sk_xuehe", "血河倾泻", 3,
                    new[] { new EffectOp(EffectOpKind.AddPenInteger, null, 60, "清空全部 MoGong 自爆式范围打击,伤害=MoGong全槽×2(燃心阀开再乘放大),放后归零(空电量极高方差)") },
                    new Dictionary<string, int> { { "MoGong", 6 } }),
                // 燃心狂魔[burst]：主动把 burnGate 拉满3档持续3动作,魔功分量×(10+innerDemon×3/10)/10 暴涨;结束 innerDemon+12。
                //   赌命起手:无 MoGong 门槛,代价是 innerDemon 风险(A.0 仅以 AddResource(burnGate,3) 落档位占位,放大与心魔涨 L1/A.2 接)。
                new CombatSkillDef("mo_sk_ranxin", "燃心狂魔", 2,
                    new[] { new EffectOp(EffectOpKind.AddResource, "burnGate", 3, "把 burnGate 拉满3档(魔功分量×(10+innerDemon×3/10)/10 暴涨);结束 innerDemon+12(纵魔换爆发·本路标志赌命起手)") },
                    new Dictionary<string, int>()),
                // 噬心镇魔印[brake]：强制对冲技,立即 innerDemon-15、burnGate 归0、本回合不触暴走,暴走前打断;moHeart 越高镇得越稳。消耗 MoGong4。
                //   本路强制风险对冲选取规则锚点(对位鬼修镇魂);A.0 以 AddResource(burnGate,-3) 归档 + Note 表达刹车,innerDemon-15 走 Note(A.2 接)。
                new CombatSkillDef("mo_sk_zhenmo", "噬心镇魔印", 2,
                    new[] { new EffectOp(EffectOpKind.AddResource, "burnGate", -3, "强制对冲:burnGate 归0、本回合不触暴走(innerDemon-15 由 A.2 道心层接);moHeart 越高镇得越稳(纵魔流保命刹车)") },
                    new Dictionary<string, int> { { "MoGong", 4 } }),
                // 夺舍重生[escape]：魔念濒死强夺邻近肉身续命,成功保命且 innerDemon 清零洗暗德、按宿主 realm 重算;
                //   失败(场上有佛光/纯阳/雷)则魂飞魄散永久退场。消耗 MoGong15 + 全部魔念压上。
                new CombatSkillDef("mo_sk_duoshe", "夺舍重生", 5,
                    new[] { new EffectOp(EffectOpKind.AddPenInteger, null, 0, "魔念濒死强夺邻近肉身续命:成功保命且 innerDemon 清零洗暗德、按宿主 realm 重算;失败(场上佛光/纯阳/雷)则永久退场") },
                    new Dictionary<string, int> { { "MoGong", 15 } }),
                // 渡心魔劫·证道[control]：双变体收束技,moHeart≥60 主动引心魔劫自炼:过则 moHeart+10、innerDemon-20(借魔入道·洗白);
                //   fallenDevil 态则不可渡、强制 innerDemon+10 换永久 power+8(堕魔淬魔档)。消耗 MoGong10 + 一次心魔劫掷点。
                new CombatSkillDef("mo_sk_dujie", "渡心魔劫·证道", 4,
                    new[] { new EffectOp(EffectOpKind.AddPenInteger, null, 0, "双变体收束:moHeart≥60 引心魔劫自炼,过则 moHeart+10、innerDemon-20(洗白);fallenDevil 态则强制 innerDemon+10 换永久 power+8(堕魔淬魔,均 A.2 接)") },
                    new Dictionary<string, int> { { "MoGong", 10 } }),
            };

            return new CultivationPathDef(
                "mo_xiu_xinmo", "魔修·夺养噬心道",
                "physical",
                // 属性/形态 tag（physical 暴烈外放 / melee 近战掌刀 / evil 邪道被佛雷克 / high_burst 速成爆发 /
                //   devour_resource 夺养克资源依赖者），非对手 PathId（R2）。佛/雷/正统克制走独立 CounterMatrix，不在此列对手身份。
                new[] { "physical", "melee", "evil", "high_burst", "devour_resource" },
                resources,
                power,
                curve,
                arts,
                skills,
                new EntryGateDef("tag:mo_root"),
                new SelectionRuleDef(2, 3), // 战技抽 2~3（深度设计选取规则;强制含 brake mo_sk_zhenmo 由调度层接,见 Notes）
                null);
        }
    }
}
