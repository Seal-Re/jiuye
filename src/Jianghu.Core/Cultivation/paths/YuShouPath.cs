using System.Collections.Generic;

namespace Jianghu.Cultivation.Paths
{
    /// <summary>
    /// 驭兽师·兽灵契主 <c>yu_shou</c>（役使系群战路）。数据照《每路深度设计》驭兽节 +
    /// 《内容补遗》第六部「12. 驭兽 yu_shou — 道心：人兽同心」+ 命名池驭兽条目。
    /// 役使系：本体（驭兽师肉身）极弱，战力主体为「兽群强度」抽象资源（rosterPower），
    /// 悟性为本体指挥之骨、内力供能、realm 开栏位/阵眼闸门；境界曲线低开、中段兽阵协同超线性、
    /// 后段本体弱封顶（厚积/枢纽型，低爆发高韧性、怕斩首断线——指挥链 leadership_chain tag 为命门）。
    ///
    /// 红线落实：terms 无 ×0、无 daoHeart（R3/R6）；SituationalTags=属性/形态 tag 非对手 PathId（R2）；
    /// RealmCurve 四列等长（M4）；含 1 个 Role=daoheart 类目 beastheart（M1，A.0 仅装载不结算 → tier=0
    /// 使 sumArtPower 贡献 0、effects 留空不触 daoHeart 资源算子）。canon pathId（R4）。纯整数，禁浮点。
    ///
    /// 与设计文档的显式偏差（诚实标注）：
    /// ① 设计 terms 列「根骨×0（显式置 0）」表达「本体弱、根骨不进战力」——但 R6 严禁 Weight==0 项。
    ///    本数据以「直接省略 Constitution 项」等价表达（不在公式 = 贡献 0），无 R6 违例。Note 留痕。
    /// ② 设计第三项 Σ(beastPower_i×bond_i/100) 是逐兽求和的派生量；A.0 DerivedRegistry 空（derived:* 返 0），
    ///    故落为单条「兽群强度」抽象整数资源 res:rosterPower（×10，战力主体），bond 单独成韧性资源。
    /// ③ 加 sumArtPower×2（范式标配，照剑修使所选功法计入战力 → arts load-bearing）；设计未显列但不违任何红线。
    /// </summary>
    public static class YuShouPath
    {
        public static readonly CultivationPathDef Def = Build();

        // —— 被动算子（装配期落 state，核心 EffectOpKind）。功法的「悟性+N/收服成功率+N/beastTier 上限+N」
        //    等改四维/收服判定项 A.0 为 flavor 不落算子（生成期 Σ=80 不被功法污染，深度设计「功法只加 power
        //    不改 Σ」），仅以 Note 留痕；能落 state 的「bond 上限+N」走 AddResourceCap、被动开关走
        //    GrantPassive/SetFlag、「兽群强度即时+N」走 AddResource。——
        private static EffectOp CapBond(int amt, string note)
            => new EffectOp(EffectOpKind.AddResourceCap, "bond", amt, note);

        private static EffectOp CapRoster(int amt, string note)
            => new EffectOp(EffectOpKind.AddResourceCap, "rosterPower", amt, note);

        private static EffectOp Passive(string flag, string note)
            => new EffectOp(EffectOpKind.GrantPassive, flag, 1, note);

        private static CultivationPathDef Build()
        {
            // —— per-path 专属资源（不进四维 C6）——
            // rosterPower 兽群强度：契约灵兽栏位战力之抽象整数和（Σ beastPower×bond/100 的 A.0 折算），战力主体；
            //   base cap=40（栏位/品阶随 realm 解锁 → 增益由功法 AddResourceCap 表达，A.0 单值起底）。
            // bond 纽带完整度 0..100：抗音修乱兽/毒蛊夺兽/斩首断线的纽带韧性，base cap=60（血契缔结 t1 起底，
            //   契约心法 AddResourceCap 抬至 80/90/100）。
            var resources = new[]
            {
                new ResourceDef("rosterPower", 0, 40, 0),
                new ResourceDef("bond", 0, 60, 0),
            };

            // —— 战力公式（深度设计 terms：悟性×4 + 内力×2 + 兽群强度×10 + realm×3；根骨项设计为 ×0 → 按 R6 省略，
            //    不写 stat:Constitution×0）。本体指挥项权重远低于兽群强度项（本体弱、战力库在兽不在拳）；
            //    realm 的乘性放大由 RealmCurve 承载（此处 realm 正权项=境界开栏位/阵眼/品阶闸门）。
            //    + sumArtPower×2（范式标配）。无 daoHeart、无 ×0（R3/R6）。——
            var power = new PowerFormulaDef(
                new[]
                {
                    new PowerTerm("stat:Insight", 4, null),      // 本体指挥/契约维系核心：bond 总预算、收服率、兽阵调度上限
                    new PowerTerm("stat:Internal", 2, null),     // 灵力/契约供能：催动战技、灌灵回 bond、维持兽阵持续消耗
                    new PowerTerm("res:rosterPower", 10, null),  // 兽群强度（专属资源）：战力主体，权重全路最高
                    new PowerTerm("realm", 3, null),             // 境界：解锁 slotCap/bond 预算/可契约品阶/阵眼数的闸门
                    new PowerTerm("sumArtPower", 2, null),       // 所选驭兽术/契约/兽阵/培育各功法 tier 之和
                    // 注：根骨（stat:Constitution）设计为 ×0「本体弱、根骨不进战力，只进寿命/抗反噬」——
                    //     R6 禁 Weight==0 项，故直接省略（缺席=贡献 0），等价表达「本体弱、被斩首则崩」的数值根源。
                },
                System.Array.Empty<PowerMod>(),
                null);

            // —— 战力曲线（深度设计 realmMul=[1,2,4,7,12,20,32,48,70]，realm 0..8，厚积/枢纽型）。
            //    倍率沿用设计原始整数（与剑修同一 ÷10 定点惯例；驭兽数值全程 < 剑/体/雷同阶 → 坐实「低 realm 段
            //    刻意 ≤ 高爆发路、前期可被一波带走」+「后段本体弱封顶、增速收敛非爆炸」，深度设计§曲线对比）。
            //    四列等长（M4）：倍率 / UnifiedTierOf（UT0-12 标准九阶仙侠映射）/ 境界名（炼气→…→驭灵之巅，
            //    顶阶别名「身周无兽而万兽可召」§6.4）/ 升入阈值（累进，realm0=0 起；设计「持兽总 power 达该阶门槛」
            //    的 A.0 占位累进，沿用标准里程，无设计具体数 → 占位标注）。——
            var curve = new RealmCurveDef(
                new[] { 1, 2, 4, 7, 12, 20, 32, 48, 70 },
                new[] { 0, 2, 4, 6, 8, 9, 10, 11, 12 },
                new[] { "炼气", "筑基", "金丹", "元婴", "化神", "炼虚", "合体", "大乘", "驭灵之巅" },
                // 升入第 i 境累进阈值 = Σ 100×(0..i-1)（占位：设计「持兽总 power 达该阶门槛」无具体数，沿用标准里程）。
                new[] { 0, 100, 300, 600, 1000, 1500, 2100, 2800, 3600 },
                // —— A.1 境界稿 §2：起步 SubLevelCount 全 1；CanAscend=true；MaxMajor=大境界数-1。——
                new[] { 1, 1, 1, 1, 1, 1, 1, 1, 1 }, true, 8);

            // —— 功法类目（驭兽术/契约御灵/兽阵役使/灵兽心得培育 各 5 具名 + beastheart 道心 4 具名）。
            //    具名/效果照深度设计「功法类目」节；命名与命名池驭兽条目同源。——
            var arts = new[]
            {
                // 驭兽术（压服·收服·扩军，决定可压服/收服品阶与可驭兽数 slotCap）。收服率/beastTier 上限为 flavor（Note）；
                //   能落 state 的「slotCap+N→兽群强度上限+N」走 CapRoster、顶梁兽强化走 GrantPassive。
                new ArtCategoryDef("驭兽术", "attack", 1, 1, new[]
                {
                    new ArtDef("yu_ys_bailing", "百兽听令诀", 1, "驭兽术",
                        new[] { Passive("subdue_t2", "压服 beastTier≤2 野兽,收服成功率基值+20;slotCap+0") }),
                    new ArtDef("yu_ys_fumo", "伏魔降兽印", 2, "驭兽术",
                        new[] { Passive("subdue_t4", "压服 beastTier≤4 凶兽,收服成功率+35;对狂化/被乱目标重压服 bond 回升+15/tick") }),
                    new ArtDef("yu_ys_wanshou", "万兽朝宗箓", 3, "驭兽术",
                        new[]
                        {
                            CapRoster(8, "压服 beastTier≤6,slotCap+1(额外栏位)→兽群强度上限+8"),
                            Passive("beast_goodwill", "同节点野兽被动好感+10,降低遭遇即战概率"),
                        }),
                    new ArtDef("yu_ys_zhensha", "镇煞慑灵符", 3, "驭兽术",
                        new[] { Passive("subdue_sha", "专压煞性/凶性高阶兽,收服成功率+30 且收服后该兽初始 bond+25(更稳不易脱契)") }),
                    new ArtDef("yu_ys_yulong", "上古驭龙真解", 5, "驭兽术",
                        new[]
                        {
                            CapRoster(16, "压服 beastTier≤8 上古凶兽,slotCap+2→兽群强度上限+16"),
                            Passive("topbeast_buff", "所驭最高品阶兽 beastPower 额外×120/100(顶梁兽强化)"),
                        }),
                }),
                // 契约·御灵（纽带·养魂，定每兽 bond 上限与纽带韧性、抗夺抗乱抗断线 → 落 AddResourceCap(bond)）。
                new ArtCategoryDef("契约御灵", "internal", 1, 1, new[]
                {
                    new ArtDef("yu_qy_xueqi", "血契缔结术", 1, "契约御灵",
                        new[] { Passive("contract_basic", "基础契约:单兽 bond 上限 60(资源起底);本体陨落兽群立即脱契(无缓冲)") }),
                    new ArtDef("yu_qy_xinhun", "心魂同契法", 2, "契约御灵",
                        new[]
                        {
                            CapBond(20, "bond 上限 60→80;提升纽带完整度=直接抬高 Σ(beastPower×bond/100) 有效系数"),
                            Passive("anti_charm", "被音修乱兽时 bond 仅砍 25/100(而非半数),抗扰+"),
                        }),
                    new ArtDef("yu_qy_jiuqiao", "九窍锁灵契", 3, "契约御灵",
                        new[]
                        {
                            CapBond(30, "bond 上限 60→90"),
                            Passive("anti_devour", "抗毒蛊夺兽:被夺兽时 50%概率纽带不断、夺兽失败"),
                        }),
                    new ArtDef("yu_qy_sheshen", "舍身护契大法", 4, "契约御灵",
                        new[] { Passive("contract_buffer", "本体被斩首瞬间触发:兽群 bond 不归零、改为每 tick 衰减 20(断线应急留窗,克斩首脆性)") }),
                    new ArtDef("yu_qy_wanling", "万灵归一御魂诀", 5, "契约御灵",
                        new[]
                        {
                            CapBond(40, "bond 上限 60→100(满)"),
                            Passive("soulpool_share", "全栏灵兽 bond 共享灵池,单兽被乱/被夺由灵池补偿 bond+10/tick;本体悟性每点额外+1 bond 预算"),
                        }),
                }),
                // 兽阵·役使（群战协同，定群战兽阵倍率与阵眼数 activeBeasts≥阵眼数才触发；单挑无加成、群战滚雪球）。
                //   倍率/阵眼数结算 Phase 3 接，A.0 以 GrantPassive 落阵法开关 flag + Note 留倍率，不在 power 硬编码。
                new ArtCategoryDef("兽阵役使", "movement", 1, 1, new[]
                {
                    new ArtDef("yu_zs_qunlang", "群狼撕咬阵", 1, "兽阵役使",
                        new[] { Passive("formation_wolf", "阵眼数2:在役兽≥2 时灵兽战力之和×110/100;阵亡一兽倍率不掉(冗余)") }),
                    new ArtDef("yu_zs_yanxing", "雁行围猎阵", 2, "兽阵役使",
                        new[] { Passive("formation_goose", "阵眼数3:在役兽≥3 时×125/100;对单体高手目标额外+10%(兽海拖死单体)") }),
                    new ArtDef("yu_zs_fengyong", "蜂拥蔽天大阵", 3, "兽阵役使",
                        new[] { Passive("formation_swarm", "阵眼数5:在役兽≥5 时×140/100;虫驭/海量低阶兽流专属,怕群体清场(AOE 清兽则倍率回落)") }),
                    new ArtDef("yu_zs_sixiang", "四象镇煞兽阵", 4, "兽阵役使",
                        new[] { Passive("formation_sixiang", "阵眼数4:×135/100 且阵中灵兽受音修乱兽影响减半(阵法稳心);需4只不同属性兽充阵眼") }),
                    new ArtDef("yu_zs_baiwang", "百兽朝王绝阵", 5, "兽阵役使",
                        new[] { Passive("formation_king", "阵眼数7:在役兽≥7 时×160/100;本体即中枢阵眼——本体被斩首则全阵当场崩散(高倍率高脆性,役使流命门)") }),
                }),
                // 灵兽心得·培育（挂载到具体某兽的成长心得,定该兽 beastPower 成长速度/上限 → 抬兽群强度上限/即时涨）。
                new ArtCategoryDef("灵兽培育", "growth", 1, 1, new[]
                {
                    new ArtDef("yu_py_weiyang", "喂养有方", 1, "灵兽培育",
                        new[] { Passive("breed_basic", "挂载兽每 tick 培育 beastPower+1,上限+0;最易得、广适") }),
                    new ArtDef("yu_py_lingliao", "灵料淬体", 2, "灵兽培育",
                        new[] { CapRoster(20, "挂载兽 beastPower 成长+2/tick 且上限+20(吃灵料解锁更高天花板)→兽群强度上限+20") }),
                    new ArtDef("yu_py_xuemai", "血脉返祖法", 3, "灵兽培育",
                        new[] { CapRoster(40, "挂载兽概率提升 beastTier(品阶+1),beastPower 上限+40→兽群强度上限+40(顶梁巨兽,蛮驭流)") }),
                    new ArtDef("yu_py_guchong", "蛊虫孵生术", 3, "灵兽培育",
                        new[] { Passive("breed_swarm", "挂载虫群类兽:每 tick 自繁殖等效数量+1(变相扩兽海),单只 power 低但总和涨(虫驭流核心)") }),
                    new ArtDef("yu_py_huaxing", "通灵化形诀", 5, "灵兽培育",
                        new[] { CapRoster(80, "挂载兽可化形(beastTier↑8 上限),beastPower 上限+80 且获独立施法→兽群强度上限+80(准元婴战力终局)") }),
                }),
                // beastheart 道心类目（M1，补遗第十二节「人兽同心」）。A.0 仅装载不结算 → tier=0（sumArtPower 贡献 0）、
                // effects 留空（不触 daoHeart/innerDemon/comprehension 资源算子,那是 A.2 道心层的事）。具名 + power=0。
                new ArtCategoryDef("驭心", "daoheart", 1, 1, new[]
                {
                    new ArtDef("dh_yu_tongqi", "同契安神诀", 0, "驭心", System.Array.Empty<EffectOp>()),
                    new ArtDef("dh_yu_qunxin", "群灵归心录", 0, "驭心", System.Array.Empty<EffectOp>()),
                    new ArtDef("dh_yu_buqi", "失兽不弃心经", 0, "驭心", System.Array.Empty<EffectOp>()),
                    new ArtDef("dh_yu_renshou", "人兽合一道心", 0, "驭心", System.Array.Empty<EffectOp>()),
                }),
            };

            // —— 战技（深度设计「战技」节，OnUse 算子 + Cost 资源表；兽群强度=rosterPower、纽带=bond）。
            //    集火/兽阵倍率/献祭等具体结算 Phase 3 接，A.0 以 AddPenInteger 占位破防量 + Cost 表达资源门槛/透支；
            //    回 bond/养主兽以 AddResource 表达。——
            var skills = new[]
            {
                // 群兽突袭：令全部在役灵兽同时扑击单一目标,本回合兽群战力按兽阵倍率全额计入一次集火(兽海集火)。bond 不变,内力中。
                // B5 批2 招牌招迁移：占位 AddPenInteger(24) → Modules.PenFromResource(rosterPower,×1)（全在役兽集火,
                //   兽群强度 rosterPower 全额计入转伤,兽群越强越痛、空册哑火真差分；Amount2=1 工厂保证 §15.6）。
                new CombatSkillDef("sk_yu_tuxi", "群兽突袭", 2,
                    new[] { Modules.PenFromResource("rosterPower", 1, note:"全在役兽集火单一目标,兽群强度(rosterPower)按当前兽阵倍率全额计入一次") },
                    new Dictionary<string, int>()),
                // 嗜血催狂：催动指定灵兽狂化,该兽 beastPower×150/100 持续3 tick,结束后 bond−20(透支纽带换爆发)。门槛 bond≥20。
                // B5扫尾 defer(红线A.8): beastPower×倍率=逐兽派生量(非聚合 rosterPower 资源),真 per-beast derived 未建→EPIC-COMBAT-FULLSTRUCT,保 AddPenInteger 占位。
                new CombatSkillDef("sk_yu_cuikuang", "嗜血催狂", 2,
                    new[] { new EffectOp(EffectOpKind.AddPenInteger, null, 18, "指定兽 beastPower×150/100 狂化3 tick(beastPower derived→FULLSTRUCT defer);透支纽带,结束 bond−20") },
                    new Dictionary<string, int> { { "bond", 20 } }),
                // 万兽齐鸣·镇魂：全在役兽齐吼,反制范围内音修乱兽/精神扰动并清我方被乱,bond 全体回+10(反·乱兽)。内力高。
                new CombatSkillDef("sk_yu_zhenhun", "万兽齐鸣·镇魂", 3,
                    new[]
                    {
                        new EffectOp(EffectOpKind.AddSituationalAdj, null, 0, "反制音修乱兽/精神扰动并清我方被乱状态"),
                        new EffectOp(EffectOpKind.AddResource, "bond", 10, "全体灵兽 bond 回+10(反·乱兽稳纽带)"),
                    },
                    new Dictionary<string, int>()),
                // 断线应急·影替：本体将受致命/斩首打击时触发,指定护主兽代受并瞬移本体出阵,阻止「斩首→全兽崩散」一次。门槛 bond≥15(护主兽当场脱契)。
                new CombatSkillDef("sk_yu_yingti", "断线应急·影替", 4,
                    new[] { new EffectOp(EffectOpKind.AddFlatDR, null, 999, "护主兽代受致命/斩首一次并瞬移本体出阵(克斩首脆性的保命技,近似全免一次)") },
                    new Dictionary<string, int> { { "bond", 15 } }),
                // 灵兽献祭·爆体：献祭一只契约灵兽,造成等于其 beastPower×300/100 的一次性范围爆发,该兽永久移出栏位。bond≥5(纽带网震荡−5)。
                // B5 批2：本招伤害=单兽 beastPower×300/100（逐兽派生量,非聚合 rosterPower）+ 多算子(loss rosterPower) →
                //   真 per-beast Σ derived 未建 → 显式 deferred FULLSTRUCT（红线 A.8 不静默）,保 FlatPen 占位破防量。
                new CombatSkillDef("sk_yu_xianji", "灵兽献祭·爆体", 4,
                    new[]
                    {
                        new EffectOp(EffectOpKind.AddPenInteger, null, 45, "献祭一兽,等于其 beastPower×300/100 范围爆发(beastPower×300=逐兽derived→FULLSTRUCT defer,该兽永久移出栏位)"),
                        new EffectOp(EffectOpKind.AddResource, "rosterPower", -10, "永久损失一只灵兽,兽群强度回落"),
                    },
                    new Dictionary<string, int> { { "bond", 5 } }),
                // 灵兽附身·夺魄：将一只高 bond 灵兽之力暂附本体,本体指挥项临时+悟性×3,弥补本体弱、用于近身自保/脱困。门槛 bond≥10(该兽本回合退出兽阵),内力高。
                // B5扫尾 defer(红线A.8): 改本体自身 stat(指挥项+悟性×3)需 ApplyStatDelta(未建)→改stat→EPIC-COMBAT-FULLSTRUCT(与傀儡'傀附本体'同构),保 AddPenInteger 占位。
                new CombatSkillDef("sk_yu_fushen", "灵兽附身·夺魄", 2,
                    new[] { new EffectOp(EffectOpKind.AddPenInteger, null, 12, "高 bond 灵兽之力暂附本体,本体指挥项临时+悟性×3(改self-stat→FULLSTRUCT defer;近身自保/脱困)") },
                    new Dictionary<string, int> { { "bond", 10 } }),
                // 召兽归阵：将散落/被乱/逃逸的灵兽强制召回阵中并重置阵位,恢复兽阵倍率触发条件(被打散后重整旗鼓)。内力低,无门槛。
                new CombatSkillDef("sk_yu_guizhen", "召兽归阵", 1,
                    new[] { new EffectOp(EffectOpKind.AddResource, "rosterPower", 6, "召回散落/被乱/逃逸灵兽并重置阵位,恢复兽阵触发条件(兽群强度回补)") },
                    new Dictionary<string, int>()),
                // 兽遁·闪：兽遁闪避（OnDefend）。需兽阵役使→门控。bond≥5。
                // B5扩21: Evade — 驭兽师借兽阵遁形闪避,Amount=22→22%减免。
                new CombatSkillDef("sk_yu_shoudun", "兽遁·闪", 2,
                    new[] { Modules.Evade(22, "兽遁闪避:22%来袭减免(需兽阵役使→门控)") },
                    new Dictionary<string, int> { { "bond", 5 } }),
            };

            return new CultivationPathDef(
                "yu_shou", "驭兽师·兽灵契主",
                "physical",
                // 属性/形态 tag（melee 本体近身指挥 / brute 兽海蛮力 / spirit_attack 契约御灵之力 /
                //   activeLiving 活体役使（兽群=活体,区别于死物傀儡/炼尸,可被乱兽/夺兽/反噬）/
                //   leadership_chain 指挥链（斩首脆弱命门,本体断线全兽崩散——属性 tag 非对手 PathId）），非对手 PathId（R2）。
                new[] { "melee", "brute", "spirit_attack", "activeLiving", "leadership_chain" },
                resources,
                power,
                curve,
                arts,
                skills,
                new EntryGateDef("tag:beast_root"),
                new SelectionRuleDef(2, 3), // 战技抽 2~3（深度设计选取规则,至少含 1 群体协同 + 1 断线应急/护主）
                null);
        }
    }
}
