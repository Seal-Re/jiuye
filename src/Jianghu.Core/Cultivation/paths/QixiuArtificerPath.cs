using System.Collections.Generic;

namespace Jianghu.Cultivation.Paths
{
    /// <summary>
    /// 器修·炼器师 <c>qixiu_artificer</c>（以宝御敌）。数据照《每路深度设计》器修节「以宝御敌」+
    /// 《内容补遗》第五部「5. 器修 qixiu_artificer — 道心：器我两忘心」+ 命名池器修条目。
    /// economic+physical：装备依附 + 本命法宝同构祭炼。力在器不在人——战力几乎全寄存于身外
    /// 【本命法宝品阶 itemTier】，肉身(武力)近乎废人；厚积晚发、前段全路最弱、中段「至宝压境」反超、
    /// 末段 itemTier 复利近指数；脱宝即崩、全路最脆（高方差低容错）。
    ///
    /// 红线落实：terms 无 ×0、无 daoHeart（R3/R6）——武力权重 1 而非 0 仅防除零（炼器师手无缚鸡）；
    /// SituationalTags=属性/形态 tag 非对手 PathId（R2，economic 攻坚维 + 落宝/缚器克外物依赖者）；
    /// RealmCurve 四列等长（M4）；含 1 个 Role=daoheart 类目 forgeheart（M1，A.0 仅装载不结算 →
    /// tier=0 使 sumArtPower 贡献 0、effects 留空不触 daoHeart/innerDemon/comprehension 资源算子）。
    /// canon pathId（R4）。纯整数，禁浮点。
    /// </summary>
    public static class QixiuArtificerPath
    {
        public static readonly CultivationPathDef Def = Build();

        // —— 被动算子（装配期落 state，核心 EffectOpKind）。功法的「悟性+N/craftScore+N/heat 上限+N」
        //    等炼器期数值 A.0 为 flavor 不落算子（生成期 Σ=80 不被功法污染，且 craftScore/heat 是祭炼子系统
        //    Phase 3 接），仅以 Note 留痕；能落 state 的「soulBond 上限+N」走 AddResourceCap、被动开关走
        //    GrantPassive/SetFlag、「有效 itemTier +N 视作加成」走 AddTermWeightStep 抬 itemTier 项台阶。——
        private static EffectOp CapSoulBond(int amt, string note)
            => new EffectOp(EffectOpKind.AddResourceCap, "soulBond", amt, note);

        private static EffectOp Passive(string flag, string note)
            => new EffectOp(EffectOpKind.GrantPassive, flag, 1, note);

        private static CultivationPathDef Build()
        {
            // —— per-path 专属资源（不进四维 C6）——
            // itemTier 本命法宝品阶：独立于人的整数 0..9，被 floor(realm*1.2)+1 硬封顶（炼器不得越境，
            //   封顶逻辑 Phase 3 祭炼子系统接），是 power 绝对主导项。藏宝阁认主给基线。
            // soulBond 器魂契合：滴血认主后体内祭炼累积 0..20；越高越榨得出威力、越抗被落宝夺器；归零=法宝罢工。
            var resources = new[]
            {
                new ResourceDef("itemTier", 0, 9, 0),
                new ResourceDef("soulBond", 0, 20, 0),
            };

            // —— 战力公式（深度设计 terms：itemTier×40 + 悟性×6 + soulBond×5 + 所选功法power×3 + realm×2 + 武力×1）。
            //    itemTier 权重 40 绝对主导（力在器不在人）；realm 仅微弱基底（低权+itemTier 高权→能越境战）；
            //    武力权重刻意最低=1（脱宝即弱，权重 1 而非 0 仅防除零）；无 daoHeart、无 ×0（R3/R6）。——
            var power = new PowerFormulaDef(
                new[]
                {
                    new PowerTerm("res:itemTier", 40, "itemTierStep"),  // 本命法宝品阶：绝对主导(聚纹/器灵 +阶走台阶)
                    new PowerTerm("stat:Insight", 6, null),             // 炼器悟性：祭炼推进速度+御宝精度，第二支柱
                    new PowerTerm("res:soulBond", 5, "soulBondStep"),   // 器魂契合：榨力/抗夺(心宝相照诀抬台阶)
                    new PowerTerm("sumArtPower", 3, null),              // 所选御宝/器纹/炼器各功法 tier 之和
                    new PowerTerm("realm", 2, null),                    // 境界：仅作微弱基底(供法力催宝)
                    new PowerTerm("stat:Force", 1, null),               // 肉身武力近乎废：脱宝即弱,权重 1 仅防除零
                },
                System.Array.Empty<PowerMod>(),
                null);

            // —— 战力曲线（深度设计 realmMul=[1,2,4,7,12,22,42,80,150]，realm 0..8，厚积晚发：前段平缓
            //    且常低于剑/体同阶(炼器投入期/宝滞后境界)、中段4起至宝压境陡增(12→22→42)反超、末段近指数)。
            //    四列等长（M4）：倍率 / UnifiedTierOf（UT0-12 三流→…→手中无器）/ 境界名（炼气-淬器→筑基-引纹→
            //    金丹-本命初成→元婴-器魂相生→化神-至宝压境→炼虚-万宝朝宗→合体-器道通玄→大乘-人器合一手中无器）/
            //    升入阈值（祭炼里程累进，realm0=0 起）。——
            var curve = new RealmCurveDef(
                new[] { 1, 2, 4, 7, 12, 22, 42, 80, 150 },
                new[] { 0, 2, 4, 6, 8, 9, 10, 11, 12 },
                new[] { "炼气·淬器", "筑基·引纹", "金丹·本命初成", "元婴·器魂相生", "化神·至宝压境", "炼虚·万宝朝宗", "合体·器道通玄", "大乘·人器合一", "手中无器" },
                // 祭炼里程 ≥100×当前realm 升阶 → 升入第 i 境累进阈值 = Σ 100×(0..i-1)（同剑修途径③累进式）。
                new[] { 0, 100, 300, 600, 1000, 1500, 2100, 2800, 3600 },
                // —— A.1 境界稿 §2：起步 SubLevelCount 全 1；CanAscend=true；MaxMajor=大境界数-1。——
                new[] { 1, 1, 1, 1, 1, 1, 1, 1, 1 }, true, 8);

            // —— 功法类目（炼器术/器纹学/御宝心法 各 5 具名 + 藏宝阁 7 具名 + forgeheart 器心 4 具名）。
            //    具名/效果照深度设计「功法类目」节；命名与命名池器修条目同源。——
            var arts = new[]
            {
                // 炼器术（core_forge·决祭炼/突破上限能力，必选——无炼器术则 itemTier 永封6）。
                // craftScore/heat/matGrade/runeCap 等炼器期数值为 flavor（Note，祭炼子系统 Phase 3 接）。
                new ArtCategoryDef("炼器术", "core_forge", 1, 1, new[]
                {
                    new ArtDef("qi_cf_bailian", "百炼基础锻器诀", 1, "炼器术",
                        new[] { Passive("forge_basic", "炼器craftScore+5;matGrade≤2不易炸炉(损耗系数100→50)") }),
                    new ArtDef("qi_cf_dihuo", "地火淬锋法", 2, "炼器术",
                        new[] { Passive("heat_cap_up", "火候heat上限+20(封顶80→100),热区命中craftScore+10") }),
                    new ArtDef("qi_cf_gengjin", "庚金玄玉炼宝术", 3, "炼器术",
                        new[] { Passive("rare_mat", "可掺稀材:matGrade有效值+1(最高5),itemTier单次祭炼进度+8") }),
                    new ArtDef("qi_cf_wancai", "万材归炉大法", 4, "炼器术",
                        new[] { Passive("multi_mat", "多元材料:craftScore+20且runeCap上调(matGrade*20→*25),更易出高器纹") }),
                    new ArtDef("qi_cf_xuantian", "玄天蜕器神通", 5, "炼器术",
                        new[]
                        {
                            Passive("tier_cap_break", "解锁itemTier 7→8→9上限突破(无此功法itemTier硬封顶6)"),
                            new EffectOp(EffectOpKind.AddResource, "itemTier", 1, "化神以上每次成功祭炼itemTier进度+15(A.0以+1阶占位)"),
                        }),
                }),
                // 器纹学（rune_socket·定本命法宝纹路特性,偏攻/偏夺/偏困/偏防）。
                // 「对手 itemTier/power 视作 -N」是 OnUse 御宝战技的事，此处为成品被动开关（落 flag）。
                new ArtCategoryDef("器纹学", "rune_socket", 1, 1, new[]
                {
                    new ArtDef("qi_rs_zhige", "止戈缚锋纹", 1, "器纹学",
                        new[] { Passive("rune_bind", "成品附'缚'纹:御宝战技命中后令对手itemTier/法术power视作-1(单回合)") }),
                    new ArtDef("qi_rs_juling", "聚灵导力纹", 2, "器纹学",
                        new[]
                        {
                            // 成品附'聚'纹:本命法宝有效 itemTier +1 用于 power 计算(等价+40 power)→抬 itemTier 项台阶。
                            new EffectOp(EffectOpKind.AddTermWeightStep, "itemTierStep", 1, "'聚'纹:有效itemTier+1计入power(等价+40),耗soulBond/回合-1"),
                        }),
                    new ArtDef("qi_rs_luobao", "落宝夺器纹", 3, "器纹学",
                        new[] { Passive("rune_snatch", "成品附'落'纹:落宝战技夺器判定门槛-2(更易把对手打成无宝肉身)") }),
                    new ArtDef("qi_rs_jinshen", "金身护宝纹", 4, "器纹学",
                        new[] { Passive("rune_guard", "成品附'御'纹:抵挡落宝/缚器,soulBond不被夺、itemTier不被压制(对economic克制免疫一次/战)") }),
                    new ArtDef("qi_rs_shanhe", "山河镇杀纹", 5, "器纹学",
                        new[] { Passive("rune_trap", "成品附'困'纹:御宝战技附困阵,对手当回合realm视作-1参与结算") }),
                }),
                // 御宝心法（channel_mind·定能御几件宝与 soulBond 上限 → 落 AddResourceCap）。
                new ArtCategoryDef("御宝心法", "channel_mind", 1, 1, new[]
                {
                    new ArtDef("qi_cm_xinbao", "心宝相照诀", 1, "御宝心法",
                        new[]
                        {
                            // soulBond 每点额外 +1 power → 抬 soulBond 项权重台阶（强化专属资源项）。
                            new EffectOp(EffectOpKind.AddTermWeightStep, "soulBondStep", 1, "soulBond每点额外+1power(强化专属资源项),祭炼推进+3"),
                        }),
                    new ArtDef("qi_cm_yiqi", "一气御三器法", 2, "御宝心法",
                        new[] { Passive("dual_artifact", "可同驱2件法宝:副法宝itemTier的50%(整数下取整)计入power") }),
                    new ArtDef("qi_cm_xiangsheng", "人器相生大法", 3, "御宝心法",
                        new[] { CapSoulBond(5, "soulBond上限20→25;境界突破soulBond门槛达成更易,缓'宝跟不上'卡境") }),
                    new ArtDef("qi_cm_wanbao", "万宝朝宗诀", 4, "御宝心法",
                        new[] { Passive("tri_artifact", "可同驱3件法宝:第三件itemTier的33%计入power;落宝命中后可借用对手被夺之宝1回合") }),
                    new ArtDef("qi_cm_heyi", "人器合一·手中无器", 5, "御宝心法",
                        new[]
                        {
                            // 大乘顶法:器灵影子 itemTier 加成翻倍(+1→+2)→ itemTier 项再 +1 阶；被落宝夺尽仍保本命1件不可夺。
                            new EffectOp(EffectOpKind.AddTermWeightStep, "itemTierStep", 1, "器灵影子itemTier加成翻倍(+1→+2)"),
                            Passive("benming_unsnatchable", "被落宝夺尽身外法宝仍保'本命'1件不可夺(脱宝不崩的唯一保险)"),
                        }),
                }),
                // 藏宝阁(法器谱)（named_artifacts·认主 1 件本命法宝,其 tier 给 itemTier 基线=power 主导项）。
                // 认主时 AddResource(itemTier, 基线) 落 state 起底；后续祭炼推进由 Phase 3 子系统接。
                new ArtCategoryDef("藏宝阁", "named_artifacts", 1, 1, new[]
                {
                    new ArtDef("qi_na_hantie", "下品·寒铁飞针", 1, "藏宝阁",
                        new[] { new EffectOp(EffectOpKind.AddResource, "itemTier", 1, "本命胚器itemTier基线1;power项itemTier*40=40;入门即用") }),
                    new ArtDef("qi_na_hongxian", "中品·红线遁光针", 3, "藏宝阁",
                        new[] { new EffectOp(EffectOpKind.AddResource, "itemTier", 3, "itemTier基线3=120power;攻击型,附自动追敌(御宝战技命中+1档)") }),
                    new ArtDef("qi_na_qingzhu", "上品·青竹蜂云剑", 5, "藏宝阁",
                        new[] { new EffectOp(EffectOpKind.AddResource, "itemTier", 5, "itemTier基线5=200power;随主成长典范,soulBond每+2额外itemTier+1进度") }),
                    new ArtDef("qi_na_baling", "古宝·八灵尺", 6, "藏宝阁",
                        new[] { new EffectOp(EffectOpKind.AddResource, "itemTier", 6, "itemTier基线6=240power;自带'困'效果,对手realm视作-1(困人型)") }),
                    new ArtDef("qi_na_fantian", "仿通天·番天镇岳印", 7, "藏宝阁",
                        new[] { new EffectOp(EffectOpKind.AddResource, "itemTier", 7, "itemTier基线7=280power;'至宝压境'门槛降1(itemTier≥对手realm+1即压制)") }),
                    new ArtDef("qi_na_luobao", "通天灵宝·落宝金钱", 8, "藏宝阁",
                        new[] { new EffectOp(EffectOpKind.AddResource, "itemTier", 8, "itemTier基线8=320power;专精经济维:落宝战技必中、夺器无视'御'纹一次") }),
                    new ArtDef("qi_na_hunyuan", "玄天之宝·混元玲珑塔", 9, "藏宝阁",
                        new[] { new EffectOp(EffectOpKind.AddResource, "itemTier", 9, "itemTier基线9=360power;'一件至宝压一境'封顶件,可同时困+夺+防,器修毕业宝") }),
                }),
                // forgeheart 道心类目（M1，补遗第五部「器我两忘心」）。A.0 仅装载不结算 → tier=0（sumArtPower 贡献 0）、
                // effects 留空（不触 daoHeart/innerDemon/comprehension 资源算子，那是 A.2 道心层的事）。具名 + power=0。
                new ArtCategoryDef("器心", "daoheart", 1, 1, new[]
                {
                    new ArtDef("dh_qi_xinbao", "心宝相照诀（道心篇）", 0, "器心", System.Array.Empty<EffectOp>()),
                    new ArtDef("dh_qi_chenglu", "成炉静定录", 0, "器心", System.Array.Empty<EffectOp>()),
                    new ArtDef("dh_qi_buduo", "宝失不夺心经", 0, "器心", System.Array.Empty<EffectOp>()),
                    new ArtDef("dh_qi_qiwo", "器我两忘·人器合一心", 0, "器心", System.Array.Empty<EffectOp>()),
                }),
            };

            // —— 战技（深度设计「战技」节，OnUse 算子 + Cost 资源表；soulBond=器魂契合,消耗驱动）。
            //    伤害/落宝/压境等具体结算 Phase 3 接，A.0 以 AddPenInteger 近似整数破防量占位 + Cost 表达资源门槛。——
            var skills = new[]
            {
                // 至宝压境：无视境界压制,满足整数门槛(itemTier≥对手realm+2)直接判定镇压(伤害=itemTier*20且对手失先手)。门槛 soulBond≥8。
                // B5扫尾: 占位 AddPenInteger(100) → Modules.PenFromResource(itemTier,20)（itemTier 绝对主导转伤,脱宝即崩→0;
                //   失先手/越境压制走 Phase3；Amount2=1 工厂保证 §15.6）。
                new CombatSkillDef("sk_qi_yazhi", "至宝压境", 5,
                    new[] { Modules.PenFromResource("itemTier", 20, note:"itemTier*20无视境界压制(法宝品阶主导),满足itemTier≥对手realm+2则对手当回合失去先手 Phase3") },
                    new Dictionary<string, int> { { "soulBond", 8 } }),
                // 万宝齐发：AOE/爆发,所驱使全部法宝itemTier之和*8一次性倾泻;发动后soulBond骤降转虚弱(low容错)。soulBond≥5且需已御≥2宝。
                // B5批2: → PenFromResource(itemTier,8) 本批用自身itemTier×8(真Σ多宝聚合 derived → EPIC-COMBAT-FULLSTRUCT defer)。
                new CombatSkillDef("sk_qi_wanbao", "万宝齐发", 4,
                    new[] { Modules.PenFromResource("itemTier", 8, note: "所驱使法宝itemTier*8一次性倾泻(本批自身itemTier;真Σ多宝聚合deferred FULLSTRUCT),高方差发动后转虚弱") },
                    new Dictionary<string, int> { { "soulBond", 5 } }),
                // 落宝金光：经济维核心,判定(本路power+落纹加成)过门槛则夺/打落对手1件法宝→对手该宝itemTier/法术power本战清零;命中后自身借用1回合。soulBond≥3。
                // B5批2: 唯一档签名机制(夺器清零+借用) → 批3 Special(luobao) handler(§10覆盖账器=已结构化·签名Special批3);本批保 Note 占位。
                new CombatSkillDef("sk_qi_luobao", "落宝金光", 3,
                    new[] { new EffectOp(EffectOpKind.AddPenInteger, null, 30, "判定过门槛夺/打落对手1件法宝→其itemTier或法术power本战清零(克剑修飞剑/法修符),命中后借用1回合(批3 Special luobao)") },
                    new Dictionary<string, int> { { "soulBond", 3 } }),
                // 玄黄护宝罡：防御/反夺器,开'御'罩本战免疫一次落宝/缚器,并把对手落宝反弹(谁夺器谁被夺)。soulBond≥4。
                new CombatSkillDef("sk_qi_huhao", "玄黄护宝罡", 3,
                    new[] { new EffectOp(EffectOpKind.AddFlatDR, null, 20, "开'御'罩本战免疫一次落宝/缚器,并把对手的落宝反弹(谁夺器谁被夺)") },
                    new Dictionary<string, int> { { "soulBond", 4 } }),
                // 缚锋锁器：压制非夺取,令对手itemTier与所选功法power各-2(持续2回合),配'缚/困'纹连锁;对装备依附者结构性克制。soulBond≥2。
                // B5批2: → Drain(itemTier,2) 夺压对手itemTier(经chokepoint,攻方借得;法术power-2 debuff 批4接)。
                new CombatSkillDef("sk_qi_fufeng", "缚锋锁器", 2,
                    new[] { Modules.Drain("itemTier", 2, "令对手itemTier-2(夺压,经chokepoint攻方借得),配'缚/困'纹连锁,对装备依附者结构性克制") },
                    new Dictionary<string, int> { { "soulBond", 2 } }),
                // 御剑斩：基础御宝攻,驱本命法宝攻敌,伤害=itemTier*10+悟性;最廉价输出手段(soulBond 0/法力少量)。无 soulBond 门槛。
                // B5批2: → PenFromResource(itemTier,10) 法宝品阶绝对主导(脱宝即崩→0;悟性部分批4基线承载)。
                new CombatSkillDef("sk_qi_yujian", "御剑斩", 1,
                    new[] { Modules.PenFromResource("itemTier", 10, note: "驱本命法宝攻敌itemTier*10(法宝品阶主导,脱宝即崩),最廉价的输出手段") },
                    new Dictionary<string, int>()),
                // 器灵自爆(舍器一击)：绝境终招,将一件法宝itemTier*30倾泻为真伤后该宝损毁;脱宝即崩路线的'同归于尽'保险。无 soulBond 门槛(代价=销毁本命法宝)。
                // B5扫尾: 占位 AddPenInteger(90) → Modules.PenFromResource(itemTier,30)（itemTier 倾泻为真伤,品阶越高越猛;
                //   该宝损毁=itemTier 永久损耗走 Phase3；Amount2=1 工厂保证 §15.6）。
                new CombatSkillDef("sk_qi_zibao", "器灵自爆", 5,
                    new[] { Modules.PenFromResource("itemTier", 30, note:"一件法宝itemTier*30倾泻为真伤(同归于尽保险)后该宝损毁(itemTier永久损耗 Phase3),脱宝即崩路线") },
                    new Dictionary<string, int>()),
            };

            return new CultivationPathDef(
                "qixiu_artificer", "器修·炼器师",
                // 攻击维=economic（不直接磨血,用落宝/缚器压制夺取对手法宝）；仅 flavor 分类,不做硬克制（R2）。
                "economic",
                // 属性/形态 tag（economic 经济维 / ranged 御宝远袭 / artifact 法宝向 / righteous 多为铸兵正道世家），
                // 非对手 PathId（R2）。落宝/缚器对外物依赖者的结构性克制由 CounterMatrix 据 economic 维评估,零 PathId。
                new[] { "economic", "ranged", "artifact", "righteous" },
                resources,
                power,
                curve,
                arts,
                skills,
                new EntryGateDef("tag:qixiu_root"),
                new SelectionRuleDef(2, 2), // 战技抽 2（深度设计选取规则⑤：特殊战技 2 个,至少 1 个经济维/克制类）
                null);
        }
    }
}
