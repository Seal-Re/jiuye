using System;
using System.Collections.Generic;

namespace Jianghu.Cultivation
{
    /// <summary>
    /// 角色侧修炼运行态（spec §5）。Character 可空（散修=null；cultivation-off=null）。
    /// 资源变更只经 <see cref="ApplyResource"/>（单一 chokepoint，钳制 [Min,Cap]，R-A-NF6）。
    /// <see cref="Clone"/> 逐项深拷（进 S0-a 全状态快照对账）。纯整数，禁浮点。
    /// </summary>
    public sealed class CultivationState
    {
        public string PathId { get; set; } = string.Empty;

        /// <summary>A.0 单层 flatIndex（A.1 投影双层，不存额外字段）。</summary>
        public int RealmIndex { get; set; }

        /// <summary>本路修为累加器（单调计数器，非资源 → 不经 ApplyResource，无 [Min,Cap] 钳）。默认 0；运行期行动累加。</summary>
        public int CultivationPoints { get; set; }

        public IReadOnlyList<string> ChosenArtIds { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> ChosenSkillIds { get; init; } = Array.Empty<string>();

        /// <summary>resourceKey→整数，恒在 [Min,Cap]（经 ApplyResource 钳制）。</summary>
        public Dictionary<string, int> Resources { get; }

        /// <summary>0/1 标志。</summary>
        public Dictionary<string, int> Flags { get; }

        // 资源边界留存：key→(Min,Cap)，ApplyResource 据此钳制。
        private readonly Dictionary<string, (int Min, int Cap)> _caps;

        // —— A.2 道心层（R3/R-A-NF7：严禁进 EffectivePower）——
        public int DaoHeart { get; set; }       // A.2 才读写，禁进 EffectivePower（R3）
        public int InnerDemon { get; set; }     // A.2 才读写，禁进 EffectivePower（R3）
        public int Comprehension { get; set; }  // A.2 才读写，禁进 EffectivePower（R3）

        /// <summary>增益道心（钳 [0,100]）。返回实际增益量（可能小于 delta）。</summary>
        public int GainDaoHeart(int delta)
        {
            int old = DaoHeart;
            DaoHeart = Math.Max(0, Math.Min(100, DaoHeart + delta));
            return DaoHeart - old;
        }

        /// <summary>增益心魔（钳 [0,100]）。返回实际增益量（可能小于 delta）。</summary>
        public int GainInnerDemon(int delta)
        {
            int old = InnerDemon;
            InnerDemon = Math.Max(0, Math.Min(100, InnerDemon + delta));
            return InnerDemon - old;
        }

        private CultivationState(
            string pathId, int realmIndex, int cultivationPoints,
            IReadOnlyList<string> chosenArtIds, IReadOnlyList<string> chosenSkillIds,
            Dictionary<string, int> resources, Dictionary<string, int> flags,
            Dictionary<string, (int Min, int Cap)> caps,
            int daoHeart, int innerDemon, int comprehension)
        {
            PathId = pathId;
            RealmIndex = realmIndex;
            CultivationPoints = cultivationPoints;
            ChosenArtIds = chosenArtIds;
            ChosenSkillIds = chosenSkillIds;
            Resources = resources;
            Flags = flags;
            _caps = caps;
            DaoHeart = daoHeart;
            InnerDemon = innerDemon;
            Comprehension = comprehension;
        }

        /// <summary>
        /// 初路初始化：每资源 key→Initial 钳 [Min,Cap]，Flags 空，RealmIndex=0，
        /// daoHeart/innerDemon/comprehension=0（A.2 预留，A.0 不读写 R3）。
        /// </summary>
        public static CultivationState NewForPath(string pathId, IReadOnlyList<ResourceDef> resources)
            => NewForPath(pathId, resources, Array.Empty<string>(), Array.Empty<string>());

        /// <summary>
        /// 初路初始化（带所选功法/战技）：生成期 <see cref="PathAssigner"/> 定路后填 ChosenArtIds/ChosenSkillIds。
        /// 资源 key→Initial 钳 [Min,Cap]，Flags 空，RealmIndex=0，daoHeart/innerDemon/comprehension=0（R3 预留）。
        /// </summary>
        public static CultivationState NewForPath(
            string pathId, IReadOnlyList<ResourceDef> resources,
            IReadOnlyList<string> chosenArtIds, IReadOnlyList<string> chosenSkillIds)
        {
            var res = new Dictionary<string, int>();
            var caps = new Dictionary<string, (int Min, int Cap)>();
            foreach (var r in resources)
            {
                res[r.Key] = Clamp(r.Initial, r.Min, r.Cap);
                caps[r.Key] = (r.Min, r.Cap);
            }
            return new CultivationState(
                pathId, 0, 0,
                chosenArtIds, chosenSkillIds,
                res, new Dictionary<string, int>(), caps,
                0, 0, 0);
        }

        /// <summary>资源变更唯一入口（chokepoint）：delta 后钳到该 key 的 [Min,Cap]。</summary>
        public void ApplyResource(string key, int delta)
        {
            var (min, cap) = _caps[key];
            Resources[key] = Clamp(Resources[key] + delta, min, cap);
        }

        /// <summary>抬某资源上限 [Min,Cap] 的 Cap（装配期 AddResourceCap 算子用）；现值不动，后续 ApplyResource 据新 Cap 钳。</summary>
        public void RaiseCap(string key, int delta)
        {
            var (min, cap) = _caps[key];
            _caps[key] = (min, cap + delta);
        }

        /// <summary>逐项深拷（新 Dictionary，独立）。</summary>
        public CultivationState Clone()
        {
            return new CultivationState(
                PathId, RealmIndex, CultivationPoints,
                ChosenArtIds, ChosenSkillIds,
                new Dictionary<string, int>(Resources),
                new Dictionary<string, int>(Flags),
                new Dictionary<string, (int Min, int Cap)>(_caps),
                DaoHeart, InnerDemon, Comprehension);
        }

        private static int Clamp(int v, int min, int max)
        {
            if (v < min) return min;
            if (v > max) return max;
            return v;
        }
    }
}
