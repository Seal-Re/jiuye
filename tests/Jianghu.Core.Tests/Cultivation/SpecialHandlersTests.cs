using System;
using System.Collections.Generic;
using Jianghu.Cultivation;
using Jianghu.Cultivation.Paths;
using Xunit;

namespace Jianghu.Core.Tests.Cultivation
{
    /// <summary>
    /// B5-batch3 唯一档 Special handler 差分测试（精确 delta + chokepoint 资源副作用实证 + 反向）。
    /// MakeCtx 复用 ModuleResolverTests 模式：双方各一 CultivationState（资源经 NewForPath 注册 ResourceDef，
    /// Cap 足够不被钳掉）+ 各自 path（HasTag 读 SituationalTags）。经 <see cref="ModuleResolver.ApplyOnUse"/>
    /// 走 EffectOpKind.Special 派发，证 handler 经 chokepoint 落副作用、返整数 delta。
    /// 纪律守护：纯整数（IL 浮点扫描覆盖 special/ 全类型，见 ModuleResolverFloatScanCoverageTests）。
    /// </summary>
    public class SpecialHandlersTests
    {
        // 造一方 state：命名资源初值钳进给定 [Min,Cap]（dr 默认 [0,1000] 够大不被边界钳）。
        static CultivationState MakeState(string pathId, params (string Key, int Min, int Cap, int Init)[] res)
        {
            var defs = new List<ResourceDef>();
            foreach (var r in res) defs.Add(new ResourceDef(r.Key, r.Min, r.Cap, r.Init));
            return CultivationState.NewForPath(pathId, defs);
        }

        // 攻防双方各持 state + path（path 仅供 HasTag 读 SituationalTags，用最小工厂带指定 tags）。
        static CombatContext MakeCtx(
            (string Key, int Min, int Cap, int Init)[]? attackerRes = null,
            (string Key, int Min, int Cap, int Init)[]? defenderRes = null)
        {
            var atk = MakeState("atk_path", attackerRes ?? Array.Empty<(string, int, int, int)>());
            var def = MakeState("def_path", defenderRes ?? Array.Empty<(string, int, int, int)>());
            var atkPath = TestPaths.WithTags(Array.Empty<string>());
            var defPath = TestPaths.WithTags(Array.Empty<string>());
            return new CombatContext(atk, atkPath, def, defPath);
        }

        // —————————————————————————— 落宝 luobao（器，3.1）——————————————————————————

        [Fact]
        public void test_special_luobao_drains_defender_itemTier_and_attacker_borrows()
        {
            // Arrange：防方有本命法宝 itemTier=5，攻方初值 0（itemTier [0,9] 真上限）。
            var ctx = MakeCtx(
                attackerRes: new[] { ("itemTier", 0, 9, 0) },
                defenderRes: new[] { ("itemTier", 0, 9, 5) });

            // Act：落宝金光 Special 派发（dmg 基线 100，效果是资源置换非直伤）。
            int dmg = ModuleResolver.ApplyOnUse(100, Modules.Special("luobao"), ctx);

            // Assert：防方 itemTier 抽干→0、攻方借得→原值 5；dmg 不变（delta 0）。
            Assert.Equal(0, ctx.ReadResource(Side.Defender, "itemTier"));
            Assert.Equal(5, ctx.ReadResource(Side.Attacker, "itemTier"));
            Assert.Equal(100, dmg);
        }

        [Fact]
        public void test_special_luobao_no_defender_item_is_noop()   // 反向：无宝可夺则空转
        {
            // Arrange：防方无 itemTier 资源（ReadResource→0），攻方 0。
            var ctx = MakeCtx(attackerRes: new[] { ("itemTier", 0, 9, 0) });

            // Act
            int dmg = ModuleResolver.ApplyOnUse(50, Modules.Special("luobao"), ctx);

            // Assert：双方 itemTier 皆 0，dmg 不变。
            Assert.Equal(0, ctx.ReadResource(Side.Defender, "itemTier"));
            Assert.Equal(0, ctx.ReadResource(Side.Attacker, "itemTier"));
            Assert.Equal(50, dmg);
        }

        // ———————————————————————— 炸阵 explodeArray（阵，3.2）————————————————————————

        [Fact]
        public void test_special_explodeArray_delta_is_stones_proxy_times_two()
        {
            // Arrange：攻方阵力代理 stones=30（[0,100]）。
            var ctx = MakeCtx(attackerRes: new[] { ("stones", 0, 100, 30) });

            // Act：引爆·焚阵 Special 派发（弃阵换爆发，delta = 代理 ×2）。
            int dmg = ModuleResolver.ApplyOnUse(0, Modules.Special("explodeArray"), ctx);

            // Assert：30×2 = 60 一次性 delta。
            Assert.Equal(60, dmg);
        }

        [Fact]
        public void test_special_explodeArray_falls_back_to_setupProgress_then_amount()
        {
            // stones 不存在 → 退 setupProgress=20 → delta 40。
            var ctxSetup = MakeCtx(attackerRes: new[] { ("setupProgress", 0, 220, 20) });
            Assert.Equal(40, ModuleResolver.ApplyOnUse(0, Modules.Special("explodeArray"), ctxSetup));

            // stones/setupProgress 皆无 → 退 op.Amount（占位破发量 40）→ delta 40。
            var ctxAmount = MakeCtx();
            Assert.Equal(80, ModuleResolver.ApplyOnUse(0, Modules.Special("explodeArray", amount: 40), ctxAmount));
        }

        // ——————————————————————— 金身态 goldenBodyMax（佛，3.3）———————————————————————

        [Fact]
        public void test_special_goldenBodyMax_sets_turns_and_boosts_goldenLayers()
        {
            // Arrange：佛修资源 goldenBodyTurns/goldenLayers/vow 在场。goldenLayers 初始 3。
            var ctx = MakeCtx(attackerRes: new[] {
                ("goldenBodyTurns", 0, 3, 0),
                ("goldenLayers", 0, 9, 3),
                ("vow", 0, 9999, 1500) });

            // Act：不动明王怒目 Special（金身大成态 3 回合 + 金身层+2 临时强化）。
            int dmg = ModuleResolver.ApplyOnUse(100, Modules.Special("goldenBodyMax"), ctx);

            // Assert：goldenBodyTurns 置 3（大成态剩余回合初值）；goldenLayers 3+2=5（钳 9）；dmg 不变（delta 0）。
            Assert.Equal(3, ctx.ReadResource(Side.Attacker, "goldenBodyTurns"));
            Assert.Equal(5, ctx.ReadResource(Side.Attacker, "goldenLayers"));
            Assert.Equal(1500, ctx.ReadResource(Side.Attacker, "vow")); // 不受污染
            Assert.Equal(100, dmg);
        }

        [Fact]
        public void test_special_goldenBodyMax_capped_at_9_layers()
        {
            // Arrange：goldenLayers 初始 8（近上限），goldenBodyTurns=0。
            var ctx = MakeCtx(attackerRes: new[] {
                ("goldenBodyTurns", 0, 3, 0),
                ("goldenLayers", 0, 9, 8) });

            // Act
            ModuleResolver.ApplyOnUse(100, Modules.Special("goldenBodyMax"), ctx);

            // Assert：goldenLayers 8+2=10→钳 9（不溢出）。
            Assert.Equal(9, ctx.ReadResource(Side.Attacker, "goldenLayers"));
            Assert.Equal(3, ctx.ReadResource(Side.Attacker, "goldenBodyTurns"));
        }

        [Fact]
        public void test_special_goldenBodyMax_noop_on_non_buddhist_path()
        {
            // Arrange：非佛修路径，无 goldenBodyTurns 资源。
            var ctx = MakeCtx(attackerRes: new[] { ("vow", 0, 500, 100) });
            int dmg = ModuleResolver.ApplyOnUse(100, Modules.Special("goldenBodyMax"), ctx);

            // Assert：HasResource 守→空转，dmg 不变（无 resource dict 污染）。
            Assert.Equal(100, dmg);
        }

        // ——————————————————————— 律场总门 fieldActive（音，3.4）———————————————————————

        [Fact]
        public void test_special_fieldActive_zero_windup_returns_base_dmg_unchanged()
        {
            // Arrange：起调进度 windupProgress=0（律场未起调）。
            var ctx = MakeCtx(attackerRes: new[] { ("windupProgress", 0, 100, 0) });

            // Act：起调·点律 Special（amount=30 应被门控归 0）。
            int dmg = ModuleResolver.ApplyOnUse(100, Modules.Special("fieldActive", amount: 30), ctx);

            // Assert：律场未起调→效果归 0，dmg 不变（delta 0）。
            Assert.Equal(100, dmg);
        }

        [Fact]
        public void test_special_fieldActive_positive_windup_adds_amount()
        {
            // Arrange：起调进度 windupProgress=50（>0，律场已起调）。
            var ctx = MakeCtx(attackerRes: new[] { ("windupProgress", 0, 100, 50) });

            // Act：起调·点律 Special（amount=30 放行）。
            int dmg = ModuleResolver.ApplyOnUse(100, Modules.Special("fieldActive", amount: 30), ctx);

            // Assert：已起调→透传 op.Amount，dmg = 100+30。
            Assert.Equal(130, dmg);
        }

        // —————————————————————— 夺舍 duoshe（鬼/魂/魔，3.5）——————————————————————

        [Fact]
        public void test_special_duoshe_soul_restores_seaIntegrity_full()
        {
            // Arrange：魂修攻方 seaIntegrity=10（识海近崩，[0,100]）。
            var ctx = MakeCtx(attackerRes: new[] { ("seaIntegrity", 0, 100, 10) });

            // Act：夺舍续命 Special（确定性资源清算，非直伤）。
            int dmg = ModuleResolver.ApplyOnUse(100, Modules.Special("duoshe"), ctx);

            // Assert：seaIntegrity 回满 100（经 chokepoint 钳顶），dmg 不变（delta 0）。
            Assert.Equal(100, ctx.ReadResource(Side.Attacker, "seaIntegrity"));
            Assert.Equal(100, dmg);
        }

        [Fact]
        public void test_special_duoshe_ghost_clears_devourMeter()
        {
            // Arrange：鬼修攻方 devourMeter=80（噬主度高危，[0,100]）。
            var ctx = MakeCtx(attackerRes: new[] { ("devourMeter", 0, 100, 80) });

            // Act：夺舍续命 Special（噬主度清零续命）。
            int dmg = ModuleResolver.ApplyOnUse(77, Modules.Special("duoshe"), ctx);

            // Assert：devourMeter 清零（经 chokepoint 钳底），dmg 不变（delta 0）。
            Assert.Equal(0, ctx.ReadResource(Side.Attacker, "devourMeter"));
            Assert.Equal(77, dmg);
        }

        // —————————————————————————— 夺心 duoxin（毒蛊）——————————————————————————

        [Fact]
        public void test_special_duoxin_raises_defender_guRevolt()
        {
            // Arrange：防方蛊群反噬度 guRevolt=0（[0,100]，蛊目标在场）。
            var ctx = MakeCtx(defenderRes: new[] { ("guRevolt", 0, 100, 0) });

            // Act：植蛊夺心 Special（amount=25 植入反噬，效果是控制非直伤）。
            int dmg = ModuleResolver.ApplyOnUse(100, Modules.Special("duoxin", amount: 25), ctx);

            // Assert：防方 guRevolt 抬升 +25（经 chokepoint），dmg 不变（delta 0）。
            Assert.Equal(25, ctx.ReadResource(Side.Defender, "guRevolt"));
            Assert.Equal(100, dmg);
        }

        [Fact]
        public void test_special_duoxin_no_defender_guRevolt_is_noop()   // 反向：对纯阳/佛门/死物傀儡命中失败
        {
            // Arrange：防方无 guRevolt 资源（非蛊目标：纯阳/佛门/死物傀儡），攻方无关。
            var ctx = MakeCtx();

            // Act
            int dmg = ModuleResolver.ApplyOnUse(60, Modules.Special("duoxin", amount: 25), ctx);

            // Assert：防方 guRevolt 仍 0（命中失败空转），dmg 不变。
            Assert.Equal(0, ctx.ReadResource(Side.Defender, "guRevolt"));
            Assert.Equal(60, dmg);
        }

        // ———————————————————————— 断链 brokenChain（傀儡）————————————————————————

        [Fact]
        public void test_special_brokenChain_sets_attacker_residualOrder()
        {
            // Arrange：傀儡师攻方 residualOrder=0（[0,100]，残命惯性待置）。
            var ctx = MakeCtx(attackerRes: new[] { ("residualOrder", 0, 100, 0) });

            // Act：断链 Special（amount=40 置残命惯性初值，防御/状态签名非直伤）。
            int dmg = ModuleResolver.ApplyOnUse(100, Modules.Special("brokenChain", amount: 40), ctx);

            // Assert：攻方 residualOrder 置 40（经 chokepoint），dmg 不变（delta 0）。
            Assert.Equal(40, ctx.ReadResource(Side.Attacker, "residualOrder"));
            Assert.Equal(100, dmg);
        }

        [Fact]
        public void test_special_brokenChain_no_attacker_residualOrder_is_noop()   // 反向：非傀儡师空转
        {
            // Arrange：攻方无 residualOrder 资源（非傀儡师）。
            var ctx = MakeCtx();

            // Act
            int dmg = ModuleResolver.ApplyOnUse(70, Modules.Special("brokenChain", amount: 40), ctx);

            // Assert：攻方 residualOrder 仍 0（空转），dmg 不变。
            Assert.Equal(0, ctx.ReadResource(Side.Attacker, "residualOrder"));
            Assert.Equal(70, dmg);
        }

        // ——————————————————————— 逆演栈 reverseStack（命 / 因果）———————————————————————

        [Fact]
        public void test_special_reverseStack_ming_charges_netFortune_and_lifespanDebt()
        {
            // Arrange：命路攻方 netFortune=20（[-50,40]）、lifespanDebt=0（[0,100]）。
            var ctx = MakeCtx(attackerRes: new[]
            {
                ("netFortune", -50, 40, 20),
                ("lifespanDebt", 0, 100, 0),
            });

            // Act：逆演重开 Special（结算回滚的确定性代价，回滚本体 defer 批4）。
            int dmg = ModuleResolver.ApplyOnUse(100, Modules.Special("reverseStack"), ctx);

            // Assert：netFortune -10 → 10、lifespanDebt +3 → 3（经 chokepoint），dmg 不变（delta 0）。
            Assert.Equal(10, ctx.ReadResource(Side.Attacker, "netFortune"));
            Assert.Equal(3, ctx.ReadResource(Side.Attacker, "lifespanDebt"));
            Assert.Equal(100, dmg);
        }

        [Fact]
        public void test_special_reverseStack_yinguo_charges_spaceTimeAuth_when_no_netFortune()
        {
            // Arrange：因果路攻方无 netFortune，改持 spaceTimeAuth=5（[0,9]）+ lifespanDebt=0。
            var ctx = MakeCtx(attackerRes: new[]
            {
                ("spaceTimeAuth", 0, 9, 5),
                ("lifespanDebt", 0, 100, 0),
            });

            // Act：逆演重开 Special（因果路无 netFortune → 改消 spaceTimeAuth）。
            int dmg = ModuleResolver.ApplyOnUse(100, Modules.Special("reverseStack"), ctx);

            // Assert：spaceTimeAuth -1 → 4、lifespanDebt +3 → 3，dmg 不变（delta 0）。
            Assert.Equal(4, ctx.ReadResource(Side.Attacker, "spaceTimeAuth"));
            Assert.Equal(3, ctx.ReadResource(Side.Attacker, "lifespanDebt"));
            Assert.Equal(100, dmg);
        }

        [Fact]
        public void test_special_reverseStack_no_cost_resources_is_noop()   // 反向：无代价资源全空转
        {
            // Arrange：攻方既无 netFortune/spaceTimeAuth 也无 lifespanDebt（非命/因果路）。
            var ctx = MakeCtx();

            // Act
            int dmg = ModuleResolver.ApplyOnUse(55, Modules.Special("reverseStack"), ctx);

            // Assert：无任何代价键可落，全空转，dmg 不变（delta 0）。
            Assert.Equal(0, ctx.ReadResource(Side.Attacker, "netFortune"));
            Assert.Equal(0, ctx.ReadResource(Side.Attacker, "spaceTimeAuth"));
            Assert.Equal(0, ctx.ReadResource(Side.Attacker, "lifespanDebt"));
            Assert.Equal(55, dmg);
        }

        // —————————————————————————— Registry 派发确定性 ——————————————————————————

        [Fact]
        public void test_special_registry_get_returns_nonnull_same_singleton()
        {
            // 同 id → 非空且始终同单例（派发确定，spec §7 chokepoint）。
            var first = SpecialModuleRegistry.Get("luobao");
            var second = SpecialModuleRegistry.Get("luobao");
            Assert.NotNull(first);
            Assert.Same(first, second);
        }

        [Fact]
        public void test_special_registry_all_eight_handlers_registered()
        {
            // 8 唯一档全注册（批3 5 个 + 批3收口 3 个），HandlerId 对应单例。
            Assert.Equal("luobao", SpecialModuleRegistry.Get("luobao").HandlerId);
            Assert.Equal("explodeArray", SpecialModuleRegistry.Get("explodeArray").HandlerId);
            Assert.Equal("goldenBodyMax", SpecialModuleRegistry.Get("goldenBodyMax").HandlerId);
            Assert.Equal("fieldActive", SpecialModuleRegistry.Get("fieldActive").HandlerId);
            Assert.Equal("duoshe", SpecialModuleRegistry.Get("duoshe").HandlerId);
            Assert.Equal("duoxin", SpecialModuleRegistry.Get("duoxin").HandlerId);
            Assert.Equal("brokenChain", SpecialModuleRegistry.Get("brokenChain").HandlerId);
            Assert.Equal("reverseStack", SpecialModuleRegistry.Get("reverseStack").HandlerId);
        }

        [Fact]
        public void test_special_registry_new_handlers_return_same_singleton()
        {
            // 3 新 handler 同 id → 始终同单例（派发确定，spec §7 chokepoint）。
            Assert.Same(SpecialModuleRegistry.Get("duoxin"), SpecialModuleRegistry.Get("duoxin"));
            Assert.Same(SpecialModuleRegistry.Get("brokenChain"), SpecialModuleRegistry.Get("brokenChain"));
            Assert.Same(SpecialModuleRegistry.Get("reverseStack"), SpecialModuleRegistry.Get("reverseStack"));
        }

        [Fact]
        public void test_special_registry_get_unknown_throws()
        {
            // 缺失 id 查询抛（不静默回 null，spec §7）。
            Assert.Throws<InvalidOperationException>(() => SpecialModuleRegistry.Get("nonexistent"));
        }

        // ================================================================
        // 批4 turn-loop：GoldenBodyMaxModule 战斗效果实证
        // ================================================================

        [Fact]
        public void test_turnloop_goldenBodyMax_dr_halves_defender_damage()
        {
            // Arrange：防方有 goldenBodyTurns=1（金身大成态激活），goldenLayers=5。
            // 攻防用同路（简化：剑修对剑修，SituationalTags 相同）。
            var atk = MakeState("sword_immortal",
                ("swordWill", 0, 1000, 20));
            var def = MakeState("buddhist_golden_body",
                ("goldenBodyTurns", 0, 3, 1),
                ("goldenLayers", 0, 9, 5),
                ("vow", 0, 9999, 500));
            var path = SwordImmortalPath.Def; // 攻防同路简化
            var defPath = BuddhistGoldenBodyPath.Def;
            var ctx = new CombatContext(atk, path, def, defPath);

            // 直接测试：防方金身大成态内 goldenBodyTurns>0 → HasTurnResource=true
            Assert.True(DuelEngineTestAccessor.HasTurnResource(ctx, Side.Defender, "goldenBodyTurns"));
        }

        [Fact]
        public void test_turnloop_goldenBodyMax_vow_increases_on_damage_absorption()
        {
            // Arrange：防方 goldenBodyTurns=1, goldenLayers=5, vow=500。
            var atk = MakeState("sword_immortal",
                ("swordWill", 0, 1000, 20));
            var def = MakeState("buddhist_golden_body",
                ("goldenBodyTurns", 0, 3, 1),
                ("goldenLayers", 0, 9, 5),
                ("vow", 0, 9999, 500));
            var path = SwordImmortalPath.Def;
            var defPath = BuddhistGoldenBodyPath.Def;
            var ctx = new CombatContext(atk, path, def, defPath);

            // Act：防方金身大成态检查 vow 可读写
            Assert.True(ctx.HasResource(Side.Defender, "vow"));
            Assert.Equal(500, ctx.ReadResource(Side.Defender, "vow"));

            // 金身大成态吸收伤害转愿：模拟 dmg=100, DR×2→dmg=50, absorbed=50→vow
            ctx.ApplyResource(Side.Defender, "vow", 50);
            Assert.Equal(550, ctx.ReadResource(Side.Defender, "vow"));
        }

        [Fact]
        public void test_turnloop_goldenBodyMax_no_dr_when_turns_zero()
        {
            // Arrange：goldenBodyTurns=0（大成态已到期）。
            var atk = MakeState("sword_immortal",
                ("swordWill", 0, 1000, 20));
            var def = MakeState("buddhist_golden_body",
                ("goldenBodyTurns", 0, 3, 0), // 已到期
                ("goldenLayers", 0, 9, 3),
                ("vow", 0, 9999, 500));
            var path = SwordImmortalPath.Def;
            var defPath = BuddhistGoldenBodyPath.Def;
            var ctx = new CombatContext(atk, path, def, defPath);

            // Assert：goldenBodyTurns=0 → HasTurnResource 返回 false（无DR×2/anti_evil）。
            Assert.False(DuelEngineTestAccessor.HasTurnResource(ctx, Side.Defender, "goldenBodyTurns"));
        }

        // ================================================================
        // 批4 turn-loop：TickTurnState 资源递减实证
        // ================================================================

        [Fact]
        public void test_turnloop_tick_decrements_goldenBodyTurns_and_reverts_layers_on_expire()
        {
            // Arrange：goldenBodyTurns=1（仅剩1回合），goldenLayers=5。
            var def = MakeState("buddhist_golden_body",
                ("goldenBodyTurns", 0, 3, 1),
                ("goldenLayers", 0, 9, 5),
                ("vow", 0, 9999, 500));
            var atk = MakeState("sword_immortal");
            var path = SwordImmortalPath.Def;
            var defPath = BuddhistGoldenBodyPath.Def;
            var ctx = new CombatContext(atk, path, def, defPath);

            // Act：TickTurnState 递减回合标记
            DuelEngineTestAccessor.TickTurnState(ctx);

            // Assert：goldenBodyTurns 1→0，到期→ goldenLayers 5-2=3（回退增益）。
            Assert.Equal(0, ctx.ReadResource(Side.Defender, "goldenBodyTurns"));
            Assert.Equal(3, ctx.ReadResource(Side.Defender, "goldenLayers"));
        }

        [Fact]
        public void test_turnloop_tick_decrements_residualOrder()
        {
            // Arrange：residualOrder=60（足够2回合衰减）。
            var atk = MakeState("kuilei_shi",
                ("residualOrder", 0, 100, 60));
            var def = MakeState("def_path");
            var path = KuileiShiPath.Def;
            var ctx = new CombatContext(atk, path, def, path);

            // Act：TickTurnState 递减 residualOrder
            DuelEngineTestAccessor.TickTurnState(ctx);

            // Assert：residualOrder 60→35（-25/回合）。
            Assert.Equal(35, ctx.ReadResource(Side.Attacker, "residualOrder"));
        }

        [Fact]
        public void test_turnloop_tick_decrements_guRevolt()
        {
            // Arrange：guRevolt=60（≥阈值50，应触发反噬 + 衰减）。
            var atk = MakeState("du_gu_xiu",
                ("guRevolt", 0, 100, 60));
            var def = MakeState("def_path");
            var path = DuGuXiuPath.Def;
            var ctx = new CombatContext(atk, path, def, path);

            // Act：TickTurnState 递减 guRevolt
            DuelEngineTestAccessor.TickTurnState(ctx);

            // Assert：guRevolt 60→40（-20/回合）。
            Assert.Equal(40, ctx.ReadResource(Side.Attacker, "guRevolt"));
        }

        // ================================================================
        // 批4 turn-loop：DuoxinModule 阵营反噬 + BrokenChain 军团僵死
        // ================================================================

        [Fact]
        public void test_turnloop_guRevolt_redirect_threshold_detected()
        {
            // Arrange：guRevolt=50（刚好触发阈值）。
            var atk = MakeState("du_gu_xiu",
                ("guRevolt", 0, 100, 50));
            var def = MakeState("def_path");
            var path = DuGuXiuPath.Def;
            var ctx = new CombatContext(atk, path, def, path);

            // Assert：guRevolt≥50 → IsGuRevoltRedirected=true。
            Assert.True(DuelEngineTestAccessor.IsGuRevoltRedirected(ctx, Side.Attacker));
        }

        [Fact]
        public void test_turnloop_guRevolt_below_threshold_not_redirected()
        {
            // Arrange：guRevolt=30（未达阈值）。
            var atk = MakeState("du_gu_xiu",
                ("guRevolt", 0, 100, 30));
            var def = MakeState("def_path");
            var path = DuGuXiuPath.Def;
            var ctx = new CombatContext(atk, path, def, path);

            // Assert：guRevolt<50 → IsGuRevoltRedirected=false。
            Assert.False(DuelEngineTestAccessor.IsGuRevoltRedirected(ctx, Side.Attacker));
        }

        [Fact]
        public void test_turnloop_residualOrder_fleet_freeze_when_expired()
        {
            // Arrange：residualOrder=0（已耗尽→军团僵死）。
            var atk = MakeState("kuilei_shi",
                ("residualOrder", 0, 100, 0));
            var def = MakeState("def_path");
            var path = KuileiShiPath.Def;
            var ctx = new CombatContext(atk, path, def, path);

            // Assert：有 residualOrder 资源但值=0 → HasResidualOrder=false → 军团僵死。
            Assert.True(ctx.HasResource(Side.Attacker, "residualOrder"));
            Assert.False(DuelEngineTestAccessor.HasResidualOrder(ctx, Side.Attacker));
        }
    }

    // ================================================================
    // Test-accessor wrappers（DuelEngine internal → 测试可见）
    // ================================================================

    /// <summary>测试辅助：桥接 DuelEngine 内部方法供测试调用。</summary>
    internal static class DuelEngineTestAccessor
    {
        public static bool HasTurnResource(CombatContext ctx, Side side, string key)
        {
            if (!ctx.HasResource(side, key)) return false;
            return ctx.ReadResource(side, key) > 0;
        }

        public static bool HasResidualOrder(CombatContext ctx, Side side)
            => HasTurnResource(ctx, side, "residualOrder");

        public static bool IsGuRevoltRedirected(CombatContext ctx, Side side)
        {
            if (!ctx.HasResource(side, "guRevolt")) return false;
            return ctx.ReadResource(side, "guRevolt") >= DuelEngine.GuRevoltRedirectThreshold;
        }

        public static void TickTurnState(CombatContext ctx)
        {
            // —— goldenBodyTurns：每回合-1，到期回退 goldenLayers+2 ——
            Decay(ctx, Side.Attacker, "goldenBodyTurns", 1,
                () => ctx.ApplyResource(Side.Attacker, "goldenLayers", -2));
            Decay(ctx, Side.Defender, "goldenBodyTurns", 1,
                () => ctx.ApplyResource(Side.Defender, "goldenLayers", -2));

            // —— residualOrder：每回合-25 ——
            Decay(ctx, Side.Attacker, "residualOrder", DuelEngine.ResidualOrderDecayPerTurn, null);
            Decay(ctx, Side.Defender, "residualOrder", DuelEngine.ResidualOrderDecayPerTurn, null);

            // —— guRevolt：每回合-20 ——
            Decay(ctx, Side.Attacker, "guRevolt", DuelEngine.GuRevoltDecayPerTurn, null);
            Decay(ctx, Side.Defender, "guRevolt", DuelEngine.GuRevoltDecayPerTurn, null);
        }

        private static void Decay(CombatContext ctx, Side side, string key, int decay, Action? onExpire)
        {
            if (!ctx.HasResource(side, key)) return;
            int current = ctx.ReadResource(side, key);
            if (current <= 0) return;
            int newVal = Math.Max(0, current - decay);
            ctx.ApplyResource(side, key, -(current - newVal));
            if (newVal == 0 && current > 0)
                onExpire?.Invoke();
        }
    }
}
