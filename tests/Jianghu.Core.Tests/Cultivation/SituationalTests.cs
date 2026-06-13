using System;
using System.Collections.Generic;
using Jianghu.Cultivation;
using Xunit;

public class SituationalTests
{
    // 构造 SitContext 的测试 helper（env 默认空）。
    static SitContext SitCtx(
        string[] atkTags, string[] defTags,
        string axis = "physical",
        IReadOnlyDictionary<string, string>? env = null)
        => new SitContext(atkTags, defTags, axis, env ?? new Dictionary<string, string>());

    [Fact]
    public void Edge_FiresOnTagsAndEnv_NoPathId()
    {
        var edges = new[]
        {
            new SituationalEdge("element", "attacker.tag:fire & defender.tag:ice", +15),
            new SituationalEdge("form", "defender.tag:undead_construct & attacker.axis:spirit_attack", -100),
        };
        var r = new SituationalResolver(edges);
        var ctx = SitCtx(atkTags: new[] { "fire" }, defTags: new[] { "ice" });
        Assert.Equal(15, r.AdjPct(ctx, p0: 400)); // clamp ±100 (=400/4) 内
    }

    [Fact]
    public void DeadConstruct_ImmuneToSpirit()
    {
        // 死物免精神：defender.tag:undead_construct & attacker.axis:spirit_attack → -100
        var edges = new[]
        {
            new SituationalEdge("form", "defender.tag:undead_construct & attacker.axis:spirit_attack", -100),
        };
        var r = new SituationalResolver(edges);
        var ctx = SitCtx(
            atkTags: Array.Empty<string>(), defTags: new[] { "undead_construct" },
            axis: "spirit_attack");
        Assert.Equal(-100, r.AdjPct(ctx, p0: 400)); // clamp 下界 -100 (=-400/4)
    }

    [Fact]
    public void Adj_ClampedToQuarterP0()
    {
        var edges = new[] { new SituationalEdge("x", "attacker.tag:a", +999) };
        var r = new SituationalResolver(edges);
        Assert.Equal(100, r.AdjPct(SitCtx(new[] { "a" }, Array.Empty<string>()), p0: 400)); // 400/4=100
    }

    [Fact]
    public void Adj_AccumulatesAcrossHits()
    {
        // 多边命中 → CoefPct 累加（在 clamp 内）。
        var edges = new[]
        {
            new SituationalEdge("element", "attacker.tag:fire", +15),
            new SituationalEdge("form", "attacker.tag:fire", +10),
        };
        var r = new SituationalResolver(edges);
        Assert.Equal(25, r.AdjPct(SitCtx(new[] { "fire" }, Array.Empty<string>()), p0: 400));
    }

    [Fact]
    public void Edge_FiresOnNightEnv()
    {
        // 昼夜 env 命中：is_night & attacker.tag:ghost → +10
        var edges = new[]
        {
            new SituationalEdge("time", "env:is_night=1 & attacker.tag:ghost", +10),
        };
        var r = new SituationalResolver(edges);
        var night = new Dictionary<string, string> { { "is_night", "1" } };
        var day = new Dictionary<string, string> { { "is_night", "0" } };
        Assert.Equal(10, r.AdjPct(SitCtx(new[] { "ghost" }, Array.Empty<string>(), env: night), p0: 400));
        Assert.Equal(0, r.AdjPct(SitCtx(new[] { "ghost" }, Array.Empty<string>(), env: day), p0: 400));
    }

    [Fact]
    public void Edge_FiresOnEnvComparison()
    {
        // env 简单比较：distance>=30 & attacker.tag:melee_brute → -20
        var edges = new[]
        {
            new SituationalEdge("range", "env:distance>=30 & attacker.tag:melee_brute", -20),
        };
        var r = new SituationalResolver(edges);
        var far = new Dictionary<string, string> { { "distance", "50" } };
        var near = new Dictionary<string, string> { { "distance", "10" } };
        Assert.Equal(-20, r.AdjPct(SitCtx(new[] { "melee_brute" }, Array.Empty<string>(), env: far), p0: 400));
        Assert.Equal(0, r.AdjPct(SitCtx(new[] { "melee_brute" }, Array.Empty<string>(), env: near), p0: 400));
    }

    [Fact]
    public void NoEdges_AdjIsZero()
    {
        var r = new SituationalResolver(Array.Empty<SituationalEdge>());
        Assert.Equal(0, r.AdjPct(SitCtx(new[] { "fire" }, new[] { "ice" }), p0: 400));
    }
}
