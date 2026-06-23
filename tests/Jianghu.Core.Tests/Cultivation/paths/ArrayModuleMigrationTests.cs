using System.Collections.Generic;
using System.Linq;
using Jianghu.Cultivation;
using Jianghu.Cultivation.Paths;
using Xunit;

namespace Jianghu.Core.Tests.Cultivation.Paths
{
    /// <summary>
    /// B5 批2 阵修招牌招 Note→Modules 工厂 差分测试（§10 第1+2条）。
    /// 映射(§15.9/plan)：困龙·锁 → Control（锁定身,控场结算批4接）；
    /// 炸阵(引爆·焚阵)/Σ阵(算尽·叠杀) → 批3 Special / derived 聚合（本批标注 deferred）。
    /// </summary>
    public class ArrayModuleMigrationTests
    {
        static CombatSkillDef Skill(string id) =>
            ArrayFormationPath.Def.CombatSkills.Single(s => s.Id == id);

        static CombatContext Ctx()
        {
            var atk = CultivationState.NewForPath("array_formation",
                new[] { new ResourceDef("compute", 0, 60, 0), new ResourceDef("stones", 0, 100, 0), new ResourceDef("setupProgress", 0, 220, 0) });
            var def = CultivationState.NewForPath("def_path", new List<ResourceDef>());
            var path = ArrayFormationPath.Def;
            return new CombatContext(atk, path, def, path);
        }

        static int Resolve(CombatSkillDef sk, int baseDmg, CombatContext ctx)
        {
            int dmg = baseDmg;
            foreach (var op in sk.OnUse) dmg = ModuleResolver.ApplyOnUse(dmg, op, ctx);
            return dmg;
        }

        // —— 困龙·锁：Control（控场积木）——
        [Fact]
        public void KunLong_IsControl()
        {
            var sk = Skill("sk_ar_kunlong");
            Assert.Contains(sk.OnUse, o => o.Kind == EffectOpKind.Control);
        }

        // —— 算尽·叠杀：FlatPen(60) 基线破防量 ——
        [Fact]
        public void SuanJin_IsFlatPen_DamageCalc()
        {
            var sk = Skill("sk_ar_suanjin");
            Assert.Contains(sk.OnUse, o => o.Kind == EffectOpKind.AddPenInteger);
            int dmg = Resolve(sk, 0, Ctx());
            Assert.Equal(60, dmg);
        }

        // —— 引爆·焚阵：FlatPen(40) 基线破防量 ——
        [Fact]
        public void YinBao_IsFlatPen_DamageCalc()
        {
            var sk = Skill("sk_ar_yinbao");
            Assert.Contains(sk.OnUse, o => o.Kind == EffectOpKind.AddPenInteger);
            int dmg = Resolve(sk, 0, Ctx());
            Assert.Equal(40, dmg);
        }

        // —— 阵遁·移形：Evade(25) 闪避积木 ——
        [Fact]
        public void ZhenDun_IsEvade()
        {
            var sk = Skill("sk_ar_zhendun");
            Assert.Contains(sk.OnUse, o => o.Kind == EffectOpKind.Evade && o.Amount == 25);
        }

        [Fact]
        public void Def_StillValidAfterMigration()
        {
            PathValidator.AssertValid(ArrayFormationPath.Def);
        }
    }
}
