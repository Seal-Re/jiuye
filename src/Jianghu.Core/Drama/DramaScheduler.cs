using System;
using System.Collections.Generic;

namespace Jianghu.Drama
{
    /// <summary>调度项：弧 + 唤醒时刻（drama-009）。</summary>
    public readonly record struct DramaScheduleItem(ArcId Arc, long At);

    /// <summary>
    /// 戏剧弧调度器（drama-009，spec §3.4A）。确定性最小堆，按 (At, Arc.Id) 排序——
    /// 仿 <see cref="Jianghu.Sim.Scheduler"/>（既有成熟件），弧到期推进的时间轮。
    /// 加 HasDue 到期判定（Pump 用）+ Snapshot/LoadFrom 续跑（drama-010 Clone 深拷）。
    /// </summary>
    public sealed class DramaScheduler
    {
        private readonly List<DramaScheduleItem> _h = new List<DramaScheduleItem>();
        public bool IsEmpty => _h.Count == 0;
        public int Count => _h.Count;

        private static int Cmp(DramaScheduleItem a, DramaScheduleItem b)
        {
            int c = a.At.CompareTo(b.At);
            return c != 0 ? c : a.Arc.Value.CompareTo(b.Arc.Value);
        }

        public void Push(ArcId arc, long at)
        {
            _h.Add(new DramaScheduleItem(arc, at));
            int i = _h.Count - 1;
            while (i > 0) { int p = (i - 1) / 2; if (Cmp(_h[i], _h[p]) < 0) { (_h[i], _h[p]) = (_h[p], _h[i]); i = p; } else break; }
        }

        public DramaScheduleItem PopMin()
        {
            if (_h.Count == 0) throw new InvalidOperationException("DramaScheduler empty");
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

        public DramaScheduleItem PeekMin()
        {
            if (_h.Count == 0) throw new InvalidOperationException("DramaScheduler empty");
            return _h[0];
        }

        /// <summary>是否有到期弧（堆顶 At &lt;= clock）。空堆→false。</summary>
        public bool HasDue(long clock) => _h.Count > 0 && _h[0].At <= clock;

        /// <summary>当前堆项只读快照（供 drama-010 Clone）。</summary>
        public IReadOnlyList<DramaScheduleItem> Snapshot() => _h.AsReadOnly();

        /// <summary>清空后按 items 重建（R-NF2 续跑）。</summary>
        public void LoadFrom(IEnumerable<DramaScheduleItem> items)
        {
            _h.Clear();
            foreach (var it in items) Push(it.Arc, it.At);
        }
    }
}
