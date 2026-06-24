using System.Collections.Generic;

namespace Jianghu.Cultivation
{
    /// <summary>
    /// 魂修·神识元神道 <c>soul_divine_sense</c>（spirit 正交·神识/魂术·spirit_attack 绕物防）。数据照
    /// 《每路深度设计》魂修节「神识道 soul_divine_sense [spirit·spirit]」+ 《内容补遗》第六部
    /// 「6. 魂修 soul_divine_sense — 道心：识海澄明心」+ 命名池魂修条目。
    /// 高爆发低容错·神识覆盖型：悟性为根、魂力为薪、所修魂术 power 为刃、realm（神识覆盖广度）放大；
    /// 攻击走精神轴绕物防（武力/根骨不参与挡），击穿失败反震识海（reverbBacklash 本路最高），永远低容错。
    ///
    /// 红线落实：terms 无 ×0、无 daoHeart（R3/R6）；武力×0 项按 R6 直接删去（不入 terms）；
    /// SituationalTags=属性/形态 tag（spirit/spirit_attack/ranged）非对手 PathId（R2）；
    /// RealmCurve 四列等长（M4）；含 1 个 Role=daoheart 类目 soulheart（M1，A.0 仅装载不结算 → tier=0
    /// 使 sumArtPower 贡献 0、effects 留空不触 daoHeart/innerDemon 资源算子）。canon pathId（R4）。纯整数，禁浮点。
    /// </summary>
    public static class SoulDivineSensePath
    {
        public static readonly CultivationPathDef Def = Build();

        // —— 被动算子（装配期落 state，核心 EffectOpKind）。功法的「悟性+N/内力+N」等改四维项 A.0
        //    为 flavor 不落算子（生成期 Σ=80 不被功法污染，深度设计§选取规则「功法只加 power 不改 Σ」），
        //    仅以 Note 留痕；能落 state 的「魂力上限+N」走 AddResourceCap、被动开关走 GrantPassive。——
        private static EffectOp CapSoulForce(int amt, string note)
            => new EffectOp(EffectOpKind.AddResourceCap, "soulForce", amt, note);

        private static EffectOp Passive(string flag, string note)
            => new EffectOp(EffectOpKind.GrantPassive, flag, 1, note);

        private static CultivationPathDef Build()
        {
            // —— per-path 专属资源（不进四维 C6）——
            // soulForce 魂力：薪柴，乘 SeaIntegrity/100 后入算（深度设计「魂力×SeaIntegrity/100」乘子门 A.0 不接，
            //   见 Note：SeaIntegrityMul 属 ModKind=L1/R5，本 A.0 不引入 → 魂力作 plain term，乘子门留 L1）。
            //   base cap=30（深度设计「魂力上限+N」由神识功法 AddResourceCap 增益，A.0 单值起底）。
            // seaIntegrity 识海完整度 0..100：魂力上限乘子来源 / 被反震·夺舍失败扣减 / 闭关炼神回升；initial=100（识海起始完整）。
            var resources = new[]
            {
                new ResourceDef("soulForce", 0, 30, 8), // INV-CROSS: enter combat with modest soulForce (balance: 20→8, avoid overshoot)
                new ResourceDef("seaIntegrity", 0, 100, 100),
            };

            // —— 战力公式（深度设计 terms：悟性×5 + 魂力×4 + realm×3 + 所选魂术power×2 + 内力×1 + 武力×0）。
            //    悟性权重全路最重（神识之根，他路最多次权）；武力×0 按 R6 禁 ×0 项直接删去（与剑/体修硬分野=自身也不靠物理）；
            //    无 daoHeart、无 ×0（R3/R6）。realm 乘性放大由 RealmCurve 承载，此处 realm 仅置一正权底项。——
            var power = new PowerFormulaDef(
                new[]
                {
                    new PowerTerm("stat:Insight", 5, null),    // 悟性为根：神识之根，决定探查精度与穿透基数，全路唯一重权四维
                    new PowerTerm("res:soulForce", 4, null),   // 魂力：薪柴（深度设计应乘 SeaIntegrity/100，乘子门属 L1 未接，见上 Note）
                    new PowerTerm("realm", 3, null),           // 境界：神识覆盖广度（化神/阳神段陡增，倍率见 RealmCurve）
                    new PowerTerm("sumArtPower", 2, null),     // 所选魂术 tier 之和：穿透之「刃」的锋利度
                    new PowerTerm("stat:Internal", 1, null),   // 内力：神识载体的微弱底盘（凝魂需一线真元承载），远低于法修内力主权
                    // 武力×0 删去（R6 禁 ×0 项）：肉身武力对神魂攻防无贡献，绕物防的另一面=自身也不靠物理。
                },
                System.Array.Empty<PowerMod>(),
                null);

            // —— 战力曲线（深度设计 realmMul=[1,2,4,8,14,24,40]，realm 0..6，跃迁陡峭·反震自险）。
            //    四列等长（M4）：倍率 / UnifiedTierOf（UT0-12 映射：炼气→筑基→金丹→元婴→化神→合体→大乘，跳过炼虚直化神→合体）/
            //    境界名（凝魂→出窍→神游→分魂→神桥→阳神→阳神大成，深度设计 realm0..6 神识凝练度阶）/
            //    升入阈值（神识凝练度累进 ≥100×当前realm 升阶，realm0=0 起；突破即一次自我反震测试故同剑修陡度）。——
            var curve = new RealmCurveDef(
                new[] { 19, 27, 30, 43, 65, 158, 404 }, // INV-CROSS v2 (pre balance-003): scale -15% → UT8=1.20x sword (was 1.41x)
                new[] { 0, 2, 4, 6, 8, 10, 12 },
                new[] { "凝魂", "出窍", "神游", "分魂", "神桥", "阳神", "阳神大成" },
                // 神识凝练度累进（Σ 100×(0..i-1)）：凝魂(0)→出窍(100)→神游(300)→分魂(600)→神桥(1000)→阳神(1500)→阳神大成(2100)。
                new[] { 0, 100, 300, 600, 1000, 1500, 2100 },
                // —— A.1 境界稿 §2：起步 SubLevelCount 全 1；CanAscend=true；MaxMajor=大境界数-1。——
                new[] { 1, 1, 1, 1, 1, 1, 1 }, true, 6);

            // —— 功法类目（神识/魂术/秘术 各 5 具名 + soulheart 道心 4 具名）。
            //    具名/效果照深度设计「功法类目」节；命名与命名池魂修条目同源。——
            var arts = new[]
            {
                // 神识（主修功法·识海根基，本路「内功」位：定魂力上限、SeaIntegrity 恢复、神识覆盖与探查精度）。
                //   「魂力上限+N」落 AddResourceCap；探查精度/突破反噬减免/夺舍成功率等改判定为 flavor（Note）。
                new ArtCategoryDef("神识", "internal", 1, 1, new[]
                {
                    new ArtDef("so_ss_dayan", "大衍洞玄诀", 5, "神识",
                        new[]
                        {
                            CapSoulForce(50, "魂力上限+50"),
                            Passive("probe_precise", "炼神回升+8;神识探查误差减半(取整向下),神识冠绝同阶"),
                        }),
                    new ArtDef("so_ss_yangshen", "阳神炼魂经", 4, "神识",
                        new[]
                        {
                            CapSoulForce(35, "魂力上限+35"),
                            Passive("soul_solid", "突破反噬伤害减12(下限0);realm≥4 解锁神魂凝实(被物理攻击伤害减半取整)"),
                        }),
                    new ArtDef("so_ss_jiuzhuan", "九转洗魂诀", 3, "神识",
                        new[]
                        {
                            CapSoulForce(22, "魂力上限+22"),
                            Passive("snatch_bonus", "SeaIntegrity 自然回升+5/闭关;夺舍成功率判定+15(整数加成)"),
                        }),
                    new ArtDef("so_ss_ningshen", "凝神astral静定法", 2, "神识",
                        new[]
                        {
                            CapSoulForce(12, "魂力上限+12"),
                            Passive("probe_precise", "神识探查精度+10;连续被反震时第二次起反震扣减-5(缓冲)"),
                        }),
                    new ArtDef("so_ss_shouyi", "守一养神术", 1, "神识",
                        new[]
                        {
                            CapSoulForce(6, "魂力上限+6"),
                            Passive("sea_floor20", "SeaIntegrity 上限锁定不低于20(防全崩),入门保命功"),
                        }),
                }),
                // 魂术（攻坚招式·神魂穿透，本路「外功/法术」位：结算走 spirit 轴绕物防，tier→sumArtPower 构成「刃」）。
                //   SpiritPen/穿透改判定为 flavor（Note）；被动开关走 GrantPassive。
                new ArtCategoryDef("魂术", "soul", 1, 1, new[]
                {
                    new ArtDef("so_st_jiuyou", "九幽噬魂大法", 5, "魂术",
                        new[] { Passive("spirit_pierce", "SpiritPen+60(无视武力/根骨);击穿后吞噬目标魂力,本方魂力当前值回补+15(攻坚最强反震也最重)") }),
                    new ArtDef("so_st_fentian", "焚天灭魂指", 4, "魂术",
                        new[] { Passive("spirit_pierce", "SpiritPen+42;对 SeaIntegrity<50 目标穿透×2(整数翻倍),单体爆发") }),
                    new ArtDef("so_st_wanpo", "万魄锁神咒", 3, "魂术",
                        new[] { Passive("multi_lock", "SpiritPen+28;可同时锁定 realm 个目标(神识覆盖广度),群体偷袭专用") }),
                    new ArtDef("so_st_shehun", "摄魂夺魄术", 2, "魂术",
                        new[] { Passive("spirit_pierce", "SpiritPen+16;击穿后令目标下一动作判定-20(夺其神),控场向") }),
                    new ArtDef("so_st_shennian", "神念刺", 1, "魂术",
                        new[] { Passive("spirit_pierce", "SpiritPen+8;消耗极低,反震系数减半(失手只扣一半),试探/磨血入门术") }),
                }),
                // 秘术（夺舍·分魂·寿元枢纽，本路独有高风险战略位：改写「风险曲线」与寿元/翻盘条件，不直接加穿透）。
                //   均为被动能力开关 → GrantPassive；不在装配期触 SeaIntegrity/分魂 等结算（Phase 3 接）。
                new ArtCategoryDef("秘术", "esoteric", 1, 1, new[]
                {
                    new ArtDef("so_mi_duoshe", "夺舍重生大法", 5, "秘术",
                        new[] { Passive("body_snatch", "可对活体/容器发动夺舍:成功=realm跃升1级+SeaIntegrity回满100+寿元重置;失败=分魂尽灭、SeaIntegrity-40(凡人式翻盘核心)") }),
                    new ArtDef("so_mi_fenhun", "分魂化念秘术", 4, "秘术",
                        new[] { Passive("soul_split", "凝出至多2道分魂(各持本体40%魂力):神识覆盖+2目标、可代本体承受一次反震;分魂亡则 SeaIntegrity-15") }),
                    new ArtDef("so_mi_guiyuan", "归元守识秘法", 3, "秘术",
                        new[] { Passive("sea_safety_net", "战中触发:把一次本会致识海崩裂(SeaIntegrity→0)的反震钳为 SeaIntegrity=10(保命),冷却后方可再触发(低容错唯一安全网)") }),
                    new ArtDef("so_mi_wangqi", "神识探查·望气", 2, "秘术",
                        new[] { Passive("divine_probe", "战前/战中读取目标神魂壁(悟性+魂力·realm)整数估值,误差≤探查精度;据此择机奇袭、规避强敌(降低误击穿)") }),
                    new ArtDef("so_mi_hunjin", "魂烬反噬引", 1, "秘术",
                        new[] { Passive("burn_sea_for_pen", "主动消耗自身5点 SeaIntegrity,将下一次魂术 SpiritPen+20(燃魂搏命),入门级以险换伤") }),
                }),
                // soulheart 道心类目（M1，补遗第六部「识海澄明心」soulheart）。A.0 仅装载不结算 → tier=0（sumArtPower 贡献 0）、
                // effects 留空（不触 daoHeart/innerDemon/SeaIntegrity 资源算子，那是 A.2 道心层的事）。具名 + power=0。
                // 注：补遗 art 摘要含 AddResource(daoHeart/innerDemon)，但 daoHeart/innerDemon 不在本路 Resources 字典，
                //     A.0 落算子会崩且违 R3 → 道心占位类目一律空 Effects，真道心机制 A.2 接。
                new ArtCategoryDef("神心", "daoheart", 1, 1, new[]
                {
                    new ArtDef("dh_so_shouyi", "守一养神心诀", 0, "神心", System.Array.Empty<EffectOp>()),
                    new ArtDef("dh_so_chengming", "识海澄明录", 0, "神心", System.Array.Empty<EffectOp>()),
                    new ArtDef("dh_so_buzhui", "神魂不坠心经", 0, "神心", System.Array.Empty<EffectOp>()),
                    new ArtDef("dh_so_yangshen", "阳神不灭道心", 0, "神心", System.Array.Empty<EffectOp>()),
                }),
            };

            // —— 战技（深度设计「战技」节，OnUse 算子 + Cost 资源表；弹药=soulForce 魂力）。
            //    伤害/穿透/夺舍等具体结算 Phase 3 接，A.0 以 AddPenInteger 占位破防量（非伤害技置 0）+ Cost 表达魂力门槛。
            //    注：「焚魂自爆=全部当前魂力」「夺舍=魂力50+一道分魂前提」等动态成本 A.0 以代表性整数 Cost 占位（见各 Note）。——
            var skills = new[]
            {
                // 神识奇袭·夺魂一击：先手偷袭,DivineProbe 已读壁且穿透≥壁则无视一切物理防御秒杀(spirit 全额入魂);误判则反震=壁−穿透全额扣 SeaIntegrity。
                new CombatSkillDef("sk_so_qixi", "神识奇袭·夺魂一击", 5,
                    new[] { Modules.FlatPen(60, "无视一切物理防御秒杀(spirit 全额入魂)基线破防量;须先用神识探查否则反震系数×2(误判反震 Phase3/批4),本路标志性高风险一击") },
                    new Dictionary<string, int> { { "soulForce", 40 } }),
                // 万魂幡·群体摄神：对 realm 个目标同时摄魂,各受 SpiritPen 穿透判定;击穿者下回合行动判定-25(神乱)。
                // B5扫尾: 占位 AddPenInteger(28) → Modules.AoePerTarget(28)（对 realm 个目标群攻,R2 单挑退化×1=+28,群战按敌数放大）。
                new CombatSkillDef("sk_so_wanhun", "万魂幡·群体摄神", 4,
                    new[] { Modules.AoePerTarget(28, "对 realm 个目标同时摄魂穿透,敌越多总伤越高(R2单挑退化×1);击穿者下回合行动判定-25(神乱 Phase3)") },
                    new Dictionary<string, int> { { "soulForce", 35 } }),
                // 焚魂自爆·阳神反噬：绝境技,燃烧全部魂力+30点 SeaIntegrity,对当前目标 SpiritPen=(魂力当前值×2)必杀级一击;施后 SeaIntegrity 至多剩10。
                // B5批2: → PenFromResource(soulForce,2) 魂力自爆(魂力当前值×2,越满越痛,见底哑火;SeaIntegrity 自损 Phase3 结算)。
                new CombatSkillDef("sk_so_fenhun", "焚魂自爆·阳神反噬", 4,
                    new[] { Modules.PenFromResource("soulForce", 2, note: "燃烧全部魂力(魂力当前值×2)必杀级一击,同归于尽向(SeaIntegrity 自损 Phase3 结算)") },
                    new Dictionary<string, int> { { "soulForce", 18 } }),
                // 夺舍·借尸：对濒死活体/傀儡容器发动,成功则本体神魂迁入新躯(realm跃升1、识海回满、寿元重置、继承新躯根骨);失败分魂尽灭。战略级而非战术级。
                // B5扫尾 defer(红线A.8): 夺舍战略级(realm跃升/识海回满/寿元重置)→batch4/A.2 战略层,非战术伤害(amount 0),保 AddPenInteger(0) 占位。
                new CombatSkillDef("sk_so_jieshi", "夺舍·借尸", 5,
                    new[] { Modules.Special("duoshe", 1, 0, "夺舍:realm跃升+识海回满+寿元重置(失败→分魂尽灭)") },
                    new Dictionary<string, int> { { "soulForce", 50 } }),
                // 神念结界·锁魂困敌：以神识织结界罩住一节点,域内敌方游历判定-30(神识封路)、其对我方物理偷袭仍按 spirit 轴被反制。控场/逃生两用。
                new CombatSkillDef("sk_so_jinjie", "神念结界·锁魂困敌", 3,
                    new[] { Modules.FlatPen(0, "罩住一节点:域内敌方游历判定-30(神识封路),其物理偷袭仍按 spirit 轴被反制(控场/逃生两用);每回合维持魂力5(结界非伤害置0)"), Modules.FlatPen(10, "结界反冲 spirit 余波") },
                    new Dictionary<string, int> { { "soulForce", 12 } }),
                // 分魂诱敌·金蝉脱壳：放出一道分魂代本体承受下一次攻击(无论物理或神魂),本体脱离;分魂亡则 SeaIntegrity-15。低容错路线核心走位保命。
                new CombatSkillDef("sk_so_jinchan", "分魂诱敌·金蝉脱壳", 3,
                    new[] { Modules.FlatPen(0, "放出一道分魂代本体承受下一次攻击(物理/神魂皆可),本体脱离;分魂亡则 SeaIntegrity-15,凝有分魂为前提(低容错核心保命,代受/脱离非伤害置0)"), Modules.FlatPen(6, "分魂反冲 spirit 余震") },
                    new Dictionary<string, int> { { "soulForce", 8 } }),
                // 望气探魂：非伤害侦察,对一目标读出神魂壁整数估值与 SeaIntegrity 残量,误差≤探查精度;为后续奇袭/规避提供数据。开战前必备侦察。
                new CombatSkillDef("sk_so_wangqi", "望气探魂", 2,
                    new[] { Modules.FlatPen(0, "对一目标读出神魂壁整数估值与 SeaIntegrity 残量,误差≤探查精度,为奇袭/规避提供数据(开战前必备侦察,纯侦察非伤害置0)"), Modules.FlatPen(4, "神识探查 spirit 轻触") },
                    new Dictionary<string, int> { { "soulForce", 3 } }),
            };

            return new CultivationPathDef(
                "soul_divine_sense", "魂修·神识元神道",
                "spirit",
                // 属性/形态 tag（spirit 精神轴 / spirit_attack 绕物防 / ranged 神识离体远袭），非对手 PathId（R2）。
                new[] { "spirit", "spirit_attack", "ranged" },
                resources,
                power,
                curve,
                arts,
                skills,
                new EntryGateDef("tag:soul_root"),
                new SelectionRuleDef(3, 3), // 战技抽 3（深度设计选取规则:战技池选3,至少含1侦察技）
                null);
        }
    }
}
