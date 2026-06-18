using System.Collections.Generic;
using Jianghu.Config;
using Jianghu.Cultivation;
using Jianghu.Decide;
using Jianghu.Events;
using Jianghu.Model;
using Jianghu.Stats;

namespace Jianghu.Actions
{
    public sealed class SparAction : IAction
    {
        // off（registry==null）：构造无参 → 纯走旧公式（v1.0 逐字节，不碰 cultivation/resolver）。
        // on：注入 limits + 路线注册表，按 actor.Cultivation 分流到 PowerEngine × 软情境 adj。
        private readonly LimitsConfig? _limits;
        private readonly PathRegistry? _registry;
        private readonly SituationalResolver? _resolver;

        public SparAction() { } // off 默认（v1.0 caller）：registry/resolver=null。

        public SparAction(LimitsConfig limits, PathRegistry? registry)
        {
            _limits = limits;
            _registry = registry;
            // on：软情境结算器经全局零-PathId 边表构造（off=null 时不构造，不参与结算）。
            _resolver = registry != null ? new SituationalResolver(SituationalEdges.Default) : null;
        }

        public ActionType Type => ActionType.Spar;
        public ValidationResult CanExecute(IWorldView w, Character a, DecisionContext ctx) => ValidationResult.Valid;

        // v1.0 旧公式（off 逐字节）：Force×2+Internal+Constitution。无 Cultivation/registry 时唯一路径。
        private static int Power(Character c) =>
            c.Stats.Get(StatKind.Force) * 2 + c.Stats.Get(StatKind.Internal) + c.Stats.Get(StatKind.Constitution);

        public IReadOnlyList<DomainEvent> Apply(IWorldMutator w, Character a, ActionChoice choice)
        {
            var c = (SparChoice)choice;
            Character? target = null;
            foreach (var x in w.AtNode(a.Node)) if (x.Id.Value == c.Target.Value) { target = x; break; }
            if (target == null) return new DomainEvent[0];

            // ON 分支：双方均有 Cultivation + registry → DuelEngine.ResolveR2（模块战斗）
            if (a.Cultivation != null && target.Cultivation != null && _registry != null && _limits != null)
            {
                var aPath = _registry.ById(a.Cultivation.PathId);
                var tPath = _registry.ById(target.Cultivation.PathId);
                var result = DuelEngine.ResolveR2(a, target, aPath, tPath, _registry, _limits, _resolver,
                    attackerSkill: null, defenderSkill: null); // 自动选招

                var winner = result.Winner == a.Id ? a : target;
                var loser = result.Winner == a.Id ? target : a;
                int margin = result.WasAutoWin ? 999 : result.Margin;

                // Apply stat deltas (ModifyStat 算子累积的跨路 stat 修改)
                ApplyStatDeltas(w, a, result.AttackerStatDeltas);
                ApplyStatDeltas(w, target, result.DefenderStatDeltas);

                int wToL = w.AdjustRelation(winner.Id, loser.Id, +3);
                int lToW = w.AdjustRelation(loser.Id, winner.Id, margin > 20 ? -4 : +2);
                return new DomainEvent[]
                {
                    new DuelResolved(w.Clock, winner.Id, loser.Id, margin),
                    new RelationChanged(w.Clock, winner.Id, loser.Id, +3, wToL),
                    new RelationChanged(w.Clock, loser.Id, winner.Id, margin > 20 ? -4 : +2, lToW),
                };
            }

            // OFF 分支：legacy 公式（逐字节，不碰 cultivation/resolver）
            int pa = EffectivePower(a, target), pb = EffectivePower(target, a);
            var legacyWinner = pa >= pb ? a : target;
            var legacyLoser = pa >= pb ? target : a;
            int legacyMargin = System.Math.Abs(pa - pb);

            int lwToL = w.AdjustRelation(legacyWinner.Id, legacyLoser.Id, +3);
            int llToW = w.AdjustRelation(legacyLoser.Id, legacyWinner.Id, legacyMargin > 20 ? -4 : +2);
            return new DomainEvent[]
            {
                new DuelResolved(w.Clock, legacyWinner.Id, legacyLoser.Id, legacyMargin),
                new RelationChanged(w.Clock, legacyWinner.Id, legacyLoser.Id, +3, lwToL),
                new RelationChanged(w.Clock, legacyLoser.Id, legacyWinner.Id, legacyMargin > 20 ? -4 : +2, llToW),
            };
        }

        // 分流：self 无 Cultivation 或 registry==null（off）→ 旧公式（逐字节）；
        // 否则 per-path PowerEngine（含境界曲线）× 软情境 adj（clamp ±P0/4）。
        private int EffectivePower(Character self, Character opponent)
        {
            if (self.Cultivation == null || _registry == null || _limits == null)
                return Power(self);

            var def = _registry.ById(self.Cultivation.PathId);
            int pe = PowerEngine.Evaluate(self.Cultivation, self.Stats, def, _limits);

            // 软情境上下文：双方 SituationalTags（缺路=空），self 作攻方 axis；env A.0 暂空（Phase 4.5 接）。
            var defTags = OpponentTags(opponent);
            var ctx = new SitContext(def.SituationalTags, defTags, def.AttackDimension, EmptyEnv);
            int adj = _resolver!.AdjPct(ctx, _limits.SituationalP0Base);

            return (int)((long)pe * (100 + adj) / 100);
        }

        // 对手 SituationalTags：经 registry 查其路；无路（散修/off）→ 空 tag。
        private IReadOnlyList<string> OpponentTags(Character opponent)
        {
            if (opponent.Cultivation == null || _registry == null)
                return System.Array.Empty<string>();
            return _registry.ById(opponent.Cultivation.PathId).SituationalTags;
        }

        private static readonly IReadOnlyDictionary<string, string> EmptyEnv =
            new Dictionary<string, string>();

        /// <summary>将 DuelEngine 累积的 stat delta 经 IWorldMutator.ApplyStat 落地。</summary>
        private static void ApplyStatDeltas(IWorldMutator w, Character c, IReadOnlyDictionary<string, int>? deltas)
        {
            if (deltas == null) return;
            foreach (var kv in deltas)
            {
                var kind = kv.Key switch
                {
                    "Force" => StatKind.Force,
                    "Internal" => StatKind.Internal,
                    "Constitution" => StatKind.Constitution,
                    "Insight" => StatKind.Insight,
                    _ => (StatKind)(-1)
                };
                if ((int)kind >= 0)
                    w.ApplyStat(c, kind, kv.Value);
            }
        }
    }
}
