using Jianghu.Random;
using Xunit;

public class Pcg32Tests
{
    [Fact]
    public void Fixed_vector_is_stable_across_runs()
    {
        var r = new Pcg32(seed: 42, stream: 54);
        uint[] got = { r.NextUInt(), r.NextUInt(), r.NextUInt() };
        // 黄金向量：实现完成后用首次输出回填，固化跨运行/跨平台一致
        uint[] golden = { 0xA15C02B7u, 0x7B47F409u, 0xBA1D3330u };
        Assert.Equal(golden, got);
    }

    [Fact]
    public void GetState_SetState_restores_full_stream()
    {
        var r = new Pcg32(1, 7);
        for (int i = 0; i < 5; i++) r.NextUInt();
        ulong[] snap = r.GetState();
        uint[] expect = new uint[5];
        for (int i = 0; i < 5; i++) expect[i] = r.NextUInt();

        var r2 = new Pcg32(999, 999);          // 不同 inc，确保恢复的是完整状态
        r2.SetState(snap);
        for (int i = 0; i < 5; i++) Assert.Equal(expect[i], r2.NextUInt()); // 整段逐值一致
    }

    [Fact]
    public void Split_streams_are_independent_and_deterministic()
    {
        var a1 = new Pcg32(7, 1).Split(100);
        var a2 = new Pcg32(7, 1).Split(100);
        var b  = new Pcg32(7, 1).Split(200);
        Assert.Equal(a1.NextUInt(), a2.NextUInt());      // 同源同 streamId → 一致
        Assert.NotEqual(a1.NextUInt(), b.NextUInt());     // 不同 streamId → 分流
    }
}
