using System.Collections.Generic;
using Jianghu.Cultivation;
using Xunit;

public class EffectOpTests
{
    // —— 装配期被动算子 ——

    [Fact]
    public void AddResource_AppliesViaState()
    {
        var st = CultivationState.NewForPath("p", new[] { new ResourceDef("qi", 0, 100, 0) });
        EffectInterpreter.ApplyPassive(new EffectOp(EffectOpKind.AddResource, "qi", 30, null), st);
        Assert.Equal(30, st.Resources["qi"]);
    }

    [Fact]
    public void AddResource_IsClampedByState()
    {
        var st = CultivationState.NewForPath("p", new[] { new ResourceDef("qi", 0, 20, 0) });
        EffectInterpreter.ApplyPassive(new EffectOp(EffectOpKind.AddResource, "qi", 999, null), st);
        Assert.Equal(20, st.Resources["qi"]);
    }

    [Fact]
    public void AddResourceCap_RaisesCapAndAllowsHigherFill()
    {
        var st = CultivationState.NewForPath("p", new[] { new ResourceDef("qi", 0, 20, 0) });
        EffectInterpreter.ApplyPassive(new EffectOp(EffectOpKind.AddResourceCap, "qi", 30, null), st);
        EffectInterpreter.ApplyPassive(new EffectOp(EffectOpKind.AddResource, "qi", 999, null), st);
        Assert.Equal(50, st.Resources["qi"]); // cap 20→50
    }

    [Fact]
    public void SetFlag_SetsFlagValue()
    {
        var st = CultivationState.NewForPath("p", new[] { new ResourceDef("qi", 0, 100, 0) });
        EffectInterpreter.ApplyPassive(new EffectOp(EffectOpKind.SetFlag, "swordHeart", 1, null), st);
        Assert.Equal(1, st.Flags["swordHeart"]);
    }

    [Fact]
    public void GrantPassive_SetsFlagToOne()
    {
        var st = CultivationState.NewForPath("p", new[] { new ResourceDef("qi", 0, 100, 0) });
        EffectInterpreter.ApplyPassive(new EffectOp(EffectOpKind.GrantPassive, "ironSkin", 0, null), st);
        Assert.Equal(1, st.Flags["ironSkin"]);
    }

    [Fact]
    public void AddTermWeightStep_AccumulatesStep()
    {
        var st = CultivationState.NewForPath("p", new[] { new ResourceDef("qi", 0, 100, 0) });
        EffectInterpreter.ApplyPassive(new EffectOp(EffectOpKind.AddTermWeightStep, "swordWillStep", 2, null), st);
        EffectInterpreter.ApplyPassive(new EffectOp(EffectOpKind.AddTermWeightStep, "swordWillStep", 1, null), st);
        Assert.Equal(3, st.Flags["swordWillStep"]);
    }

    // —— 资源消耗 ——

    [Fact]
    public void Cost_FailsWhenInsufficient()
    {
        var st = CultivationState.NewForPath("p", new[] { new ResourceDef("qi", 0, 100, 10) });
        Assert.False(EffectInterpreter.TryPayCost(new Dictionary<string, int> { { "qi", 20 } }, st));
        Assert.True(EffectInterpreter.TryPayCost(new Dictionary<string, int> { { "qi", 5 } }, st));
        Assert.Equal(5, st.Resources["qi"]);
    }

    [Fact]
    public void Cost_AllOrNothing_AcrossMultipleResources()
    {
        var st = CultivationState.NewForPath("p", new[]
        {
            new ResourceDef("qi", 0, 100, 10),
            new ResourceDef("blood", 0, 100, 3),
        });
        // qi 够但 blood 不够 → 整体拒，qi 不扣。
        Assert.False(EffectInterpreter.TryPayCost(new Dictionary<string, int> { { "qi", 5 }, { "blood", 8 } }, st));
        Assert.Equal(10, st.Resources["qi"]);
        Assert.Equal(3, st.Resources["blood"]);
    }

    // —— 战斗期 OnUse 算子骨架（产整数 delta；完整结算 Phase 3）——

    [Fact]
    public void OnUseDelta_AddPenInteger_IsAmount()
    {
        Assert.Equal(7, EffectInterpreter.ComputeOnUseDelta(new EffectOp(EffectOpKind.AddPenInteger, null, 7, null)));
    }

    [Fact]
    public void OnUseDelta_AddFlatDR_IsAmount()
    {
        Assert.Equal(4, EffectInterpreter.ComputeOnUseDelta(new EffectOp(EffectOpKind.AddFlatDR, null, 4, null)));
    }

    [Fact]
    public void OnUseDelta_AddSituationalAdj_IsAmount()
    {
        Assert.Equal(-15, EffectInterpreter.ComputeOnUseDelta(new EffectOp(EffectOpKind.AddSituationalAdj, null, -15, null)));
    }

    [Fact]
    public void OnUseDelta_ScalarMul_IsAmount()
    {
        Assert.Equal(120, EffectInterpreter.ComputeOnUseDelta(new EffectOp(EffectOpKind.ScalarMul, null, 120, null)));
    }
}
