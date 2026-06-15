using System.Collections.Generic;
using System.Linq;
using Jianghu.Cultivation;
using Jianghu.Cultivation.Paths;
using Xunit;

namespace Jianghu.Core.Tests.Cultivation.Paths
{
    /// <summary>
    /// B5 批2 魂修招牌招 Note→Modules 工厂 差分测试（§10 第1+2条）。
    /// 映射(§15.9/plan)：焚魂自爆 → PenFromResource(soulForce,2)（魂力自爆,越满越痛）；
    /// 夺舍 → 批3 Special（濒死续命,本批标注 deferred）。
    /// </summary>
    public class SoulModuleMigrationTests
    {
        static CombatSkillDef Skill(string id) =>
            SoulDivineSensePath.Def.CombatSkills.Single(s => s.Id == id);

        static CombatContext Ctx(int soulForce)
        {
            var atk = CultivationState.NewForPath("soul_divine_sense",
                new[] { new ResourceDef("soulForce", 0, 1000, soulForce) });
            var def = CultivationState.NewForPath("def_path", new List<ResourceDef>());
            var path = SoulDivineSensePath.Def;
            return new CombatContext(atk, path, def, path);
        }

        static int Resolve(CombatSkillDef sk, int baseDmg, CombatContext ctx)
        {
            int dmg = baseDmg;
            foreach (var op in sk.OnUse) dmg = ModuleResolver.ApplyOnUse(dmg, op, ctx);
            return dmg;
        }

        // —— 焚魂自爆：PenFromResource(soulForce,2)。魂力满载自爆越痛（真差分）——
        [Fact]
        public void FenHun_ScalesWithSoulForce()
        {
            var sk = Skill("sk_so_fenhun");
            Assert.Contains(sk.OnUse, o => o.Kind == EffectOpKind.PenFromResource && o.Key == "soulForce");
            int full = Resolve(sk, 0, Ctx(soulForce: 30)); // 30×2 = 60
            int empty = Resolve(sk, 0, Ctx(soulForce: 0));
            Assert.Equal(30 * 2, full);
            Assert.Equal(0, empty);
            Assert.True(full > empty, "焚魂自爆未随 soulForce 缩放");
        }

        [Fact]
        public void Def_StillValidAfterMigration()
        {
            PathValidator.AssertValid(SoulDivineSensePath.Def);
        }
    }
}
