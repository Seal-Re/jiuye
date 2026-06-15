using System.Collections.Generic;
using System.Linq;
using Jianghu.Cultivation;
using Jianghu.Cultivation.Paths;
using Xunit;

namespace Jianghu.Core.Tests.Cultivation.Paths
{
    /// <summary>
    /// B5 批2 毒蛊修招牌招 Note→Modules 工厂 差分测试（§10 第1+2条）。
    /// 映射(§15.9/plan)：万蛊噬身 → PenFromResource(guSwarmPower,2)（Σ子蛊聚合资源转伤,蛊群越强越痛、空册哑火真差分；真per-child Σ derived→FULLSTRUCT）；
    /// 瘟疫毒雾 → Dot(plague,8,3)（范围毒雾持续掉血3回合,OnUse 挂载,ApplyOnUse 不改 dmg,批4 结算,断言 Kind+Key 在册）；
    /// 夺心控蛊 = mind control batch3 Special defer（保 AddPenInteger 占位）。
    /// </summary>
    public class DuGuModuleMigrationTests
    {
        static CombatSkillDef Skill(string id) =>
            DuGuXiuPath.Def.CombatSkills.Single(s => s.Id == id);

        static CombatContext Ctx(int guSwarmPower)
        {
            var atk = CultivationState.NewForPath("du_gu_xiu",
                new[] { new ResourceDef("guSwarmPower", 0, 1000, guSwarmPower) });
            var def = CultivationState.NewForPath("def_path", new List<ResourceDef>());
            var path = DuGuXiuPath.Def;
            return new CombatContext(atk, path, def, path);
        }

        static int Resolve(CombatSkillDef sk, int baseDmg, CombatContext ctx)
        {
            int dmg = baseDmg;
            foreach (var op in sk.OnUse)
                dmg = ModuleResolver.ApplyOnUse(dmg, op, ctx);
            return dmg;
        }

        // —— 万蛊噬身：PenFromResource(guSwarmPower,2)。蛊群之力越强自爆越痛（真差分）——
        [Fact]
        public void WanGu_ScalesWithGuSwarmPower()
        {
            var sk = Skill("sk_du_wangu");
            Assert.Contains(sk.OnUse, o => o.Kind == EffectOpKind.PenFromResource && o.Key == "guSwarmPower");

            int full = Resolve(sk, 0, Ctx(guSwarmPower: 30)); // 30×2 = 60
            int empty = Resolve(sk, 0, Ctx(guSwarmPower: 0)); // 0
            Assert.Equal(30 * 2, full);
            Assert.Equal(0, empty);
            Assert.True(full > empty, "万蛊噬身未随 guSwarmPower 缩放（仍是占位定值）");
        }

        // —— 瘟疫毒雾：Dot(plague,8/tick,3回合)。OnUse 挂载,批4 结算,断言 Kind+Key 在册 ——
        [Fact]
        public void DuWu_IsDot()
        {
            var sk = Skill("sk_du_duwu");
            Assert.Contains(sk.OnUse, o => o.Kind == EffectOpKind.Dot && o.Key == "plague");
        }

        // —— 夺心控蛊：mind control batch3 Special defer，仍 AddPenInteger 占位 ——
        [Fact]
        public void DuoXin_DeferredPlaceholder()
        {
            var sk = Skill("sk_du_duoxin");
            Assert.Contains(sk.OnUse, o => o.Kind == EffectOpKind.AddPenInteger);
        }

        [Fact]
        public void Def_StillValidAfterMigration()
        {
            PathValidator.AssertValid(DuGuXiuPath.Def);
        }
    }
}
