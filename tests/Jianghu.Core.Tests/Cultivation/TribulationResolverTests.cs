using System;
using System.Collections.Generic;
using Jianghu.Cultivation;
using Jianghu.Random;
using Jianghu.Stats;
using Xunit;

namespace Jianghu.Core.Tests.Cultivation
{
    /// <summary>
    /// cultivation-a1-rest: TribulationDef + TribulationResolver 测试。
    /// 三劫数据驱动：渡劫/天劫/心魔劫。
    /// </summary>
    public class TribulationResolverTests
    {
        static CultivationState MakeState(Dictionary<string, int>? flags = null, Dictionary<string, int>? res = null)
        {
            var st = CultivationState.NewForPath("test_path", new List<ResourceDef>
            {
                new ResourceDef("manaPool", 0, 100, 50),
                new ResourceDef("henglian", 0, 100, 40),
                new ResourceDef("qixue", 0, 200, 100),
                new ResourceDef("daoHeart", 0, 100, 30),
            });
            if (flags != null)
                foreach (var kv in flags) st.Flags[kv.Key] = kv.Value;
            if (res != null)
                foreach (var kv in res) st.Resources[kv.Key] = kv.Value;
            return st;
        }

        static StatBlock MakeStats(int f = 20, int i = 20, int c = 20, int ins = 20)
            => new StatBlock(new[] { f, i, c, ins });

        // ================================================================
        // TribulationDef 三劫定义
        // ================================================================

        [Fact]
        public void ThreeTribulationDefs_Registered()
        {
            Assert.True(TribulationResolver.All.ContainsKey("tribulation"));
            Assert.True(TribulationResolver.All.ContainsKey("heavenly"));
            Assert.True(TribulationResolver.All.ContainsKey("heart_demon"));
        }

        [Fact]
        public void Tribulation_ResistTerms_HaveCorrectWeights()
        {
            var def = TribulationResolver.Tribulation;
            Assert.Equal(3, def.ResistTerms.Count);
            Assert.Contains(def.ResistTerms, t => t.Src == "EffectivePower" && t.Weight == 3);
            Assert.Contains(def.ResistTerms, t => t.Src == "Foundation" && t.Weight == 2);
        }

        [Fact]
        public void HeavenlyTribulation_HasConstitutionWeight()
        {
            var def = TribulationResolver.HeavenlyTribulation;
            Assert.Contains(def.ResistTerms, t => t.Src == "Constitution" && t.Weight == 4);
            Assert.Contains(def.ResistTerms, t => t.Src == "henglian" && t.Weight == 3);
        }

        [Fact]
        public void HeartDemonTribulation_InnerDemonNegativeWeight()
        {
            var def = TribulationResolver.HeartDemonTribulation;
            Assert.Contains(def.ResistTerms, t => t.Src == "InnerDemon" && t.Weight == -2);
            Assert.Contains(def.ResistTerms, t => t.Src == "daoHeart" && t.Weight == 2);
        }

        // ================================================================
        // ComputeTribScore
        // ================================================================

        [Fact]
        public void ComputeTribScore_NoRNG_ReturnsSumMinusPenalty()
        {
            var def = TribulationResolver.Tribulation;
            var st = MakeState(new Dictionary<string, int> { ["foundation"] = 70 });
            var stats = MakeStats(20, 20, 20, 20); // Σ=80 → EffectivePower

            int score = TribulationResolver.ComputeTribScore(def, st, stats, threatPenalty: 0, rng: null);
            // EffectivePower×3: 80×3=240, Foundation×2: 70×2=140, manaPool×1: 50×1=50
            // Sum = 430, no roll, no penalty → 430
            Assert.Equal(430, score);
        }

        [Fact]
        public void ComputeTribScore_WithThreatPenalty_ReducesScore()
        {
            var def = TribulationResolver.Tribulation;
            var st = MakeState(new Dictionary<string, int> { ["foundation"] = 70 });
            var stats = MakeStats();

            int noPenalty = TribulationResolver.ComputeTribScore(def, st, stats, threatPenalty: 0, rng: null);
            int withPenalty = TribulationResolver.ComputeTribScore(def, st, stats, threatPenalty: 50, rng: null);
            Assert.Equal(noPenalty - 50, withPenalty);
        }

        [Fact]
        public void ComputeTribScore_HeartDemon_HighInnerDemon_ReducesScore()
        {
            var def = TribulationResolver.HeartDemonTribulation;
            var st = MakeState(new Dictionary<string, int> { ["innerDemon"] = 50 });
            // daoHeart=30 → +60, InnerDemon=50×−2=−100, Insight=20×3=60 → total=20
            var stats = MakeStats();
            int score = TribulationResolver.ComputeTribScore(def, st, stats, threatPenalty: 0, rng: null);
            int expected = 60 + (-100) + 60; // Insight×3=60, InnerDemon×−2=−100, daoHeart×2=60
            Assert.Equal(expected, score);
        }

        [Fact]
        public void ComputeTribScore_WithRNG_ProducesDeterministicResult()
        {
            var def = TribulationResolver.Tribulation;
            var st = MakeState(new Dictionary<string, int> { ["foundation"] = 70 });
            var stats = MakeStats();

            var rng1 = new Pcg32(42, 0);
            var rng2 = new Pcg32(42, 0);
            int s1 = TribulationResolver.ComputeTribScore(def, st, stats, rng: rng1);
            int s2 = TribulationResolver.ComputeTribScore(def, st, stats, rng: rng2);
            Assert.Equal(s1, s2); // 确定性
        }

        // ================================================================
        // ComputeTribGate
        // ================================================================

        [Fact]
        public void ComputeTribGate_ScalesWithUT()
        {
            var def = TribulationResolver.Tribulation;
            int gate0 = TribulationResolver.ComputeTribGate(def, unifiedTier: 0);
            int gate4 = TribulationResolver.ComputeTribGate(def, unifiedTier: 4);
            int gate8 = TribulationResolver.ComputeTribGate(def, unifiedTier: 8);

            Assert.Equal(20, gate0);  // 40*(0+1)/2 = 20
            Assert.Equal(100, gate4); // 40*(4+1)/2 = 100
            Assert.Equal(180, gate8); // 40*(8+1)/2 = 180
        }

        // ================================================================
        // GetFailBranch
        // ================================================================

        [Fact]
        public void GetFailBranch_ScoreMeetsGate_ReturnsNull()
        {
            var def = TribulationResolver.Tribulation;
            var branch = TribulationResolver.GetFailBranch(def, tribScore: 200, tribGate: 100);
            Assert.Null(branch); // 通过
        }

        [Fact]
        public void GetFailBranch_ScoreInSurviveBand_ReturnsSetback()
        {
            var def = TribulationResolver.Tribulation;
            var branch = TribulationResolver.GetFailBranch(def, tribScore: 95, tribGate: 100);
            Assert.NotNull(branch);
            Assert.Equal(CultivationPhase.Setback, branch!.FailTarget);
        }

        [Fact]
        public void GetFailBranch_ScoreFarBelow_ReturnsFallen()
        {
            var def = TribulationResolver.Tribulation;
            var branch = TribulationResolver.GetFailBranch(def, tribScore: 50, tribGate: 100);
            Assert.NotNull(branch);
            Assert.Equal(CultivationPhase.Fallen, branch!.FailTarget);
        }

        // ================================================================
        // 数据驱动：新劫型不改引擎
        // ================================================================

        [Fact]
        public void NewTribulationType_WorksWithSameResolver()
        {
            // 模拟加"器劫"：器修独有劫，只测 ResistTerms
            var artifactTrib = new TribulationDef(
                "artifact_trib",
                new TribulationTerm[] { new("itemTier", 5), new("craftScore", 3) },
                "BASE * UT",
                Array.Empty<TribulationFailBranch>());

            var st = MakeState(res: new Dictionary<string, int> { ["itemTier"] = 4, ["craftScore"] = 60 });
            var stats = MakeStats();
            int score = TribulationResolver.ComputeTribScore(artifactTrib, st, stats, rng: null);
            // itemTier×5=20, craftScore×3=180 → 200
            Assert.Equal(200, score);
        }
    }
}
