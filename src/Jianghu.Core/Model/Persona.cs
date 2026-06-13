using System;
using System.Collections.Generic;

namespace Jianghu.Model
{
    public enum ArchetypeKind { Martial, Knight } // 武痴(重修炼/切磋) / 游侠(重游历/结识)
    public sealed record Persona(string Name, string Title, string Origin, ArchetypeKind Archetype, int? SectId)
    {
        /// <summary>灵根/资质/体质/形态 tag（修炼生成期定路用，spec §3/§10）。纯加，默认空 → off 逐字节。</summary>
        public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
    }
}
