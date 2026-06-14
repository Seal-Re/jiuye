using System.Collections.Generic;

namespace Jianghu.Cultivation.Paths
{
    /// <summary>
    /// 因果时空命运修·大道法则 <c>yinguo_faze</c>（law/fate 顶层天敌位）。数据照《余 9 路深度设计》
    /// 「# 因果时空命运修 · 大道法则 yinguo_faze」节 +《内容补遗》第 7 部命修条目同源命名约束
    /// （严格不与命修 ming_fate_causality「卜术/推衍/夺运/因果」类目重名）。九野「合道」投影：
    /// 转职终点非起点、用法即先付天谴费（RetributionDebt）、几乎只吃悟性 + 三律权限阶；合道(lawBound)前
    /// 战力压地板、合道瞬间悬崖式跃迁（全路最陡后置凸曲线）。
    ///
    /// 红线落实：
    ///  - terms 无 ×0、无 daoHeart/innerDemon（R3/R6）。设计 §④ 原列 stat:Force×0 / stat:Constitution×0
    ///    是「练拳脚/堆肉身零收益」的 flavor，**A.0 禁 ×0 项故直接不列**（以本注释 + 公式注释留痕），不写
    ///    Weight==0 项。realm 的乘性放大全由 RealmCurve 承载，公式只放一个 realm 正权项（权重压到 1 防早期虚高）。
    ///  - SituationalTags = 属性/形态 tag（spirit_attack 役使规则=神识/法则侧攻击 / ranged 算计非贴脸·本体极脆
    ///    近身被碾压 / righteous 掌天机隐世正道传承），**非对手 PathId**（R2）。设计 §⑧ counterKeys（law_supreme/
    ///    fate_apex/melee_assassinate_decap…）属克制网 CounterMatrix 的事，不落本 Def 的 tag。
    ///  - RealmCurve 四列等长（M4，长度 8 = major 0..7）。**修正 backlog2-M4 原案**：设计 §⑤ realmMul=[6,7,9,12,18,
    ///    60,110,200] 为 8 档，但 §⑤ UnifiedTierOf=[0,2,4,8,10,11,12] 仅 7 档 → 8vs7 不等长会被 RealmCurve.Validate
    ///    拦截。此处补齐为等长 8 档：在「演化(UT4)→合道初(UT8)」间补回原案漏掉的一档（证因 UT6），并使合道(lawBound)
    ///    落在 index5（对应设计「本路 major≥5 合道」），18→60 的 4→5 跨阶即「合道=战力来源整体切换」的全路最陡跃迁。
    ///  - 含 1 个 Role=daoheart 类目 fateheart_law（M1，A.0 仅装载不结算 → tier=0 使 sumArtPower 贡献 0、effects 留空
    ///    不触 daoHeart/innerDemon/comprehension 资源算子，那是 A.2 道心层的事）。canon pathId（R4）。纯整数，禁浮点。
    /// </summary>
    public static class YinguoFazePath
    {
        public static readonly CultivationPathDef Def = Build();

        // —— 被动算子（装配期落 state，核心 EffectOpKind）。功法的「悟性+N/规则压制广度+N」等改四维/衍生项 A.0
        //    为 flavor 不落算子（生成期 Σ=80 不被功法污染）；能落 state 的「因果栈深上限+N」走 AddResourceCap、
        //    被动开关/布法子（种因/缩地/无相/合道复利）走 GrantPassive/SetFlag、即时律权限阶/栈深/天谴债走 AddResource。
        //    A.0 仅这 10 个核心 EffectOpKind（EffectOp.cs）；设计 §② 提到的 AddCounterAdj/AdjustRelationEdge 是 L1
        //    留给后续的算子，A.0 不引入 → 以最接近的 GrantPassive/AddResource 近似 + Note 注释说明（见各功法）。——
        private static EffectOp CapKarmic(int amt, string note)
            => new EffectOp(EffectOpKind.AddResourceCap, "karmicDepth", amt, note);

        private static EffectOp Passive(string flag, string note)
            => new EffectOp(EffectOpKind.GrantPassive, flag, 1, note);

        private static CultivationPathDef Build()
        {
            // —— per-path 专属资源（不进四维 C6）。设计 §① 三律分账 + §② 天谴债贷款 + §③ 因果索引栈。——
            // causalAuth/spaceTimeAuth/destinyAuth 三律权限阶 [0,9]：战力脊柱（合道前经 modifier ×1/4 重压，A.0 单值
            //   起底 0，悟道枢残痕/合道经 AddResource 升阶）。三轴只在 Flags["lawBound"]==1 时满权计入战力。
            // karmicDepth 因果栈深：回溯/还施的「弹药储量」（设计 causalDepth=2+causalAuth/3，A.0 base=2、cap=10 起底，
            //   因果类功法经 AddResourceCap 抬深；被斩首/乱因果时清空 = karmicDepth 归 0 → 时空/因果术哑火）。
            // retributionDebt 天谴债（用法即先付费的贷款，本路强代价地板）：施法预扣，越高战力越虚（modifier②，A.0 公式层
            //   不放进 terms — 它是「打折/反噬」轴非战力轴，权重 0 不进战力，承负类功法主动还）。Min 0、cap 99（跨 80 反噬）。
            // lifespanDebt 折寿：逆演/夺定数/合道劫的成本与反噬落点（与命修同构货币）。
            var resources = new[]
            {
                new ResourceDef("causalAuth", 0, 9, 0),
                new ResourceDef("spaceTimeAuth", 0, 9, 0),
                new ResourceDef("destinyAuth", 0, 9, 0),
                new ResourceDef("karmicDepth", 0, 10, 2),
                new ResourceDef("retributionDebt", 0, 99, 0),
                new ResourceDef("lifespanDebt", 0, 100, 0),
            };

            // —— 战力公式（设计 §④ terms）：悟性×6（唯一主属性,顿悟法则全凭悟性）+ 因果/时空/命运三律权限阶各×5
            //    （合道前实权重经 modifier 砍 1/4，A.0 以 lawBoundStep 台阶键承载「合道改权重不改结构」范式）
            //    + karmicDepth×2（因果栈深=弹药储量）+ sumArtPower×1（=「规则压制广度」lawBreadth：所选 因果/时空/命运/
            //    承负功法 power 之和；设计写 derived:lawBreadth，A.0 DerivedRegistry 空注册会恒返 0 → 改用引擎已结算的
            //    sumArtPower 表达同一语义,与命修 ming_fate_causality 同处理,权重低于三律权限）+ realm×1（只作线性下限,
            //    真正爬升在 Curve）。设计 stat:Force×0 / stat:Constitution×0（练拳堆肉身零收益）因 R6 禁 ×0 **直接不列**。
            //    无 daoHeart/innerDemon（R3）、无 ×0（R6）。weightStepKey:lawBoundStep 挂三律权限项 → 合道时抬权重台阶。——
            var power = new PowerFormulaDef(
                new[]
                {
                    new PowerTerm("stat:Insight", 6, null),                    // 悟性：唯一主属性,代替命修净气运/法修内力成为战力脊柱
                    new PowerTerm("res:causalAuth", 5, "lawBoundStep"),        // 因果律权限阶（合道前实权 1/4,合道经 lawBoundStep 抬阶）
                    new PowerTerm("res:spaceTimeAuth", 5, "lawBoundStep"),     // 时空律权限阶（同上 step 门）
                    new PowerTerm("res:destinyAuth", 5, "lawBoundStep"),       // 命运律权限阶（同上 step 门,本路对外泛克兑现层）
                    new PowerTerm("res:karmicDepth", 2, null),                 // 因果栈深：回溯/还施弹药储量,越深越强
                    new PowerTerm("sumArtPower", 1, null),                     // 规则压制广度 lawBreadth(所选三律/承负功法 power 和),权重低于律权限
                    new PowerTerm("realm", 1, null),                           // 境界：只作乘性倍率索引,线性权重压到 1 防早期虚高,真爬升在 Curve
                },
                System.Array.Empty<PowerMod>(),
                null);

            // —— 战力曲线（设计 §⑤ realmMul=[6,7,9,12,18,60,110,200]，major 0..7，厚积晚发 + 4→5 合道悬崖跃迁,
            //    末倍率 20.0）。四列等长（M4，长度 8）：倍率 / UnifiedTierOf（修正原案 8vs7：补回证因 UT6 一档凑等长,
            //    合道落 index5=UT10=LAW_BIND_TIER,18→60 即「合道=战力来源切换」全路最陡单阶跃迁,低段跳档大=转职终点
            //    低境界几乎不存在）/ 境界名（碎窥→缔因→演化→证因→演道→合道→证道→大道归一,major≥5 合道起）/
            //    升入阈值（合道里程累进,realm0=0 起,取 150× 链=比命修 120× 更陡,坐实「门控最严/慢且无资源捷径」）。——
            var curve = new RealmCurveDef(
                new[] { 6, 7, 9, 12, 18, 60, 110, 200 },
                new[] { 0, 2, 4, 6, 8, 10, 11, 12 },
                new[] { "碎窥", "缔因", "演化", "证因", "演道", "合道", "证道", "大道归一" },
                // 合道里程（设计「突破=合道、门控最严无捷径」）：升入第 i 境累进阈值 = Σ 150×(0..i-1)（比命修 120× 更陡=逆天合道代价）。
                new[] { 0, 150, 450, 900, 1500, 2250, 3150, 4200 },
                // —— A.1 境界稿 §2：起步 SubLevelCount 全 1；CanAscend=true；MaxMajor=大境界数-1。——
                new[] { 1, 1, 1, 1, 1, 1, 1, 1 }, true, 7);

            // —— 功法类目（因果律/时空律/命运律/承负 各 5 具名 + fateheart_law 道心 4 具名）。具名/power/gates 照设计 §②；
            //    EffectOp 只用核心 10 算子,设计的「AddCounterAdj 抹因 / AdjustRelationEdge 渡化 / 撤销重演 / 改定数」等
            //    战斗期复杂语义 A.0 以 GrantPassive/SetFlag 占位（被动开关）+ Note 记真义,完整结算 Phase 3 接。
            //    art Power 即 tier（schema 无独立 power 字段,沿范式以 Tier 表达 power 档,sumArtPower=Σtier）。——
            var arts = new[]
            {
                // 类目一·因果律 causality_law（种因得果/断因果/还施彼身,绑 causalAuth,因果栈来源与出口）。
                new ArtCategoryDef("因果律", "causality", 1, 1, new[]
                {
                    new ArtDef("yg_yg_yinyuan", "因缘缔结印", 2, "因果律",
                        new[] { Passive("plant_cause", "种因:向目标埋因果种,其每对本方造成伤害则其侧 RetributionDebt+Δ 为还施积栈(causalAuth≥1);成本 RetributionDebt(战斗期结算)") }),
                    new ArtDef("yg_yg_duanyin", "断因绝果诀", 3, "因果律",
                        new[] { Passive("sever_cause", "抹除目标本回合一次攻击的'因':其下一动作 counterAdj/先手归零('从未发生',近似AddCounterAdj→A.0 用被动开关占位),成本 RetributionDebt+6(causalAuth≥3)") }),
                    new ArtDef("yg_yg_huanshi", "还施彼身·业镜回照", 4, "因果律",
                        new[] { Passive("retort_karmic", "从 KarmicIndex 栈弹出'本方承受的一次伤害'原样回写来源(绕防,近似ApplyStatDelta→A.0 被动占位),成本 RetributionDebt+10、栈深-1(causalAuth≥4)") }),
                    new ArtDef("yg_yg_wanfa", "因果律·万法归因", 5, "因果律",
                        new[]
                        {
                            CapKarmic(2, "合道复利:因果栈深上限+2(每解析一次交锋栈深+1的容量门)"),
                            Passive("cause_in_me", "合道(lawBound)态每解析一次交锋因果栈深+1且本方被克 counterAdj 负值减半('因在我果由我定');RetributionDebt 每跨20此被动暂失效一回合(causalAuth≥6)"),
                        }),
                    new ArtDef("yg_yg_liaoduan", "一念断尘·了断三世", 5, "因果律",
                        new[] { Passive("end_three_lives", "终结技基底:清空敌方已积全部'因'(其对本方 counterAdj/先手归零)并把本方天谴债 1/3(整除)以'了断'勾销;仅合道且 causalAuth≥7 入池") }),
                }),
                // 类目二·时空律 spacetime_law（停滞/加速/回溯/瞬移/囚禁,绑 spaceTimeAuth,与因果栈联动撤销重演）。
                new ArtCategoryDef("时空律", "spacetime", 1, 1, new[]
                {
                    new ArtDef("yg_sk_xumi", "须弥纳芥诀", 2, "时空律",
                        new[] { Passive("shrink_ground", "缩地:本场距离档对本方失效一次(瞬移贴脸或脱离),破'近身快攻'天敌的下限保命术(spaceTimeAuth≥1)") }),
                    new ArtDef("yg_sk_chana", "刹那停滞印", 3, "时空律",
                        new[] { Passive("time_freeze", "令目标本回合'时间停滞':其 Tempo/先手价值整回合归零、不可发动战技,成本 RetributionDebt+6;对同为 fate/law 者效果减半(同类制衡)(spaceTimeAuth≥3)") }),
                    new ArtDef("yg_sk_niyan", "逆演前尘·时光回溯", 4, "时空律",
                        new[] { Passive("rewind_law", "本场限一次:弹出 KarmicIndex 栈顶一条结算撤销重演(伤害/夺运/胜负回滚,天谴债不回滚),成本 RetributionDebt+10、LifespanDebt+3、栈深-1;法则回溯可指定回滚栈中任一条(spaceTimeAuth≥4)") }),
                    new ArtDef("yg_sk_qiulong", "时空囚笼·须弥困界", 4, "时空律",
                        new[] { Passive("spacetime_cage", "布时空囚:目标被囚一回合(移动/瞬移/逃遁失效=强控),己方获一次免干扰布法窗口,成本 RetributionDebt+8;对体修/驭兽群战拉开拖延的克制位(spaceTimeAuth≥4)") }),
                    new ArtDef("yg_sk_changhe", "时空律·长河逆流", 5, "时空律",
                        new[] { Passive("river_reverse", "合道态每回合开局自动'微回溯':上一回合本方净亏则以 RetributionDebt+5 把该回合一半损失(整除)勾销;RetributionDebt≥60 时此被动反噬(损失翻倍回灌)(spaceTimeAuth≥6)") }),
                }),
                // 类目三·命运律 destiny_law（改命/窥天机/夺定数,绑 destinyAuth,本路对外泛克兑现层,亦天谴高发区）。
                new ArtCategoryDef("命运律", "destiny", 1, 1, new[]
                {
                    new ArtDef("yg_mw_kuitian", "窥天机·命盘垂象", 2, "命运律",
                        new[] { Passive("peek_destiny", "读目标'定数':必先手并使本方对其'改命'反弹 reflect 减半,成本 RetributionDebt+4;读的是规则层定数非走位,产出'改命许可'(destinyAuth≥1)") }),
                    new ArtDef("yg_mw_jieming", "夺定数·截命改运", 3, "命运律",
                        new[] { Passive("seize_destiny", "强改目标本场一项'定数':将其 EffectivePower 按 destinyAuth×3 比例削减并计入本方,成本 RetributionDebt+8;对高实力低气运者泛克(destinyAuth≥3)") }),
                    new ArtDef("yg_mw_niming", "逆天改命·我命由我", 4, "命运律",
                        new[] { Passive("defy_destiny", "改本方'定数':本场一次'必败/必死'结算强翻为'不该如此'(一次保命/翻盘);撞大气运/天命者时 reflect=对方气运-己权限 全额打入本方 RetributionDebt+LifespanDebt(强逆命大者自险)(destinyAuth≥4)") }),
                    new ArtDef("yg_mw_dingshu", "命运律·定数归一", 5, "命运律",
                        new[] { Passive("destiny_unify", "合道态下本方'净法则压制'为正时把超出部分 1/4(整除)额外计入 EffectivePower(后期超线性起飞主源,本路'厚积晚发'尾),每触发一次 RetributionDebt+3(destinyAuth≥6)") }),
                    new ArtDef("yg_mw_duotian", "一指定生死·夺天之数", 5, "命运律",
                        new[] { Passive("seize_heaven_number", "终结技基底:仅当本方三律权限阶之和高于目标'气运/命格'整数评估时可发,直接将目标本场定数判负(近乎必杀已被压制者);条件不满足则发动即天谴全额反噬入己(RetributionDebt+30);合道且 destinyAuth≥7 入池") }),
                }),
                // 类目四·承负 forbearance（还债/抗反噬/无为定心,把高方差贷款博弈对冲成可控,降本路雪崩风险）。
                new ArtCategoryDef("承负", "forbearance", 1, 1, new[]
                {
                    new ArtDef("yg_cf_huanzhai", "承负还债诀", 1, "承负",
                        new[]
                        {
                            new EffectOp(EffectOpKind.AddResource, "retributionDebt", -8, "主动还债:以一回合'无为不施'换 RetributionDebt-8(积功德式偿还),本路唯一常驻压债手段(强制 brake/forbearance)"),
                        }),
                    new ArtDef("yg_cf_wuxiang", "太上忘情·无相护道", 2, "承负",
                        new[] { Passive("formless_guard", "无相:本场免疫敌方'同类法则'的因果回溯/夺定数(无因果可循、无定数可夺,对应克制网纯粹无相者自保位),但本方'命运律'夺取项-1阶(守换攻)") }),
                    new ArtDef("yg_cf_zhuanyi", "天谴转移·承负分劫", 3, "承负",
                        new[] { Passive("debt_defer", "把本回合将记的 RetributionDebt 暂存'承负阵'(延迟3回合结算)给透支动法则开窗;阵破则天谴债双倍回灌(高风险开窗)") }),
                    new ArtDef("yg_cf_zhendao", "镇道安神·承负护体", 4, "承负",
                        new[] { Passive("backlash_shield", "受法则反噬/Backlash 时以 RetributionDebt-(实付)抵消最多 reflect 的一半(整除),防'逆命大者被一击打穿';daoFirm 时抵消上限+1档") }),
                    new ArtDef("yg_cf_dunqu", "大道无为·遁去其一", 5, "承负",
                        new[]
                        {
                            new EffectOp(EffectOpKind.AddResource, "retributionDebt", -40, "保命底牌:天谴债跨 RETRIB_CALAMITY=80 濒临反噬死时自动触发一次,本场限一次:清空 RetributionDebt 至40(以约-40近似清至40)、本场三律权限阶各-1续命(对冲债务雪崩),并清空本场 KarmicIndex 栈"),
                            Passive("last_ditch_void", "保命底牌触发标志(本场限一次;三律阶各-1续命+清空因果栈为战斗期结算,A.0 被动占位)"),
                        }),
                }),
                // 类目五·道心 fateheart_law（role=daoheart,补遗第七部同源「承负不惧心」,与上四类并列追加）。
                // A.0 仅装载不结算 → tier=0(sumArtPower 贡献 0)、effects 留空(不触 daoHeart/innerDemon/comprehension 资源算子,
                // 那是 A.2 道心层的事)。具名 + power=0。禁在此写 AddResource(daoHeart/innerDemon)(key 不在 Resources 字典会崩且违 R3)。
                new ArtCategoryDef("道心", "daoheart", 1, 1, new[]
                {
                    new ArtDef("dh_yg_chengfu", "承负安神诀", 0, "道心", System.Array.Empty<EffectOp>()),
                    new ArtDef("dh_yg_wuwei", "太上无为定心录", 0, "道心", System.Array.Empty<EffectOp>()),
                    new ArtDef("dh_yg_buju", "知法不惧心经", 0, "道心", System.Array.Empty<EffectOp>()),
                    new ArtDef("dh_yg_woming", "我命由我·承负归一心", 0, "道心", System.Array.Empty<EffectOp>()),
                }),
            };

            // —— 战技（设计 §③ 战技池,OnUse 算子 + Cost 资源表;消耗以 RetributionDebt 先付费为主,叠 lifespanDebt/律权限阶）。
            //    伤害/撤销重演/改定数等具体结算 Phase 3 接,A.0 以 AddPenInteger 近似整数破防量占位 + Cost 表达资源门槛。
            //    本路成本货币=retributionDebt(用法即付费的天谴费),故 Cost 多以其为主键 + lifespanDebt。——
            var skills = new[]
            {
                // 一念抹因[burst]：开战即结算,抹除目标本回合全部'因'(counterAdj/先手/Tempo 价值归零)并按 causalAuth 从因果栈弹一条还施其身。最强先手'未战先胜'。
                new CombatSkillDef("sk_yg_moyin", "一念抹因", 5,
                    new[] { new EffectOp(EffectOpKind.AddPenInteger, null, 36, "抹除目标本回合全部'因'(其 counterAdj/先手/Tempo 价值归零)并按 causalAuth 从因果栈弹一条还施其身,法则修士最强先手技") },
                    new Dictionary<string, int> { { "retributionDebt", 12 }, { "lifespanDebt", 3 } }),
                // 时光回溯·逆演[control]：本场限一次,撤销 KarmicIndex 栈中指定一条交锋结算(伤害/夺运/胜负回滚),天谴债不回滚。规则纠错'重来一手'。
                new CombatSkillDef("sk_yg_niyan", "时光回溯·逆演", 4,
                    new[] { new EffectOp(EffectOpKind.AddPenInteger, null, 0, "撤销 KarmicIndex 栈中指定一条交锋结算(伤害/夺运/胜负回滚),天谴债不回滚(规则纠错重来一手),需 spaceTimeAuth≥4") },
                    new Dictionary<string, int> { { "retributionDebt", 10 }, { "lifespanDebt", 3 } }),
                // 夺定数·截命一击[burst]：对单体强改定数,削其 EffectivePower 的 destinyAuth×3% 并计入己;撞大气运者 reflect=对方气运-己权限 全额入己(自险)。
                new CombatSkillDef("sk_yg_jieming", "夺定数·截命一击", 4,
                    new[] { new EffectOp(EffectOpKind.AddPenInteger, null, 24, "对单体强改定数:削其 EffectivePower 的 destinyAuth×3%(整除)并计入己;撞大气运者 reflect=对方气运-己权限 全额入己 RetributionDebt+LifespanDebt(自险),需 destinyAuth≥3") },
                    new Dictionary<string, int> { { "retributionDebt", 8 } }),
                // 须弥困界[control]：布时空囚困目标一回合(移动/瞬移/逃遁/拉开失效),换己方一次免干扰布法窗口;对放风筝/群战拖延位的反制。
                new CombatSkillDef("sk_yg_kunjie", "须弥困界", 4,
                    new[] { new EffectOp(EffectOpKind.AddPenInteger, null, 12, "布时空囚困目标一回合(移动/瞬移/逃遁/拉开失效),换己方一次免干扰布法窗口;对放风筝/群战拖延位反制") },
                    new Dictionary<string, int> { { "retributionDebt", 8 } }),
                // 须弥遁影[escape]：'缩地'瞬移脱战/接战择一,遁后下一法则技 RetributionDebt 消耗-4。本体太脆时的活路(破'近身快攻'天敌)。
                new CombatSkillDef("sk_yg_dunying", "须弥遁影", 2,
                    new[] { new EffectOp(EffectOpKind.AddFlatDR, null, 8, "缩地瞬移脱战/接战择一,遁后下一法则技 RetributionDebt 消耗-4,本体太脆时的活路(破近身快攻天敌)") },
                    new Dictionary<string, int> { { "retributionDebt", 3 } }),
                // 承负清债·了断[brake]：主动以一回合无为不施 + 渡化场上一条恩怨边,换 RetributionDebt-15。本路核心刹车(强制对冲'越用越虚')。
                new CombatSkillDef("sk_yg_qingzhai", "承负清债·了断", 3,
                    new[]
                    {
                        new EffectOp(EffectOpKind.AddResource, "retributionDebt", -15, "主动以一回合无为不施换 RetributionDebt-15(本路核心刹车,强制对冲越用越虚)"),
                        new EffectOp(EffectOpKind.SetFlag, "purge_one_grudge", 1, "渡化场上一条恩怨边(近似 AdjustRelationEdge→A.0 置标志占位,完整渡化 Phase 3 接 v1.2 恩怨债)"),
                    },
                    new Dictionary<string, int>()),
                // 一指定数·绝命[burst]：终结技,仅己三律权限阶之和>目标气运/命格评估时可发,直接判目标本场定数为负(近乎必杀已压制者);条件不满足则发动即天谴全额反噬入己。
                new CombatSkillDef("sk_yg_jueming", "一指定数·绝命", 5,
                    new[] { new EffectOp(EffectOpKind.AddPenInteger, null, 50, "终结技:仅己三律权限阶之和>目标气运/命格评估时可发,直接判目标本场定数为负(近乎必杀已压制者);条件不满足则发动即天谴全额反噬入己,需 destinyAuth≥7 且 lawBound==1") },
                    new Dictionary<string, int> { { "retributionDebt", 30 }, { "lifespanDebt", 6 } }),
            };

            return new CultivationPathDef(
                "yinguo_faze", "因果时空命运修·大道法则",
                "fate",
                // 属性/形态 tag（spirit_attack 役使规则=神识/法则侧攻击非实体 / ranged 算计非贴脸·本体极脆近身被碾压 /
                // righteous 掌天机隐世正道传承非邪术），非对手 PathId（R2）。设计 §⑧ 的近身快攻/斩首天敌即由 ranged tag
                // 在情境边「远程克近战 / 放风筝」的对偶（melee 攻方克 ranged 守方）表达,零 PathId。
                new[] { "spirit_attack", "ranged", "righteous" },
                resources,
                power,
                curve,
                arts,
                skills,
                new EntryGateDef("tag:yinguo_root"),
                new SelectionRuleDef(2, 3), // 战技抽 2~3（设计 §⑨ 选取规则:再从战技池选 2–3 个,至少 1 个 escape/brake）
                null);
        }
    }
}
