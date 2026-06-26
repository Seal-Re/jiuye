using System;
using System.Collections.Generic;
using Jianghu.Model;

namespace Jianghu.Drama
{
    /// <summary>
    /// 恩怨账本（drama-005，spec §3.1 / Step 2）。独立有向恩怨真相源——与 Relations 并存，
    /// **不**复用负 affinity（日常切磋 -4 累积会误把口角当灭门）。
    ///
    /// 结构：List 主存（确定性迭代）+ Dictionary 索引（仅查询加速，**不参与裁决顺序**）。
    /// 合并幂等：同 (Holder,Target) 取 Kind=max / Intensity=max / Generation=min / OriginTick=首次。
    /// AboveIntensity 稳定排序 (Intensity desc, OriginTick asc, Id asc)。纯整数确定性。Clone 深拷（R-NF2）。
    /// </summary>
    public sealed class GrudgeLedger
    {
        private readonly List<Grudge> _grudges;
        private readonly Dictionary<long, List<int>> _byHolder; // holder.Value → _grudges 索引（仅加速）
        private long _nextId;

        public GrudgeLedger()
        {
            _grudges = new List<Grudge>();
            _byHolder = new Dictionary<long, List<int>>();
            _nextId = 1;
        }

        private GrudgeLedger(GrudgeLedger src)
        {
            _grudges = new List<Grudge>(src._grudges); // Grudge record 不可变，浅拷元素即深拷
            _byHolder = new Dictionary<long, List<int>>();
            foreach (var kv in src._byHolder) _byHolder[kv.Key] = new List<int>(kv.Value);
            _nextId = src._nextId;
        }

        public int Count => _grudges.Count;

        /// <summary>全部恩怨（主存序，确定性）。裁决须经 AboveIntensity 显式排序。</summary>
        public IReadOnlyList<Grudge> All => _grudges;

        /// <summary>
        /// 新增或合并恩怨。同 (holder,target) 已存在 → 合并幂等
        /// （Kind=max, Intensity=max, Generation=min, OriginTick=首次）。返回该恩怨 Id。
        /// </summary>
        public GrudgeId Form(CharacterId holder, CharacterId target, GrudgeKind kind, int intensity,
            long originTick, GrudgeCause cause, int generation, GrudgeId? inheritedFrom, int cap)
        {
            int clamped = Clamp(intensity, cap);
            int idx = IndexOf(holder, target);
            if (idx >= 0)
            {
                var old = _grudges[idx];
                var merged = old with
                {
                    Kind = (GrudgeKind)Math.Max((int)old.Kind, (int)kind),
                    Intensity = Math.Max(old.Intensity, clamped),
                    Generation = Math.Min(old.Generation, generation),
                    // OriginTick 保留首次（旧）；Cause/InheritedFrom 保留旧（首次成因）
                };
                _grudges[idx] = merged;
                return merged.Id;
            }

            var g = new Grudge(new GrudgeId(_nextId++), holder, target, kind, clamped,
                originTick, generation, cause, inheritedFrom);
            int newIdx = _grudges.Count;
            _grudges.Add(g);
            if (!_byHolder.TryGetValue(holder.Value, out var list))
                _byHolder[holder.Value] = list = new List<int>();
            list.Add(newIdx);
            return g.Id;
        }

        /// <summary>查 (holder,target) 恩怨（无→null）。</summary>
        public Grudge? Get(CharacterId holder, CharacterId target)
        {
            int idx = IndexOf(holder, target);
            return idx >= 0 ? _grudges[idx] : null;
        }

        /// <summary>某 holder 的全部恩怨（经索引，确定性序=索引插入序）。</summary>
        public IReadOnlyList<Grudge> ByHolder(CharacterId holder)
        {
            if (!_byHolder.TryGetValue(holder.Value, out var idxs)) return Array.Empty<Grudge>();
            var result = new List<Grudge>(idxs.Count);
            foreach (int i in idxs) result.Add(_grudges[i]);
            return result;
        }

        /// <summary>
        /// 强恩怨（Intensity ≥ threshold），稳定排序 (Intensity desc, OriginTick asc, Id asc)。
        /// 点火候选源（drama-007）；显式排序保确定，不依赖 Dictionary 枚举。
        /// </summary>
        public IReadOnlyList<Grudge> AboveIntensity(int threshold)
        {
            var result = new List<Grudge>();
            foreach (var g in _grudges)
                if (g.Intensity >= threshold) result.Add(g);
            result.Sort((a, b) =>
            {
                int c = b.Intensity.CompareTo(a.Intensity);      // Intensity desc
                if (c != 0) return c;
                c = a.OriginTick.CompareTo(b.OriginTick);        // OriginTick asc
                if (c != 0) return c;
                return a.Id.Value.CompareTo(b.Id.Value);         // Id asc
            });
            return result;
        }

        /// <summary>调整某恩怨强度（钳制）；不存在则 no-op 返 false。</summary>
        public bool Adjust(CharacterId holder, CharacterId target, int delta, int cap)
        {
            int idx = IndexOf(holder, target);
            if (idx < 0) return false;
            var g = _grudges[idx];
            _grudges[idx] = g with { Intensity = Clamp(g.Intensity + delta, cap) };
            return true;
        }

        /// <summary>深拷（R-NF2 续跑安全）。</summary>
        public GrudgeLedger Clone() => new GrudgeLedger(this);

        private int IndexOf(CharacterId holder, CharacterId target)
        {
            if (!_byHolder.TryGetValue(holder.Value, out var idxs)) return -1;
            foreach (int i in idxs)
                if (_grudges[i].Target.Equals(target)) return i;
            return -1;
        }

        private static int Clamp(int v, int cap) => v < 0 ? 0 : (v > cap ? cap : v);
    }
}
