using System.Collections.Generic;

namespace Jianghu.Cultivation.Paths
{
    /// <summary>
    /// 法修·术法修士 <c>fa_xiu</c>（physical 中远程·元素代表路）。数据照《每路深度设计》法修节 +
    /// 《内容补遗》第六部「3. 法修 fa_xiu — 道心：明镜止水心」+ 命名池法修条目。
    /// 稳健均衡百搭续航：内力为本、悟性驭法、法术库广度百搭、灵力池续航（平滑递增曲线，无单点爆发；
    /// 元素相生克带横向波动非纵向跳变）。本路主灵根代表取「火」（spiritRootElement=火），SituationalTags
    /// 含 fire；相生克边照设计 counterWheel(火克木/木克雷/雷克冰/冰克火) 接 element 轴（SituationalEdges）。
    ///
    /// 红线落实：terms 无 ×0、无 daoHeart（R3/R6）；SituationalTags=属性/形态 tag 非对手 PathId（R2）；
    /// RealmCurve 四列等长（M4）；含 1 个 Role=daoheart 类目 spellheart（M1，A.0 仅装载不结算 → tier=0）。
    /// canon pathId（R4）。纯整数，禁浮点。
    /// </summary>
    public static class FaXiuPath
    {
        public static readonly CultivationPathDef Def = Build();

        // —— 被动算子（装配期落 state，核心 EffectOpKind）。功法的「内力+N/悟性+N」等改四维项 A.0
        //    为 flavor 不落算子（生成期 Σ=80 不被功法污染），仅以 Note 留痕；能落 state 的「manaPool 上限+N」
        //    走 AddResourceCap、「已修法术系数 spellBreadth+1」走 AddResource、被动开关走 GrantPassive。——
        private static EffectOp CapMana(int amt, string note)
            => new EffectOp(EffectOpKind.AddResourceCap, "manaPool", amt, note);

        private static EffectOp AddBreadth(int amt, string note)
            => new EffectOp(EffectOpKind.AddResource, "spellBreadth", amt, note);

        private static EffectOp Passive(string flag, string note)
            => new EffectOp(EffectOpKind.GrantPassive, flag, 1, note);

        private static CultivationPathDef Build()
        {
            // —— per-path 专属资源（不进四维 C6）——
            // manaPool 灵力池：续航弹药（深度设计 =内力×2+realm×4，A.0 单值起底；realm/功法增益由 AddResourceCap）。
            //   cap=100（容心法/符印多重 AddResourceCap 增益起底）。spellBreadth 法术库广度 0..4（百搭核心,多系并修线性叠战力）。
            var resources = new[]
            {
                new ResourceDef("manaPool", 0, 100, 0),
                new ResourceDef("spellBreadth", 0, 4, 1),
            };

            // —— 战力公式（深度设计 P0=内力×5+悟性×3+武力×2+根骨×1+realm×8+Σ法术power×1+spellBreadth×4+manaPool×3）。
            //    内力权重全路最高(法力本源)、根骨垫底(与体修对立)；唯一含 spellBreadth×4 百搭项与 manaPool×3 续航项；
            //    无 daoHeart、无 ×0（R3/R6）。manaPool 项深度设计 raw×3，照剑修范式取 res×3 占位(Initial=0 起底)，
            //    续航惩罚(manaPool/cost=0 →×3/4)属 Phase 3 战斗结算，A.0 不落 Modifier，Note 留痕。——
            var power = new PowerFormulaDef(
                new[]
                {
                    new PowerTerm("stat:Internal", 5, null),     // 内力：法力/灵力本源，法修最高权重
                    new PowerTerm("stat:Insight", 3, null),      // 悟性：神识/法术驾驭，决定多系维持与命中
                    new PowerTerm("stat:Force", 2, null),        // 武力：御法身法自保，低权重
                    new PowerTerm("stat:Constitution", 1, null), // 根骨：肉身，法修最低权重(与体修对立)
                    new PowerTerm("realm", 8, null),             // 境界：整数基底倍率锚
                    new PowerTerm("sumArtPower", 1, null),       // 所选法术/符印/心法各功法 tier 之和
                    new PowerTerm("res:spellBreadth", 4, null),  // 法术库广度：百搭核心，多系线性叠战力
                    new PowerTerm("res:manaPool", 3, null),      // 灵力池续航（raw 占位，续航惩罚 Phase 3 接）
                },
                System.Array.Empty<PowerMod>(),
                null);

            // —— 战力曲线（深度设计 realmMul=[1,2,4,7,11,16,23,32,44]，realm 0..8，单调递增·加速度逐境衰减·最平滑）。
            //    四列等长（M4）：倍率 / UnifiedTierOf（UT0-12 映射）/ 境界名（炼气→筑基→金丹→元婴→化神→炼虚→合体→大乘→渡劫）/
            //    升入阈值（资源积累+法术参悟厚积，realm0=0 起；法术早成体系故前段阈低、稳健渐进）。——
            var curve = new RealmCurveDef(
                new[] { 1, 2, 4, 7, 11, 16, 23, 32, 44 },
                new[] { 0, 2, 4, 6, 8, 9, 10, 11, 12 },
                new[] { "炼气", "筑基", "金丹", "元婴", "化神", "炼虚", "合体", "大乘", "渡劫" },
                // 法术早成体系故厚积渐进（≥90×当前realm 升阶，平滑无跳变）。
                new[] { 0, 90, 270, 540, 900, 1350, 1890, 2520, 3240 });

            // —— 功法类目（法术/符印/心法 各 5 具名 + spellheart 道心 4 具名）。
            //    具名/效果照深度设计「功法类目」节；命名与命名池法修条目同源。——
            var arts = new[]
            {
                // 法术(攻伐系)·主输出，按主灵根分火攻/冰控/雷伐/木疗四系（多系并修即 spellBreadth 来源 → AddBreadth）。
                new ArtCategoryDef("法术", "attack", 1, 1, new[]
                {
                    new ArtDef("fa_fs_qingmu", "青木生发术·回春", 2, "法术",
                        new[] { Passive("regen_mana", "木:每回合回己方manaPool+10,被克惩罚减免4,伤害仅+6") }),
                    new ArtDef("fa_fs_chiyan", "赤焰术·烈阳爆", 3, "法术",
                        new[] { Passive("fire_spell", "火:单体伤害+18;命中木属性敌(相克)额外+6") }),
                    new ArtDef("fa_fs_xuanbing", "玄冰锥·寒髓封", 3, "法术",
                        new[] { Passive("ice_spell", "冰:伤害+12并冻滞使敌可释战技数-1;命中火属性敌额外+5") }),
                    new ArtDef("fa_fs_zilei", "紫雷诀·九霄落", 4, "法术",
                        new[] { Passive("thunder_spell", "雷:伤害+22(法修最高单系);命中冰属性敌额外+7,无视御物防御8") }),
                    new ArtDef("fa_fs_wuxing", "五行倒乱·混元法", 6, "法术",
                        new[]
                        {
                            AddBreadth(1, "全系:需spellBreadth≥3;综合伤害+30且本回合对己counterAdj清0(百搭巅峰)"),
                            Passive("hunyuan", "五行倒乱:本回合对己counterAdj清0"),
                        }),
                }),
                // 符印(辅助/封印系)·即放符箓与持续封印阵，不占常驻法术维持位。manaPool 上限增益落 AddResourceCap。
                new ArtCategoryDef("符印", "movement", 1, 1, new[]
                {
                    new ArtDef("fa_fy_juling", "聚灵符", 1, "符印",
                        new[] { CapMana(15, "manaPool上限+15持续整场") }),
                    new ArtDef("fa_fy_jinguang", "金光护身符", 2, "符印",
                        new[] { Passive("shield_20", "护盾吸收20伤;被克状态额外扣6") }),
                    new ArtDef("fa_fy_qiankun", "乾坤挪移符", 3, "符印",
                        new[] { Passive("blink_dodge", "瞬移脱战/换位,规避当回合一次致命战技(0 cost应急)") }),
                    new ArtDef("fa_fy_wuxingfeng", "五行封灵阵", 4, "符印",
                        new[] { Passive("seal_element", "对单一灵根敌施封印,其同系法术power归0共2回合") }),
                    new ArtDef("fa_fy_wanfu", "万符归元·天书符篆", 6, "符印",
                        new[] { Passive("all_seal_burst", "一次释放已修全系各一道,总伤=8×spellBreadth") }),
                }),
                // 心法(灵力运转/神识系)·常驻内功,抬内力/悟性折算、扩 manaPool、提可同时维持法术数。manaPool/spellBreadth 增益落算子。
                new ArtCategoryDef("心法", "internal", 1, 1, new[]
                {
                    new ArtDef("fa_xf_jichu", "基础吐纳引气诀", 1, "心法",
                        new[] { CapMana(8, "内力折算战力+1/级,manaPool+8") }),
                    new ArtDef("fa_xf_liangyi", "两仪聚灵功", 3, "心法",
                        new[] { CapMana(20, "manaPool的realm项+2,可同时维持法术数+1") }),
                    new ArtDef("fa_xf_taixu", "太虚神识录", 4, "心法",
                        new[] { Passive("ignore_evade", "悟性折算+2,神识外放使命中无视身法闪避5") }),
                    new ArtDef("fa_xf_zhuxian", "诛仙天书·正卷心法", 5, "心法",
                        new[]
                        {
                            AddBreadth(1, "spellBreadth上限+1(破四系封顶),全法术power+4"),
                            Passive("breadth_cap_up", "诛仙正卷:spellBreadth封顶+1"),
                        }),
                    new ArtDef("fa_xf_piaomiao", "缥缈御灵·周天搬运", 5, "心法",
                        new[]
                        {
                            CapMana(30, "manaPool上限+30"),
                            Passive("regen_sustain", "每回合manaPool自然回复+12且续航惩罚阈值减半"),
                        }),
                }),
                // spellheart 道心类目（M1，补遗第六部「明镜止水心」）。A.0 仅装载不结算 → tier=0（sumArtPower 贡献 0）、
                // effects 留空（不触 daoHeart/innerDemon/comprehension 资源算子,那是 A.2 道心层的事）。具名 + power=0。
                new ArtCategoryDef("法心", "daoheart", 1, 1, new[]
                {
                    new ArtDef("dh_fa_jingshui", "明镜止水诀", 0, "法心", System.Array.Empty<EffectOp>()),
                    new ArtDef("dh_fa_canfa", "参法悟道心", 0, "法心", System.Array.Empty<EffectOp>()),
                    new ArtDef("dh_fa_buzheng", "不诤元神录", 0, "法心", System.Array.Empty<EffectOp>()),
                    new ArtDef("dh_fa_wuxiang", "无相清静经", 0, "法心", System.Array.Empty<EffectOp>()),
                }),
            };

            // —— 战技（深度设计「战技」节，OnUse 算子 + Cost 资源表；弹药=manaPool 灵力池）。
            //    伤害/穿透等具体结算 Phase 3 接，A.0 以 AddPenInteger 占位破防量（量级对齐该路公式）+ Cost 表达资源门槛。——
            var skills = new[]
            {
                // 混元一气·万法归宗：需spellBreadth=4,融四系一击,伤害=40+realm×3,无视相克与护盾。manaPool≥40(几近清池)。
                new CombatSkillDef("sk_fa_hunyuan", "混元一气·万法归宗", 7,
                    new[] { new EffectOp(EffectOpKind.AddPenInteger, null, 40, "需spellBreadth=4,融四系一击40+realm×3,无视相克与护盾") },
                    new Dictionary<string, int> { { "manaPool", 40 } }),
                // 五雷正法：召天雷,雷系伤害+28;对tag:ghost/tag:demon额外+10。manaPool≥20,消耗20。
                new CombatSkillDef("sk_fa_wulei", "五雷正法", 5,
                    new[] { new EffectOp(EffectOpKind.AddPenInteger, null, 28, "召天雷雷系伤害+28;对tag:ghost/tag:demon额外+10(破阴邪)") },
                    new Dictionary<string, int> { { "manaPool", 20 } }),
                // 焚天燎原阵：火系AoE每敌16伤;命中木属性敌每个额外+5。manaPool≥22,消耗22。
                new CombatSkillDef("sk_fa_fentian", "焚天燎原阵", 5,
                    new[] { new EffectOp(EffectOpKind.AddPenInteger, null, 16, "火系AoE每敌16伤;命中木属性敌每个额外+5") },
                    new Dictionary<string, int> { { "manaPool", 22 } }),
                // 万剑诀·御物千刃：御使飞剑/法宝群攻,伤害=12+2×spellBreadth。manaPool≥15,消耗15。
                new CombatSkillDef("sk_fa_wanjian", "万剑诀·御物千刃", 4,
                    new[] { new EffectOp(EffectOpKind.AddPenInteger, null, 12, "御使飞剑/法宝群攻,伤害=12+2×spellBreadth") },
                    new Dictionary<string, int> { { "manaPool", 15 } }),
                // 寒冰六合困：范围冻结,敌全体可释战技数-1共1回合。manaPool≥18,消耗18。
                new CombatSkillDef("sk_fa_hanbing", "寒冰六合困", 4,
                    new[] { new EffectOp(EffectOpKind.AddPenInteger, null, 10, "范围冻结,敌全体可释战技数-1共1回合(控场)") },
                    new Dictionary<string, int> { { "manaPool", 18 } }),
                // 五行大遁术：强行脱战/贴近,规避一次必杀并回manaPool+8。manaPool≥10,消耗10。
                new CombatSkillDef("sk_fa_dadun", "五行大遁术", 3,
                    new[]
                    {
                        new EffectOp(EffectOpKind.AddPenInteger, null, 0, "强行脱战/贴近,规避一次必杀"),
                        new EffectOp(EffectOpKind.AddResource, "manaPool", 8, "回manaPool+8(应急续航)"),
                    },
                    new Dictionary<string, int> { { "manaPool", 10 } }),
                // 灵犀御空：御风飞遁,身法折算闪避+6,规避物理近身一回合。manaPool≥8,消耗8。
                new CombatSkillDef("sk_fa_lingxi", "灵犀御空", 2,
                    new[] { new EffectOp(EffectOpKind.AddFlatDR, null, 6, "御风飞遁,身法折算闪避+6,规避物理近身一回合(对抗近战)") },
                    new Dictionary<string, int> { { "manaPool", 8 } }),
            };

            return new CultivationPathDef(
                "fa_xiu", "法修·术法修士",
                "physical",
                // 属性/形态 tag（ranged 中远程 / elemental 元素 / fire 主灵根代表），非对手 PathId（R2）。
                new[] { "ranged", "elemental", "fire" },
                resources,
                power,
                curve,
                arts,
                skills,
                new EntryGateDef("tag:spirit_root"),
                new SelectionRuleDef(2, 2), // 战技抽 2（深度设计'1心法+1~2法术+1符印+2战技,≥1主灵根系'）
                null);
        }
    }
}
