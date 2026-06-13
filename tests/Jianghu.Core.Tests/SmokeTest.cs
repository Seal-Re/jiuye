using Xunit;

public class SmokeTest
{
    [Fact]
    public void Core_assembly_loads()
    {
        Assert.NotNull(typeof(Jianghu.Random.Pcg32).Assembly);
    }
}
