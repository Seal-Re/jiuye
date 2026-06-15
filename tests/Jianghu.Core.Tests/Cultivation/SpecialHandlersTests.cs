using System;
using System.Collections.Generic;
using Jianghu.Cultivation;
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
        public void test_special_goldenBodyMax_returns_zero_marker_deferred_to_batch4()
        {
            // Arrange：佛修资源 goldenLayers/vow 在场（验 handler 不污染它们）。
            var ctx = MakeCtx(attackerRes: new[] { ("goldenLayers", 0, 9, 3), ("vow", 0, 9999, 1500) });

            // Act：不动明王怒目 Special（金身大成态 DR×2/3 回合标记 → 批4 回合循环消费）。
            int dmg = ModuleResolver.ApplyOnUse(100, Modules.Special("goldenBodyMax"), ctx);

            // Assert：占位返 0 delta（dmg 不变），且不写 goldenLayers/vow（炼体层语义不被污染，红线 A.8）。
            Assert.Equal(100, dmg);
            Assert.Equal(3, ctx.ReadResource(Side.Attacker, "goldenLayers"));
            Assert.Equal(1500, ctx.ReadResource(Side.Attacker, "vow"));
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
        public void test_special_registry_all_five_handlers_registered()
        {
            // 5 唯一档全注册，HandlerId 对应单例。
            Assert.Equal("luobao", SpecialModuleRegistry.Get("luobao").HandlerId);
            Assert.Equal("explodeArray", SpecialModuleRegistry.Get("explodeArray").HandlerId);
            Assert.Equal("goldenBodyMax", SpecialModuleRegistry.Get("goldenBodyMax").HandlerId);
            Assert.Equal("fieldActive", SpecialModuleRegistry.Get("fieldActive").HandlerId);
            Assert.Equal("duoshe", SpecialModuleRegistry.Get("duoshe").HandlerId);
        }

        [Fact]
        public void test_special_registry_get_unknown_throws()
        {
            // 缺失 id 查询抛（不静默回 null，spec §7）。
            Assert.Throws<InvalidOperationException>(() => SpecialModuleRegistry.Get("nonexistent"));
        }
    }
}
