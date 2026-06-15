using System.Collections.Generic;
using System.Linq;
using Jianghu.Cultivation;
using Jianghu.Cultivation.Paths;
using Xunit;

namespace Jianghu.Core.Tests.Cultivation.Paths
{
    /// <summary>
    /// B5 批2 雷修招牌招 Note→Modules 工厂 差分测试（§10 第1+2条）。
    /// 映射(§15.9/plan)：普化天雷·诛邪 → CounterMul(evil,3)（灭阴×3,对阴邪联合上界倍乘,正道几乎无伤）；
    /// 引天劫 → AddPenInteger(基线)+Backlash（承雷自伤,自伤通道批4接）。
    /// </summary>
    public class LeiModuleMigrationTests
    {
        static CombatSkillDef Skill(string id) =>
            LeiXiuPath.Def.CombatSkills.Single(s => s.Id == id);

        static CombatContext Ctx(bool defenderEvil)
        {
            var atk = CultivationState.NewForPath("lei_xiu", new List<ResourceDef>());
            var def = CultivationState.NewForPath("def_path", new List<ResourceDef>());
            var atkPath = LeiXiuPath.Def;
            var defPath = defenderEvil ? TestPaths.WithTags(new[] { "evil" })
                                       : TestPaths.WithTags(System.Array.Empty<string>());
            return new CombatContext(atk, atkPath, def, defPath);
        }

        static int Resolve(CombatSkillDef sk, int baseDmg, CombatContext ctx)
        {
            int dmg = baseDmg;
            foreach (var o in sk.OnUse) dmg = ModuleResolver.ApplyOnUse(dmg, o, ctx);
            return dmg;
        }

        // —— 普化天雷：CounterMul(evil) 灭阴。对 evil ×3/2(联合上界),非 evil 不变（真差分）——
        [Fact]
        public void PuHua_CounterMul_VsEvil()
        {
            var sk = Skill("sk_le_puhua");
            Assert.Contains(sk.OnUse, o => o.Kind == EffectOpKind.CounterMul && o.Key == "evil");
            int vsEvil = Resolve(sk, 0, Ctx(defenderEvil: true));
            int vsNorm = Resolve(sk, 0, Ctx(defenderEvil: false));
            Assert.True(vsEvil > vsNorm, "普化天雷对 evil 未触发灭阴倍乘");
        }

        // —— 引天劫：Backlash（承雷自伤通道）——
        [Fact]
        public void YinTianJie_IsBacklash()
        {
            var sk = Skill("sk_le_yintianjie");
            Assert.Contains(sk.OnUse, o => o.Kind == EffectOpKind.Backlash);
        }

        [Fact]
        public void Def_StillValidAfterMigration()
        {
            PathValidator.AssertValid(LeiXiuPath.Def);
        }
    }
}
