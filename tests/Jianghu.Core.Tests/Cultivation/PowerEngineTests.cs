using Jianghu.Config;
using Jianghu.Cultivation;
using Jianghu.Core.Tests.Cultivation;
using Xunit;

public class PowerEngineTests
{
    [Fact]
    public void Evaluate_MatchesHandComputed()
    {
        // Force=20,Insight=10; terms Force×4 + Insight×3 = 80+30=110; realm0 mul=10 → 110*10/10=110
        var def = TestPaths.SwordMinimal();
        var stats = StatBlockTestUtil.Of(force: 20, intl: 0, con: 0, insight: 10);
        var st = CultivationState.NewForPath("sword_immortal", def.Resources);
        st.RealmIndex = 0;
        Assert.Equal(110, PowerEngine.Evaluate(st, stats, def, LimitsConfig.Default));
    }

    [Fact]
    public void RealmCurve_Scales()
    {
        var def = TestPaths.SwordMinimal();
        var stats = StatBlockTestUtil.Of(20, 0, 0, 10);
        var st = CultivationState.NewForPath("sword_immortal", def.Resources);
        st.RealmIndex = 2; // mul=25
        Assert.Equal(110 * 25 / 10, PowerEngine.Evaluate(st, stats, def, LimitsConfig.Default));
    }

    [Fact]
    public void Terms_MustNotReferenceDaoHeart() // R3/R-A-NF7 护栏：Resolve 不认 daoHeart src
    {
        Assert.Throws<System.ArgumentException>(() => PowerEngine.Resolve("res:daoHeart", null!, null!));
    }

    [Fact]
    public void Terms_MustNotReferenceInnerDemon() // R3/R-A-NF7 护栏：Resolve 不认 innerDemon src
    {
        Assert.Throws<System.ArgumentException>(() => PowerEngine.Resolve("res:innerDemon", null!, null!));
    }

    [Fact]
    public void Evaluate_ClampsToPowerCap()
    {
        // realm2 mul=25：BaseSum=110 → 110*25/10=275；PowerCap=200 → 钳到 200。
        var def = TestPaths.SwordMinimal();
        var stats = StatBlockTestUtil.Of(20, 0, 0, 10);
        var st = CultivationState.NewForPath("sword_immortal", def.Resources);
        st.RealmIndex = 2;
        var limits = LimitsConfig.Default with { PowerCap = 200 };
        Assert.Equal(200, PowerEngine.Evaluate(st, stats, def, limits));
    }
}
