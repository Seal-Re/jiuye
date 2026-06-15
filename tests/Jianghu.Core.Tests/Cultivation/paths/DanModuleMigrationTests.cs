using System.Collections.Generic;
using System.Linq;
using Jianghu.Cultivation;
using Jianghu.Cultivation.Paths;
using Xunit;

namespace Jianghu.Core.Tests.Cultivation.Paths
{
    /// <summary>
    /// B5 批2 Task2.5：丹修招牌招 Note→Modules 工厂模块 差分测试（§10 第1+2条 + §15.9 边界）。
    /// 映射(§15.9/plan)：炸炉自爆 → PenFromResource(flameTier,N)（异火阶越高炉越猛,占位战斗）。
    /// 注：plan 原写 PenFromResource(directPower)，但 directPower 非本路资源(只 flameTier/recipeCount/
    /// pillStock)，故炸炉锚到 flameTier（真资源·语义=异火引爆）。改人造网(夺元/施丹改stat造边)非战斗机制、
    /// 算子集缺 → 显式 deferred FULLSTRUCT（红线 A.8 不静默，留 Note）。
    /// </summary>
    public class DanModuleMigrationTests
    {
        static CombatSkillDef Skill(string id) =>
            DanXiuPath.Def.CombatSkills.Single(s => s.Id == id);

        static CombatContext Ctx(int flameTier)
        {
            var atk = CultivationState.NewForPath("dan_xiu",
                new[] { new ResourceDef("flameTier", 0, 1000, flameTier) });
            var def = CultivationState.NewForPath("def_path", new List<ResourceDef>());
            var path = DanXiuPath.Def;
            return new CombatContext(atk, path, def, path);
        }

        static int Resolve(CombatSkillDef sk, int baseDmg, CombatContext ctx)
        {
            int dmg = baseDmg;
            foreach (var op in sk.OnUse)
                dmg = ModuleResolver.ApplyOnUse(dmg, op, ctx);
            return dmg;
        }

        // —— 炸炉自爆：PenFromResource(flameTier)。异火阶越高炉越猛（占位战斗,真差分）——
        [Fact]
        public void ZhaLu_ScalesWithFlameTier()
        {
            var sk = Skill("sk_da_zhalu");
            Assert.Contains(sk.OnUse, o => o.Kind == EffectOpKind.PenFromResource && o.Key == "flameTier");

            int hot = Resolve(sk, 0, Ctx(flameTier: 8));  // 高异火阶
            int cold = Resolve(sk, 0, Ctx(flameTier: 0)); // 无异火
            Assert.True(hot > cold, "炸炉自爆未随 flameTier 缩放");
        }

        [Fact]
        public void Def_StillValidAfterMigration()
        {
            PathValidator.AssertValid(DanXiuPath.Def);
        }
    }
}
