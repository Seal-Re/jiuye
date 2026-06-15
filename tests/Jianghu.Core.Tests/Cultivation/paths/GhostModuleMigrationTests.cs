using System.Collections.Generic;
using System.Linq;
using Jianghu.Cultivation;
using Jianghu.Cultivation.Paths;
using Xunit;

namespace Jianghu.Core.Tests.Cultivation.Paths
{
    /// <summary>
    /// B5 批2 Task2.4：鬼修招牌招 Note→Modules 工厂模块 差分测试（§10 第1+2条）。
    /// 映射(§15.9/plan)：幽冥煞爆 → PenFromResource(shaCharge,2)（煞值自爆,带电量越满越痛）；
    /// 勾魂索命 → Control（定身一动作,控场结算批4接,本轮ApplyOnUse不改dmg）。
    /// </summary>
    public class GhostModuleMigrationTests
    {
        static CombatSkillDef Skill(string id) =>
            GhostYangHunPath.Def.CombatSkills.Single(s => s.Id == id);

        static CombatContext Ctx(int shaCharge)
        {
            var atk = CultivationState.NewForPath("gui_xiu_yang_hun",
                new[] { new ResourceDef("shaCharge", 0, 1000, shaCharge) });
            var def = CultivationState.NewForPath("def_path", new List<ResourceDef>());
            var path = GhostYangHunPath.Def;
            return new CombatContext(atk, path, def, path);
        }

        static int Resolve(CombatSkillDef sk, int baseDmg, CombatContext ctx)
        {
            int dmg = baseDmg;
            foreach (var op in sk.OnUse)
                dmg = ModuleResolver.ApplyOnUse(dmg, op, ctx);
            return dmg;
        }

        // —— 幽冥煞爆：PenFromResource(shaCharge,2)。煞值满载自爆越痛（真差分）——
        [Fact]
        public void ShaBao_ScalesWithShaCharge()
        {
            var sk = Skill("sk_gu_shabao");
            Assert.Contains(sk.OnUse, o => o.Kind == EffectOpKind.PenFromResource && o.Key == "shaCharge");

            int full = Resolve(sk, 0, Ctx(shaCharge: 40)); // 40×2 = 80
            int empty = Resolve(sk, 0, Ctx(shaCharge: 0)); // 0
            Assert.Equal(40 * 2, full);
            Assert.Equal(0, empty);
            Assert.True(full > empty, "幽冥煞爆未随 shaCharge 缩放");
        }

        // —— 勾魂索命：Control（定身,控场积木）——
        [Fact]
        public void SuoMing_IsControl()
        {
            var sk = Skill("sk_gu_suoming");
            Assert.Contains(sk.OnUse, o => o.Kind == EffectOpKind.Control);
        }

        [Fact]
        public void Def_StillValidAfterMigration()
        {
            PathValidator.AssertValid(GhostYangHunPath.Def);
        }
    }
}
