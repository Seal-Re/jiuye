using System.Collections.Generic;

namespace Jianghu.Cultivation.Paths
{
    /// <summary>
    /// 阵修·阵法师 <c>array_formation</c>（physical 控制·环境-准备型资源枢纽路）。数据照《每路深度设计》阵修节 +
    /// 《内容补遗》第 4 节「阵修 array_formation — 道心：定盘星心」（arrayheart 道心类目）。
    /// 厚积晚发·强环境依赖：悟性掌阵为骨、realm 承阵阶、阵图 power 为锋、灵石算力为枢纽燃料；
    /// 地板极低天花板极高，强弱几乎全由「是否布阵×地利 terrain」决定（双包络），单挑最弱、偷家必败。
    ///
    /// A.0 设计取舍（照 FaXiu 范式：把非核心机制留 Note、不引入 L1）：深度设计原案战力
    ///   Power = 悟性×3 + realm×4 + arrayed_flag*((Σ阵图power)*(2+terrain) + compute×2) + stones/10 + 武力×1，
    ///   其中 arrayed_flag 门控（GateByFlag）与 (2+terrain) 地利乘子（TerrainMul）是 L1 ModKind、stones/10
    ///   是 L1 derived 量——A.0 **不引入**。本路 PowerFormula 仅落可由核心 src 表达的项：
    ///   悟性×3 / realm×4 / Σ所选阵图阵纹心法 tier×1 / compute×2 / stones×1 / 武力×1（全程整数、无 Modifier）。
    ///   「未布阵阵图项置 0」「地利乘子」「stones/10 整除」属 Phase 3 战斗/L1 结算，仅以 Note 留痕（不污染 A.0 公式）。
    ///
    /// 红线落实：terms 无 ×0、无 daoHeart（R3/R6）；SituationalTags=属性/形态 tag 非对手 PathId（R2）；
    /// RealmCurve 四列等长（M4，长度 8 = realm 0..7）；含 1 个 Role=daoheart 类目 arrayheart（M1，A.0 仅装载
    /// 不结算 → tier=0 使 sumArtPower 贡献 0、effects 留空不触 daoHeart/innerDemon 资源算子）。canon pathId（R4）。纯整数，禁浮点。
    /// </summary>
    public static class ArrayFormationPath
    {
        public static readonly CultivationPathDef Def = Build();

        // —— 被动算子（装配期落 state，核心 EffectOpKind）。功法的「悟性+N/掌阵根基+N」等改四维/战斗判定项
        //    A.0 为 flavor 不落算子（生成期 Σ=80 不被功法污染），仅以 Note 留痕；能落 state 的「compute 基线+N」
        //    走 AddResource、「stones 上限+N」走 AddResourceCap、被动开关（叠阵/借势/反偷家）走 GrantPassive。——
        private static EffectOp AddCompute(int amt, string note)
            => new EffectOp(EffectOpKind.AddResource, "compute", amt, note);

        private static EffectOp CapStones(int amt, string note)
            => new EffectOp(EffectOpKind.AddResourceCap, "stones", amt, note);

        private static EffectOp Passive(string flag, string note)
            => new EffectOp(EffectOpKind.GrantPassive, flag, 1, note);

        private static CultivationPathDef Build()
        {
            // —— per-path 专属资源（不进四维 C6）——
            // compute 阵纹算力：布阵速度（每步充能 setup_progress）与同时在场阵图上限；直接进公式（depth「compute×2」）。
            //   base=0 起底（基线由布阵心得 AddCompute 抬）；cap=60 容心法/阵纹多重增益。
            // stones 灵石储量：各阵每步耗能燃料、突破料；按 depth「stones/10 整除入战力」(A.0 简化为 res×1，/10 留 Note)。
            //   cap=100（聚石纹等 AddResourceCap 起底扩容）。setupProgress 布阵进度 0..220（最大 setupCost=周天星斗大阵）。
            var resources = new[]
            {
                new ResourceDef("compute", 0, 60, 0),
                new ResourceDef("stones", 0, 100, 0),
                new ResourceDef("setupProgress", 0, 220, 0),
            };

            // —— 战力公式（深度设计 terms 的 A.0 可表达子集）：悟性×3(掌阵根基·主项,替剑修武力位) +
            //    realm×4(可承载最高阵阶/阵基稳固) + Σ阵图阵纹心法 tier×1(所挂阵图 power,depth「阵图power×1」) +
            //    compute×2(算力,布阵速度/阵图上限) + stones×1(灵石燃料,depth 原 /10 整除留 Note) + 武力×1(单挑最弱·防除零)。
            //    悟性项挂 insightStep 台阶(无相心阵×3→×4)、compute 项挂 computeStep 台阶(周天算经×2→×3)；无 daoHeart、无 ×0（R3/R6）。——
            var power = new PowerFormulaDef(
                new[]
                {
                    new PowerTerm("stat:Insight", 3, "insightStep"),    // 悟性：掌阵根基/可同操阵纹路数，阵修主属性（无相心阵抬 ×3→×4）
                    new PowerTerm("realm", 4, null),                    // 境界：决定可承载最高阵阶与阵基稳固度
                    new PowerTerm("sumArtPower", 1, null),              // 所选阵图/阵纹/心法各功法 tier 之和（depth「阵图power×1」）
                    new PowerTerm("res:compute", 2, "computeStep"),     // 阵纹算力：布阵速度/阵图上限（周天算经抬 ×2→×3）
                    new PowerTerm("res:stones", 1, null),               // 灵石储量：各阵供能燃料（depth 原 stones/10 整除，A.0 取 ×1，/10 Phase3/L1）
                    new PowerTerm("stat:Force", 1, null),               // 武力：近乎摆设·单挑最弱，权重 1 而非 0 仅防除零（脆皮地板）
                },
                System.Array.Empty<PowerMod>(),
                null);

            // —— 战力曲线（深度设计 realmMul=[1,1,2,3,5,8,13,21]，realm 0..7，厚积晚发·后期陡峭·越级困杀）。
            //    此数组是「已布阵且 terrain=2 灵脉」的标准包络；未布阵 floor≈常数(被同阶碾压)、荒地 terrain=0 半之——
            //    双包络由 arrayed_flag×terrain（L1）实现，A.0 倍率表只承载「布阵灵脉态」基线，Note 留痕。
            //    四列等长（M4，长度 8）：倍率 / UnifiedTierOf（UT0-12 映射，枢纽路前段刻意低）/ 境界名
            //    （炼气→筑基→金丹→元婴→化神→炼虚→合体→阵道宗师）/ 升入阈值（除内息外须演阵台布成标志阵，厚积渐进，realm0=0 起）。——
            var curve = new RealmCurveDef(
                new[] { 1, 1, 2, 3, 5, 8, 13, 21 },
                new[] { 0, 1, 2, 3, 4, 5, 6, 7 }, // UT重锚: 阵修实战当量(原UT11→UT7)
                new[] { "演阵", "布阵", "结阵", "阵婴", "化阵", "炼阵", "合阵", "阵道宗师" },
                // 厚积晚发枢纽路：除内息外每升一境须独力布成本境标志阵，前段阈值累进（realm0=0 起）。
                new[] { 0, 95, 285, 570, 950, 1425, 1995, 2660 },
                // —— A.1 境界稿 §2：起步 SubLevelCount 全 1；CanAscend=true；MaxMajor=大境界数-1。——
                new[] { 1, 1, 1, 1, 1, 1, 1, 1 }, true, 7);

            // —— 功法类目（阵图/阵纹/布阵心得 各 5 具名 + arrayheart 阵心 4 具名）。
            //    具名/数值照深度设计「功法类目」节；每步耗能/困敌/真伤/封印 realm 等属 Phase 3 战斗结算，A.0 落
            //    能装配的 compute/stones 资源算子 + GrantPassive 开关，具体数值以 Note 留痕（不进 A.0 公式）。——
            var arts = new[]
            {
                // 阵图（具名阵法·核心输出/控制源）：每张给一份 power（A.0 走 tier→sumArtPower）；
                // 仅布成(arrayed_flag=1)后按地利计入战力(L1)、同时在场张数上限=realm+1(L1)，A.0 以 Note 留痕。
                new ArtCategoryDef("阵图", "attack", 1, 1, new[]
                {
                    new ArtDef("ar_zt_sixiang", "四象聚灵阵", 1, "阵图",
                        new[] { Passive("supply_array", "供能阵·续航地基:布成后每步回stones+2;setupCost=20") }),
                    new ArtDef("ar_zt_luanshi", "乱石迷踪阵", 2, "阵图",
                        new[] { Passive("trap_array", "困锁基石:阵内目标移动/脱离判定-30;setupCost=40") }),
                    new ArtDef("ar_zt_wuxing", "五行生杀阵", 3, "阵图",
                        new[] { Passive("kill_array", "主杀阵:阵内单体每步真伤+12,耗stones-5/步;setupCost=70") }),
                    new ArtDef("ar_zt_jiugong", "九宫八卦锁灵阵", 4, "阵图",
                        new[] { Passive("seal_array", "越级困杀核心:阵内敌方realm视为-2结算,耗stones-8/步;setupCost=120") }),
                    new ArtDef("ar_zt_zhoutian", "周天星斗大阵", 5, "阵图",
                        new[] { Passive("fortress_array", "护山级:阵内己方terrain视为3、敌方arrayed/法术-50%,不可移动;setupCost=220") }),
                }),
                // 阵纹（纹路构件·被动构件/乘区）：刻入阵基改写布阵效率/地利/资源转化。compute/stones 增益落资源算子，
                // 地利/叠阵/抗拆等开关落 GrantPassive。对齐他路 movement 槽位（每路第二类）。
                new ArtCategoryDef("阵纹", "movement", 1, 1, new[]
                {
                    new ArtDef("ar_zw_daoling", "导灵纹", 1, "阵纹",
                        new[] { AddCompute(4, "布阵充能/步+4,缩短setup窗口(compute基线+4)") }),
                    new ArtDef("ar_zw_jushi", "聚石纹", 2, "阵纹",
                        new[] { CapStones(50, "资源枢纽扩容:stones上限+50且采石所得+10/次") }),
                    new ArtDef("ar_zw_guji", "固基纹", 2, "阵纹",
                        new[] { Passive("anchor_durable", "抗拆阵:阵基耐久+30,被'破阵眼'打断arrayed_flag需额外命中1次") }),
                    new ArtDef("ar_zw_jieshi", "借势纹", 3, "阵纹",
                        new[] { Passive("terrain_up1", "弱地利翻盘:terrain实际档位+1(上限3),小灵脉当灵脉用") }),
                    new ArtDef("ar_zw_diezhen", "叠阵纹", 4, "阵纹",
                        new[] { Passive("array_cap_up2", "多阵叠杀:同时在场阵图上限额外+2(突破realm+1硬顶)") }),
                }),
                // 布阵心得（心法/算力层）：决定 compute 基线、悟性转化与布阵手数，是'算力布阵'内功根。
                // compute 基线落 AddResource、主项/算力权重台阶落 AddTermWeightStep、并行布阵/反偷家开关落 GrantPassive。
                new ArtCategoryDef("布阵心得", "internal", 1, 1, new[]
                {
                    new ArtDef("ar_xd_yiqi", "一气演阵诀", 1, "布阵心得",
                        new[] { AddCompute(3, "compute基线+3;悟性每点额外+1掌阵根基(flavor强化主项)") }),
                    new ArtDef("ar_xd_hubo", "双手互搏布阵心法", 2, "布阵心得",
                        new[] { Passive("parallel_setup", "并行布阵:每逻辑步可同时推进2张阵的setup_progress(折半总窗口)") }),
                    new ArtDef("ar_xd_qimen", "奇门遁甲心得", 3, "布阵心得",
                        new[] { Passive("disorder_open", "反偷家:布成阵首步附一次'乱序',敌setup/技能CD+2") }),
                    new ArtDef("ar_xd_zhoutian", "周天算经", 4, "布阵心得",
                        new[]
                        {
                            // 速布机变流核心：compute 项权重 ×2→×3（AddTermWeightStep 抬 computeStep 台阶）。
                            new EffectOp(EffectOpKind.AddTermWeightStep, "computeStep", 1, "算力直接变战力:compute战力项权重2→3"),
                            Passive("compute_to_power", "速布机变流:算力计入权重提升"),
                        }),
                    new ArtDef("ar_xd_wuxiang", "无相心阵", 5, "布阵心得",
                        new[]
                        {
                            // 主项跃迁：悟性 ×3→×4（AddTermWeightStep 抬 insightStep 台阶）；缓解单挑地板过低。
                            new EffectOp(EffectOpKind.AddTermWeightStep, "insightStep", 1, "主项跃迁:悟性战力项权重3→4"),
                            Passive("floor_up", "未布阵裸值floor抬升,缓解单挑地板过低"),
                        }),
                }),
                // arrayheart 道心类目（M1，补遗第 4 节「定盘星心」）。A.0 仅装载不结算 → tier=0（sumArtPower 贡献 0）、
                // effects 留空（不触 daoHeart/innerDemon/comprehension 资源算子，那是 A.2 道心层的事）。具名 + power=0。
                new ArtCategoryDef("阵心", "daoheart", 1, 1, new[]
                {
                    new ArtDef("dh_ar_dingpan", "定盘星诀", 0, "阵心", System.Array.Empty<EffectOp>()),
                    new ArtDef("dh_ar_tuiyan", "推演静心录", 0, "阵心", System.Array.Empty<EffectOp>()),
                    new ArtDef("dh_ar_buluan", "临阵不乱心经", 0, "阵心", System.Array.Empty<EffectOp>()),
                    new ArtDef("dh_ar_tianyuan", "天元归一阵心", 0, "阵心", System.Array.Empty<EffectOp>()),
                }),
            };

            // —— 战技（深度设计「战技」节，OnUse 算子 + Cost 资源表；枢纽燃料=stones 灵石 / compute 算力 / setupProgress 布阵进度）。
            //    伤害/点亮 arrayed_flag/搬阵/拆阵等具体结算 Phase 3 接，A.0 以 AddPenInteger 占位破防量（量级对齐本路公式）
            //    + Cost 表达资源门槛；非伤害开阵/搬阵以 AddResource/AddPenInteger=0 + Note 留痕。——
            var skills = new[]
            {
                // 算尽·叠杀：本步将在场所有阵 power 之和额外计一次入伤害(多阵叠加总爆发);需 compute≥30 撑联动 + 在场阵≥3 + stones-40。
                // B5批2: Σ阵 power 聚合 = derived provider(现全返0,未建)→ 显式 deferred EPIC-COMBAT-FULLSTRUCT(红线A.8);本批保 AddPenInteger 占位。
                new CombatSkillDef("sk_ar_suanjin", "算尽·叠杀", 5,
                    new[] { Modules.FlatPen(60, "在场所有阵power之和额外计一次入伤害(多阵叠加总爆发),需在场阵≥3(Σ阵 derived→FULLSTRUCT)") },
                    new Dictionary<string, int> { { "compute", 30 }, { "stones", 40 } }),
                // 引爆·焚阵：主动炸毁己方一张在场阵,对阵内全体造成该阵power×2一次性真伤(弃阵换爆发/同归手段);牺牲1张已布阵 + 该阵供能 stones 清。
                // B5批2: 炸阵唯一档签名(炸己方阵→power×2一次性)→ 批3 Special(explodeArray);本批保 AddPenInteger 占位标注。
                new CombatSkillDef("sk_ar_yinbao", "引爆·焚阵", 4,
                    new[] { Modules.FlatPen(40, "炸毁己方一张在场阵,对阵内全体造成该阵power×2一次性真伤(弃阵换爆发,批3 Special explodeArray)") },
                    new Dictionary<string, int> { { "stones", 30 } }),
                // 破阵·反制：反向解析敌方阵修,命中使其一张在场阵arrayed_flag归0(阵修内战/拆台手),命中率随双方悟性差;消耗compute-12。
                new CombatSkillDef("sk_ar_pozhen", "破阵·反制", 4,
                    new[] { Modules.FlatPen(30, "反向解析敌方阵:命中使其一张在场阵arrayed_flag归0(拆台手),命中率随双方悟性差") },
                    new Dictionary<string, int> { { "compute", 12 } }),
                // 困龙·锁：对阵内最高战力单体施'锁',其逃离/位移判定-50、realm结算-1,持续3步(越级困杀专点强敌);需杀阵/锁阵在场 + stones-12。
                // B5批2: → Control(arrayLock,3) 困龙锁定3步(控场积木,逃离/realm debuff 批4接,本轮ApplyOnUse不改dmg)。
                new CombatSkillDef("sk_ar_kunlong", "困龙·锁", 3,
                    new[] { Modules.Control("arrayLock", 3, "对阵内最高战力单体施'锁':逃离/位移判定-50、realm结算-1持续3步(专点强敌)") },
                    new Dictionary<string, int> { { "stones", 12 } }),
                // 挪阵·乾坤：把一张已布成阵的坐标平移到相邻节点并保持arrayed_flag(破'拉离灵脉'克制);消耗compute-8 + stones-15。
                new CombatSkillDef("sk_ar_nuozhen", "挪阵·乾坤", 3,
                    new[] { Modules.FlatPen(0, "把一张已布成阵平移到相邻节点并保持arrayed_flag(把灵脉/护山阵搬到敌前,破拉离灵脉克制;挪阵非伤害置0)") },
                    new Dictionary<string, int> { { "compute", 8 }, { "stones", 15 } }),
                // 急布·简阵：无视常规setup节奏,本步立刻布成1张tier≤2阵(应急开阵),但该阵power-30%、stones双倍耗;消耗compute≥10 + stones-20。
                new CombatSkillDef("sk_ar_jibu", "急布·简阵", 2,
                    new[] { Modules.FlatPen(0, "无视常规setup节奏本步立刻布成1张tier≤2阵(应急开阵),该阵power-30%、stones双倍耗;急布非伤害置0") },
                    new Dictionary<string, int> { { "compute", 10 }, { "stones", 20 } }),
                // 落阵·点枢：一次性把当前节点已蓄满setup_progress的阵全部点亮:arrayed_flag 0→1,阵图power解锁(开战核心动作);需 setupProgress≥20 且 stones≥10。
                new CombatSkillDef("sk_ar_dianshu", "落阵·点枢", 1,
                    new[]
                    {
                        Modules.FlatPen(0, "把当前节点已蓄满setup_progress的阵全部点亮:arrayed_flag 0→1,阵图power解锁(开战核心动作;点阵非伤害置0)"),
                        new EffectOp(EffectOpKind.AddResource, "setupProgress", -20, "消耗已蓄满的布阵进度点亮在场阵"),
                    },
                    new Dictionary<string, int> { { "setupProgress", 20 }, { "stones", 10 } }),
                // 阵遁·移形：阵纹遁形闪避（OnDefend）。需阵纹→门控。stones≥5,消耗5。
                // B5扩21: Evade — 阵修借阵纹地形闪避,Amount=25→25%来袭减免。
                new CombatSkillDef("sk_ar_zhendun", "阵遁·移形", 2,
                    new[] { Modules.Evade(25, "阵纹遁形闪避:25%来袭减免(需阵纹→门控)") },
                    new Dictionary<string, int> { { "stones", 5 } }),
            };

            return new CultivationPathDef(
                "array_formation", "阵修·阵法师",
                "physical",
                // 属性/形态 tag（physical 形态 / control 控制流派 / terrain_bound 强环境依赖），非对手 PathId（R2）。
                new[] { "physical", "control", "terrain_bound" },
                resources,
                power,
                curve,
                arts,
                skills,
                // 21 路唯一 entry tag 约定：每路 entry tag = 唯一 <pathkey>_root（array_root）。
                // 派生池 RootTagPool() 随之含 array_root → 阵修可被定路。
                new EntryGateDef("tag:array_root"),
                new SelectionRuleDef(2, 2), // 战技抽 2（深度设计选取规则'1主阵图+1阵纹+1布阵心得+2阵术战技'）
                null);
        }
    }
}
