using System.Collections.Generic;

namespace Jianghu.Cultivation.Paths
{
    /// <summary>
    /// 体修·炼体横世武夫 <c>ti_xiu_hengshi</c>（physical 耐久代表路）。数据照《每路深度设计》体修节 +
    /// 《内容补遗》第六部「2. 体修 ti_xiu_hengshi — 道心：金石不移心」+ 命名池体修条目。
    /// 稳健耐久攻防碾压：根骨为骨、横练成底盘、血气养不灭金身（低开高走平滑曲线，无 Insight 主导、无尖峰）。
    ///
    /// 红线落实：terms 无 ×0、无 daoHeart（R3/R6）；SituationalTags=属性/形态 tag 非对手 PathId（R2）；
    /// RealmCurve 四列等长（M4）；含 1 个 Role=daoheart 类目 bodyheart（M1，A.0 仅装载不结算 → tier=0
    /// 使 sumArtPower 贡献 0、effects 留空不触 daoHeart 资源算子）。canon pathId（R4）。纯整数，禁浮点。
    /// </summary>
    public static class BodyHenglianPath
    {
        public static readonly CultivationPathDef Def = Build();

        // —— 被动算子（装配期落 state，核心 EffectOpKind）。功法的「根骨+N/武力+N」等改四维项 A.0
        //    为 flavor 不落算子（生成期 Σ=80 不被功法污染），仅以 Note 留痕；能落 state 的「血气上限+N」
        //    走 AddResourceCap、被动开关走 GrantPassive。横练值 henglian 由横练功永久叠加 → AddResource。——
        private static EffectOp CapQixue(int amt, string note)
            => new EffectOp(EffectOpKind.AddResourceCap, "qixue", amt, note);

        private static EffectOp AddHenglian(int amt, string note)
            => new EffectOp(EffectOpKind.AddResource, "henglian", amt, note);

        private static EffectOp Passive(string flag, string note)
            => new EffectOp(EffectOpKind.GrantPassive, flag, 1, note);

        private static CultivationPathDef Build()
        {
            // —— per-path 专属资源（不进四维 C6）——
            // qixue 血气：燃血护体/不灭金身燃料；cap=200（深度设计 0..cap=200），realm/功法增益由 AddResourceCap。
            // henglian 横练值：常驻减伤底盘，由所选横练功永久叠加（AddResource），base cap=120（容 t6 金身+40 起底）。
            var resources = new[]
            {
                new ResourceDef("qixue", 0, 200, 0),
                new ResourceDef("henglian", 0, 120, 0),
            };

            // —— 战力公式（深度设计 terms：根骨×3 + 武力×2 + 内力×1 + realm×8 + 所选功法power×1 + 血气×1）。
            //    根骨权重全路最高（肉身堆叠主轴）、内力刻意最低（仅供血气吐纳转化）；无悟性项、无爆发项；
            //    无 daoHeart、无 ×0（R3/R6）。血气项深度设计意为 qixue/20 折算，A.0 无 per-term 除子 →
            //    照剑修范式取 raw res×1 占位（Initial=0 起底不污染，÷20 缩放 Phase 3 结算接），Note 留痕。——
            var power = new PowerFormulaDef(
                new[]
                {
                    new PowerTerm("stat:Constitution", 3, null),// 根骨为骨：肉身堆叠主轴，全路最高
                    new PowerTerm("stat:Force", 2, null),       // 武力：横练拳脚输出与破横练，第二权重
                    new PowerTerm("stat:Internal", 1, null),    // 内力：仅供血气吐纳转化效率，弱权
                    new PowerTerm("realm", 8, null),            // 境界：每境界+8 厚势基线，稳健底盘非尖峰
                    new PowerTerm("sumArtPower", 1, null),      // 所选横练功/拳脚功/血气吐纳各功法 tier 之和
                    new PowerTerm("res:qixue", 1, null),        // 血气：raw 占位（深度设计意 qixue/20 折算，÷20 缩放 Phase 3 接）
                },
                System.Array.Empty<PowerMod>(),
                null);

            // —— 战力曲线（深度设计 realmMul=[10,13,17,22,28,35,43,52,62,73]，realm 0..9，低开高走平滑无跳变）。
            //    四列等长（M4）：倍率 / UnifiedTierOf（UT0-12 映射：三流→二流→一流→小宗师→宗师→大宗师/陆地神仙）/
            //    境界名（锻皮→淬肉→淬骨→开窍→伐毛洗髓→铜皮→铁骨→金身→不灭金身→肉身成圣，realm0..9）/
            //    升入阈值（淬炼里程累进，realm0=0 起；耐久路日课锤炼故阈值平滑递增）。——
            var curve = new RealmCurveDef(
                new[] { 16, 21, 28, 36, 45, 57, 70, 84, 100, 118 }, // INV-CROSS二轮: ×1.62→target 0.85×剑修
                // A1.2 迁移（境界稿 §11.1）：偏离 UT1 压偶数主阶并入 UT0（淬肉随锻皮入「炼气」大境界）→
                //   UnifiedTierOf 全落锚集 {0,2,4,6,8,9,10,11,12}、非降；密度走 SubLevel（决策③）。
                new[] { 0, 0, 2, 4, 6, 8, 9, 10, 11, 12 },
                new[] { "锻皮", "淬肉", "淬骨", "开窍", "伐毛洗髓", "铜皮", "铁骨", "金身", "不灭金身", "肉身成圣" },
                // 淬炼里程 ≥80×当前realm 升阶（笨功夫稳步累进，略低于剑修斩道阈，体现厚积稳进）。
                new[] { 0, 80, 240, 480, 800, 1200, 1680, 2240, 2880, 3600 },
                // —— A.1：SubLevelCount = 同 UT 段长（UT0 段 2 = 锻皮/淬肉 → 大境界数 9）；
                //    CanAscend=true（炼体修士一脉，境界稿 §3.1）；MaxMajor=大境界数-1=8。——
                new[] { 2, 1, 1, 1, 1, 1, 1, 1, 1 }, true, 8);

            // —— 功法类目（横练功/拳脚功/血气吐纳法 各 5 具名 + bodyheart 道心 4 具名）。
            //    具名/效果照深度设计「功法类目」节；命名与命名池体修条目同源。——
            var arts = new[]
            {
                // 横练功（罩门皮肉硬功·常驻减伤底盘，提供永久 henglian，定 sumArtPower 主体）。henglian 叠加落 AddResource。
                new ArtCategoryDef("横练功", "attack", 1, 1, new[]
                {
                    new ArtDef("ti_hl_tiebushan", "铁布衫", 2, "横练功",
                        new[] { AddHenglian(12, "henglian+12;受钝击类武力威额外−4(抗砸抗摔)") }),
                    new ArtDef("ti_hl_jinzhongzhao", "金钟罩", 3, "横练功",
                        new[] { AddHenglian(18, "henglian+18;罩门收束至单点(章门),非罩门处减伤额外+3") }),
                    new ArtDef("ti_hl_shisantaibao", "十三太保横练", 4, "横练功",
                        new[]
                        {
                            AddHenglian(24, "henglian+24;每被穿透1次永久henglian+1(挨打越硬,上限+10)"),
                            Passive("henglian_growth", "'十三太保':每被穿透1次永久henglian+1"),
                        }),
                    new ArtDef("ti_hl_jingang", "金刚不坏体神功", 5, "横练功",
                        new[]
                        {
                            AddHenglian(32, "henglian+32"),
                            Passive("weakpoint_hardened", "realm≥5免疫'罩门×2'加成(罩门彻底淬实)"),
                        }),
                    new ArtDef("ti_hl_bumiejinshen", "不灭金身决", 6, "横练功",
                        new[]
                        {
                            AddHenglian(40, "henglian+40"),
                            Passive("revive_twice", "不灭金身复活次数1→2,复活后henglian不砍半"),
                        }),
                }),
                // 拳脚功（血肉输出·把武力转穿透与连击，本路'招式'，定来袭武力威与破横练能力）。武力威改判定为 flavor（Note）。
                new ArtCategoryDef("拳脚功", "movement", 1, 1, new[]
                {
                    new ArtDef("ti_qj_luohanquan", "罗汉拳", 1, "拳脚功",
                        new[] { Passive("combo", "出手武力威+6;连击第2下起每下额外+1") }),
                    new ArtDef("ti_qj_tieshazhang", "铁砂掌", 2, "拳脚功",
                        new[] { Passive("break_henglian", "出手武力威+10;攻击附带破横练−5(削对方henglian当次)") }),
                    new ArtDef("ti_qj_bengquan", "崩拳", 3, "拳脚功",
                        new[] { Passive("strike_weakpoint", "出手武力威+14;命中后25%直击罩门(无视横练值)") }),
                    new ArtDef("ti_qj_jingangzhi", "大力金刚指", 4, "拳脚功",
                        new[] { Passive("pierce_henglian", "出手武力威+20;穿透:忽略对方henglian的1/3") }),
                    new ArtDef("ti_qj_bawangjuding", "霸王举鼎", 5, "拳脚功",
                        new[]
                        {
                            new EffectOp(EffectOpKind.AddResource, "qixue", -20, "耗血气20本击无视全部henglian(纯肉身碾压)"),
                            Passive("ignore_all_henglian", "出手武力威+28;本击无视全部henglian"),
                        }),
                }),
                // 血气吐纳法（资源引擎·养血气与肉身回复，定 qixue 上限与回充 → 落 AddResourceCap，对抗放风筝消耗）。
                new ArtCategoryDef("血气吐纳法", "internal", 1, 1, new[]
                {
                    new ArtDef("ti_xt_guixi", "龟息吐纳", 1, "血气吐纳法",
                        new[] { CapQixue(40, "qixue上限+40;静修每跳回血气+5(耐久续航底)") }),
                    new ArtDef("ti_xt_dari", "大日如来掌·气血篇", 3, "血气吐纳法",
                        new[] { CapQixue(70, "qixue上限+70;以内力换血气:每点内力额外折2点血气上限") }),
                    new ArtDef("ti_xt_yijin", "易筋经", 4, "血气吐纳法",
                        new[]
                        {
                            CapQixue(90, "qixue上限+90;每场战后回满血气;根骨成长速率+1/阶"),
                            Passive("postwar_qixue_full", "'易筋':每场战斗结束回满血气"),
                        }),
                    new ArtDef("ti_xt_xisui", "洗髓经", 5, "血气吐纳法",
                        new[]
                        {
                            CapQixue(120, "qixue上限+120"),
                            Passive("burn_blood_3x", "濒死自动燃血护体折算2→3(每点血气抵3伤)"),
                        }),
                    new ArtDef("ti_xt_jiuzhuan", "九转玄功", 6, "血气吐纳法",
                        new[]
                        {
                            CapQixue(160, "qixue上限+160"),
                            Passive("revive_reset_weakpoint", "不灭金身触发时连带重置罩门威胁为0(满血回场)"),
                        }),
                }),
                // bodyheart 道心类目（M1，补遗第六部「金石不移心」）。A.0 仅装载不结算 → tier=0（sumArtPower 贡献 0）、
                // effects 留空（不触 daoHeart/innerDemon/comprehension 资源算子,那是 A.2 道心层的事）。具名 + power=0。
                new ArtCategoryDef("武胆", "daoheart", 1, 1, new[]
                {
                    new ArtDef("dh_ti_zhagen", "扎根桩心诀", 0, "武胆", System.Array.Empty<EffectOp>()),
                    new ArtDef("dh_ti_aida", "挨打养性功", 0, "武胆", System.Array.Empty<EffectOp>()),
                    new ArtDef("dh_ti_jinshi", "金石不移心经", 0, "武胆", System.Array.Empty<EffectOp>()),
                    new ArtDef("dh_ti_wugu", "武骨铮铮志", 0, "武胆", System.Array.Empty<EffectOp>()),
                }),
            };

            // —— 战技（深度设计「战技」节，OnUse 算子 + Cost 资源表；弹药=qixue 血气）。
            //    伤害/穿透等具体结算 Phase 3 接，A.0 以 AddPenInteger 占位破防量（量级对齐该路公式）+ Cost 表达资源门槛。——
            var skills = new[]
            {
                // 燃血狂攻：燃尽当前血气,每10点转来袭武力威+6并附穿透,打空对手放风筝窗口。门槛血气≥30,清空全部。
                // B5批2: → PenFromResource(qixue,6,÷10) 血气转伤(每10点血气+6,满池约+120窗口爆发;血池越满越痛,见底哑火)。
                new CombatSkillDef("sk_ti_ranxue", "燃血狂攻", 3,
                    new[] { Modules.PenFromResource("qixue", 6, div: 10, note: "燃尽当前血气,每10点转武力威+6并附穿透(资源转伤,满池约+120的窗口爆发)") },
                    new Dictionary<string, int> { { "qixue", 30 } }),
                // 金身震：对周身所有交互对象造根骨/2真实伤害并打断蓄力,反制围杀消耗。血气≥30,消耗30。
                new CombatSkillDef("sk_ti_jinshenzhen", "金身震", 4,
                    new[] { Modules.FlatPen(15, "对周身所有交互对象造根骨/2真实伤害并打断其蓄力(反围杀)") },
                    new Dictionary<string, int> { { "qixue", 30 } }),
                // 舍身撞：无视双方henglian互拼,按武力+根骨对轰;体修血气垫伤多半赢对耗。血气≥25,消耗25(+自伤flavor)。
                new CombatSkillDef("sk_ti_sheshenzhuang", "舍身撞", 4,
                    new[] { Modules.FlatPen(40, "无视双方henglian互拼,按武力+根骨对轰(血气垫伤多半赢对耗)") },
                    new Dictionary<string, int> { { "qixue", 25 } }),
                // 金钟落锁(封罩门)：1回合彻底封闭自身罩门,免疫'罩门×2'与直击罩门类战技。血气≥20,消耗20。
                new CombatSkillDef("sk_ti_jinzhongluosuo", "金钟落锁", 3,
                    new[] { Modules.FlatDR(20, "1回合封闭自身罩门,免疫'罩门×2'与直击罩门类战技(专破打罩门套路)") },
                    new Dictionary<string, int> { { "qixue", 20 } }),
                // 横练护体(铁山靠)：1回合内henglian×2,把整段攻势硬扛成擦伤;结束后复原。血气≥15,消耗15。
                // B5批2: → ReflectDamage(OnDefend,÷2) 铁山靠硬扛反震(入伤的1/2反弹攻方;时序读扣血前/不递归批4接,本轮ApplyOnUse不改入伤)。
                new CombatSkillDef("sk_ti_henglianhuti", "横练护体·铁山靠", 2,
                    new[] { Modules.Reflect(1, 2, "1回合henglian×2硬扛整段攻势,入伤1/2反震攻方(铁山靠)") },
                    new Dictionary<string, int> { { "qixue", 15 } }),
                // 横练闪身：体修闪避（OnDefend）。需拳脚功→门控。血气≥8,消耗8。
                // B5补缺：Evade 模块 — 体修靠拳脚功练就的闪身卸力, Amount=20→20%来袭伤害减免(功法门控)。
                new CombatSkillDef("sk_ti_henglian_shanshen", "横练闪身", 2,
                    new[] { Modules.Evade(20, "拳脚功闪身卸力:20%来袭伤害减免(需拳脚功→门控)") },
                    new Dictionary<string, int> { { "qixue", 8 } }),
                // 不灭金身(被动·濒死自启)：realm≥6每场首次濒死自动燃尽血气复活并回50%体力;血气=0不触发。无血气门槛(被动)。
                new CombatSkillDef("sk_ti_bumiejinshen", "不灭金身", 6,
                    new[] { Modules.FlatDR(0, "realm≥6每场首次濒死自动燃尽血气复活并回50%体力(克制点:血气=0不触发,被动)") },
                    new Dictionary<string, int>()),
            };

            return new CultivationPathDef(
                "ti_xiu_hengshi", "炼体·横世武夫",
                "physical",
                // 属性/形态 tag（melee 近战 / brute 蛮力肉身 / body 横练耐久），非对手 PathId（R2）。
                new[] { "melee", "brute", "body" },
                resources,
                power,
                curve,
                arts,
                skills,
                new EntryGateDef("tag:body_root"),
                new SelectionRuleDef(2, 2), // 战技抽 2（深度设计'1+1+1主修+2战技',realm≥6 自动获被动不占位）
                null);
        }
    }
}
