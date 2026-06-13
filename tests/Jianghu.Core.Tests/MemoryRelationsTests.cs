using System.Linq;
using Jianghu.Model;
using Xunit;

public class MemoryRelationsTests
{
    [Fact]
    public void Memory_caps_keeping_most_salient()
    {
        var m = new MemoryStore(cap: 3);
        m.Remember(new MemoryEntry(1, "spar", new CharacterId(1), new CharacterId(2), Valence: 1));
        m.Remember(new MemoryEntry(2, "spar", new CharacterId(1), new CharacterId(3), Valence: 9)); // 高显著
        m.Remember(new MemoryEntry(3, "train", new CharacterId(1), null, Valence: 1));
        m.Remember(new MemoryEntry(4, "train", new CharacterId(1), null, Valence: 1)); // 触发淘汰
        Assert.Equal(3, m.Recall().Count);
        Assert.Contains(m.Recall(), e => e.Valence == 9); // 高显著保留
    }

    [Fact]
    public void Relations_directed_affinity_adjust_and_clamp()
    {
        var r = new Relations();
        var a = new CharacterId(1); var b = new CharacterId(2);
        r.Adjust(a, b, +30);
        Assert.Equal(30, r.Affinity(a, b));
        Assert.Equal(0, r.Affinity(b, a));      // 有向
        r.Adjust(a, b, +1000);
        Assert.Equal(100, r.Affinity(a, b));    // 钳到 [-100,100]
    }
}
