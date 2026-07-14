using Jianghu.Config;
using Jianghu.Stats;

namespace Jianghu.Cultivation
{
    /// <summary>
    /// cv-007（adr-0010 决策③）：派生抗性 R 的派生计算——防御漏斗第③层（抵抗层）的核心。
    ///
    /// **独立于 <see cref="DerivedProviders"/>**：后者是 power-registry 单一职责（9 个 IDerivedProvider 注册进
    /// DerivedRegistry，参与 EffectivePower 战力计算）；抗性 R 是**防御结算用**的派生属性，**禁进 EffectivePower**
    /// （B.5：抗性只作防御结算，不算战力）。故新建本静态类承载 <see cref="ResistanceOf"/>，与 power registry 解耦。
    ///
    /// R 的数据链**完全可追溯**（从 StatBlock 四维 + 功法标签派生，无凭空魔法数字）：
    /// - 物理抗性 ← 体质(Constitution) × <see cref="LimitsConfig.PhysResistPerConstitution"/>；HasBodyArt 功法标签 → +<see cref="LimitsConfig.BodyArtPhysResistBonus"/>
    /// - 属性/法术抗性 ← 识/悟性(Insight) × <see cref="LimitsConfig.ElemResistPerInsight"/>
    /// - 法宝 OnDefend 加成 → 留 TODO(cv-008) 接口（角色不持法宝实例，不臆造遍历）
    ///
    /// **内力(Internal)不参与常驻被动抗性**（adr-0010 用户裁定：留作后续主动护盾蓝条资源）。
    /// **daoHeart/innerDemon 不参与**（B.5：签名不含，编译期保证）。
    ///
    /// 纯整数、确定性、无 RNG（B.2）。R 派生源（StatBlock 四维 + 功法标签）在单场对决期间不变
    /// （无跨回合累积）→ duel-local 纯净，off 走 legacy SparAction 不入 DuelEngine 天然守 B.3。
    /// </summary>
    public static class ResistanceProviders
    {
        /// <summary>
        /// 派生防方对指定 <paramref name="damageType"/> 的抗性分值 R（用于 <see cref="CombatMath.ApplyResistance"/> 半衰减）。
        ///
        /// DamageType → 抗性维度映射（确定性规则，无 RNG）：
        /// - <see cref="DamageType.Normal"/> / <see cref="DamageType.Blunt"/> → 物理抗性（体质派生 + HasBodyArt 加成）
        /// - <see cref="DamageType.Elemental"/> → 属性/法术抗性（识/悟性派生）
        ///
        /// **B.5 守门**：参数列表**不含** daoHeart/innerDemon（<see cref="CultivationState"/> 本就有这些字段，但签名不传）。
        /// 抗性 R **禁进 EffectivePower**——本函数仅作防御结算用，不被 <see cref="PowerEngine"/> 调用。
        /// </summary>
        /// <param name="cultivation">防方修炼态（读 <see cref="CultivationState.ChosenArtIds"/> 判 HasBodyArt；daoHeart/innerDemon 字段不读）</param>
        /// <param name="stats">防方四维属性（<see cref="StatBlock.Get(StatKind)"/> 取 Constitution/Insight）</param>
        /// <param name="path">防方修炼路定义（读 <see cref="CultivationPathDef.ArtCategories"/> 判 HasBodyArt）</param>
        /// <param name="gate">战斗上下文门控位（预留 cv-008 SBC 用；当前 R 派生**不依赖**此参数——HasBodyArt 判定复用 <see cref="DuelEngine.HasBodyArt"/> helper，非 gate.HasFlag 语义）</param>
        /// <param name="damageType">攻击伤害类型（决定走物理抗还是属性抗）</param>
        /// <param name="limits">配置旋钮（显式传参，不在 CultivationState/StatBlock 内）</param>
        /// <returns>派生抗性分值 R（≥0；纯整数）</returns>
        public static int ResistanceOf(
            CultivationState cultivation, StatBlock stats,
            CultivationPathDef path, GateType gate,
            DamageType damageType, LimitsConfig limits)
        {
            // —— DamageType → 抗性维度映射（确定性，无 RNG）——
            // Normal/Blunt → 物理抗性（体质派生）；Elemental → 属性/法术抗性（识/悟性派生）。
            // StatBlock 无命名属性，经 StatKind 枚举索引：Force=0, Internal=1, Constitution=2, Insight=3。
            int baseR;
            if (damageType == DamageType.Elemental)
            {
                // 属性/法术抗性 ← 识/悟性(Insight) × ElemResistPerInsight（"神识御法"修仙设定）
                baseR = stats.Get(StatKind.Insight) * limits.ElemResistPerInsight;
                // TODO(cv-008): 对应门类功法标签 → +PathElemResistBonus（21 路功法标签数据 deferred，红线 A.8）
            }
            else
            {
                // 物理抗性 ← 体质(Constitution) × PhysResistPerConstitution（"横练护体"直觉）
                baseR = stats.Get(StatKind.Constitution) * limits.PhysResistPerConstitution;
                // HasBodyArt（已修横练/护体类功法）→ 抬物理抗性固定加成。
                // 复用 DuelEngine.HasBodyArt helper（遍历 path.ArtCategories Role=body/defense 且 ChosenArtIds 命中，
                // 语义="已修横练/护体"）。**不**用 gate.HasFlag(GateType.HasBodyArt)——那是静态能力门="有能力"，非"已修"。
                if (DuelEngine.HasBodyArt(path, cultivation))
                    baseR += limits.BodyArtPhysResistBonus;
            }

            // TODO(cv-008): ArtifactDef OnDefend 效果加 R（角色不持法宝实例列表，CultivationState 无 Artifacts 字段；
            // 法宝防御接入属 cv-004/cv-008 域。本 story 仅预留接口，不臆造遍历代码。）

            // 抗性非负（所有派生源均 ≥0，理论上不会负，防御性钳制）。
            return baseR < 0 ? 0 : baseR;
        }
    }
}
