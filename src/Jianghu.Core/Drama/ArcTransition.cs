namespace Jianghu.Drama
{
    /// <summary>复仇弧单次推进的结果分类（drama-007b）。</summary>
    public enum ArcResolution
    {
        Advanced = 0,  // 进入下一阶段
        Stalled = 1,   // 门控未满足，停留原阶段待下次唤醒重试
        Completed = 2, // Showdown 结算，弧收束（Resolved）
        Abandoned = 3, // 参与者亡，弧放弃
    }

    /// <summary>
    /// RevengeArc.TryAdvance 的纯转移结果（drama-007b）。Next 是非破坏式产出的新弧实例。
    /// AvengerPrevailed 仅 Resolution==Completed（Showdown 结算）时有意义。
    /// </summary>
    public sealed record ArcTransition(ArcInstance Next, ArcResolution Resolution, bool AvengerPrevailed);
}
