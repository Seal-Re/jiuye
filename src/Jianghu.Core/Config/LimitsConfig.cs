using System;

namespace Jianghu.Config
{
    public sealed record LimitsConfig
    {
        // 属性（§5）
        public int StatCount { get; init; } = 4;
        public int StatSum { get; init; } = 80;
        public int StatCap { get; init; } = 30;
        public int StatMin { get; init; } = 5;
        public int Concentration { get; init; } = 6;   // 偏中庸集中度（越大越向均值聚集）

        // 生命周期（§7.4）
        public long LifespanMin { get; init; } = 600;  // 逻辑时间单位
        public long LifespanMax { get; init; } = 1200;
        public int PopulationLow { get; init; } = 5;    // 低于则涌现新人
        public int PopulationHigh { get; init; } = 30;  // 高于则抑制涌现

        // 行动（§8）
        public int TrainGainMin { get; init; } = 1;
        public int TrainGainMax { get; init; } = 3;
        public long ActionIntervalBase { get; init; } = 10;

        public static LimitsConfig Default { get; } = new();

        /// <summary>加载期可行域非空断言（§5.3）。</summary>
        public void Validate()
        {
            if (StatCount <= 0) throw new InvalidOperationException("StatCount<=0");
            if ((long)StatCap * StatCount < StatSum)
                throw new InvalidOperationException($"可行域空: cap*count({StatCap}*{StatCount}) < sum({StatSum})");
            if ((long)StatMin * StatCount > StatSum)
                throw new InvalidOperationException($"可行域空: min*count({StatMin}*{StatCount}) > sum({StatSum})");
            if (StatMin < 0 || StatMin > StatCap) throw new InvalidOperationException("min 越界 [0,cap]");
            if (PopulationLow > PopulationHigh) throw new InvalidOperationException("人口带非法");
            if (LifespanMin > LifespanMax) throw new InvalidOperationException("寿命带非法");
        }
    }
}
