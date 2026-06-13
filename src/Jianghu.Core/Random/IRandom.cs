namespace Jianghu.Random
{
    /// <summary>整数确定性 PRNG 端口。所有随机必须经此。</summary>
    public interface IRandom
    {
        uint NextUInt();
        int NextInt(int maxExclusive);             // [0, maxExclusive)
        int NextInclusive(int minInclusive, int maxInclusive);
        ulong[] GetState();
        void SetState(ulong[] state);
        IRandom Split(ulong streamId);             // 派生独立子流（spawn/per-character）
    }
}
