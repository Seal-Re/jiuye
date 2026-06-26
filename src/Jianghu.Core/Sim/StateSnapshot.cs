using System;
using System.Collections.Generic;
using System.Text;
using Jianghu.Cultivation;
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

            // faction：off/factionOff 时 w.Faction==null → 整段省略（保 off 逐字节）。
            // on 时序列化全门派态（含 Rank/贡献度），补此前 Faction 快照空白（story-010）。
            if (w.Faction != null)
            {
                sb.Append("faction\n");
                sb.Append(w.Faction.CaptureState());
            }

            // drama：off/dramaOff 时 w.Grudges==null → 整段省略（保 off 逐字节，drama-010）。
            // on 时序列化恩怨账本（确定性 All 序：Id/Holder/Target/Kind/Intensity/Gen）。
            if (w.Grudges != null)
            {
                sb.Append("drama\n");
                foreach (var g in w.Grudges.All)
                {
                    sb.Append(g.Id.Value).Append(FieldSep)
                      .Append(g.Holder.Value).Append(FieldSep)
                      .Append(g.Target.Value).Append(FieldSep)
                      .Append((int)g.Kind).Append(FieldSep)
                      .Append(g.Intensity).Append(FieldSep)
                      .Append(g.Generation).Append('\n');
                }
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

            // CultivationState：on 修士非空（off / 散修 == null，此段恒为 "cult=_" 不变 → off 逐字节）。
            // 确定性序列化：Resources/Flags 按 key 排序（不依赖字典枚举序/GetHashCode）；
            // Chosen*/ChosenSkillIds 保留 PathAssigner 抽取序（确定性）。
            // 纳入后快照续跑可抓 CultivationState 惰性漂移（不止靠 Chronicle）。
            sb.Append(FieldSep);
            AppendCultivation(sb, c.Cultivation);
        }

        private static void AppendCultivation(StringBuilder sb, CultivationState? st)
        {
            sb.Append("cult=");
            if (st == null) { sb.Append('_'); return; } // off / 散修：空段，逐字节不变

            sb.Append(st.PathId).Append(',')
              .Append("realm").Append(st.RealmIndex).Append(',')
              .Append("cp").Append(st.CultivationPoints);

            sb.Append(",arts[");
            AppendList(sb, st.ChosenArtIds);   // 抽取序（确定性，不排序）
            sb.Append(']');

            sb.Append(",skills[");
            AppendList(sb, st.ChosenSkillIds); // 抽取序（确定性，不排序）
            sb.Append(']');

            sb.Append(",res[");
            AppendSortedMap(sb, st.Resources); // key 排序（确定性）
            sb.Append(']');

            sb.Append(",flags[");
            AppendSortedMap(sb, st.Flags);     // key 排序（确定性）
            sb.Append(']');
        }

        private static void AppendList(StringBuilder sb, IReadOnlyList<string> items)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (i > 0) sb.Append(ListSep);
                sb.Append(items[i]);
            }
        }

        private static void AppendSortedMap(StringBuilder sb, IReadOnlyDictionary<string, int> map)
        {
            var keys = new List<string>(map.Keys);
            keys.Sort(StringComparer.Ordinal); // 确定性：Ordinal 升序，不依赖字典枚举/GetHashCode
            for (int i = 0; i < keys.Count; i++)
            {
                if (i > 0) sb.Append(ListSep);
                sb.Append(keys[i]).Append(':').Append(map[keys[i]]);
            }
        }
    }
}
