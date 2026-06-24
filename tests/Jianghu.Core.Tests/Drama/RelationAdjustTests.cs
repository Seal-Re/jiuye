using System;
using System.Collections.Generic;
using Jianghu.Drama;
using Jianghu.Events;
using Jianghu.Model;
using Xunit;

namespace Jianghu.Core.Tests.Drama
{
    /// <summary>
    /// drama-001: 关系调整 tests.
    /// AC: relation delta application, Chronicle events, clamp [-100,100], determinism.
    /// </summary>
    public class RelationAdjustTests
    {
        static List<DomainEvent> Chronicle = new List<DomainEvent>();
        static void Record(DomainEvent e) => Chronicle.Add(e);

        [Fact]
        public void AdjustRelation_PositiveDelta_IncreasesAffinity()
        {
            var rel = new Relations();
            Chronicle.Clear();

            int nv = RelationService.AdjustRelation(rel, new CharacterId(1), new CharacterId(2), 10, 100, Record);

            Assert.Equal(10, nv);
            Assert.Equal(10, RelationService.GetAffinity(rel, new CharacterId(1), new CharacterId(2)));
            Assert.Equal(0, RelationService.GetAffinity(rel, new CharacterId(2), new CharacterId(1))); // 单向
        }

        [Fact]
        public void AdjustRelation_NegativeDelta_DecreasesAffinity()
        {
            var rel = new Relations();
            Chronicle.Clear();

            RelationService.AdjustRelation(rel, new CharacterId(1), new CharacterId(2), -20, 100, Record);

            Assert.Equal(-20, RelationService.GetAffinity(rel, new CharacterId(1), new CharacterId(2)));
        }

        [Fact]
        public void AdjustRelation_Clamps_Max100()
        {
            var rel = new Relations();
            int nv = RelationService.AdjustRelation(rel, new CharacterId(1), new CharacterId(2), 150, 100, Record);
            Assert.Equal(100, nv);
        }

        [Fact]
        public void AdjustRelation_Clamps_MinNeg100()
        {
            var rel = new Relations();
            int nv = RelationService.AdjustRelation(rel, new CharacterId(1), new CharacterId(2), -150, 100, Record);
            Assert.Equal(-100, nv);
        }

        [Fact]
        public void Chronicle_Event_Fires()
        {
            var rel = new Relations();
            Chronicle.Clear();

            RelationService.AdjustRelation(rel, new CharacterId(5), new CharacterId(7), 15, 42, Record);

            Assert.Single(Chronicle);
            var evt = Chronicle[0] as RelationChanged;
            Assert.NotNull(evt);
            Assert.Equal(42L, evt.Tick);
            Assert.Equal(new CharacterId(5), evt.From);
            Assert.Equal(new CharacterId(7), evt.To);
            Assert.Equal(15, evt.Delta);
        }

        [Fact]
        public void Deterministic_SameOps_SameResult()
        {
            var r1 = new Relations();
            var r2 = new Relations();
            RelationService.AdjustRelation(r1, new CharacterId(1), new CharacterId(2), 10, 0, _ => { });
            RelationService.AdjustRelation(r2, new CharacterId(1), new CharacterId(2), 10, 0, _ => { });
            Assert.Equal(
                RelationService.GetAffinity(r1, new CharacterId(1), new CharacterId(2)),
                RelationService.GetAffinity(r2, new CharacterId(1), new CharacterId(2)));
        }
    }
}
