using System.Collections.Generic;
using System.Linq;
using Jianghu.Cultivation;
using Jianghu.Cultivation.Paths;
using Xunit;

namespace Jianghu.Core.Tests.Cultivation.Paths
{
    /// <summary>
    /// B5 批2 驭兽师招牌招 Note→Modules 工厂 差分测试（§10 第1+2条）。
    /// 映射(§15.9/plan)：群兽突袭 → PenFromResource(rosterPower,1)（兽群强度全额集火,兽群越强越痛、空册哑火真差分）；
    /// 灵兽献祭·爆体 = 单兽 beastPower×300 逐兽派生 → FULLSTRUCT defer（保 FlatPen 占位）。
    /// </summary>
    public class YuShouModuleMigrationTests
    {
        static CombatSkillDef Skill(string id) =>
            YuShouPath.Def.CombatSkills.Single(s => s.Id == id);

        static CombatContext Ctx(int rosterPower)
        {
            var atk = CultivationState.NewForPath("yu_shou",
                new[] { new ResourceDef("rosterPower", 0, 1000, rosterPower) });
            var def = CultivationState.NewForPath("def_path", new List<ResourceDef>());
            var path = YuShouPath.Def;
            return new CombatContext(atk, path, def, path);
        }

        static int Resolve(CombatSkillDef sk, int baseDmg, CombatContext ctx)
        {
            int dmg = baseDmg;
            foreach (var op in sk.OnUse)
                dmg = ModuleResolver.ApplyOnUse(dmg, op, ctx);
            return dmg;
        }

        // —— 群兽突袭：PenFromResource(rosterPower,1)。兽群越强集火越痛（真差分）——
        [Fact]
        public void TuXi_ScalesWithRosterPower()
        {
            var sk = Skill("sk_yu_tuxi");
            Assert.Contains(sk.OnUse, o => o.Kind == EffectOpKind.PenFromResource && o.Key == "rosterPower");

            int full = Resolve(sk, 0, Ctx(rosterPower: 40)); // 40×1 = 40
            int empty = Resolve(sk, 0, Ctx(rosterPower: 0)); // 0
            Assert.Equal(40, full);
            Assert.Equal(0, empty);
            Assert.True(full > empty, "群兽突袭未随 rosterPower 缩放（仍是占位定值）");
        }

        // —— 献祭·爆体：FULLSTRUCT defer（逐兽 beastPower×300 派生），仍 AddPenInteger 占位 ——
        [Fact]
        public void XianJi_DeferredFlatPen()
        {
            var sk = Skill("sk_yu_xianji");
            Assert.Contains(sk.OnUse, o => o.Kind == EffectOpKind.AddPenInteger);
            Assert.DoesNotContain(sk.OnUse, o => o.Kind == EffectOpKind.PenFromResource);
        }

        // —— 兽遁·闪：Evade(22) 兽遁闪避 22% 减免——
        [Fact]
        public void ShouDun_IsEvade()
        {
            var sk = Skill("sk_yu_shoudun");
            Assert.Contains(sk.OnUse, o => o.Kind == EffectOpKind.Evade && o.Amount == 22);
        }

        // —— 万兽齐鸣·镇魂：SituationalAdj(0) 反制音修乱兽——
        [Fact]
        public void ZhenHun_IsSituationalAdj()
        {
            var sk = Skill("sk_yu_zhenhun");
            Assert.Contains(sk.OnUse, o => o.Kind == EffectOpKind.AddSituationalAdj);
        }

        // —— 召兽归阵：AddResource(rosterPower,6) 召回调整兽群强度回补——
        [Fact]
        public void GuiZhen_UsesAddResource()
        {
            var sk = Skill("sk_yu_guizhen");
            Assert.Contains(sk.OnUse, o => o.Kind == EffectOpKind.AddResource && o.Key == "rosterPower" && o.Amount == 6);
        }

        [Fact]
        public void Def_StillValidAfterMigration()
        {
            PathValidator.AssertValid(YuShouPath.Def);
        }
    }
}
