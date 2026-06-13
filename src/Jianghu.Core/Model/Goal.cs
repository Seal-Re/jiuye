namespace Jianghu.Model
{
    public enum GoalKind { Advance, Wander } // 精进武学 / 游历访友
    public sealed record Goal(GoalKind Kind, int Progress);
}
