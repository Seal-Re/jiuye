using Jianghu.Config;
using Jianghu.Random;
using Jianghu.Stats;
using Xunit;

public class StatGeneratorTests
{
    [Fact]
    public void Always_satisfies_sum_cap_min_invariants()
    {
        var c = LimitsConfig.Default;
        var r = new Pcg32(123, 1);
        for (int i = 0; i < 5000; i++)
        {
            var s = StatGenerator.Generate(r, c);
            Assert.Equal(c.StatSum, s.Sum);
            for (int k = 0; k < 4; k++)
            {
                int v = s.Get((StatKind)k);
                Assert.InRange(v, c.StatMin, c.StatCap);
            }
        }
    }

    [Fact]
    public void Distribution_is_central_tendency_not_uniform()
    {
        var c = LimitsConfig.Default;
        int near = 0, extreme = 0;
        foreach (ulong seed in new ulong[] { 7, 1, 2, 42, 123, 999 })
        {
            var r = new Pcg32(seed, 1);
            for (int i = 0; i < 3000; i++)
            {
                var s = StatGenerator.Generate(r, c);
                for (int k = 0; k < 4; k++)            // 全 4 维
                {
                    int x = s.Get((StatKind)k);
                    if (x >= 16 && x <= 24) near++;
                    if (x <= 8 || x >= 28) extreme++;
                }
            }
        }
        Assert.True(near > extreme * 5, $"near={near} extreme={extreme} 不够中庸"); // 收紧到 5x
    }

    [Fact]
    public void Generation_is_deterministic()
    {
        var c = LimitsConfig.Default;
        Assert.Equal(StatGenerator.Generate(new Pcg32(42, 1), c).ToArray(),
                     StatGenerator.Generate(new Pcg32(42, 1), c).ToArray());
    }
}
