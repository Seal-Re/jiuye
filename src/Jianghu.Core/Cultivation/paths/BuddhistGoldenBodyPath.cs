using System.Collections.Generic;

namespace Jianghu.Cultivation.Paths
{
    /// <summary>
    /// 佛修·金身道 <c>buddhist_golden_body</c>（fate 佛门金身/禅愿·邪修天克节点）。数据照《每路深度设计》佛修节 +
    /// 《内容补遗》第 10 节「佛修 buddhist_golden_body — 道心：菩提无生心」chanheart 类目 + 命名池佛修条目。
    /// 厚积晚发·physical 防御净化·低反噬：根骨为基、愿力驱动「佛光克邪」、金身层叠 flat DR 且受击转愿、
    /// 悟性小幅（主作突破而非直伤）、武力权重最低（佛修不尚搏杀）。对【阴邪】目标 anti_evil 放大（克鬼/魔/血/一切阴邪）。
    ///
    /// 红线落实：terms 无 ×0、无 daoHeart（R3/R6）；SituationalTags=属性/形态 tag 非对手 PathId（R2）——
    /// anti_evil 是「克阴邪」的属性/克制 tag（喂入克制矩阵 element 轴），非任何 pathId；
    /// RealmCurve 四列等长（M4）；含 1 个 Role=daoheart 类目 佛心（M1，A.0 仅装载不结算 → tier=0
    /// 使 sumArtPower 贡献 0、effects 留空不触 daoHeart/innerDemon 资源算子，那是 A.2 道心层的事）。
    /// canon pathId（R4）。纯整数，禁浮点。
    /// </summary>
    public static class BuddhistGoldenBodyPath
    {
        public static readonly CultivationPathDef Def = Build();

        // —— 被动算子（装配期落 state，核心 EffectOpKind）。功法的「根骨+N/根骨成长+档」等改四维项 A.0
        //    为 flavor 不落算子（生成期 Σ=80 不被功法污染，深度设计「功法只加 power 不改 Σ」），仅以 Note 留痕；
        //    能落 state 的「愿力上限+N」走 AddResourceCap、「goldenLayers 上限+N」走 AddResourceCap、
        //    「即时+功德/+愿力」走 AddResource、被动开关（anti_evil 上限+1 / 每层 DR / 受击转愿）走 GrantPassive/SetFlag。——
        private static EffectOp CapVow(int amt, string note)
            => new EffectOp(EffectOpKind.AddResourceCap, "vow", amt, note);

        private static EffectOp CapGoldenLayers(int amt, string note)
            => new EffectOp(EffectOpKind.AddResourceCap, "goldenLayers", amt, note);

        private static EffectOp Passive(string flag, string note)
            => new EffectOp(EffectOpKind.GrantPassive, flag, 1, note);

        private static CultivationPathDef Build()
        {
            // —— per-path 专属资源（不进四维 C6）——
            // vow 愿力：禅定/渡化所积，唯一驱动「佛光克邪」的资源（对阴邪按 anti_evil 倍乘后右移定标，Phase 3 接）。
            //   base cap=500（深度设计「般若波罗蜜心经 +500」起底，更高上限由佛法心法 AddResourceCap 表达）。
            //   生成期初始=100×悟性（深度设计选取规则），A.0 单值起底 init=0，realm/选取增益另结算。
            // merit 功德：只能由渡化/降魔/护生获得，杀戮破戒清零愿力并冻结功德——突破硬门槛（merit≥realm×1000）。0..9999。
            // goldenLayers 金身层：炼体所得金刚不坏叠层 0..9，每层 flat DR=8×层 且受击 1/4 回灌为愿力（受击→转愿正反馈）。
            var resources = new[]
            {
                new ResourceDef("vow", 0, 500, 0),
                new ResourceDef("merit", 0, 9999, 0),
                new ResourceDef("goldenLayers", 0, 9, 0),
            };

            // —— 战力公式（深度设计 terms：根骨×4 + 愿力×3 + realm×18 + goldenLayers×25 + 悟性×2 + 功法power×1 + 武力×1 + 内力×1）。
            //    根骨权重最高（金身之基，肉身承愿）、武力/内力刻意最低（佛修不尚搏杀，内力仅供般若/佛光开销）；
            //    愿力×3=本路独有克邪项（式中按 3×愿力/10 计入，对阴邪目标再乘(anti_evil-1)放大——放大走克制矩阵/情境边 Phase 3 接，
            //    A.0 不落 Modifier，Note 留痕）；goldenLayers×25 直接加战力并另给 flat DR=8×层 与受击转愿（被动开关由金身功法落 Flags）；
            //    realm×18 为线性底，乘性放大另见 RealmCurve。无 daoHeart、无 ×0（R3/R6）。
            //    愿力项挂 WeightStepKey="vowStep"：般若 t4「无量寿般若藏」愿力≥3000 时 AddTermWeightStep(vowStep,+1) 抬其权重台阶。——
            var power = new PowerFormulaDef(
                new[]
                {
                    new PowerTerm("stat:Constitution", 4, null),    // 根骨为基：金身之骨、肉身承愿，全路最高
                    new PowerTerm("res:vow", 3, "vowStep"),         // 愿力：路线专属克邪资源(对阴邪再乘 anti_evil 放大,Phase 3);可被 vowStep 抬阶
                    new PowerTerm("realm", 18, null),               // 境界：线性底，乘性放大另见 RealmCurve（厚积晚发凸尾）
                    new PowerTerm("res:goldenLayers", 25, null),    // 金身层数：直接加战力并另给 flat DR=8×层 与受击转愿
                    new PowerTerm("stat:Insight", 2, null),         // 悟性：小幅；主作突破心魔劫与禅定效率，不堆直伤
                    new PowerTerm("sumArtPower", 1, null),          // 所选佛法/金身/禅定/愿行各功法 tier 之和
                    new PowerTerm("stat:Force", 1, null),           // 武力：权重最低；佛修降魔渡魔，不以搏杀为本
                    new PowerTerm("stat:Internal", 1, null),        // 内力：辅，供般若/佛光招式开销
                },
                System.Array.Empty<PowerMod>(),
                null);

            // —— 战力曲线（深度设计 realmMul=[8,11,15,21,30,44,66,100,150,225]，realm 0..9，厚积晚发凸尾·几何级翘尾）。
            //    四列等长（M4）：倍率 / UnifiedTierOf（UT0-12 映射：三流→…→大宗师/金身大成不坏之体）/
            //    境界名（炼气→筑基→金丹→元婴→化神→炼虚→合体→大乘→金身→不坏金身）/
            //    升入阈值（功德为突破货币 merit≥realm×1000，早期积累慢故升境最慢、晚期渡化滚雪球反向加速；realm0=0 起）。——
            var curve = new RealmCurveDef(
                new[] { 11, 15, 20, 28, 40, 59, 88, 133, 200, 299 }, // INV-CROSS二轮: ×1.33→target 0.85×剑修
                // A1.2 顶段 plateau（境界稿 §11.3）：fi8/fi9 都 →UT12（金身/不坏金身共 UT12，mul 150→225），
                //   合并为「金身」一个大境界（major8）含两小境界；UnifiedTierOf 保留 dup12 不变。
                new[] { 0, 2, 4, 6, 8, 9, 10, 11, 12, 12 },
                new[] { "炼气", "筑基", "金丹", "元婴", "化神", "炼虚", "合体", "大乘", "金身", "不坏金身" },
                // 功德达标为突破货币（merit≥realm×1000）：升入第 i 境累进阈值 = Σ 1000×(0..i-1)（深度设计途径③）。
                new[] { 0, 1000, 3000, 6000, 10000, 15000, 21000, 28000, 36000, 45000 },
                // —— A.1：SubLevelCount = 同 UT 段长（顶 UT12 段 2 = 金身/不坏金身 → 大境界数 9）；
                //    CanAscend=true；MaxMajor=大境界数-1=8（plateau 合并后段数减 1）。——
                new[] { 1, 1, 1, 1, 1, 1, 1, 1, 2 }, true, 8);

            // —— 功法类目（佛法/金身/禅定/愿行 各 5 具名 + 佛心 道心 4 具名）。
            //    具名/效果照深度设计「功法类目」节；命名与命名池佛修条目同源；道心类目命名照补遗第 10 节 chanheart 条目。——
            var arts = new[]
            {
                // 佛法(般若)：心法内功——增益愿力上限/愿力转化率与佛光威能，决定「克邪」的资源天花板。
                //   愿力上限增益落 AddResourceCap(vow)；anti_evil 上限/转化率/掷点等开关落 GrantPassive（喂入克制矩阵）。
                new ArtCategoryDef("佛法", "internal", 1, 1, new[]
                {
                    new ArtDef("bd_ff_boruo", "般若波罗蜜心经", 1, "佛法",
                        new[]
                        {
                            CapVow(500, "愿力上限+500"),
                            Passive("chanding_vow_up", "禅定涨愿+1/次"),
                        }),
                    new ArtDef("bd_ff_jingang", "金刚般若功", 2, "佛法",
                        new[]
                        {
                            CapVow(1200, "愿力上限+1200"),
                            Passive("anti_evil_cap_up1", "对阴邪 anti_evil 上限+1(封顶项,喂入克制矩阵)"),
                        }),
                    new ArtDef("bd_ff_dari", "大日如来真经", 3, "佛法",
                        new[]
                        {
                            CapVow(2500, "愿力上限+2500"),
                            Passive("foguang_cost_down", "佛光招式内力开销-30%(整除);白昼/阳光节点愿力转化+20%"),
                        }),
                    new ArtDef("bd_ff_wuliangshou", "无量寿般若藏", 4, "佛法",
                        new[]
                        {
                            CapVow(4500, "愿力上限+4500"),
                            Passive("salvation_merit_up", "每渡化1名阴邪额外+200功德"),
                            // 愿力≥3000 时战力额外+15%：核心算子 AddTermWeightStep 抬 res:vow 项权重台阶（vowStep）近似该乘性增益。
                            new EffectOp(EffectOpKind.AddTermWeightStep, "vowStep", 1, "愿力≥3000时res:vow项权重+1阶(近似战力额外+15%)"),
                        }),
                    new ArtDef("bd_ff_fanchang", "诸天梵唱大光明经", 5, "佛法",
                        new[]
                        {
                            CapVow(9999, "愿力上限+9999"),
                            Passive("foguang_aoe_all_evil", "佛光AOE触及全场阴邪;突破心魔劫掷点+30"),
                        }),
                }),
                // 金身(炼体)：外功横练——把根骨/功德铸成金刚不坏，提供 flat DR 与受击转愿，本路耐久与反制底盘。
                //   goldenLayers 上限增益落 AddResourceCap(goldenLayers)；每层 DR/受击转愿率/不死等开关落 GrantPassive。
                //   「根骨成长+档」改四维项 A.0 为 flavor（Note 留痕，不落算子，Σ 不被污染）。
                new ArtCategoryDef("金身", "physical", 1, 1, new[]
                {
                    new ArtDef("bd_js_yijin", "易筋经", 1, "金身",
                        new[]
                        {
                            CapGoldenLayers(3, "根骨成长+1档;可叠 goldenLayers 至3层"),
                            Passive("golden_dr8", "每层 flat DR+8"),
                        }),
                    new ArtDef("bd_js_jinzhong", "金钟罩铁布衫", 2, "金身",
                        new[]
                        {
                            CapGoldenLayers(1, "goldenLayers 上限+1(至4)"),
                            Passive("reflect_vow_third", "受击转愿 1/4→1/3(整除)"),
                        }),
                    new ArtDef("bd_js_luohan", "罗汉金身诀", 3, "金身",
                        new[]
                        {
                            CapGoldenLayers(2, "goldenLayers 上限至6"),
                            Passive("golden_dr11_immune_bloodpoison", "每层DR 8→11;免疫'血/毒'类减益(克阴邪向)"),
                        }),
                    new ArtDef("bd_js_bukuai", "金刚不坏神功", 4, "金身",
                        new[]
                        {
                            CapGoldenLayers(2, "goldenLayers 上限至8"),
                            Passive("golden_dr15_undying_once", "每层DR 11→15;受致命伤时1次/场不死(残1)且爆愿+1000"),
                        }),
                    new ArtDef("bd_js_budongming", "不动明王琉璃体", 5, "金身",
                        new[]
                        {
                            CapGoldenLayers(1, "goldenLayers 满9"),
                            Passive("golden_dr20_evil_half", "DR 15→20/层;阴邪攻击对本体伤害再减50%(整除),'金身大成'"),
                        }),
                }),
                // 禅定(愿心)：修炼/状态术——提升积愿与功德效率、抗心魔、稳定收束，决定升境速度。
                //   涨愿/抗心魔/功德效率等多为状态开关落 GrantPassive（Phase 3 结算），A.0 不落资源算子（避免无端涨 vow/merit）。
                new ArtCategoryDef("禅定", "support", 1, 1, new[]
                {
                    new ArtDef("bd_cd_anaboruo", "安那般那守息禅", 1, "禅定",
                        new[] { Passive("chanding_vow_x15", "禅定一次涨愿×1.5(整除);心魔劫抗+5") }),
                    new ArtDef("bd_cd_sinianchu", "四念处止观", 2, "禅定",
                        new[] { Passive("merit_up25_unfreeze", "护生/降魔功德+25%(整除);破戒冻结时长-2步") }),
                    new ArtDef("bd_cd_jiucidi", "九次第定", 3, "禅定",
                        new[] { Passive("retreat_vow_double", "可'入定闭关':连续禅定愿力翻倍上限解锁;悟性对涨愿权重 8→6(更高效)") }),
                    new ArtDef("bd_cd_huixiang", "愿力回向法门", 4, "禅定",
                        new[] { Passive("vow_to_merit_2to1", "可把自身愿力按2:1转为功德(加速突破);渡化所得功德+30%") }),
                    new ArtDef("bd_cd_putiwusheng", "菩提无生大定", 5, "禅定",
                        new[] { Passive("innerdemon_pass_allturn", "心魔劫几乎必过(抗+50);入定期间被击不退、伤害全转愿;'我不入地狱'") }),
                }),
                // 愿行(降魔渡化)：功德善行类——把「降魔渡魔」变成可数据化的收束与功德引擎，本路独有戏剧轴。
                //   大额功德/愿力获取走 AddResource；镇/渡化/破戒护身等开关落 GrantPassive。
                new ArtCategoryDef("愿行", "salvation", 1, 1, new[]
                {
                    new ArtDef("bd_yx_husheng", "护生戒杀行", 1, "愿行",
                        new[]
                        {
                            new EffectOp(EffectOpKind.AddResource, "merit", 150, "每救1人/退1场杀劫+150功德"),
                            Passive("vowbroken_half", "杀戮破戒惩罚由清零→减半(护身)"),
                        }),
                    new ArtDef("bd_yx_fumo", "伏魔金刚印", 2, "愿行",
                        new[] { Passive("evil_pin_minus20", "对阴邪命中附'镇'(其攻-20%);镇中目标更易被渡化") }),
                    new ArtDef("bd_yx_xiangmo", "降魔杵法", 3, "愿行",
                        new[] { Passive("evil_dmg_x13_sha2merit", "对阴邪伤害×1.3(整除);击溃(非杀)阴邪转化其煞气×0.5为功德") }),
                    new ArtDef("bd_yx_duhua", "渡化往生咒", 4, "愿行",
                        new[] { Passive("salvation_close", "可对濒败阴邪'渡化收束':其煞气/恨值×1.0转本方功德并使其退场(不计杀业)") }),
                    new ArtDef("bd_yx_wobu", "我不入地狱大愿", 5, "愿行",
                        new[] { Passive("bear_karma_group_salvation", "可代他人承业:吸收一名同伴所受致命伤转为本方愿力消耗;群体渡化(一次清退全场低煞阴邪)") }),
                }),
                // 佛心 道心类目（M1，补遗第 10 节 chanheart「禅心·愿心之念」/「菩提无生心」）。A.0 仅装载不结算 →
                // tier=0（sumArtPower 贡献 0）、effects 留空（不触 daoHeart/innerDemon/comprehension 资源算子，A.2 道心层的事）。
                // 具名 + power=0。命名照补遗第 10 节 chanheart 条目。
                new ArtCategoryDef("佛心", "daoheart", 1, 1, new[]
                {
                    new ArtDef("dh_fo_anxin", "安心守息禅心诀", 0, "佛心", System.Array.Empty<EffectOp>()),
                    new ArtDef("dh_fo_hush", "护生养愿录", 0, "佛心", System.Array.Empty<EffectOp>()),
                    new ArtDef("dh_fo_buchen", "不嗔不痴心经", 0, "佛心", System.Array.Empty<EffectOp>()),
                    new ArtDef("dh_fo_puti", "菩提无生大定心", 0, "佛心", System.Array.Empty<EffectOp>()),
                }),
            };

            // —— 战技（深度设计「战技」节，OnUse 算子 + Cost 资源表；佛光弹药=vow 愿力 / merit 功德）。
            //    伤害/穿透/渡化收束等具体结算 Phase 3 接，A.0 以 AddPenInteger 占位破防量（量级对齐该路公式）+ Cost 表达资源门槛；
            //    命中回愿/收束转功德等正反馈以 AddResource 表达；anti_evil 倍伤/AOE 镇压走 Phase 3 克制矩阵，A.0 Note 留痕。——
            var skills = new[]
            {
                // 诸天神佛降世：终极AOE,对全场阴邪 base×anti_evil 真伤并群体渡化低煞者;耗尽愿力。门槛愿力≥3000(清空)。
                // B5扫尾: 占位 AddPenInteger(90) → FlatPen(90) 基线 + Modules.CounterMul(evil,×3)（佛光 anti_evil 对阴邪(evil tag)×3
                //   联合上界钳 §15.4；全场AOE 与 群体渡化低煞者走 Phase3,本模块表达 anti_evil 倍乘破防量）。
                new CombatSkillDef("sk_bd_zhutian", "诸天神佛降世", 5,
                    new[]
                    {
                        Modules.FlatPen(90, "终极AOE对全场阴邪 base×anti_evil 真伤基线破防量;群体渡化低煞者/耗尽愿力 Phase3"),
                        Modules.CounterMul("evil", 3, note:"对阴邪(evil tag) anti_evil×3(联合上界钳);非阴邪与群体渡化 Phase3"),
                    },
                    new Dictionary<string, int> { { "vow", 3000 } }),
                // 不动明王怒目：开金身大成态3回合,DR×2、所有受击全额转愿、对阴邪伤害再×1.5。愿力≥1500,消耗1500。
                // B5 批2：开金身大成态(goldenBodyMax 态 3 回合,DR×2/受击全额转愿)是唯一档签名状态机制(SpecialModuleRegistry 派发) → batch3 Special,
                //   显式 deferred（红线 A.8 不静默,待批3 wiring 后补 Special 构造）,保 AddPenInteger 占位破防量。
                new CombatSkillDef("sk_bd_budongming", "不动明王怒目", 5,
                    new[] { new EffectOp(EffectOpKind.AddPenInteger, null, 36, "开金身大成态3回合:DR×2、所有受击全额转愿、对阴邪伤害再×1.5(goldenBodyMax态→batch3 Special defer)") },
                    new Dictionary<string, int> { { "vow", 1500 } }),
                // 大须弥山掌：AOE镇压,对全场阴邪 base×2 伤害并附'镇';对非阴邪 base 伤害。愿力≥500,消耗500(+内力60 flavor)。
                // B5扫尾: 占位 AddPenInteger(48) → FlatPen(48) 基线 + Modules.CounterMul(evil,×2)（对阴邪(evil tag)×2;非阴邪 base 走 Phase3,'镇' debuff Phase3）。
                new CombatSkillDef("sk_bd_xumi", "大须弥山掌", 4,
                    new[]
                    {
                        Modules.FlatPen(48, "AOE镇压对全场阴邪 base×2 基线破防量;对非阴邪 base 伤害(内力60开销 flavor)"),
                        Modules.CounterMul("evil", 2, note:"对阴邪(evil tag)×2并附'镇';非阴邪只算 base Phase3"),
                    },
                    new Dictionary<string, int> { { "vow", 500 } }),
                // 渡魔往生印：收束技,对濒败阴邪触发渡化,煞气转功德、使其退场,不计杀业。愿力≥400+功德≥100。
                new CombatSkillDef("sk_bd_duomo", "渡魔往生印", 4,
                    new[]
                    {
                        Modules.FlatPen(0, "收束技:对濒败阴邪触发渡化,使其退场不计杀业(降魔渡魔戏剧收束;收束非伤害置0)"),
                        new EffectOp(EffectOpKind.AddResource, "merit", 100, "渡化所得:煞气转本方功德+100"),
                    },
                    new Dictionary<string, int> { { "vow", 400 }, { "merit", 100 } }),
                // 金刚伏魔圈：结界,圈内阴邪攻-30%、本方DR+5/层;持续3回合,期间受击转愿×2。愿力≥200,消耗200(+内力40 flavor)。
                new CombatSkillDef("sk_bd_fumoquan", "金刚伏魔圈", 3,
                    new[] { Modules.FlatPen(20, "结界:圈内阴邪攻-30%、本方DR+5/层;持续3回合,期间受击转愿×2(内力40开销 flavor;结界debuff/buff Phase3)") },
                    new Dictionary<string, int> { { "vow", 200 } }),
                // 摩诃无量佛光：对单体阴邪 anti_evil 倍伤(默认×3,整数定标);非阴邪只算 base 的一半。愿力≥300,消耗300。
                // B5 批2 招牌招迁移：占位 AddPenInteger(30) → FlatPen(30) 基线 + Modules.CounterMul(evil,×3)（佛光 anti_evil：
                //   防方带 evil tag(阴邪)→×3,联合上界钳 ×3/2 §15.4；非阴邪半数走 Phase 3。佛 SituationalTags 用 anti_evil 克制 tag,
                //   敌侧阴邪以 evil tag 表达,故 CounterMul 锚 "evil"）。
                new CombatSkillDef("sk_bd_foguang", "摩诃无量佛光", 3,
                    new[]
                    {
                        Modules.FlatPen(30, "对单体阴邪 anti_evil 倍伤基线破防量(整数定标)"),
                        Modules.CounterMul("evil", 3, note:"对阴邪(evil tag) anti_evil×3(默认,联合上界钳);非阴邪只算 base 的一半 Phase3"),
                    },
                    new Dictionary<string, int> { { "vow", 300 } }),
                // 韦驮献杵：金身重击,伤害=2×根骨+8×goldenLayers;命中回愿+100。门槛内力20(flavor),无愿力门槛,命中回愿。
                new CombatSkillDef("sk_bd_weituo", "韦驮献杵", 2,
                    new[]
                    {
                        Modules.FlatPen(24, "金身重击:伤害=2×根骨+8×goldenLayers 基线破防量(根骨/goldenLayers 双项 Phase3;内力20开销 flavor)"),
                        new EffectOp(EffectOpKind.AddResource, "vow", 100, "命中回愿+100(金身吃打涨愿)"),
                    },
                    new Dictionary<string, int>()),
                // 狮子吼：震慑范围内目标(攻-15%/1回合);对阴邪改为眩晕1回合。愿力≥50,消耗50(+内力20 flavor)。
                new CombatSkillDef("sk_bd_shizihou", "狮子吼", 1,
                    new[] { Modules.FlatPen(8, "震慑范围内目标(攻-15%/1回合);对阴邪改为眩晕1回合(内力20开销 flavor;debuff/对阴邪眩晕 Phase3)") },
                    new Dictionary<string, int> { { "vow", 50 } }),
                // 金刚反震：金身反震（OnDefend）。愿力≥30,消耗30。
                // B5扩21: ReflectDamage — 佛修金身反震,Amount=1/Amount2=3→1/3来袭伤害反震攻方。
                new CombatSkillDef("sk_bd_jingang_fanzhen", "金刚反震", 3,
                    new[] { Modules.Reflect(1, 3, "金身反震:1/3来袭伤害反震攻方") },
                    new Dictionary<string, int> { { "vow", 30 } }),
            };

            return new CultivationPathDef(
                "buddhist_golden_body", "佛修·金身道",
                "physical",
                // 属性/形态 tag（melee 近战金身 / righteous 正道 / anti_evil 克阴邪克制 tag），非对手 PathId（R2）。
                // anti_evil 喂入克制矩阵 element 轴（纯阳/正向 克 evil 阴邪），不硬编码任何对手 pathId。
                new[] { "melee", "righteous", "anti_evil" },
                resources,
                power,
                curve,
                arts,
                skills,
                new EntryGateDef("tag:fo_root"),
                new SelectionRuleDef(2, 8), // 战技抽 min(2+realm/2,8)（深度设计选取规则：早期拿不到 tier5 神通,呼应厚积晚发）
                null);
        }
    }
}
