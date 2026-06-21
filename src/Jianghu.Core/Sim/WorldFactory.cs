using System;
using System.Collections.Generic;
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
            if (cultivation)
                DerivedProviders.RegisterAll(); // derived:* provider 注册(stockFirepower/demonWeapon/wenGong/atavismFold)
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

        /// <summary>
        /// 构造典型角色（balance-cross harness用）：中庸四维 Σ=80(各20),
        /// realm=指定UT的段首, 标准loadout（每非道心类目取中位tier功法）, Flags空。
        /// 纯整数、确定性——同输入→同Character，无随机。
        /// 故事：balance-001 BalanceMatrixDump harness。
        /// </summary>
        /// <param name="pathId">修炼路线ID（canon全名）</param>
        /// <param name="ut">目标UT等级（须在pathDef.Curve.UnifiedTierOf中存在）</param>
        /// <param name="pathDef">路线定义（提供Resources/ArtCategories/Curve）</param>
        /// <param name="id">角色ID（默认0，测试可用）</param>
        /// <exception cref="ArgumentException">UT对给定路线不可达时抛出</exception>
        public static Character CreateTypicalChar(string pathId, int ut, CultivationPathDef pathDef, long id = 0)
        {
            // 1. 找到目标UT对应的第一个realmIndex（段首）
            int realmIdx = -1;
            for (int i = 0; i < pathDef.Curve.UnifiedTierOf.Count; i++)
            {
                if (pathDef.Curve.UnifiedTierOf[i] == ut)
                {
                    realmIdx = i;
                    break;
                }
            }
            if (realmIdx < 0)
                throw new ArgumentException(
                    $"UT {ut} 对路线 {pathId} 不可达（max UT = {MaxUT(pathDef.Curve)}）",
                    nameof(ut));

            // 2. 标准loadout：每个非道心ArtCategory取PickMin个中位tier功法
            var chosenArts = new List<string>();
            foreach (var cat in pathDef.ArtCategories)
            {
                if (cat.Role == "daoheart") continue;
                int pick = cat.PickMin;
                var sorted = new List<ArtDef>(cat.Arts);
                sorted.Sort((a, b) => a.Tier.CompareTo(b.Tier));
                int startIdx = Math.Max(0, (sorted.Count - pick) / 2);
                for (int i = 0; i < pick && startIdx + i < sorted.Count; i++)
                    chosenArts.Add(sorted[startIdx + i].Id);
            }

            // 3. 标准战技：取中位tier战技（PickMin个，供DuelEngine对拍用）
            var chosenSkills = new List<string>();
            int skillPick = pathDef.Selection.SkillPickMin;
            if (skillPick > 0 && pathDef.CombatSkills.Count > 0)
            {
                var sortedSkills = new List<CombatSkillDef>(pathDef.CombatSkills);
                sortedSkills.Sort((a, b) => a.Tier.CompareTo(b.Tier));
                int skillStart = Math.Max(0, (sortedSkills.Count - skillPick) / 2);
                for (int i = 0; i < skillPick && skillStart + i < sortedSkills.Count; i++)
                    chosenSkills.Add(sortedSkills[skillStart + i].Id);
            }

            // 4. 构造CultivationState（Flags空，RealmIndex=段首）
            var st = CultivationState.NewForPath(pathId, pathDef.Resources, chosenArts, chosenSkills);
            st.RealmIndex = realmIdx;

            // 5. 构造Character（中庸四维 Σ=80 各20，身份散客）
            var persona = new Persona("典型", "散客", "市井", ArchetypeKind.Martial, null);
            var stats = new StatBlock(new[] { 20, 20, 20, 20 });
            var ch = new Character(
                new CharacterId(id), persona, stats, new NodeId(0),
                new Goal(GoalKind.Advance, 0), age: 0, lifespan: 800, memoryCap: 16);
            ch.Cultivation = st;
            return ch;
        }

        /// <summary>返回RealmCurveDef中最大UT值（纯整数，确定性）。</summary>
        private static int MaxUT(RealmCurveDef curve)
        {
            int max = 0;
            for (int i = 0; i < curve.UnifiedTierOf.Count; i++)
                if (curve.UnifiedTierOf[i] > max)
                    max = curve.UnifiedTierOf[i];
            return max;
        }
    }
}
