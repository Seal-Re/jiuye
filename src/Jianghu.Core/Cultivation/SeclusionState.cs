using System;
using Jianghu.Model;
using Jianghu.Random;

namespace Jianghu.Cultivation
{
    /// <summary>
    /// 闭关 DES 单点唤醒模型（A3-FINAL §2, story-009）。
    /// 纯整数，确定性，off 模式 skip。
    /// </summary>
    public static class SeclusionState
    {
        // Flag keys
        public const string FLAG_SECLUDED = "secluded";
        public const string FLAG_DISTURB = "seclusionDisturb";
        public const string FLAG_WORK_UNITS = "seclusionWorkUnits";
        public const string FLAG_WAKE_AT = "seclusionWakeAt";
        public const string FLAG_STREAK = "seclusionStreak";

        /// <summary>被打扰强制出关阈值（G5 defer，当前仅累加不强制）。</summary>
        public const int DISTURB_FORCE_EXIT = 3;

        /// <summary>
        /// 进入闭关：设置标志位、NextActAt 远未来。
        /// </summary>
        /// <param name="st">角色修炼状态</param>
        /// <param name="character">角色</param>
        /// <param name="now">当前 tick</param>
        /// <param name="duration">闭关时长（tick 数）</param>
        /// <param name="workUnits">投入的 WorkUnits</param>
        public static void Enter(CultivationState st, Character character, long now, long duration, int workUnits)
        {
            st.Flags[FLAG_SECLUDED] = 1;
            st.Flags[FLAG_DISTURB] = 0;
            st.Flags[FLAG_WORK_UNITS] = workUnits;
            st.Flags[FLAG_WAKE_AT] = (int)(now + duration);

            int streak = st.Flags.TryGetValue(FLAG_STREAK, out var s) ? s : 0;
            st.Flags[FLAG_STREAK] = streak + 1;

            // Set NextActAt far into the future → Scheduler won't PopMin during seclusion
            character.NextActAt = now + duration;
        }

        /// <summary>
        /// 出关：读取冻结参数、清除标志位、计算收益（由 story-010 扩展）。
        /// </summary>
        /// <returns>闭关期间投入的 WorkUnits（调用方据此计算收益）</returns>
        public static int Exit(CultivationState st, Character character)
        {
            int workUnits = st.Flags.TryGetValue(FLAG_WORK_UNITS, out var w) ? w : 0;
            int disturb = st.Flags.TryGetValue(FLAG_DISTURB, out var d) ? d : 0;

            // 被打扰：WorkUnits 折半
            if (disturb >= DISTURB_FORCE_EXIT)
                workUnits /= 2;

            // Clear seclusion flags
            st.Flags.Remove(FLAG_SECLUDED);
            st.Flags.Remove(FLAG_DISTURB);
            st.Flags.Remove(FLAG_WORK_UNITS);
            st.Flags.Remove(FLAG_WAKE_AT);
            // Streak preserved (decays in non-seclusion ticks)

            return workUnits;
        }

        /// <summary>是否正在闭关。</summary>
        public static bool IsSecluded(CultivationState st)
            => st.Flags.TryGetValue(FLAG_SECLUDED, out var v) && v != 0;

        /// <summary>闭关期间 spar 动作 no-op。</summary>
        public static bool CanSpar(CultivationState st) => !IsSecluded(st);

        /// <summary>Discourage: 增加 Disturb 计数（其他角色互动触发）。</summary>
        public static void Disturb(CultivationState st)
        {
            if (!IsSecluded(st)) return;
            int current = st.Flags.TryGetValue(FLAG_DISTURB, out var d) ? d : 0;
            st.Flags[FLAG_DISTURB] = Math.Min(current + 1, DISTURB_FORCE_EXIT + 1);
        }

        /// <summary>闭关不可在 Breakthrough 阶段进入。</summary>
        public static bool CanEnter(CultivationState st)
        {
            if (IsSecluded(st)) return false;
            int phase = st.Flags.TryGetValue("cultPhase", out var p) ? p : -1;
            return phase != 6; // Breakthrough=6
        }

        /// <summary>闭关闭锁：突破中不可闭关。</summary>
        public static bool IsLockedOut(CultivationState st)
        {
            int streak = st.Flags.TryGetValue(FLAG_STREAK, out var s) ? s : 0;
            return streak >= 5; // Lockout after 5 consecutive seclusions (story-011)
        }
    }
}
