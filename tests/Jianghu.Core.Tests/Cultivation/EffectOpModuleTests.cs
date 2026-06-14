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
    }
}
