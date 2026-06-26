using Jianghu.Events;

namespace Jianghu.Drama
{
    /// <summary>
    /// 戏剧层**唯一写 seam**（drama-009，落「DomainEvent 单源」红线）。
    /// 戏剧效果只经 Emit 产出 DomainEvent，由实现（drama-010 World）翻译为 Project + Chronicle.Append——
    /// 绝不旁路 mutate 属性/关系/恩怨。DramaDirector.Pump（drama-009b）注入此 seam 发事件。
    ///
    /// 注：Goal 覆写 / 镜像 Relations 等受控耦合写操作延 drama-011（届时扩此接口或并入 World 实现），
    /// 本接口骨架仅立事件汇。
    /// </summary>
    public interface IDramaMutator
    {
        /// <summary>产出一个领域事件（实现侧 Project + Chronicle.Append）。</summary>
        void Emit(DomainEvent e);
    }
}
