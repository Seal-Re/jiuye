using Jianghu.Cultivation;
using Xunit;

namespace Jianghu.Core.Tests.Cultivation
{
    /// <summary>
    /// B5 模块工厂 Modules 单元验证：工厂产出的 EffectOp 字段正确——
    /// 重点验易漏参（ratio-Kind 的 Amount2≥1 自动钳、Reflect 自动 OnDefend、稀有档 Rarity），
    /// 这些散写时最易出错，集中到工厂后由本测试守门。
    /// </summary>
    public class ModulesTests
    {
        [Fact]
        public void FlatPen_IsCommonOnUsePen()
        {
            var op = Modules.FlatPen(40, "x");
            Assert.Equal(EffectOpKind.AddPenInteger, op.Kind);
            Assert.Equal(40, op.Amount);
            Assert.Equal(EffectRarity.Common, op.Rarity);
            Assert.Equal(EffectTrigger.OnUse, op.Trigger);
        }

        [Fact]
        public void PenFromResource_DefaultDiv1_Rare()
        {
            var op = Modules.PenFromResource("swordWill", 4);
            Assert.Equal(EffectOpKind.PenFromResource, op.Kind);
            Assert.Equal("swordWill", op.Key);
            Assert.Equal(4, op.Amount);
            Assert.Equal(1, op.Amount2);              // div 默认 1（≥1 不抛）
            Assert.Equal(EffectRarity.Rare, op.Rarity);
        }

        [Fact]
        public void PenFromResource_DivBelow1_ClampedTo1()   // §15.6 消双义：div<1 自动钳 1
        {
            Assert.Equal(1, Modules.PenFromResource("qixue", 6, div: 0).Amount2);
            Assert.Equal(10, Modules.PenFromResource("qixue", 6, div: 10).Amount2);
        }

        [Fact]
        public void CounterMul_TagAndDen_Rare()
        {
            var op = Modules.CounterMul("evil", 3, 2);
            Assert.Equal(EffectOpKind.CounterMul, op.Kind);
            Assert.Equal("evil", op.Key);
            Assert.Equal(3, op.Amount);
            Assert.Equal(2, op.Amount2);
            Assert.Equal(EffectRarity.Rare, op.Rarity);
        }

        [Fact]
        public void Reflect_AutoOnDefend_DivClamped()   // §15.5 Reflect 固定 OnDefend
        {
            var op = Modules.Reflect(1, 2);
            Assert.Equal(EffectOpKind.ReflectDamage, op.Kind);
            Assert.Equal(EffectTrigger.OnDefend, op.Trigger);   // 工厂强制
            Assert.Equal(2, op.Amount2);
            Assert.Equal(1, Modules.Reflect(1, 0).Amount2);     // div<1 钳 1
        }

        [Fact]
        public void AoeBacklashDrain_RareOnUse()
        {
            Assert.Equal(EffectOpKind.AoePerTarget, Modules.AoePerTarget(25).Kind);
            Assert.Equal(EffectOpKind.Backlash, Modules.Backlash("lowHp", 10).Kind);
            var drain = Modules.Drain("itemTier", 2);
            Assert.Equal(EffectOpKind.DrainResource, drain.Kind);
            Assert.Equal(EffectRarity.Rare, drain.Rarity);
        }

        [Fact]
        public void DotControl_CarryTurnsInAmount2OrAmount()
        {
            var dot = Modules.Dot("poison", perTick: 5, turns: 3);
            Assert.Equal(EffectOpKind.Dot, dot.Kind);
            Assert.Equal(5, dot.Amount);
            Assert.Equal(3, dot.Amount2);     // turns
            var ctrl = Modules.Control("stun", turns: 2);
            Assert.Equal(EffectOpKind.Control, ctrl.Kind);
            Assert.Equal(2, ctrl.Amount);
        }

        [Fact]
        public void FlatDR_Evade_OnDefend()
        {
            Assert.Equal(EffectTrigger.OnDefend, Modules.FlatDR(20).Trigger);
            Assert.Equal(EffectTrigger.OnDefend, Modules.Evade(3).Trigger);
        }
    }
}
