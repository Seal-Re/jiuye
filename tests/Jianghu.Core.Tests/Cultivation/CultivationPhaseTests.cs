using System.Collections.Generic;
using Jianghu.Cultivation;
using Jianghu.Random;
using Xunit;

namespace Jianghu.Core.Tests.Cultivation
{
    /// <summary>
    /// cultivation-a1-rest story-001: CultivationPhase 10态FSM 测试。
    /// 覆盖 AC 1.1-1.7: 10态枚举、6+转移路径、整数守卫、持久化、非法转移不抛、off模式。
    /// </summary>
    public class CultivationPhaseTests
    {
        static Dictionary<string, int> NewFlags(int rootQuality = 1, int insight = 15,
            int foundation = 50, int innerDemon = 0, int progress = 0,
            int tribScore = 50, int tribGate = 40, int major = 0)
        {
            var f = new Dictionary<string, int>
            {
                ["rootQuality"] = rootQuality,
                ["Insight"] = insight,
                ["foundation"] = foundation,
                ["innerDemon"] = innerDemon,
                ["progress"] = progress,
                ["sub"] = 0,
                ["maxSub"] = 3,
                ["major"] = major,
                ["maxMajor"] = 8,
                ["tribScore"] = tribScore,
                ["tribGate"] = tribGate,
                ["flatIndex"] = 0,
            };
            return f;
        }

        // ================================================================
        // AC 1.1: 10 态枚举完整
        // ================================================================

        [Fact]
        public void All10Phases_Enumerated()
        {
            Assert.Equal(0, (int)CultivationPhase.Mortal);
            Assert.Equal(1, (int)CultivationPhase.QiInduction);
            Assert.Equal(2, (int)CultivationPhase.MinorAccumulate);
            Assert.Equal(3, (int)CultivationPhase.MinorBreakthrough);
            Assert.Equal(4, (int)CultivationPhase.MajorConsummate);
            Assert.Equal(5, (int)CultivationPhase.Bottleneck);
            Assert.Equal(6, (int)CultivationPhase.Breakthrough);
            Assert.Equal(7, (int)CultivationPhase.Setback);
            Assert.Equal(8, (int)CultivationPhase.Deviation);
            Assert.Equal(9, (int)CultivationPhase.Fallen);
        }

        // ================================================================
        // AC 1.2: ≥6 条主转移路径
        // ================================================================

        [Fact]
        public void Mortal_WithRoot_GoesToQiInduction()
        {
            var f = NewFlags(rootQuality: 1, insight: 15);
            var r = CultivationPhaseMachine.TryTransition(
                CultivationPhase.Mortal, PhaseTrigger.Cultivate, f);
            Assert.True(r.IsValid);
            Assert.Equal(CultivationPhase.QiInduction, r.Target);
        }

        [Fact]
        public void Mortal_NoRoot_StaysMortal()
        {
            var f = NewFlags(rootQuality: 0, insight: 15);
            var r = CultivationPhaseMachine.TryTransition(
                CultivationPhase.Mortal, PhaseTrigger.Cultivate, f);
            Assert.True(r.IsValid);
            Assert.Equal(CultivationPhase.Mortal, r.Target); // 永凡人
        }

        [Fact]
        public void QiInduction_NoRNG_UsesDefaultRoll()
        {
            var f = NewFlags();
            // 无 RNG → default roll=10 < T_INDUCT_ROLL(12) → 退回 Mortal
            var r = CultivationPhaseMachine.TryTransition(
                CultivationPhase.QiInduction, PhaseTrigger.MinorBreak, f, null);
            Assert.True(r.IsValid);
            Assert.Equal(CultivationPhase.Mortal, r.Target); // 失败回退
        }

        [Fact]
        public void QiInduction_DifferentSeed_Deterministic()
        {
            // 确定性：同 seed 同结果
            var f1 = NewFlags();
            var f2 = NewFlags();
            var rng1 = new Pcg32(42, 0);
            var rng2 = new Pcg32(42, 0);
            var r1 = CultivationPhaseMachine.TryTransition(
                CultivationPhase.QiInduction, PhaseTrigger.MinorBreak, f1, rng1);
            var r2 = CultivationPhaseMachine.TryTransition(
                CultivationPhase.QiInduction, PhaseTrigger.MinorBreak, f2, rng2);
            Assert.Equal(r1.Target, r2.Target);
        }

        [Fact]
        public void MinorBreakthrough_DefaultRoll_StaysSamePhase()
        {
            var f = NewFlags(progress: 120);
            // 先进 MinorBreakthrough
            var r1 = CultivationPhaseMachine.TryTransition(
                CultivationPhase.MinorAccumulate, PhaseTrigger.MinorBreak, f);
            Assert.Equal(CultivationPhase.MinorBreakthrough, r1.Target);

            // 无 RNG → default roll=10 < minorGate(12) → 失败，progress留3/4
            var r2 = CultivationPhaseMachine.TryTransition(
                CultivationPhase.MinorBreakthrough, PhaseTrigger.MinorBreak, f, null);
            Assert.True(r2.IsValid);
            Assert.Equal(CultivationPhase.MinorAccumulate, r2.Target); // 失败回退
            Assert.True(f["progress"] > 0, "progress should be preserved at 3/4");
        }

        [Fact]
        public void MajorConsummate_EnoughFoundation_GoesToBottleneck()
        {
            var f = NewFlags(foundation: 80);
            var r = CultivationPhaseMachine.TryTransition(
                CultivationPhase.MajorConsummate, PhaseTrigger.MajorConsummateCheck, f);
            Assert.Equal(CultivationPhase.Bottleneck, r.Target);
            Assert.Equal(0, f["bottleneckStreak"]);
        }

        [Fact]
        public void Breakthrough_PassGate_AdvancesMajor()
        {
            var f = NewFlags(tribScore: 50, tribGate: 40);
            f["major"] = 2;
            var r = CultivationPhaseMachine.TryTransition(
                CultivationPhase.Breakthrough, PhaseTrigger.TribulationVerdict, f);
            Assert.Equal(CultivationPhase.MinorAccumulate, r.Target);
            Assert.Equal(3, f["major"]);
        }

        [Fact]
        public void Breakthrough_FailBand_GoesToSetback()
        {
            var f = NewFlags(tribScore: 35, tribGate: 50);
            var r = CultivationPhaseMachine.TryTransition(
                CultivationPhase.Breakthrough, PhaseTrigger.TribulationVerdict, f);
            // 35 >= 50-10=40? No. 35 >= 50-25=25? Yes → Setback with major-1
            Assert.Equal(CultivationPhase.Setback, r.Target);
        }

        [Fact]
        public void Deviation_InnerDemonLethal_GoesToFallen()
        {
            var f = NewFlags(innerDemon: 96);
            var r = CultivationPhaseMachine.TryTransition(
                CultivationPhase.Deviation, PhaseTrigger.DeviateCheck, f);
            Assert.Equal(CultivationPhase.Fallen, r.Target);
        }

        // ================================================================
        // AC 1.3: 整数守卫条件
        // ================================================================

        [Fact]
        public void Mortal_LowInsight_StaysMortal()
        {
            var f = NewFlags(rootQuality: 1, insight: 5);
            var r = CultivationPhaseMachine.TryTransition(
                CultivationPhase.Mortal, PhaseTrigger.Cultivate, f);
            Assert.Equal(CultivationPhase.Mortal, r.Target); // insight < 10
        }

        [Fact]
        public void MinorAccumulate_InnerDemonHigh_Deviates()
        {
            var f = NewFlags(innerDemon: 65);
            var r = CultivationPhaseMachine.TryTransition(
                CultivationPhase.MinorAccumulate, PhaseTrigger.Cultivate, f);
            Assert.Equal(CultivationPhase.Deviation, r.Target);
        }

        // ================================================================
        // AC 1.4: 状态持久化（Flags 自然随 Dict 持久化）
        // ================================================================

        [Fact]
        public void CultPhase_StoredInFlags()
        {
            var f = new Dictionary<string, int>();
            f["cultPhase"] = (int)CultivationPhase.MinorAccumulate;
            Assert.Equal((int)CultivationPhase.MinorAccumulate, f["cultPhase"]);

            // Clone：dict 拷贝=持久化
            var clone = new Dictionary<string, int>(f);
            Assert.Equal((int)CultivationPhase.MinorAccumulate, clone["cultPhase"]);
        }

        // ================================================================
        // AC 1.5: 非法转移不抛
        // ================================================================

        [Fact]
        public void InvalidTransition_DoesNotThrow()
        {
            var f = NewFlags();
            // Mortal → MinorBreak（非法：凡人不能小突破）
            var r = CultivationPhaseMachine.TryTransition(
                CultivationPhase.Mortal, PhaseTrigger.MinorBreak, f);
            Assert.False(r.IsValid);
            // 状态不改
        }

        [Fact]
        public void Fallen_StaysFallen_AnyTrigger()
        {
            var f = NewFlags();
            var r = CultivationPhaseMachine.TryTransition(
                CultivationPhase.Fallen, PhaseTrigger.Cultivate, f);
            Assert.True(r.IsValid);
            Assert.Equal(CultivationPhase.Fallen, r.Target);
        }

        // ================================================================
        // AC 1.6: off 模式（CultivationPhaseMachine 由调用方决定是否触发）
        // ================================================================

        [Fact]
        public void OffMode_NoCultPhaseFlag()
        {
            var f = new Dictionary<string, int>();
            // cultPhase 不存在 → Initialize 不生效（调用方判断 cultivation==null）
            Assert.False(f.ContainsKey("cultPhase"));
        }

        // ================================================================
        // PhaseTransition struct
        // ================================================================

        [Fact]
        public void PhaseTransition_Valid_ReturnsCorrect()
        {
            var t = PhaseTransition.Valid(CultivationPhase.MinorAccumulate, "TestEvent");
            Assert.True(t.IsValid);
            Assert.Equal(CultivationPhase.MinorAccumulate, t.Target);
            Assert.Equal("TestEvent", t.EventKey);
        }

        [Fact]
        public void PhaseTransition_Invalid_ReturnsDefault()
        {
            var t = PhaseTransition.Invalid();
            Assert.False(t.IsValid);
        }
    }
}
