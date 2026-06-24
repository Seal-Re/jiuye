using System.Collections.Generic;

namespace Jianghu.Cultivation.Paths
{
    /// <summary>
    /// 雷修·天劫雷法 <c>lei_xiu</c>（physical 纯阳破邪·渡劫绑定路）。数据照《每路深度设计》雷修节 +
    /// 《内容补遗》第十一部「11. 雷修 lei_xiu — 道心：纯阳无垢心」+ 命名池雷修条目。
    /// 高伤高自险的「开罐器」：内力为引、雷力为锋、realm 承雷量级、所选雷法 power 为外放主项；
    /// 根骨刻意权重 0（仅作承雷阈值 thr=6+realm×3 门槛与雷噬惩罚之源，与体修把根骨当主项相反）。
    /// 纯阳灭阴系数对阴邪目标整数放大、对纯阳/正道目标自损（破邪不利正）——以情境边 element 轴承载（见返回推荐边），
    /// 路线本体只声明属性 tag（thunder/spirit_attack/righteous），不硬编码具体对手（R2）。
    /// 曲线 front-mid-loaded 凸加速，元婴(index3)起引天劫不连续跃升。
    ///
    /// 红线落实：terms 无 ×0、无 daoHeart（R3/R6）；根骨权重为 0 的非战力正项设定用「不放 stat:Constitution term」
    /// 表达（放 ×0 会触 R6，且根骨惩罚=承雷阈值结算属 Phase 3 独立惩罚，A.0 仅 Note 留痕）。
    /// SituationalTags=属性/形态 tag 非对手 PathId（R2）；RealmCurve 四列等长（M4）；
    /// 含 1 个 Role=daoheart 类目 thunderheart（M1，A.0 仅装载不结算 → tier=0，effects 留空不触 daoHeart 资源算子）。
    /// canon pathId（R4）。纯整数，禁浮点。
    /// </summary>
    public static class LeiXiuPath
    {
        public static readonly CultivationPathDef Def = Build();

        // —— 被动算子（装配期落 state，核心 EffectOpKind）。功法的「内力+N/根骨+N/武力+N」等改四维项 A.0
        //    为 flavor 不落算子（生成期 Σ=80 不被功法污染，深度设计§选取规则「功法只加 power 不改 Σ」），
        //    仅以 Note 留痕；能落 state 的「雷力槽上限+N」走 AddResourceCap、被动开关/纹印走 GrantPassive/SetFlag。——
        private static EffectOp CapCharge(int amt, string note)
            => new EffectOp(EffectOpKind.AddResourceCap, "thunderCharge", amt, note);

        private static EffectOp Passive(string flag, string note)
            => new EffectOp(EffectOpKind.GrantPassive, flag, 1, note);

        private static CultivationPathDef Build()
        {
            // —— per-path 专属资源（不进四维 C6）——
            // thunderCharge 雷力槽：路线专属充能资源，平时内力×realm 折算缓慢蓄满、受雷/承劫瞬充；战技以雷力为开销，
            //   可超内力上限独立结算（深度设计特色机制①）。base cap=30 起底，realm/雷纹/心法增益走 AddResourceCap。
            // leiwen 雷纹条数 0..6：刻于身的符印承载条数（悟性决定可同时承载，深度设计特色机制；A.0 单值起底）。
            var resources = new[]
            {
                new ResourceDef("thunderCharge", 0, 30, 0),
                new ResourceDef("leiwen", 0, 6, 1),
            };

            // —— 战力公式（深度设计 terms：内力×2 + 雷力×3 + realm×4 + 所选雷法power×3 + 悟性×1）。
            //    内力为蓄雷之源、雷力为外放之锋（纯阳灭阴系数只放大此分量，Phase 3 经情境/克制结算）；
            //    realm 承雷量级最重乘数（凸曲线由 RealmCurve 承载）；根骨深度设计记权重 0（非战力正项）——
            //    A.0 以「不放 stat:Constitution term」表达（放 stat:Constitution×0 触 R6；承雷阈值 thr=6+realm×3
            //    的门槛与雷噬自伤+武力衰减属 Phase 3 独立惩罚结算，本路本体只声明、Note 留痕）。无 daoHeart、无 ×0（R3/R6）。——
            var power = new PowerFormulaDef(
                new[]
                {
                    new PowerTerm("stat:Internal", 2, null),    // 内力：蓄雷之源，决定雷力槽基础充能速率与上限基数
                    new PowerTerm("res:thunderCharge", 3, null),// 雷力槽：路线专属资源，独立结算/战技开销/灭阴系数只放大此分量
                    new PowerTerm("realm", 4, null),            // 境界：可承雷量级，本路最重乘数(渡雷劫即跃升,见 Curve 凸加速)
                    new PowerTerm("sumArtPower", 3, null),      // 所选雷法/雷纹/承雷心法/引雷身法各功法 tier 之和(外放主项)
                    new PowerTerm("stat:Insight", 1, null),     // 悟性：领悟广度(可承雷纹条数/三十六雷解锁),微调项
                    // 根骨刻意不入 terms：深度设计「根骨权重记为 0、仅作门槛与惩罚」。放 stat:Constitution×0 违 R6,
                    // 故省去该项；承雷阈值/雷噬惩罚 Phase 3 接(Note 留痕)，与体修把根骨当主项恰相反。
                },
                System.Array.Empty<PowerMod>(),
                null);

            // —— 战力曲线（深度设计 realmMul=[8,14,22,40,70,110,160,230,340]，realm 0..8，front-mid-loaded 凸加速，
            //    元婴 index3 起引天劫不连续跃升、大乘 index7→8 渡劫·肉身重铸最大跳）。四列等长（M4）：
            //    倍率 / UnifiedTierOf（UT0-12 映射：三流→二流→一流→绝顶→大宗师→…→手中有雷）/
            //    境界名（炼气→筑基→金丹→元婴→化神→炼虚→合体→大乘→雷帝真身）/ 升入阈值（承雷渡劫里程累进，realm0=0 起）。——
            var curve = new RealmCurveDef(
                new[] { 10, 17, 30, 54, 95, 150, 217, 311, 461 }, // INV-CROSS v2: buff +10~12% UT4+ (module-weak flag); UT8=2.07x sword (was 1.87x)
                new[] { 0, 2, 4, 6, 8, 9, 10, 11, 12 },
                new[] { "炼气", "筑基", "金丹", "元婴", "化神", "炼虚", "合体", "大乘", "雷帝真身" },
                // 承雷渡劫里程 ≥110×当前realm 升阶（元婴起须主动引天劫淬体，前低后高累进）→ 升入第 i 境累进阈值 = Σ 110×(0..i-1)。
                new[] { 0, 110, 330, 660, 1100, 1650, 2310, 3080, 3960 },
                // —— A.1 境界稿 §2：起步 SubLevelCount 全 1；CanAscend=true；MaxMajor=大境界数-1。——
                new[] { 1, 1, 1, 1, 1, 1, 1, 1, 1 }, true, 8);

            // —— 功法类目（雷法/雷纹/承雷心法/引雷身法 各 5 具名 + thunderheart 道心 4 具名）。
            //    具名/效果照深度设计「功法类目」节 + 命名池雷修条目同源。——
            var arts = new[]
            {
                // 雷法（外放杀伐·辟邪诛魔）·主输出与破邪，定 sumArtPower 主体。对阴邪享纯阳灭阴×3(Phase 3 经情境结算)。
                //   伤害/灭阴系数/破邪附伤为 flavor（Note，A.0 不落四维算子）。
                new ArtCategoryDef("雷法", "attack", 1, 1, new[]
                {
                    new ArtDef("le_ff_zhangxin", "五雷正法·掌心雷", 1, "雷法",
                        new[] { Passive("anti_evil_strike", "power+6;掌心引雷,命中阴邪标签该power享纯阳灭阴系数(×3结算)") }),
                    new ArtDef("le_ff_jingzhe", "惊蛰十二变", 2, "雷法",
                        new[] { Passive("thunder_spell", "power+14;雷力开销-2/技,连击同目标第2击起附+3破邪附伤") }),
                    new ArtDef("le_ff_wufu", "五府雷印", 3, "雷法",
                        new[] { Passive("anti_evil_strike", "power+24;同时印刻2道雷纹不增内力负担;对魔功/养魂/煞气类额外+8克制伤") }),
                    new ArtDef("le_ff_bixie", "辟邪神雷", 4, "雷法",
                        new[] { Passive("purge_evil", "power+40;命中即驱目标1层增益/禁制/护体阴煞,对ghost/corpse/demon伤害再×2(与灭阴叠乘)") }),
                    new ArtDef("le_ff_mieshi", "雷之古经·灭世雷光", 5, "雷法",
                        new[] { Passive("thunder_spell", "power+64;大乘级外放,雷力可一次性倾泻全槽换范围轰击;对纯阳/正道目标power减半且自损雷力(破邪不利正)") }),
                }),
                // 雷纹（被动符印·铭刻于身）·被动增幅与承雷上限，定雷力槽容量/灭阴稳定性/承劫减伤。雷力上限增益落 AddResourceCap。
                new ArtCategoryDef("雷纹", "internal", 1, 1, new[]
                {
                    new ArtDef("le_wf_dihuo", "地火社雷纹", 1, "雷纹",
                        new[] { CapCharge(10, "雷力上限+10;蓄雷速率+1/步;破除禁制/结界/阵纹时+5") }),
                    new ArtDef("le_wf_bishui", "避水御雷纹", 2, "雷纹",
                        new[] { Passive("ward_thunderbite", "渡劫/承雷时受到的雷噬自伤固定-3;对水雷/阴雷反伤免疫") }),
                    new ArtDef("le_wf_chunyang", "纯阳灭阴纹", 3, "雷纹",
                        new[] { Passive("anti_evil_lock", "纯阳灭阴系数下限锁定×3不被阴邪环境削弱;身处煞气/尸气场域不掉雷力") }),
                    new ArtDef("le_wf_sanshiliu", "三十六雷·正雷纹", 4, "雷纹",
                        new[]
                        {
                            CapCharge(30, "雷力上限+30"),
                            Passive("zheng_lei", "解锁正雷序列,外放雷法power统一+10;每多悟性5可多承1条雷纹"),
                        }),
                    new ArtDef("le_wf_tianjie", "天劫本源纹", 5, "雷纹",
                        new[]
                        {
                            CapCharge(50, "雷力上限+50"),
                            Passive("jie_archive", "借天地法则:大乘后每渡一次劫永久power+12(淬法存档式累计)"),
                        }),
                }),
                // 承雷心法（淬体淬法·自险根基）·把以身承雷转净增益的根基，抬承雷阈值容错/把雷劫伤转属性淬炼。
                //   阈值容错 thr-N/雷甲反伤/淬体增益为 flavor（Note）；雷力满充临时四维增益不落算子(A.0 不污染 Σ)。
                new ArtCategoryDef("承雷心法", "movement", 1, 1, new[]
                {
                    new ArtDef("le_xf_cuigu", "引雷淬骨诀", 1, "承雷心法",
                        new[] { Passive("thr_tolerance", "承雷阈值thr等效-2(更易达线);每渡一次小雷劫根骨永久+1(上限内)") }),
                    new ArtDef("le_xf_lianmai", "雷火炼脉功", 2, "承雷心法",
                        new[] { Passive("refine_by_thunder", "雷劫淬体时把一半(整数取半)雷噬伤转内力永久+1~+3;雷力满充时武力临时+4") }),
                    new ArtDef("le_xf_buhuai", "纯阳不坏体", 3, "承雷心法",
                        new[] { Passive("thunder_armor", "根骨≥thr时免疫雷噬武力衰减;体表常驻雷甲,被阴邪近身反弹固定6点纯阳伤") }),
                    new ArtDef("le_xf_tunlei", "渡劫吞雷功", 4, "承雷心法",
                        new[]
                        {
                            new EffectOp(EffectOpKind.AddResource, "thunderCharge", 30, "主动引天劫可吞一道劫雷直接充满雷力槽并realm进度+1"),
                            Passive("high_risk_core", "失败(根骨<thr)则雷噬翻倍——高自险核心"),
                        }),
                    new ArtDef("le_xf_chongzhu", "肉身重铸·雷帝真身", 5, "承雷心法",
                        new[]
                        {
                            CapCharge(40, "大乘级肉身重铸借天地法则:雷力上限专属溢出+40(仅本心法持有者)"),
                            Passive("leidi_body", "根骨/内力上限突破常规cap各+10(路线专属溢出);雷力外放不再自损纯阳目标"),
                        }),
                }),
                // 引雷身法（机动·借雷遁走）·以雷为遁定先手/拉距/规避,与雷力联动(耗雷换瞬移)。
                //   先手/瞬移判定为 flavor（Note）;能落 state 的开战雷力起爆走 AddResource。
                new ArtCategoryDef("引雷身法", "swordwill", 1, 1, new[]
                {
                    new ArtDef("le_bf_yufeng", "御风诀", 1, "引雷身法",
                        new[] { Passive("evade", "基础御风:移动优先级+1;脱离近战缠斗成功率提升") }),
                    new ArtDef("le_bf_fenglei", "风雷翅", 2, "引雷身法",
                        new[] { Passive("blink_thunder", "背生风雷双翅:先手判定+6;可耗雷力5瞬移到相邻节点/拉开距离") }),
                    new ArtDef("le_bf_leidun", "五行遁·雷遁", 3, "引雷身法",
                        new[] { Passive("thunder_dash", "雷遁千里:逃脱/追击时移动量翻倍;切入瞬间下一记雷法power+10") }),
                    new ArtDef("le_bf_suodi", "缩地神雷步", 4, "引雷身法",
                        new[] { Passive("evade", "短距闪现叠纯阳尾焰:闪现落点对阴邪留3点持续纯阳灼伤;规避物理招式爆发命中") }),
                    new ArtDef("le_bf_huashen", "雷光化身", 5, "引雷身法",
                        new[]
                        {
                            new EffectOp(EffectOpKind.AddResource, "thunderCharge", 5, "化身雷光:开战首回合雷力+5(先声夺人,接灭世雷光收益)"),
                            Passive("lightning_body", "耗满雷力可无视距离突进,本回合不可被物理拦截"),
                        }),
                }),
                // thunderheart 道心类目（M1，补遗第十一部「纯阳无垢心」thunderheart）。A.0 仅装载不结算 → tier=0
                // （sumArtPower 贡献 0）、effects 留空（不触 daoHeart/innerDemon/comprehension 资源算子,那是 A.2 道心层的事）。具名 + power=0。
                new ArtCategoryDef("雷心", "daoheart", 1, 1, new[]
                {
                    new ArtDef("dh_le_chengshen", "承雷养性诀", 0, "雷心", System.Array.Empty<EffectOp>()),
                    new ArtDef("dh_le_chunyang", "纯阳无垢心录", 0, "雷心", System.Array.Empty<EffectOp>()),
                    new ArtDef("dh_le_buwei", "承雷不馁心经", 0, "雷心", System.Array.Empty<EffectOp>()),
                    new ArtDef("dh_le_leidi", "雷帝真身道心", 0, "雷心", System.Array.Empty<EffectOp>()),
                }),
            };

            // —— 战技（深度设计「战技」节，OnUse 算子 + Cost 资源表；弹药=thunderCharge 雷力槽）。
            //    伤害/破邪/纯阳灭阴/雷噬自损等具体结算 Phase 3 接，A.0 以 AddPenInteger 占位破防量（量级对齐本路公式）
            //    + Cost 表达雷力门槛；自险项(引天劫/舍身雷爆)的根骨承雷阈值结算 Phase 3 独立惩罚，A.0 仅 Note 留痕。——
            var skills = new[]
            {
                // 普化天雷·诛邪：九天应元普化天雷降世,对全场阴邪AOE净化+重创(叠灭阴×3+辟邪驱除);对纯阳/正道几乎无伤。雷力40,realm≥5。
                // B5批2: → AddPenInteger(64基线)+CounterMul(evil,3) 灭阴×3(对阴邪联合上界倍乘,正道几乎无伤=不乘)。
                new CombatSkillDef("sk_le_puhua", "普化天雷·诛邪", 5,
                    new[]
                    {
                        Modules.FlatPen(64, "九天应元普化天雷AOE基线"),
                        Modules.CounterMul("evil", 3, note: "对tag:evil(阴邪)叠灭阴×3(联合上界);对纯阳/正道不乘(破邪不利正)"),
                    },
                    new Dictionary<string, int> { { "thunderCharge", 40 } }),
                // 引天劫：主动招天劫雷劈战场,对阴邪范围爆发(power×灭阴系数);自身按max(0,thr-根骨)×3承雷噬。根骨够=纯增益,不够=高自损。雷力30。
                // B5批2: → AddPenInteger(40基线)+Backlash(承雷自伤) 高伤高自险(自伤通道批4 selfDmg 接,本轮ApplyOnUse不改入伤)。
                new CombatSkillDef("sk_le_yintianjie", "引天劫", 3,
                    new[]
                    {
                        Modules.FlatPen(40, "招天劫对阴邪范围爆发power×纯阳灭阴系数(基线)"),
                        Modules.Backlash("thunderRecoil", 0, "自身按max(0,thr-根骨)×3承雷噬自伤+武力-2/级差(高伤高自险,自伤量Phase3按根骨阈结算)"),
                    },
                    new Dictionary<string, int> { { "thunderCharge", 30 } }),
                // 辟邪斩魂：纯阳雷刃斩魂体/幻身,对illusion_soul/ghost真伤无视绕物防,破魂力绕物防类阴防。雷力25+内力5。
                new CombatSkillDef("sk_le_zhanhun", "辟邪斩魂", 4,
                    new[] { Modules.FlatPen(32, "纯阳雷刃对illusion_soul/ghost真伤,无视绕物防,破魂力绕物防这类阴防机制(真伤定值;无视绕物防 Phase3)") },
                    new Dictionary<string, int> { { "thunderCharge", 25 } }),
                // 舍身雷爆：雷力槽连同部分内力一次性引爆,超高范围爆发(全槽雷力×2折算power);自身根骨永久-3、本场武力减半。雷力全槽30+内力20+根骨永久-3。
                // B5扫尾: 占位 AddPenInteger(60) → Modules.PenFromResource(thunderCharge,×2)（全槽雷力×2折算,雷力越满越痛、见底哑火真差分；
                //   根骨永久-3/武力减半自损走 Phase3/批4 selfDmg；Amount2=1 工厂保证 §15.6）。
                new CombatSkillDef("sk_le_sheshen", "舍身雷爆", 4,
                    new[] { Modules.PenFromResource("thunderCharge", 2, note:"全槽雷力×2折算超高范围爆发,自身根骨永久-3、本场武力减半(自险流终极赌命技,自损 Phase3)") },
                    new Dictionary<string, int> { { "thunderCharge", 30 } }),
                // 雷狱困魔阵：社雷布纯阳雷狱困范围阴邪,被困者每步受固定纯阳灼伤,无法遁走/借煞气回复。雷力18+1张雷纹符胆(leiwen)。
                new CombatSkillDef("sk_le_leiyu", "雷狱困魔阵", 3,
                    new[] { Modules.FlatPen(18, "纯阳雷狱困范围阴邪,被困者每步受固定纯阳灼伤,且无法遁走/借煞气回复(困场+逐步灼伤 Phase3)") },
                    new Dictionary<string, int> { { "thunderCharge", 18 }, { "leiwen", 1 } }),
                // 五雷轰顶：五道连环天雷集火单体,对ghost/corpse/demon直接打断养魂/炼尸/施法并清1层护体阴煞。雷力20。
                new CombatSkillDef("sk_le_hongding", "五雷轰顶", 2,
                    new[] { Modules.FlatPen(22, "五道连环天雷集火单体,对ghost/corpse/demon打断养魂/炼尸/施法并清1层护体阴煞(打断/净化 Phase3)") },
                    new Dictionary<string, int> { { "thunderCharge", 20 } }),
                // 惊雷破障：定向惊雷专破禁制/结界/护体罡气/阵纹,对非阴邪目标伤害平平但拆防/破阵判定+15。雷力12。
                new CombatSkillDef("sk_le_pozhang", "惊雷破障", 2,
                    new[] { Modules.FlatPen(12, "定向惊雷专破禁制/结界/护体罡气/阵纹,拆防/破阵判定+15(对非阴邪目标伤害平平;拆防 Phase3)") },
                    new Dictionary<string, int> { { "thunderCharge", 12 } }),
                // 蓄雷·引气：主动引地火社雷温养筋脉,立即+8雷力、雷纹载位临时+1。无雷力门槛(雷力见底的起爆引信)。
                new CombatSkillDef("sk_le_xulei", "蓄雷·引气", 1,
                    new[]
                    {
                        new EffectOp(EffectOpKind.AddResource, "thunderCharge", 8, "引地火社雷立即+8雷力(雷力见底的起爆引信)"),
                        new EffectOp(EffectOpKind.AddResource, "leiwen", 1, "临时多承1道雷纹载位"),
                    },
                    new Dictionary<string, int>()),
                // 雷遁·闪：雷遁闪避（OnDefend）。需承雷心法→门控。thunderCharge≥5,消耗5。
                // B5扩21: Evade — 雷修雷遁闪避,Amount=20→20%减免(侧重进攻非闪避专精)。
                new CombatSkillDef("sk_le_leidun", "雷遁·闪", 2,
                    new[] { Modules.Evade(20, "雷遁闪避:20%来袭减免(需承雷心法→门控)") },
                    new Dictionary<string, int> { { "thunderCharge", 5 } }),
            };

            return new CultivationPathDef(
                "lei_xiu", "雷修·天劫雷法",
                "physical",
                // 属性/形态 tag（thunder 雷属性 / spirit_attack 纯阳破邪外放走精神攻击轴破阴防 / righteous 正道），非对手 PathId（R2）。
                new[] { "thunder", "spirit_attack", "righteous" },
                resources,
                power,
                curve,
                arts,
                skills,
                // 21 路唯一 entry tag 约定：每路 entry tag = 唯一 <pathkey>_root（lei_root）。派生池 RootTagPool() 随之含 lei_root → 雷修可被定路。
                new EntryGateDef("tag:lei_root"),
                new SelectionRuleDef(2, 3), // 战技抽 2~3（深度设计选取规则②，建议≥1破邪类）
                null);
        }
    }
}
