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
            // —— element：元素相生克（canon counterWheel 四象环：火克木 / 木克雷 / 雷克冰 / 冰克火）。
            //    环自洽：无任一反向边（X 克 Y ⇒ 不存在 Y 克 X）。零 PathId，系数统一 +15（攻方增益）。——
            new SituationalEdge("element", "attacker.tag:fire & defender.tag:wood", +15),
            new SituationalEdge("element", "attacker.tag:wood & defender.tag:thunder", +15),
            new SituationalEdge("element", "attacker.tag:thunder & defender.tag:ice", +15),
            new SituationalEdge("element", "attacker.tag:ice & defender.tag:fire", +15),

            // —— range：远程克近战 brute（攻方远程 & 守方近战蛮力 → 守方吃亏=攻方增益）——
            new SituationalEdge("range", "attacker.tag:ranged & defender.tag:melee_brute", +20),

            // —— distance：放风筝（攻方远程 & 守方近战 → 远程增益，Bible §6.4「放风筝远程 +5」）。
            //    剑修近战 melee 脆皮被放风筝即吃此亏（零 PathId，只认 melee tag）。——
            new SituationalEdge("distance", "attacker.tag:ranged & defender.tag:melee", +5),

            // —— form：死物免精神（守方死物构装 & 攻方走精神攻击轴 → 几近无效）——
            new SituationalEdge("form", "defender.tag:undead_construct & attacker.axis:spirit_attack", -100),

            // —— spirit：魂力绕物防（鬼修代表路所涉，深度设计「spirit 维攻防绕物防,弃肉身」）。
            //    攻方走魂力 spirit_attack tag & 守方靠肉身 body 横练 → 物理罩门挡不住魂念,攻方增益。
            //    零 PathId（只认 spirit_attack/body tag），系数照深度设计「绕物防」语义取正 adj。——
            new SituationalEdge("spirit", "attacker.tag:spirit_attack & defender.tag:body", +12),

            // —— time：昼夜（夜间 & 攻方阴属/鬼 → 增益；Bible §6.4 daynight 轴；env:is_night 由战斗上下文供，
            //    A.0 SparAction env 暂空此边不在切磋命中，直接喂 env 的 SituationalResolver 测试可验，Phase 4.5 接 SparAction env）——
            new SituationalEdge("time", "env:is_night=1 & attacker.tag:ghost", +10),
        };
    }
}
