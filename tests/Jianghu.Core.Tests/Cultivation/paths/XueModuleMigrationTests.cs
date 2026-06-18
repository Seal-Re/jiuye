using System.Collections.Generic;
using System.Linq;
using Jianghu.Cultivation;
using Jianghu.Cultivation.Paths;
using Xunit;

namespace Jianghu.Core.Tests.Cultivation.Paths
{
    /// <summary>
    /// B5 批2 血修招牌招 Note→Modules 工厂 差分测试（§10 第1+2条）。
    /// 映射(§15.9/plan)：燃血狂屠 → PenFromResource(qixie,4)（血气本钱转穿透,血气越满越痛、见底哑火真差分）；
    /// 噬尽夺元 → 改stat(Internal/Force) FULLSTRUCT defer（ApplyStat chokepoint 未建,保 FlatPen 占位）。
    /// </summary>
    public class XueModuleMigrationTests
    {
        static CombatSkillDef Skill(string id) =>
            XueXiuXueshaPath.Def.CombatSkills.Single(s => s.Id == id);

        static CombatContext Ctx(int qixie)
        {
            var atk = CultivationState.NewForPath("xue_xiu_xuesha",
                new[] { new ResourceDef("qixie", 0, 1000, qixie) });
            var def = CultivationState.NewForPath("def_path", new List<ResourceDef>());
            var path = XueXiuXueshaPath.Def;
            return new CombatContext(atk, path, def, path);
        }

        static int Resolve(CombatSkillDef sk, int baseDmg, CombatContext ctx)
        {
            int dmg = baseDmg;
            foreach (var op in sk.OnUse)
                dmg = ModuleResolver.ApplyOnUse(dmg, op, ctx);
            return dmg;
        }

        // —— 燃血狂屠：PenFromResource(qixie,4)。血气满载单点透支越痛（真差分）——
        [Fact]
        public void KuangTu_ScalesWithQixie()
        {
            var sk = Skill("sk_xue_kuangtu");
            Assert.Contains(sk.OnUse, o => o.Kind == EffectOpKind.PenFromResource && o.Key == "qixie");

            int full = Resolve(sk, 0, Ctx(qixie: 30)); // 30×4 = 120
            int empty = Resolve(sk, 0, Ctx(qixie: 0)); // 0
            Assert.Equal(30 * 4, full);
            Assert.Equal(0, empty);
            Assert.True(full > empty, "燃血狂屠未随 qixie 缩放（仍是占位定值）");
        }

        // —— 噬尽夺元：B5补缺 → DrainResource（双方均有 qixie key 时生效，跨路静默跳过）——
        [Fact]
        public void DuoYuan_UpgradedToDrainResource()
        {
            var sk = Skill("sk_xue_duoyuan");
            Assert.Contains(sk.OnUse, o => o.Kind == EffectOpKind.DrainResource);
            Assert.Contains(sk.OnUse, o => o.Key == "qixie" && o.Amount == 8);
        }

        [Fact]
        public void Def_StillValidAfterMigration()
        {
            PathValidator.AssertValid(XueXiuXueshaPath.Def);
        }
    }
}
