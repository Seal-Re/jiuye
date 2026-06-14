using Jianghu.Config;
using Jianghu.Cultivation;
using Jianghu.Decide;
using Jianghu.Model;
using Jianghu.Random;
using Jianghu.Stats;

namespace Jianghu.Sim
{
    public static class WorldFactory
    {
        private static readonly string[] Xing = { "赵", "钱", "孙", "李", "周", "吴", "郑", "王", "燕", "独孤" };
        private static readonly string[] Ming = { "无忌", "寻欢", "飞", "玄机", "未央", "求败", "三", "九", "白", "青" };

        public static World CreateInitial(ulong seed, LimitsConfig limits, int initialCount,
                                          bool cultivation = false, IPathSource? pathSource = null)
        {
            limits.Validate();
            var root = new Pcg32(seed, 1);
            var genRng = root.Split(1);
            var domainRng = root.Split(2);
            var spawnRng = root.Split(3);
            var brainRngBase = root.Split(4);
            // off：cultRng 不构造（绝不调 Split(5)），保 Split(1..4) 子流编号不变 → 38 测试逐字节。
            var cultRng = cultivation ? root.Split(RngStreamIds.Cultivation) : null;

            var sect = new Sect(1, "无名谷");
            var lifecycle = new Lifecycle(spawnRng.Split(99));
            // on：定路注册表（Phase 4/5 填 21 路；调用方可注入测试/外部源；off 不构造不用）。
            // 升 World 字段 → SparAction 战斗期查对手路 def + 软情境（off=null 不参与，逐字节）。
            var registry = cultivation ? new PathRegistry(pathSource ?? new CodePathSource()) : null;
            var w = new World(limits, domainRng, spawnRng, sect, lifecycle, cultRng, registry);
            w.Nodes.Add(new WorldNode(new NodeId(0), "客栈"));
            w.Nodes.Add(new WorldNode(new NodeId(1), "山道"));
            w.Nodes.Add(new WorldNode(new NodeId(2), "市集"));

            for (int i = 0; i < initialCount; i++)
            {
                var (ch, brain) = Spawn(i, genRng, brainRngBase, limits, w.Clock);
                w.Add(ch, brain);
                // on：在既有 genRng 消费之后 append 定路（经 _cultRng）；off：_cultRng==null 无操作。
                if (registry != null)
                    w.TryAssignCultivation(ch, registry);
            }
            return w;
        }

        /// <summary>随机生成一名江湖人 + 其 RuleBrain（Factory 与 Lifecycle.MaybeSpawn 共用）。</summary>
        public static (Character, IBrain) Spawn(long id, IRandom genRng, IRandom brainRngBase, LimitsConfig limits, long bornAt)
        {
            var arch = (ArchetypeKind)genRng.NextInt(2);
            var goal = arch == ArchetypeKind.Martial ? GoalKind.Advance : GoalKind.Wander;
            var name = Xing[genRng.NextInt(Xing.Length)] + Ming[genRng.NextInt(Ming.Length)];
            var persona = new Persona(name, "江湖客", "市井", arch, 1);
            var stats = StatGenerator.Generate(genRng, limits);
            long lifespan = genRng.NextInclusive((int)limits.LifespanMin, (int)limits.LifespanMax);
            var ch = new Character(new CharacterId(id), persona, stats, new NodeId(genRng.NextInt(3)),
                new Goal(goal, 0), age: 0, lifespan: lifespan, memoryCap: 16);
            var brain = new RuleBrain(brainRngBase.Split((ulong)id), arch);
            return (ch, brain);
        }
    }
}
