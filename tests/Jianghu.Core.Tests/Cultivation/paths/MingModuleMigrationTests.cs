using System.Collections.Generic;
using System.Linq;
using Jianghu.Cultivation;
using Jianghu.Cultivation.Paths;
using Xunit;

namespace Jianghu.Core.Tests.Cultivation.Paths
{
    /// <summary>
    /// B5 批2 命修招牌招 Note→Modules 工厂 差分测试（§10 第1+2条 + §15.3 chokepoint）。
    /// 映射(§15.9/plan)：夺运截命·一击 → Drain(netFortune,8)（夺运=防方 netFortune-8、攻方 +8 经 chokepoint 钳,dmg 不变,断言资源移动）；
    /// 逆演重开 → 逆演栈回滚 batch3 Special defer（保 AddPenInteger(0) 占位）。
    /// </summary>
    public class MingModuleMigrationTests
    {
        static CombatSkillDef Skill(string id) =>
            MingFateCausalityPath.Def.CombatSkills.Single(s => s.Id == id);

        // Drain 需双方共有 netFortune 资源：攻防各给 netFortune（[Min,Cap] 与本路一致允许负与上限）。
        static CombatContext Ctx(int atkNF, int defNF)
        {
            var atk = CultivationState.NewForPath("ming_fate_causality",
                new[] { new ResourceDef("netFortune", -50, 40, atkNF) });
            var def = CultivationState.NewForPath("def_path",
                new[] { new ResourceDef("netFortune", -50, 40, defNF) });
            var path = MingFateCausalityPath.Def;
            return new CombatContext(atk, path, def, path);
        }

        static int Resolve(CombatSkillDef sk, int baseDmg, CombatContext ctx)
        {
            int dmg = baseDmg;
            foreach (var op in sk.OnUse)
                dmg = ModuleResolver.ApplyOnUse(dmg, op, ctx);
            return dmg;
        }

        // —— 夺运截命：Drain(netFortune,8)。防方 netFortune-8、攻方 +8（chokepoint 移动）,dmg 不变 ——
        [Fact]
        public void DuoYun_IsDrain_MovesNetFortune()
        {
            var sk = Skill("sk_mi_duoyun");
            Assert.Contains(sk.OnUse, o => o.Kind == EffectOpKind.DrainResource && o.Key == "netFortune");

            var ctx = Ctx(atkNF: 10, defNF: 20);
            int dmg = Resolve(sk, 100, ctx); // Drain 不改 dmg
            Assert.Equal(100, dmg);
            // 防方 -8、攻方 +8（经 [Min,Cap] 钳，未触界故全额移动）。
            Assert.Equal(10 + 8, ctx.ReadResource(Side.Attacker, "netFortune"));
            Assert.Equal(20 - 8, ctx.ReadResource(Side.Defender, "netFortune"));
        }

        // —— 逆演重开：batch3 Special defer，仍 AddPenInteger(0) 占位 ——
        [Fact]
        public void NiYan_DeferredPlaceholder()
        {
            var sk = Skill("sk_mi_niyan");
            Assert.Contains(sk.OnUse, o => o.Kind == EffectOpKind.AddPenInteger);
            Assert.DoesNotContain(sk.OnUse, o => o.Kind == EffectOpKind.DrainResource);
        }

        [Fact]
        public void Def_StillValidAfterMigration()
        {
            PathValidator.AssertValid(MingFateCausalityPath.Def);
        }
    }
}
