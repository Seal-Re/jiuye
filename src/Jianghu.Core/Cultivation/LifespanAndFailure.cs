using System.Collections.Generic;
using Jianghu.Random;

namespace Jianghu.Cultivation
{
    /// <summary>
    /// A.1 寿元与失败模式数据（§5）。配合 CultivationPhaseMachine 使用。
    /// 纯整数，确定性，无 RNG。
    /// </summary>
    public static class LifespanAndFailure
    {
        // ================================================================
        // LIFESPAN_TABLE: UT → 寿命增量（突破时叠加 lifespanBonus）
        // ================================================================

        /// <summary>UT→寿命增量表。UT0=0, UT12=ascend（飞升离场不计寿元）。</summary>
        public static readonly IReadOnlyDictionary<int, int> LifespanBonus = new Dictionary<int, int>
        {
            [0] = 0,
            [1] = 50,
            [2] = 100,
            [3] = 200,
            [4] = 300,
            [5] = 450,
            [6] = 600,
            [7] = 800,
            [8] = 1000,
            [9] = 1200,
            [10] = 1500,
            [11] = 2000,
            [12] = int.MaxValue, // Ascended（离场）
        };

        /// <summary>基础寿命（凡人基线）。</summary>
        public const int BaseLifespan = 80;

        /// <summary>走火入魔致死阈值。</summary>
        public const int InnerDemonLethal = 95;

        /// <summary>走火入魔 Devation 触发阈值。</summary>
        public const int InnerDemonDeviate = 60;

        /// <summary>瓶颈卡死 streak 阈值（≥此值 stuck=1）。</summary>
        public const int BottleneckStuckThreshold = 6;

        /// <summary>虚境软拦 Foundation 最低阈值。</summary>
        public const int FoundationSoftFloor = 70;

        /// <summary>渡劫跌境后 TribGate 永久增量（防无限重冲，§5.4）。</summary>
        public const int TribGatePermanentDelta = 5;

        /// <summary>Setback 恢复最低 Foundation。</summary>
        public const int SetbackRecoverFoundation = 40;

        // ================================================================
        // 寿命计算
        // ================================================================

        /// <summary>
        /// 计算死亡线：deathLine = BaseLifespan + Σ lifespanBonus[UT]（已突破各境累加）。
        /// </summary>
        /// <param name="lifespanBonusAccumulated">已累加的 lifespanBonus 总和（存 Flags["lifespanBonus"]）</param>
        public static int DeathLine(int lifespanBonusAccumulated)
            => BaseLifespan + lifespanBonusAccumulated;

        /// <summary>
        /// 突破时叠加寿命增量。
        /// </summary>
        /// <param name="flags">CultivationState.Flags</param>
        /// <param name="unifiedTier">刚达到的新 UT</param>
        public static void ApplyLifespanBonus(IDictionary<string, int> flags, int unifiedTier)
        {
            if (LifespanBonus.TryGetValue(unifiedTier, out int bonus))
            {
                int current = flags.TryGetValue("lifespanBonus", out int v) ? v : 0;
                flags["lifespanBonus"] = current + bonus;
            }
        }

        /// <summary>
        /// 检查寿元耗尽（Age ≥ deathLine）。
        /// </summary>
        public static bool IsLifespanExhausted(int age, int lifespanBonusAccumulated)
            => age >= DeathLine(lifespanBonusAccumulated);

        // ================================================================
        // 失败模式辅助
        // ================================================================

        /// <summary>
        /// 渡劫跌境：major-1, Foundation-40, TribGate 永久 +ΔP（防无限重冲）。
        /// 副作用经 flags chokepoint 落。
        /// </summary>
        public static void ApplyRealmFallback(IDictionary<string, int> flags)
        {
            int major = flags.TryGetValue("major", out int m) ? m : 0;
            flags["major"] = System.Math.Max(0, major - 1);
            int fnd = flags.TryGetValue("foundation", out int f) ? f : 0;
            flags["foundation"] = System.Math.Max(0, fnd - 40);
            int permDelta = flags.TryGetValue("tribGatePermDelta", out int pd) ? pd : 0;
            flags["tribGatePermDelta"] = permDelta + TribGatePermanentDelta;
        }

        /// <summary>
        /// 瓶颈解困：外部破障（天材/顿悟/外力）可清 stuck 标记并重置 streak。
        /// </summary>
        public static void UnstuckBottleneck(IDictionary<string, int> flags)
        {
            flags["bottleneckStreak"] = 0;
            flags["stuck"] = 0;
        }

        /// <summary>
        /// 走火自救结果：true=自救成功（→Setback），false=恶化（innerDemon+2）。
        /// </summary>
        public static bool TryPurgeDeviation(IDictionary<string, int> flags, IRandom? rng = null)
        {
            int innerDemon = flags.TryGetValue("innerDemon", out int id) ? id : 0;
            int purgeGate = 16 - (innerDemon - InnerDemonDeviate) / 5; // 随 innerDemon 上升放宽
            if (purgeGate < 2) purgeGate = 2;
            int roll = rng?.NextInt(20) ?? 10;
            if (roll >= purgeGate)
            {
                flags["innerDemon"] = System.Math.Max(0, innerDemon - 40);
                int fnd = flags.TryGetValue("foundation", out int f) ? f : 0;
                flags["foundation"] = System.Math.Max(0, fnd - 15);
                return true;
            }
            flags["innerDemon"] = innerDemon + 2;
            return false;
        }

        /// <summary>
        /// Setback 恢复检查：tribDebt≤0 ∧ Foundation≥恢复阈值。
        /// 每 tick tribDebt 自然衰减。
        /// </summary>
        public static bool CanRecoverFromSetback(IDictionary<string, int> flags)
        {
            int tribDebt = flags.TryGetValue("tribDebt", out int td) ? td : 0;
            int foundation = flags.TryGetValue("foundation", out int f) ? f : 0;
            return tribDebt <= 0 && foundation >= SetbackRecoverFoundation;
        }

        /// <summary>
        /// 每 tick tribDebt 自然衰减（恢复期）。
        /// </summary>
        public static void DecayTribDebt(IDictionary<string, int> flags, int decay = 5)
        {
            int tribDebt = flags.TryGetValue("tribDebt", out int td) ? td : 0;
            if (tribDebt > 0)
                flags["tribDebt"] = System.Math.Max(0, tribDebt - decay);
        }
    }
}
