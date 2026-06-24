using System.Collections.Generic;

namespace Jianghu.Cultivation.Paths
{
    /// <summary>
    /// 傀儡师·机关魁儡道 <c>kuilei_shi</c>（役使系·死物路线，对照剑修范式逐字段）。数据照《每路深度设计》
    /// 傀儡节（line 861-948）+《内容补遗》§役使三态。死物役使造物军团：本体藏阵后操偶，
    /// 战力根本不在四维而在「一份可增删的死物名册按带宽加权之和」(fleetWeighted×10)；
    /// 本体以悟性（机关推演/带宽核心，役使系「武力等价物」在脑不在拳）为首、内力（炼制供能/操控续航）次之；
    /// 武力/根骨几乎不进战力（本体藏阵脆皮、斩首即崩）。结构性免疫精神/毒/魅（undead_construct tag）、消耗战不喊累；
    /// 代价：怕雷符（雷克金属）、破阵纹、夺傀、斩首即崩。
    ///
    /// 红线落实：terms 无 ×0、无 daoHeart（R3/R6）——根骨×0 故不立根骨项，realm 乘性放大由 RealmCurve 承载；
    /// SituationalTags=属性/形态 tag 非对手 PathId（R2，undead_construct/melee/brute…）；RealmCurve 四列等长（M4）；
    /// 含 1 个 Role=daoheart 类目 匠心（M1，A.0 仅装载不结算 → tier=0 使 sumArtPower 贡献 0、effects 留空，
    /// **禁** daoHeart/innerDemon/comprehension 资源算子，那是 A.2 道心层的事）。canon pathId（R4）。纯整数，禁浮点。
    ///
    /// A.0 近似说明：mindBandwidth 过载摊薄 / constructTier 跟境折扣 / commandChain 断链 / fleetWeighted 派生求和
    /// 等真机制是 IDerivedProvider + 新 ModKind（L1，后续接）；A.0 不引入新算子，被动用核心
    /// AddResourceCap/AddResource/GrantPassive/SetFlag 近似（改四维/掷造概率/带宽数值等为 flavor 留 Note），
    /// 战技 OnUse 用 AddPenInteger 近似整数破防量占位 + Cost 资源门槛表达。
    /// </summary>
    public static class KuileiShiPath
    {
        public static readonly CultivationPathDef Def = Build();

        // —— 被动算子（装配期落 state，核心 EffectOpKind）。功法的「悟性+N/带宽+N/craftScore+N」等改四维或派生量
        //    A.0 为 flavor 不落算子（生成期 Σ=80 不被功法污染、派生量 A.0 不结算），仅以 Note 留痕；
        //    能落 state 的「军团/带宽上限+N」走 AddResourceCap、被动开关/抗夺傀走 GrantPassive/SetFlag。——
        private static EffectOp CapFleet(int amt, string note)
            => new EffectOp(EffectOpKind.AddResourceCap, "fleetWeighted", amt, note);

        private static EffectOp CapBandwidth(int amt, string note)
            => new EffectOp(EffectOpKind.AddResourceCap, "mindBandwidth", amt, note);

        private static EffectOp Passive(string flag, string note)
            => new EffectOp(EffectOpKind.GrantPassive, flag, 1, note);

        private static CultivationPathDef Build()
        {
            // —— per-path 专属资源（进 CultivationState.Resources，全整数，不进四维 C6/Σ=80）。深度设计 §941-947。——
            // fleetWeighted 傀儡军团（专属派生·战力主体，权 10）：A.0 单值起底 base cap=40
            //   （满带宽满名册理想值占位，真 Σ(constructPower×tierLagFactor) 经 OverloadDiscount/CommandSever 门控 = L1 IDerivedProvider）。
            // mindBandwidth 神识带宽（=悟性×2+realm×3，可同时操控傀儡上限）：A.0 base cap=16 起底，realm/悟性增益由操偶术 AddResourceCap 表达。
            // craftScore 炼制成功率（=悟性+realm×2+炼造法power）：A.0 base cap=100 占位。
            // residualOrder 残命惯性 [0,100]（断链存量，commandChain 断时由操偶术置初值、每 tick 衰减；为 0 则军团彻底僵死）。
            var resources = new[]
            {
                new ResourceDef("fleetWeighted", 0, 40, 0),
                new ResourceDef("mindBandwidth", 0, 16, 0),
                new ResourceDef("craftScore", 0, 100, 0),
                new ResourceDef("residualOrder", 0, 100, 0),
            };

            // —— 战力公式（深度设计 line 874 terms：悟性×4 + 内力×2 + realm×3 + 傀儡军团 fleetWeighted×10；
            //    根骨×0 显式置 0 → 红线禁 ×0 项故**不立根骨项**，本体藏阵脆皮的数值根源=根本不进战力；
            //    武力同理不进。悟性权重为本体主项（役使系「武力等价物」在脑），fleetWeighted 权 10 为战力主体）。
            //    无 daoHeart、无 ×0（R3/R6）。realm 乘性总闸门由 RealmCurve 承载，此处 realm 正权项=带宽/品阶解锁的线性贡献。——
            var power = new PowerFormulaDef(
                new[]
                {
                    new PowerTerm("stat:Insight", 4, null),       // 悟性：机关推演/阵纹刻绘/神识带宽核心，本路本体主项（脑不在拳）
                    new PowerTerm("stat:Internal", 2, null),      // 内力：炼制熔铸供能 + 操控傀儡持续神识灌注
                    new PowerTerm("realm", 3, null),              // 境界：解锁带宽 realm 项/可炼最高 constructTier/名册容量/链稳度
                    new PowerTerm("res:fleetWeighted", 10, null), // 傀儡军团：权重最高，战力主体在此（造得多≠强，由带宽过载门控，A.0 单资源近似）
                },
                System.Array.Empty<PowerMod>(),
                null);

            // —— 战力曲线（深度设计 line 882 realmMul=[1,2,4,7,11,18,28,40,55]，realm 0..8，厚积晚发+资源枢纽；
            //    地板极低、中段 r4→r5 小跳变（化神带宽大解、死物军团成军）、尾段收敛 ~1.4 略弱于驭兽顶）。
            //    四列等长（M4）：倍率 / UnifiedTierOf（UT0-12 映射：三流→二流→一流→绝顶→宗师→大宗师）/
            //    境界名（炼气→筑基→金丹→元婴→化神→炼虚→合体→大乘→机关之巅）/ 升入阈值（炼成本境标志傀儡累进，realm0=0 起）。——
            var curve = new RealmCurveDef(
                new[] { 19, 27, 32, 50, 78, 120, 185, 285, 450 }, // INV-CROSS v2r2: nerf -12~15% UT4+ (v2 was -18~22% over-nerf); UT8=1.70x sword (was 1.89x, v2=1.52x under)
                new[] { 0, 2, 4, 6, 8, 9, 10, 11, 12 },
                new[] { "炼气", "筑基", "金丹", "元婴", "化神", "炼虚", "合体", "大乘", "机关之巅" },
                // 突破须独力炼成本境标志傀儡（炼气→筑基须成功炼成并稳运一具青铜小甲整周期），里程累进，realm0=0 起。
                new[] { 0, 100, 300, 600, 1000, 1500, 2100, 2800, 3600 },
                // —— A.1 境界稿 §2：起步 SubLevelCount 全 1；CanAscend=true；MaxMajor=大境界数-1。——
                new[] { 1, 1, 1, 1, 1, 1, 1, 1, 1 }, true, 8);

            // —— 功法类目（炼造法/操偶术/机括学 各 5 具名 + 匠心 道心 4 具名 占位）。
            //    具名/效果照深度设计「功法类目」节 line 886-909。改四维/掷造概率/带宽数值为 flavor（Note）。——
            var arts = new[]
            {
                // 炼造法（傀儡炼制·造物之术·决定可炼 constructTier 与名册容量 → 军团来源，定 fleetWeighted 主体）。
                //   可炼品阶/craftScore/rosterCap 为 flavor（Note）；能落 state 的「军团上限抬高」走 AddResourceCap(fleetWeighted)。
                new ArtCategoryDef("炼造法", "craft", 1, 1, new[]
                {
                    new ArtDef("kl_lz_mojia", "墨甲入门图", 1, "炼造法",
                        new[] { CapFleet(4, "可炼constructTier≤2木甲/青铜小甲,craftScore基值+15,军团容量起底+4(最易得广适)") }),
                    new ArtDef("kl_lz_bailianjin", "百炼金人术", 2, "炼造法",
                        new[]
                        {
                            CapFleet(8, "可炼constructTier≤4金属傀儡,craftScore+30,军团上限+8"),
                            Passive("metal_construct", "金属傀儡对钝击/物理减伤额外+4(但更招雷符:招式越硬越怕雷)"),
                        }),
                    new ArtDef("kl_lz_miaojiang", "苗疆炼尸秘录", 3, "炼造法",
                        new[]
                        {
                            CapFleet(12, "可炼constructTier≤6尸傀(横死尸料),rosterCap+1,军团上限+12"),
                            // 尸傀不惧蛊毒+自带8穿透为战斗期 flavor(Note);邪修路 daoHeart不增/innerDemon增速+1 = A.2 道心层,A.0 不落算子。
                            Passive("corpse_construct", "尸傀不惧蛊毒且自带8穿透;邪修路(习之innerDemon增速+1,A.2结算)"),
                        }),
                    new ArtDef("kl_lz_huangquan", "黄泉血肉造物经", 4, "炼造法",
                        new[]
                        {
                            CapFleet(16, "可炼血肉傀儡(半生化可植机括),craftScore+25,军团上限+16"),
                            Passive("flesh_construct", "血肉傀儡吸所毁敌尸增constructPower(上限+20);苗疆黄泉墟邪法,镇玄司缉拿"),
                        }),
                    new ArtDef("kl_lz_luban", "鲁班百工·万械总图", 5, "炼造法",
                        new[]
                        {
                            CapFleet(22, "可炼constructTier≤8上古机关巨傀/机关城核,rosterCap+2,军团上限+22"),
                            // 顶梁巨傀 constructPower×120/100 为单具乘强,A.0 不结算单具,以最高军团上限近似;墨家正传顶法。
                            Passive("titan_construct", "所炼最高品阶傀constructPower额外×120/100(顶梁巨傀);墨家正传顶法镇玄司容许"),
                        }),
                }),
                // 操偶术（神识操控·一心多用·定 mindBandwidth 带宽上限/超载缓冲/断链残命惯性/抗夺傀）。带宽上限走 AddResourceCap(mindBandwidth)。
                new ArtCategoryDef("操偶术", "command", 1, 1, new[]
                {
                    new ArtDef("kl_co_eryong", "一心二用诀", 1, "操偶术",
                        new[]
                        {
                            CapBandwidth(4, "神识带宽+4;基础操控,超载时全军摊薄无缓冲"),
                            new EffectOp(EffectOpKind.SetFlag, "residualOrder_init", 20, "commandChain断则残命惯性初值=20(短窗口)"),
                        }),
                    new ArtDef("kl_co_fenliu", "神识分流法", 2, "操偶术",
                        new[]
                        {
                            CapBandwidth(8, "神识带宽+8;超载乘子下限抬高(摊薄不低于60/100留底)"),
                            Passive("bandwidth_floor60", "带宽完整度抬高=直接抬高fleetWeighted有效系数(摊薄下限60/100)"),
                        }),
                    new ArtDef("kl_co_jiushen", "九神操械印", 3, "操偶术",
                        new[]
                        {
                            CapBandwidth(12, "神识带宽+12"),
                            Passive("anti_construct_steal", "抗夺傀:被劫夺commandChain控制权时50%概率夺傀失败链不断"),
                        }),
                    new ArtDef("kl_co_canming", "残命惯性大法", 4, "操偶术",
                        new[]
                        {
                            // 残命惯性数值(置60/tick衰减12)是断链门 ModKind 参数(L1);A.0 以 SetFlag 标初值 + GrantPassive 抗斩首近似。
                            new EffectOp(EffectOpKind.SetFlag, "residualOrder_init", 60, "本体斩首瞬间触发:断链后残命惯性=60且每tick仅衰减12(长窗口)"),
                            Passive("decap_buffer", "克斩首脆性关键:断链应急留更长窗口(类比驭兽舍身护契于死物)"),
                        }),
                    new ArtDef("kl_co_wankui", "万傀归识御神诀", 5, "操偶术",
                        new[]
                        {
                            CapBandwidth(16, "神识带宽+16;全名册共享同一神识池,超载由识池补偿带宽+10/tick"),
                            // 「本体悟性每点额外+1带宽预算」=带宽派生公式抬阶,A.0 以最高带宽上限近似,真带宽随悟性结算 L1。
                            Passive("bandwidth_pool", "本体悟性每点额外+1带宽预算(役使死物流巅峰,A.0 以带宽上限近似)"),
                        }),
                }),
                // 机括学（阵纹机关·改装乘区·被动构件，改写炼制效率/跟境折扣/抗破阵纹与雷符，不单独成傀但放大整套体系）。
                //   乘区/折扣阈值/耐久为派生与战斗期 flavor(Note);藏身傀阵纹「在役上限+2」落 AddResourceCap(mindBandwidth) 近似突破名册硬顶。
                new ArtCategoryDef("机括学", "augment", 1, 1, new[]
                {
                    new ArtDef("kl_au_daoling", "导灵机括", 1, "机括学",
                        new[] { Passive("craft_easier", "craftScore+5(炼制更易成);constructTier跟境折扣阈值放宽(floor(realm*1.2)视为-1,缓朽甲贬值)") }),
                    new ArtDef("kl_au_kanglei", "抗雷符纹", 2, "机括学",
                        new[] { Passive("anti_thunder", "全名册傀儡受雷符/雷克金属的counterAdj负值减半(仍受±P0/4);专补死物怕雷硬伤") }),
                    new ArtDef("kl_au_guzhen", "固阵护甲纹", 2, "机括学",
                        new[] { Passive("array_armor", "傀儡阵纹耐久+30:被破阵纹/破阵眼拆链需额外命中1次(抗拆甲/抗符修阵修针对)") }),
                    new ArtDef("kl_au_zibao", "自爆机枢", 3, "机括学",
                        new[] { Passive("self_destruct_module", "任一傀儡可主动自爆造constructPower×200/100范围伤后移出名册(消耗战不喊累可自爆,资源换斩杀)") }),
                    new ArtDef("kl_au_cangshen", "藏身傀阵纹", 4, "机括学",
                        new[]
                        {
                            // 「在役傀儡上限+2」=突破名册硬顶 → 以带宽上限抬高近似(带宽=可同时操控上限);藏身缓本体脆皮为 flag。
                            CapBandwidth(2, "本体藏身傀儡阵后,被斩首/直取本体类战技命中需额外+1命中(缓本体脆皮非免疫);在役上限额外+2"),
                            Passive("hide_in_array", "本体藏阵:斩首/直取本体需额外+1命中(缓脆皮,仍非免疫)"),
                        }),
                }),
                // 匠心 道心类目（M1，深度设计 line 905-909「魁傀·造化之念」role=daoheart）。A.0 仅装载不结算 → tier=0
                // （sumArtPower 贡献 0）、effects 留空（**禁** daoHeart/innerDemon/comprehension 资源算子,守 INV-DECOUPLE,A.2 道心层接）。具名 + power=0。
                new ArtCategoryDef("匠心", "daoheart", 1, 1, new[]
                {
                    new ArtDef("dh_kl_jiangxin", "持械静心诀", 0, "匠心", System.Array.Empty<EffectOp>()),
                    new ArtDef("dh_kl_wuyi", "物我相照录", 0, "匠心", System.Array.Empty<EffectOp>()),
                    new ArtDef("dh_kl_bushe", "视傀不弃心经", 0, "匠心", System.Array.Empty<EffectOp>()),
                    new ArtDef("dh_kl_xinjiang", "心匠合一道心", 0, "匠心", System.Array.Empty<EffectOp>()),
                }),
            };

            // —— 战技（深度设计「战技」节 line 911-918，OnUse 算子 + Cost 资源表）。
            //    傀儡战力之和按带宽乘子集火、构件过载、自爆等具体结算 Phase 3 接，A.0 以 AddPenInteger 占位破防量 +
            //    Cost 表达资源门槛。门槛资源用 fleetWeighted（军团存量近似内力/构件消耗）——A.0 用已有资源表达门槛，
            //    真「内力中/高、构件过载、永久损失一具傀儡」语义 = L1（战斗结算 + roster 增删 IDerivedProvider）。——
            var skills = new[]
            {
                // 万傀齐攻 [t1]：全部在役傀儡同时扑击单一目标,本回合傀儡战力之和按带宽乘子全额集火一次(死物军团集火,精确无随机应变)。
                // B5 批2 招牌招迁移：占位 AddPenInteger(40) → Modules.PenFromResource(fleetWeighted,×1)（傀儡军团集火,
                //   带宽乘子全额=fleetWeighted 全额转伤,军团越强越痛、空册哑火真差分；Amount2=1 工厂保证 §15.6）。
                new CombatSkillDef("sk_kl_wankuiqi", "万傀齐攻", 1,
                    new[] { Modules.PenFromResource("fleetWeighted", 1, note:"傀儡军团集火单一目标,带宽乘子全额=fleetWeighted 全额计入一次集火伤害(死物军团集火)") },
                    new Dictionary<string, int> { { "fleetWeighted", 8 } }),
                // 傀附本体·临阵 [t2]：一具高品阶傀机括之力暂附本体,本体指挥项临时+悟性×3,弥补藏阵脆皮(近身自保/脱困;该傀本回合退出名册)。
                // B5扫尾 defer(红线A.8): 改本体自身 stat(指挥项+悟性×3)需 ApplyStatDelta(未建)→改stat→EPIC-COMBAT-FULLSTRUCT,保 AddPenInteger 占位。
                new CombatSkillDef("sk_kl_kuifu", "傀附本体·临阵", 2,
                    new[] { Modules.ModifyStat("self:Insight", 3, "本体指挥项+悟性×3(近身自保)") },
                    new Dictionary<string, int> { { "fleetWeighted", 6 } }),
                // 强令催动 [t2]：超频驱动指定傀,该傀constructPower×150/100持续3tick,结束后机括过载constructTier临时−1(透支构件换爆发)。
                // B5扫尾 defer(红线A.8): constructPower×倍率=逐傀派生量(非聚合 fleetWeighted 资源),真 per-construct derived 未建→EPIC-COMBAT-FULLSTRUCT,保 AddPenInteger 占位。
                new CombatSkillDef("sk_kl_qiangling", "强令催动", 2,
                    new[] { Modules.FlatPen(24, "指定傀constructPower×150/100持续3tick(constructPower derived→FULLSTRUCT defer),后机括过载constructTier临时−1(透支构件换爆发,死物不喊累会损耗)") },
                    new Dictionary<string, int> { { "fleetWeighted", 6 } }),
                // 机枢自爆·焚甲 [t4]：引爆一具傀造constructPower×300/100一次性范围爆发,该傀永久移出名册(资源换斩杀,消耗战收尾);不损其余傀。
                // B5扫尾 defer(红线A.8): constructPower×倍率=逐傀派生量(非聚合 fleetWeighted 资源),真 per-construct derived 未建→EPIC-COMBAT-FULLSTRUCT,保 AddPenInteger 占位。
                new CombatSkillDef("sk_kl_jishuzibao", "机枢自爆·焚甲", 4,
                    new[] { Modules.FlatPen(36, "引爆一具傀constructPower×300/100一次性范围爆发(constructPower derived→FULLSTRUCT defer),该傀永久移出名册(资源换斩杀);不损其余傀(死物无纽带网震荡)") },
                    new Dictionary<string, int> { { "fleetWeighted", 12 } }),
                // 镇魂不乱·钢令 [t3]：被音修乱兽/精神扰动笼罩时,钢令贯链——死物本免疫心智,本技额外把范围内被乱己方活体援军/契约兽拉回钢令节奏并清除被乱状态(反·乱兽)。
                new CombatSkillDef("sk_kl_zhenhun", "镇魂不乱·钢令", 3,
                    new[] { Modules.SituationalAdj(10, "钢令贯链:把范围内被乱己方活体援军/契约兽拉回节奏并清除被乱状态(死物路独有心智净土反制)") },
                    new Dictionary<string, int> { { "fleetWeighted", 4 } }),
                // 断链应急·影替傀 [t4]：本体将受致命/斩首打击时触发,指定影替傀代受并把本体瞬移出阵,阻止本体斩首→全军失令一次(保命技;与残命惯性大法联动彻底化解一次断链)。
                // B5 批2：断链应急(影替代受致命/瞬移本体/commandSevered 复位)是唯一档签名机制(SpecialModuleRegistry 派发) → batch3 Special,
                //   显式 deferred（红线 A.8 不静默,待批3 wiring 后补 Special 构造）,保 AddFlatDR(999)/SetFlag 占位。
                new CombatSkillDef("sk_kl_yingti", "断链应急·影替傀", 4,
                    new[]
                    {
                        Modules.Special("brokenChain", 1, 0, "断链应急:影替傀代受致命+瞬移本体+化解断链"),
                        Modules.FlatDR(40, "影替傀护体减伤"),
                    },
                    new Dictionary<string, int> { { "fleetWeighted", 10 } }),
                // 重整旗鼓·召傀归阵 [t1]：将散落/惯性僵立/被打断的傀强制召回阵中并重置阵位,恢复带宽乘子触发条件(被破阵纹打散后重整军团)。无资源门槛。
                new CombatSkillDef("sk_kl_chongzheng", "重整旗鼓·召傀归阵", 1,
                    new[]
                    {
                        new EffectOp(EffectOpKind.AddResource, "residualOrder", 10, "散落/惯性僵立/被打断的傀强制召回重置阵位,恢复带宽乘子触发条件(被破阵纹打散后重整)"),
                    },
                    new Dictionary<string, int>()),
            };

            return new CultivationPathDef(
                "kuilei_shi", "傀儡师·机关魁儡道",
                "physical",
                // 属性/形态 tag，非对手 PathId（R2）：undead_construct 死物军团（免精神/毒/魅、死物免精神边）/
                // melee 傀儡近身扑击 / brute 死物硬耗集火。
                new[] { "undead_construct", "melee", "brute" },
                resources,
                power,
                curve,
                arts,
                skills,
                new EntryGateDef("tag:kuilei_root"),
                new SelectionRuleDef(2, 3), // 战技抽 2~3（与役使系/范式选取规则一致）
                null);
        }
    }
}
