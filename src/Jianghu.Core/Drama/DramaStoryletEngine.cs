using System;
using System.Collections.Generic;
using Jianghu.Cultivation;
using Jianghu.Events;
using Jianghu.Model;
using Jianghu.Random;

namespace Jianghu.Drama
{
    /// <summary>
    /// 戏剧 storylet 引擎（drama-002）。
    /// 复用 A.2 StoryletDef/StoryletExecutor 框架，扩展关系/恩怨/复仇 storylet。
    /// </summary>
    public static class DramaStoryletEngine
    {
        /// <summary>
        /// 关系奇遇结算——将 StoryletOption 的 RelationDelta 应用到 Relations。
        /// </summary>
        public static int ApplyRelationStorylet(
            Relations relations, CharacterId from, CharacterId to,
            StoryletOption option, long tick, Action<DomainEvent> chronicle)
        {
            if (!option.RelationDelta.HasValue || option.RelationDelta.Value == 0)
                return 0;

            int nv = relations.Adjust(from, to, option.RelationDelta.Value);
            chronicle(new RelationChanged(tick, from, to, option.RelationDelta.Value, nv));
            return nv;
        }

        /// <summary>
        /// 仇敌检测——A 对 B 的好感 < -50 → 视为仇敌。
        /// </summary>
        public static bool IsEnemy(Relations relations, CharacterId from, CharacterId to)
            => relations.Affinity(from, to) <= -50;

        /// <summary>
        /// 复仇链触发——当 B 恶意攻击 A → A 对 B 好感 < -50 → 触发复仇 storylet 候选。
        /// </summary>
        public static bool CheckVengeanceTrigger(Relations relations, CharacterId victim, CharacterId attacker)
            => IsEnemy(relations, victim, attacker);
    }
}
