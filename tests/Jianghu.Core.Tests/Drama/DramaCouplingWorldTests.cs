using System.Linq;
using Jianghu.Config;
using Jianghu.Drama;
using Jianghu.Model;
using Jianghu.Sim;
using Xunit;

namespace Jianghu.Core.Tests.Drama
{
    /// <summary>
    /// drama-011 World 端受控耦合涌现：drama-on 预置强恩怨长跑 → 复仇者 BuildUp 期 Goal 翻 Advance、
    /// Hunting 期对仇人 affinity ≤ -RelationMirrorCap、弧收束后 Goal 还原。RuleBrain 零改。
    /// </summary>
    public class DramaCouplingWorldTests
    {
        private static readonly LimitsConfig L = LimitsConfig.Default;

        private static World SeededDramaWorld(ulong seed)
        {
            var w = WorldFactory.CreateInitial(seed, LimitsConfig.Default, 6, dramaOn: true);
            Assert.NotNull(w.Grudges);
            // 角色 0 对角色 1 灭门血仇（经账本 chokepoint）。
            w.Grudges!.Form(new CharacterId(0), new CharacterId(1), GrudgeKind.Slaughter, 95,
                w.Clock, GrudgeCause.Direct, 0, null, L.GrudgeCap);
            return w;
        }

        private static Character? ById(World w, long id)
            => w.AliveCharacters().FirstOrDefault(c => c.Id.Value == id);

        // —— D11.6 复仇者在弧推进中某刻 Goal 翻 Advance（疯修通道）——
        [Fact]
        public void test_avenger_goal_flips_to_advance_during_arc()
        {
            var w = SeededDramaWorld(42);
            bool sawAdvance = false;
            for (int i = 0; i < 200; i++)
            {
                w.Advance(6);
                var avenger = ById(w, 0);
                if (avenger != null && avenger.Goal.Kind == GoalKind.Advance) { sawAdvance = true; break; }
            }
            Assert.True(sawAdvance, "复仇者应在 BuildUp 期被覆写 Goal=Advance（疯修通道）");
        }

        // —— D11.6 复仇者对仇人 affinity 在 Hunting 期降至 ≤ -RelationMirrorCap（notFoe 通道）——
        // off 模式战力受 StatCap 封顶，BuildUp→Hunting 的 GrowthNeeded 门常达不到 → 用 GrowthNeeded=0
        // 让弧可达 Hunting（测的是镜像 Relations 通道本身，非战力增长）。
        [Fact]
        public void test_avenger_relation_to_target_mirrored_negative()
        {
            var limits = LimitsConfig.Default with { GrowthNeeded = 0 };
            var w = WorldFactory.CreateInitial(123, limits, 6, dramaOn: true);
            Assert.NotNull(w.Grudges);
            w.Grudges!.Form(new CharacterId(0), new CharacterId(1), GrudgeKind.Slaughter, 95,
                w.Clock, GrudgeCause.Direct, 0, null, limits.GrudgeCap);
            bool sawMirror = false;
            for (int i = 0; i < 400; i++)
            {
                w.Advance(6);
                if (w.Relations.Affinity(new CharacterId(0), new CharacterId(1)) <= -limits.RelationMirrorCap)
                { sawMirror = true; break; }
            }
            Assert.True(sawMirror, $"复仇者对仇人 affinity 应在 Hunting 期 ≤ -{limits.RelationMirrorCap}（镜像负 Relations）");
        }

        // —— D11.8 drama-on 同种子两跑逐字节（耦合不破确定性）——
        [Fact]
        public void test_coupling_deterministic_same_seed()
        {
            string Run()
            {
                var w = SeededDramaWorld(777);
                for (int i = 0; i < 250; i++) w.Advance(6);
                return string.Join("\n", w.Chronicle.Lines);
            }
            Assert.Equal(Run(), Run());
        }

        // —— D11.8 Clone 续跑逐字节（含耦合 + 原 Goal 表）——
        [Fact]
        public void test_coupling_clone_continuation_byte_identical()
        {
            var w = SeededDramaWorld(99);
            for (int i = 0; i < 120; i++) w.Advance(6);
            var clone = w.Clone();
            for (int i = 0; i < 120; i++) { w.Advance(6); clone.Advance(6); }
            Assert.Equal(
                string.Join("\n", w.Chronicle.Lines),
                string.Join("\n", clone.Chronicle.Lines));
        }
    }
}
