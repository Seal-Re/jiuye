using System.Collections.Generic;
using System.Linq;
using Jianghu.Cultivation;
using Jianghu.Cultivation.Paths;
using Xunit;

namespace Jianghu.Core.Tests.Cultivation.Paths
{
    /// <summary>
    /// B5 批2 儒修招牌招 Note→Modules 工厂 差分测试（§10 第1+2条）。
    /// 映射(§15.9/plan)：诗成杀敌·绝句 → FlatPen(40)+CounterMul(evil,2)（克邪两档,对阴邪 demon/ghost/illusion 正气克邪×2,真差分）；
    /// 王法镇压·律狱 → Control(lawPrison,1)（控场困范围,ApplyOnUse 不改 dmg,批4 结算,断言 Kind+Key 在册）。
    /// </summary>
    public class RuModuleMigrationTests
    {
        static CombatSkillDef Skill(string id) =>
            RuXiuHaoranPath.Def.CombatSkills.Single(s => s.Id == id);

        // 攻方儒修空白；防方 evil/非evil 取 TestPaths.WithTags（CounterMul 读防方 tag）。
        static CombatContext Ctx(bool defenderEvil)
        {
            var atk = CultivationState.NewForPath("ru_xiu_haoran", new List<ResourceDef>());
            var def = CultivationState.NewForPath("def_path", new List<ResourceDef>());
            var atkPath = RuXiuHaoranPath.Def;
            var defPath = defenderEvil ? TestPaths.WithTags(new[] { "evil" })
                                       : TestPaths.WithTags(System.Array.Empty<string>());
            return new CombatContext(atk, atkPath, def, defPath);
        }

        static int Resolve(CombatSkillDef sk, int baseDmg, CombatContext ctx)
        {
            int dmg = baseDmg;
            foreach (var op in sk.OnUse) dmg = ModuleResolver.ApplyOnUse(dmg, op, ctx);
            return dmg;
        }

        // —— 绝句：FlatPen(40)+CounterMul(evil)。对 evil 克邪倍乘,非 evil 不乘（真差分）——
        [Fact]
        public void JueJu_CounterMul_VsEvil()
        {
            var sk = Skill("sk_ru_jueju");
            Assert.Contains(sk.OnUse, o => o.Kind == EffectOpKind.CounterMul && o.Key == "evil");
            Assert.Contains(sk.OnUse, o => o.Kind == EffectOpKind.AddPenInteger); // FlatPen 基线

            int vsEvil = Resolve(sk, 0, Ctx(defenderEvil: true));  // 40→×2 联合上界钳 ×3/2 = 60
            int vsNorm = Resolve(sk, 0, Ctx(defenderEvil: false)); // 40
            Assert.True(vsEvil > vsNorm, "绝句对 evil 未触发正气克邪倍乘");
        }

        // —— 律狱：Control（控场困范围）——
        [Fact]
        public void LvYu_IsControl()
        {
            var sk = Skill("sk_ru_lvyu");
            Assert.Contains(sk.OnUse, o => o.Kind == EffectOpKind.Control && o.Key == "lawPrison");
        }

        [Fact]
        public void Def_StillValidAfterMigration()
        {
            PathValidator.AssertValid(RuXiuHaoranPath.Def);
        }
    }
}
