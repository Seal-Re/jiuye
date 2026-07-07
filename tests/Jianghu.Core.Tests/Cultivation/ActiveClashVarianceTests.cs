using System;
using System.Collections.Generic;
using Jianghu.Config;
using Jianghu.Cultivation;
using Jianghu.Model;
using Jianghu.Random;
using Jianghu.Stats;
using Xunit;

namespace Jianghu.Core.Tests.Cultivation
{
    /// <summary>
    /// cv-001（adr-0008 决策①③④）：主动交锋概率拦截接线测试。
    /// 验 duelRng=null 确定性旁路（既有行为不变）、同种子逐字节复现、方差实际改变结果、
    /// 弱方理论反杀窗口。触 Jianghu.Cultivation → 旗舰档 + 主控核验（B.7/A.3）。
    /// </summary>
    public class ActiveClashVarianceTests
    {
        static LimitsConfig Limits => LimitsConfig.Default;

        // PE = stat:Force × 4 × RealmMult(=1)。故 Force 直控 PE：Force=25 → PE=100。
        static CultivationPathDef MakePath(string id) => new CultivationPathDef(
            id, id, "physical", new[] { "melee" },
            new[] { new ResourceDef("qi", 0, 1000, 0) },
            new PowerFormulaDef(new[] { new PowerTerm("stat:Force", 4, null) },
                Array.Empty<PowerMod>(), null),
            new RealmCurveDef(new[] { 10, 15, 25 }, new[] { 0, 1, 2 },
                new[] { "L1", "L2", "L3" }, new[] { 0, 100, 300 },
                new[] { 1, 1, 1 }, true, 2),
            Array.Empty<ArtCategoryDef>(),
            new[] { new CombatSkillDef("atk", "atk", 0, Array.Empty<EffectOp>(),
                new Dictionary<string, int>()) },
            new EntryGateDef(""), new SelectionRuleDef(1, 3), null);

        static Character MakeChar(long id, int force, CultivationPathDef path)
        {
            var c = new Character(new CharacterId(id),
                new Persona("n", "t", "s", ArchetypeKind.Martial, null),
                new StatBlock(new[] { force, 0, 0, 0 }),
                new NodeId(0), new Goal(GoalKind.Advance, 0), 0, 800, 16);
            var cult = CultivationState.NewForPath(path.PathId, path.Resources,
                Array.Empty<string>(), new[] { "atk" });
            cult.RealmIndex = 1; // 同 UT（避免 gap≥2 auto-win 短路，测方差区）
            c.Cultivation = cult;
            return c;
        }

        static PathRegistry Reg(CultivationPathDef p) => new PathRegistry(new ListPathSource(new[] { p }));
        static IRandom Rng(ulong seed) => new Pcg32(seed, 9);

        // 私有 IPathSource（同各 Cultivation 测试文件既有模式：每文件自带一份）。
        sealed class ListPathSource : IPathSource
        {
            private readonly IReadOnlyList<CultivationPathDef> _paths;
            public ListPathSource(IReadOnlyList<CultivationPathDef> paths) => _paths = paths;
            public IReadOnlyList<CultivationPathDef> Load() => _paths;
        }

        [Fact]
        public void test_null_duelrng_is_deterministic_bypass()
        {
            // Arrange：近等 PE 对（Force 25 vs 24 → PE 100 vs 96）
            var path = MakePath("p");
            var a = MakeChar(1, 25, path);
            var b = MakeChar(2, 24, path);

            // Act：duelRng=null（既有行为）
            var r1 = DuelEngine.ResolveR2(a, b, path, path, Reg(path), Limits, null, null, null);
            var r2 = DuelEngine.ResolveR2(a, b, path, path, Reg(path), Limits, null, null, null);

            // Assert：null 旁路 → 确定，两跑同结果（既有断言逐字节不变的根据）
            Assert.Equal(r1.Winner, r2.Winner);
            Assert.Equal(r1.Margin, r2.Margin);
        }

        [Fact]
        public void test_same_seed_produces_byte_identical_result()
        {
            // Arrange
            var path = MakePath("p");
            var a = MakeChar(1, 25, path);
            var b = MakeChar(2, 24, path);

            // Act：同种子 duelRng 两跑
            var r1 = DuelEngine.ResolveR2(a, b, path, path, Reg(path), Limits, null, null, null,
                duelRng: Rng(42));
            var r2 = DuelEngine.ResolveR2(a, b, path, path, Reg(path), Limits, null, null, null,
                duelRng: Rng(42));

            // Assert：确定性（B.2）——同种子逐字节复现
            Assert.Equal(r1.Winner, r2.Winner);
            Assert.Equal(r1.Margin, r2.Margin);
            Assert.Equal(r1.AttackerHpRemaining, r2.AttackerHpRemaining);
            Assert.Equal(r1.DefenderHpRemaining, r2.DefenderHpRemaining);
        }

        [Fact]
        public void test_variance_changes_outcome_versus_deterministic()
        {
            // Arrange：近等 PE 对
            var path = MakePath("p");
            var a = MakeChar(1, 25, path);
            var b = MakeChar(2, 24, path);

            // Act：确定性 margin vs 方差 margin
            var det = DuelEngine.ResolveR2(a, b, path, path, Reg(path), Limits, null, null, null);

            // Assert：至少一个种子的方差结果与确定性 margin 不同（证方差在起作用，非哑弹）
            bool anyDiffers = false;
            for (ulong s = 1; s <= 20 && !anyDiffers; s++)
            {
                var v = DuelEngine.ResolveR2(a, b, path, path, Reg(path), Limits, null, null, null,
                    duelRng: Rng(s));
                if (v.Margin != det.Margin) anyDiffers = true;
            }
            Assert.True(anyDiffers, "方差未改变任何对局结果——拦截器未生效");
        }

        [Fact]
        public void test_weaker_side_can_win_across_seeds()
        {
            // Arrange：a 略强（Force 26→PE104），b 略弱（Force 24→PE96）。确定性下 a 必胜。
            var path = MakePath("p");
            var a = MakeChar(1, 26, path);
            var b = MakeChar(2, 24, path);
            var det = DuelEngine.ResolveR2(a, b, path, path, Reg(path), Limits, null, null, null);
            Assert.Equal(a.Id, det.Winner); // 前提：确定性下强者 a 胜

            // Act：跨种子 sweep 统计弱者 b 是否能爆冷
            int bWins = 0, total = 0;
            for (ulong s = 1; s <= 100; s++)
            {
                var v = DuelEngine.ResolveR2(a, b, path, path, Reg(path), Limits, null, null, null,
                    duelRng: Rng(s));
                total++;
                if (v.Winner == b.Id) bWins++;
            }

            // Assert：弱者有非零胜场（弱胜强叙事地基，adr-0008 α）——但非过半（强者仍占优）
            Assert.True(bWins > 0, "弱者跨 100 种子零胜——理论反杀窗口缺失");
            Assert.True(bWins < total / 2, $"弱者胜率过半（{bWins}/{total}）——强弱倒置");
        }

        [Fact]
        public void test_calibration_mode_bypasses_variance()
        {
            // Arrange：标定期应旁路方差（只测裸 PE），即便传入 duelRng
            var path = MakePath("p");
            var a = MakeChar(1, 25, path);
            var b = MakeChar(2, 24, path);

            // Act：calibrationMode=true + duelRng → 应等同确定性
            var det = DuelEngine.ResolveR2(a, b, path, path, Reg(path), Limits, null, null, null);
            var calib = DuelEngine.ResolveR2(a, b, path, path, Reg(path), Limits, null, null, null,
                calibrationMode: true, duelRng: Rng(7));

            // Assert：标定期 margin 与确定性一致（方差被旁路）
            Assert.Equal(det.Winner, calib.Winner);
            Assert.Equal(det.Margin, calib.Margin);
        }
    }
}
