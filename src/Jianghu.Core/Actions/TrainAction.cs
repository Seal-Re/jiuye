using System.Collections.Generic;
using Jianghu.Config;
using Jianghu.Decide;
using Jianghu.Events;
using Jianghu.Model;
using Jianghu.Stats;

namespace Jianghu.Actions
{
    public sealed class TrainAction : IAction
    {
        private readonly LimitsConfig _c;
        public TrainAction(LimitsConfig c) { _c = c; }
        public ActionType Type => ActionType.Train;
        public ValidationResult CanExecute(IWorldView w, Character a, DecisionContext ctx) => ValidationResult.Valid;

        public IReadOnlyList<DomainEvent> Apply(IWorldMutator w, Character a, ActionChoice choice)
        {
            var c = (TrainChoice)choice;
            int gain = _c.TrainGainMin + (a.Stats.Get(StatKind.Insight) * (_c.TrainGainMax - _c.TrainGainMin)) / 30;
            if (gain < _c.TrainGainMin) gain = _c.TrainGainMin;
            int before = a.Stats.Get(c.Stat);
            w.ApplyStat(a, c.Stat, gain);
            int delta = a.Stats.Get(c.Stat) - before;
            return new DomainEvent[] { new CharacterTrained(w.Clock, a.Id, c.Stat, delta) };
        }
    }
}
