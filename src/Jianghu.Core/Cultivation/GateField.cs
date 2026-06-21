namespace Jianghu.Cultivation
{
    /// <summary>
    /// 功法门控类型（story fullstruct-006）。角色能否使用某种操作（如闪避/护体真气/剑意突破），
    /// 取决于是否修了对应功法。门控在 <see cref="ModuleResolver"/> 中作用于 EffectOp 级别：
    /// gate 不满足 → 操作跳过（dmg 不变），不抛异常。
    ///
    /// <para>≥6 种门控类型（AC 6.1）：HasMovementArt / HasBodyArt / HasSwordIntent / HasFormation / HasAlchemy / HasArtifactArts。</para>
    ///
    /// <para>门控判定在 <see cref="CombatContext.CheckGate"/> 中实现：
    /// role-based gate（movement/swordwill/body/core_forge 等）直接查 ArtCategoryDef.Role；
    /// resource-based gate（formation/alchemy）据 path 专属资源判定。</para>
    /// </summary>
    [System.Flags]
    public enum GateType
    {
        /// <summary>无门控（默认值，始终通过）。</summary>
        None = 0,

        /// <summary>身法/轻功类功法：ArtCategoryDef.Role == "movement"。</summary>
        HasMovementArt = 1 << 0,

        /// <summary>横练/护体类功法：ArtCategoryDef.Role == "body" 或 path 含体修资源(qixue/henglian)。</summary>
        HasBodyArt = 1 << 1,

        /// <summary>剑意/兵意类功法：ArtCategoryDef.Role == "swordwill"。</summary>
        HasSwordIntent = 1 << 2,

        /// <summary>阵图/布阵类功法：path 含阵修资源(stones/setupProgress/compute) 且角色已选功法。</summary>
        HasFormation = 1 << 3,

        /// <summary>丹道/炼丹类功法：path 含丹修资源(flameTier/recipeCount/pillStock) 且角色已选功法。</summary>
        HasAlchemy = 1 << 4,

        /// <summary>法宝/炼器/御器类功法：ArtCategoryDef.Role in {"core_forge","channel_mind","named_artifacts"}。</summary>
        HasArtifactArts = 1 << 5,
    }
}
