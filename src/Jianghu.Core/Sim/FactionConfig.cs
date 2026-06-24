using System;
using System.Collections.Generic;

namespace Jianghu.Sim
{
    /// <summary>门派关系类型。</summary>
    public enum FactionRelationKind { Ally = 80, Friendly = 40, Neutral = 0, Rival = -40, Enemy = -80 }

    /// <summary>门派生成配置——所有硬编码参数集中于此。</summary>
    public sealed record FactionConfig(
        int FactionCountMin = 3, int FactionCountMax = 6,
        int MembersPerFactionMin = 3, int MembersPerFactionMax = 8,
        // Alignment distribution weights (sum to 100)
        int WeightRighteous = 33, int WeightNeutral = 34, int WeightEvil = 33,
        // Names
        IReadOnlyList<string>? FactionNames = null,
        IReadOnlyList<string>? PathKeys = null
    )
    {
        public static readonly FactionConfig Default = new();

        public IReadOnlyList<string> FactionNamesOrDefault => FactionNames ?? new[]
        { "剑宗", "道门", "佛寺", "魔教", "血煞门", "万兽山庄", "丹霞谷", "百蛊门" };

        public void Validate()
        {
            if (FactionCountMin < 1 || FactionCountMin > FactionCountMax)
                throw new ArgumentException("FactionCountMin must be ≤ FactionCountMax");
            if (MembersPerFactionMin < 1 || MembersPerFactionMin > MembersPerFactionMax)
                throw new ArgumentException("MembersPerFactionMin must be ≤ MembersPerFactionMax");
            int tw = WeightRighteous + WeightNeutral + WeightEvil;
            if (tw != 100) throw new ArgumentException($"Alignment weights sum={tw}, must be 100");
        }
    }
}
