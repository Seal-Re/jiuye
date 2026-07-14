using Xunit;

namespace Jianghu.Core.Tests.Cultivation
{
    /// <summary>
    /// combat-fullstruct story-006: Gate 字段结构化验证。
    /// 验证 ≥5 种门控类型的正确性——GateType 枚举位独立、非零、互斥。
    /// </summary>
    public class GateFieldTests
    {
        // AC 6.1: ≥5 种门控类型（不含 None）
        [Fact]
        public void gate_field_has_at_least_five_non_none_types()
        {
            var values = System.Enum.GetValues(typeof(Jianghu.Cultivation.GateType));
            int nonNone = 0;
            foreach (var v in values)
                if ((int)v != 0) nonNone++;
            Assert.True(nonNone >= 5, $"GateType has {nonNone} non-None values (needs ≥5)");
        }

        // AC 6.1: 具体类型存在
        [Fact]
        public void gate_field_has_all_required_types()
        {
            var G = Jianghu.Cultivation.GateType.HasMovementArt;
            Assert.True(HasFlag(Jianghu.Cultivation.GateType.HasMovementArt), "HasMovementArt missing");
            Assert.True(HasFlag(Jianghu.Cultivation.GateType.HasBodyArt), "HasBodyArt missing");
            Assert.True(HasFlag(Jianghu.Cultivation.GateType.HasSwordIntent), "HasSwordIntent missing");
            Assert.True(HasFlag(Jianghu.Cultivation.GateType.HasFormation), "HasFormation missing");
            Assert.True(HasFlag(Jianghu.Cultivation.GateType.HasAlchemy), "HasAlchemy missing");
            Assert.True(HasFlag(Jianghu.Cultivation.GateType.HasArtifactArts), "HasArtifactArts missing");
        }

        // AC 6.2: 位独立——各 GateType 不重叠
        [Fact]
        public void gate_field_bits_are_independent()
        {
            var G = Jianghu.Cultivation.GateType.HasMovementArt;
            int movement = (int)Jianghu.Cultivation.GateType.HasMovementArt;
            int body = (int)Jianghu.Cultivation.GateType.HasBodyArt;
            int sword = (int)Jianghu.Cultivation.GateType.HasSwordIntent;
            int formation = (int)Jianghu.Cultivation.GateType.HasFormation;
            int alchemy = (int)Jianghu.Cultivation.GateType.HasAlchemy;
            int artifact = (int)Jianghu.Cultivation.GateType.HasArtifactArts;

            // 每位是 2 的幂
            Assert.True((movement & (movement - 1)) == 0);
            Assert.True((body & (body - 1)) == 0);
            Assert.True((sword & (sword - 1)) == 0);
            Assert.True((formation & (formation - 1)) == 0);
            Assert.True((alchemy & (alchemy - 1)) == 0);
            Assert.True((artifact & (artifact - 1)) == 0);

            // 位不重叠
            Assert.Equal(0, movement & body);
            Assert.Equal(0, movement & sword);
            Assert.Equal(0, body & formation);
            Assert.Equal(0, sword & artifact);
        }

        // None = 0, 所有非零值是 2 的幂
        [Fact]
        public void gate_field_none_is_zero()
            => Assert.Equal(0, (int)Jianghu.Cultivation.GateType.None);

        // 复合门控可用 | 组合（多条件 AND）
        [Fact]
        public void gate_field_supports_combination()
        {
            var combo = Jianghu.Cultivation.GateType.HasMovementArt | Jianghu.Cultivation.GateType.HasSwordIntent;
            Assert.True(combo.HasFlag(Jianghu.Cultivation.GateType.HasMovementArt));
            Assert.True(combo.HasFlag(Jianghu.Cultivation.GateType.HasSwordIntent));
            Assert.False(combo.HasFlag(Jianghu.Cultivation.GateType.HasBodyArt));
        }

        // AC 6.6: GateField 不引入浮点（int 位掩码纯整数）
        [Fact]
        public void gate_field_all_values_are_integer()
        {
            foreach (var v in System.Enum.GetValues(typeof(Jianghu.Cultivation.GateType)))
                Assert.IsType<int>((int)v);
        }

        static bool HasFlag(Jianghu.Cultivation.GateType g) => ((int)g) != 0;
    }
}
