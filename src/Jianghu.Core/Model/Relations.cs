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

        /// <summary>只读边快照，按 (from,to) 升序（确定性，供全状态快照对账）。</summary>
        public IReadOnlyList<(long From, long To, int Value)> Edges()
        {
            var list = new List<(long, long, int)>(_aff.Count);
            foreach (var kv in _aff) list.Add((kv.Key.Item1, kv.Key.Item2, kv.Value));
            list.Sort((a, b) =>
            {
                int c = a.Item1.CompareTo(b.Item1);
                return c != 0 ? c : a.Item2.CompareTo(b.Item2);
            });
            return list;
        }
    }
}
