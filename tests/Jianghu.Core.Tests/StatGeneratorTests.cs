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
        var r = new Pcg32(7, 1);
        int near = 0, extreme = 0;
        for (int i = 0; i < 5000; i++)
        {
            int force = StatGenerator.Generate(r, c).Get(StatKind.Force);
            if (force >= 16 && force <= 24) near++;       // |x-20|<=4
            if (force <= 8 || force >= 28) extreme++;      // 远端
        }
        Assert.True(near > extreme * 3, $"near={near} extreme={extreme} 不够中庸");
    }

    [Fact]
    public void Generation_is_deterministic()
    {
        var c = LimitsConfig.Default;
        Assert.Equal(StatGenerator.Generate(new Pcg32(42, 1), c).ToArray(),
                     StatGenerator.Generate(new Pcg32(42, 1), c).ToArray());
    }
}
