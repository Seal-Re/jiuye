namespace Jianghu.Cultivation
{
    /// <summary>
    /// 三轴境界查询结果（A1.5）：单一 flatIndex 同时给 (大境界 major, 小境界 sub, UT 当量, 名)。
    /// 纯派生、确定整数，不进运行期、不改 schema。大境界名取「该 UT 段首小境界名」（A1.3 设计 §3.1），免 MajorRealmNames 列。
    /// </summary>
    public readonly struct RealmInfo
    {
        /// <summary>大境界索引（MajorTier）。</summary>
        public int Major { get; }
        /// <summary>大境界内小境界序（0 起）。</summary>
        public int Sub { get; }
        /// <summary>该大境界的小境界总数（SubLevelCount[Major]）。</summary>
        public int SubCount { get; }
        /// <summary>跨路战力当量刻度 UnifiedTier 0-12。</summary>
        public int UnifiedTier { get; }
        /// <summary>大境界名（段首小境界名）。</summary>
        public string MajorName { get; }
        /// <summary>小境界名（该 flatIndex 的展示名）。</summary>
        public string SubName { get; }

        public RealmInfo(int major, int sub, int subCount, int unifiedTier, string majorName, string subName)
        {
            Major = major; Sub = sub; SubCount = subCount; UnifiedTier = unifiedTier;
            MajorName = majorName; SubName = subName;
        }

        /// <summary>全名：单小境界（或 sub 名 == 大境界名）= 名本身；多小境界 = 「大境界·小境界」。</summary>
        public string FullName => MajorName == SubName ? SubName : MajorName + "·" + SubName;

        /// <summary>渲染串：全名 + UT 当量刻度（跨路可比）。</summary>
        public string Display => FullName + "（UT" + UnifiedTier + "）";
    }

    /// <summary>境界三轴查询（A1.5）。投影 + UT + 名一处取齐，渲染/UI/对拍共用，不耦合 Core 运行期。</summary>
    public static class RealmQuery
    {
        /// <summary>flatIndex → 三轴描述（大/小/UT/名）。大境界名取段首（A1.3 §3.1）。</summary>
        public static RealmInfo Describe(RealmCurveDef curve, int flatIndex)
        {
            var (major, sub) = RealmProjection.Decode(flatIndex, curve.SubLevelCount);
            int majorHead = RealmProjection.Encode(major, 0, curve.SubLevelCount);
            return new RealmInfo(
                major, sub, curve.SubLevelCount[major], curve.UnifiedTierOf[flatIndex],
                curve.RealmNames[majorHead], curve.RealmNames[flatIndex]);
        }
    }
}
