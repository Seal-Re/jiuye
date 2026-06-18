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
        /// <summary>完整边表 v2（22边, combat-fullstruct story-002）。零 PathId, 环自洽, 全 CoefPct 钳 ±P0/4。</summary>
        public static readonly IReadOnlyList<SituationalEdge> Default = new[]
        {
            // ═══ element 元素相生克（四象环: 火克木/木克雷/雷克冰/冰克火）═══
            new SituationalEdge("element", "attacker.tag:fire & defender.tag:wood", +15),
            new SituationalEdge("element", "attacker.tag:wood & defender.tag:thunder", +15),
            new SituationalEdge("element", "attacker.tag:thunder & defender.tag:ice", +15),
            new SituationalEdge("element", "attacker.tag:ice & defender.tag:fire", +15),

            // ═══ anti_evil 灭阴/克邪（正道克阴邪，Bible §6.4 counterWheel 正义轴）═══
            new SituationalEdge("anti_evil", "attacker.tag:anti_evil & defender.tag:evil", +20),
            new SituationalEdge("anti_evil", "attacker.tag:righteous & defender.tag:evil", +15),
            new SituationalEdge("anti_evil", "attacker.tag:thunder & defender.tag:evil", +15),
            new SituationalEdge("anti_evil", "attacker.tag:righteous & defender.tag:ghost", +18),
            // 反制: 阴邪攻正道受罚
            new SituationalEdge("anti_evil", "attacker.tag:evil & defender.tag:righteous", -10),
            new SituationalEdge("anti_evil", "attacker.tag:ghost & defender.tag:anti_evil", -15),

            // ═══ range 远近/形态克制 ═══
            new SituationalEdge("range", "attacker.tag:ranged & defender.tag:melee_brute", +20),
            new SituationalEdge("range", "attacker.tag:ranged & defender.tag:melee", +5),
            // 反制: 体修蛮力贴身克远程
            new SituationalEdge("range", "attacker.tag:brute & defender.tag:ranged", +8),

            // ═══ spirit/form 精神/形态轴 ═══
            new SituationalEdge("form", "defender.tag:undead_construct & attacker.axis:spirit_attack", -100),
            new SituationalEdge("spirit", "attacker.tag:spirit_attack & defender.tag:body", +12),
            // 反制: 寄生/蛊虫克肉身
            new SituationalEdge("form", "attacker.tag:parasite & defender.tag:body", +8),

            // ═══ economic 经济维（器修落宝克artifact/economic依赖）═══
            new SituationalEdge("economic", "attacker.tag:artifact & defender.tag:economic", +12),
            new SituationalEdge("economic", "attacker.tag:economic & defender.tag:artifact", +12),

            // ═══ control/trap 控场轴 ═══
            new SituationalEdge("control", "attacker.tag:control & defender.tag:brute", +10),
            new SituationalEdge("control", "attacker.tag:control & defender.tag:high_burst", +8),

            // ═══ time/terrain 环境轴 ═══
            new SituationalEdge("time", "env:is_night=1 & attacker.tag:ghost", +10),
            new SituationalEdge("terrain", "env:terrain=mountain & attacker.tag:body & defender.tag:ranged", +8),
        };
    }
}
