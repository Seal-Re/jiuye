using System.Collections.Generic;

namespace Jianghu.Cultivation
{
    /// <summary>
    /// 音修·音律乐道（琴箫）<c>yin_xiu_yuedao</c>（spirit 范围软控 + 团队增益减益支援枢纽路）。
    /// 数据照《余 9 路深度设计》音修节 +《WorldBible 九野·内容补遗》音修条目。
    /// 以音律为法、声波直击神魂心境：攻心/控场/增益/疗伤四位一体，一曲同时作用全场。强在 AOE 心智软控与
    /// 持续团队 BUFF（战场指挥乐）、双形态「独奏软柿/合奏指挥若神」；弱在单点斩杀近乎为零、对无心识死物傀儡
    /// 结构性失效（DeadConstructImmune，全路最大死穴）、起调窗口可被近身破奏打断、斩首即全场律场坍塌。
    /// 主属性=悟性+内力双主权（弃肉身），武力仅防除零、根骨本应 ×0。
    ///
    /// 红线落实：terms 无 ×0、无 daoHeart（R3/R6）——深度设计「根骨×0」属 ×0 项，按 A.0 约定**整体不发该 term**
    /// （根骨不入战力=乐师弃炼肉身的签名机制，仅以 Note 留痕，对位鬼修「武力×0/realm×0 不发项」处理）。
    /// SituationalTags=属性/形态 tag 非对手 PathId（R2）；RealmCurve 四列等长（M4）；含 1 个 Role=daoheart 类目
    /// yueheart（M1，A.0 仅装载不结算 → tier=0 使 sumArtPower 贡献 0、effects 留空不触 daoHeart 资源算子）。
    /// canon pathId（R4）。纯整数，禁浮点。
    ///
    /// A.0 核心算子近似（核心 10 个 EffectOpKind，需 Note 说明的留给 L1 不自创）：
    /// - 「曲意场 / AOE 律场 ResonanceField」「FieldBreadthMul 群战广度乘子」「DeadConstructImmune 死物免疫」
    ///   皆是 spirit 律场范围乘子机制，核心集无范围/广度乘算子 → A.0 以 AddPenInteger（律场 debuff 整数占位，
    ///   量级对齐本路公式）+ Cost + Note 近似，真正的「按场上单位数叠加 / 对死物按 0」结算 Phase 3 接。
    /// - 「起调窗口 windup / fieldActive 总门」以 GrantPassive(flag) + AddResource(windupProgress) 近似落 state，
    ///   真正的「未起调则律场项整体置 0」是 PowerEngine 的 GateByFlag，A.0 公式不落 Modifier，Note 留痕。
    /// - 资源上限「qiYun 上限 +N」走 AddResourceCap；「resonance/qiYun 即时 +N」走 AddResource；被动开关走 GrantPassive。
    /// </summary>
    public static class YinXiuYuedaoPath
    {
        public static readonly CultivationPathDef Def = Build();

        // —— 被动算子（装配期落 state，核心 EffectOpKind）。功法的「悟性+N/内力+N」等改四维项 A.0
        //    为 flavor 不落算子（生成期 Σ=80 不被功法污染），仅以 Note 留痕；能落 state 的「qiYun 上限+N」
        //    走 AddResourceCap、「resonance/qiYun 即时+N」走 AddResource、被动开关走 GrantPassive。——
        private static EffectOp CapQiYun(int amt, string note)
            => new EffectOp(EffectOpKind.AddResourceCap, "qiYun", amt, note);

        private static EffectOp Passive(string flag, string note)
            => new EffectOp(EffectOpKind.GrantPassive, flag, 1, note);

        private static CultivationPathDef Build()
        {
            // —— per-path 专属资源（不进四维 C6）——
            // qiYun 乐韵：律场/战技弹药（奏曲耗、应和/被动回，归零则律场断奏哑火）；cap=24（深度设计「24+6×realm」
            //   的 realm0 基线，realm/心法增益由 AddResourceCap 表达，A.0 单值起底）。
            // resonance 心弦共鸣 [0,16]：律场对当前敌群渗透深度，仅 fieldActive=1 计入战力；采风/试音预升、对硬心志封顶。
            // instrumentTier 本命乐器品阶 [0,9]：律场上限与 postMul 跟境门（仅供奏律不供御敌），A.0 单值起底。
            // windupProgress 起调进度 [0,..]：每步 +tempo 累积到 windupCost 点亮 fieldActive，被近身破奏清零（硬窗口）。
            var resources = new[]
            {
                new ResourceDef("qiYun", 0, 24, 0),
                new ResourceDef("resonance", 0, 16, 0),
                new ResourceDef("instrumentTier", 0, 9, 0),
                new ResourceDef("windupProgress", 0, 100, 0),
            };

            // —— 战力公式（深度设计 terms：悟性×3 + 内力×3 + realm×3 + 所选曲谱power×2 + resonance×3 + qiYun×1
            //    + 武力×1 + 根骨×0）。根骨×0 按约定**不发 term**（R6 禁 ×0；根骨不进战力=弃肉身签名机制）。
            //    悟性/内力并列双主权（音修「悟性+内力」双驱，不靠肉身），武力×1 仅防除零（乐师单挑最弱）；
            //    resonance×3 + qiYun×1 是律场弹药×渗透二元项（深度设计仅 fieldActive=1 计入，GateByFlag 是 PowerEngine
            //    的事，A.0 公式不落 Modifier、Note 留痕，qiYun/4 整除的除法定标同属 Phase 3）。无 daoHeart、无 ×0（R3/R6）。——
            var power = new PowerFormulaDef(
                new[]
                {
                    new PowerTerm("stat:Insight", 3, null),     // 悟性：通乐理/悟意境/谱曲共鸣调度之根，本路主属性
                    new PowerTerm("stat:Internal", 3, null),    // 内力：以内力鼓荡音波/护己心神/为律场续奏供能，与悟性并列双主权
                    new PowerTerm("realm", 3, null),            // 境界：决定可起调最高曲阶/律场覆盖广度/可叠律场数
                    new PowerTerm("sumArtPower", 2, null),      // 所选曲谱/乐章/心法/乐器 tier→power 之和（律场基础威力）
                    new PowerTerm("res:resonance", 3, null),    // 心弦共鸣：律场对敌群渗透深度（仅 fieldActive=1 计，GateByFlag Phase 3 接）
                    new PowerTerm("res:qiYun", 1, null),        // 乐韵：律场弹药（深度设计 qiYun/4，A.0 raw 占位，除法定标 Phase 3 接）
                    new PowerTerm("stat:Force", 1, null),       // 武力：御器/身法自保的近乎摆设权重，仅防除零（乐师单挑最弱）
                },
                System.Array.Empty<PowerMod>(),
                null);

            // —— 战力曲线（深度设计 realmMul=[10,13,17,23,31,42,57,78,106]，realm 0..8，单调微增·近匀速偏凸·加速温和，
            //    无剑修式末段陡尾；封顶倍率 10.6 落支援带）。四列等长（M4）：
            //    倍率 / UnifiedTierOf（UT0-12：不入流→二流→一流→后天巅峰→先天宗师→大宗师→绝顶→天人→声入道）/
            //    境界名（炼气习律→筑基开嗓→金丹凝乐韵→元婴乐婴→化神声入万法→炼虚→合体→大乘→乐道宗师声入道）/
            //    升入阈值（识律→通感→谱意→声入道四阶心路累进，realm0=0 起；演成本境标志曲稳运全场方破境，稳健渐进）。——
            var curve = new RealmCurveDef(
                new[] { 14, 19, 22, 33, 51, 78, 121, 191, 305 }, // INV-CROSS v2: nerf -18~22% UT4+; UT8=1.11x sword (was 1.39x)
                new[] { 0, 2, 4, 6, 8, 9, 10, 11, 12 },
                new[] { "炼气", "筑基", "金丹", "元婴", "化神", "炼虚", "合体", "大乘", "乐道宗师" },
                // 演成本境标志曲并稳运全场不被破奏方破境（≥90×当前realm 升阶，平滑无跳变，呼应近匀速曲线）。
                new[] { 0, 90, 270, 540, 900, 1350, 1890, 2520, 3240 },
                // —— A.1 境界稿 §2：起步 SubLevelCount 全 1；CanAscend=true；MaxMajor=大境界数-1。——
                new[] { 1, 1, 1, 1, 1, 1, 1, 1, 1 }, true, 8);

            // —— 功法类目（曲谱/乐章/心法/乐器身法 各 5 具名 + yueheart 道心 4 具名）。
            //    具名/效果照深度设计「功法类目」节；命名与命名池音修条目同源。——
            var arts = new[]
            {
                // 曲谱（杀曲/惑曲·律场输出与控制源，attack_dimension=spirit 主招式位）。律场 debuff 改临场 spirit 状态
                // A.0 为 flavor(Note)，仅悟性/内力改四维项不落算子；resonance/qiYun 即时增益落 AddResource。
                new ArtCategoryDef("曲谱", "attack", 1, 1, new[]
                {
                    new ArtDef("yin_qp_yinshang", "引商刻羽·乱心调", 1, "曲谱",
                        new[] { Passive("field_swaymind", "fieldActive时对全场敌方有效战力×(16−resonance/2)/16(基础乱心律场,resonance半档,入门AOE软控)") }),
                    new ArtDef("yin_qp_guangling", "广陵止息·杀伐音", 3, "曲谱",
                        new[] { Passive("field_killtone", "对全场敌方每步真伤=悟性/4(spirit绕物防),其行动判定−4(中阶杀控合一,琴中藏剑意)") }),
                    new ArtDef("yin_qp_mohedoule", "摩诃兜勒·惑神曲", 3, "曲谱",
                        new[] { Passive("field_confuse", "fieldActive时敌方可释战技数−1;对心志薄弱(心境壁<本人悟性)目标额外行动判定−6(专控低修群体)") }),
                    new ArtDef("yin_qp_gaoshan", "高山流水·正心遏邪", 4, "曲谱",
                        new[] { Passive("anti_evil_music", "对全场阴邪tag(ghost/demon/blood/gu)真伤×3/2并清我方被乱状态(以正乐遏邪,与雷儒横向破邪呼应,正向克邪非自堕)") }),
                    new ArtDef("yin_qp_tianmo", "天魔解体·噬心大乐", 5, "曲谱",
                        new[]
                        {
                            // [魔音分支] 杀伤暴涨以 daoHeart 为抵押：daoHeart<50 起调即 innerDemon 暴涨走火（魔音门是 A.2 道心层判定，
                            // A.0 仅 flag 留痕不触 daoHeart/innerDemon 资源算子，守 R3）。
                            Passive("demon_music", "[魔音]对全场敌方真伤=悟性/2+内力/3且乱心系数升为×(12−resonance)/16(更狠);须daoHeart≥50起调否则反噬走火'天魔入心'(杀伤与道心同杠杆,门判A.2)"),
                        }),
                }),
                // 乐章（增益/疗愈·团队 BUFF 源，本路签名类目·覆盖全场友方持续 BUFF 律场）。团队增益改临场状态 A.0 为 flavor(Note)，
                // qiYun 上限增益落 AddResourceCap、即时资源落 AddResource。
                new ArtCategoryDef("乐章", "movement", 1, 1, new[]
                {
                    new ArtDef("yin_lz_qinwang", "秦王破阵·鼓舞乐", 2, "乐章",
                        new[] { Passive("buff_inspire", "fieldActive时全场友方EffectivePower+mindResonance/8(mindResonance=悟性+resonance×2),人越多增益总价值越高(团队鼓舞底盘)") }),
                    new ArtDef("yin_lz_yangguan", "阳关三叠·护心调", 3, "乐章",
                        new[] { Passive("buff_wardmind", "全场友方获'护心':抵御敌方音修乱心/魂修摄魂/精神控制一次/场,且被克counterAdj负值减半(团队精神护盾,反·软控)") }),
                    new ArtDef("yin_lz_nichang", "霓裳羽衣·愈心曲", 3, "乐章",
                        new[] { Passive("buff_heal", "全场友方每步回内力+6、qixue/魂力等本命资源按realm折算回升(持续疗伤续航,'疗心修复神魂'团队版)") }),
                    new ArtDef("yin_lz_shimian", "十面埋伏·战阵乐", 4, "乐章",
                        new[] { Passive("buff_warmarch", "[军乐]全场友方可释战技数+1且Tempo/起手判定+1档(大规模军团增益/士气操控,军伍战阵指挥乐)") }),
                    new ArtDef("yin_lz_fengqiuhuang", "凤求凰·和鸣大乐", 5, "乐章",
                        new[]
                        {
                            new EffectOp(EffectOpKind.AddResource, "resonance", 2, "和鸣共振开场即铺:resonance+2(同时增益友方)"),
                            Passive("field_cap_up", "本人同时在场律场上限+1(叠'杀曲+护心+鼓舞'三律齐奏的支援巅峰,和鸣共振)"),
                        }),
                }),
                // 心法（乐心内功·乐韵续航与心境根基，常驻内功位）。抬 qiYun/resonance 上限、悟性/内力折算、抗反噬。
                // qiYun 上限增益落 AddResourceCap、即时回充落 AddResource、resonance 项权重台阶落 AddTermWeightStep。
                new ArtCategoryDef("心法", "internal", 1, 1, new[]
                {
                    new ArtDef("yin_xf_ningyun", "凝韵吐纳诀", 1, "心法",
                        new[]
                        {
                            CapQiYun(4, "内力+2;qiYun上限+4"),
                            new EffectOp(EffectOpKind.AddResource, "qiYun", 2, "每场战后qiYun额外回+2(续航底盘)"),
                        }),
                    new ArtDef("yin_xf_tonggan", "通感清心经", 2, "心法",
                        new[]
                        {
                            new EffectOp(EffectOpKind.AddResourceCap, "resonance", 2, "悟性+2,内力+2;resonance上限+2(更深渗透)"),
                            Passive("anti_interrupt", "被近身打断奏乐时有概率不断奏(抗破奏)"),
                        }),
                    new ArtDef("yin_xf_taiyin", "太音希声心典", 3, "心法",
                        new[]
                        {
                            CapQiYun(8, "内力+5;qiYun上限+8"),
                            Passive("field_linger1", "'余韵不散':律场被打断/停奏后仍残留1步spirit效果(续奏韧性)"),
                        }),
                    new ArtDef("yin_xf_cihang", "慈航普度·静念禅乐", 4, "心法",
                        new[] { Passive("hard_block_once", "内力+6,根骨+3(乐师罕见肉身兜底);被击破奏时qiYun≥10可硬抗一次致命/打断(每境界1次,护奏保命)") }),
                    new ArtDef("yin_xf_dayue", "大乐与天地同和经", 5, "心法",
                        new[]
                        {
                            CapQiYun(12, "悟性+5,内力+5;qiYun上限+12"),
                            // 声入道后心法 power 使 resonance 项权重再 +1 阶（AddTermWeightStep 抬 resonance 项权重台阶）。
                            new EffectOp(EffectOpKind.AddTermWeightStep, "resonanceStep", 1, "声入道后resonance项权重+1阶(登顶律场天花板,'声入万法')"),
                        }),
                }),
                // 乐器·身法（御器走位·脆皮容错与起调先手，本路独有混合类目·本命乐器驾驭 + 御音遁形）。
                // instrumentTier/qiYun/resonance 即时增益落 AddResource、被动开关落 GrantPassive。
                new ArtCategoryDef("乐器身法", "swordwill", 1, 1, new[]
                {
                    new ArtDef("yin_qs_jiaowei", "焦尾·灵韵琴", 1, "乐器身法",
                        new[]
                        {
                            new EffectOp(EffectOpKind.AddResource, "instrumentTier", 1, "instrumentTier视作+1(律场上限抬升)"),
                            new EffectOp(EffectOpKind.AddResource, "qiYun", 1, "本命乐器每步被动回qiYun+1(续奏弹药源)"),
                        }),
                    new ArtDef("yin_qs_raoliang", "绕梁御音步", 2, "乐器身法",
                        new[] { Passive("evade_play", "闪避+5;可保持奏曲同时位移至安全位,起调期被贴脸破奏判定−(边奏边走,缓解'奏乐易被打断')") }),
                    new ArtDef("yin_qs_changhong", "长虹贯日·御音飞遁", 3, "乐器身法",
                        new[]
                        {
                            new EffectOp(EffectOpKind.AddResource, "resonance", 1, "解锁'曲意离体':律场越障覆盖后排/远端节点,起调首步即附resonance+1(远程铺场)"),
                            Passive("field_remote", "内力+3;曲意离体远程铺场"),
                        }),
                    new ArtDef("yin_qs_dayinxisheng", "大音稀声·凝弦罩", 4, "乐器身法",
                        new[] { Passive("evade_must_once", "闪避+7;qiYun≥10时每场可'凝弦一震'强制规避一次必中近身打断(专破刺客断奏套路)") }),
                    new ArtDef("yin_qs_wanlai", "万籁俱寂·声临天下", 5, "乐器身法",
                        new[]
                        {
                            new EffectOp(EffectOpKind.AddResource, "qiYun", 5, "开战首步qiYun+5(先声夺人的支援起手)"),
                            new EffectOp(EffectOpKind.AddResource, "resonance", 2, "开战首步resonance+2(一开场即铺满律场)"),
                            Passive("escape_pursue", "内力+6,悟性+3;脱离战场或追击必成"),
                        }),
                }),
                // yueheart 道心类目（M1，补遗音修「乐心道·心境」）。A.0 仅装载不结算 → tier=0（sumArtPower 贡献 0）、
                // effects 留空（不触 daoHeart/innerDemon/comprehension 资源算子,那是 A.2 道心层「定心抗反噬、镇魔音之噬」的事）。
                // 具名 + power=0。深度设计原案为 4 具名（PickMin/Max=1）。
                new ArtCategoryDef("乐心道", "daoheart", 1, 1, new[]
                {
                    new ArtDef("dh_yin_chijie", "持节守心诀", 0, "乐心道", System.Array.Empty<EffectOp>()),
                    new ArtDef("dh_yin_zhongzheng", "中正平和乐心录", 0, "乐心道", System.Array.Empty<EffectOp>()),
                    new ArtDef("dh_yin_buran", "不染天魔心经", 0, "乐心道", System.Array.Empty<EffectOp>()),
                    new ArtDef("dh_yin_dayin", "大音无形·乐我两忘道心", 0, "乐心道", System.Array.Empty<EffectOp>()),
                }),
            };

            // —— 战技（深度设计「战技」节，OnUse 算子 + Cost 资源表；弹药=qiYun 乐韵）。
            //    律场 AOE 软控/团队 BUFF/范围乘子 Phase 3 接，A.0 以 AddPenInteger 占位 spirit 律场量级（对齐本路公式）
            //    + Cost 表达资源门槛 + Note 说明范围/死物免疫语义。强制选取规则（≥1 开场类 + ≥1 反制/护奏类）匹配
            //    本路「怕打断、怕斩首」的脆性——音修签名选取规则（深度设计选取规则⑥）。——
            var skills = new[]
            {
                // 起调·点律：把当前节点已蓄满 windupProgress 的曲/乐章全部点亮 fieldActive 0→1（开战核心动作,开场类）。qiYun≥8 + windupProgress 满。
                // B5扫尾: 占位 AddPenInteger(0) → FlatPen(0)（起调非伤害,开场flag-flip;律场总门 GateByFlag 由配套 GrantPassive(fieldActive) 表达,
                //   完整门控解锁 Phase3/批3 接,FlatPen(0) 为诚实非伤害占位）。
                new CombatSkillDef("sk_yin_qidiao", "起调·点律", 1,
                    new[]
                    {
                        Modules.FlatPen(0, "把已蓄满windupProgress的曲/乐章全部点亮:fieldActive0→1(律场总门GateByFlag),律场spirit效果解锁(开战核心动作,起调非伤害置0)"),
                        new EffectOp(EffectOpKind.GrantPassive, "fieldActive", 1, "fieldActive置1(律场总门,GateByFlag解锁 Phase3)"),
                    },
                    new Dictionary<string, int> { { "qiYun", 8 } }),
                // 急奏·短调：无视常规起调节奏本步立刻起调1首tier≤2曲(应急开场),该律场效果−30%、乐韵双倍耗(被偷家救场手,开场类)。qiYun≥18,消耗18。
                new CombatSkillDef("sk_yin_jizou", "急奏·短调", 2,
                    new[]
                    {
                        Modules.FlatPen(0, "无视常规起调节奏本步立刻起调1首tier≤2曲(应急开场),该律场效果−30%(被偷家时救场手,起调非伤害置0)"),
                        new EffectOp(EffectOpKind.GrantPassive, "fieldActive", 1, "fieldActive置1(应急点律)"),
                    },
                    new Dictionary<string, int> { { "qiYun", 18 } }),
                // 采风·试音：非伤害侦察,读敌群心境壁/心志档据此预升resonance+2(封顶受敌方硬心志限制)(开战前必备,反制/护奏类)。qiYun≥6,消耗6。
                new CombatSkillDef("sk_yin_caifeng", "采风·试音", 2,
                    new[]
                    {
                        new EffectOp(EffectOpKind.AddResource, "resonance", 2, "读敌群心境壁/心志档预升resonance+2(封顶受敌方硬心志限制,为后续律场渗透择机,对应魂修'望气探魂')"),
                    },
                    new Dictionary<string, int> { { "qiYun", 6 } }),
                // 裂石穿云·杀音爆：倾注律场为单点终极一击,对最高威胁者单点spirit真伤=悟性×2+内力×1+qiYun(绕物防),对死物傀儡/尸傀无效(本路罕见强单点仍受无心识免疫硬限)。qiYun≥16,清空一半。
                // B5 批2 招牌招迁移：占位 AddPenInteger(40) → Modules.PenFromResource(qiYun,×1)（悟性×2+内力×1+qiYun 绕物防,
                //   乐韵越满单点越痛、见底哑火真差分；Amount2=1 工厂保证 §15.6）。死物免疫硬限走 Phase 3 CounterMatrix。
                new CombatSkillDef("sk_yin_lieshi", "裂石穿云·杀音爆", 4,
                    new[] { Modules.PenFromResource("qiYun", 1, note:"对最高威胁者单点spirit真伤=悟性×2+内力×1+qiYun(qiYun乐韵转伤,绕物防);对死物傀儡/尸傀无效(无心识免疫硬限 Phase3)") },
                    new Dictionary<string, int> { { "qiYun", 16 } }),
                // 万籁齐鸣·镇场：全律场共振一震,对范围内敌'乱兽/扰心/破奏'反制并清我方被乱被控,全场友方resonance+2、qiYun回+5(反·软控、反·斩首断线,反制/护奏类)。qiYun≥12,消耗12净回。
                new CombatSkillDef("sk_yin_wanlai", "万籁齐鸣·镇场", 3,
                    new[]
                    {
                        Modules.FlatPen(12, "全律场共振一震,对范围内敌'乱兽/扰心/破奏'反制并清我方被乱被控状态(反·软控、反·斩首断线,与驭兽'万兽齐鸣·镇魂'互为镜像;反制/清控 Phase3)"),
                        new EffectOp(EffectOpKind.AddResource, "resonance", 2, "全场友方resonance应和+2"),
                        new EffectOp(EffectOpKind.AddResource, "qiYun", 5, "qiYun回+5"),
                    },
                    new Dictionary<string, int> { { "qiYun", 12 } }),
                // 高山流水·遏邪奏：[正乐]对全场阴邪tag(ghost/demon/blood/gu)spirit真伤×3/2并使其煞气/阴煞护体减免本场失效(横向破邪,counterAdj在外置矩阵本技只声明tag)。qiYun≥10,消耗10。
                // B5 批2 招牌招迁移：占位 AddPenInteger(16) → FlatPen(16) 基线 + Modules.CounterMul(evil,×3/2)（正乐破邪：
                //   防方带 evil tag(ghost/demon/blood/gu 阴邪)→×3/2,联合上界 §15.4；num=3 den=2 工厂 Amount2≥1 保证）。
                new CombatSkillDef("sk_yin_gaoshan", "高山流水·遏邪奏", 3,
                    new[]
                    {
                        Modules.FlatPen(16, "[正乐]全场阴邪 spirit 真伤基线破防量;使其煞气/阴煞护体减免本场失效"),
                        Modules.CounterMul("evil", 3, 2, note:"正乐破邪对阴邪tag(ghost/demon/blood/gu,evil)×3/2(横向破邪,counterAdj走外置CounterMatrix)"),
                    },
                    new Dictionary<string, int> { { "qiYun", 10 } }),
                // 续弦·应急：将被打断/被夺的律场强制重续,恢复fieldActive触发条件并回windupProgress至半(被破奏后重整旗鼓,反制/护奏类)。qiYun≥4,消耗4。
                new CombatSkillDef("sk_yin_xuxian", "续弦·应急", 1,
                    new[]
                    {
                        new EffectOp(EffectOpKind.AddResource, "windupProgress", 50, "被打断/被夺律场强制重续:恢复fieldActive触发条件并回windupProgress至半(被破奏后重整旗鼓,对应驭兽'召兽归阵')"),
                    },
                    new Dictionary<string, int> { { "qiYun", 4 } }),
                // 乐韵遁形·闪：乐韵遁形闪避（OnDefend）。需乐章→门控。qiYun≥3,消耗3。
                // B5扩21: Evade — 音修乐韵遁形闪避,Amount=28→28%减免。
                new CombatSkillDef("sk_yin_yuedun", "乐韵遁形·闪", 2,
                    new[] { Modules.Evade(28, "乐韵遁形闪避:28%来袭减免(需乐章→门控)") },
                    new Dictionary<string, int> { { "qiYun", 3 } }),
                // 迷魂引[control]：控场—目标下1回合无法行动。qiYun≥6,消耗6。
                // B5扩21: Control — 音修迷魂控场代表招。
                new CombatSkillDef("sk_yin_mihun", "迷魂引", 2,
                    new[] { Modules.Control("mihun", 1, "控场:目标下1回合无法行动(迷魂引)"), Modules.FlatPen(8, "迷魂音波 spirit 冲击") },
                    new Dictionary<string, int> { { "qiYun", 6 } }),
            };

            return new CultivationPathDef(
                "yin_xiu_yuedao", "音修·音律乐道",
                "spirit",
                // 属性/形态 tag（spirit_attack 声波绕物攻心 / ranged 律场覆盖远端 / righteous 正乐遏邪偏正道），
                // 非对手 PathId（R2）。本路对死物傀儡免疫由 tag 谓词在 CounterMatrix 表达（深度设计 DeadConstructImmune），
                // 不写入 SituationalTags（tag 非对手身份）。
                new[] { "spirit_attack", "ranged", "righteous" },
                resources,
                power,
                curve,
                arts,
                skills,
                // 21 路唯一 entry tag 约定：每路 entry tag = 唯一 <pathkey>_root。音修=yin_root。
                new EntryGateDef("tag:yin_root"),
                new SelectionRuleDef(2, 3), // 战技抽 2~3,其中 ≥1 开场类 + ≥1 反制/护奏类（深度设计音修选取规则⑥）
                null);
        }
    }
}
