using Jianghu.Model;
using Xunit;

public class IdentityTests
{
    [Fact]
    public void CharacterId_value_equality()
    {
        Assert.Equal(new CharacterId(7), new CharacterId(7));
        Assert.NotEqual(new CharacterId(7), new CharacterId(8));
    }

    [Fact]
    public void Goal_progress_advances()
    {
        var g = new Goal(GoalKind.Advance, 0);
        var g2 = g with { Progress = g.Progress + 1 };
        Assert.Equal(1, g2.Progress);
    }
}
