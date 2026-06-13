using System.Collections.Generic;
using Jianghu.Config;
using Jianghu.Events;
using Jianghu.Model;

namespace Jianghu.Actions
{
    /// <summary>按 ActionType 分发到 IAction.Apply。属性只经 IWorldMutator 改（统一钳制）。</summary>
    public sealed class ActionSystem
    {
        private readonly Dictionary<ActionType, IAction> _actions;
        public ActionSystem(LimitsConfig c)
        {
            _actions = new Dictionary<ActionType, IAction>
            {
                { ActionType.Train, new TrainAction(c) },
                { ActionType.Travel, new TravelAction() },
                { ActionType.Spar, new SparAction() },
            };
        }
        public IReadOnlyList<ActionType> Types => new List<ActionType>(_actions.Keys);

        public IReadOnlyList<DomainEvent> Execute(IWorldMutator w, Character actor, ActionChoice choice)
            => _actions[choice.Type].Apply(w, actor, choice);
    }
}
