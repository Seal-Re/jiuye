using Jianghu.Stats;

namespace Jianghu.Core.Tests.Cultivation
{
    /// <summary>
    /// 四维 StatBlock 测试构造助手。按 StatKind{Force=0,Internal=1,Constitution=2,Insight=3}
    /// 真实索引序造 <see cref="StatBlock"/>（new StatBlock(int[4])）。
    /// </summary>
    public static class StatBlockTestUtil
    {
        public static StatBlock Of(int force, int intl, int con, int insight)
            => new StatBlock(new[] { force, intl, con, insight });
    }
}
