namespace Jianghu.Model
{
    public enum ArchetypeKind { Martial, Knight } // 武痴(重修炼/切磋) / 游侠(重游历/结识)
    public sealed record Persona(string Name, string Title, string Origin, ArchetypeKind Archetype, int? SectId);
}
