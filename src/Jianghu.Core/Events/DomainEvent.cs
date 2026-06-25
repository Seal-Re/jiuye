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
}
