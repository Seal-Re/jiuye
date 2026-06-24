using System.Collections.Generic;
using Jianghu.Events;
using Jianghu.Model;

namespace Jianghu.Sim
{
    /// <summary>
    /// 管线阶段接口（integration story-005）。
    /// World.Tick 管线中每个阶段是一个 IPipelineStage 实现。
    /// 纯整数，确定性，off 模式可 skip。
    /// </summary>
    public interface IPipelineStage
    {
        /// <summary>阶段名（用于日志/快照对账）。</summary>
        string Name { get; }

        /// <summary>是否在 off/无 Cultivation 模式下激活。false → World.Tick skip。</summary>
        bool ActiveWhenOff { get; }

        /// <summary>
        /// 执行本阶段逻辑。返回本阶段产生的 DomainEvent 列表（可能为空）。
        /// </summary>
        /// <param name="world">世界引用（可读写）</param>
        /// <param name="actor">当前行动角色</param>
        /// <param name="tick">当前 tick 号</param>
        /// <returns>本阶段产生的事件</returns>
        IReadOnlyList<DomainEvent> Execute(World world, Character actor, long tick);
    }

    /// <summary>
    /// 管线阶段注册表（integration story-005）。
    /// 持全量 IPipelineStage，按注册顺序执行。
    /// 加阶段=加注册行，不改 World.Tick 核心循环。
    /// </summary>
    public sealed class PipelineStageRegistry
    {
        private readonly List<IPipelineStage> _stages = new List<IPipelineStage>();

        public void Register(IPipelineStage stage) => _stages.Add(stage);
        public IReadOnlyList<IPipelineStage> Stages => _stages;
        public int Count => _stages.Count;
    }
}
