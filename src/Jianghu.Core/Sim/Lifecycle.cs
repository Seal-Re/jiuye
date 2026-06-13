using Jianghu.Config;
using Jianghu.Events;
using Jianghu.Model;
using Jianghu.Random;

namespace Jianghu.Sim
{
    /// <summary>衰老/死亡 + 人口低于下界时复用人设生成器涌现新人（spawn-RNG，§7.4）。</summary>
    public sealed class Lifecycle
    {
        private readonly IRandom _spawnRng;
        private long _nextId = 1_000_000; // 涌现者 Id 段，避免与初始 0..N 撞

        public Lifecycle(IRandom spawnRng) { _spawnRng = spawnRng; }

        public long ActionInterval(Character c, LimitsConfig limits)
        {
            long jitter = c.Stats.Get(Jianghu.Stats.StatKind.Insight) % 5; // 错峰（确定性，不耗共享 RNG）
            return limits.ActionIntervalBase + jitter;
        }

        public void Tick(Character actor, World world, out CharacterDied? died)
        {
            actor.Age += ActionInterval(actor, world.Limits);
            if (actor.Age >= actor.Lifespan)
            {
                died = new CharacterDied(world.Clock, actor.Id, actor.Age);
                return;
            }
            died = null;
        }

        public void MaybeSpawn(World world)
        {
            while (world.AliveCount < world.Limits.PopulationLow)
            {
                long id = _nextId++;
                var genRng = _spawnRng.Split((ulong)id);
                var brainBase = _spawnRng.Split((ulong)id ^ 0xABCDEFUL);
                var (ch, brain) = WorldFactory.Spawn(id, genRng, brainBase, world.Limits, world.Clock);
                world.Add(ch, brain);
            }
        }

        public Lifecycle Clone()
        {
            var p = new Pcg32(0, 1); p.SetState(_spawnRng.GetState());
            var lc = new Lifecycle(p);
            lc._nextId = _nextId;
            return lc;
        }
    }
}
