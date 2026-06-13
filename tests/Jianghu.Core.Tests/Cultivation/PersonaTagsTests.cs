using System.Linq;
using Jianghu.Model;
using Xunit;

namespace Jianghu.Core.Tests.Cultivation
{
    /// <summary>
    /// Task 2.1：Persona 纯加 Tags（灵根/资质/体质/形态 tag），默认空表。
    /// 既有所有位置构造点不破 → off 逐字节（38 测试不变）。
    /// </summary>
    public class PersonaTagsTests
    {
        [Fact]
        public void Persona_DefaultTags_IsEmpty()
        {
            // 既有位置构造（5 参）不变，Tags 默认空表。
            var p = new Persona("无名", "客", "市井", ArchetypeKind.Martial, null);
            Assert.NotNull(p.Tags);
            Assert.Empty(p.Tags);
        }

        [Fact]
        public void Persona_CanInitTags()
        {
            var p = new Persona("无名", "客", "市井", ArchetypeKind.Martial, 1)
            {
                Tags = new[] { "sword_root", "righteous" },
            };
            Assert.Equal(new[] { "sword_root", "righteous" }, p.Tags.ToArray());
        }
    }
}
