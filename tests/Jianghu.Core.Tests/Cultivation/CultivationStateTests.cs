using Jianghu.Cultivation;
using Xunit;

public class CultivationStateTests
{
    [Fact]
    public void ApplyResource_ClampsToBounds()
    {
        var st = CultivationState.NewForPath("sword_immortal", new[] { new ResourceDef("swordWill", 0, 20, 5) });
        st.ApplyResource("swordWill", +999);
        Assert.Equal(20, st.Resources["swordWill"]);
        st.ApplyResource("swordWill", -999);
        Assert.Equal(0, st.Resources["swordWill"]);
    }

    [Fact]
    public void Clone_IsDeepIndependent()
    {
        var st = CultivationState.NewForPath("sword_immortal", new[] { new ResourceDef("swordWill", 0, 20, 5) });
        var c = st.Clone();
        c.ApplyResource("swordWill", +10);
        Assert.NotEqual(st.Resources["swordWill"], c.Resources["swordWill"]);
    }
}
