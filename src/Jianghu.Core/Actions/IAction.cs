using System.Collections.Generic;
using Jianghu.Decide;
using Jianghu.Events;
using Jianghu.Model;

namespace Jianghu.Actions
{
    public interface IAction
    {
        ActionType Type { get; }
        ValidationResult CanExecute(IWorldView world, Character actor, DecisionContext ctx);
        IReadOnlyList<DomainEvent> Apply(IWorldMutator world, Character actor, ActionChoice choice);
    }

    /// <summary>只读世界视图（CanExecute 用）。</summary>
    public interface IWorldView
    {
        long Clock { get; }
        IReadOnlyList<Character> AtNode(NodeId node);
        int NodeCount { get; }
    }

    /// <summary>受控世界变更（Apply 用）：属性只经此改，关系经此调。</summary>
    public interface IWorldMutator : IWorldView
    {
        void ApplyStat(Character c, Jianghu.Stats.StatKind k, int delta);
        int AdjustRelation(CharacterId from, CharacterId to, int delta);
        void Move(Character c, NodeId to);
    }
}
