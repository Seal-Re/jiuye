using Jianghu.Model;

namespace Jianghu.Drama
{
    // 戏剧引擎值类型骨架（drama-004，spec Step 1）。纯值类型，无逻辑、无可变共享态。
    // 全整数（B.2）。行为在 drama-005+（账本/弧/序列器）。

    /// <summary>恩怨主键（不可变）。</summary>
    public readonly record struct GrudgeId(long Value);

    /// <summary>复仇弧主键（不可变）。</summary>
    public readonly record struct ArcId(long Value);

    /// <summary>恩怨种类。序数 = 严重度（max 合并：Slaughter &gt; Maiming &gt; Insult）。</summary>
    public enum GrudgeKind { Insult = 0, Maiming = 1, Slaughter = 2 }

    /// <summary>恩怨成因。</summary>
    public enum GrudgeCause { Direct = 0, Inherited = 1, SectFeud = 2 }

    /// <summary>戏剧弧种类（v1 只复仇）。</summary>
    public enum ArcKind { Revenge = 0 }

    /// <summary>复仇弧阶段（5 态 + 2 终态，spec §1）。</summary>
    public enum ArcStage { Victimized = 0, BuildUp = 1, Hunting = 2, Showdown = 3, Resolved = 4, Abandoned = 5 }

    /// <summary>storylet 谓词变量（纯整数解析，drama-007 Resolve）。</summary>
    public enum DramaVar { Power = 0, Affinity = 1, GrudgeIntensity = 2, SameNode = 3, TargetAlive = 4 }

    /// <summary>整数比较算子。</summary>
    public enum CmpOp { Ge = 0, Le = 1, Eq = 2, Gt = 3, Lt = 4 }

    /// <summary>storylet 角色引用（相对弧的参与者）。</summary>
    public enum RoleRef { Holder = 0, Target = 1, Self = 2 }

    /// <summary>storylet 效果种类（director 翻译为 DomainEvent）。</summary>
    public enum EffectKind { AdjustRelation = 0, OverrideGoal = 1, FormGrudge = 2, EmitChronicle = 3 }

    /// <summary>
    /// 一笔有向恩怨。Intensity 钳制责任在 GrudgeLedger（drama-005），此处只定字段。
    /// 合并规则（drama-005）：同 (Holder,Target) 取 Kind=max / Intensity=max / Generation=min / OriginTick=首次。
    /// </summary>
    public sealed record Grudge(
        GrudgeId Id,
        CharacterId Holder,
        CharacterId Target,
        GrudgeKind Kind,
        int Intensity,
        long OriginTick,
        int Generation,
        GrudgeCause Cause,
        GrudgeId? InheritedFrom);

    /// <summary>
    /// 一条复仇弧实例。record（值相等 + 非破坏式 with 推进阶段）。
    /// 续跑敏感态——进 GrudgeLedger/DramaDirector 的 Clone 清单（drama-010）。
    /// </summary>
    public sealed record ArcInstance(
        ArcId Id,
        ArcKind Kind,
        CharacterId Avenger,
        CharacterId Target,
        ArcStage Stage,
        long NextWakeAt,
        int BuildUpBasePower,
        bool Completed);

    /// <summary>storylet 前置谓词（纯整数比较，全 AND）。</summary>
    public sealed record Predicate(RoleRef Subject, DramaVar Var, CmpOp Op, int Threshold);

    /// <summary>storylet 效果意图（声明式，director 翻译成 DomainEvent）。</summary>
    public sealed record Effect(EffectKind Kind, RoleRef From, RoleRef To, int Amount, int Tag);

    /// <summary>师承/血缘侧表（不侵入 Character/Persona record 字段顺序）。</summary>
    public sealed record DramaProfile(CharacterId Self, CharacterId? Master, CharacterId? Bloodline);
}
