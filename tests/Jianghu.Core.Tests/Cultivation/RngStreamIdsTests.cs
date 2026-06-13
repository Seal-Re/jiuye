using Jianghu.Random;
using Xunit;

public class RngStreamIdsTests
{
    [Fact]
    public void StreamIds_AreFrozenAndDistinct()
    {
        Assert.Equal(1, RngStreamIds.Gen);
        Assert.Equal(2, RngStreamIds.Domain);
        Assert.Equal(3, RngStreamIds.Spawn);
        Assert.Equal(4, RngStreamIds.Brain);
        Assert.Equal(5, RngStreamIds.Cultivation);
        Assert.Equal(6, RngStreamIds.Drama);
        Assert.Equal(7, RngStreamIds.Map);
        Assert.Equal(8, RngStreamIds.Faction);
    }
}
