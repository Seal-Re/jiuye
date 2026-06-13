using System;

namespace Jianghu.Random
{
    /// <summary>PCG-XSH-RR 32（O'Neill）。纯整数，确定性，可序列化状态。</summary>
    public sealed class Pcg32 : IRandom
    {
        private ulong _state;
        private ulong _inc;
        private const ulong Mult = 6364136223846793005UL;

        public Pcg32(ulong seed, ulong stream)
        {
            _inc = (stream << 1) | 1UL;
            _state = 0UL;
            NextUInt();
            _state += seed;
            NextUInt();
        }

        // 内部直接状态构造（用于 Split / SetState 克隆），通过 enum tag 区分签名
        private enum Raw { Instance }
        private Pcg32(ulong state, ulong inc, Raw _) { _state = state; _inc = inc; }

        public uint NextUInt()
        {
            ulong old = _state;
            _state = old * Mult + _inc;
            uint xorshifted = (uint)(((old >> 18) ^ old) >> 27);
            int rot = (int)(old >> 59);
            return (xorshifted >> rot) | (xorshifted << ((-rot) & 31));
        }

        public int NextInt(int maxExclusive)
        {
            if (maxExclusive <= 0) throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            ulong m = (ulong)(uint)NextUInt() * (ulong)(uint)maxExclusive;
            uint l = (uint)m;
            if (l < (uint)maxExclusive)
            {
                uint t = (uint)(-maxExclusive) % (uint)maxExclusive;
                while (l < t) { m = (ulong)(uint)NextUInt() * (ulong)(uint)maxExclusive; l = (uint)m; }
            }
            return (int)(m >> 32);
        }

        public int NextInclusive(int minInclusive, int maxInclusive)
        {
            if (maxInclusive < minInclusive) throw new ArgumentException("max<min");
            return minInclusive + NextInt(maxInclusive - minInclusive + 1);
        }

        public ulong[] GetState() => new[] { _state, _inc };
        public void SetState(ulong[] state) { _state = state[0]; _inc = state[1]; }

        public IRandom Split(ulong streamId)
        {
            ulong s = _state ^ (streamId * Mult);
            return new Pcg32(s, (streamId << 1) | 1UL, Raw.Instance);
        }
    }
}
