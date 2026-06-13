using Jianghu.Model;
using Jianghu.Stats;

namespace Jianghu.Actions
{
    public abstract record ActionChoice(ActionType Type);
    public sealed record TrainChoice(StatKind Stat) : ActionChoice(ActionType.Train);
    public sealed record TravelChoice(NodeId To) : ActionChoice(ActionType.Travel);
    public sealed record SparChoice(CharacterId Target) : ActionChoice(ActionType.Spar);
}
