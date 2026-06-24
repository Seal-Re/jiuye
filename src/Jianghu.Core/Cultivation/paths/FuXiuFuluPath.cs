using System.Collections.Generic;

namespace Jianghu.Cultivation.Paths
{
    /// <summary>
    /// 符修·符箓师 <c>fu_xiu_fulu</c>（照剑修 <see cref="SwordImmortalPath"/> 范式补的同等深度数据路）。
    /// 数据照《每路深度设计》「符修·符箓师」节（战力衡量 / 战力曲线 / 功法类目 / 战技池 / per-path 资源）+
    /// 《内容补遗》符修条目。physical 快充火力：以笔为剑、以纸为兵——战前预制、战中一次性引爆的「弹药经济」；
    /// 弱本体（武力/根骨权 1）、悟性为根、储备火力当量 stockFirepower 主导、零续航、万金油泛用。
    ///
    /// 红线落实（同剑修范式）：terms 无 ×0、无 daoHeart（R3/R6）；SituationalTags=属性/形态 tag（ranged/fire/
    /// thunder/talisman）非对手 PathId（R2）；RealmCurve 四列等长（M4）；含 1 个 Role=daoheart 类目 fuheart
    /// （M1，A.0 仅装载不结算 → tier=0 使 sumArtPower 贡献 0、effects 留空不触 daoHeart/innerDemon/comprehension
    /// 资源算子，那是 A.2 道心层的事）。canon pathId（R4）。纯整数，禁浮点。
    ///
    /// A.0 算子近似（核心 10 个 EffectOpKind，不自创）：设计里的「stockFirepower/storeCap/gradeFirepower/
    /// castWindow/maxGrade/批量贴符/破阵」等专属机制 A.0 未引入对应算子 → 一律以最接近的核心算子落 state
    /// （AddResourceCap 抬 fuPotency/talismanStore 上限、AddResource 播种、GrantPassive 置被动 flag、
    /// AddTermWeightStep 抬 stockFirepower/fuPotency 项台阶），具体语义全部进 Note 留痕，留待后续（L1）实装。
    /// </summary>
    public static class FuXiuFuluPath
    {
        public static readonly CultivationPathDef Def = Build();

        // —— 被动算子助手（装配期落 state，核心 EffectOpKind）。功法的「悟性折算+N/武力+N」等改四维项 A.0
        //    为 flavor 不落算子（生成期 Σ=80 不被功法污染，深度设计「NPC 选功法只加 sumArtPower 与资源，不改 Σ=80」），
        //    仅以 Note 留痕；能落 state 的「符库容量+N/符道功底上限+N」走 AddResourceCap、被动开关走 GrantPassive/SetFlag。——
        private static EffectOp CapStore(int amt, string note)
            => new EffectOp(EffectOpKind.AddResourceCap, "talismanStore", amt, note);

        private static EffectOp CapPotency(int amt, string note)
            => new EffectOp(EffectOpKind.AddResourceCap, "fuPotency", amt, note);

        private static EffectOp Passive(string flag, string note)
            => new EffectOp(EffectOpKind.GrantPassive, flag, 1, note);

        private static CultivationPathDef Build()
        {
            // —— per-path 专属资源（不进四维 C6）——
            // talismanStore 符库·一次性弹药池：攻/防/辅/役使符按张消耗,引爆即 −1；base cap=12（深度设计
            //   「storeCap = 12 + 4×realm」的 realm0 基线，realm 增益由功法 AddResourceCap 表达,A.0 单值起底）。
            // fuPotency 符道功底 0..cap：画符日课累积,定单符威力上限/可制最高符品；base cap=30（深度设计
            //   「30 + 10×realm」的 realm0 基线）。initial 取 0（出关前画符囤积；设计的 Insight/4 传承播种属生成期,A.0 起底 0）。
            var resources = new[]
            {
                new ResourceDef("talismanStore", 0, 12, 0),
                new ResourceDef("fuPotency", 0, 30, 0),
            };

            // —— 战力公式（深度设计 terms：悟性×5 + stockFirepower×3 + fuPotency×2 + realm×4 + 所选符箓 power×2
            //    + 内力×1 + 武力×1 + 根骨×1）。悟性权全路最高（画符心神/引符精度,与剑修武力主导相反）；
            //    武力/根骨刻意最低权 1（手无缚鸡,断笔即弱,仅防除零）；无 daoHeart、无 ×0（R3/R6）。——
            var power = new PowerFormulaDef(
                new[]
                {
                    new PowerTerm("stat:Insight", 5, null),          // 悟性为根：画符心神与引符精度,本路最高权四维
                    new PowerTerm("derived:stockFirepower", 3, null),// 储备火力当量:Σ符库各档存量×档火力(快充火力数值落点),专属派生
                    new PowerTerm("res:fuPotency", 2, null),         // 符道功底:决定单符威力上限与可制最高符品
                    new PowerTerm("realm", 4, null),                 // 境界:本路低权基底,越级齐射源于低 realm 权 + 高 stockFirepower 权
                    new PowerTerm("sumArtPower", 2, null),           // 所选制符术/符纹/役使符/心法各功法 tier 之和
                    new PowerTerm("stat:Internal", 1, null),         // 内力:引符灌灵的微弱真元底盘,远低于法修内力主权
                    new PowerTerm("stat:Force", 1, null),            // 武力:肉身近乎废,权 1 仅防除零(断笔即弱·手无缚鸡)
                    new PowerTerm("stat:Constitution", 1, null),     // 根骨:开光蕴灵耗精血的微弱兜底,弱权
                },
                System.Array.Empty<PowerMod>(),
                null);

            // —— 战力曲线（深度设计 realmMul=[10,14,19,25,33,43,56,73]，realm 0..7,平缓凸·高初值零续航）。
            //    四列等长（M4）：倍率 / UnifiedTierOf（设计 UT [0,1,2,3,4,6,8,10]:金丹后跳档,顶端渡劫态）/
            //    境界名（画符学徒→制符入门→本命符成→符箓通玄→符海大成→万符朝宗→符道宗师→纸笔通神·万象归符）/
            //    升入阈值（前期投资·画符囤库累进里程,realm0=0 起）。——
            var curve = new RealmCurveDef(
                // A1.2 迁移（境界稿 §11.1）：符修是修士但 build 期顶仅 UT10（under-shoot）→
                //   顶补渡劫 UT11/飞升 UT12 两大境界，四列各 +2 项（mul 续凸增/thresholds 续增/names 占位）。
                new[] { 10, 14, 19, 25, 36, 46, 60, 79, 103, 133 }, // INV-CROSS v2: buff +8% UT4+; UT8=1.30x sword (was 1.22x)
                // 偏离 UT1/3 压相邻偶数主阶（1→0,3→2）→ 全落锚集 {0,2,4,6,8,9,10,11,12}、非降；密度走 SubLevel（决策③）。
                new[] { 0, 0, 2, 2, 4, 6, 8, 10, 11, 12 },
                new[] { "画符学徒", "制符入门", "本命符成", "符箓通玄", "符海大成", "万符朝宗", "符道宗师", "纸笔通神·万象归符", "符道渡劫", "符仙" },
                // 制符里程累进阈值 = Σ 100×(0..i-1)（前期投资型,与剑修同构,realm0=0 起底）；顶补两境续增（+800/+900）。
                new[] { 0, 100, 300, 600, 1000, 1500, 2100, 2800, 3600, 4500 },
                // —— A.1：SubLevelCount = 同 UT 段长（UT0 段 2 = 画符学徒/制符入门、UT2 段 2 = 本命符成/符箓通玄 → 大境界数 8）；
                //    CanAscend=true（符修是修士）；MaxMajor=大境界数-1=7。——
                new[] { 2, 2, 1, 1, 1, 1, 1, 1 }, true, 7);

            // —— 功法类目（制符术/符纹学/役使符/心法 各 4~5 具名 + fuheart 符心 4 具名）。
            //    具名/效果照深度设计「功法类目」节；A.0 以核心算子近似落 state,专属语义进 Note。——
            var arts = new[]
            {
                // 制符术（core·决定 fuPotency 推进速率/可制最高符品 maxGrade/单批产量/单符威力上限;选 1 部为主修）。
                new ArtCategoryDef("制符术", "craft_talisman", 1, 1, new[]
                {
                    new ArtDef("fu_ct_zhusha", "朱砂启蒙画符诀", 1, "制符术",
                        new[] { CapPotency(3, "符道功底推进+3,制符batchMax+1,matGrade≤2不易废纸(失败损耗减半)") }),
                    new ArtDef("fu_ct_leihuo", "雷火符箓真传", 2, "制符术",
                        new[] { Passive("unlock_attack_grade3", "解锁攻符系(火/雷符)maxGrade至t3,制攻符单符gradeFirepower+2(火力当量抬升)") }),
                    new ArtDef("fu_ct_wuxing", "五行符篆秘法", 3, "制符术",
                        new[]
                        {
                            CapPotency(10, "符道功底上限+10"),
                            Passive("mat_grade_plus1", "可掺稀材使matGrade有效值+1(最高5),batchMax+2,更易出高品符"),
                        }),
                    new ArtDef("fu_ct_guiyuan", "万符归元制符大法", 4, "制符术",
                        new[] { Passive("craft_count_insight_bonus", "制符一批count额外+悟性/10,解锁maxGrade至t4,炼成满纹符(fuPerfect)概率提升") }),
                    new ArtDef("fu_ct_tongshen", "纸笔通神·万象符诀", 5, "制符术",
                        new[]
                        {
                            // 化神以上每批 fuPotency 推进 +15、stockFirepower 计入额外 +1 档 → 抬 stockFirepower 项台阶（AddTermWeightStep）。
                            new EffectOp(EffectOpKind.AddTermWeightStep, "stockFirepowerStep", 1, "解锁maxGrade t5(无此功法硬封顶t4);化神以上每批fuPotency推进+15,stockFirepower项视为+1阶"),
                        }),
                }),
                // 符纹学（被动构件/乘区·刻入符基改写符库经济/火力当量/续航转化,不单独成符但放大整套弹药体系;选 1 部）。
                new ArtCategoryDef("符纹学", "rune_inscribe", 1, 1, new[]
                {
                    new ArtDef("fu_ri_juling", "聚灵符纹", 1, "符纹学",
                        new[] { CapStore(6, "符库容量+6(囤更多弹药)") }),
                    new ArtDef("fu_ri_suyin", "速引符纹", 2, "符纹学",
                        new[] { Passive("cast_window_minus1", "所有符战技castWindow−1(起手更快,缓被断笔窗口;下限1)") }),
                    new ArtDef("fu_ri_gubi", "固笔符纹", 2, "符纹学",
                        new[] { Passive("anti_interrupt_once", "被断笔/打断画符需对手额外命中1次才作废(抗断笔,固基纹同构)") }),
                    new ArtDef("fu_ri_dieshe", "叠射符纹", 3, "符纹学",
                        new[] { Passive("volley_plus1", "齐射类符战技可同时引爆符数上限+1(多线齐射放大)") }),
                    new ArtDef("fu_ri_dawen", "帝纹·天火符印", 4, "符纹学",
                        new[]
                        {
                            CapStore(2, "高品符火力当量翻倍式抬升(gradeFirepower[t4..t5]各+3的A.0近似:抬符库容量上限以承高品弹药)"),
                            // 每引爆一张高品符 fuPotency −1（火力↔功底对称负反馈）→ A.0 仅 Note,引爆耗为战斗期事,装配期不落。
                            Passive("dawen_high_grade", "gradeFirepower[t4..t5]各+3;每引爆一张高品符fuPotency−1(火力↔功底对称负反馈,战斗期结算)"),
                        }),
                }),
                // 役使符（战场级役使·把符化为纸甲神兵/封锁符阵/批量贴符增益,是「万金油泛用」与役使态来源;选 1 部）。
                new ArtCategoryDef("役使符", "summon_talisman", 1, 1, new[]
                {
                    new ArtDef("fu_st_zhijia", "纸甲神兵符", 2, "役使符",
                        new[] { Passive("summon_paper_soldier", "召一具纸甲神兵代战(power=itemFirepower×2,1场后焚毁);属死物役使,免疫毒蛊/音修乱心") }),
                    new ArtDef("fu_st_zhaojiang", "召将符·三十六天将", 3, "役使符",
                        new[] { Passive("summon_generals", "一次性召至多3道符将群攻全场,对每邻近敌stockFirepower/10+悟性伤害(多线齐射核心)") }),
                    new ArtDef("fu_st_baizhanqi", "万军贴符·百战旗", 3, "役使符",
                        new[] { Passive("batch_paste_ally", "批量给至多N友军(N=1+realm/2)各贴攻/防符,AdjustRelationEdge同盟侧+并给其增益(后勤枢纽落点)") }),
                    new ArtDef("fu_st_wufang", "五方封禁符阵", 4, "役使符",
                        new[] { Passive("seal_array_node", "节点预埋封锁符阵(战场标记):阵内敌castWindow/位移−,阵内己方符gradeFirepower+2,破阵符可拆") }),
                    new ArtDef("fu_st_jinchan", "纸人替身·金蝉符", 5, "役使符",
                        new[] { Passive("paper_substitute", "被斩首/必杀时一次性纸人替身硬抗(每境界1次)、原地脱战(缓斩首脆性,但talismanStore−1高品符为引)") }),
                }),
                // 心法（常驻内功·抬悟性/内力折算、扩 fuPotency cap 与画符稳定度,是「制符投资」的内功根;选 1 部）。
                new ArtCategoryDef("心法", "fu_mind", 1, 1, new[]
                {
                    new ArtDef("fu_mind_ningshen", "凝神画符诀", 1, "心法",
                        new[] { CapPotency(3, "悟性折算战力+1/级,符道功底推进+3") }),
                    new ArtDef("fu_mind_longshe", "笔走龙蛇心法", 2, "心法",
                        new[] { Passive("invest_speedup", "制符日课fuPotency每步+悟性/8改+悟性/6(投资提速)") }),
                    new ArtDef("fu_mind_yangling", "朱砂养灵心经", 3, "心法",
                        new[]
                        {
                            CapPotency(10, "符道功底上限+10"),
                            CapStore(4, "符库容量+4,战后回库判定更稳"),
                        }),
                    new ArtDef("fu_mind_zhuxian", "诛仙符篆·正卷心法", 4, "心法",
                        new[] { Passive("art_power_plus4", "所选符箓功法sumArtPower整体+4,被断笔后innerDemon+减半(接道心轴,仅心法层flag)") }),
                    new ArtDef("fu_mind_zhoutian", "万象归符·周天搬运", 5, "心法",
                        new[]
                        {
                            new EffectOp(EffectOpKind.AddResource, "talismanStore", 1, "每场战斗开局自动制1批应急符入库(fullStock更易达成→postMul开局爆发更稳)"),
                        }),
                }),
                // fuheart 道心类目（M1,补遗「符心·凝笔之念」role=daoheart）。A.0 仅装载不结算 → tier=0（sumArtPower 贡献 0）、
                // effects 留空（不触 daoHeart/innerDemon/comprehension 资源算子,那是 A.2 道心层的事）。具名 + power=0。
                new ArtCategoryDef("符心", "daoheart", 1, 1, new[]
                {
                    new ArtDef("dh_fu_ningbi", "凝笔静心诀", 0, "符心", System.Array.Empty<EffectOp>()),
                    new ArtDef("dh_fu_yunfu", "蕴符悟道录", 0, "符心", System.Array.Empty<EffectOp>()),
                    new ArtDef("dh_fu_buzhuo", "笔意不乱心经", 0, "符心", System.Array.Empty<EffectOp>()),
                    new ArtDef("dh_fu_wanxiang", "纸笔通神·万象归符道心", 0, "符心", System.Array.Empty<EffectOp>()),
                }),
            };

            // —— 战技（深度设计「战技池」节,OnUse 算子 + Cost 资源表;cost 走 talismanStore/fuPotency）。
            //    伤害/穿透/castWindow/符废等具体结算 Phase 3 接,A.0 以 AddPenInteger 近似整数破防量占位 + Cost 表达资源门槛。
            //    Cost key 只引 Resources 现有键（talismanStore/fuPotency）,余量不足则 TryPayCost 拒。——
            var skills = new[]
            {
                // 五雷轰顶符（攻·爆发）t5:引爆顶品攻符,单点/小范围终极火力(悟性×2+gradeFirepower[t5]×3+fuPotency);
                //   对 ghost/demon/阴邪+10。castWindow=2 被断则符废。耗 talismanStore×1+fuPotency×2。
                new CombatSkillDef("sk_fu_wulei", "五雷轰顶符", 5,
                    new[] { Modules.FlatPen(60, "引爆顶品攻符,悟性×2+gradeFirepower[t5]×3+fuPotency终极火力(gradeFirepower derived→Phase3),对ghost/demon/阴邪+10;castWindow=2被断符废") },
                    new Dictionary<string, int> { { "talismanStore", 1 }, { "fuPotency", 2 } }),
                // 符海齐射（攻·多线）t4:一次性引爆多张攻符覆盖全场,对每邻近敌stockFirepower/8+悟性/2,敌越多总伤越高;
                //   引爆张数=2+叠射符纹。castWindow=2。耗 talismanStore×2(按引爆张数近似)。
                // B5 批2：本招吃 stockFirepower（=Σ符库各档存量×档火力,是 derived:stockFirepower 派生量,**非 ResourceDef**：
                //   本路 Resources 仅 talismanStore/fuPotency）→ PenFromResource 锚不到资源 → 显式 deferred FULLSTRUCT
                //   （红线 A.8 不静默,真 stockFirepower 派生求和 L1 IDerivedProvider 后接）,保 FlatPen 占位破防量。
                new CombatSkillDef("sk_fu_qishe", "符海齐射", 4,
                    new[] { Modules.FlatPen(36, "一次性引爆多张攻符覆盖全场,对每邻近敌stockFirepower/8+悟性/2(stockFirepower=derived非资源→FULLSTRUCT defer),敌越多总伤越高") },
                    new Dictionary<string, int> { { "talismanStore", 2 } }),
                // 护身金光符（防·一次性挡）t2:即贴护身符吸收gradeFirepower[t2]×2+内力伤害并免一次穿透,可贴己/贴1友军;
                //   castWindow=1。耗 talismanStore×1。
                new CombatSkillDef("sk_fu_hushen", "护身金光符", 2,
                    new[] { Modules.FlatDR(18, "即贴护身符吸收gradeFirepower[t2]×2+内力伤害并免疫一次穿透,可贴己或1友军(扛剑修/体修一击)") },
                    new Dictionary<string, int> { { "talismanStore", 1 } }),
                // 疾风遁符（辅/逃·放风筝）t3:贴疾风符强制位移拉距,规避当回合一次必杀并使下回合对近战敌攻符判定+5;
                //   castWindow=0(应急0起手)。耗 talismanStore×1。
                new CombatSkillDef("sk_fu_jifeng", "疾风遁符", 3,
                    new[] { Modules.SituationalAdj(5, "贴疾风符强制位移/拉开距离,规避当回合一次必杀,下回合对近战敌攻符判定+5(放风筝克近战起手)") },
                    new Dictionary<string, int> { { "talismanStore", 1 } }),
                // 破阵焚符（控·破被动防御）t3:对敌方阵法/被动防御流/护盾引爆破阵符,使其一张在场阵归0或护盾power清零;
                //   castWindow=1。耗 talismanStore×1。
                new CombatSkillDef("sk_fu_pozhen", "破阵焚符", 3,
                    new[] { Modules.FlatPen(24, "对敌方阵法/被动防御流/护盾引爆破阵符,使其一张在场阵arrayed_flag归0或护盾power清零(克阵修/防御流;拆阵 Phase3)") },
                    new Dictionary<string, int> { { "talismanStore", 1 } }),
                // 召将纸甲（役使·代战）t3:召纸甲神兵/符将群代战1场,分担伤害并对全场敌持续悟性/2真伤;死物役使免毒蛊/音修乱心。
                //   耗 talismanStore×1+fuPotency×3。
                new CombatSkillDef("sk_fu_zhaojiazhijia", "召将纸甲", 3,
                    new[] { Modules.FlatPen(12, "召纸甲神兵/符将群代战1场,分担伤害并对全场敌持续悟性/2真伤;死物役使免疫毒蛊/音修乱心(万金油役使位;召物+持续真伤 Phase3)") },
                    new Dictionary<string, int> { { "talismanStore", 1 }, { "fuPotency", 3 } }),
                // 血符·焚尽（邪·搏命）t4:以精血为引强催符威(悟性×3+stockFirepower/4,倾尽部分库存),
                //   但 innerDemon+5、根骨−2(本场)、fuPotency减半(邪符暴涨+反噬+道德下滑搏命引信)。castWindow=1。耗 talismanStore×1+fuPotency×2。
                // B5 批2 招牌招迁移：FlatPen(40) 基线破防量（stockFirepower/4 是 derived 派生→FULLSTRUCT defer,与符海齐射同因）
                //   + Modules.Backlash(bloodCast,自损)（以精血催符自损 innerDemon+5/根骨-2；selfDmg 通道批4 接,ApplyOnUse 不改 dmg,本轮断言 Kind 在册）。
                new CombatSkillDef("sk_fu_xuefu", "血符·焚尽", 4,
                    new[]
                    {
                        Modules.FlatPen(40, "以精血为引强催符威 悟性×3+stockFirepower/4(stockFirepower=derived→FULLSTRUCT defer) 基线破防量"),
                        Modules.Backlash("bloodCast", 0, "以精血催符自损:innerDemon+5/根骨−2本场/fuPotency减半(自伤通道批4接,A.2 道心轴 innerDemon 不在 A.0 落)"),
                    },
                    new Dictionary<string, int> { { "talismanStore", 1 }, { "fuPotency", 2 } }),
                // 纸人金蝉（保命·反斩首）t5:被斩首/濒死时一次性纸人替身硬抗致死伤并原地脱战(每境界1次);缓役使物断线斩首脆性。
                //   耗 talismanStore×1(被动触发,高品符为引)。
                new CombatSkillDef("sk_fu_jinchan", "纸人金蝉", 5,
                    new[] { Modules.FlatDR(0, "被斩首/濒死时一次性纸人替身硬抗致死伤并原地脱战(每境界1次),缓操控者被斩首→役使物断线脆性(被动触发)") },
                    new Dictionary<string, int> { { "talismanStore", 1 } }),
            };

            return new CultivationPathDef(
                "fu_xiu_fulu", "符修·符箓师",
                "physical",
                // 属性/形态 tag（ranged 远程齐射/放风筝 / fire 火符 / thunder 雷符 / talisman 用符）,非对手 PathId（R2）。
                new[] { "ranged", "fire", "thunder", "talisman" },
                resources,
                power,
                curve,
                arts,
                skills,
                new EntryGateDef("tag:fu_root"),
                new SelectionRuleDef(2, 3), // 战技抽 2~3（多线齐射,符海/护身/役使多位一体,同剑修档）
                null);
        }
    }
}
