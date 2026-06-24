using System;
using System.Collections.Generic;
using Jianghu.Random;

namespace Jianghu.Cultivation
{
    /// <summary>
    /// 奇遇执行器（A3-FINAL §4, story-013）。
    /// Roam 模式下骰 encounter → 选题 → NPC 贪心选 option → 结算。
    /// </summary>
    public static class StoryletExecutor
    {
        /// <summary>基础遭遇概率（每 tick，Roam 模式 ×3，见 DailyMode）。</summary>
        public const int BASE_ENCOUNTER_RATE = 10; // 10%

        /// <summary>ActorMinGap: 同一角色两次奇遇最小间隔（tick）。</summary>
        public const int ACTOR_MIN_GAP = 30;

        /// <summary>Flag 键名。</summary>
        public const string FLAG_LAST_ENCOUNTER = "lastEncounterTick";

        /// <summary>Category cap: 每 100 tick 同类奇遇最多触 4 次（story-014）。</summary>
        public const int CAT_CAP_PER_100 = 4;

        /// <summary>
        /// 在当前 tick 尝试触发奇遇。
        /// </summary>
        /// <param name="st">角色修炼状态</param>
        /// <param name="tick">当前 tick</param>
        /// <param name="encounterExposure">遭遇曝光乘子（Roam=3, 其他=0/1）</param>
        /// <param name="registry">奇遇注册表</param>
        /// <param name="saliences">当前各奇遇的 salience（可读写，用于 decay）</param>
        /// <param name="rng">确定性 RNG</param>
        /// <returns>触发的奇遇+选中选项，或 null</returns>
        public static (StoryletDef Storylet, StoryletOption Option)? TryTrigger(
            CultivationState st, long tick, int encounterExposure,
            StoryletRegistry registry, Dictionary<string, int> saliences, IRandom rng)
        {
            // ActorMinGap check
            int lastEnc = st.Flags.TryGetValue(FLAG_LAST_ENCOUNTER, out var le) ? le : -ACTOR_MIN_GAP;
            if (tick - lastEnc < ACTOR_MIN_GAP) return null;

            // Encounter roll: baseRate * exposure (percentage, rolled vs 0-99)
            int rate = BASE_ENCOUNTER_RATE * encounterExposure;
            if (rate <= 0) return null;
            if (rng.NextInt(100) >= rate) return null;

            // Eligible storylets
            var eligible = registry.Eligible(st.RealmIndex);
            if (eligible.Count == 0) return null;

            // Weighted selection by salience
            int totalSal = 0;
            foreach (var s in eligible)
                totalSal += saliences.TryGetValue(s.Id, out var sal) ? Math.Max(sal, 1) : s.Salience;

            int roll = rng.NextInt(totalSal);
            StoryletDef? selected = null;
            foreach (var s in eligible)
            {
                int w = saliences.TryGetValue(s.Id, out var sal) ? Math.Max(sal, 1) : s.Salience;
                roll -= w;
                if (roll < 0) { selected = s; break; }
            }
            selected ??= eligible[0];

            // NPC greedy option selection
            var option = SelectOption(selected, st);

            // Update tracking
            st.Flags[FLAG_LAST_ENCOUNTER] = (int)tick;

            // Salience decay: triggered storylet halved
            if (saliences.TryGetValue(selected.Id, out var cur))
                saliences[selected.Id] = Math.Max(cur * 2 / 3, 1);
            else
                saliences[selected.Id] = Math.Max(selected.Salience * 2 / 3, 1);

            return (selected, option);
        }

        private static StoryletOption SelectOption(StoryletDef storylet, CultivationState st)
        {
            int bestScore = int.MinValue;
            StoryletOption best = storylet.Options[0];

            foreach (var opt in storylet.Options)
            {
                int score = 0;
                if (opt.DaoHeartDelta.HasValue) score += opt.DaoHeartDelta.Value * 3;
                if (opt.InnerDemonDelta.HasValue) score -= opt.InnerDemonDelta.Value * 3; // risk
                if (opt.ProgressDelta.HasValue) score += opt.ProgressDelta.Value;
                if (opt.RelationDelta.HasValue) score += opt.RelationDelta.Value * 2;

                // Bias: high innerDemon → prefer innerDemon-reducing options
                if (st.InnerDemon >= 60 && opt.InnerDemonDelta < 0) score += 20;

                if (score > bestScore) { bestScore = score; best = opt; }
            }

            return best;
        }
    }
}
