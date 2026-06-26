using System.Collections.Generic;
using Jianghu.Drama;
using Jianghu.Model;
using Xunit;

namespace Jianghu.Core.Tests.Drama
{
    /// <summary>
    /// DramaContext（drama-007，spec §3/§4）：唯一 Resolve(RoleRef,DramaVar)→int 纯映射
    /// + 谓词整数比较 Eval + AllPass 全 AND。经 mock IDramaView 与 World 解耦（可单测，零接线）。
    /// </summary>
    public class DramaContextTests
    {
        // —— 可配 mock 只读 seam ——
        private sealed class FakeView : IDramaView
        {
            public Dictionary<long, int> Powers = new();
            public Dictionary<(long, long), int> Affinities = new();
            public HashSet<long> Alive = new();
            public HashSet<(long, long)> Together = new();

            public int Power(CharacterId who) => Powers.TryGetValue(who.Value, out var p) ? p : 0;
            public int Affinity(CharacterId from, CharacterId to)
                => Affinities.TryGetValue((from.Value, to.Value), out var a) ? a : 0;
            public bool IsAlive(CharacterId who) => Alive.Contains(who.Value);
            public bool SameNode(CharacterId a, CharacterId b)
                => Together.Contains((a.Value, b.Value)) || Together.Contains((b.Value, a.Value));
        }

        private static readonly CharacterId Avenger = new(1);
        private static readonly CharacterId Target = new(2);

        private static ArcInstance Arc(ArcStage stage = ArcStage.BuildUp)
            => new ArcInstance(new ArcId(10), ArcKind.Revenge, Avenger, Target, stage, 0, 0, false);

        // —— D7.4 Resolve 各 DramaVar ——
        [Fact]
        public void test_resolve_power_reads_view_for_role()
        {
            var v = new FakeView { Powers = { [1] = 300, [2] = 150 } };
            var ctx = new DramaContext(v, Arc());
            Assert.Equal(300, ctx.Resolve(RoleRef.Holder, DramaVar.Power)); // Holder=Avenger
            Assert.Equal(300, ctx.Resolve(RoleRef.Self, DramaVar.Power));   // Self=Avenger
            Assert.Equal(150, ctx.Resolve(RoleRef.Target, DramaVar.Power));
        }

        [Fact]
        public void test_resolve_affinity_holder_toward_target()
        {
            var v = new FakeView { Affinities = { [(1, 2)] = -70, [(2, 1)] = 10 } };
            var ctx = new DramaContext(v, Arc());
            Assert.Equal(-70, ctx.Resolve(RoleRef.Holder, DramaVar.Affinity)); // 1→2
            Assert.Equal(10, ctx.Resolve(RoleRef.Target, DramaVar.Affinity));  // 2→1
        }

        [Fact]
        public void test_resolve_grudge_intensity_from_context()
        {
            var v = new FakeView();
            var g = new Grudge(new GrudgeId(1), Avenger, Target, GrudgeKind.Slaughter, 88, 0, 0, GrudgeCause.Direct, null);
            var ctx = new DramaContext(v, Arc(), g);
            Assert.Equal(88, ctx.Resolve(RoleRef.Holder, DramaVar.GrudgeIntensity));
        }

        [Fact]
        public void test_resolve_grudge_intensity_zero_when_no_grudge()
        {
            var ctx = new DramaContext(new FakeView(), Arc()); // grudge=null
            Assert.Equal(0, ctx.Resolve(RoleRef.Holder, DramaVar.GrudgeIntensity));
        }

        [Fact]
        public void test_resolve_samenode_one_or_zero()
        {
            var v = new FakeView();
            var ctxApart = new DramaContext(v, Arc());
            Assert.Equal(0, ctxApart.Resolve(RoleRef.Self, DramaVar.SameNode));
            v.Together.Add((1, 2));
            var ctxTogether = new DramaContext(v, Arc());
            Assert.Equal(1, ctxTogether.Resolve(RoleRef.Self, DramaVar.SameNode));
        }

        [Fact]
        public void test_resolve_target_alive_one_or_zero()
        {
            var v = new FakeView();
            var ctxDead = new DramaContext(v, Arc());
            Assert.Equal(0, ctxDead.Resolve(RoleRef.Self, DramaVar.TargetAlive)); // 2 not in Alive
            v.Alive.Add(2);
            var ctxAlive = new DramaContext(v, Arc());
            Assert.Equal(1, ctxAlive.Resolve(RoleRef.Self, DramaVar.TargetAlive));
        }

        // —— D7.5 Eval 各 CmpOp ——
        [Theory]
        [InlineData(CmpOp.Ge, 300, true)]  // 300 >= 300
        [InlineData(CmpOp.Ge, 301, false)]
        [InlineData(CmpOp.Gt, 299, true)]
        [InlineData(CmpOp.Gt, 300, false)] // 300 > 300 false
        [InlineData(CmpOp.Le, 300, true)]
        [InlineData(CmpOp.Le, 299, false)]
        [InlineData(CmpOp.Lt, 301, true)]
        [InlineData(CmpOp.Lt, 300, false)]
        [InlineData(CmpOp.Eq, 300, true)]
        [InlineData(CmpOp.Eq, 299, false)]
        public void test_eval_cmpop_integer_comparison(CmpOp op, int threshold, bool expected)
        {
            var v = new FakeView { Powers = { [1] = 300 } };
            var ctx = new DramaContext(v, Arc());
            var p = new Predicate(RoleRef.Self, DramaVar.Power, op, threshold);
            Assert.Equal(expected, ctx.Eval(p));
        }

        // —— D7.5 AllPass 全 AND ——
        [Fact]
        public void test_allpass_empty_is_true()
        {
            var ctx = new DramaContext(new FakeView(), Arc());
            Assert.True(ctx.AllPass(new List<Predicate>()));
        }

        [Fact]
        public void test_allpass_all_true_passes()
        {
            var v = new FakeView { Powers = { [1] = 300 }, Alive = { 2 } };
            var ctx = new DramaContext(v, Arc());
            var preds = new List<Predicate>
            {
                new Predicate(RoleRef.Self, DramaVar.Power, CmpOp.Ge, 100),
                new Predicate(RoleRef.Self, DramaVar.TargetAlive, CmpOp.Eq, 1),
            };
            Assert.True(ctx.AllPass(preds));
        }

        [Fact]
        public void test_allpass_one_false_fails()
        {
            var v = new FakeView { Powers = { [1] = 300 } }; // target NOT alive
            var ctx = new DramaContext(v, Arc());
            var preds = new List<Predicate>
            {
                new Predicate(RoleRef.Self, DramaVar.Power, CmpOp.Ge, 100),   // true
                new Predicate(RoleRef.Self, DramaVar.TargetAlive, CmpOp.Eq, 1), // false
            };
            Assert.False(ctx.AllPass(preds));
        }
    }
}
