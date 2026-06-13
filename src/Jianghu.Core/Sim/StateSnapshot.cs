using System.Text;
using Jianghu.Model;

namespace Jianghu.Sim
{
    /// <summary>
    /// 全状态确定性序列化为规范字符串（spec §4 S0-a / C4）。
    /// 覆盖 Clock + alive（按 Id 排序）+ Relations（排序边）+ Scheduler 堆。
    /// 显式排序、固定分隔符，不依赖 GetHashCode/字典枚举顺序。
    /// 用途：Clone 深拷在快照口径下逐字节对账，建立续跑等价的唯一验证手段。
    /// 所有 INV-DET-* gate 依赖它。
    /// </summary>
    public static class StateSnapshot
    {
        private const char FieldSep = '|';
        private const char ListSep = ';';

        public static string Capture(World w)
        {
            var sb = new StringBuilder();

            sb.Append("clock=").Append(w.Clock).Append('\n');

            // alive：World.AliveCharacters() 已按 Id 升序。
            sb.Append("alive\n");
            foreach (var c in w.AliveCharacters())
            {
                AppendCharacter(sb, c);
                sb.Append('\n');
            }

            // relations：Relations.Edges() 已按 (from,to) 升序。
            sb.Append("relations\n");
            foreach (var e in w.Relations.Edges())
            {
                sb.Append(e.From).Append(FieldSep)
                  .Append(e.To).Append(FieldSep)
                  .Append(e.Value).Append('\n');
            }

            // scheduler：堆数组顺序确定（Push/Pop 路径确定），直接序列化即可对账续跑。
            sb.Append("scheduler\n");
            foreach (var it in w.SchedulerSnapshot())
            {
                sb.Append(it.Id.Value).Append(FieldSep)
                  .Append(it.At).Append('\n');
            }

            return sb.ToString();
        }

        private static void AppendCharacter(StringBuilder sb, Character c)
        {
            sb.Append(c.Id.Value).Append(FieldSep);

            // 四维：ToArray() 固定 [Force,Internal,Constitution,Insight] 顺序。
            var stats = c.Stats.ToArray();
            sb.Append("stats=");
            for (int i = 0; i < stats.Length; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(stats[i]);
            }
            sb.Append(FieldSep);

            sb.Append("node=").Append(c.Node.Value).Append(FieldSep);
            sb.Append("goal=").Append((int)c.Goal.Kind).Append(',').Append(c.Goal.Progress).Append(FieldSep);
            sb.Append("next=").Append(c.NextActAt).Append(FieldSep);
            sb.Append("alive=").Append(c.Alive ? 1 : 0).Append(FieldSep);

            // 记忆摘要：RecallMemory() 顺序确定（插入/淘汰路径确定），逐条序列化。
            // Character.Clone 深拷 _memory，漏拷在此对账下会暴露。
            sb.Append("mem=");
            var mem = c.RecallMemory();
            for (int i = 0; i < mem.Count; i++)
            {
                if (i > 0) sb.Append(ListSep);
                var m = mem[i];
                sb.Append(m.Tick).Append(':').Append(m.Kind).Append(':')
                  .Append(m.Subject.Value).Append(':')
                  .Append(m.Object.HasValue ? m.Object.Value.Value.ToString() : "_").Append(':')
                  .Append(m.Valence);
            }

            // TODO(Phase 2)：Character.Cultivation 字段加入后，在此纳入 CultivationState 快照
            // （PathId/RealmIndex/Resources(排序)/Flags(排序)/Chosen*）。A.0 v1.0 现态无此字段。
        }
    }
}
