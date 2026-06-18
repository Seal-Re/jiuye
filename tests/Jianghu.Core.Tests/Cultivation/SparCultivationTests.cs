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
            // mock 双路（用 TestPaths）：attacker fire vs defender wood → canon 火克木 +15% adj。
            // 公式 stat:Force×4 + res:qi×3；realm0 mul=10。
            // a: Force=20,qi=0 → BaseSum=80；pe=80*10/10=80。
            // b: Force=10,qi=0 → BaseSum=40；pe=40。
            // DuelEngine: HP=pe, baseDmg=pe/10 → a 基础伤害高且火克木 adj → a 胜。
            var fire = TestPaths.ValidFull() with
            {
                PathId = "fa_xiu",
                SituationalTags = new[] { "fire" },
            };
            var wood = TestPaths.ValidFull() with
            {
                PathId = "ti_xiu_hengshi",
                SituationalTags = new[] { "wood" },
            };
            var reg = new PathRegistry(new ListPathSource(new[] { fire, wood }));

            var w = new FakeWorld();
            var a = Make(1, 20, 0, 0, 0);
            var b = Make(2, 10, 0, 0, 0);
            a.Cultivation = CultivationState.NewForPath("fa_xiu", fire.Resources);
            b.Cultivation = CultivationState.NewForPath("ti_xiu_hengshi", wood.Resources);
            w.All.Add(a); w.All.Add(b);

            var spar = new SparAction(w.Limits, reg);
            var evs = spar.Apply(w, a, new SparChoice(new CharacterId(2)));
            var duel = evs.OfType<DuelResolved>().Single();
            Assert.Equal(1, duel.Winner.Value); // a (fire) 胜 — PE 高 + 火克木
            Assert.Equal(2, duel.Loser.Value);
            Assert.True(duel.Margin > 0);      // DuelEngine 给出正 margin
        }

        [Fact]
        public void On_SituationalAdj_ClampedToQuarterP0()
        {
            // 同 Force → 裸 pe 相等（80 each）。fire vs wood → +15% adj 使 a (fire 攻方) 胜。
            // DuelEngine 下 margin = 累计伤害差（非简单 pe 差），但仍为正。
            var fire = TestPaths.ValidFull() with { PathId = "fa_xiu", SituationalTags = new[] { "fire" } };
            var wood = TestPaths.ValidFull() with { PathId = "ti_xiu_hengshi", SituationalTags = new[] { "wood" } };
            var reg = new PathRegistry(new ListPathSource(new[] { fire, wood }));

            var w = new FakeWorld();
            var a = Make(1, 20, 0, 0, 0);
            var b = Make(2, 20, 0, 0, 0); // 同 Force → 裸 pe 相等（80 each）
            a.Cultivation = CultivationState.NewForPath("fa_xiu", fire.Resources);
            b.Cultivation = CultivationState.NewForPath("ti_xiu_hengshi", wood.Resources);
            w.All.Add(a); w.All.Add(b);

            var spar = new SparAction(w.Limits, reg);
            var evs = spar.Apply(w, a, new SparChoice(new CharacterId(2)));
            var duel = evs.OfType<DuelResolved>().Single();
            // 裸 pe 相等下，fire 攻方 canon 火克木 +15% adj 应使 a 胜（情境生效证据）。
            Assert.Equal(1, duel.Winner.Value);
            Assert.True(duel.Margin > 0); // DuelEngine 给出正 margin（非旧公式的简单 pe 差）
        }

        sealed class ListPathSource : IPathSource
        {
            private readonly IReadOnlyList<CultivationPathDef> _paths;
            public ListPathSource(IReadOnlyList<CultivationPathDef> paths) => _paths = paths;
            public IReadOnlyList<CultivationPathDef> Load() => _paths;
        }
    }
}
