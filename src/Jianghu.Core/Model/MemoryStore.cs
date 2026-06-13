using System.Collections.Generic;
using System.Linq;

namespace Jianghu.Model
{
    /// <summary>聚合根私有记忆。超限按 |Valence| 升序淘汰，平手淘汰更早 Tick（确定性，§4.5）。</summary>
    public sealed class MemoryStore
    {
        private readonly List<MemoryEntry> _items = new List<MemoryEntry>();
        private readonly int _cap;
        public MemoryStore(int cap) { _cap = cap; }

        public void Remember(MemoryEntry e)
        {
            _items.Add(e);
            if (_items.Count <= _cap) return;
            int worst = 0;
            for (int i = 1; i < _items.Count; i++)
            {
                int wa = System.Math.Abs(_items[worst].Valence), wi = System.Math.Abs(_items[i].Valence);
                if (wi < wa || (wi == wa && _items[i].Tick < _items[worst].Tick)) worst = i;
            }
            _items.RemoveAt(worst);
        }

        public IReadOnlyList<MemoryEntry> Recall() => _items.AsReadOnly();
        public MemoryStore Clone()
        {
            var m = new MemoryStore(_cap);
            m._items.AddRange(_items);
            return m;
        }
    }
}
