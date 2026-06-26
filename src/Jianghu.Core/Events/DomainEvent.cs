using Jianghu.Drama;
using Jianghu.Model;
using Jianghu.Stats;

namespace Jianghu.Events
{
    public abstract record DomainEvent(long Tick);
    public sealed record CharacterBorn(long Tick, CharacterId Id) : DomainEvent(Tick);
    public sealed record CharacterTrained(long Tick, CharacterId Id, StatKind Stat, int Delta) : DomainEvent(Tick);
    public sealed record CharacterTraveled(long Tick, CharacterId Id, NodeId From, NodeId To) : DomainEvent(Tick);
    public sealed record DuelResolved(long Tick, CharacterId Winner, CharacterId Loser, int Margin) : DomainEvent(Tick);
    public sealed record RelationChanged(long Tick, CharacterId From, CharacterId To, int Delta, int NewValue) : DomainEvent(Tick);
    public sealed record CharacterDied(long Tick, CharacterId Id, long Age) : DomainEvent(Tick);
    public sealed record PathEntered(long Tick, CharacterId Id, string PathId) : DomainEvent(Tick);
    public sealed record RealmBreakthrough(long Tick, CharacterId Id, int NewRealmIndex) : DomainEvent(Tick);
    public sealed record DaoHeartChanged(long Tick, CharacterId Id, int OldValue, int NewValue, string Source) : DomainEvent(Tick);
    public sealed record InnerDemonChanged(long Tick, CharacterId Id, int OldValue, int NewValue, string Source) : DomainEvent(Tick);
    public sealed record FactionPromoted(long Tick, CharacterId Id, int FactionId, int NewRank) : DomainEvent(Tick);
    public sealed record TerritoryLost(long Tick, long Site, int FromFaction, int ToFaction) : DomainEvent(Tick);

    // 戏剧引擎 B 事件（drama-008，spec Step 6）。由戏剧层（drama-009 Pump/drama-010 Advance）产出；
    // off/空库根本不构造 drama 子系统 → 这些事件永不进 Chronicle.Append（off 逐字节守恒 B.3）。
    // 字段全整数/枚举/Id（B.2 无浮点）；引用 Jianghu.Drama 值类型（同程序集跨命名空间，无循环）。
    public sealed record GrudgeFormed(long Tick, GrudgeId Grudge, CharacterId Holder, CharacterId Target, GrudgeKind Kind, int Intensity) : DomainEvent(Tick);
    public sealed record GrudgeInherited(long Tick, GrudgeId Grudge, CharacterId Heir, CharacterId Target, GrudgeId FromGrudge, int Generation, int Intensity) : DomainEvent(Tick);
    public sealed record ArcIgnited(long Tick, ArcId Arc, CharacterId Avenger, CharacterId Target) : DomainEvent(Tick);
    public sealed record ArcStageEntered(long Tick, ArcId Arc, ArcStage Stage) : DomainEvent(Tick);
    public sealed record RevengeConsummated(long Tick, ArcId Arc, CharacterId Avenger, CharacterId Target, bool AvengerPrevailed) : DomainEvent(Tick);
    public sealed record ArcAbandoned(long Tick, ArcId Arc, string Reason) : DomainEvent(Tick);
}
