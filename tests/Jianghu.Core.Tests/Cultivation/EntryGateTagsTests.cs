using System.Collections.Generic;
using System.Linq;
using Jianghu.Cultivation;
using Xunit;

namespace Jianghu.Core.Tests.Cultivation
{
    /// <summary>
    /// 任务 1（Phase2 #6 缺口修）：生成期灵根 tag 池从注册表派生。
    /// EntryGateDef.RequiredTags() 列本闸所需 tag；PathRegistry.RootTagPool() = 全注册表所需 tag 并集
    /// （确定性排序去重）。加路 → 新路 gate 的 tag 自动入池 → 自动可被定路。
    /// </summary>
    public class EntryGateTagsTests
    {
        [Fact]
        public void Gate_ListsRequiredTags()
        {
            var g = new EntryGateDef("tag:sword_root");
            Assert.Equal(new[] { "sword_root" }, g.RequiredTags());
        }

        [Fact]
        public void Gate_ListsMultipleTags_AndSpace()
        {
            var g = new EntryGateDef("tag:body_root & tag:iron_bone");
            Assert.Equal(new[] { "body_root", "iron_bone" }, g.RequiredTags());
        }

        [Fact]
        public void Gate_EmptyPred_NoTags()
        {
            Assert.Empty(new EntryGateDef("").RequiredTags());
        }

        [Fact]
        public void Registry_RootTagPool_IsUnionOfGates_SortedDistinct()
        {
            var src = new ListPathSource(new[]
            {
                TestPaths.ValidFull() with { PathId = "p_sword", EntryGate = new EntryGateDef("tag:sword_root") },
                TestPaths.ValidFull() with { PathId = "p_body", EntryGate = new EntryGateDef("tag:body_root") },
                // 重复 tag → 去重；乱序声明 → 排序后确定。
                TestPaths.ValidFull() with { PathId = "p_sword2", EntryGate = new EntryGateDef("tag:sword_root") },
            });
            var reg = new PathRegistry(src);
            Assert.Equal(new[] { "body_root", "sword_root" }, reg.RootTagPool());
        }

        // 显式 IPathSource，喂任意路集。
        sealed class ListPathSource : IPathSource
        {
            private readonly IReadOnlyList<CultivationPathDef> _paths;
            public ListPathSource(IReadOnlyList<CultivationPathDef> paths) => _paths = paths;
            public IReadOnlyList<CultivationPathDef> Load() => _paths;
        }
    }
}
