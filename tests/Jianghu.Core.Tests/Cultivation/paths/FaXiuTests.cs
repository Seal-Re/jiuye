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
    /// Task 4 续（法修）：法修 fa_xiu（physical 中远程·元素代表）端到端。
    /// ①PathValidator 过 + canon/shape；②生成期带 fa_root 定到 fa_xiu；③战力随 realm 升（平滑曲线）；
    /// ④两法修切磋出 DuelResolved + 情境 adj 生效（ranged 法修放风筝 melee 守方,零 PathId 真隔离）；
    /// ⑤被动功法 EffectOp（AddResourceCap manaPool / AddResource spellBreadth）装配生效。
    /// 元素相生克边数据另由 SituationalEdges 测试守（element 轴 counterWheel）。
    /// </summary>
    public class FaXiuTests
    {
        // ① 数据质量 gate：法修 Def 必过 PathValidator 全部校验。
        [Fact]
        public void Def_PassesPathValidator()
        {
            PathValidator.AssertValid(FaXiuPath.Def); // 不抛即过
        }

        // ① 附加：canon pathId + 含 daoheart 类目（spellheart=法心）+ terms 无 daoHeart/×0 + 属性 tag。
        [Fact]
        public void Def_IsCanonAndShaped()
        {
            var d = FaXiuPath.Def;
            Assert.Equal("fa_xiu", d.PathId);
            Assert.Contains(d.ArtCategories, c => c.Role == "daoheart");
            Assert.True(d.ArtCategories.Count >= 3);
            Assert.All(d.ArtCategories, c => Assert.True(c.Arts.Count >= 4));
            Assert.True(d.CombatSkills.Count >= 5);
            Assert.All(d.Power.Terms, t => Assert.NotEqual(0, t.Weight));
            Assert.DoesNotContain(d.Power.Terms, t => t.Src.Contains("daoHeart") || t.Src.Contains("innerDemon"));
            // SituationalTags = 属性/形态/元素 tag（非对手 PathId）。
            Assert.Equal(new[] { "ranged", "elemental", "fire" }, d.SituationalTags);
        }

        // ② 生成期：唯法修一路注册 → 派生池仅 fa_root（唯一 entry tag 约定）→ 人人带 fa_root → 人人定到 fa_xiu。
        [Fact]
        public void Generation_AssignsFaXiu_OnFaRoot()
        {
            var src = new ListPathSource(new[] { FaXiuPath.Def });
            var w = WorldFactory.CreateInitial(2026, LimitsConfig.Default, 5,
                cultivation: true, pathSource: src);
            foreach (var c in w.AliveCharacters())
            {
                Assert.NotNull(c.Cultivation);
                Assert.Equal("fa_xiu", c.Cultivation!.PathId);
                Assert.Contains("fa_root", c.Persona.Tags);
            }
        }

        // ③ 战力随 realm 升：同四维同选择，realm 越高 PowerEngine×Curve 越大（平滑递增）。
        [Fact]
        public void Power_RisesWithRealm()
        {
            var def = FaXiuPath.Def;
            var stats = new StatBlock(new[] { 8, 25, 12, 15 }); // 内力主导，四维总和=60≤80
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

        // ④a 两法修切磋出 DuelResolved；境界高者战力主导取胜。
        [Fact]
        public void TwoFaXiu_Spar_HigherRealmWins()
        {
            var def = FaXiuPath.Def;
            var reg = new PathRegistry(new ListPathSource(new[] { def }));
            var w = new FakeWorld();
            var a = Make(1, 8, 22, 12, 15); // 攻方
            var b = Make(2, 8, 22, 12, 15); // 同四维守方
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

        // ④b 情境 adj 生效（零 PathId 真隔离）：法修 ranged 放风筝近战 melee 守方（Bible §6.4 distance 轴 +5）。
        //    守方=melee mock 路（克隆法修 def 同 Power/Curve → 裸 pe 相等），仅改 pathId+tags → 唯放风筝 adj 区分胜负。
        [Fact]
        public void FaXiu_KitesMelee_SituationalAdj_Decides()
        {
            var fa = FaXiuPath.Def;
            // 近战守方：克隆法修 def（同公式 → 裸 pe 与法修相等），仅改 pathId + SituationalTags=melee。
            var melee = fa with { PathId = "ti_xiu_hengshi", SituationalTags = new[] { "melee" } };
            var reg = new PathRegistry(new ListPathSource(new[] { fa, melee }));
            var w = new FakeWorld();
            var atk = Make(1, 8, 22, 12, 15); // 法修 ranged 攻方
            var def = Make(2, 8, 22, 12, 15); // melee 守方（同四维同境界 → 裸 pe 相等）
            atk.Cultivation = CultivationState.NewForPath(fa.PathId, fa.Resources);
            def.Cultivation = CultivationState.NewForPath("ti_xiu_hengshi", melee.Resources);
            // 同境界 realm4：法修曲线低阶倍率小(realm0 mul=1，pe÷10 截断会吞掉+5%)，取 realm4(mul=11)
            //   使放大后 pe 足够大，+5% adj 整数截断后仍可观测（同境界 → 裸 pe 仍相等，唯 adj 区分）。
            atk.Cultivation!.RealmIndex = 4;
            def.Cultivation!.RealmIndex = 4;
            w.All.Add(atk); w.All.Add(def);

            var spar = new SparAction(w.Limits, reg);
            var duel = spar.Apply(w, atk, new SparChoice(new CharacterId(2))).OfType<DuelResolved>().Single();
            // 裸 pe 相等下，法修远程放风筝 +5% adj 增益攻方 → atk 胜（唯一区分源=情境 adj）。
            Assert.Equal(1, duel.Winner.Value);
            Assert.True(duel.Margin > 0);
        }

        // ④c 元素相克 adj 生效（零 PathId 真隔离）：法修主灵根 fire，canon 火克木 → 守方 wood 吃 +15% adj。
        //    守方=wood mock 路（克隆法修 def 同 Power/Curve → 裸 pe 相等），仅改 pathId+tags=wood → 唯元素相克区分胜负。
        //    反向不命中（wood 攻方 vs fire 守方 → 木克雷边不中，法修守方无 thunder tag）→ 守方 adj=0，真隔离。
        [Fact]
        public void FaXiu_ElementCounter_SituationalAdj_Decides()
        {
            var fa = FaXiuPath.Def; // 法修 fire 攻方（SituationalTags 含 fire）
            // 守方 wood：克隆法修 def（同公式 → 裸 pe 与法修相等），仅改 pathId + SituationalTags=wood（被火克）。
            var wood = fa with { PathId = "ti_xiu_hengshi", SituationalTags = new[] { "wood" } };
            var reg = new PathRegistry(new ListPathSource(new[] { fa, wood }));
            var w = new FakeWorld();
            var atk = Make(1, 8, 22, 12, 15); // 法修 fire 攻方
            var def = Make(2, 8, 22, 12, 15); // wood 守方（同四维同境界 → 裸 pe 相等）
            atk.Cultivation = CultivationState.NewForPath(fa.PathId, fa.Resources);
            def.Cultivation = CultivationState.NewForPath("ti_xiu_hengshi", wood.Resources);
            // 同境界 realm4（mul=11）：放大后 pe 足够大，+15% adj 整数截断后仍可观测；裸 pe 仍相等，唯元素相克区分。
            atk.Cultivation!.RealmIndex = 4;
            def.Cultivation!.RealmIndex = 4;
            w.All.Add(atk); w.All.Add(def);

            var spar = new SparAction(w.Limits, reg);
            var duel = spar.Apply(w, atk, new SparChoice(new CharacterId(2))).OfType<DuelResolved>().Single();
            // 裸 pe 相等下，法修 fire 克 wood +15% adj 增益攻方 → atk 胜（唯一区分源=元素相克 adj）。
            Assert.Equal(1, duel.Winner.Value);
            Assert.True(duel.Margin > 0);
        }

        // ⑤ 被动功法 EffectOp 装配生效：法修符印「聚灵符」AddResourceCap(manaPool,+15) → 装配后 cap 抬高；
        //    心法/法术 AddResource(spellBreadth) 装配后 spellBreadth 增（百搭核心广度）。
        [Fact]
        public void PassiveArt_EffectOp_AppliesViaInterpreter()
        {
            var def = FaXiuPath.Def;
            var manaDef = def.Resources.Single(r => r.Key == "manaPool");
            int baseCap = manaDef.Cap;

            var st = CultivationState.NewForPath(def.PathId, def.Resources);
            // 找一部带 AddResourceCap(manaPool) 被动的符印/心法功法，装配其 effects。
            var capArt = def.ArtCategories
                .SelectMany(c => c.Arts)
                .First(a => a.Effects.Any(e => e.Kind == EffectOpKind.AddResourceCap && e.Key == "manaPool"));
            foreach (var op in capArt.Effects)
                EffectInterpreter.ApplyPassive(op, st);

            st.ApplyResource("manaPool", baseCap + 100);
            Assert.True(st.Resources["manaPool"] > baseCap, "AddResourceCap 未抬高 manaPool 上限");

            // 法术/心法 AddResource(spellBreadth)：装配后 spellBreadth 由初值(1)起涨。
            var breadthArt = def.ArtCategories
                .SelectMany(c => c.Arts)
                .First(a => a.Effects.Any(e => e.Kind == EffectOpKind.AddResource && e.Key == "spellBreadth"));
            var st2 = CultivationState.NewForPath(def.PathId, def.Resources);
            int baseBreadth = st2.Resources["spellBreadth"];
            foreach (var op in breadthArt.Effects)
                EffectInterpreter.ApplyPassive(op, st2);
            Assert.True(st2.Resources["spellBreadth"] > baseBreadth, "AddResource 未叠加 spellBreadth 法术库广度");
        }

        // —— helpers ——

        private static Character Make(long id, int force, int intl, int con, int insight) =>
            new Character(new CharacterId(id),
                new Persona("术士", "y", "z", ArchetypeKind.Martial, null),
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
