namespace Jianghu.Random
{
    /// <summary>RNG 子流编号单一真相源（冻结，append-only）。root.Split(id) 派生。</summary>
    public static class RngStreamIds
    {
        public const int Gen = 1, Domain = 2, Spawn = 3, Brain = 4,
                         Cultivation = 5, Drama = 6, Map = 7, Faction = 8,
                         Duel = 9; // combat-variance cv-001（append-only，绝不复用 1-8）：per-duel 方差流，off 不构造→B.3 天然守
    }
}
