using System.Linq;
using System.Collections.Generic;
using Jianghu.Cultivation;
using Xunit;

namespace Jianghu.Core.Tests.Cultivation
{
    /// <summary>
    /// Task 4.5（剑修所涉边）：SituationalEdges.Default 含剑修近战 melee 相关边（distance 轴放风筝，
    /// 照 Bible §6.4「放风筝远程 +5/近战被风筝 −5」）。零 PathId（只认 tag/axis），系数照 Bible。
    /// </summary>
    public class SituationalEdgesDataTests
    {
        static SitContext Ctx(string[] atk, string[] def, string axis = "physical")
            => new SitContext(atk, def, axis, new Dictionary<string, string>());

        [Fact]
        public void Default_HasMeleeKitedEdge_ZeroPathId()
        {
            var r = new SituationalResolver(SituationalEdges.Default);
            // 远程攻近战剑修 → 放风筝增益攻方（剑修脆皮被放风筝吃亏=远程 +adj）。
            int adj = r.AdjPct(Ctx(new[] { "ranged" }, new[] { "melee" }), p0: 400);
            Assert.True(adj > 0, "缺 distance 轴放风筝边（远程攻近战 → +adj）");
        }

        [Fact]
        public void Default_AllEdges_AreZeroPathId()
        {
            // 红线：边谓词绝不含任何已知 21 路 pathId（只认 tag/axis/env）。
            foreach (var e in SituationalEdges.Default)
                foreach (var pid in PathValidator.KnownPathIds)
                    Assert.DoesNotContain(pid, e.WhenPred);
        }
    }
}
