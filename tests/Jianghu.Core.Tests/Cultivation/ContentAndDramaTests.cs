using System.Collections.Generic;
using Jianghu.Cultivation;
using Jianghu.Drama;
using Jianghu.Model;
using Xunit;

namespace Jianghu.Core.Tests.Cultivation
{
    /// <summary>
    /// drama-002 + content-001 tests.
    /// </summary>
    public class ContentAndDramaTests
    {
        // ================================================================
        // content-001: Storylet content batch 1
        // ================================================================

        [Fact]
        public void ContentBatch1_Has_20Storylets()
        {
            Assert.Equal(20, StoryletContentBatch1.All.Count);
        }

        [Fact]
        public void ContentBatch1_WithExampleStorylets_TotalAtLeast30()
        {
            int total = ExampleStorylets.All.Count + StoryletContentBatch1.All.Count;
            Assert.True(total >= 30);
        }

        [Fact]
        public void ContentBatch1_AllStorylets_Valid()
        {
            foreach (var s in StoryletContentBatch1.All)
            {
                Assert.False(string.IsNullOrWhiteSpace(s.Id));
                Assert.False(string.IsNullOrWhiteSpace(s.Title));
                Assert.True(s.Options.Count >= 2);
            }
        }

        // ================================================================
        // drama-002: Drama storylet engine
        // ================================================================

        [Fact]
        public void ApplyRelationStorylet_Positive()
        {
            var rel = new Relations();
            var option = new StoryletOption("帮助", null, null, null, null, RelationDelta: 15);
            var chronicle = new List<Jianghu.Events.DomainEvent>();

            int nv = DramaStoryletEngine.ApplyRelationStorylet(rel, new CharacterId(1), new CharacterId(2),
                option, 100, chronicle.Add);

            Assert.Equal(15, nv);
            Assert.Single(chronicle);
        }

        [Fact]
        public void IsEnemy_BelowNeg50()
        {
            var rel = new Relations();
            rel.Adjust(new CharacterId(1), new CharacterId(2), -60);
            Assert.True(DramaStoryletEngine.IsEnemy(rel, new CharacterId(1), new CharacterId(2)));
        }

        [Fact]
        public void IsEnemy_False_AboveNeg50()
        {
            var rel = new Relations();
            rel.Adjust(new CharacterId(1), new CharacterId(2), -30);
            Assert.False(DramaStoryletEngine.IsEnemy(rel, new CharacterId(1), new CharacterId(2)));
        }

        [Fact]
        public void VengeanceTrigger_WhenEnemy()
        {
            var rel = new Relations();
            rel.Adjust(new CharacterId(1), new CharacterId(2), -80);
            Assert.True(DramaStoryletEngine.CheckVengeanceTrigger(rel,
                new CharacterId(1), new CharacterId(2)));
        }
    }
}
