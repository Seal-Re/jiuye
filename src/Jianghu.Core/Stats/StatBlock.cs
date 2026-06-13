using System;
using Jianghu.Config;

namespace Jianghu.Stats
{
    /// <summary>属性容器。运行期变更只经 Apply（单一 chokepoint，§4.6）。</summary>
    public sealed class StatBlock
    {
        private readonly int[] _v;
        public StatBlock(int[] values)
        {
            if (values.Length != 4) throw new ArgumentException("需 4 维");
            _v = (int[])values.Clone();
        }
        public int Get(StatKind k) => _v[(int)k];
        public int Sum { get { int s = 0; foreach (var x in _v) s += x; return s; } }

        /// <summary>运行期成长：只受单项 cap，钳制到 [0, StatCap]，溢出丢弃（§5.4）。</summary>
        public void Apply(StatKind k, int delta, LimitsConfig limits)
        {
            int nv = _v[(int)k] + delta;
            if (nv < 0) nv = 0;
            if (nv > limits.StatCap) nv = limits.StatCap;
            _v[(int)k] = nv;
        }

        public StatBlock Clone() => new StatBlock(_v);
        public int[] ToArray() => (int[])_v.Clone();
    }
}
