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
    /// Task 4 续（鬼修）：鬼修 gui_xiu_yang_hun（spirit 鬼系代表）端到端。
    /// ①PathValidator 过 + canon/shape（spirit AttackDimension + 根骨负权 + 无 ×0/realm 项）；
    /// ②生成期带 ghost_root 定到 gui_xiu_yang_hun；③战力随 realm 升（高爆发陡曲线）；
    /// ④两鬼修切磋出 DuelResolved + 情境 adj 生效（spirit_attack 绕物防 body 守方,零 PathId 真隔离）；
    /// ④c 昼夜边经 SituationalResolver 直喂 env 验证（is_night=1 & ghost → +adj，SparAction A.0 env 暂空故另测）；
    /// ⑤被动功法 EffectOp（AddResourceCap shaCharge / AddResource ghostSoldierPower/devourMeter）装配生效。
    /// </summary>
    public class GhostYangHunTests
    {
        // ① 数据质量 gate：鬼修 Def 必过 PathValidator 全部校验。
        [Fact]
        public void Def_PassesPathValidator()
        {
            PathValidator.AssertValid(GhostYangHunPath.Def); // 不抛即过
        }

        // ① 附加：canon pathId + spirit AttackDimension + 含 daoheart 类目（ghostheart=鬼心）+ terms 无 daoHeart/×0
        //    + 根骨负权签名机制 + 属性 tag。
        [Fact]
        public void Def_IsCanonAndShaped()
        {
            var d = GhostYangHunPath.Def;
            Assert.Equal("gui_xiu_yang_hun", d.PathId);
            Assert.Equal("spirit", d.AttackDimension);
            Assert.Contains(d.ArtCategories, c => c.Role == "daoheart");
            Assert.True(d.ArtCategories.Count >= 3);
            Assert.All(d.ArtCategories, c => Assert.True(c.Arts.Count >= 4));
            Assert.True(d.CombatSkills.Count >= 5);
            // 无 ×0 项（R6）；含根骨负权签名机制（弃肉身）。
            Assert.All(d.Power.Terms, t => Assert.NotEqual(0, t.Weight));
            Assert.Contains(d.Power.Terms, t => t.Src == "stat:Constitution" && t.Weight < 0);
            Assert.DoesNotContain(d.Power.Terms, t => t.Src.Contains("daoHeart") || t.Src.Contains("innerDemon"));
            // SituationalTags = spirit 属性/形态 tag（非对手 PathId）。
            Assert.Equal(new[] { "spirit_attack", "ghost", "evil" }, d.SituationalTags);
        }

        // ② 生成期：唯鬼修一路注册 → 派生池仅 ghost_root → 人人带 ghost_root → 人人定到 gui_xiu_yang_hun。
        [Fact]
        public void Generation_AssignsGhostYangHun_OnGhostRoot()
        {
            var src = new ListPathSource(new[] { GhostYangHunPath.Def });
            var w = WorldFactory.CreateInitial(2026, LimitsConfig.Default, 5,
                cultivation: true, pathSource: src);
            foreach (var c in w.AliveCharacters())
            {
                Assert.NotNull(c.Cultivation);
                Assert.Equal("gui_xiu_yang_hun", c.Cultivation!.PathId);
                Assert.Contains("ghost_root", c.Persona.Tags);
            }
        }

        // ③ 战力随 realm 升：同四维同选择，realm 越高 PowerEngine×Curve 越大（高爆发陡曲线递增）。
        //    根骨负权下 BaseSum 仍正（内力+悟性主导），经 Clamp(0,cap) 不会负 final。
        [Fact]
        public void Power_RisesWithRealm()
        {
            var def = GhostYangHunPath.Def;
            var stats = new StatBlock(new[] { 5, 25, 8, 22 }); // 内力/悟性主导(魂力源),根骨低,四维总和=60≤80
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

        // ④a 两鬼修切磋出 DuelResolved；境界高者战力主导取胜。
        [Fact]
        public void TwoGhostCultivators_Spar_HigherRealmWins()
        {
            var def = GhostYangHunPath.Def;
            var reg = new PathRegistry(new ListPathSource(new[] { def }));
            var w = new FakeWorld();
            var a = Make(1, 5, 22, 8, 20); // 攻方
            var b = Make(2, 5, 22, 8, 20); // 同四维守方
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

        // ④b 情境 adj 生效（零 PathId 真隔离·spirit 绕物防）：鬼修 spirit_attack 绕过 body 守方物理罩门
        //    （新增 spirit 轴边 +12）。守方=body mock 路（克隆鬼修 def 同 Power/Curve → 裸 pe 相等），仅改 pathId+tags。
        //    同境界 realm4（鬼修曲线低阶倍率小 pe÷10 截断会吞 adj，realm4 mul=12 放大后 +12% 整数截断仍可观测）。
        [Fact]
        public void GhostCultivator_PiercesBodyArmor_SpiritAdj_Decides()
        {
            var gui = GhostYangHunPath.Def;
            // 肉身守方：克隆鬼修 def（同公式 → 裸 pe 与鬼修相等），仅改 pathId + SituationalTags=body。
            var body = gui with { PathId = "ti_xiu_hengshi", SituationalTags = new[] { "body" } };
            var reg = new PathRegistry(new ListPathSource(new[] { gui, body }));
            var w = new FakeWorld();
            var atk = Make(1, 5, 22, 8, 20); // 鬼修 spirit_attack 攻方
            var def = Make(2, 5, 22, 8, 20); // body 守方（同四维同境界 → 裸 pe 相等）
            atk.Cultivation = CultivationState.NewForPath(gui.PathId, gui.Resources);
            def.Cultivation = CultivationState.NewForPath("ti_xiu_hengshi", body.Resources);
            atk.Cultivation!.RealmIndex = 4;
            def.Cultivation!.RealmIndex = 4;
            w.All.Add(atk); w.All.Add(def);

            var spar = new SparAction(w.Limits, reg);
            var duel = spar.Apply(w, atk, new SparChoice(new CharacterId(2))).OfType<DuelResolved>().Single();
            // 裸 pe 相等下，魂力绕物防 +12% adj 增益攻方 → atk 胜（唯一区分源=spirit 轴情境 adj）。
            Assert.Equal(1, duel.Winner.Value);
            Assert.True(duel.Margin > 0);
        }

        // ④c 昼夜边经 SituationalResolver 直喂 env 验证（SparAction A.0 env 暂空，故此边在切磋不命中，另测）。
        //    is_night=1 & 攻方 ghost → +adj（昼弱夜强落整数）；白昼(is_night=0)同攻方则不命中（昼弱）。
        [Fact]
        public void DayNightEdge_NightGhostGetsAdj_DayDoesNot()
        {
            var r = new SituationalResolver(SituationalEdges.Default);
            var night = new Dictionary<string, string> { { "is_night", "1" } };
            var day = new Dictionary<string, string> { { "is_night", "0" } };
            // 鬼修 SituationalTags 含 ghost；spirit axis（绕物防边需 body 守方,此处守方裸 → 仅昼夜边纳入）。
            var atkTags = GhostYangHunPath.Def.SituationalTags; // [spirit_attack, ghost, evil]

            int nightAdj = r.AdjPct(new SitContext(atkTags, System.Array.Empty<string>(), "spirit", night), p0: 400);
            int dayAdj = r.AdjPct(new SitContext(atkTags, System.Array.Empty<string>(), "spirit", day), p0: 400);
            Assert.True(nightAdj > 0, "入夜 & ghost 攻方应得昼夜 +adj（夜强）");
            Assert.Equal(0, dayAdj); // 白昼此边不命中（昼弱）
        }

        // ⑤ 被动功法 EffectOp 装配生效：鬼修煞气「阴煞结丹诀」AddResourceCap(shaCharge,+20) → 装配后 cap 抬高；
        //    养魂 AddResource(ghostSoldierPower) 装配后鬼兵之力增、噬主度 devourMeter 涨（养鬼噬主账本）。
        [Fact]
        public void PassiveArt_EffectOp_AppliesViaInterpreter()
        {
            var def = GhostYangHunPath.Def;
            var shaDef = def.Resources.Single(r => r.Key == "shaCharge");
            int baseCap = shaDef.Cap;

            var st = CultivationState.NewForPath(def.PathId, def.Resources);
            // 找一部带 AddResourceCap(shaCharge) 被动的煞气功法，装配其 effects。
            var capArt = def.ArtCategories
                .SelectMany(c => c.Arts)
                .First(a => a.Effects.Any(e => e.Kind == EffectOpKind.AddResourceCap && e.Key == "shaCharge"));
            foreach (var op in capArt.Effects)
                EffectInterpreter.ApplyPassive(op, st);

            st.ApplyResource("shaCharge", baseCap + 100);
            Assert.True(st.Resources["shaCharge"] > baseCap, "AddResourceCap 未抬高 shaCharge 上限");

            // 养魂 AddResource(ghostSoldierPower)：装配后鬼兵之力由 0 起涨（养鬼噬主账本之鬼兵侧）。
            var summonArt = def.ArtCategories
                .SelectMany(c => c.Arts)
                .First(a => a.Effects.Any(e => e.Kind == EffectOpKind.AddResource && e.Key == "ghostSoldierPower"));
            var st2 = CultivationState.NewForPath(def.PathId, def.Resources);
            foreach (var op in summonArt.Effects)
                EffectInterpreter.ApplyPassive(op, st2);
            Assert.True(st2.Resources["ghostSoldierPower"] > 0, "AddResource 未叠加 ghostSoldierPower 鬼兵之力");
        }

        // —— helpers ——

        private static Character Make(long id, int force, int intl, int con, int insight) =>
            new Character(new CharacterId(id),
                new Persona("鬼修", "y", "z", ArchetypeKind.Martial, null),
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
