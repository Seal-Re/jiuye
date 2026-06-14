using System.Collections.Generic;
using Jianghu.Config;
using Jianghu.Cultivation;
using Jianghu.Events;
using Jianghu.Model;

namespace Jianghu.Actions
{
    /// <summary>按 ActionType 分发到 IAction.Apply。属性只经 IWorldMutator 改（统一钳制）。</summary>
    public sealed class ActionSystem
    {
        private readonly Dictionary<ActionType, IAction> _actions;

        // registry==null（off / v1.0 caller）→ SparAction 走旧公式（逐字节）；
        // on 时 World 注入注册表 → SparAction 按 Cultivation 分流到 PowerEngine × 软情境。
        public ActionSystem(LimitsConfig c, PathRegistry? registry = null)
        {
            _actions = new Dictionary<ActionType, IAction>
            {
                { ActionType.Train, new TrainAction(c) },
                { ActionType.Travel, new TravelAction() },
                { ActionType.Spar, registry == null ? new SparAction() : new SparAction(c, registry) },
            };
        }
        public IReadOnlyList<ActionType> Types => new List<ActionType>(_actions.Keys);

        public IReadOnlyList<DomainEvent> Execute(IWorldMutator w, Character actor, ActionChoice choice)
            => _actions[choice.Type].Apply(w, actor, choice);
    }
}
