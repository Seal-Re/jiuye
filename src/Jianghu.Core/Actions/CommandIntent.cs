using Jianghu.Model;
using Jianghu.Stats;

namespace Jianghu.Actions;

/// <summary>
/// 命令端口（ADR-0004 §①）：玩家/AI 的整数意图。
/// 纯整数字段，无浮点/坐标/指针（B.2 守）。
/// 录制自 RuleBrain.DecideAsync 的输出 → 供 CLI/Godot 重放。
/// JSON 序列化在宿主层（Core 为 netstandard2.1，不依赖 System.Text.Json）。
/// </summary>
public sealed record CommandIntent(
    long Tick,
    long ActorId,
    ActionType Type,
    string? TrainStat,
    int? TravelNodeId,
    long? SparTargetId)
{
    /// <summary>从 ActionChoice 转换。</summary>
    public static CommandIntent FromChoice(long tick, long actorId, ActionChoice choice)
    {
        return choice switch
        {
            TrainChoice t => new CommandIntent(tick, actorId, ActionType.Train,
                t.Stat.ToString(), null, null),
            TravelChoice t => new CommandIntent(tick, actorId, ActionType.Travel,
                null, t.To.Value, null),
            SparChoice s => new CommandIntent(tick, actorId, ActionType.Spar,
                null, null, s.Target.Value),
            _ => new CommandIntent(tick, actorId, ActionType.Train, "Force", null, null)
        };
    }

    /// <summary>还原为 ActionChoice（供重放时替代 RuleBrain）。</summary>
    public ActionChoice ToChoice()
    {
        return Type switch
        {
            ActionType.Train => new TrainChoice(
                TrainStat != null && System.Enum.TryParse<StatKind>(TrainStat, out var sk) ? sk : StatKind.Force),
            ActionType.Travel => new TravelChoice(new NodeId(TravelNodeId ?? 0)),
            ActionType.Spar => new SparChoice(new CharacterId(SparTargetId ?? -1L)),
            _ => new TrainChoice(StatKind.Force)
        };
    }
}
