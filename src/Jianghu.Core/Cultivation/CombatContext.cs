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
    }
}
