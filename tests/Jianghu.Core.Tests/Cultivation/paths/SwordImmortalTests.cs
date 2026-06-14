using System.Collections.Generic;
using System.Linq;
using Jianghu.Actions;
using Jianghu.Config;
using Jianghu.Cultivation;
using Jianghu.Cultivation.Paths;
using Jianghu.Decide;
using Jianghu.Events;
using Jianghu.Model;
using Jianghu.Sim;
using Jianghu.Stats;
using Xunit;

namespace Jianghu.Core.Tests.Cultivation.Paths
{
    /// <summary>
    /// Task 4.1：剑修 sword_immortal 范式路端到端。
    /// ①PathValidator 过；②生成期带 sword_root 定到 sword_immortal；③战力随 realm 升；
    /// ④两剑修切磋出 DuelResolved + 情境 adj 生效（righteous 克 evil，零 PathId）；
    /// ⑤被动功法 EffectOp（AddResourceCap）装配生效。
    /// </summary>
    public class SwordImmortalTests
    {
        // ① 数据质量 gate：剑修 Def 必过 PathValidator 全部校验。
        [Fact]
        public void Def_PassesPathValidator()
        {
            PathValidator.AssertValid(SwordImmortalPath.Def); // 不抛即过
        }

        // ① 附加：canon pathId + 含 daoheart 类目（swordheart）+ terms 无 daoHeart/×0。
        [Fact]
        public void Def_IsCanonAndShaped()
        {
            var d = SwordImmortalPath.Def;
            Assert.Equal("sword_immortal", d.PathId);
            Assert.Contains(d.ArtCategories, c => c.Role == "daoheart");
            Assert.True(d.ArtCategories.Count >= 3);
            Assert.All(d.ArtCategories, c => Assert.True(c.Arts.Count >= 4));
            Assert.True(d.CombatSkills.Count >= 5);
            Assert.All(d.Power.Terms, t => Assert.NotEqual(0, t.Weight));
            Assert.DoesNotContain(d.Power.Terms, t => t.Src.Contains("daoHeart") || t.Src.Contains("innerDemon"));
            // SituationalTags = 属性/形态 tag（非对手 PathId）。
            Assert.Equal(new[] { "melee", "sword", "righteous" }, d.SituationalTags);
        }

        // ② 生成期：唯剑修一路注册 → 派生池仅 sword_root → 人人带 sword_root → 人人定到 sword_immortal。
        [Fact]
        public void Generation_AssignsSwordImmortal_OnSwordRoot()
        {
            var src = new ListPathSource(new[] { SwordImmortalPath.Def });
            var w = WorldFactory.CreateInitial(2026, LimitsConfig.Default, 5,
                cultivation: true, pathSource: src);
            foreach (var c in w.AliveCharacters())
            {
                Assert.NotNull(c.Cultivation);
                Assert.Equal("sword_immortal", c.Cultivation!.PathId);
                Assert.Contains("sword_root", c.Persona.Tags);
            }
        }

        // ③ 战力随 realm 升：同四维同选择，realm 越高 PowerEngine×Curve 越大（凸曲线递增）。
        [Fact]
        public void Power_RisesWithRealm()
        {
            var def = SwordImmortalPath.Def;
            var stats = new StatBlock(new[] { 25, 15, 10, 20 }); // 四维总和=70≤80
            int prev = -1;
            for (int r = 0; r < def.Curve.RealmMultipliers.Count; r++)
            {
                var st = CultivationState.NewForPath(def.PathId, def.Resources);
                st.RealmIndex = r;
                int pe = PowerEngine.Evaluate(st, stats, def, LimitsConfig.Default);
                Assert.True(pe > prev, $"realm {r} pe={pe} 未高于前一阶 {prev}");
                prev = pe;
            }
        }

        // ④a 两剑修切磋出 DuelResolved；境界高者战力主导取胜。
        [Fact]
        public void TwoSwordsmen_Spar_HigherRealmWins()
        {
            var def = SwordImmortalPath.Def;
            var reg = new PathRegistry(new ListPathSource(new[] { def }));
            var w = new FakeWorld();
            var a = Make(1, 22, 12, 8, 18); // 攻方
            var b = Make(2, 22, 12, 8, 18); // 同四维守方
            a.Cultivation = CultivationState.NewForPath(def.PathId, def.Resources);
            b.Cultivation = CultivationState.NewForPath(def.PathId, def.Resources);
            a.Cultivation!.RealmIndex = 3; // 高境界
            b.Cultivation!.RealmIndex = 0;
            w.All.Add(a); w.All.Add(b);

            var spar = new SparAction(w.Limits, reg);
            var duel = spar.Apply(w, a, new SparChoice(new CharacterId(2))).OfType<DuelResolved>().Single();
            Assert.Equal(1, duel.Winner.Value); // 高境界 a 胜
            Assert.True(duel.Margin > 0);
        }

        // ④b 情境 adj 生效（零 PathId，真隔离）：剑修近战 melee 被远程对手放风筝（Bible §6.4 distance 轴 +5）。
        //    攻方=ranged 对手，用与剑修同 Power/Curve 但不同 pathId+tags 的 mock 路 → 双方裸 pe 完全相等，
        //    唯放风筝 adj 区分胜负 → 证软情境真生效（非靠公式天然差）。
        [Fact]
        public void Swordsman_KitedByRanged_SituationalAdj_Decides()
        {
            var sword = SwordImmortalPath.Def;
            // 远程攻方：克隆剑修 def（同公式 → 裸 pe 与剑修相等），仅改 pathId + SituationalTags=ranged。
            var ranged = sword with { PathId = "fa_xiu", SituationalTags = new[] { "ranged" } };
            var reg = new PathRegistry(new ListPathSource(new[] { sword, ranged }));
            var w = new FakeWorld();
            var atk = Make(1, 22, 12, 8, 18); // ranged 攻方
            var def = Make(2, 22, 12, 8, 18); // 剑修守方（同四维同境界 → 裸 pe 相等）
            atk.Cultivation = CultivationState.NewForPath("fa_xiu", ranged.Resources);
            def.Cultivation = CultivationState.NewForPath(sword.PathId, sword.Resources);
            w.All.Add(atk); w.All.Add(def);

            var spar = new SparAction(w.Limits, reg);
            var duel = spar.Apply(w, atk, new SparChoice(new CharacterId(2))).OfType<DuelResolved>().Single();
            // 裸 pe 相等下，远程放风筝 +5% adj 增益攻方 → atk 胜（唯一区分源=情境 adj）。
            Assert.Equal(1, duel.Winner.Value);
            Assert.True(duel.Margin > 0);
        }

        // ⑤ 被动功法 EffectOp 装配生效：剑修心法「凝剑诀」AddResourceCap(swordWill,+3) → 装配后 cap 抬高。
        [Fact]
        public void PassiveArt_EffectOp_AppliesViaInterpreter()
        {
            var def = SwordImmortalPath.Def;
            // 取 swordWill 原始 cap。
            var swordWillDef = def.Resources.Single(r => r.Key == "swordWill");
            int baseCap = swordWillDef.Cap;

            var st = CultivationState.NewForPath(def.PathId, def.Resources);
            // 找一部带 AddResourceCap(swordWill) 被动的心法功法，装配其 effects。
            var capArt = def.ArtCategories
                .SelectMany(c => c.Arts)
                .First(a => a.Effects.Any(e => e.Kind == EffectOpKind.AddResourceCap && e.Key == "swordWill"));
            foreach (var op in capArt.Effects)
                EffectInterpreter.ApplyPassive(op, st);

            // 抬 cap 后，注入超原 cap 的资源应能突破原上限（证 RaiseCap 生效）。
            st.ApplyResource("swordWill", baseCap + 100);
            Assert.True(st.Resources["swordWill"] > baseCap, "AddResourceCap 未抬高 swordWill 上限");
        }

        // —— helpers ——

        private static Character Make(long id, int force, int intl, int con, int insight) =>
            new Character(new CharacterId(id),
                new Persona("剑客", "y", "z", ArchetypeKind.Martial, null),
                new StatBlock(new[] { force, intl, con, insight }),
                new NodeId(0), new Goal(GoalKind.Advance, 0), 0, 800, 16);

        private sealed class FakeWorld : IWorldMutator
        {
            public long Clock { get; set; } = 5;
            public List<Character> All { get; } = new List<Character>();
            public Relations Rel { get; } = new Relations();
            public LimitsConfig Limits { get; } = LimitsConfig.Default;
            public int NodeCount => 3;
            public IReadOnlyList<Character> AtNode(NodeId n) => All.Where(c => c.Node.Value == n.Value && c.Alive).ToList();
            public void ApplyStat(Character c, StatKind k, int d) => c.Stats.Apply(k, d, Limits);
            public int AdjustRelation(CharacterId f, CharacterId t, int d) => Rel.Adjust(f, t, d);
            public void Move(Character c, NodeId to) => c.Node = to;
        }

        sealed class ListPathSource : IPathSource
        {
            private readonly IReadOnlyList<CultivationPathDef> _paths;
            public ListPathSource(IReadOnlyList<CultivationPathDef> paths) => _paths = paths;
            public IReadOnlyList<CultivationPathDef> Load() => _paths;
        }
    }
}
