using System;

namespace Jianghu.Cultivation
{
    /// <summary>
    /// cv-004（adr-0008 决策⑧A/⑨.2）：防守帧钩子契约——Model→View 整数接口。
    ///
    /// 内核（Model）产出本钩子，表现层（View/Godot）读取后驱动 QTE 防守帧窗。
    /// **零引擎依赖**（不含 Godot.*/UnityEngine.*），纯数据 record，承 ADR-0004 Model/View 边界。
    ///
    /// - NPC vs NPC：钩子为 null（内核确定性结算，无帧窗需求）
    /// - Player vs NPC：钩子非 null（View 读 FrameWindow 决定 QTE 窗口帧数）
    ///
    /// 保底帧语义（ADr-0008 ⑧A）：即便战力差达 int.MaxValue，无必中标签的攻击必须输出 >0 的保底帧
    /// → 捍卫"理论可操作性"（无必中 = 理论可无限格挡）。
    /// </summary>
    public sealed record DefenseFrameHook(
        /// <summary>防守判定帧数（≥1；GuaranteeFrameCount 保底 或 计算帧数）。View 据此设 QTE 窗口。</summary>
        int FrameWindow,

        /// <summary>本回合是否触发溢出（p ≥ OverflowThreshold）。View 据此选"按键碎裂定身"特效。</summary>
        bool Overflowed,

        /// <summary>攻击伤害类型。View 据此选 QTE 类型：Normal→Block/Dodge 均可用；Blunt→仅 Dodge；Elemental→仅 Block。</summary>
        DamageType AttackDamageType,

        /// <summary>原始伤害（溢出时可为极大值）。View 据此决定"擦伤/普通/会心"质量档乘子。</summary>
        int RawDamage
    );
}
