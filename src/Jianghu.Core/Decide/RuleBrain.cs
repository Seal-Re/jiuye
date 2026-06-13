using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jianghu.Actions;
using Jianghu.Model;
using Jianghu.Random;
using Jianghu.Stats;

namespace Jianghu.Decide
{
    /// <summary>目标驱动效用机（§7.3）。纯整数效用（§4.1：禁浮点确定性决策路径，保证跨运行时一致）。</summary>
    public sealed class RuleBrain : IBrain
    {
        private readonly IRandom _rng;
        private readonly ArchetypeKind _arch;
        private ActionType _last;
        private int _streak;

        public RuleBrain(IRandom rng, ArchetypeKind arch) { _rng = rng; _arch = arch; }

        public ValueTask<ActionChoice> DecideAsync(DecisionContext ctx, CancellationToken ct)
        {
            long bestU = long.MinValue;
            ActionChoice bestChoice = new TrainChoice(StatKind.Force);
            ActionType bestType = ActionType.Train;
            foreach (var act in ctx.Allowed)
            {
                var (u, choice) = Evaluate(act, ctx);
                if (u == long.MinValue) continue;            // 非法（如无邻可切磋）
                if (act == _last) u -= (long)_streak * 400;   // 重复衰减（线性整数）
                u += _rng.NextInt(50);                        // 确定性打破平手
                if (u > bestU) { bestU = u; bestChoice = choice; bestType = act; }
            }
            _streak = bestType == _last ? _streak + 1 : 0;
            _last = bestType;
            return new ValueTask<ActionChoice>(bestChoice);
        }

        private (long, ActionChoice) Evaluate(ActionType act, DecisionContext ctx)
        {
            int archMartial = _arch == ArchetypeKind.Martial ? 1000 : 500;
            switch (act)
            {
                case ActionType.Train:
                {
                    var stat = WeakestCombat(ctx.Stats);
                    long goalW = ctx.Goal.Kind == GoalKind.Advance ? 1500 : 600;
                    long headroom = (30 - ctx.Stats.Get(stat)) * 30; // 0..750
                    return (goalW + archMartial + headroom, new TrainChoice(stat));
                }
                case ActionType.Travel:
                {
                    long goalW = ctx.Goal.Kind == GoalKind.Wander ? 1400 : 700;
                    long lonely = ctx.Nearby.Count == 0 ? 800 : 0;
                    int to = ctx.Node.Value + 1;
                    return (goalW + lonely, new TravelChoice(new NodeId(to)));
                }
                case ActionType.Spar:
                {
                    if (ctx.Nearby.Count == 0) return (long.MinValue, new SparChoice(new CharacterId(-1)));
                    var t = BestSparTarget(ctx);
                    int gap = System.Math.Abs(SelfPower(ctx.Stats) - t.Power);
                    long rival = System.Math.Max(0, 600 - 15L * gap);  // 势均力敌更想切磋
                    long notFoe = t.Affinity > -50 ? 0 : -500;
                    return (archMartial - 300 + rival + notFoe, new SparChoice(t.Id));
                }
                default: return (long.MinValue, new TrainChoice(StatKind.Force));
            }
        }

        private static int SelfPower(StatBlock s) =>
            s.Get(StatKind.Force) * 2 + s.Get(StatKind.Internal) + s.Get(StatKind.Constitution);

        private static StatKind WeakestCombat(StatBlock s)
        {
            StatKind w = StatKind.Force; int min = int.MaxValue;
            foreach (var k in new[] { StatKind.Force, StatKind.Internal, StatKind.Constitution })
                if (s.Get(k) < min) { min = s.Get(k); w = k; }
            return w;
        }

        private static NearbyActor BestSparTarget(DecisionContext ctx)
        {
            NearbyActor best = ctx.Nearby[0]; int bestGap = int.MaxValue; int self = SelfPower(ctx.Stats);
            foreach (var n in ctx.Nearby)
            {
                int gap = System.Math.Abs(self - n.Power);
                if (gap < bestGap) { bestGap = gap; best = n; }
            }
            return best;
        }

        public RuleBrain Clone()
        {
            var p = new Pcg32(0, 1); p.SetState(_rng.GetState());
            var b = new RuleBrain(p, _arch);
            b._last = _last; b._streak = _streak;
            return b;
        }
    }
}
