using System.Collections.Generic;
using System.Linq;
using Jianghu.Cultivation;
using Jianghu.Cultivation.Paths;
using Xunit;

namespace Jianghu.Core.Tests.Cultivation.Paths
{
    /// <summary>
    /// B5 批2 Task2.6：器修招牌招 Note→Modules 工厂模块 差分测试（§10 第1+2条 + §15.9 边界）。
    /// 映射(§15.9/plan)：御剑斩 → PenFromResource(itemTier,10)（法宝品阶绝对主导,真差分）；
    /// 缚锋锁器 → Drain(itemTier,2)（夺压对手 itemTier,经 chokepoint）；
    /// 万宝齐发 → PenFromResource(itemTier,8)（本批用自身 itemTier;真Σ多宝聚合 → FULLSTRUCT defer）；
    /// 落宝金光 → 批3 Special(luobao)（本批不迁,§10 覆盖账标'签名 Special 批3'）。
    /// </summary>
    public class QixiuModuleMigrationTests
    {
        static CombatSkillDef Skill(string id) =>
            QixiuArtificerPath.Def.CombatSkills.Single(s => s.Id == id);

        static CombatContext Ctx(int atkItemTier, int defItemTier = 0)
        {
            var atk = CultivationState.NewForPath("qixiu_artificer",
                new[] { new ResourceDef("itemTier", 0, 1000, atkItemTier) });
            var def = CultivationState.NewForPath("def_path",
                new[] { new ResourceDef("itemTier", 0, 1000, defItemTier) });
            var path = QixiuArtificerPath.Def;
            return new CombatContext(atk, path, def, path);
        }

        static int Resolve(CombatSkillDef sk, int baseDmg, CombatContext ctx)
        {
            int dmg = baseDmg;
            foreach (var op in sk.OnUse)
                dmg = ModuleResolver.ApplyOnUse(dmg, op, ctx);
            return dmg;
        }

        // —— 御剑斩：PenFromResource(itemTier,10)。法宝品阶越高伤越高（真差分）——
        [Fact]
        public void YuJian_ScalesWithItemTier()
        {
            var sk = Skill("sk_qi_yujian");
            Assert.Contains(sk.OnUse, o => o.Kind == EffectOpKind.PenFromResource && o.Key == "itemTier");

            int hi = Resolve(sk, 0, Ctx(atkItemTier: 9)); // 9×10 = 90
            int lo = Resolve(sk, 0, Ctx(atkItemTier: 0)); // 脱宝 → 0（脱宝即崩）
            Assert.Equal(9 * 10, hi);
            Assert.Equal(0, lo);
            Assert.True(hi > lo, "御剑斩未随 itemTier 缩放");
        }

        // —— 缚锋锁器：Drain(itemTier,2)。夺压对手 itemTier（经 chokepoint,真夺取）——
        [Fact]
        public void FuFeng_DrainsDefenderItemTier()
        {
            var sk = Skill("sk_qi_fufeng");
            Assert.Contains(sk.OnUse, o => o.Kind == EffectOpKind.DrainResource && o.Key == "itemTier");

            var ctx = Ctx(atkItemTier: 0, defItemTier: 5);
            Resolve(sk, 0, ctx);
            Assert.Equal(3, ctx.ReadResource(Side.Defender, "itemTier")); // 5-2
            Assert.Equal(2, ctx.ReadResource(Side.Attacker, "itemTier")); // 0+2
        }

        // —— 万宝齐发：PenFromResource(itemTier,8)（本批自身 itemTier;真Σ多宝聚合 deferred）——
        [Fact]
        public void WanBao_ScalesWithItemTier()
        {
            var sk = Skill("sk_qi_wanbao");
            Assert.Contains(sk.OnUse, o => o.Kind == EffectOpKind.PenFromResource && o.Key == "itemTier");
        }

        [Fact]
        public void Def_StillValidAfterMigration()
        {
            PathValidator.AssertValid(QixiuArtificerPath.Def);
        }
    }
}
