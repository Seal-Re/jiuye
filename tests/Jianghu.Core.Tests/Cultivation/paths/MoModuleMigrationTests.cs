using System.Collections.Generic;
using System.Linq;
using Jianghu.Cultivation;
using Jianghu.Cultivation.Paths;
using Xunit;

namespace Jianghu.Core.Tests.Cultivation.Paths
{
    /// <summary>
    /// B5 批2 魔修招牌招 Note→Modules 工厂 差分测试（§10 第1+2条）。
    /// 映射(§15.9/plan)：灭世魔刀·斩 → PenFromResource(MoGong,1)（蓄魔功越满 power 越高,真差分）；
    /// 血河倾泻 → PenFromResource(MoGong,2)（清空 MoGong 自爆,全槽×2,满槽自爆极痛、空槽哑火真差分）；
    /// 噬元夺脉 → PenFromResource(MoGong,1)（伤害基于 MoGong,一致性优先）；
    /// 夺舍/渡心魔劫 = A.2 道心层(innerDemon/moHeart) defer（保 AddPenInteger(0) 占位）。
    /// 注：MoXiuXinmoPath 在 namespace Jianghu.Cultivation（非 .Paths），由 using Jianghu.Cultivation 覆盖。
    /// </summary>
    public class MoModuleMigrationTests
    {
        static CombatSkillDef Skill(string id) =>
            MoXiuXinmoPath.Def.CombatSkills.Single(s => s.Id == id);

        static CombatContext Ctx(int moGong)
        {
            var atk = CultivationState.NewForPath("mo_xiu_xinmo",
                new[] { new ResourceDef("MoGong", 0, 1000, moGong) });
            var def = CultivationState.NewForPath("def_path", new List<ResourceDef>());
            var path = MoXiuXinmoPath.Def;
            return new CombatContext(atk, path, def, path);
        }

        static int Resolve(CombatSkillDef sk, int baseDmg, CombatContext ctx)
        {
            int dmg = baseDmg;
            foreach (var op in sk.OnUse)
                dmg = ModuleResolver.ApplyOnUse(dmg, op, ctx);
            return dmg;
        }

        // —— 灭世魔刀·斩：PenFromResource(MoGong,1)。蓄魔功越满本招越痛（真差分）——
        [Fact]
        public void MoDao_ScalesWithMoGong()
        {
            var sk = Skill("mo_sk_modao");
            Assert.Contains(sk.OnUse, o => o.Kind == EffectOpKind.PenFromResource && o.Key == "MoGong");

            int full = Resolve(sk, 0, Ctx(moGong: 30)); // 30×1 = 30
            int empty = Resolve(sk, 0, Ctx(moGong: 0)); // 0
            Assert.Equal(30, full);
            Assert.Equal(0, empty);
            Assert.True(full > empty, "灭世魔刀·斩未随 MoGong 缩放（仍是占位定值）");
        }

        // —— 血河倾泻：PenFromResource(MoGong,2)。清空 MoGong 自爆,全槽×2（真差分,满槽双倍）——
        [Fact]
        public void XueHe_ScalesWithMoGong_x2()
        {
            var sk = Skill("mo_sk_xuehe");
            Assert.Contains(sk.OnUse, o => o.Kind == EffectOpKind.PenFromResource && o.Key == "MoGong");

            int full = Resolve(sk, 0, Ctx(moGong: 30)); // 30×2 = 60
            int empty = Resolve(sk, 0, Ctx(moGong: 0)); // 0
            Assert.Equal(30 * 2, full);
            Assert.Equal(0, empty);
            Assert.True(full > empty, "血河倾泻未随 MoGong 缩放");
        }

        // —— 噬元夺脉：PenFromResource(MoGong,1)（一致性迁移，原占位 12）——
        [Fact]
        public void DuoMai_ScalesWithMoGong()
        {
            var sk = Skill("mo_sk_duomai");
            Assert.Contains(sk.OnUse, o => o.Kind == EffectOpKind.PenFromResource && o.Key == "MoGong");
            Assert.True(Resolve(sk, 0, Ctx(moGong: 20)) > Resolve(sk, 0, Ctx(moGong: 0)),
                "噬元夺脉未随 MoGong 缩放");
        }

        // —— 夺舍重生：B5消化 → Special(duoshe) handler 已激活 ——
        [Fact]
        public void DuoShe_UpgradedToSpecial()
        {
            var sk = Skill("mo_sk_duoshe");
            Assert.Contains(sk.OnUse, o => o.Kind == EffectOpKind.Special && o.Key == "duoshe");
        }

        // —— 燃心狂魔：AddResource(burnGate,3) 拉满燃心阀档位——
        [Fact]
        public void RanXin_SetsBurnGate()
        {
            var sk = Skill("mo_sk_ranxin");
            Assert.Contains(sk.OnUse, o => o.Kind == EffectOpKind.AddResource && o.Key == "burnGate" && o.Amount == 3);
        }

        // —— 渡心魔劫·证道：FlatPen(0) 双变体收束（moHeart/innerDemon 纯 A.2 道心层）——
        [Fact]
        public void DuJie_IsFlatPenZero()
        {
            var sk = Skill("mo_sk_dujie");
            Assert.Contains(sk.OnUse, o => o.Kind == EffectOpKind.AddPenInteger && o.Amount == 0);
        }

        [Fact]
        public void Def_StillValidAfterMigration()
        {
            PathValidator.AssertValid(MoXiuXinmoPath.Def);
        }
    }
}
