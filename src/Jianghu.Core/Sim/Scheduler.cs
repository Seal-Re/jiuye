using System;
using System.Collections.Generic;
using Jianghu.Model;

namespace Jianghu.Sim
{
    public readonly record struct ScheduleItem(CharacterId Id, long At);

    /// <summary>确定性最小堆，按 (At, Id) 排序。</summary>
    public sealed class Scheduler
    {
        private readonly List<ScheduleItem> _h = new List<ScheduleItem>();
        public bool IsEmpty => _h.Count == 0;
        public int Count => _h.Count;

        private static int Cmp(ScheduleItem a, ScheduleItem b)
        {
            int c = a.At.CompareTo(b.At);
            return c != 0 ? c : a.Id.Value.CompareTo(b.Id.Value);
        }

        public void Push(CharacterId id, long at)
        {
            _h.Add(new ScheduleItem(id, at));
            int i = _h.Count - 1;
            while (i > 0) { int p = (i - 1) / 2; if (Cmp(_h[i], _h[p]) < 0) { (_h[i], _h[p]) = (_h[p], _h[i]); i = p; } else break; }
        }

        public ScheduleItem PopMin()
        {
            if (_h.Count == 0) throw new InvalidOperationException("empty");
            var min = _h[0];
            _h[0] = _h[_h.Count - 1];
            _h.RemoveAt(_h.Count - 1);
            int i = 0;
            while (true)
            {
                int l = 2 * i + 1, r = 2 * i + 2, s = i;
                if (l < _h.Count && Cmp(_h[l], _h[s]) < 0) s = l;
                if (r < _h.Count && Cmp(_h[r], _h[s]) < 0) s = r;
                if (s == i) break;
                (_h[i], _h[s]) = (_h[s], _h[i]); i = s;
            }
            return min;
        }

        public ScheduleItem PeekMin() => _h[0];
        public IReadOnlyList<ScheduleItem> Snapshot() => _h.AsReadOnly();
        public void LoadFrom(IEnumerable<ScheduleItem> items) { _h.Clear(); foreach (var it in items) Push(it.Id, it.At); }
    }
}
