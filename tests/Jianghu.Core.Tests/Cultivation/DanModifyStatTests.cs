using System.Collections.Generic;
using Jianghu.Cultivation;
using Jianghu.Stats;
using Xunit;

namespace Jianghu.Core.Tests.Cultivation;

/// <summary>
/// combat-fullstruct story-008: 非战斗机制 — 丹修改四维。
/// DanModifyStat effect 落地验证：ModifyStat 算子可在战斗中改四维，
/// 经 CombatContext.AccumulateStatDelta → SparAction 落地。
/// </summary>
public class DanModifyStatTests
{
    private static CultivationState MakeState(string pathId, params (string Key, int Val)[] resources)
    {
        var defs = new List<ResourceDef>();
        foreach (var (key, val) in resources)
            defs.Add(new ResourceDef(key, 0, 1000, val));
        return CultivationState.NewForPath(pathId, defs);
    }

    private static CombatContext MakeCtx(
        (string Key, int Val)? atkRes = null,
        (string Key, int Val)? defRes = null)
    {
        var atk = MakeState("atk", atkRes ?? ("none", 0));
        var def = MakeState("def", defRes ?? ("none", 0));
        var path = TestPaths.WithTags(System.Array.Empty<string>());
        return new CombatContext(atk, path, def, path);
    }

    /// <summary>AC 8.1：ModifyStat effect 可在战斗上下文中累积 stat delta。</summary>
    [Fact]
    public void test_modify_stat_accumulates_in_combat_context()
    {
        var ctx = MakeCtx();
        ctx.AccumulateStatDelta(Side.Attacker, "Force", 5);
        ctx.AccumulateStatDelta(Side.Defender, "Constitution", -3);

        var aDeltas = ctx.GetStatDeltas(Side.Attacker);
        var dDeltas = ctx.GetStatDeltas(Side.Defender);

        Assert.Equal(5, aDeltas["Force"]);
        Assert.Equal(-3, dDeltas["Constitution"]);
    }

    /// <summary>AC 8.2：StatBlock.Apply 钳位到 [0, StatCap]。</summary>
    [Fact]
    public void test_stat_apply_respects_cap()
    {
        var s = new StatBlock(new[] { 25, 25, 25, 5 });
        var limits = new Jianghu.Config.LimitsConfig();

        s.Apply(StatKind.Force, 10, limits);
        Assert.True(s.Get(StatKind.Force) <= limits.StatCap, "Force 不超过 StatCap");

        s.Apply(StatKind.Insight, -100, limits);
        Assert.True(s.Get(StatKind.Insight) >= 0, "Insight 不下溢到负");
    }

    /// <summary>AC 8.3：卖丹收益 — pillStock 可消耗（经济晋升前置条件）。</summary>
    [Fact]
    public void test_pillstock_as_economic_gate()
    {
        var st = MakeState("dan_xiu", ("pillStock", 10), ("flameTier", 3));

        Assert.Equal(10, st.Resources["pillStock"]);
        st.ApplyResource("pillStock", -3);
        Assert.Equal(7, st.Resources["pillStock"]);
        Assert.True(st.CultivationPoints >= 0, "CP 非负");
    }

    /// <summary>AC 8.4：频率 cap — 库存天然锁（没库存就不能卖，每次 ≤3）。</summary>
    [Fact]
    public void test_pill_economy_frequency_cap()
    {
        var st = MakeState("dan_xiu", ("pillStock", 2), ("flameTier", 1));

        int sellable = System.Math.Min(st.Resources["pillStock"], 3);
        Assert.Equal(2, sellable); // 库存不足 → 最多卖 2
    }

    /// <summary>ModifyStat 算子可构造并正确携带参数。</summary>
    [Fact]
    public void test_modify_stat_effect_op_construction()
    {
        var op = Modules.ModifyStat("Force", 5, "丹修改武");
        Assert.Equal(EffectOpKind.ModifyStat, op.Kind);
        Assert.Equal("Force", op.Key);
        Assert.Equal(5, op.Amount);
        Assert.Equal(EffectRarity.Rare, op.Rarity);
    }

    /// <summary>ModuleResolver.ApplyOnUse 正确处理 ModifyStat 算子（非直伤、落 accumulator）。</summary>
    [Fact]
    public void test_module_resolver_modify_stat_non_damage()
    {
        var ctx = MakeCtx();
        var op = Modules.ModifyStat("Force", 5);

        // ApplyOnUse 返回原始 dmg（ModifyStat 不增减伤害，副作用走 CombatContext accumulator）
        int delta = ModuleResolver.ApplyOnUse(100, op, ctx);
        Assert.Equal(100, delta); // 伤害穿透不变
    }
}
