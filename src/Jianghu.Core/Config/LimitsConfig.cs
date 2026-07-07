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
        // Viability 调平（2026-07-03 用户指令 Step 2）：600/1200→1200/2400。配合破境率提升，
        // 让典型实体有生之年走完更长成长线（寿命消耗与成长速度匹配）。整数（B.2）。
        public long LifespanMin { get; init; } = 1200; // 逻辑时间单位
        public long LifespanMax { get; init; } = 2400;
        public int PopulationLow { get; init; } = 5;    // 低于则涌现新人
        public int PopulationHigh { get; init; } = 30;  // 高于则抑制涌现

        // 行动（§8）
        public int TrainGainMin { get; init; } = 1;
        public int TrainGainMax { get; init; } = 3;
        public long ActionIntervalBase { get; init; } = 10;

        // 修炼（§6/§8；纯加，默认值不影响 v1.0 cultivation-off 轨迹）
        public int PowerCap { get; init; } = 1_000_000;     // PowerEngine.final 上钳
        public int SituationalP0Base { get; init; } = 400;  // 情境 adj clamp ±P0/4 基准（Task 1.6 用）

        // 控制经济（balance-007；DuelEngine 结算侧，duel-local，off 不调 DuelEngine → B.3 天然守）。
        // 治 turns≥2 硬控每回合重挂→永久锁死（stun-lock）。纯整数（B.2），从时间经济根治零博弈碾压，不碰 PE。
        public int ControlCooldown { get; init; } = 2;  // 挂控成功后隔 N 回合该控不可再挂（≥0；0=退化无冷却，向后兼容）
        public int ControlDRStep { get; init; } = 1;    // 同目标同类控制每次重复，TurnsRemaining 递减步长（≥0；0=退化无递减）

        // 削韧副轴（combat-variance cv-002；DuelEngine 结算侧，duel-local PoiseState，off 不调 DuelEngine → B.3 天然守）。
        // adr-0008 决策⑦步7：伤害结算后同步派生削韧累加防方韧性条；韧性≤0 触发硬直（复用 Control 管线注入 turns=1）+ DR。
        // 纯整数（B.2）。全默认值不影响 off（off 走 legacy SparAction，不入 DuelEngine）。
        public int PoiseMax { get; init; } = 300;              // 韧性条上限基准（≥1；硬直后重置至此值。默认 ≈ 3× 基础削韧使连击 ~3 回合破韧）
        public int PoiseDamageRatioPermille { get; init; } = 1000; // 有效伤害→基础削韧千分比（≥0；0=退化无基础削韧，仅算子削韧。1000=削韧==伤害）
        public int StaggerDurationTurns { get; init; } = 1;    // 硬直注入的 stagger 控制回合（≥1；1=打断语义，经 balance-008 StoredControlTurns 补偿实拒止 1 回合）
        public int StaggerDRStep { get; init; } = 1;           // 同方连续硬直，PoiseMax 重置量阶梯抬升的递减基（≥0；0=退化无 DR，防 stagger-lock 关闭）
        public int StaggerCooldown { get; init; } = 2;         // 硬直触发后隔 N 回合该方不可再硬直（≥0；0=退化无冷却）

        // 戏剧引擎 B（drama-006，GDD §7；纯加，off 全关时绝不消费 → B.3 逐字节守恒）。
        // 全 int，含 MaxArcWeightSum（int 范围内 → WeightedPicker (int)total 抽取安全）。
        public int GrudgeCap { get; init; } = 100;             // 恩怨强度上限 [0,Cap]（§3.1）
        public int GrudgeIgniteThreshold { get; init; } = 60;  // 点火阈值，须 ∈ [1,GrudgeCap]（强恩怨才点火）
        public int MaxConcurrentArcs { get; init; } = 3;       // 并发弧上限；**0 合法=戏剧 no-op 开关**（§7/AC-1）
        public int MaxArcsPerCharacter { get; init; } = 1;     // 每角色弧上限（防多弧抢 Goal §5）
        public int IgnitionCheckInterval { get; init; } = 20;  // 点火节流间隔（§3.4）
        public int ArcPairCooldown { get; init; } = 200;       // 对子冷却（同对不二次点火 §7）
        public int FirstStageDelay { get; init; } = 10;        // Victimized→BuildUp（§3.2）
        public int BuildUpDelay { get; init; } = 100;          // BuildUp 长延迟（疯修涨战力 §3.2）
        public int HuntingDelay { get; init; } = 30;           // Hunting 中延迟（§3.2）
        public int ShowdownDelay { get; init; } = 10;          // Showdown 短延迟（§3.2）
        public int GrowthNeeded { get; init; } = 50;           // Hunting 门控战力增量（§4）
        public int EscapeRatioPct { get; init; } = 50;         // 仇人脱逃比率，须 ∈ [1,100]（§7）
        public int DramaBudget { get; init; } = 4;             // 每 Pump 推进弧上限，须 ≥1（§7）
        public int MaxArcWeightSum { get; init; } = 1_000_000; // 点火权重和上界（溢出守门 §5），须 ≥1
        public int InheritDecayPct { get; init; } = 60;        // 继承强度衰减，须 ∈ [0,100]（保单调不增 §3.3）
        public int MaxGeneration { get; init; } = 3;           // 跨代恩怨封顶，须 ≥1（§7）
        public int RelationMirrorCap { get; init; } = 30;      // 镜像负 Relations 钳幅，须 ∈ [0,100]（§3.1）
        public int ShowdownTimeout { get; init; } = 100;       // Showdown 死锁兜底超时，须 ≥1（§3.2）
        public int StallTimeout { get; init; } = 300;          // BuildUp/Hunting 停滞兜底：停滞超此 tick → 强制推进（飞蛾扑火），须 ≥1

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

            // 控制经济（balance-007）：两旋钮 ≥0（0 合法=退化无限制，向后兼容）。
            if (ControlCooldown < 0) throw new InvalidOperationException("ControlCooldown<0（0 合法=无冷却）");
            if (ControlDRStep < 0) throw new InvalidOperationException("ControlDRStep<0（0 合法=无递减）");

            // 削韧副轴（combat-variance cv-002）：韧性上限 ≥1，其余 ≥1/≥0（0 合法=退化关闭对应机制）。
            if (PoiseMax < 1) throw new InvalidOperationException("PoiseMax<1（韧性上限须 ≥1）");
            if (PoiseDamageRatioPermille < 0) throw new InvalidOperationException("PoiseDamageRatioPermille<0（0 合法=无基础削韧）");
            if (StaggerDurationTurns < 1) throw new InvalidOperationException("StaggerDurationTurns<1（硬直须 ≥1 回合）");
            if (StaggerDRStep < 0) throw new InvalidOperationException("StaggerDRStep<0（0 合法=无 DR）");
            if (StaggerCooldown < 0) throw new InvalidOperationException("StaggerCooldown<0（0 合法=无冷却）");

            // 戏剧引擎 B 越界断言（drama-006，GDD §7）。独立断言，顺序不限。
            if (GrudgeCap < 1) throw new InvalidOperationException("GrudgeCap<1");
            if (GrudgeIgniteThreshold < 1 || GrudgeIgniteThreshold > GrudgeCap)
                throw new InvalidOperationException($"GrudgeIgniteThreshold 须 ∈ [1,GrudgeCap({GrudgeCap})]，实为 {GrudgeIgniteThreshold}");
            if (MaxConcurrentArcs < 0) throw new InvalidOperationException("MaxConcurrentArcs<0（0 合法=no-op）");
            if (MaxArcsPerCharacter < 1) throw new InvalidOperationException("MaxArcsPerCharacter<1");
            if (IgnitionCheckInterval < 1) throw new InvalidOperationException("IgnitionCheckInterval<1");
            if (ArcPairCooldown < 0) throw new InvalidOperationException("ArcPairCooldown<0");
            if (FirstStageDelay < 1) throw new InvalidOperationException("FirstStageDelay<1");
            if (BuildUpDelay < 1) throw new InvalidOperationException("BuildUpDelay<1");
            if (HuntingDelay < 1) throw new InvalidOperationException("HuntingDelay<1");
            if (ShowdownDelay < 1) throw new InvalidOperationException("ShowdownDelay<1");
            if (GrowthNeeded < 0) throw new InvalidOperationException("GrowthNeeded<0");
            if (EscapeRatioPct < 1 || EscapeRatioPct > 100)
                throw new InvalidOperationException($"EscapeRatioPct 须 ∈ [1,100]，实为 {EscapeRatioPct}");
            if (DramaBudget < 1) throw new InvalidOperationException("DramaBudget<1");
            if (MaxArcWeightSum < 1) throw new InvalidOperationException("MaxArcWeightSum<1");
            if (InheritDecayPct < 0 || InheritDecayPct > 100)
                throw new InvalidOperationException($"InheritDecayPct 须 ∈ [0,100]，实为 {InheritDecayPct}");
            if (MaxGeneration < 1) throw new InvalidOperationException("MaxGeneration<1");
            if (RelationMirrorCap < 0 || RelationMirrorCap > 100)
                throw new InvalidOperationException($"RelationMirrorCap 须 ∈ [0,100]，实为 {RelationMirrorCap}");
            if (ShowdownTimeout < 1) throw new InvalidOperationException("ShowdownTimeout<1");
            if (StallTimeout < 1) throw new InvalidOperationException("StallTimeout<1");
        }
    }
}
