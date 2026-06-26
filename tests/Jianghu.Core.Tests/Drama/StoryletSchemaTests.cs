using System.Collections.Generic;
using Jianghu.Drama;
using Xunit;

namespace Jianghu.Core.Tests.Drama
{
    /// <summary>
    /// storylet 声明式 schema（drama-007，spec §2）：StoryletSpec 纯不可变 record +
    /// IStoryletSource/CodeStoryletSource 内容池（核库只吃接口、零 IO）。CooldownScope 枚举。
    /// </summary>
    public class StoryletSchemaTests
    {
        // 共享空集合实例：record 对引用型成员用引用相等，故同实例才值相等
        // （映射真实用法——storylet 为 static readonly 常量，复用共享/字面集合）。
        private static readonly IReadOnlyList<Predicate> NoPre = System.Array.Empty<Predicate>();
        private static readonly IReadOnlyList<Effect> NoEff = System.Array.Empty<Effect>();

        private static StoryletSpec Spec(int id, int weight = 100) => new StoryletSpec(
            Id: id,
            Arc: ArcKind.Revenge,
            Stage: ArcStage.Hunting,
            BaseWeight: weight,
            OncePerArc: false,
            CooldownTicks: 0,
            Scope: CooldownScope.Global,
            Preconditions: NoPre,
            Effects: NoEff,
            ChronicleTemplate: "x 寻 y 寻仇");

        [Fact]
        public void test_storyletspec_value_equality()
        {
            Assert.Equal(Spec(1), Spec(1));      // 同共享集合 + 同标量 → record 值相等
            Assert.NotEqual(Spec(1), Spec(2));
        }

        [Fact]
        public void test_chronicle_template_is_render_only_field()
        {
            // ChronicleTemplate 仅渲染——纯字段，存在且可读（绝不进数值路径由实现保证）。
            Assert.Equal("x 寻 y 寻仇", Spec(1).ChronicleTemplate);
        }

        [Fact]
        public void test_cooldown_scope_enum_has_four_scopes()
        {
            Assert.Equal(0, (int)CooldownScope.Global);
            Assert.Equal(1, (int)CooldownScope.PerActor);
            Assert.Equal(2, (int)CooldownScope.PerPair);
            Assert.Equal(3, (int)CooldownScope.PerSect);
        }

        [Fact]
        public void test_code_storylet_source_returns_stable_list()
        {
            var src = new CodeStoryletSource(new List<StoryletSpec> { Spec(2), Spec(1), Spec(3) });
            IStoryletSource asInterface = src; // 核库只吃接口
            Assert.Equal(3, asInterface.All.Count);
            // 保持构造序（确定性，不重排）。
            Assert.Equal(2, asInterface.All[0].Id);
            Assert.Equal(1, asInterface.All[1].Id);
            Assert.Equal(3, asInterface.All[2].Id);
        }
    }
}
