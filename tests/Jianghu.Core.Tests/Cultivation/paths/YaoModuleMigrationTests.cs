using System.Collections.Generic;
using System.Linq;
using Jianghu.Cultivation;
using Jianghu.Cultivation.Paths;
using Xunit;

namespace Jianghu.Core.Tests.Cultivation.Paths
{
    /// <summary>
    /// B5 批2 妖修招牌招 Note→Modules 工厂 差分测试（§10 第1+2条）。
    /// 映射(§15.9/plan)：血脉爆发·撼天扑 → PenFromResource(yaoDan,3)（武力×2+妖丹×3单点穿透,妖丹越满扑击越狠真差分）。
    /// 返祖=AddTermWeightStep（被动 art 装配期算子,非战技 OnUse）→ 不在本批迁移范围。
    /// </summary>
    public class YaoModuleMigrationTests
    {
        static CombatSkillDef Skill(string id) =>
            YaoXiuHuaxingPath.Def.CombatSkills.Single(s => s.Id == id);

        static CombatContext Ctx(int yaoDan = 0)
        {
            var atk = CultivationState.NewForPath("yao_xiu_huaxing",
                new[] { new ResourceDef("yaoDan", 0, 1000, yaoDan) });
            var def = CultivationState.NewForPath("def_path", new List<ResourceDef>());
            var path = YaoXiuHuaxingPath.Def;
            return new CombatContext(atk, path, def, path);
        }

        static int Resolve(CombatSkillDef sk, int baseDmg, CombatContext ctx)
        {
            int dmg = baseDmg;
            foreach (var op in sk.OnUse)
                dmg = ModuleResolver.ApplyOnUse(dmg, op, ctx);
            return dmg;
        }

        // —— 撼天扑：PenFromResource(yaoDan,3)。妖丹满载单点穿透越狠（真差分）——
        [Fact]
        public void HanTian_ScalesWithYaoDan()
        {
            var sk = Skill("sk_yx_hantian");
            Assert.Contains(sk.OnUse, o => o.Kind == EffectOpKind.PenFromResource && o.Key == "yaoDan");

            int full = Resolve(sk, 0, Ctx(yaoDan: 20)); // 20×3 = 60
            int empty = Resolve(sk, 0, Ctx(yaoDan: 0)); // 0
            Assert.Equal(20 * 3, full);
            Assert.Equal(0, empty);
            Assert.True(full > empty, "撼天扑未随 yaoDan 缩放（仍是占位定值）");
        }

        // —— 妖毒噬体：Dot(yaoDu,2,3) 持续伤积木 ——
        [Fact]
        public void YaoDu_IsDot()
        {
            var sk = Skill("sk_yx_yaodu");
            Assert.Contains(sk.OnUse, o => o.Kind == EffectOpKind.Dot && o.Key == "yaoDu" && o.Amount == 2);
        }

        // —— 妖兽铠·反震：Reflect(1,4) 反震积木 ——
        [Fact]
        public void YaoShouKai_IsReflect()
        {
            var sk = Skill("sk_yx_yaoshou_kai");
            Assert.Contains(sk.OnUse, o => o.Kind == EffectOpKind.ReflectDamage);
        }

        // —— 兽躯硬抗·铜筋：FlatDR(12) 减免积木 ——
        [Fact]
        public void TongJin_IsFlatDR()
        {
            var sk = Skill("sk_yx_tongjin");
            Assert.Contains(sk.OnUse, o => o.Kind == EffectOpKind.AddFlatDR && o.Amount == 12);
        }

        // —— 镇群：FlatPen(15) 破防量 ——
        [Fact]
        public void ZhenQun_IsFlatPen_DamageCalc()
        {
            var sk = Skill("sk_yx_zhenqun");
            Assert.Contains(sk.OnUse, o => o.Kind == EffectOpKind.AddPenInteger);
            int dmg = Resolve(sk, 0, Ctx());
            Assert.Equal(15, dmg);
        }

        [Fact]
        public void Def_StillValidAfterMigration()
        {
            PathValidator.AssertValid(YaoXiuHuaxingPath.Def);
        }
    }
}
