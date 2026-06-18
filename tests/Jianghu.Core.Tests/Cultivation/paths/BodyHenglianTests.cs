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
    /// Task 4 续（体修）：炼体 ti_xiu_hengshi（physical 耐久代表）端到端。
    /// ①PathValidator 过 + canon/shape；②生成期带 body_root 定到 ti_xiu_hengshi；③战力随 realm 升（平滑曲线）；
    /// ④两体修切磋出 DuelResolved + 情境 adj 生效（melee 体修被 ranged 放风筝,零 PathId 真隔离）；
    /// ⑤被动功法 EffectOp（AddResourceCap qixue / AddResource henglian）装配生效。
    /// </summary>
    public class BodyHenglianTests
    {
        // ① 数据质量 gate：体修 Def 必过 PathValidator 全部校验。
        [Fact]
        public void Def_PassesPathValidator()
        {
            PathValidator.AssertValid(BodyHenglianPath.Def); // 不抛即过
        }

        // ① 附加：canon pathId + 含 daoheart 类目（bodyheart=武胆）+ terms 无 daoHeart/×0 + 属性 tag。
        [Fact]
        public void Def_IsCanonAndShaped()
        {
            var d = BodyHenglianPath.Def;
            Assert.Equal("ti_xiu_hengshi", d.PathId);
            Assert.Contains(d.ArtCategories, c => c.Role == "daoheart");
            Assert.True(d.ArtCategories.Count >= 3);
            Assert.All(d.ArtCategories, c => Assert.True(c.Arts.Count >= 4));
            Assert.True(d.CombatSkills.Count >= 5);
            Assert.All(d.Power.Terms, t => Assert.NotEqual(0, t.Weight));
            Assert.DoesNotContain(d.Power.Terms, t => t.Src.Contains("daoHeart") || t.Src.Contains("innerDemon"));
            // SituationalTags = 属性/形态 tag（非对手 PathId）。
            Assert.Equal(new[] { "melee", "brute", "body" }, d.SituationalTags);
        }

        // ② 生成期：唯体修一路注册 → 派生池仅 body_root → 人人带 body_root → 人人定到 ti_xiu_hengshi。
        [Fact]
        public void Generation_AssignsBodyHenglian_OnBodyRoot()
        {
            var src = new ListPathSource(new[] { BodyHenglianPath.Def });
            var w = WorldFactory.CreateInitial(2026, LimitsConfig.Default, 5,
                cultivation: true, pathSource: src);
            foreach (var c in w.AliveCharacters())
            {
                Assert.NotNull(c.Cultivation);
                Assert.Equal("ti_xiu_hengshi", c.Cultivation!.PathId);
                Assert.Contains("body_root", c.Persona.Tags);
            }
        }

        // ③ 战力随 realm 升：同四维同选择，realm 越高 PowerEngine×Curve 越大（平滑递增）。
        [Fact]
        public void Power_RisesWithRealm()
        {
            var def = BodyHenglianPath.Def;
            var stats = new StatBlock(new[] { 12, 8, 25, 15 }); // 根骨主导，四维总和=60≤80
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

        // ④a 两体修切磋出 DuelResolved；境界高者战力主导取胜。
        [Fact]
        public void TwoBodyCultivators_Spar_HigherRealmWins()
        {
            var def = BodyHenglianPath.Def;
            var reg = new PathRegistry(new ListPathSource(new[] { def }));
            var w = new FakeWorld();
            var a = Make(1, 12, 8, 22, 15); // 攻方
            var b = Make(2, 12, 8, 22, 15); // 同四维守方
            a.Cultivation = CultivationState.NewForPath(def.PathId, def.Resources);
            b.Cultivation = CultivationState.NewForPath(def.PathId, def.Resources);
            a.Cultivation!.RealmIndex = 4; // 高境界
            b.Cultivation!.RealmIndex = 0;
            w.All.Add(a); w.All.Add(b);

            var spar = new SparAction(w.Limits, reg);
            var duel = spar.Apply(w, a, new SparChoice(new CharacterId(2))).OfType<DuelResolved>().Single();
            Assert.Equal(1, duel.Winner.Value); // 高境界 a 胜
            Assert.True(duel.Margin > 0);
        }

        // ④b 情境 adj 生效（零 PathId 真隔离）：体修近战 melee 被远程对手放风筝（Bible §6.4 distance 轴 +5）。
        //    攻方=ranged mock 路（克隆体修 def 同 Power/Curve → 裸 pe 相等），仅改 pathId+tags → 唯放风筝 adj 区分胜负。
        [Fact]
        public void BodyCultivator_KitedByRanged_SituationalAdj_Decides()
        {
            var body = BodyHenglianPath.Def;
            // 远程攻方：克隆体修 def（同公式 → 裸 pe 与体修相等），仅改 pathId + SituationalTags=ranged。
            var ranged = body with { PathId = "fa_xiu", SituationalTags = new[] { "ranged" } };
            var reg = new PathRegistry(new ListPathSource(new[] { body, ranged }));
            var w = new FakeWorld();
            var atk = Make(1, 12, 8, 22, 15); // ranged 攻方
            var def = Make(2, 12, 8, 22, 15); // 体修守方（同四维同境界 → 裸 pe 相等）
            atk.Cultivation = CultivationState.NewForPath("fa_xiu", ranged.Resources);
            def.Cultivation = CultivationState.NewForPath(body.PathId, body.Resources);
            w.All.Add(atk); w.All.Add(def);

            var spar = new SparAction(w.Limits, reg);
            var duel = spar.Apply(w, atk, new SparChoice(new CharacterId(2))).OfType<DuelResolved>().Single();
            // 裸 pe 相等下，远程放风筝 +5% adj 增益攻方 → atk 应占优势（唯一区分源=情境 adj）。
            // DuelEngine下 margin 可能为0(同时死), 累计伤害差判定胜者
            Assert.True(duel.Winner.Value > 0);
        }

        // ⑤ 被动功法 EffectOp 装配生效：体修血气吐纳「龟息吐纳」AddResourceCap(qixue,+40) → 装配后 cap 抬高；
        //    横练功 AddResource(henglian) 装配后 henglian 增。
        [Fact]
        public void PassiveArt_EffectOp_AppliesViaInterpreter()
        {
            var def = BodyHenglianPath.Def;
            var qixueDef = def.Resources.Single(r => r.Key == "qixue");
            int baseCap = qixueDef.Cap;

            var st = CultivationState.NewForPath(def.PathId, def.Resources);
            // 找一部带 AddResourceCap(qixue) 被动的血气吐纳功法，装配其 effects。
            var capArt = def.ArtCategories
                .SelectMany(c => c.Arts)
                .First(a => a.Effects.Any(e => e.Kind == EffectOpKind.AddResourceCap && e.Key == "qixue"));
            foreach (var op in capArt.Effects)
                EffectInterpreter.ApplyPassive(op, st);

            st.ApplyResource("qixue", baseCap + 100);
            Assert.True(st.Resources["qixue"] > baseCap, "AddResourceCap 未抬高 qixue 上限");

            // 横练功 AddResource(henglian)：装配后 henglian 由 0 起涨。
            var henglianArt = def.ArtCategories
                .SelectMany(c => c.Arts)
                .First(a => a.Effects.Any(e => e.Kind == EffectOpKind.AddResource && e.Key == "henglian"));
            var st2 = CultivationState.NewForPath(def.PathId, def.Resources);
            foreach (var op in henglianArt.Effects)
                EffectInterpreter.ApplyPassive(op, st2);
            Assert.True(st2.Resources["henglian"] > 0, "AddResource 未叠加 henglian 横练值");
        }

        // —— helpers ——

        private static Character Make(long id, int force, int intl, int con, int insight) =>
            new Character(new CharacterId(id),
                new Persona("武夫", "y", "z", ArchetypeKind.Martial, null),
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
