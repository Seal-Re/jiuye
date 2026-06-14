using Jianghu.Cultivation;
using Xunit;

namespace Jianghu.Core.Tests.Cultivation
{
    public class EffectOpModuleTests
    {
        [Fact]
        public void LegacyFourArgCtor_StillCompiles_DefaultsCorrect()
        {
            var op = new EffectOp(EffectOpKind.AddPenInteger, null, 40, "note");
            Assert.Equal(0, op.Amount2);
            Assert.Equal(EffectTrigger.OnUse, op.Trigger);
            Assert.Equal(EffectRarity.Common, op.Rarity);
        }

        [Fact]
        public void ExtendedCtor_SetsModuleFields()
        {
            var op = new EffectOp(EffectOpKind.AddPenInteger, "qixie", 4, "燃血×4",
                Amount2: 1, Trigger: EffectTrigger.OnUse, Rarity: EffectRarity.Rare);
            Assert.Equal(1, op.Amount2);
            Assert.Equal(EffectRarity.Rare, op.Rarity);
        }

        [Fact]
        public void RareCombatKinds_Exist()
        {
            var kinds = new[]
            {
                EffectOpKind.PenFromResource, EffectOpKind.AoePerTarget,
                EffectOpKind.CounterMul, EffectOpKind.DrainResource, EffectOpKind.Backlash,
                EffectOpKind.Dot, EffectOpKind.Control, EffectOpKind.ReflectDamage, EffectOpKind.Evade
            };
            foreach (var k in kinds)
                Assert.True(System.Enum.IsDefined(typeof(EffectOpKind), k));
            // SumOfSet 本轮撤(§15.1 复验补丁): 断言不存在
            Assert.DoesNotContain("SumOfSet", System.Enum.GetNames(typeof(EffectOpKind)));
        }
    }
}
