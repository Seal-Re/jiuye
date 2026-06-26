using System.Linq;
using Jianghu.Config;
using Jianghu.Drama;
using Jianghu.Events;
using Jianghu.Model;
using Jianghu.Sim;
using Xunit;

namespace Jianghu.Core.Tests.Drama
{
    /// <summary>
    /// drama-012 World 端跨代链涌现（验收核心 AC-7）：复仇者寿尽且恩怨未了 → 弟子继承 → 点燃下一弧。
    /// 经 World.RemoveDead → DramaDirector.OnDeath 自动触发。Clone 含 profiles 续跑一致。
    /// </summary>
    public class DramaInheritanceWorldTests
    {
        private static readonly LimitsConfig L = LimitsConfig.Default;

        // —— D12.9 Clone 含 profiles：drama-on World Clone 后继承链续跑一致 ——
        [Fact]
        public void test_world_clone_preserves_inheritance_determinism()
        {
            World Build()
            {
                var w = WorldFactory.CreateInitial(2026, LimitsConfig.Default with { GrowthNeeded = 0 }, 6, dramaOn: true);
                w.Grudges!.Form(new CharacterId(0), new CharacterId(1), GrudgeKind.Slaughter, 95,
                    w.Clock, GrudgeCause.Direct, 0, null, L.GrudgeCap);
                // 注册师徒：角色 2 是角色 0 的弟子（0 寿尽则 2 继承）。
                w.RegisterDramaProfile(new DramaProfile(new CharacterId(2), Master: new CharacterId(0), Bloodline: null));
                return w;
            }
            var a = Build();
            for (int i = 0; i < 150; i++) a.Advance(6);
            var clone = a.Clone();
            for (int i = 0; i < 150; i++) { a.Advance(6); clone.Advance(6); }
            Assert.Equal(
                string.Join("\n", a.Chronicle.Lines),
                string.Join("\n", clone.Chronicle.Lines));
        }

        // —— D12.8 跨代链涌现：长跑出现继承事件（GrudgeInherited）——
        // 完整链依赖弟子比师父长寿的自然时序（随机寿命）→ 跨多种子搜索，证端到端**可**产生跨代链。
        // （单代弧机制 + 继承机制本身已由 DramaInheritanceTests 单测穷尽覆盖；此处证 World 接线端到端连通。）
        [Fact]
        public void test_inheritance_chain_can_emerge_end_to_end()
        {
            bool anySeedShowsInheritance = false;
            for (ulong seed = 1; seed <= 12 && !anySeedShowsInheritance; seed++)
            {
                var limits = LimitsConfig.Default with { GrowthNeeded = 0, LifespanMin = 40, LifespanMax = 90 };
                var w = WorldFactory.CreateInitial(seed, limits, 8, cultivation: true, dramaOn: true);
                // 多条师徒边铺继承候选（不同角色寿命各异 → 增加弟子比师父长寿的机会）。
                w.Grudges!.Form(new CharacterId(0), new CharacterId(1), GrudgeKind.Slaughter, 98,
                    w.Clock, GrudgeCause.Direct, 0, null, limits.GrudgeCap);
                for (int heir = 2; heir < 8; heir++)
                    w.RegisterDramaProfile(new DramaProfile(new CharacterId(heir), Master: new CharacterId(0), Bloodline: null));

                for (int i = 0; i < 600; i++) w.Advance(6);
                if (w.Chronicle.Lines.Any(l => l.Contains("继承"))) anySeedShowsInheritance = true;
            }
            Assert.True(anySeedShowsInheritance, "跨多种子端到端长跑应至少一次触发跨代继承（父债子偿）");
        }

        // —— off 不可达继承（off=_drama null，RemoveDead 不调 OnDeath）——
        [Fact]
        public void test_off_mode_no_inheritance()
        {
            var w = WorldFactory.CreateInitial(7, L, 6); // off
            for (int i = 0; i < 300; i++) w.Advance(6);
            Assert.DoesNotContain(w.Chronicle.Lines, l => l.Contains("继承"));
        }
    }
}
