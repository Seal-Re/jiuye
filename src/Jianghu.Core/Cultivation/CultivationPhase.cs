using System;
using System.Collections.Generic;
using Jianghu.Model;
using Jianghu.Random;

namespace Jianghu.Cultivation
{
    /// <summary>
    /// 修炼全流程 10 态枚举（§A1.1 骨架）。存 <c>CultivationState.Flags["cultPhase"]</c>，
    /// 随 Flags dict 自动深拷——零新 Clone 字段（A.0 策略）。
    /// 0=Mortal 1=QiInduction 2=MinorAccumulate 3=MinorBreakthrough 4=MajorConsummate
    /// 5=Bottleneck 6=Breakthrough 7=Setback 8=Deviation 9=Fallen
    /// </summary>
    public enum CultivationPhase
    {
        /// <summary>凡人：尚未引气入体。rootQuality==0 者永困此态。</summary>
        Mortal = 0,
        /// <summary>引气入体：灵根激活中，需 InductRoll≥T_INDUCT 方可 MinorAccumulate。</summary>
        QiInduction = 1,
        /// <summary>小境界积累：progress 累积至 MinorThr 即触发 MinorBreakthrough。</summary>
        MinorAccumulate = 2,
        /// <summary>小境界突破：掷 MinorRoll 判定升 sub 或暂滞。</summary>
        MinorBreakthrough = 3,
        /// <summary>大圆满·瓶颈前：Foundation≥T_CONSUM_FND 方可入 Bottleneck。</summary>
        MajorConsummate = 4,
        /// <summary>瓶颈滞留：需破障四法积 BreakAid 达 Gate 方可 Breakthrough。</summary>
        Bottleneck = 5,
        /// <summary>渡劫裁决：TribScore≥TribGate→升境/飞升；未达→Setback/Fallen。</summary>
        Breakthrough = 6,
        /// <summary>渡劫受挫·可恢复：tribDebt 清且 Foundation 回升即 MinorAccumulate。</summary>
        Setback = 7,
        /// <summary>走火入魔：innerDemon 超标触发，需 PurgeRoll 自救。</summary>
        Deviation = 8,
        /// <summary>陨落·终态：不可逆，复用 v1.0 CharacterDied→Deceased。</summary>
        Fallen = 9
    }

    /// <summary>
    /// CultivationPhase 状态机转移触发器枚举。
    /// </summary>
    public enum PhaseTrigger
    {
        /// <summary>每 Tick 修炼推进（progress += gain）</summary>
        Cultivate,
        /// <summary>小境界突破判定</summary>
        MinorBreak,
        /// <summary>大圆满进入瓶颈</summary>
        MajorConsummateCheck,
        /// <summary>破障尝试（瓶颈→渡劫）</summary>
        BreakAttempt,
        /// <summary>渡劫裁决</summary>
        TribulationVerdict,
        /// <summary>走火判定（innerDemon 阈值）</summary>
        DeviateCheck,
        /// <summary>走火自救（PurgeRoll）</summary>
        PurgeAttempt,
        /// <summary>渡劫受挫恢复</summary>
        RecoverCheck,
        /// <summary>寿元耗尽</summary>
        LifespanExhausted
    }

    /// <summary>
    /// 转移结果：目标 phase + 是否为有效转移 + 副作用描述。
    /// </summary>
    public readonly struct PhaseTransition
    {
        public readonly CultivationPhase Target;
        public readonly bool IsValid;
        public readonly string? EventKey;

        public PhaseTransition(CultivationPhase target, bool isValid, string? eventKey = null)
        { Target = target; IsValid = isValid; EventKey = eventKey; }

        public static PhaseTransition Valid(CultivationPhase target, string? eventKey = null)
            => new PhaseTransition(target, true, eventKey);
        public static PhaseTransition Invalid()
            => new PhaseTransition(default, false, null);
    }

    /// <summary>
    /// CultivationPhase 状态机（§A1.1 骨架）。22 条整数守卫转移，存 Flags["cultPhase"]。
    /// 本 story (001) 落骨架 + 主转移——劫/破障/走火详细结算见后续 story。
    /// </summary>
    public static class CultivationPhaseMachine
    {
        // —— 整数守卫阈值（硬化常量，调参走 Config/LimitsConfig 扩展） ——
        public const int T_INDUCT_INS = 10;       // 引气入体最低悟性
        public const int T_INDUCT_ROLL = 12;      // 引气成功掷点（d20≥12）
        public const int INIT_FND = 30;           // 引气后初始 Foundation
        public const int T_CONSUM_FND = 70;       // 大圆满进瓶颈最低 Foundation
        public const int T_DEVIATE = 60;          // 走火入魔 innerDemon 阈值
        public const int T_DEMON_LETHAL = 95;     // 走火致死 innerDemon 阈值
        public const int T_STUCK = 6;             // 瓶颈卡死 streak 阈值
        public const int MINOR_THR_BASE = 100;    // 小境界突破基线阈值
        public const int BASE_TRIB_GATE = 40;     // 渡劫基线门值
        public const int SURVIVE_BAND = 10;       // 渡劫幸存带
        public const int LETHAL_BAND = 25;        // 渡劫致命带

        /// <summary>
        /// 尝试状态转移。守卫全整数，RNG 由调用方注入（确定性）。
        /// </summary>
        /// <param name="phase">当前 phase</param>
        /// <param name="trigger">触发事件</param>
        /// <param name="flags">CultivationState.Flags（读写）</param>
        /// <param name="rng">确定性 PRNG（可空=纯守卫不用随机）</param>
        /// <returns>转移结果</returns>
        public static PhaseTransition TryTransition(
            CultivationPhase phase, PhaseTrigger trigger,
            IDictionary<string, int> flags, IRandom? rng = null)
        {
            int rootQuality = GetFlag(flags, "rootQuality");
            int insight = GetFlag(flags, "Insight");
            int progress = GetFlag(flags, "progress");
            int innerDemon = GetFlag(flags, "innerDemon");
            int foundation = GetFlag(flags, "foundation");
            int bottleneckStreak = GetFlag(flags, "bottleneckStreak");
            int flatIndex = GetFlag(flags, "flatIndex");
            int sub = GetFlag(flags, "sub");
            int maxSub = GetFlag(flags, "maxSub");
            int major = GetFlag(flags, "major");
            int maxMajor = GetFlag(flags, "maxMajor");
            int tribScore = GetFlag(flags, "tribScore");
            int tribGate = GetFlag(flags, "tribGate");

            switch (phase, trigger)
            {
                // ——— Mortal 态 ———
                case (CultivationPhase.Mortal, PhaseTrigger.Cultivate):
                    if (rootQuality > 0 && insight >= T_INDUCT_INS)
                        return PhaseTransition.Valid(CultivationPhase.QiInduction, "QiInducted");
                    return PhaseTransition.Valid(CultivationPhase.Mortal); // 永凡人

                // ——— QiInduction 态 ———
                case (CultivationPhase.QiInduction, PhaseTrigger.MinorBreak):
                    int roll = rng?.NextInt(20) ?? 10;
                    if (roll >= T_INDUCT_ROLL)
                    {
                        flags["flatIndex"] = 0;
                        flags["foundation"] = INIT_FND;
                        flags["sub"] = 0;
                        return PhaseTransition.Valid(CultivationPhase.MinorAccumulate, "QiInducted");
                    }
                    flags["inductCooldown"] = GetFlag(flags, "inductCooldown") + 2;
                    return PhaseTransition.Valid(CultivationPhase.Mortal, "InductionFailed");

                // ——— MinorAccumulate 态（每修炼 tick） ———
                case (CultivationPhase.MinorAccumulate, PhaseTrigger.Cultivate):
                    // innerDemon 漂移检查
                    if (innerDemon >= T_DEVIATE)
                        return PhaseTransition.Valid(CultivationPhase.Deviation, "DeviationOnset");
                    // 否则留 MinorAccumulate（调用方负责 progress+=gain）
                    return PhaseTransition.Valid(CultivationPhase.MinorAccumulate);

                case (CultivationPhase.MinorAccumulate, PhaseTrigger.MinorBreak):
                    if (progress >= MINOR_THR_BASE)
                        return PhaseTransition.Valid(CultivationPhase.MinorBreakthrough);
                    return PhaseTransition.Valid(CultivationPhase.MinorAccumulate);

                // ——— MinorBreakthrough 态 ———
                case (CultivationPhase.MinorBreakthrough, PhaseTrigger.MinorBreak):
                    int mRoll = rng?.NextInt(20) ?? 10;
                    int minorGate = 12;
                    if (mRoll >= minorGate)
                    {
                        flags["sub"] = sub + 1;
                        flags["progress"] = 0;
                        flags["foundation"] = foundation + 3;
                        if (sub + 1 >= maxSub)
                            return PhaseTransition.Valid(CultivationPhase.MajorConsummate, "MajorConsummated");
                        return PhaseTransition.Valid(CultivationPhase.MinorAccumulate, "MinorBreakthrough");
                    }
                    // 失败：progress 保留 3/4
                    flags["progress"] = progress * 3 / 4;
                    flags["innerDemon"] = innerDemon + 5;
                    return PhaseTransition.Valid(CultivationPhase.MinorAccumulate, "MinorBreakStalled");

                // ——— MajorConsummate 态 ———
                case (CultivationPhase.MajorConsummate, PhaseTrigger.MajorConsummateCheck):
                    if (foundation >= T_CONSUM_FND)
                    {
                        flags["bottleneckStreak"] = 0;
                        return PhaseTransition.Valid(CultivationPhase.Bottleneck, "BottleneckEntered");
                    }
                    // 固本：回 MinorAccumulate（虚境软拦，有收敛证明 §5.5）
                    return PhaseTransition.Valid(CultivationPhase.MinorAccumulate, "FoundationUnstable");

                // ——— Bottleneck 态 ———
                case (CultivationPhase.Bottleneck, PhaseTrigger.BreakAttempt):
                    int breakAid = GetFlag(flags, "breakAid");
                    int breakRoll = rng?.NextInt(20) ?? 10;
                    int majorGate = 14;
                    if (breakRoll + breakAid >= majorGate)
                        return PhaseTransition.Valid(CultivationPhase.Breakthrough, "BreakthroughBegun");
                    // 失败
                    flags["bottleneckStreak"] = bottleneckStreak + 1;
                    flags["innerDemon"] = innerDemon + 3;
                    if (bottleneckStreak + 1 >= T_STUCK)
                        flags["stuck"] = 1;
                    return PhaseTransition.Valid(CultivationPhase.Bottleneck, bottleneckStreak + 1 >= T_STUCK ? "BottleneckStuck" : "BottleneckPersist");

                // ——— Breakthrough 态 (渡劫裁决) ———
                case (CultivationPhase.Breakthrough, PhaseTrigger.TribulationVerdict):
                    if (tribScore >= tribGate)
                    {
                        if (major < maxMajor)
                        {
                            flags["major"] = major + 1;
                            flags["flatIndex"] = GetFlag(flags, "nextFlatIndex");
                            flags["lifespanBonus"] = GetFlag(flags, "lifespanBonus");
                            flags["foundation"] = INIT_FND;
                            return PhaseTransition.Valid(CultivationPhase.MinorAccumulate, "RealmBreakthrough");
                        }
                        // 顶境飞升
                        return PhaseTransition.Valid(CultivationPhase.Mortal, "Ascension");
                    }
                    if (tribScore >= tribGate - SURVIVE_BAND)
                    {
                        flags["tribDebt"] = GetFlag(flags, "tribDebt") + (tribGate - tribScore);
                        flags["foundation"] = foundation - 20;
                        return PhaseTransition.Valid(CultivationPhase.Setback, "TribulationSurvived");
                    }
                    if (tribScore >= tribGate - LETHAL_BAND)
                    {
                        flags["major"] = Math.Max(0, major - 1);
                        flags["foundation"] = foundation - 40;
                        return PhaseTransition.Valid(CultivationPhase.Setback, "RealmFellback");
                    }
                    return PhaseTransition.Valid(CultivationPhase.Fallen, "TribulationFallen");

                // ——— Setback 态 ———
                case (CultivationPhase.Setback, PhaseTrigger.RecoverCheck):
                    int tribDebt = GetFlag(flags, "tribDebt");
                    if (tribDebt <= 0 && foundation >= 40)
                    {
                        flags["tribDebt"] = 0;
                        return PhaseTransition.Valid(CultivationPhase.MinorAccumulate, "Recovered");
                    }
                    return PhaseTransition.Valid(CultivationPhase.Setback);

                // ——— Deviation 态 ———
                case (CultivationPhase.Deviation, PhaseTrigger.DeviateCheck):
                    if (innerDemon >= T_DEMON_LETHAL)
                        return PhaseTransition.Valid(CultivationPhase.Fallen, "DeviationFatal");
                    return PhaseTransition.Valid(CultivationPhase.Deviation);

                case (CultivationPhase.Deviation, PhaseTrigger.PurgeAttempt):
                    int purgeRoll = rng?.NextInt(20) ?? 10;
                    int purgeGate = 16 - (innerDemon - T_DEVIATE) / 5; // 随 innerDemon 上升放宽
                    if (purgeRoll >= purgeGate)
                    {
                        flags["innerDemon"] = innerDemon - 40;
                        flags["foundation"] = foundation - 15;
                        return PhaseTransition.Valid(CultivationPhase.Setback, "DeviationPurged");
                    }
                    flags["innerDemon"] = innerDemon + 2;
                    return PhaseTransition.Valid(CultivationPhase.Deviation, "DeviationWorsen");

                // ——— Fallen 终态 ———
                case (CultivationPhase.Fallen, _):
                    return PhaseTransition.Valid(CultivationPhase.Fallen); // 终态不可转移

                // ——— 任意态→寿元耗尽 ———
                case (_, PhaseTrigger.LifespanExhausted):
                    if (phase != CultivationPhase.Fallen)
                        return PhaseTransition.Valid(CultivationPhase.Fallen, "LifespanExhausted");
                    return PhaseTransition.Valid(CultivationPhase.Fallen);

                // ——— 非法转移 ———
                default:
                    return PhaseTransition.Invalid();
            }
        }

        /// <summary>
        /// 初始化修炼 phase（从凡人开始；rootQuality>0 的角色随后经 Cultivate 触发入 QiInduction）。
        /// </summary>
        public static void Initialize(Character character)
        {
            if (character.Cultivation == null) return;
            // off 模式：不设 cultPhase
            character.Cultivation.Flags["cultPhase"] = (int)CultivationPhase.Mortal;
        }

        /// <summary>安全读取 flag（不存在→0）。</summary>
        private static int GetFlag(IDictionary<string, int> flags, string key)
            => flags.TryGetValue(key, out int v) ? v : 0;
    }
}
