using System.Collections.Generic;
using Jianghu.Random;

namespace Jianghu.Cultivation
{
    /// <summary>
    /// 生成期定路 + 选功法战技（spec §10），纯整数确定性，消费 cultRng（生成期一次性）。
    /// 流程：筛 <see cref="EntryGateDef.CanEnter"/> 的路 → 确定性加权抽 PathId →
    /// 各 ArtCategory 按 PickMin/Max（tier 闸）选功法 + N 战技 →
    /// <see cref="CultivationState.NewForPath"/> 初始化 + 填 ChosenArtIds/ChosenSkillIds。
    /// 无可入路 → 散修（PathId/State 皆 null）。
    /// </summary>
    public static class PathAssigner
    {
        /// <summary>定路结果：散修时 PathId/State 皆 null。</summary>
        public readonly struct Result
        {
            public Result(string? pathId, CultivationState? state) { PathId = pathId; State = state; }
            public string? PathId { get; }
            public CultivationState? State { get; }
        }

        /// <summary>
        /// 按 tags 定路 + 选功法战技。同 (种子, tags, registry) → 同 PathId + 同 Chosen（确定性）。
        /// cultRng 仅生成期消费：①抽 PathId ②各类目抽功法 ③抽战技。
        /// </summary>
        public static Result Assign(IReadOnlyList<string> tags, PathRegistry registry, IRandom cultRng)
        {
            // —— 筛可入路（EntryGate 纯查询，不消费随机）——
            var eligible = new List<CultivationPathDef>();
            foreach (var p in registry.All)
                if (p.EntryGate.CanEnter(tags))
                    eligible.Add(p);

            if (eligible.Count == 0)
                return new Result(null, null); // 散修

            // —— 确定性加权抽 PathId（A.0 注册表无 per-path 权重 → 等权 uniform，消费 cultRng）——
            var def = eligible[cultRng.NextInt(eligible.Count)];

            // —— 各 ArtCategory 按 PickMin/Max（tier 闸）选功法（realm=0）——
            // tier 闸（HOW，见报告偏差）：A.0 schema 无 realmTierCap 表，realm=0 处用本路曲线
            //   UnifiedTierOf[0] 为唯一 realm→tier 信号，闸 = art.Tier <= UnifiedTierOf[0] + 1。
            int realm = 0;
            int tierCap = def.Curve.UnifiedTierOf[realm] + 1;

            var chosenArtIds = new List<string>();
            foreach (var cat in def.ArtCategories)
            {
                // 闸内候选（确定性顺序 = 类目内声明序）。
                var pool = new List<string>();
                foreach (var art in cat.Arts)
                    if (art.Tier <= tierCap)
                        pool.Add(art.Id);

                int n = PickCount(cat.PickMin, cat.PickMax, pool.Count, cultRng);
                PickInto(pool, n, cultRng, chosenArtIds);
            }

            // —— 选 N 战技（SelectionRule [SkillPickMin, SkillPickMax]，tier 闸）——
            var skillPool = new List<string>();
            foreach (var sk in def.CombatSkills)
                if (sk.Tier <= tierCap)
                    skillPool.Add(sk.Id);

            int skillN = PickCount(def.Selection.SkillPickMin, def.Selection.SkillPickMax, skillPool.Count, cultRng);
            var chosenSkillIds = new List<string>();
            PickInto(skillPool, skillN, cultRng, chosenSkillIds);

            var state = CultivationState.NewForPath(def.PathId, def.Resources, chosenArtIds, chosenSkillIds);
            return new Result(def.PathId, state);
        }

        /// <summary>确定性定数：在 [min, max] 内经 cultRng 取一数，再钳到 [0, available]（候选不足则取全部）。</summary>
        private static int PickCount(int min, int max, int available, IRandom cultRng)
        {
            if (max < min) max = min;
            int n = cultRng.NextInclusive(min, max);
            if (n > available) n = available;
            if (n < 0) n = 0;
            return n;
        }

        /// <summary>从 pool 经 cultRng 无放回抽 n 项（部分 Fisher-Yates），按抽取序追加进 dst。pool 就地打乱（局部副本调用方负责）。</summary>
        private static void PickInto(List<string> pool, int n, IRandom cultRng, List<string> dst)
        {
            for (int i = 0; i < n && pool.Count - i > 0; i++)
            {
                int remaining = pool.Count - i;
                int j = i + cultRng.NextInt(remaining);
                // swap pool[i] <-> pool[j]
                var tmp = pool[i]; pool[i] = pool[j]; pool[j] = tmp;
                dst.Add(pool[i]);
            }
        }
    }
}
