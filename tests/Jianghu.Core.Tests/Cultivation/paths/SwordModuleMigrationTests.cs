using System.Collections.Generic;
using System.Linq;
using Jianghu.Cultivation;
using Jianghu.Cultivation.Paths;
using Xunit;

namespace Jianghu.Core.Tests.Cultivation.Paths
{
    /// <summary>
    /// B5 批2 Task2.1：剑修招牌招 Note→结构化模块 差分测试（§10 第1+2条，非仅 ≠0）。
    /// 映射(§15.9/plan)：剑二十三 → PenFromResource(swordWill,4)（资源转伤，剑意越满越痛）；
    /// 万剑归宗 → AoePerTarget（R2 单挑退化×1）；舍身一剑 → Backlash（自伤通道，批4 接 selfDmg）。
    /// 「装备 vs 剥离」可观测结算差异 = 招牌招吃 swordWill 资源量，剥离（swordWill=0）则退回基础。
    /// </summary>
    public class SwordModuleMigrationTests
    {
        static CombatSkillDef Skill(string id) =>
            SwordImmortalPath.Def.CombatSkills.Single(s => s.Id == id);

        // 造攻方 ctx：攻方持指定 swordWill，防方空白（剑修对剑修，tag 取剑修属性）。
        static CombatContext Ctx(int swordWill)
        {
            var atk = CultivationState.NewForPath("sword_immortal",
                new[] { new ResourceDef("swordWill", 0, 1000, swordWill) });
            var def = CultivationState.NewForPath("def_path", new List<ResourceDef>());
            var path = SwordImmortalPath.Def;
            return new CombatContext(atk, path, def, path);
        }

        // 把一招的 OnUse 模块顺序施加到 baseDmg，返回结算后 dmg。
        static int Resolve(CombatSkillDef sk, int baseDmg, CombatContext ctx)
        {
            int dmg = baseDmg;
            foreach (var op in sk.OnUse)
                dmg = ModuleResolver.ApplyOnUse(dmg, op, ctx);
            return dmg;
        }

        // —— 剑二十三：PenFromResource(swordWill,4)。剑意越高伤害越高（资源转伤·真差分）——
        [Fact]
        public void Jian23_ScalesWithSwordWill_NotFlat()
        {
            var sk = Skill("sk_sw_jian23");
            // 含 PenFromResource(swordWill) 模块（已结构化，非占位 AddPenInteger）。
            Assert.Contains(sk.OnUse, o => o.Kind == EffectOpKind.PenFromResource && o.Key == "swordWill");

            int withWill = Resolve(sk, 0, Ctx(swordWill: 30)); // 剑意满载
            int noWill   = Resolve(sk, 0, Ctx(swordWill: 0));  // 剑意见底
            // 差分：满载剑意单点终极穿透 = 30×4/1 = 120；见底则 0。装备 vs 剥离结果不同。
            Assert.Equal(30 * 4 / 1, withWill);
            Assert.Equal(0, noWill);
            Assert.True(withWill > noWill, "剑二十三未随 swordWill 缩放（仍是占位定值）");
        }

        // —— 万剑归宗：AoePerTarget（R2 单挑退化×1 = +Amount，群战才放大）——
        [Fact]
        public void WanJian_IsAoePerTarget()
        {
            var sk = Skill("sk_sw_wanjian");
            Assert.Contains(sk.OnUse, o => o.Kind == EffectOpKind.AoePerTarget);
        }

        // —— 舍身一剑：Backlash（自伤通道，本轮 ApplyOnUse 不改 dmg；批4 接 selfDmg）——
        [Fact]
        public void SheShen_IsBacklash()
        {
            var sk = Skill("sk_sw_sheshen");
            Assert.Contains(sk.OnUse, o => o.Kind == EffectOpKind.Backlash);
        }

        // —— 数据 gate：迁移后 Def 仍过 PathValidator（ratio-Kind Amount2≥1，§15.6）——
        [Fact]
        public void Def_StillValidAfterMigration()
        {
            PathValidator.AssertValid(SwordImmortalPath.Def);
        }
    }
}
