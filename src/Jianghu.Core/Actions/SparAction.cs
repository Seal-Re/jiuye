using System.Collections.Generic;
using Jianghu.Decide;
using Jianghu.Events;
using Jianghu.Model;
using Jianghu.Stats;

namespace Jianghu.Actions
{
    public sealed class SparAction : IAction
    {
        public ActionType Type => ActionType.Spar;
        public ValidationResult CanExecute(IWorldView w, Character a, DecisionContext ctx) => ValidationResult.Valid;

        private static int Power(Character c) =>
            c.Stats.Get(StatKind.Force) * 2 + c.Stats.Get(StatKind.Internal) + c.Stats.Get(StatKind.Constitution);

        public IReadOnlyList<DomainEvent> Apply(IWorldMutator w, Character a, ActionChoice choice)
        {
            var c = (SparChoice)choice;
            Character? target = null;
            foreach (var x in w.AtNode(a.Node)) if (x.Id.Value == c.Target.Value) { target = x; break; }
            if (target == null) return new DomainEvent[0];

            int pa = Power(a), pb = Power(target);
            var winner = pa >= pb ? a : target;
            var loser = pa >= pb ? target : a;
            int margin = System.Math.Abs(pa - pb);

            int wToL = w.AdjustRelation(winner.Id, loser.Id, +3);
            int lToW = w.AdjustRelation(loser.Id, winner.Id, margin > 20 ? -4 : +2);
            return new DomainEvent[]
            {
                new DuelResolved(w.Clock, winner.Id, loser.Id, margin),
                new RelationChanged(w.Clock, winner.Id, loser.Id, +3, wToL),
                new RelationChanged(w.Clock, loser.Id, winner.Id, margin > 20 ? -4 : +2, lToW),
            };
        }
    }
}
