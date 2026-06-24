using System;
using System.Collections.Generic;

namespace Jianghu.Cultivation
{
    /// <summary>
    /// 破单调 VarietyTracker——滑动窗口追踪 DailyMode 多样性（A3-FINAL §1.3, story-006）。
    /// 纯整数，确定性。两个不变量：
    /// INV-VARIETY: K=10 窗口内 DailyMode 种类 ≥ 2
    /// INV-NO-DOMINANT: 50 tick 窗口单一模式占比 ≤ 80%
    /// </summary>
    public sealed class VarietyTracker
    {
        public const int VARIETY_WINDOW = 10;
        public const int DOMINANT_WINDOW = 50;
        public const int DOMINANT_THRESHOLD_PCT = 80;

        private readonly int[] _shortWindow;  // 10-tick ring buffer
        private readonly int[] _longWindow;   // 50-tick ring buffer
        private int _shortIndex;
        private int _longIndex;
        private int _shortFill;  // entries written (0..10)
        private int _longFill;   // entries written (0..50)

        public VarietyTracker()
        {
            _shortWindow = new int[VARIETY_WINDOW];
            _longWindow = new int[DOMINANT_WINDOW];
            _shortIndex = 0;
            _longIndex = 0;
            _shortFill = 0;
            _longFill = 0;
        }

        /// <summary>记录一次 DailyMode 选择。</summary>
        public void Record(DailyMode mode)
        {
            int m = (int)mode;

            _shortWindow[_shortIndex] = m;
            _shortIndex = (_shortIndex + 1) % VARIETY_WINDOW;
            if (_shortFill < VARIETY_WINDOW) _shortFill++;

            _longWindow[_longIndex] = m;
            _longIndex = (_longIndex + 1) % DOMINANT_WINDOW;
            if (_longFill < DOMINANT_WINDOW) _longFill++;
        }

        /// <summary>INV-VARIETY: K=10 窗口内不同模式数。未满窗口返回实际填充数。</summary>
        public int DistinctModesInShortWindow()
        {
            if (_shortFill == 0) return 0;
            var seen = new HashSet<int>();
            int count = Math.Min(_shortFill, VARIETY_WINDOW);
            for (int i = 0; i < count; i++)
                seen.Add(_shortWindow[i]);
            return seen.Count;
        }

        /// <summary>INV-NO-DOMINANT: 50 tick 窗口内主导模式占比（%）。</summary>
        public int DominantModePct()
        {
            if (_longFill == 0) return 0;
            var counts = new int[4]; // 4 modes
            int count = Math.Min(_longFill, DOMINANT_WINDOW);
            for (int i = 0; i < count; i++)
                counts[_longWindow[i]]++;
            int maxCount = 0;
            for (int i = 0; i < 4; i++)
                if (counts[i] > maxCount) maxCount = counts[i];
            return maxCount * 100 / count;
        }

        // ============================
        // Invariant checks
        // ============================

        /// <summary>INV-VARIETY 是否满足（窗口未满 2 时自动通过）。</summary>
        public bool PassesInvariety()
        {
            if (_shortFill < 2) return true; // Not enough data
            return DistinctModesInShortWindow() >= 2;
        }

        /// <summary>INV-NO-DOMINANT 是否满足（窗口未满时自动通过）。</summary>
        public bool PassesInvNoDominant()
        {
            if (_longFill < DOMINANT_WINDOW / 2) return true; // Not enough data
            return DominantModePct() <= DOMINANT_THRESHOLD_PCT;
        }

        /// <summary>诊断：当前两个不变量的状态。</summary>
        public (bool varietyOk, bool dominantOk, int distinctModes, int dominantPct) Diagnosis()
        {
            return (PassesInvariety(), PassesInvNoDominant(),
                DistinctModesInShortWindow(), DominantModePct());
        }

        /// <summary>深拷——用于 Clone/CultivationState 复制。</summary>
        public VarietyTracker Clone()
        {
            var t = new VarietyTracker();
            Array.Copy(_shortWindow, t._shortWindow, VARIETY_WINDOW);
            Array.Copy(_longWindow, t._longWindow, DOMINANT_WINDOW);
            t._shortIndex = _shortIndex;
            t._longIndex = _longIndex;
            t._shortFill = _shortFill;
            t._longFill = _longFill;
            return t;
        }
    }
}
