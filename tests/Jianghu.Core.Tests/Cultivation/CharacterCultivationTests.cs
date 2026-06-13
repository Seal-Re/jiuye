using Jianghu.Cultivation;
using Jianghu.Model;
using Jianghu.Stats;
using Xunit;

namespace Jianghu.Core.Tests.Cultivation
{
    /// <summary>
    /// Task 2.3：Character 纯加可空 Cultivation（默认 null）；Clone 深拷 Cultivation 独立。
    /// off 角色 Cultivation==null → 逐字节不变。
    /// </summary>
    public class CharacterCultivationTests
    {
        static Character NewCharacter() => new Character(
            new CharacterId(1),
            new Persona("无名", "客", "市井", ArchetypeKind.Martial, null),
            new StatBlock(new[] { 20, 20, 20, 20 }),
            new NodeId(0), new Goal(GoalKind.Advance, 0),
            age: 0, lifespan: 800, memoryCap: 16);

        [Fact]
        public void Cultivation_DefaultsToNull()
        {
            var ch = NewCharacter();
            Assert.Null(ch.Cultivation);
        }

        [Fact]
        public void Clone_OffCharacter_CultivationStaysNull()
        {
            var ch = NewCharacter();
            var clone = ch.Clone();
            Assert.Null(clone.Cultivation);
        }

        [Fact]
        public void Clone_DeepCopiesCultivation_Independent()
        {
            var ch = NewCharacter();
            ch.Cultivation = CultivationState.NewForPath(
                "sword_immortal", new[] { new ResourceDef("swordWill", 0, 20, 5) });

            var clone = ch.Clone();
            Assert.NotNull(clone.Cultivation);
            // 深拷独立：改原件资源不影响克隆。
            ch.Cultivation!.ApplyResource("swordWill", +10);
            Assert.NotEqual(
                ch.Cultivation!.Resources["swordWill"],
                clone.Cultivation!.Resources["swordWill"]);
        }
    }
}
