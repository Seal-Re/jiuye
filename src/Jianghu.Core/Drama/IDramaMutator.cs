using Jianghu.Events;
using Jianghu.Model;

namespace Jianghu.Drama
{
    /// <summary>
    /// 戏剧层**唯一写 seam**（drama-009，落「DomainEvent 单源」红线）。
    /// 戏剧效果只经此产出，由实现（drama-010 World）翻译为 Project + Chronicle.Append——
    /// 绝不旁路 mutate 属性/关系/恩怨。DramaDirector.Pump 注入此 seam 发事件 + 受控耦合写。
    /// </summary>
    public interface IDramaMutator
    {
        /// <summary>产出一个领域事件（实现侧 Project + Chronicle.Append）。</summary>
        void Emit(DomainEvent e);

        // —— drama-011 受控耦合写口（drama→decision 唯一受控通道，spec §0.2）——

        /// <summary>覆写角色 Goal 种类（BuildUp 阶段 → Advance，触发 RuleBrain 既有疯修权重）。Progress 置 0。</summary>
        void OverrideGoal(CharacterId who, GoalKind kind);

        /// <summary>还原角色原 Goal（弧收束，防永久卡复仇态）。</summary>
        void RestoreGoal(CharacterId who, Goal original);

        /// <summary>镜像调整复仇者→仇人 Relations（Hunting 阶段负向，触发 RuleBrain 既有 notFoe 项）。</summary>
        void MirrorRelation(CharacterId holder, CharacterId target, int delta);
    }
}
