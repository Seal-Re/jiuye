using System.Collections.Generic;

namespace Jianghu.Cultivation
{
    /// <summary>
    /// 全局软情境边表（spec §8 / Bible §6.4）。**绝对零 PathId**：边只认战斗轴 + tag/环境谓词。
    /// A.0 最小样例集（元素相生克 / 远程克近战 brute / 死物免精神 / 昼夜），Phase 4.5 随代表路补全。
    /// 所有 CoefPct 受 SituationalResolver clamp ±P0/4，情境只修正不翻盘。
    /// </summary>
    public static class SituationalEdges
    {
        /// <summary>A.0 默认边表（SparAction 接此构造 SituationalResolver）。</summary>
        public static readonly IReadOnlyList<SituationalEdge> Default = new[]
        {
            // —— element：元素相生克（火克冰 / 冰克雷 / 雷克火，三元环样例）——
            new SituationalEdge("element", "attacker.tag:fire & defender.tag:ice", +15),
            new SituationalEdge("element", "attacker.tag:ice & defender.tag:thunder", +15),
            new SituationalEdge("element", "attacker.tag:thunder & defender.tag:fire", +15),

            // —— range：远程克近战 brute（攻方远程 & 守方近战蛮力 → 守方吃亏=攻方增益）——
            new SituationalEdge("range", "attacker.tag:ranged & defender.tag:melee_brute", +20),

            // —— form：死物免精神（守方死物构装 & 攻方走精神攻击轴 → 几近无效）——
            new SituationalEdge("form", "defender.tag:undead_construct & attacker.axis:spirit_attack", -100),

            // —— time：昼夜（夜间 & 攻方阴属/鬼 → 增益；A.0 无环境源时此边不命中，Phase 4.5 接 env）——
            new SituationalEdge("time", "env:is_night=1 & attacker.tag:ghost", +10),
        };
    }
}
