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

        // 鬼修代表路所涉 spirit 轴绕物防边（魂力 spirit_attack 绕过 body 横练物理罩门 → +adj）。零 PathId。
        [Fact]
        public void Default_HasSpiritPierceBodyEdge_ZeroPathId()
        {
            var r = new SituationalResolver(SituationalEdges.Default);
            int adj = r.AdjPct(Ctx(new[] { "spirit_attack" }, new[] { "body" }), p0: 400);
            Assert.True(adj > 0, "缺 spirit 轴绕物防边（spirit_attack 攻 body → +adj）");
        }

        // 鬼修昼夜边（is_night=1 & 攻方 ghost → +adj 夜强；白昼不命中昼弱）。env 经 SitContext 喂入。
        [Fact]
        public void Default_HasDayNightGhostEdge_NightOnly()
        {
            var r = new SituationalResolver(SituationalEdges.Default);
            var atk = new[] { "spirit_attack", "ghost", "evil" };
            int night = r.AdjPct(new SitContext(atk, System.Array.Empty<string>(), "spirit",
                new Dictionary<string, string> { { "is_night", "1" } }), p0: 400);
            int day = r.AdjPct(new SitContext(atk, System.Array.Empty<string>(), "spirit",
                new Dictionary<string, string> { { "is_night", "0" } }), p0: 400);
            Assert.True(night > 0, "入夜 & ghost 攻方应得昼夜 +adj");
            Assert.Equal(0, day); // 白昼此边不命中（昼弱）
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
