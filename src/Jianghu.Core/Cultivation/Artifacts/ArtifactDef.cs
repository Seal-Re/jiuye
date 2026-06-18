using System.Collections.Generic;

namespace Jianghu.Cultivation.Artifacts
{
    /// <summary>法宝九品阶梯。对应修炼境界：凡器(炼气)→法器(筑基)→灵器(金丹)→宝器(元婴)
    /// →道器(化神)→灵宝(炼虚)→通天灵宝(合体)→玄天之宝(大乘)→先天/混沌至宝(渡劫/真仙)。</summary>
    public enum ArtifactGrade
    {
        /// <summary>凡器·炼气期·BasePower=10</summary>
        Mortal = 1,
        /// <summary>法器·筑基期·BasePower=30</summary>
        Dharma = 2,
        /// <summary>灵器·金丹期·BasePower=60</summary>
        Spirit = 3,
        /// <summary>宝器·元婴期·BasePower=100</summary>
        Treasure = 4,
        /// <summary>道器·化神期·BasePower=160</summary>
        DaoWeapon = 5,
        /// <summary>灵宝·炼虚期·BasePower=240</summary>
        NuminousTreasure = 6,
        /// <summary>通天灵宝·合体期·BasePower=340</summary>
        HeavenReaching = 7,
        /// <summary>玄天之宝·大乘期·BasePower=480</summary>
        ProfoundSky = 8,
        /// <summary>先天/混沌至宝·渡劫/真仙·BasePower=680</summary>
        Primordial = 9
    }

    /// <summary>品内四档浮动品质。下品(×0.8)/中品(×1.0)/上品(×1.2)/极品(×1.5)。</summary>
    public enum QualityTier
    {
        /// <summary>下品·BasePower×0.8</summary>
        Inferior = 1,
        /// <summary>中品·BasePower×1.0（基准）</summary>
        Common = 2,
        /// <summary>上品·BasePower×1.2</summary>
        Superior = 3,
        /// <summary>极品·BasePower×1.5</summary>
        Supreme = 4
    }

    /// <summary>法宝二十四形态。刃兵(剑/刀/枪/针)、重兵(印/锤/斧)、阵器(幡/阵盘/图卷)、
    /// 护宝(钟/塔/盾/莲台)、奇物(珠/葫芦/镜/鼎/扇/索/环)、符宝(符箓/灯)、乐器(琴箫)、鞭尺(鞭)。</summary>
    public enum ArtifactForm
    {
        // —— 刃兵 ——
        Sword,   // 剑·穿透/破防
        Blade,   // 刀·斩杀/流血
        Spear,   // 枪/戟·长距/突刺
        Needle,  // 针/钉·暗袭/点穴
        // —— 重兵 ——
        Seal,    // 印·镇压/眩晕
        Hammer,  // 锤/杵·粉碎/破甲
        Axe,     // 斧·破界/毁阵
        // —— 阵器 ——
        Banner,     // 幡/旗·领域/光环
        ArrayDisk,  // 阵盘·布阵/困敌
        Scroll,     // 图/卷·空间/困杀
        // —— 护宝 ——
        Bell,    // 钟·镇守/防魂
        Tower,   // 塔·镇压/保护
        Shield,  // 盾/甲·护体/反震
        Lotus,   // 莲台·净化/结界
        // —— 奇物 ——
        Orb,      // 珠·元素/克制
        Gourd,    // 葫芦·收容/炼化
        Mirror,   // 镜·反照/洞察
        Cauldron, // 鼎/炉·炼制/吞噬
        Fan,      // 扇·范围/吹飞
        Rope,     // 索/绳·束缚/擒拿
        Ring,     // 环/圈·套取/缴械
        // —— 符宝 ——
        Talisman, // 符箓·一次性爆发
        Lamp,     // 灯·照明/驱邪
        // —— 乐器 ——
        Instrument, // 琴箫·音攻/控场
        // —— 鞭尺 ——
        Whip      // 鞭/尺·中距/削甲
    }

    /// <summary>法宝七功能轴。攻(Attack)/防(Defense)/困(Trap)/夺(Snatch)/辅(Support)/遁(Escape)/愈(Heal)。</summary>
    public enum ArtifactFunction
    {
        /// <summary>攻·FlatPen/PenFromResource/AoePerTarget/CounterMul</summary>
        Attack,
        /// <summary>防·FlatDR/ReflectDamage/Evade</summary>
        Defense,
        /// <summary>困·Control/Dot</summary>
        Trap,
        /// <summary>夺·DrainResource/Special(luobao)</summary>
        Snatch,
        /// <summary>辅·AddResource/GrantPassive/AddTermWeightStep</summary>
        Support,
        /// <summary>遁·Evade/ScalarMul</summary>
        Escape,
        /// <summary>愈·AddResource(回资源)/Dot(驱毒)</summary>
        Heal
    }

    /// <summary>
    /// 单件法宝全量定义。
    /// 接入现有 EffectOp 模块系统与 SpecialModuleRegistry 唯一档 handler；
    /// 法宝不只是道具——数值底 + 配套功法解锁的战斗模块（承模块化效果系统 §6）。
    /// </summary>
    public sealed record ArtifactDef(
        /// <summary>唯一标识符，如 "art_sword_azure_dragon"</summary>
        string Id,
        /// <summary>显示名，如 "青索剑"</summary>
        string Name,
        /// <summary>二十四形态之一</summary>
        ArtifactForm Form,
        /// <summary>主功能轴</summary>
        ArtifactFunction PrimaryFunc,
        /// <summary>副功能轴（4-6品可选，7-9品+Unique常用），可空</summary>
        ArtifactFunction? SecondaryFunc,
        /// <summary>九品阶梯</summary>
        ArtifactGrade Grade,
        /// <summary>品内四档品质</summary>
        QualityTier Quality,
        /// <summary>物品阶位 0-9，从 Grade 映射</summary>
        int ItemTier,
        /// <summary>基础战力值（品阶×档位乘子）</summary>
        int BasePower,
        /// <summary>效果算子列表，经 ModuleResolver 结算</summary>
        IReadOnlyList<EffectOp> Effects,
        /// <summary>稀有度：Common 普通 / Rare 稀有 / Unique 唯一</summary>
        EffectRarity Rarity,
        /// <summary>预留：元素属性（五行/阴阳/风雷/时空），未来镶嵌/附魔补</summary>
        string? ElementHint,
        /// <summary>背景描述·flavor text，不参与结算</summary>
        string? FlavorText,
        /// <summary>获取来源提示·lore hint</summary>
        string? SourceHint);
}
