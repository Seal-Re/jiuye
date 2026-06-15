using System.Collections.Generic;
using System.Linq;
using Jianghu.Cultivation;
using Jianghu.Cultivation.Paths;
using Xunit;

namespace Jianghu.Core.Tests.Cultivation.Paths
{
    /// <summary>
    /// B5 批2 Task2.2：体修招牌招 Note→结构化模块 差分测试（§10 第1+2条，非仅 ≠0）。
    /// 映射(§15.9/plan)：燃血狂攻 → PenFromResource(qixue,6,÷10)（每10点血气转武力威+6,血池越满越痛）；
    /// 铁山靠(横练护体) → ReflectDamage(OnDefend)（整段攻势反弹,时序批4接,本轮ApplyOnUse不改入伤）。
    /// 「装备 vs 剥离」可观测差异 = 燃血吃 qixue 资源量,剥离(qixue=0)则退回基础。
    /// </summary>
    public class BodyModuleMigrationTests
    {
        static CombatSkillDef Skill(string id) =>
            BodyHenglianPath.Def.CombatSkills.Single(s => s.Id == id);

        static CombatContext Ctx(int qixue)
        {
            var atk = CultivationState.NewForPath("ti_xiu_hengshi",
                new[] { new ResourceDef("qixue", 0, 1000, qixue) });
            var def = CultivationState.NewForPath("def_path", new List<ResourceDef>());
            var path = BodyHenglianPath.Def;
            return new CombatContext(atk, path, def, path);
        }

        static int Resolve(CombatSkillDef sk, int baseDmg, CombatContext ctx)
        {
            int dmg = baseDmg;
            foreach (var op in sk.OnUse)
                dmg = ModuleResolver.ApplyOnUse(dmg, op, ctx);
            return dmg;
        }

        // —— 燃血狂攻：PenFromResource(qixue,6,÷10)。血气越满伤越高（每10点+6,真差分）——
        [Fact]
        public void RanXue_ScalesWithQixue_NotFlat()
        {
            var sk = Skill("sk_ti_ranxue");
            Assert.Contains(sk.OnUse, o => o.Kind == EffectOpKind.PenFromResource && o.Key == "qixue");

            int full = Resolve(sk, 0, Ctx(qixue: 100)); // 满池：100×6/10 = 60
            int empty = Resolve(sk, 0, Ctx(qixue: 0));  // 见底：0
            Assert.Equal(100 * 6 / 10, full);
            Assert.Equal(0, empty);
            Assert.True(full > empty, "燃血狂攻未随 qixue 缩放（仍是占位定值）");
        }

        // —— 铁山靠：ReflectDamage(OnDefend)。结构断言（时序结算批4接,ApplyOnUse 本轮不改 dmg）——
        [Fact]
        public void TieShanKao_IsReflectDamage_OnDefend()
        {
            var sk = Skill("sk_ti_henglianhuti");
            var reflect = sk.OnUse.FirstOrDefault(o => o.Kind == EffectOpKind.ReflectDamage);
            Assert.NotNull(reflect);
            Assert.Equal(EffectTrigger.OnDefend, reflect!.Trigger);
            Assert.True(reflect.Amount2 >= 1, "ReflectDamage 是 ratio-Kind 须 Amount2≥1(§15.6)");
        }

        // —— 数据 gate：迁移后 Def 仍过 PathValidator ——
        [Fact]
        public void Def_StillValidAfterMigration()
        {
            PathValidator.AssertValid(BodyHenglianPath.Def);
        }
    }
}
