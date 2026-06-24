using System;

namespace Jianghu.Cultivation
{
    /// <summary>
    /// 佛修破戒修正（A3-FINAL §3.1, story-003）。
    /// 破戒时 daoHeart 折半非归零，仅 innerDemon≥95 触发 Fallen。
    /// </summary>
    public static class BuddhistVow
    {
        /// <summary>破戒 innerDemon 增量（A3 §3.1 默认值）。</summary>
        public const int VOW_BREAK_DEMON = 5;

        /// <summary>破戒 lethal 阈值——仅 innerDemon≥95 触发 Fallen。</summary>
        public const int VOW_LETHAL = 95;

        /// <summary>
        /// 破戒修正：daoHeart = max(current/2, 1)，innerDemon + delta。
        /// 返回 (newDaoHeart, actualDemonGain)。
        /// </summary>
        public static (int NewDaoHeart, int DemonGain) ApplyVowBreak(
            int currentDaoHeart, int currentInnerDemon, int demonDelta = VOW_BREAK_DEMON)
        {
            int newDaoHeart = Math.Max(currentDaoHeart / 2, 1);
            int newInnerDemon = Math.Min(currentInnerDemon + demonDelta, 100);
            return (newDaoHeart, newInnerDemon - currentInnerDemon);
        }

        /// <summary>破戒是否触发 Fallen（仅 innerDemon≥LETHAL）。</summary>
        public static bool TriggersFallen(int innerDemon) => innerDemon >= VOW_LETHAL;

        /// <summary>破戒不直接触发 Fallen：仅 innerDemon 达到 lethal 阈值才触发。</summary>
        public static bool ShouldFall(int innerDemonAfterVowBreak) => TriggersFallen(innerDemonAfterVowBreak);
    }
}
