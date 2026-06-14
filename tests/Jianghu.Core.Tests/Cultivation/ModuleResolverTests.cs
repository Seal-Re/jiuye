using System;
using System.Collections.Generic;
using Jianghu.Cultivation;
using Xunit;

namespace Jianghu.Core.Tests.Cultivation
{
    /// <summary>
    /// B5-batch1 OnUse 分支差分测试（精确 delta，非 ≥0）+ 反向 + chokepoint 实证。
    /// MakeCtx 按真实 CombatContext/CultivationState 构造：双方各一 CultivationState
    /// （资源经 NewForPath 注册 ResourceDef，Cap 足够不被钳掉）+ 各自 path（HasTag 读 SituationalTags）。
    /// </summary>
    public class ModuleResolverTests
    {
        static EffectOp Op(EffectOpKind k, string? key, int amt, int amt2 = 0) =>
            new EffectOp(k, key, amt, null, Amount2: amt2);

        // 造一方 state：把命名资源初值钳进足够大的 [0,1000]，确保后续移动不被边界钳掉。
        static CultivationState MakeState(string pathId, (string Key, int Val)? res)
        {
            var defs = new List<ResourceDef>();
            if (res is { } r) defs.Add(new ResourceDef(r.Key, 0, 1000, r.Val));
            return CultivationState.NewForPath(pathId, defs);
        }

        // 攻防双方各持 state + path（path 仅供 HasTag 读 SituationalTags，用最小工厂带指定 tags）。
        static CombatContext MakeCtx(
            (string Key, int Val)? attackerRes = null,
            (string Key, int Val)? defenderRes = null,
            string[]? defenderTags = null)
        {
            var atk = MakeState("atk_path", attackerRes);
            var def = MakeState("def_path", defenderRes);
            var atkPath = TestPaths.WithTags(Array.Empty<string>());
            var defPath = TestPaths.WithTags(defenderTags ?? Array.Empty<string>());
            return new CombatContext(atk, atkPath, def, defPath);
        }

        [Fact]
        public void PenFromResource_AddsResourceTimesRatio()
        {
            var ctx = MakeCtx(attackerRes: ("qixie", 50));
            Assert.Equal(100 + 50 * 4 / 1,
                ModuleResolver.ApplyOnUse(100, Op(EffectOpKind.PenFromResource, "qixie", 4, 1), ctx));
        }

        [Fact]
        public void PenFromResource_ZeroResource_NoChange()   // 反向
        {
            var ctx = MakeCtx(attackerRes: ("qixie", 0));
            Assert.Equal(100,
                ModuleResolver.ApplyOnUse(100, Op(EffectOpKind.PenFromResource, "qixie", 4, 1), ctx));
        }

        [Fact]
        public void PenFromResource_Amount2Den_DivInteger()
        {
            // res(60)*Amount(3)/Den(2) = 90 → dmg 100+90=190（整数除）
            var ctx = MakeCtx(attackerRes: ("qixie", 60));
            Assert.Equal(190,
                ModuleResolver.ApplyOnUse(100, Op(EffectOpKind.PenFromResource, "qixie", 3, 2), ctx));
        }

        [Fact]
        public void AddPenInteger_AddsFlat()
        {
            var ctx = MakeCtx();
            Assert.Equal(140,
                ModuleResolver.ApplyOnUse(100, Op(EffectOpKind.AddPenInteger, null, 40), ctx));
        }

        [Fact]
        public void AoePerTarget_SingleDuelDegradesToFlatAdd()
        {
            // R2 单挑退化×1：dmg + Amount
            var ctx = MakeCtx();
            Assert.Equal(125,
                ModuleResolver.ApplyOnUse(100, Op(EffectOpKind.AoePerTarget, null, 25), ctx));
        }

        [Fact]
        public void CounterMul_OnlyWhenDefenderHasTag_AndCapped()
        {
            var with = MakeCtx(defenderTags: new[] { "evil" });
            Assert.Equal(100 * 3 / 2,
                ModuleResolver.ApplyOnUse(100, Op(EffectOpKind.CounterMul, "evil", 3, 2), with)); // ×3/2 命中
            var without = MakeCtx(defenderTags: Array.Empty<string>());
            Assert.Equal(100,
                ModuleResolver.ApplyOnUse(100, Op(EffectOpKind.CounterMul, "evil", 3, 2), without)); // 反向
        }

        [Fact]
        public void CounterMul_RawExceedsUpperBound_ClampedTo3Over2()
        {
            // raw = 100*5/1 = 500 > 上界 100*3/2=150 → Min(500,150)=150（上界真触发钳）
            var ctx = MakeCtx(defenderTags: new[] { "evil" });
            Assert.Equal(150,
                ModuleResolver.ApplyOnUse(100, Op(EffectOpKind.CounterMul, "evil", 5, 1), ctx));
        }

        [Fact]
        public void DrainResource_MovesViaChokepoint()
        {
            var ctx = MakeCtx(defenderRes: ("itemTier", 5), attackerRes: ("itemTier", 0));
            ModuleResolver.ApplyOnUse(0, Op(EffectOpKind.DrainResource, "itemTier", 2), ctx);
            Assert.Equal(3, ctx.ReadResource(Side.Defender, "itemTier"));
            Assert.Equal(2, ctx.ReadResource(Side.Attacker, "itemTier"));
        }

        [Fact]
        public void DrainResource_ReturnsDmgUnchanged()
        {
            var ctx = MakeCtx(defenderRes: ("itemTier", 5), attackerRes: ("itemTier", 0));
            Assert.Equal(77,
                ModuleResolver.ApplyOnUse(77, Op(EffectOpKind.DrainResource, "itemTier", 2), ctx));
        }

        [Fact]
        public void Backlash_ReturnsDmgUnchanged()   // 自伤走批4 selfDmg 通道，本 task 不处理
        {
            var ctx = MakeCtx();
            Assert.Equal(100,
                ModuleResolver.ApplyOnUse(100, Op(EffectOpKind.Backlash, "lowHp", 30), ctx));
        }

        [Fact]
        public void DotControlReflectEvade_HitDefaultReturnUnchanged()   // 批3/批4 处理 → default 不变
        {
            var ctx = MakeCtx();
            Assert.Equal(100, ModuleResolver.ApplyOnUse(100, Op(EffectOpKind.Dot, "poison", 5, 3), ctx));
            Assert.Equal(100, ModuleResolver.ApplyOnUse(100, Op(EffectOpKind.Control, "stun", 2), ctx));
            Assert.Equal(100, ModuleResolver.ApplyOnUse(100, Op(EffectOpKind.ReflectDamage, null, 2, 1), ctx));
            Assert.Equal(100, ModuleResolver.ApplyOnUse(100, Op(EffectOpKind.Evade, null, 3), ctx));
        }
    }
}
