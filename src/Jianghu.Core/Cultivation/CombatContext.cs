using System.Collections.Generic;

namespace Jianghu.Cultivation
{
    /// <summary>战斗双方标识：算子从攻方/防方视角读写资源、查情境 tag。</summary>
    public enum Side { Attacker, Defender }

    /// <summary>
    /// 单挑战斗上下文（spec §15.3 chokepoint）。持双方 <see cref="CultivationState"/> 与各自 path
    /// （HasTag 读 path.SituationalTags）。**不暴露裸 Dictionary**：资源只经
    /// <see cref="ReadResource"/>/<see cref="ApplyResource"/>，改资源必经
    /// <see cref="CultivationState.ApplyResource"/>（带 [Min,Cap] 钳），算子不得直写 dict。纯整数。
    /// </summary>
    public sealed class CombatContext
    {
        private readonly CultivationState _attacker;
        private readonly CultivationPathDef _attackerPath;
        private readonly CultivationState _defender;
        private readonly CultivationPathDef _defenderPath;
        private readonly Dictionary<Side, Dictionary<string, int>> _statDeltas = new();

        public CombatContext(
            CultivationState attacker, CultivationPathDef attackerPath,
            CultivationState defender, CultivationPathDef defenderPath)
        {
            _attacker = attacker;
            _attackerPath = attackerPath;
            _defender = defender;
            _defenderPath = defenderPath;
        }

        private CultivationState StateOf(Side s) => s == Side.Attacker ? _attacker : _defender;
        private CultivationPathDef PathOf(Side s) => s == Side.Attacker ? _attackerPath : _defenderPath;

        /// <summary>读该方资源（无该 key → 0）。</summary>
        public int ReadResource(Side s, string key)
        {
            StateOf(s).Resources.TryGetValue(key, out int v);
            return v;
        }

        /// <summary>
        /// 该方是否注册了某资源 key（唯一档 handler 路无关，写前据此判定「该路是否有此资源」，
        /// 区分「缺该 key」与「有 key 但值 0」——只对**该路存在**的资源经 chokepoint 落副作用，
        /// 不往无该资源的路硬写（避免 <see cref="CultivationState.ApplyResource"/> 撞 KeyNotFound）。只读，不改 state。
        /// </summary>
        public bool HasResource(Side s, string key) => StateOf(s).Resources.ContainsKey(key);

        /// <summary>改该方资源（chokepoint）：转发 <see cref="CultivationState.ApplyResource"/>，钳 [Min,Cap]。</summary>
        public void ApplyResource(Side s, string key, int delta)
        {
            StateOf(s).ApplyResource(key, delta);
        }

        /// <summary>该方 path 是否含某情境 tag（读 SituationalTags，含则 true）。</summary>
        public bool HasTag(Side s, string tag)
        {
            foreach (var t in PathOf(s).SituationalTags)
                if (t == tag) return true;
            return false;
        }

        /// <summary>累积 stat 修改（ModifyStat 算子用）。不直接改 StatBlock，战后落。</summary>
        public void AccumulateStatDelta(Side s, string statKind, int delta)
        {
            if (!_statDeltas.TryGetValue(s, out var dict))
                _statDeltas[s] = dict = new Dictionary<string, int>();
            dict.TryGetValue(statKind, out int cur);
            dict[statKind] = cur + delta;
        }

        /// <summary>提取指定方 stat delta 快照。</summary>
        public IReadOnlyDictionary<string, int> GetStatDeltas(Side s)
            => _statDeltas.TryGetValue(s, out var d) ? d : new Dictionary<string, int>();
    }
}
