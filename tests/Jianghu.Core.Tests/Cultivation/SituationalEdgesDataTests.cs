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

        // 红线：element 轴元素环自洽——对每条 X 克 Y 边，不得存在反向 Y 克 X 边（canon counterWheel 四象环，无双向）。
        [Fact]
        public void Default_ElementWheel_IsAcyclicallyConsistent_NoReverseEdge()
        {
            // 提取每条 element 边的 (attacker tag, defender tag) 有向对。
            var pairs = SituationalEdges.Default
                .Where(e => e.Axis == "element")
                .Select(e => (Atk: TagOf(e.WhenPred, "attacker.tag:"), Def: TagOf(e.WhenPred, "defender.tag:")))
                .ToList();

            Assert.NotEmpty(pairs); // canon 四象环至少 4 条
            foreach (var (atk, def) in pairs)
            {
                bool hasReverse = pairs.Any(p => p.Atk == def && p.Def == atk);
                Assert.False(hasReverse, $"element 环含反向边: {atk}克{def} 与 {def}克{atk} 同存（破环自洽）");
            }
        }

        [Fact]
        public void AntiEvil_EdgesExist_RighteousThunderAntiEvil_VsEvil()
        {
            var r = new SituationalResolver(SituationalEdges.Default);
            int a1 = r.AdjPct(Ctx(new[] { "anti_evil" }, new[] { "evil" }), p0: 400);
            int a2 = r.AdjPct(Ctx(new[] { "righteous" }, new[] { "evil" }), p0: 400);
            int a3 = r.AdjPct(Ctx(new[] { "thunder" }, new[] { "evil" }), p0: 400);
            Assert.True(a1 > 0, "anti_evil vs evil should gain adj");
            Assert.True(a2 > 0, "righteous vs evil should gain adj");
            Assert.True(a3 > 0, "thunder vs evil should gain adj");
        }

        [Fact]
        public void AntiEvil_ReversePenalty_EvilVsRighteousLosesAdj()
        {
            var r = new SituationalResolver(SituationalEdges.Default);
            int adj = r.AdjPct(Ctx(new[] { "evil" }, new[] { "righteous" }), p0: 400);
            Assert.True(adj < 0, $"evil vs righteous should lose adj, got {adj}");
        }

        [Fact]
        public void Economic_EdgesExist_ArtifactVsEconomic_AndReverse()
        {
            var r = new SituationalResolver(SituationalEdges.Default);
            int a1 = r.AdjPct(Ctx(new[] { "artifact" }, new[] { "economic" }), p0: 400);
            int a2 = r.AdjPct(Ctx(new[] { "economic" }, new[] { "artifact" }), p0: 400);
            Assert.True(a1 > 0, "artifact vs economic should gain");
            Assert.True(a2 > 0, "economic vs artifact should gain (双向克)");
        }

        [Fact]
        public void Control_EdgesExist_VsBrute_AndHighBurst()
        {
            var r = new SituationalResolver(SituationalEdges.Default);
            int a1 = r.AdjPct(Ctx(new[] { "control" }, new[] { "brute" }), p0: 400);
            int a2 = r.AdjPct(Ctx(new[] { "control" }, new[] { "high_burst" }), p0: 400);
            Assert.True(a1 > 0, "control vs brute should gain");
            Assert.True(a2 > 0, "control vs high_burst should gain");
        }

        [Fact]
        public void EdgeCount_IsAtLeast20()
        {
            Assert.True(SituationalEdges.Default.Count >= 20,
                $"Expected >=20 edges, got {SituationalEdges.Default.Count}");
        }

        [Fact]
        public void AllEdges_NoSelfCounter()
        {
            // No counter edge should have attacker and defender with same tag pair reversed
            var edges = SituationalEdges.Default;
            for (int i = 0; i < edges.Count; i++)
            {
                var ea = edges[i];
                var atkTags = TagsOf(ea.WhenPred, "attacker.tag:");
                var defTags = TagsOf(ea.WhenPred, "defender.tag:");
                for (int j = i + 1; j < edges.Count; j++)
                {
                    var eb = edges[j];
                    var atkB = TagsOf(eb.WhenPred, "attacker.tag:");
                    var defB = TagsOf(eb.WhenPred, "defender.tag:");
                    // Allow mutual counter only if CoefPct signs differ (e.g. righteous克evil +15 vs evil攻righteous -10)
                    // 同号+同轴+反向tag = 真实互克(设计错误)
                    if (ea.Axis == eb.Axis)
                    {
                        bool reversed = atkTags.SetEquals(defB) && defTags.SetEquals(atkB);
                        bool sameSign = (ea.CoefPct > 0) == (eb.CoefPct > 0);
                        if (reversed && sameSign && ea.Axis != "economic")
                            Assert.False(true,
                                $"Same-sign mutual counter on axis '{ea.Axis}': ({string.Join(",", atkTags)})↔({string.Join(",", defTags)}) coefs {ea.CoefPct}/{eb.CoefPct}");
                    }
                }
            }
        }

        static HashSet<string> TagsOf(string pred, string prefix)
        {
            var tags = new HashSet<string>();
            foreach (var raw in pred.Split('&'))
            {
                var atom = raw.Trim();
                if (atom.StartsWith(prefix, System.StringComparison.Ordinal))
                    tags.Add(atom.Substring(prefix.Length));
            }
            return tags;
        }

        // 从 WhenPred（'&' 连接的 attacker.tag:X & defender.tag:Y）取 prefix 后的 tag 名。
        static string TagOf(string pred, string prefix)
        {
            foreach (var raw in pred.Split('&'))
            {
                var atom = raw.Trim();
                if (atom.StartsWith(prefix, System.StringComparison.Ordinal))
                    return atom.Substring(prefix.Length);
            }
            Assert.Fail($"element 边谓词缺 '{prefix}' 原子: {pred}");
            return string.Empty; // 不可达（Assert.Fail 抛）
        }
    }
}
