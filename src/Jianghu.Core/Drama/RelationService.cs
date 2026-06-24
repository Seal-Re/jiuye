using System;
using Jianghu.Events;
using Jianghu.Model;

namespace Jianghu.Drama
{
    /// <summary>
    /// 戏剧引擎 B——关系服务（drama-001，最小实现）。
    /// 封装 Relations.Adjust + Chronicle 事件。
    /// </summary>
    public static class RelationService
    {
        /// <summary>
        /// 调整关系并记录 Chronicle 事件。
        /// </summary>
        /// <param name="relations">关系图</param>
        /// <param name="from">发起方</param>
        /// <param name="to">目标方</param>
        /// <param name="delta">关系变化量（正=提升好感，负=恶化）</param>
        /// <param name="tick">当前 tick</param>
        /// <param name="chronicle">编年史回调</param>
        /// <returns>调整后的关系值</returns>
        public static int AdjustRelation(
            Relations relations, CharacterId from, CharacterId to, int delta,
            long tick, Action<DomainEvent> chronicle)
        {
            int newValue = relations.Adjust(from, to, delta);
            chronicle(new RelationChanged(tick, from, to, delta, newValue));
            return newValue;
        }

        /// <summary>
        /// 查询两角色间好感。双向独立——A→B 与 B→A 不相等。
        /// </summary>
        public static int GetAffinity(Relations relations, CharacterId from, CharacterId to)
            => relations.Affinity(from, to);
    }
}
