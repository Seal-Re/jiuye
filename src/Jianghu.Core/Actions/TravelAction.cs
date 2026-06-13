using System.Collections.Generic;
using Jianghu.Decide;
using Jianghu.Events;
using Jianghu.Model;

namespace Jianghu.Actions
{
    public sealed class TravelAction : IAction
    {
        public ActionType Type => ActionType.Travel;
        public ValidationResult CanExecute(IWorldView w, Character a, DecisionContext ctx) => ValidationResult.Valid;

        public IReadOnlyList<DomainEvent> Apply(IWorldMutator w, Character a, ActionChoice choice)
        {
            var c = (TravelChoice)choice;
            var from = a.Node;
            if (c.To.Value < 0 || c.To.Value >= w.NodeCount) return new DomainEvent[0];
            w.Move(a, c.To);
            return new DomainEvent[] { new CharacterTraveled(w.Clock, a.Id, from, c.To) };
        }
    }
}
