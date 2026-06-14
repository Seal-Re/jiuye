using System.Collections.Generic;
using System.Linq;
using Jianghu.Actions;
using Jianghu.Config;
using Jianghu.Cultivation;
using Jianghu.Decide;
using Jianghu.Events;
using Jianghu.Model;
using Jianghu.Stats;
using Xunit;

namespace Jianghu.Core.Tests.Cultivation
{
    /// <summary>
    /// Task 3.2：SparAction.Power 分流。
    /// off（双方 Cultivation==null）→ 旧公式 Force×2+Internal+Constitution 逐字节、不碰 registry/resolver。
    /// on（双方定路）→ PowerEngine.Evaluate × 情境 adj（clamp ±P0/4）；winner=pa>=pb、margin=|pa-pb| 沿用。
    /// </summary>
    public class SparCultivationTests
    {
        private sealed class FakeWorld : IWorldMutator
        {
            public long Clock { get; set; } = 5;
            public List<Character> All { get; } = new List<Character>();
            public Relations Rel { get; } = new Relations();
            public LimitsConfig Limits { get; } = LimitsConfig.Default;
            public int NodeCount => 3;
            public IReadOnlyList<Character> AtNode(NodeId n) => All.Where(c => c.Node.Value == n.Value && c.Alive).ToList();
            public void ApplyStat(Character c, StatKind k, int d) => c.Stats.Apply(k, d, Limits);
            public int AdjustRelation(CharacterId f, CharacterId t, int d) => Rel.Adjust(f, t, d);
            public void Move(Character c, NodeId to) => c.Node = to;
        }

        private static Character Make(long id, int force, int intl, int con, int insight) =>
            new Character(new CharacterId(id),
                new Persona("x", "y", "z", ArchetypeKind.Martial, null),
                new StatBlock(new[] { force, intl, con, insight }),
                new NodeId(0), new Goal(GoalKind.Advance, 0), 0, 800, 16);

        [Fact]
        public void Off_BothNull_UsesLegacyFormula()
        {
            // 双方无 Cultivation → 旧公式 F×2+I+C。a:F25 → 25*2+20+20=90；b:F20 → 20*2+20+20=80；
            // margin=10，winner=a。registry/resolver=null（off 不需要）。
            var w = new FakeWorld();
            var a = Make(1, 25, 20, 20, 20); var b = Make(2, 20, 20, 20, 20);
            w.All.Add(a); w.All.Add(b);
            var spar = new SparAction(w.Limits, null);
            var evs = spar.Apply(w, a, new SparChoice(new CharacterId(2)));
            var duel = evs.OfType<DuelResolved>().Single();
            Assert.Equal(1, duel.Winner.Value);
            Assert.Equal(2, duel.Loser.Value);
            Assert.Equal(10, duel.Margin); // |90-80|
        }

        [Fact]
        public void On_UsesPerPathPlusSituational()
        {
            // mock 双路（用 TestPaths）：attacker fire vs defender ice → 元素相生克 +15% adj。
            // 公式 stat:Force×4 + res:qi×3；realm0 mul=10。
            // a: Force=20,qi=0 → BaseSum=80；pe=80*10/10=80；fire vs ice → adj +15 → eff=80*115/100=92。
            // b: Force=10,qi=0 → BaseSum=40；pe=40；ice vs fire → 无边命中 → adj 0 → eff=40。
            // winner=a，margin=|92-40|=52。
            var fire = TestPaths.ValidFull() with
            {
                PathId = "fa_xiu",
                SituationalTags = new[] { "fire" },
            };
            var ice = TestPaths.ValidFull() with
            {
                PathId = "ti_xiu_hengshi",
                SituationalTags = new[] { "ice" },
            };
            var reg = new PathRegistry(new ListPathSource(new[] { fire, ice }));

            var w = new FakeWorld();
            var a = Make(1, 20, 0, 0, 0);
            var b = Make(2, 10, 0, 0, 0);
            a.Cultivation = CultivationState.NewForPath("fa_xiu", fire.Resources);
            b.Cultivation = CultivationState.NewForPath("ti_xiu_hengshi", ice.Resources);
            w.All.Add(a); w.All.Add(b);

            var spar = new SparAction(w.Limits, reg);
            var evs = spar.Apply(w, a, new SparChoice(new CharacterId(2)));
            var duel = evs.OfType<DuelResolved>().Single();
            Assert.Equal(1, duel.Winner.Value); // a (fire) 胜
            Assert.Equal(2, duel.Loser.Value);
            Assert.Equal(52, duel.Margin);      // |92-40|
        }

        [Fact]
        public void On_SituationalAdj_ClampedToQuarterP0()
        {
            // P0=400 → clamp ±100。即便巨额 adj 也被钳；此处只验 winner/margin 自洽（adj 命中单边边 +15 不触顶，
            // 用对称双 fire-vs-ice 不成立；改测 clamp 由 SituationalTests 守，这里验 on 路 effective 用了 adj 而非裸 pe）。
            var fire = TestPaths.ValidFull() with { PathId = "fa_xiu", SituationalTags = new[] { "fire" } };
            var ice = TestPaths.ValidFull() with { PathId = "ti_xiu_hengshi", SituationalTags = new[] { "ice" } };
            var reg = new PathRegistry(new ListPathSource(new[] { fire, ice }));

            var w = new FakeWorld();
            var a = Make(1, 20, 0, 0, 0);
            var b = Make(2, 20, 0, 0, 0); // 同 Force → 裸 pe 相等（80 each）
            a.Cultivation = CultivationState.NewForPath("fa_xiu", fire.Resources);
            b.Cultivation = CultivationState.NewForPath("ti_xiu_hengshi", ice.Resources);
            w.All.Add(a); w.All.Add(b);

            var spar = new SparAction(w.Limits, reg);
            var evs = spar.Apply(w, a, new SparChoice(new CharacterId(2)));
            var duel = evs.OfType<DuelResolved>().Single();
            // 裸 pe 相等下，fire 攻方 +15% adj 应使 a 胜（情境生效证据）。
            Assert.Equal(1, duel.Winner.Value);
            Assert.Equal(80 * 115 / 100 - 80, duel.Margin); // 92-80=12
        }

        sealed class ListPathSource : IPathSource
        {
            private readonly IReadOnlyList<CultivationPathDef> _paths;
            public ListPathSource(IReadOnlyList<CultivationPathDef> paths) => _paths = paths;
            public IReadOnlyList<CultivationPathDef> Load() => _paths;
        }
    }
}
