using System.Collections.Generic;

namespace Jianghu.Drama
{
    /// <summary>
    /// storylet cooldown 作用域（drama-007，spec §2/§4）。本 story 仅 schema 字段；
    /// 实际计时在 DramaDirector.Pump（drama-009）。
    /// </summary>
    public enum CooldownScope { Global = 0, PerActor = 1, PerPair = 2, PerSect = 3 }

    /// <summary>
    /// 声明式 storylet（drama-007，spec §2）。**纯不可变 record、无可变态、单例可单测**。
    /// 统一四镜 BeatTemplate/Storylet 为单一 schema：整数权重 + 谓词门控（全 AND）+
    /// 声明式效果意图（由 director 翻译成 DomainEvent，drama-008）。
    /// ChronicleTemplate **仅渲染层**——绝不进整数数值路径（B 红线）。
    /// </summary>
    public sealed record StoryletSpec(
        int Id,                                  // 稳定整数键（cooldown/去重）
        ArcKind Arc,                             // 归属弧种类
        ArcStage Stage,                          // 弧内阶段（仅该阶段可选中）
        int BaseWeight,                          // 整数基础权重（选择器 max(1,·) 兜底）
        bool OncePerArc,                         // 一弧仅一次（计时在 drama-009）
        long CooldownTicks,                      // 冷却时长（计时在 drama-009）
        CooldownScope Scope,                     // 冷却作用域
        IReadOnlyList<Predicate> Preconditions,  // 全 AND 整数阈值门控
        IReadOnlyList<Effect> Effects,           // 声明事件意图（drama-008 翻译）
        string ChronicleTemplate);               // 仅渲染层，绝不进数值路径

    /// <summary>
    /// storylet 内容源接口（drama-007，spec §2 lens 2 裁决）。**核库只吃接口、保零 IO**；
    /// JSON 适配层预留于 CLI 层（net8.0）。
    /// </summary>
    public interface IStoryletSource
    {
        IReadOnlyList<StoryletSpec> All { get; }
    }

    /// <summary>
    /// C# 常量池 storylet 源（drama-007）。`static readonly` 编译期校验 + 零 IO + 确定性
    /// （避 GBK/CRLF 历史坑）。保持构造序（不重排——裁决序由 StoryletSelector 显式排序负责）。
    /// </summary>
    public sealed class CodeStoryletSource : IStoryletSource
    {
        private readonly List<StoryletSpec> _all;
        public CodeStoryletSource(IReadOnlyList<StoryletSpec> specs)
        {
            _all = new List<StoryletSpec>(specs);
        }
        public IReadOnlyList<StoryletSpec> All => _all;
    }
}
