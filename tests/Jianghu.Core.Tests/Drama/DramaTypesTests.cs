using Jianghu.Drama;
using Jianghu.Model;
using Xunit;

namespace Jianghu.Core.Tests.Drama
{
    /// <summary>
    /// 戏剧值类型骨架（drama-004，spec Step 1）。纯 record/enum 值语义，无逻辑。
    /// </summary>
    public class DramaTypesTests
    {
        [Fact]
        public void test_id_value_types_equality()
        {
            Assert.Equal(new GrudgeId(7), new GrudgeId(7));
            Assert.NotEqual(new GrudgeId(7), new GrudgeId(8));
            Assert.Equal(new ArcId(3), new ArcId(3));
            Assert.NotEqual(new ArcId(3), new ArcId(4));
        }

        [Fact]
        public void test_grudge_kind_severity_ordering()
        {
            // 严重度序数：Slaughter > Maiming > Insult（供 max 合并）。
            Assert.True((int)GrudgeKind.Slaughter > (int)GrudgeKind.Maiming);
            Assert.True((int)GrudgeKind.Maiming > (int)GrudgeKind.Insult);
        }

        [Fact]
        public void test_grudge_record_equality()
        {
            var a = new Grudge(new GrudgeId(1), new CharacterId(10), new CharacterId(20),
                GrudgeKind.Slaughter, 80, OriginTick: 100, Generation: 0, GrudgeCause.Direct, InheritedFrom: null);
            var b = new Grudge(new GrudgeId(1), new CharacterId(10), new CharacterId(20),
                GrudgeKind.Slaughter, 80, OriginTick: 100, Generation: 0, GrudgeCause.Direct, InheritedFrom: null);
            var c = a with { Intensity = 50 };
            Assert.Equal(a, b);          // 同字段相等
            Assert.NotEqual(a, c);       // Intensity 异 → 不等
            Assert.Equal(80, a.Intensity);
            Assert.Null(a.InheritedFrom);
        }

        [Fact]
        public void test_grudge_inherited_carries_source()
        {
            var inherited = new Grudge(new GrudgeId(2), new CharacterId(30), new CharacterId(20),
                GrudgeKind.Slaughter, 40, OriginTick: 500, Generation: 1, GrudgeCause.Inherited,
                InheritedFrom: new GrudgeId(1));
            Assert.Equal(GrudgeCause.Inherited, inherited.Cause);
            Assert.Equal(new GrudgeId(1), inherited.InheritedFrom);
            Assert.Equal(1, inherited.Generation);
        }

        [Fact]
        public void test_arc_instance_with_advances_stage_nondestructive()
        {
            var arc = new ArcInstance(new ArcId(1), ArcKind.Revenge, new CharacterId(10), new CharacterId(20),
                ArcStage.Victimized, NextWakeAt: 0, BuildUpBasePower: 0, Completed: false);
            var advanced = arc with { Stage = ArcStage.BuildUp, BuildUpBasePower = 120 };
            Assert.Equal(ArcStage.Victimized, arc.Stage);       // 原不变（非破坏式）
            Assert.Equal(ArcStage.BuildUp, advanced.Stage);
            Assert.Equal(120, advanced.BuildUpBasePower);
            Assert.False(advanced.Completed);
        }

        [Fact]
        public void test_predicate_and_effect_integer_declarative()
        {
            var pred = new Predicate(RoleRef.Self, DramaVar.Power, CmpOp.Ge, 100);
            Assert.Equal(RoleRef.Self, pred.Subject);
            Assert.Equal(DramaVar.Power, pred.Var);
            Assert.Equal(CmpOp.Ge, pred.Op);
            Assert.Equal(100, pred.Threshold);

            var eff = new Effect(EffectKind.AdjustRelation, RoleRef.Holder, RoleRef.Target, Amount: -40, Tag: 0);
            Assert.Equal(EffectKind.AdjustRelation, eff.Kind);
            Assert.Equal(-40, eff.Amount);
        }

        [Fact]
        public void test_drama_profile_optional_master_bloodline()
        {
            var orphan = new DramaProfile(new CharacterId(10), Master: null, Bloodline: null);
            var disciple = new DramaProfile(new CharacterId(11), Master: new CharacterId(10), Bloodline: null);
            Assert.Null(orphan.Master);
            Assert.Equal(new CharacterId(10), disciple.Master);
        }
    }
}
