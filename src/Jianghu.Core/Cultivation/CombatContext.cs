using System.Collections.Generic;

namespace Jianghu.Cultivation
{
    /// <summary>战斗双方标识：算子从攻方/防方视角读写资源、查情境 tag。</summary>
    public enum Side { Attacker, Defender }

    /// <summary>
    /// 回滚信号枚举（story fullstruct-007）。特殊模块（逆演/夺舍）在 OnUse 中设置此信号，
    /// DuelEngine 在扣血后读取并执行对应回滚操作。None 表示无回滚请求。
    /// </summary>
    internal enum RollbackSignal { None, ReverseRequested, PossessionRequested }

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
        private readonly Dictionary<Side, List<(string ResourceKey, int Num, int Den)>> _epModifiers = new();
        private readonly Dictionary<Side, int> _relationDeltas = new();

        /// <summary>回滚信号（story fullstruct-007）。特殊模块设置，DuelEngine 读取后执行回滚并清零。</summary>
        internal RollbackSignal PendingRollback { get; set; }

        /// <summary>模块伤害上限（balance-003）。OnUse 模块每操作伤害增量钳至此值（未缩放空间）。0=无限制。</summary>
        public int ModuleDamageCap { get; set; }

        public CombatContext(
            CultivationState attacker, CultivationPathDef attackerPath,
            CultivationState defender, CultivationPathDef defenderPath)
        {
            _attacker = attacker;
            _attackerPath = attackerPath;
            _defender = defender;
            _defenderPath = defenderPath;
            PendingRollback = RollbackSignal.None;
            ModuleDamageCap = 0; // 0=no cap (backward compatible)
        }

        internal CultivationState StateOf(Side s) => s == Side.Attacker ? _attacker : _defender;
        internal CultivationPathDef PathOf(Side s) => s == Side.Attacker ? _attackerPath : _defenderPath;

        /// <summary>读该方资源（无该 key → 0）。A.2 伪资源: daoHeart/innerDemon/comprehension 读 CultivationState 字段。</summary>
        public int ReadResource(Side s, string key)
        {
            // A.2 伪资源: 读道心/心魔/悟道值（不进 EffectivePower, R3）
            var st = StateOf(s);
            if (key == "daoHeart") return st.DaoHeart;
            if (key == "innerDemon") return st.InnerDemon;
            if (key == "comprehension") return st.Comprehension;
            st.Resources.TryGetValue(key, out int v);
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

        /// <summary>累积 EP 百分比修改器。Amount×res(Key)/Den → 乘子。</summary>
        public void AccumulateEPModifier(Side target, string resourceKey, int num, int den)
        {
            if (!_epModifiers.TryGetValue(target, out var list))
                _epModifiers[target] = list = new List<(string, int, int)>();
            list.Add((resourceKey, num, den));
        }

        /// <summary>累积关系边修改（RelationAdjust 算子用）。</summary>
        public void AccumulateRelationDelta(Side s, int delta)
        {
            _relationDeltas.TryGetValue(s, out int cur);
            _relationDeltas[s] = cur + delta;
        }

        /// <summary>提取指定方 relation delta。</summary>
        public int GetRelationDelta(Side s)
            => _relationDeltas.TryGetValue(s, out int d) ? d : 0;

        // —— 功法门控（story fullstruct-006）——

        /// <summary>
        /// 检查指定方是否满足门控条件。所有 flag 位必须全部满足（AND 语义）。
        /// GateType.None 始终返回 true。
        /// role-based gate（movement/swordwill/body/core_forge 等）直接查 ArtCategoryDef.Role；
        /// resource-based gate（formation/alchemy）据 path 专属资源判定。
        /// </summary>
        public bool CheckGate(Side side, GateType gate)
        {
            if (gate == GateType.None) return true;

            var state = StateOf(side);
            var path = PathOf(side);

            if (gate.HasFlag(GateType.HasMovementArt) && !HasRoleArt(path, state, "movement"))
                return false;
            if (gate.HasFlag(GateType.HasSwordIntent) && !HasRoleArt(path, state, "swordwill"))
                return false;
            if (gate.HasFlag(GateType.HasBodyArt) && !CheckBodyGate(path, state))
                return false;
            if (gate.HasFlag(GateType.HasArtifactArts) && !CheckArtifactGate(path, state))
                return false;
            if (gate.HasFlag(GateType.HasFormation) && !CheckFormationGate(path, state))
                return false;
            if (gate.HasFlag(GateType.HasAlchemy) && !CheckAlchemyGate(path, state))
                return false;

            return true;
        }

        private static bool HasRoleArt(CultivationPathDef path, CultivationState state, string role)
        {
            foreach (var cat in path.ArtCategories)
            {
                if (cat.Role != role) continue;
                if (HasAnyChosenArt(state, cat)) return true;
            }
            return false;
        }

        private static bool CheckBodyGate(CultivationPathDef path, CultivationState state)
        {
            if (HasRoleArt(path, state, "body")) return true;
            if (HasResourceDef(path, "qixue") || HasResourceDef(path, "henglian"))
                return HasAnyArtInPath(state, path);
            return false;
        }

        private static bool CheckArtifactGate(CultivationPathDef path, CultivationState state)
        {
            if (HasRoleArt(path, state, "core_forge")) return true;
            if (HasRoleArt(path, state, "channel_mind")) return true;
            if (HasRoleArt(path, state, "named_artifacts")) return true;
            return false;
        }

        private static bool CheckFormationGate(CultivationPathDef path, CultivationState state)
        {
            bool isFormationPath = HasResourceDef(path, "stones")
                || HasResourceDef(path, "setupProgress")
                || HasResourceDef(path, "compute");
            return isFormationPath && HasAnyArtInPath(state, path);
        }

        private static bool CheckAlchemyGate(CultivationPathDef path, CultivationState state)
        {
            bool isAlchemyPath = HasResourceDef(path, "flameTier")
                || HasResourceDef(path, "recipeCount")
                || HasResourceDef(path, "pillStock");
            return isAlchemyPath && HasAnyArtInPath(state, path);
        }

        private static bool HasResourceDef(CultivationPathDef path, string key)
        {
            foreach (var r in path.Resources)
                if (r.Key == key) return true;
            return false;
        }

        private static bool HasAnyArtInPath(CultivationState state, CultivationPathDef path)
        {
            foreach (var cat in path.ArtCategories)
                if (HasAnyChosenArt(state, cat)) return true;
            return false;
        }

        private static bool HasAnyChosenArt(CultivationState state, ArtCategoryDef cat)
        {
            foreach (var art in cat.Arts)
                if (ListContains(state.ChosenArtIds, art.Id)) return true;
            return false;
        }

        private static bool ListContains(System.Collections.Generic.IReadOnlyList<string> list, string item)
        {
            foreach (var x in list)
                if (x == item) return true;
            return false;
        }

        /// <summary>应用所有 EP 修改器到给定 PE 值，返回修正后 PE。</summary>
        public int ApplyEPModifiers(Side s, int basePE)
        {
            if (!_epModifiers.TryGetValue(s, out var list)) return basePE;
            long adjusted = basePE * 100; // ×100 防截断
            foreach (var (key, num, den) in list)
            {
                int resVal = ReadResource(s, key);
                // modifier = (10000 + num × resVal × 100 / den) / 10000
                long mod = 10000L + (long)num * resVal * 100 / den;
                adjusted = adjusted * mod / 10000;
            }
            return (int)(adjusted / 100);
        }
    }
}
