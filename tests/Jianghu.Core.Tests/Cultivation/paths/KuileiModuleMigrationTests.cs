using System.Collections.Generic;
using System.Linq;
using Jianghu.Cultivation;
using Jianghu.Cultivation.Paths;
using Xunit;

namespace Jianghu.Core.Tests.Cultivation.Paths
{
    /// <summary>
    /// B5 批2 傀儡师招牌招 Note→Modules 工厂 差分测试（§10 第1+2条）。
    /// 映射(§15.9/plan)：万傀齐攻 → PenFromResource(fleetWeighted,1)（傀儡军团集火,带宽乘子全额=军团越强越痛、空册哑火真差分）；
    /// 断链应急·影替傀 = batch3 Special defer（保 AddFlatDR/SetFlag 占位）。
    /// </summary>
    public class KuileiModuleMigrationTests
    {
        static CombatSkillDef Skill(string id) =>
            KuileiShiPath.Def.CombatSkills.Single(s => s.Id == id);

        static CombatContext Ctx(int fleetWeighted)
        {
            var atk = CultivationState.NewForPath("kuilei_shi",
                new[] { new ResourceDef("fleetWeighted", 0, 1000, fleetWeighted) });
            var def = CultivationState.NewForPath("def_path", new List<ResourceDef>());
            var path = KuileiShiPath.Def;
            return new CombatContext(atk, path, def, path);
        }

        static int Resolve(CombatSkillDef sk, int baseDmg, CombatContext ctx)
        {
            int dmg = baseDmg;
            foreach (var op in sk.OnUse)
                dmg = ModuleResolver.ApplyOnUse(dmg, op, ctx);
            return dmg;
        }

        // —— 万傀齐攻：PenFromResource(fleetWeighted,1)。傀儡军团越强集火越痛（真差分）——
        [Fact]
        public void WanKuiQi_ScalesWithFleetWeighted()
        {
            var sk = Skill("sk_kl_wankuiqi");
            Assert.Contains(sk.OnUse, o => o.Kind == EffectOpKind.PenFromResource && o.Key == "fleetWeighted");

            int full = Resolve(sk, 0, Ctx(fleetWeighted: 40)); // 40×1 = 40
            int empty = Resolve(sk, 0, Ctx(fleetWeighted: 0)); // 0
            Assert.Equal(40, full);
            Assert.Equal(0, empty);
            Assert.True(full > empty, "万傀齐攻未随 fleetWeighted 缩放（仍是占位定值）");
        }

        // —— 断链应急·影替傀：batch3 Special defer，仍 AddFlatDR 占位 ——
        [Fact]
        public void YingTi_DeferredPlaceholder()
        {
            var sk = Skill("sk_kl_yingti");
            Assert.Contains(sk.OnUse, o => o.Kind == EffectOpKind.AddFlatDR);
            Assert.DoesNotContain(sk.OnUse, o => o.Kind == EffectOpKind.PenFromResource);
        }

        [Fact]
        public void Def_StillValidAfterMigration()
        {
            PathValidator.AssertValid(KuileiShiPath.Def);
        }
    }
}
