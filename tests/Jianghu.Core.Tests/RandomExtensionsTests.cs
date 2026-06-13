using System.Collections.Generic;
using System.Linq;
using Jianghu.Random;
using Xunit;

public class RandomExtensionsTests
{
    [Fact]
    public void Shuffle_is_deterministic_and_a_permutation()
    {
        var a = Enumerable.Range(0, 20).ToList();
        var b = Enumerable.Range(0, 20).ToList();
        new Pcg32(5, 1).Shuffle(a);
        new Pcg32(5, 1).Shuffle(b);
        Assert.Equal(a, b);                          // 同种子同序
        Assert.Equal(Enumerable.Range(0, 20), a.OrderBy(x => x)); // 是个排列
    }

    [Fact]
    public void NextInclusive_hits_both_endpoints()
    {
        var r = new Pcg32(9, 1);
        var seen = new HashSet<int>();
        for (int i = 0; i < 2000; i++) seen.Add(r.NextInclusive(3, 6));
        Assert.True(seen.SetEquals(new[] { 3, 4, 5, 6 })); // 含端点、不越界
    }
}
