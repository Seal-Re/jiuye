using System;
using System.Collections.Generic;

namespace Jianghu.Cultivation
{
    /// <summary>
    /// 道心增益来源（事件/动作 → 道心增量）。纯整数。
    /// </summary>
    public sealed record DaoHeartGain(
        string Source,    // 增益来源标识（如 "养气"、"教化"）
        int Amount        // 道心增量（整数，正=增道心）
    );

    /// <summary>
    /// 心魔来源（事件/动作 → 心魔增量）。纯整数。
    /// </summary>
    public sealed record InnerDemonSource(
        string Source,    // 心魔来源标识（如 "噬心反噬"、"杀业"）
        int Amount        // 心魔增量（整数，正=增心魔）
    );

    /// <summary>
    /// 单路道心定义（A3 §3 + A123 §A.2.3 扩展 21 路）。
    /// 数据驱动：加路=加数据行，不改引擎。
    /// </summary>
    public sealed record DaoHeartDef(
        string PathId,                           // 路线标识
        int InitMultiplier,                      // 道心初值乘子（default *2, soul/buddhist *3）
        IReadOnlyList<DaoHeartGain> GainSources, // 道心增益来源（≥3 条）
        IReadOnlyList<InnerDemonSource> DemonSources // 心魔来源（≥3 条）
    );
}
