using Jianghu.Model;
using Jianghu.Sim;
using Xunit;

public class SchedulerTests
{
    [Fact]
    public void Pops_in_time_then_id_order()
    {
        var s = new Scheduler();
        s.Push(new CharacterId(2), at: 10);
        s.Push(new CharacterId(1), at: 10);  // 同时刻 → 小 Id 先
        s.Push(new CharacterId(3), at: 5);
        Assert.Equal(new CharacterId(3), s.PopMin().Id);
        Assert.Equal(new CharacterId(1), s.PopMin().Id);
        Assert.Equal(new CharacterId(2), s.PopMin().Id);
        Assert.True(s.IsEmpty);
    }
}
