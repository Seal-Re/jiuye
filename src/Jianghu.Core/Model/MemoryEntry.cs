namespace Jianghu.Model
{
    public readonly record struct MemoryEntry(long Tick, string Kind, CharacterId Subject, CharacterId? Object, int Valence);
}
