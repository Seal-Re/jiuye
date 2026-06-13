using System.Collections.Generic;

namespace Jianghu.Model
{
    /// <summary>有向好感图，键 (from,to)，值钳制 [-100,100]。</summary>
    public sealed class Relations
    {
        private readonly Dictionary<(long, long), int> _aff = new Dictionary<(long, long), int>();
        public int Affinity(CharacterId from, CharacterId to)
            => _aff.TryGetValue((from.Value, to.Value), out var v) ? v : 0;

        public int Adjust(CharacterId from, CharacterId to, int delta)
        {
            int nv = Affinity(from, to) + delta;
            if (nv > 100) nv = 100; if (nv < -100) nv = -100;
            _aff[(from.Value, to.Value)] = nv;
            return nv;
        }

        public Relations Clone()
        {
            var r = new Relations();
            foreach (var kv in _aff) r._aff[kv.Key] = kv.Value;
            return r;
        }
    }
}
